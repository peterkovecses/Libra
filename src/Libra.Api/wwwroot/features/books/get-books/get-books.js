
import { createPagination } from "../../../js/components/pagination/pagination.js";
import { createSearchSort } from "../../../js/components/search-sort/search-sort.js";
import { fetchResponse, renderError, renderState } from "../../../js/http.js";
import { loadStylesheet } from "../../../js/stylesheet.js";
import { validateBooksPage } from "../common/validation/book-validation.js";

const htmlUrl = new URL("./get-books.html", import.meta.url);
const cssUrl = new URL("./get-books.css", import.meta.url);

const DEFAULT_PAGE_NUMBER = 1;
const DEFAULT_PAGE_SIZE = 10;

export async function load(container, isCurrent = () => true) {
    const response = await fetchResponse(htmlUrl);

    const html = await response.text();

    if (!isCurrent()) {
        return;
    }

    await loadStylesheet(cssUrl, { feature: true, isCurrent });

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
        currentRequestId: 0,
        pageSize: DEFAULT_PAGE_SIZE,
        search: "",
        sortBy: "title",
        sortDirection: "asc"
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
                requestState.pageSize = pageSize;
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

    await createSearchSort(container.querySelector("#book-filters"), {
        searchLabel: "Search books",
        searchPlaceholder: "Title or author",
        maxLength: 200,
        sortOptions: [
            { value: "title:asc", label: "Title (ascending)" },
            { value: "title:desc", label: "Title (descending)" },
            { value: "id:asc", label: "ID (ascending)" },
            { value: "id:desc", label: "ID (descending)" }
        ],
        isCurrent,
        onPending: () => {
            requestState.currentRequestId++;
            pagination.update({ loading: true });
            bookCount.textContent = "Loading...";
            renderState(booksContainer, "Loading books...");
        },
        onChange: ({ search, sort }) => {
            requestState.search = search;
            [requestState.sortBy, requestState.sortDirection] = sort.split(":");
            loadBooks(
                booksContainer, bookCount, pagination, requestState,
                isCurrent, DEFAULT_PAGE_NUMBER, requestState.pageSize
            );
        }
    });

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
    pagination.update({ loading: true });
    bookCount.textContent = "Loading...";
    renderState(booksContainer, "Loading books...");

    try {
        const params = new URLSearchParams({
            pageNumber: pageNumber.toString(),
            pageSize: pageSize.toString(),
            search: requestState.search,
            sortBy: requestState.sortBy,
            sortDirection: requestState.sortDirection
        });

        const response = await fetchResponse(`/api/books?${params}`);
        const data = validateBooksPage(await response.json(), pageNumber, pageSize);

        if (!isCurrent() || requestId !== requestState.currentRequestId) {
            return;
        }

        if (data.pageNumber > Math.max(1, data.totalPages)) {
            return await loadBooks(
                booksContainer, bookCount, pagination, requestState,
                isCurrent, Math.max(1, data.totalPages), pageSize
            );
        }

        renderBooks(data, booksContainer, bookCount, requestState.search);

        pagination.update({ ...data, loading: false });
    } catch (error) {
        if (!isCurrent() || requestId !== requestState.currentRequestId) {
            return;
        }

        bookCount.textContent = "Unavailable";
        renderError(
            booksContainer,
            error,
            "Unable to load books. Please try again.",
            () => loadBooks(
                booksContainer, bookCount, pagination, requestState,
                isCurrent, pageNumber, pageSize
            )
        );
    }
}

function renderBooks(data, container, bookCount, search) {
    const { items, totalCount } = data;

    bookCount.textContent = `${totalCount} books`;

    if (items.length === 0) {
        renderState(
            container,
            search ? "No books match your search." : "No books found in the library."
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