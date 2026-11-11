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
        const book = validateBookDetails(await response.json(), bookId);

        if (!isCurrent()) {
            return;
        }

        setupEditor(container, detailsContainer, book, isCurrent);
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

function validateBookDetails(book, id) {
    validateBook(book, id);
    if (typeof book.version !== "string" || !/^[1-9]\d*$/.test(book.version)) {
        throw new Error("Invalid book version.");
    }
    return book;
}

async function loadAuthors() {
    const authors = [];
    let pageNumber = 1;
    let hasNextPage;
    do {
        const response = await fetchResponse(`/api/authors?pageNumber=${pageNumber}&pageSize=100`);
        const page = await response.json();
        if (!page || !Array.isArray(page.items) ||
            !page.items.every(a => Number.isSafeInteger(a.id) && a.id > 0 &&
                typeof a.name === "string" && a.name.trim()) ||
            page.pageNumber !== pageNumber || page.pageSize !== 100 ||
            !Number.isSafeInteger(page.totalCount) || page.totalCount < 0 ||
            page.totalPages !== Math.ceil(page.totalCount / 100) ||
            page.hasNextPage !== (pageNumber < page.totalPages) ||
            page.items.length > 100 || (page.hasNextPage && page.items.length === 0)) {
            throw new Error("Invalid authors response.");
        }
        authors.push(...page.items);
        hasNextPage = page.hasNextPage;
        pageNumber++;
    } while (hasNextPage);
    return authors;
}

function setupEditor(container, detailsContainer, initialBook, isCurrent) {
    const actions = container.querySelector(".panel-actions");
    const editButton = container.querySelector("#edit-book-button");
    const cancelButton = container.querySelector("#cancel-book-button");
    const errorBox = container.querySelector("#book-edit-error");
    let book = initialBook;
    let draft = null;
    let allAuthors = [];
    let busy = false;

    function showError(message, reload = false) {
        errorBox.hidden = false;
        errorBox.replaceChildren();
        const text = document.createElement("span");
        text.textContent = message;
        errorBox.appendChild(text);
        if (reload) {
            const button = document.createElement("button");
            button.type = "button";
            button.textContent = "Reload book (discard draft)";
            button.addEventListener("click", async () => {
                if (busy) return;
                busy = true;
                updateControls();
                try {
                    const response = await fetchResponse(`/api/books/${book.id}`);
                    const latest = validateBookDetails(await response.json(), book.id);
                    if (!isCurrent()) return;
                    book = latest;
                    draft = null;
                    errorBox.hidden = true;
                    render();
                } catch (error) {
                    if (isCurrent()) {
                        console.error("Unable to reload book.", error);
                        showError("Unable to reload the book. Please try again.", true);
                    }
                } finally {
                    busy = false;
                    if (isCurrent()) updateControls();
                }
            });
            errorBox.appendChild(button);
        }
    }

    function updateControls() {
        editButton.disabled = busy;
        editButton.textContent = busy ? (draft ? "Saving..." : "Loading...") : (draft ? "Save" : "Edit");
        cancelButton.hidden = !draft;
        cancelButton.disabled = busy;
        detailsContainer.querySelectorAll("input, select, button").forEach(element => { element.disabled = busy; });
        errorBox.querySelectorAll("button").forEach(element => { element.disabled = busy; });
    }

    function render() {
        if (draft) renderDraft(detailsContainer, draft, allAuthors, render);
        else renderBook(detailsContainer, book);
        updateControls();
    }

    cancelButton.addEventListener("click", () => {
        draft = null;
        errorBox.hidden = true;
        render();
    });

    editButton.addEventListener("click", async () => {
        if (busy) return;
        errorBox.hidden = true;
        if (draft && !draft.title.trim()) {
            showError("Title must not be blank.");
            return;
        }
        busy = true;
        updateControls();
        try {
            if (!draft) {
                const authors = await loadAuthors();
                if (!isCurrent()) return;
                allAuthors = authors;
                draft = { title: book.title, authors: [...book.authors], version: book.version };
            } else {
                const response = await fetchResponse(`/api/books/${book.id}`, {
                    method: "PUT",
                    headers: { "Content-Type": "application/json" },
                    body: JSON.stringify({
                        title: draft.title,
                        authorIds: draft.authors.map(author => author.id),
                        version: draft.version
                    })
                });
                const updated = validateBookDetails(await response.json(), book.id);
                if (!isCurrent()) return;
                book = updated;
                draft = null;
            }
            render();
        } catch (error) {
            if (!isCurrent()) return;
            console.error("Unable to edit book.", error);
            if (error instanceof HttpError && error.status === 409) {
                showError("This book changed elsewhere. Reload it before saving; your draft is still here.", true);
            } else if (error instanceof HttpError && error.status === 400) {
                showError("The title or author selection is invalid. Check your changes and try again.");
            } else if (error instanceof HttpError && error.status === 404) {
                showError("This book no longer exists.");
            } else {
                showError(draft ? "Unable to save the book. Your draft is still here." : "Unable to load authors. Please try again.");
            }
        } finally {
            busy = false;
            if (isCurrent()) updateControls();
        }
    });

    actions.hidden = false;
    render();
}

function renderDraft(container, draft, allAuthors, render) {
    const details = document.createElement("div");
    details.className = "book-details";
    const titleLabel = document.createElement("label");
    titleLabel.className = "book-details-label";
    titleLabel.textContent = "Title";
    titleLabel.htmlFor = "book-title-input";
    const title = document.createElement("input");
    title.id = "book-title-input";
    title.value = draft.title;
    title.addEventListener("input", () => { draft.title = title.value; });
    details.append(titleLabel, title);

    const label = document.createElement("div");
    label.className = "book-details-label";
    label.textContent = "Authors";
    const authorList = document.createElement("div");
    authorList.className = "book-details-authors";
    draft.authors.forEach(author => {
        const row = document.createElement("div");
        row.className = "book-edit-author";
        const name = document.createElement("span");
        name.textContent = author.name;
        const remove = document.createElement("button");
        remove.type = "button";
        remove.textContent = "Remove";
        remove.setAttribute("aria-label", `Remove ${author.name}`);
        remove.addEventListener("click", () => {
            draft.authors = draft.authors.filter(item => item.id !== author.id);
            render();
        });
        row.append(name, remove);
        authorList.appendChild(row);
    });
    const select = document.createElement("select");
    select.setAttribute("aria-label", "Add author");
    const placeholder = document.createElement("option");
    placeholder.value = "";
    placeholder.textContent = "Add an author...";
    select.appendChild(placeholder);
    allAuthors.filter(author => !draft.authors.some(item => item.id === author.id)).forEach(author => {
        const option = document.createElement("option");
        option.value = String(author.id);
        option.textContent = author.name;
        select.appendChild(option);
    });
    select.addEventListener("change", () => {
        const author = allAuthors.find(item => String(item.id) === select.value);
        if (author) {
            draft.authors.push(author);
            render();
        }
    });
    authorList.appendChild(select);
    details.append(label, authorList);
    container.replaceChildren(details);
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
