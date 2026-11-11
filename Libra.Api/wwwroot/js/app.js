const pageContent = document.querySelector("#page-content");
const breadcrumb = document.querySelector("#breadcrumb");
const booksLink = document.querySelector("#books-link");

let currentNavigation = 0;

const routes = {
    "/books": {
        breadcrumb: "Library / Books",
        link: booksLink,
        load: () => import("../features/books/get-books/get-books.js")
    }
};

function resolveRoute(route) {
    const exactRoute = routes[route];

    if (exactRoute) {
        return {
            ...exactRoute,
            params: {}
        };
    }

    const bookDetailsMatch = route.match(/^\/books\/(\d+)$/);

    if (bookDetailsMatch) {
        return {
            breadcrumb: "Library / Books / Book Details",
            link: booksLink,
            load: () => import("../features/books/get-book-details/get-book-details.js"),
            params: {
                bookId: Number(bookDetailsMatch[1])
            }
        };
    }

    return null;
}

async function navigate() {
    const navigationId = ++currentNavigation;
    const route = location.hash.slice(1);

    // Reset navigation state.
    document.querySelectorAll(".nav-link").forEach(link => {
        link.classList.remove("active");
        link.removeAttribute("aria-current");
    });

    // Empty route: show only the application shell.
    if (!route) {
        breadcrumb.textContent = "Library";
        pageContent.replaceChildren();
        removeFeatureStylesheet();

        return;
    }

    const page = resolveRoute(route);

    // Unknown route.
    if (!page) {
        breadcrumb.textContent = "Not found";
        pageContent.textContent = "Page not found.";

        removeFeatureStylesheet();

        return;
    }

    // Update navigation.
    page.link.classList.add("active");
    page.link.setAttribute("aria-current", "page");

    breadcrumb.textContent = page.breadcrumb;

    try {
        pageContent.replaceChildren();

        const feature = await page.load();

        if (navigationId !== currentNavigation) {
            return;
        }

        await feature.load(
            pageContent,
            () => navigationId === currentNavigation,
            page.params
        );
    } catch (error) {
        console.error("Navigation error:", error);

        if (navigationId === currentNavigation) {
            pageContent.textContent =
                "Unable to load this page. Please try again later.";
        }
    }
}

function removeFeatureStylesheet() {
    document
        .querySelector("[data-feature-stylesheet]")
        ?.remove();
}

window.addEventListener("hashchange", navigate);

// Handle direct navigation to a route.
navigate();