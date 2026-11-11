import { loadStylesheet } from "../../stylesheet.js";

const cssUrl = new URL("./search-sort.css", import.meta.url);

export async function createSearchSort(container, {
    searchLabel, searchPlaceholder, maxLength, sortOptions,
    onPending = () => {}, onChange, isCurrent = () => true
}) {
    await loadStylesheet(cssUrl);

    if (!isCurrent()) {
        return;
    }

    const root = document.createElement("div");
    root.className = "search-sort";

    const searchLabelElement = document.createElement("label");
    searchLabelElement.textContent = searchLabel;
    const search = document.createElement("input");
    search.type = "search";
    search.placeholder = searchPlaceholder;
    if (maxLength !== undefined) {
        search.maxLength = maxLength;
    }
    searchLabelElement.appendChild(search);

    const sortLabel = document.createElement("label");
    sortLabel.className = "search-sort-order";
    sortLabel.textContent = "Sort by";
    const sort = document.createElement("select");
    for (const { value, label } of sortOptions) {
        const option = document.createElement("option");
        option.value = value;
        option.textContent = label;
        sort.appendChild(option);
    }
    sortLabel.appendChild(sort);
    root.append(searchLabelElement, sortLabel);
    container.replaceChildren(root);

    let timer;
    const notify = () => {
        if (isCurrent()) {
            onChange({ search: search.value.trim(), sort: sort.value });
        }
    };

    search.addEventListener("input", () => {
        clearTimeout(timer);
        onPending();
        timer = setTimeout(notify, 300);
    });
    sort.addEventListener("change", () => {
        clearTimeout(timer);
        notify();
    });
}
