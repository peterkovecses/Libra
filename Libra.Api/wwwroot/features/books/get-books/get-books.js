
const htmlUrl = new URL("./get-books.html", import.meta.url);
const cssUrl = new URL("./get-books.css", import.meta.url);

export async function load(container, isCurrent = () => true) {
    const response = await fetch(htmlUrl);

    if (!response.ok) {
        throw new Error(
            `Failed to load GetBooks HTML (${response.status})`
        );
    }

    const html = await response.text();

    if (!isCurrent()) {
        return;
    }

    await loadStylesheet();

    if (!isCurrent()) {
        return;
    }

    container.innerHTML = html;

    await loadBooks(container, isCurrent);
}

async function loadStylesheet() {
    const existingStylesheet = document.querySelector(
        "[data-feature-stylesheet]"
    );

    if (existingStylesheet?.href === cssUrl.href) {
        return;
    }

    existingStylesheet?.remove();

    const stylesheet = document.createElement("link");

    stylesheet.rel = "stylesheet";
    stylesheet.href = cssUrl.href;
    stylesheet.dataset.featureStylesheet = "";

    const loaded = new Promise((resolve, reject) => {
        stylesheet.onload = resolve;

        stylesheet.onerror = () => {
            reject(
                new Error("Failed to load GetBooks stylesheet.")
            );
        };
    });

    document.head.appendChild(stylesheet);

    try {
        await loaded;
    } catch (error) {
        stylesheet.remove();
        throw error;
    }
}

async function loadBooks(container, isCurrent) {
    const booksContainer = container.querySelector(
        "#books-container"
    );

    const bookCount = container.querySelector("#book-count");

    try {
        const response = await fetch("/api/books");

        if (!response.ok) {
            throw new Error(
                `Failed to load books (${response.status})`
            );
        }

        const books = await response.json();

        if (!isCurrent()) {
            return;
        }

        if (!Array.isArray(books)) {
            throw new Error("Invalid books response.");
        }

        renderBooks(books, booksContainer, bookCount);
    } catch (error) {
        if (!isCurrent()) {
            return;
        }

        console.error("Error loading books:", error);

        renderError(booksContainer, bookCount);
    }
}

function renderBooks(books, container, bookCount) {
    bookCount.textContent = `${books.length} books`;

    if (books.length === 0) {
        renderState(
            container,
            "No books found in the library."
        );

        return;
    }

    const table = document.createElement("table");
    table.className = "books-table";

    const thead = document.createElement("thead");
    const headerRow = document.createElement("tr");

    for (const label of ["ID", "Title", "Authors"]) {
        const th = document.createElement("th");
        th.textContent = label;

        headerRow.appendChild(th);
    }

    thead.appendChild(headerRow);
    table.appendChild(thead);

    const tbody = document.createElement("tbody");

    for (const book of books) {
        const row = document.createElement("tr");

        const idCell = document.createElement("td");
        idCell.className = "book-id";
        idCell.textContent = `#${book.id}`;

        const titleCell = document.createElement("td");
        titleCell.className = "book-title";
        titleCell.textContent = book.title ?? "";

        const authorsCell = document.createElement("td");
        authorsCell.className = "author-list";
        authorsCell.textContent = (book.authors ?? [])
            .map(author => author.name)
            .join(", ");

        row.append(idCell, titleCell, authorsCell);

        tbody.appendChild(row);
    }

    table.appendChild(tbody);

    const wrapper = document.createElement("div");
    wrapper.className = "books-table-wrapper";
    wrapper.appendChild(table);

    container.replaceChildren(wrapper);
}

function renderState(container, message, isError = false) {
    const element = document.createElement("div");

    element.className = isError
        ? "state-message error"
        : "state-message";

    element.textContent = message;

    container.replaceChildren(element);
}

function renderError(container, bookCount) {
    renderState(
        container,
        "Unable to load books. Please try again later.",
        true
    );

    bookCount.textContent = "Unavailable";
}
