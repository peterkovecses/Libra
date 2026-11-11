
import { createPagination } from "../../../js/components/pagination/pagination.js";

const htmlUrl = new URL("./get-books.html", import.meta.url);
const cssUrl = new URL("./get-books.css", import.meta.url);

const DEFAULT_PAGE_NUMBER = 1;
const DEFAULT_PAGE_SIZE = 10;

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

    const booksContainer = container.querySelector(
        "#books-container"
    );

    const paginationContainer = container.querySelector(
        "#pagination-container"
    );

    const bookCount = container.querySelector("#book-count");

    const requestState = {
        currentRequestId: 0
    };

    const pagination = await createPagination(
        paginationContainer,
        {
            pageNumber: DEFAULT_PAGE_NUMBER,
            pageSize: DEFAULT_PAGE_SIZE,
            totalCount: 0,
            totalPages: 0,
            hasPreviousPage: false,
            hasNextPage: false,

            onPageChange: ({ pageNumber, pageSize }) => {
                loadBooks(
                    booksContainer,
                    bookCount,
                    pagination,
                    requestState,
                    isCurrent,
                    pageNumber,
                    pageSize
                );
            }
        }
    );

    if (!isCurrent()) {
        pagination.destroy();
        return;
    }

    await loadBooks(
        booksContainer,
        bookCount,
        pagination,
        requestState,
        isCurrent,
        DEFAULT_PAGE_NUMBER,
        DEFAULT_PAGE_SIZE
    );
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

async function loadBooks(
    booksContainer,
    bookCount,
    pagination,
    requestState,
    isCurrent,
    pageNumber,
    pageSize
) {
    const requestId = ++requestState.currentRequestId;

    try {
        const params = new URLSearchParams({
            pageNumber: pageNumber.toString(),
            pageSize: pageSize.toString()
        });

        const response = await fetch(`/api/books?${params}`);

        if (!response.ok) {
            throw new Error(
                `Failed to load books (${response.status})`
            );
        }

        const data = await response.json();

        if (!isCurrent() || requestId !== requestState.currentRequestId) {
            return;
        }

        if (
            !data ||
            !Array.isArray(data.items) ||
            !Number.isInteger(data.totalCount) ||
            !Number.isInteger(data.pageNumber) ||
            !Number.isInteger(data.pageSize) ||
            !Number.isInteger(data.totalPages) ||
            typeof data.hasPreviousPage !== "boolean" ||
            typeof data.hasNextPage !== "boolean"
        ) {
            throw new Error("Invalid paginated books response.");
        }

        renderBooks(data, booksContainer, bookCount);

        pagination.update(data);
    } catch (error) {
        if (!isCurrent() || requestId !== requestState.currentRequestId) {
            return;
        }

        console.error("Error loading books:", error);

        renderError(booksContainer, bookCount);
    }
}

function renderBooks(data, container, bookCount) {
    const { items, totalCount } = data;

    bookCount.textContent = `${totalCount} books`;

    if (items.length === 0) {
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

    for (const book of items) {
        const row = document.createElement("tr");

        row.classList.add("book-row");
        row.tabIndex = 0;
        row.setAttribute("role", "link");
        row.setAttribute("aria-label", `View details for ${book.title}`);

        const navigateToBook = () => {
            location.hash = `/books/${book.id}`;
        };

        row.addEventListener("click", navigateToBook);

        row.addEventListener("keydown", event => {
            if (event.key === "Enter" || event.key === " ") {
                event.preventDefault();
                navigateToBook();
            }
        });

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