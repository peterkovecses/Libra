export class HttpError extends Error {
    constructor(response) {
        super(`Request failed with status ${response.status}.`);
        this.name = "HttpError";
        this.status = response.status;
    }
}

export async function fetchResponse(url) {
    const response = await fetch(url);

    if (!response.ok) {
        throw new HttpError(response);
    }

    return response;
}

export function renderState(container, message, { error = false, retry } = {}) {
    const state = document.createElement("div");
    state.className = error ? "state-message error" : "state-message";
    state.setAttribute("role", error ? "alert" : "status");

    const text = document.createElement("div");
    text.textContent = message;
    state.appendChild(text);

    if (retry) {
        const button = document.createElement("button");
        button.type = "button";
        button.className = "state-retry";
        button.textContent = "Try again";
        button.addEventListener("click", retry);
        state.appendChild(button);
    }

    container.replaceChildren(state);
}

export function renderError(container, error, message, retry) {
    console.error(message, error);
    const status = error instanceof HttpError ? ` (HTTP ${error.status})` : "";
    renderState(container, `${message}${status}`, { error: true, retry });
}
