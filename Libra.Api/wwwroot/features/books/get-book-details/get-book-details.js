const htmlUrl = new URL("./get-book-details.html", import.meta.url);
const cssUrl = new URL("./get-book-details.css", import.meta.url);

export async function load(container, isCurrent = () => true, params = {}) {
    const { bookId } = params;

    if (!Number.isInteger(bookId) || bookId <= 0) {
        renderError(container, "Invalid book ID.");
        return;
    }

    try {
        const [htmlResponse] = await Promise.all([
            fetch(htmlUrl)
        ]);

        if (!htmlResponse.ok) {
            throw new Error("Unable to load book details page.");
        }

        const html = await htmlResponse.text();

        if (!isCurrent()) {
            return;
        }

        await loadStylesheet();

        if (!isCurrent()) {
            return;
        }

        container.innerHTML = html;

        await loadBook(bookId, container, isCurrent);
    } catch (error) {
        console.error("Book details page error:", error);

        if (isCurrent()) {
            renderError(
                container,
                "Unable to load this page. Please try again later."
            );
        }
    }
}

async function loadStylesheet() {
    const existingStylesheet = document.querySelector(
        "[data-feature-stylesheet]"
    );

    if (existingStylesheet) {
        existingStylesheet.remove();
    }

    const stylesheet = document.createElement("link");

    stylesheet.rel = "stylesheet";
    stylesheet.href = cssUrl;
    stylesheet.dataset.featureStylesheet = "";

    document.head.appendChild(stylesheet);

    await new Promise((resolve, reject) => {
        stylesheet.addEventListener("load", resolve, { once: true });
        stylesheet.addEventListener(
            "error",
            () => reject(new Error("Unable to load feature stylesheet.")),
            { once: true }
        );
    });
}

async function loadBook(bookId, container, isCurrent) {
    const detailsContainer = container.querySelector(
        "#book-details-container"
    );

    try {
        const response = await fetch(`/api/books/${bookId}`);

        if (!isCurrent()) {
            return;
        }

        if (response.status === 404) {
            renderState(detailsContainer, "Book not found.");
            return;
        }

        if (!response.ok) {
            throw new Error(`Request failed with status ${response.status}.`);
        }

        const book = await response.json();

        if (!isCurrent()) {
            return;
        }

        renderBook(detailsContainer, book);
    } catch (error) {
        console.error("Unable to load book:", error);

        if (isCurrent()) {
            renderError(
                detailsContainer,
                "Unable to load book details. Please try again later."
            );
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

function renderState(container, message) {
    const element = document.createElement("p");
    element.className = "state-message";
    element.textContent = message;

    container.replaceChildren(element);
}

function renderError(container, message) {
    renderState(container, message);
}