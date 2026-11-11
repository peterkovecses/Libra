import { fetchResponse, HttpError, renderError, renderState } from "../../../js/http.js";
import { loadStylesheet } from "../../../js/stylesheet.js";
import { validateBook } from "../common/validation/book-validation.js";

const htmlUrl = new URL("./get-book-details.html", import.meta.url);
const cssUrl = new URL("./get-book-details.css", import.meta.url);

export async function load(container, isCurrent = () => true, params = {}) {
    const { bookId } = params;

    if (!Number.isInteger(bookId) || bookId <= 0) {
        renderError(container, new Error("Invalid book ID."), "Invalid book ID.");
        return;
    }

    const htmlResponse = await fetchResponse(htmlUrl);
    const html = await htmlResponse.text();

    if (!isCurrent()) {
        return;
    }

    await loadStylesheet(cssUrl, { feature: true, isCurrent });

    if (!isCurrent()) {
        return;
    }

    container.innerHTML = html;
    await loadBook(bookId, container, isCurrent);
}

async function loadBook(bookId, container, isCurrent) {
    const detailsContainer = container.querySelector(
        "#book-details-container"
    );

    try {
        const response = await fetchResponse(`/api/books/${bookId}`);
        const book = validateBook(await response.json(), bookId);

        if (!isCurrent()) {
            return;
        }

        renderBook(detailsContainer, book);
        container.querySelector(".panel-actions").hidden = false;
    } catch (error) {
        if (isCurrent()) {
            if (error instanceof HttpError && error.status === 404) {
                renderState(detailsContainer, "Book not found. (HTTP 404)");
            } else {
                renderError(
                    detailsContainer,
                    error,
                    "Unable to load book details. Please try again later."
                );
            }
        }
    }
}

function renderBook(container, book) {
    const details = document.createElement("div");
    details.className = "book-details";

    appendDetail(details, "Book ID", book.id, "book-details-id");
    appendDetail(details, "Title", book.title);

    const authorsLabel = document.createElement("div");
    authorsLabel.className = "book-details-label";
    authorsLabel.textContent = "Authors";

    const authorsValue = document.createElement("div");
    authorsValue.className = "book-details-value";

    if (book.authors?.length) {
        const authors = document.createElement("div");
        authors.className = "book-details-authors";

        book.authors.forEach(author => {
            const authorElement = document.createElement("div");
            authorElement.className = "book-details-author";
            authorElement.textContent = author.name;

            authors.appendChild(authorElement);
        });

        authorsValue.appendChild(authors);
    } else {
        const emptyMessage = document.createElement("span");
        emptyMessage.className = "book-details-empty";
        emptyMessage.textContent = "No authors";

        authorsValue.appendChild(emptyMessage);
    }

    details.append(authorsLabel, authorsValue);

    container.replaceChildren(details);
}

function appendDetail(container, label, value, valueClass = "") {
    const labelElement = document.createElement("div");
    labelElement.className = "book-details-label";
    labelElement.textContent = label;

    const valueElement = document.createElement("div");
    valueElement.className = "book-details-value";

    if (valueClass) {
        valueElement.classList.add(valueClass);
    }

    valueElement.textContent = value ?? "";

    container.append(labelElement, valueElement);
}
