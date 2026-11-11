const stylesheets = new Map();
let activeFeatureStylesheet;

export async function loadStylesheet(url, { feature = false, isCurrent = () => true } = {}) {
    const href = url.href;
    let entry = stylesheets.get(href);

    if (!entry) {
        const link = document.createElement("link");
        link.rel = "stylesheet";
        link.href = href;

        if (feature) {
            link.media = "not all";
        }

        const loaded = new Promise((resolve, reject) => {
            link.onload = resolve;
            link.onerror = () => reject(new Error(`Failed to load stylesheet: ${href}`));
        });

        entry = { link, loaded };
        stylesheets.set(href, entry);
        document.head.appendChild(link);
    }

    try {
        await entry.loaded;
    } catch (error) {
        if (stylesheets.get(href) === entry) {
            stylesheets.delete(href);
            entry.link.remove();
        }
        throw error;
    }

    if (feature && isCurrent()) {
        removeFeatureStylesheet();
        entry.link.media = "all";
        activeFeatureStylesheet = entry.link;
    }
}

export function removeFeatureStylesheet() {
    if (activeFeatureStylesheet) {
        activeFeatureStylesheet.media = "not all";
        activeFeatureStylesheet = undefined;
    }
}
