function isBook(book) {
    return book !== null &&
        typeof book === "object" &&
        Number.isSafeInteger(book.id) && book.id > 0 &&
        typeof book.title === "string" && book.title.trim().length > 0 &&
        Array.isArray(book.authors) &&
        book.authors.every(author =>
            author !== null &&
            typeof author === "object" &&
            Number.isSafeInteger(author.id) && author.id > 0 &&
            typeof author.name === "string" && author.name.trim().length > 0
        );
}

export function validateBook(book, expectedId) {
    if (!isBook(book) || book.id !== expectedId) {
        throw new Error("Invalid book response.");
    }
    return book;
}

export function validateBooksPage(data, pageNumber, pageSize) {
    if (
        !data || typeof data !== "object" ||
        !Array.isArray(data.items) || !data.items.every(isBook) ||
        !Number.isSafeInteger(data.totalCount) || data.totalCount < 0 ||
        data.pageNumber !== pageNumber ||
        data.pageSize !== pageSize ||
        !Number.isSafeInteger(data.totalPages) ||
        data.totalPages !== Math.ceil(data.totalCount / data.pageSize) ||
        data.hasPreviousPage !== (data.pageNumber > 1) ||
        data.hasNextPage !== (data.pageNumber < data.totalPages) ||
        data.items.length > data.pageSize ||
        data.items.length > data.totalCount
    ) {
        throw new Error("Invalid paginated books response.");
    }
    return data;
}
