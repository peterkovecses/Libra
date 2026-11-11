import test from "node:test";
import assert from "node:assert/strict";
import { spawn, spawnSync } from "node:child_process";
import { mkdtemp, mkdir, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";
import { createServer } from "node:net";

test("upgrades an existing database and updates books transactionally", async () => {
    const root = await mkdtemp(join(tmpdir(), "libra-api-test-"));
    await mkdir(join(root, "App_Data"));
    const db = join(root, "App_Data", "libra.db");
    const setup = spawnSync("python3", ["-c", `
import sqlite3, sys
db = sqlite3.connect(sys.argv[1])
db.executescript("""
CREATE TABLE Books (Id INTEGER PRIMARY KEY AUTOINCREMENT, Title TEXT NOT NULL);
CREATE TABLE Authors (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL);
CREATE TABLE BookAuthors (BookId INTEGER NOT NULL, AuthorId INTEGER NOT NULL,
    PRIMARY KEY (BookId, AuthorId), FOREIGN KEY (BookId) REFERENCES Books(Id) ON DELETE CASCADE,
    FOREIGN KEY (AuthorId) REFERENCES Authors(Id) ON DELETE CASCADE);
INSERT INTO Books VALUES (101, 'Existing book');
INSERT INTO Authors VALUES (101, 'Existing author');
INSERT INTO BookAuthors VALUES (101, 101);
""")
db.executemany("INSERT INTO Authors(Id, Name) VALUES (?, ?)",
    [(i, f'Extra author {i:03}') for i in range(102, 207)])
db.commit()
`, db], { encoding: "utf8" });
    assert.equal(setup.status, 0, setup.stderr);

    const port = await new Promise((resolvePort, reject) => {
        const server = createServer();
        server.on("error", reject);
        server.listen(0, "127.0.0.1", () => {
            const port = server.address().port;
            server.close(() => resolvePort(port));
        });
    });
    const base = `http://127.0.0.1:${port}`;
    const app = spawn("dotnet", [
        resolve("Libra.Api/bin/Debug/net11.0/Libra.Api.dll"),
        "--contentRoot", root, "--urls", base
    ], { env: { ...process.env, ASPNETCORE_ENVIRONMENT: "Production" }, stdio: ["ignore", "pipe", "pipe"] });
    let output = "";
    app.stdout.on("data", chunk => { output += chunk; });
    app.stderr.on("data", chunk => { output += chunk; });
    const request = async (path, body) => fetch(base + path, body === undefined ? undefined : {
        method: "PUT", headers: { "content-type": "application/json" }, body: JSON.stringify(body)
    });
    try {
        let ready = false;
        for (let attempt = 0; attempt < 100; attempt++) {
            if (app.exitCode !== null) throw new Error(`API exited early: ${output}`);
            try {
                if ((await fetch(base + "/api/books/101")).ok) { ready = true; break; }
            } catch {
                await new Promise(resolveDelay => setTimeout(resolveDelay, 100));
            }
        }
        assert.ok(ready, `API failed to start: ${output}`);
        const initial = await (await request("/api/books/101")).json();
        assert.deepEqual(initial, { id: 101, title: "Existing book", version: "1", authors: [{ id: 101, name: "Existing author" }] });

        const page1 = await (await request("/api/authors?pageNumber=1&pageSize=100")).json();
        const page2 = await (await request("/api/authors?pageNumber=2&pageSize=100")).json();
        const authors = [...page1.items, ...page2.items];
        assert.equal(authors.length, 126);
        assert.equal(page1.hasNextPage, true);
        assert.equal(page2.hasNextPage, false);
        assert.ok(authors.some(author => author.id === 206));
        assert.deepEqual(authors, [...authors].sort((a, b) => a.name < b.name ? -1 : a.name > b.name ? 1 : a.id - b.id));

        const update = { title: "Changed", authorIds: [101, 102], version: initial.version };
        const updatedResponse = await request("/api/books/101", update);
        assert.equal(updatedResponse.status, 200);
        const updated = await updatedResponse.json();
        assert.equal(updated.title, "Changed");
        assert.equal(updated.version, "2");
        assert.deepEqual(updated.authors.map(author => author.id).sort((a, b) => a - b), [101, 102]);

        const stale = await request("/api/books/101", update);
        assert.equal(stale.status, 409);
        assert.match(stale.headers.get("content-type"), /problem\+json/);
        assert.equal((await request("/api/books/999999", update)).status, 404);
        const invalid = await request("/api/books/101", { title: "Rollback", authorIds: [102, 999999], version: "2" });
        assert.equal(invalid.status, 400);
        assert.ok((await invalid.json()).errors.AuthorIds);
        assert.deepEqual(await (await request("/api/books/101")).json(), updated);

        for (const payload of [
            { title: " ", authorIds: [101], version: "2" },
            { title: "Test", authorIds: [0, 0], version: "2" },
            { title: "Test", authorIds: null, version: "2" },
            { title: "Test", authorIds: [], version: "abc" },
            { title: "Test", authorIds: [], version: 2 }
        ]) assert.equal((await request("/api/books/101", payload)).status, 400);
        assert.equal((await request("/api/books/0", update)).status, 400);

        const removed = await request("/api/books/101", { title: "No authors", authorIds: [], version: "2" });
        assert.equal(removed.status, 200);
        assert.deepEqual(await removed.json(), { id: 101, title: "No authors", version: "3", authors: [] });
        const races = await Promise.all([
            request("/api/books/101", { title: "First", authorIds: [101], version: "3" }),
            request("/api/books/101", { title: "Second", authorIds: [102], version: "3" })
        ]);
        assert.deepEqual(races.map(response => response.status).sort(), [200, 409]);

        const setVersion = spawnSync("python3", ["-c", `
import sqlite3, sys
db = sqlite3.connect(sys.argv[1])
db.execute("UPDATE Books SET Version = 9007199254740993 WHERE Id = 101")
db.commit()
`, db], { encoding: "utf8" });
        assert.equal(setVersion.status, 0, setVersion.stderr);
        const large = await (await request("/api/books/101")).json();
        assert.equal(large.version, "9007199254740993");
        const largeUpdate = await request("/api/books/101", { title: "Large version", authorIds: [], version: large.version });
        assert.equal(largeUpdate.status, 200);
        assert.equal((await largeUpdate.json()).version, "9007199254740994");
    } finally {
        if (app.exitCode === null && app.signalCode === null) {
            const exited = new Promise(resolveExit => app.once("exit", resolveExit));
            app.kill("SIGTERM");
            await exited;
        }
        await rm(root, { recursive: true, force: true });
    }
});
