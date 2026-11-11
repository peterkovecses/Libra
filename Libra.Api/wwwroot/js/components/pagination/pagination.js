
const cssUrl = new URL("./pagination.css", import.meta.url);

const DEFAULT_PAGE_SIZE_OPTIONS = [10, 25, 50];

let stylesheetPromise;

export async function createPagination(container, options) {
    await loadStylesheet();

    const state = {
        pageNumber: 1,
        pageSize: 10,
        totalCount: 0,
        totalPages: 0,
        hasPreviousPage: false,
        hasNextPage: false,
        ...options
    };

    const root = document.createElement("div");
    root.className = "pagination";

    const summary = document.createElement("div");
    summary.className = "pagination-summary";

    const controls = document.createElement("div");
    controls.className = "pagination-controls";

    const pageSizeContainer = document.createElement("div");
    pageSizeContainer.className = "pagination-page-size";

    const pageSizeLabel = document.createElement("label");
    pageSizeLabel.textContent = "Rows per page";
    pageSizeLabel.htmlFor = "pagination-page-size";

    const pageSizeSelect = document.createElement("select");
    pageSizeSelect.id = "pagination-page-size";
    pageSizeSelect.className = "pagination-select";

    const pageSizeOptions = [
        ...new Set([
            ...DEFAULT_PAGE_SIZE_OPTIONS,
            state.pageSize
        ])
    ].sort((a, b) => a - b);

    for (const size of pageSizeOptions) {
        const option = document.createElement("option");

        option.value = size;
        option.textContent = size;

        pageSizeSelect.appendChild(option);
    }

    pageSizeContainer.append(
        pageSizeLabel,
        pageSizeSelect
    );

    const previousButton = document.createElement("button");
    previousButton.type = "button";
    previousButton.className = "pagination-button";
    previousButton.textContent = "Previous";

    const pageIndicator = document.createElement("span");
    pageIndicator.className = "pagination-page-indicator";

    const nextButton = document.createElement("button");
    nextButton.type = "button";
    nextButton.className = "pagination-button";
    nextButton.textContent = "Next";

    controls.append(
        previousButton,
        pageIndicator,
        nextButton
    );

    root.append(
        summary,
        pageSizeContainer,
        controls
    );

    container.replaceChildren(root);

    function render() {
        const start = state.totalCount === 0
            ? 0
            : (state.pageNumber - 1) * state.pageSize + 1;

        const end = Math.min(
            state.pageNumber * state.pageSize,
            state.totalCount
        );

        summary.textContent =
            `Showing ${start}–${end} of ${state.totalCount}`;

        pageIndicator.textContent =
            `Page ${state.pageNumber} of ${state.totalPages}`;

        pageSizeSelect.value = state.pageSize;

        previousButton.disabled = !state.hasPreviousPage;
        nextButton.disabled = !state.hasNextPage;

        container.hidden = state.totalCount === 0;
    }

    function update(data) {
        Object.assign(state, data);
        render();
    }

    function goToPage(pageNumber, pageSize = state.pageSize) {
        if (pageNumber < 1 || pageNumber > state.totalPages) {
            return;
        }

        if (
            pageNumber === state.pageNumber &&
            pageSize === state.pageSize
        ) {
            return;
        }

        options.onPageChange({
            pageNumber,
            pageSize
        });
    }

    previousButton.addEventListener("click", () => {
        goToPage(state.pageNumber - 1);
    });

    nextButton.addEventListener("click", () => {
        goToPage(state.pageNumber + 1);
    });

    pageSizeSelect.addEventListener("change", event => {
        const pageSize = Number(event.target.value);

        goToPage(1, pageSize);
    });

    render();

    return {
        update,

        destroy() {
            root.remove();
        }
    };
}

async function loadStylesheet() {
    if (stylesheetPromise) {
        return stylesheetPromise;
    }

    const existingStylesheet = document.querySelector(
        '[data-component-stylesheet="pagination"]'
    );

    if (existingStylesheet) {
        return;
    }

    const stylesheet = document.createElement("link");

    stylesheet.rel = "stylesheet";
    stylesheet.href = cssUrl.href;
    stylesheet.dataset.componentStylesheet = "pagination";

    stylesheetPromise = new Promise((resolve, reject) => {
        stylesheet.onload = resolve;

        stylesheet.onerror = () => {
            stylesheet.remove();

            reject(
                new Error("Failed to load Pagination stylesheet.")
            );
        };
    });

    document.head.appendChild(stylesheet);

    try {
        await stylesheetPromise;
    } catch (error) {
        stylesheetPromise = undefined;
        throw error;
    }
}
