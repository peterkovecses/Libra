import test from "node:test";
import assert from "node:assert/strict";

class Element {
    constructor(tag = "div") {
        this.tag = tag;
        this.children = [];
        this.attributes = {};
        this.listeners = {};
        this.classList = { add: (...names) => { this.className += ` ${names.join(" ")}`; } };
        this.hidden = false;
        this.value = "";
    }

    append(...children) { this.children.push(...children); }
    appendChild(child) { this.append(child); return child; }
    replaceChildren(...children) { this.children = children; }
    setAttribute(name, value) { this.attributes[name] = value; }
    addEventListener(name, callback) { this.listeners[name] = callback; }
    dispatch(name) { this.listeners[name]?.({ target: this, key: "", preventDefault() {} }); }
    remove() { this.removed = true; }
    querySelector(selector) { return this.elements?.[selector]; }
    querySelectorAll(selector) {
        const tags = selector.split(",").map(tag => tag.trim());
        return this.children.flatMap(child => [
            ...(tags.includes(child.tag) ? [child] : []),
            ...child.querySelectorAll(selector)
        ]);
    }
    set innerHTML(html) {
        this.html = html;
        this.elements = html === "books"
            ? {
                "#books-container": new Element(),
                "#pagination-container": new Element(),
                "#book-count": new Element()
            }
            : {
                "#book-details-container": new Element(),
                ".panel-actions": Object.assign(new Element(), { hidden: true }),
                "#edit-book-button": new Element("button"),
                "#cancel-book-button": new Element("button"),
                "#book-edit-error": new Element()
            };
    }
}

globalThis.document = {
    createElement: tag => new Element(tag),
    head: {
        appendChild(link) {
            queueMicrotask(() => link.onload());
        }
    }
};

const { validateBook, validateBooksPage } =
    await import("../Libra.Api/wwwroot/features/books/common/validation/book-validation.js");
const { HttpError, fetchResponse } = await import("../Libra.Api/wwwroot/js/http.js");
const { loadStylesheet, removeFeatureStylesheet } =
    await import("../Libra.Api/wwwroot/js/stylesheet.js");
const booksFeature = await import("../Libra.Api/wwwroot/features/books/get-books/get-books.js");
const detailsFeature = await import("../Libra.Api/wwwroot/features/books/get-book-details/get-book-details.js");

const book = { id: 1, title: "A book", version: "1", authors: [{ id: 2, name: "An author" }] };
const page = (pageNumber, pageSize = 10) => ({
    items: [book], totalCount: 20, pageNumber, pageSize,
    totalPages: Math.ceil(20 / pageSize),
    hasPreviousPage: pageNumber > 1,
    hasNextPage: pageNumber < Math.ceil(20 / pageSize)
});
const response = (status, body) => ({
    ok: status >= 200 && status < 300,
    status,
    json: async () => body,
    text: async () => body
});
const wait = () => new Promise(resolve => setImmediate(resolve));

test("validates book and pagination responses including nested data", () => {
    assert.equal(validateBook(book, 1), book);
    for (const invalid of [null, {}, { ...book, id: 0 }, { ...book, title: " " },
        { ...book, authors: null }, { ...book, authors: [{}] }]) {
        assert.throws(() => validateBook(invalid, 1), /Invalid book/);
    }
    assert.throws(() => validateBook(book, 2), /Invalid book/);
    assert.deepEqual(validateBooksPage(page(1), 1, 10), page(1));
    for (const invalid of [
        { ...page(1), items: [{}] },
        { ...page(1), pageSize: 0 },
        { ...page(1), totalPages: 7 },
        { ...page(1), hasNextPage: false }
    ]) {
        assert.throws(() => validateBooksPage(invalid, 1, 10), /Invalid paginated/);
    }
});

test("preserves HTTP status", async () => {
    globalThis.fetch = async () => response(503);
    await assert.rejects(fetchResponse("/api/books"), error =>
        error instanceof HttpError && error.status === 503);
});

test("shares stylesheet loading, honors stale navigation and retries failures", async () => {
    const links = [];
    document.head.appendChild = link => links.push(link);
    const url = new URL("https://example.test/feature.css");
    const stale = loadStylesheet(url, { feature: true, isCurrent: () => false });
    const current = loadStylesheet(url, { feature: true });
    assert.equal(links.length, 1);
    assert.equal(links[0].media, "not all");
    links[0].onload();
    await Promise.all([stale, current]);
    assert.equal(links[0].media, "all");
    removeFeatureStylesheet();
    assert.equal(links[0].media, "not all");

    const bad = new URL("https://example.test/bad.css");
    const failed = loadStylesheet(bad);
    links[1].onerror();
    await assert.rejects(failed, /Failed to load stylesheet/);
    assert.equal(links[1].removed, true);
    const retry = loadStylesheet(bad);
    links[2].onload();
    await retry;
    document.head.appendChild = link => queueMicrotask(() => link.onload());
});

test("pagination hides inconsistent controls and retries the failed page and page size", async () => {
    const requests = [];
    let failPage = true;
    globalThis.fetch = async url => {
        if (url instanceof URL) return response(200, "books");
        requests.push(url);
        const query = new URL(url, "https://example.test").searchParams;
        const number = Number(query.get("pageNumber"));
        const size = Number(query.get("pageSize"));
        if (failPage) return response(503);
        return response(200, page(number, size));
    };
    const container = new Element();
    const originalError = console.error;
    console.error = () => {};
    try {
        await booksFeature.load(container);
        const contents = container.querySelector("#books-container");
        const pagination = container.querySelector("#pagination-container");
        assert.equal(pagination.hidden, true);
        assert.match(contents.children[0].children[0].textContent, /HTTP 503/);
        failPage = false;
        contents.children[0].children[1].dispatch("click");
        await wait();
        assert.equal(pagination.hidden, false);
        const root = pagination.children[0];
        const select = root.children[1].children[1];
        const next = root.children[2].children[2];
        failPage = true;
        next.dispatch("click");
        await wait();
        assert.equal(pagination.hidden, true);
        assert.match(requests.at(-1), /pageNumber=2/);
        failPage = false;
        contents.children[0].children[1].dispatch("click");
        await wait();
        assert.equal(root.children[2].children[1].textContent, "Page 2 of 2");
        failPage = true;
        select.value = "25";
        select.dispatch("change");
        await wait();
        assert.equal(select.value, 10);
        assert.equal(pagination.hidden, true);
        failPage = false;
        contents.children[0].children[1].dispatch("click");
        await wait();
        assert.match(requests.at(-1), /pageNumber=1&pageSize=25/);
        assert.equal(select.value, 25);
        assert.equal(root.children[2].children[1].textContent, "Page 1 of 1");
    } finally {
        console.error = originalError;
    }
});

test("pagination returns to an available page if the collection shrinks", async () => {
    const requests = [];
    globalThis.fetch = async url => {
        if (url instanceof URL) return response(200, "books");
        requests.push(url);
        const number = Number(new URL(url, "https://example.test").searchParams.get("pageNumber"));
        if (requests.length === 1) return response(200, page(1));
        if (number === 2) {
            return response(200, {
                items: [], totalCount: 1, pageNumber: 2, pageSize: 10,
                totalPages: 1, hasPreviousPage: true, hasNextPage: false
            });
        }
        return response(200, {
            items: [book], totalCount: 1, pageNumber: 1, pageSize: 10,
            totalPages: 1, hasPreviousPage: false, hasNextPage: false
        });
    };
    const container = new Element();
    await booksFeature.load(container);
    const pagination = container.querySelector("#pagination-container");
    const root = pagination.children[0];
    root.children[2].children[2].dispatch("click");
    await wait();
    assert.match(requests.at(-1), /pageNumber=1&pageSize=10/);
    assert.equal(root.children[2].children[1].textContent, "Page 1 of 1");
    assert.equal(pagination.hidden, false);
});

test("a stale books response does not update the page", async () => {
    let current = true;
    let resolveResponse;
    globalThis.fetch = async url => url instanceof URL
        ? response(200, "books")
        : new Promise(resolve => { resolveResponse = resolve; });
    const container = new Element();
    const loading = booksFeature.load(container, () => current);
    await wait();
    current = false;
    resolveResponse(response(200, page(1)));
    await loading;
    assert.equal(container.querySelector("#books-container").children[0].children[0].textContent, "Loading books...");
});

test("book details distinguish 404, malformed success, and valid success", async () => {
    let apiResponse;
    globalThis.fetch = async url => url instanceof URL
        ? response(200, "details")
        : apiResponse;
    const originalError = console.error;
    console.error = () => {};
    try {
        const container = new Element();
        apiResponse = response(404);
        await detailsFeature.load(container, () => true, { bookId: 1 });
        assert.equal(container.querySelector("#book-details-container").children[0].children[0].textContent, "Book not found. (HTTP 404)");
        assert.equal(container.querySelector(".panel-actions").hidden, true);
        apiResponse = response(200, {});
        await detailsFeature.load(container, () => true, { bookId: 1 });
        assert.equal(container.querySelector("#book-details-container").children[0].className, "state-message error");
        assert.equal(container.querySelector(".panel-actions").hidden, true);
        apiResponse = response(200, book);
        await detailsFeature.load(container, () => true, { bookId: 1 });
        assert.equal(container.querySelector("#book-details-container").children[0].className, "book-details");
        assert.equal(container.querySelector(".panel-actions").hidden, false);
    } finally {
        console.error = originalError;
    }
});

test("editing loads every authors page, saves the draft and retains it on conflict", async () => {
    let updateStatus = 409;
    const requests = [];
    globalThis.fetch = async (url, options) => {
        if (url instanceof URL) return response(200, "details");
        requests.push({ url, options });
        if (url.startsWith("/api/authors")) {
            const pageNumber = Number(new URL(url, "https://example.test").searchParams.get("pageNumber"));
            const items = pageNumber === 1
                ? Array.from({ length: 100 }, (_, i) => ({ id: i + 2, name: `Author ${i + 2}` }))
                : [{ id: 102, name: "Last author" }];
            return response(200, { items, totalCount: 101, pageNumber, pageSize: 100, totalPages: 2, hasNextPage: pageNumber === 1 });
        }
        if (options?.method === "PUT") {
            return response(updateStatus, { ...book, title: "Changed", version: "2", authors: [{ id: 102, name: "Last author" }] });
        }
        return response(200, book);
    };
    const originalError = console.error;
    console.error = () => {};
    try {
        const container = new Element();
        await detailsFeature.load(container, () => true, { bookId: 1 });
        const edit = container.querySelector("#edit-book-button");
        const details = container.querySelector("#book-details-container");
        const errorBox = container.querySelector("#book-edit-error");
        edit.dispatch("click");
        await wait();
        assert.equal(requests.filter(request => request.url.startsWith("/api/authors")).length, 2);
        const draft = details.children[0];
        draft.children[1].value = "Changed";
        draft.children[1].dispatch("input");
        draft.children[3].children[0].children[1].dispatch("click");
        const select = details.children[0].children[3].children[0];
        select.value = "102";
        select.dispatch("change");
        edit.dispatch("click");
        await wait();
        assert.deepEqual(JSON.parse(requests.at(-1).options.body), {
            title: "Changed", authorIds: [102], version: "1"
        });
        assert.match(errorBox.children[0].textContent, /changed elsewhere/);
        assert.equal(details.children[0].children[1].value, "Changed");
        updateStatus = 200;
        edit.dispatch("click");
        await wait();
        assert.equal(edit.textContent, "Edit");
        assert.equal(details.children[0].children[3].textContent, "Changed");
    } finally {
        console.error = originalError;
    }
});
