namespace Libra.Api.Features.Books;

public static class GetBooksEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("books", HandleAsync)
            .WithName("GetBooks")
            .WithDescription("Gets books with pagination.")
            .Produces<PaginatedList<Book>>()
            .ProducesValidationProblem()
            .WithValidation<Pagination>();

    private static async Task<IResult> HandleAsync(
        [AsParameters] Pagination pagination,
        GetBooksHandler handler,
        CancellationToken cancellationToken)
    {
        var books = await handler.HandleAsync(
            pagination,
            cancellationToken);

        return Results.Ok(books);
    }
}

public sealed class GetBooksHandler(IDbConnectionFactory connectionFactory)
{
    public async Task<PaginatedList<Book>> HandleAsync(
        Pagination pagination,
        CancellationToken cancellationToken)
    {
        var parameters = PaginationSqlParameters.Create(pagination);

        await using var connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = CreateSql();

        return await GetBooksAsync(
            connection,
            sql,
            parameters,
            pagination,
            cancellationToken);
    }

    private static string CreateSql() =>
        """
        WITH BookCount AS
        (
            SELECT COUNT(*) AS TotalCount
            FROM Books
        ),
        PagedBooks AS
        (
            SELECT Id, Title, Version
            FROM Books
            ORDER BY Title, Id
            LIMIT @PageSize OFFSET @Offset
        )
        SELECT
            c.TotalCount,
            b.Id,
            b.Title,
            CAST(b.Version AS TEXT) AS Version,
            a.Id,
            a.Name
        FROM BookCount c
        LEFT JOIN PagedBooks b ON 1 = 1
        LEFT JOIN BookAuthors ba ON ba.BookId = b.Id
        LEFT JOIN Authors a ON a.Id = ba.AuthorId
        ORDER BY b.Title, b.Id, a.Name, a.Id;
        """;

    private static async Task<PaginatedList<Book>> GetBooksAsync(
        DbConnection connection,
        string sql,
        object parameters,
        Pagination pagination,
        CancellationToken cancellationToken) =>
        await connection.QueryPaginatedWithChildrenAsync<Book, Author, int>(
            new CommandDefinition(
                sql,
                parameters,
                cancellationToken: cancellationToken),
            pagination,
            parentKeySelector: b => b.Id,
            addChildAction: (book, author) =>
            {
                if (author.Id != 0 && book.Authors.All(a => a.Id != author.Id))
                {
                    book.Authors.Add(author);
                }
            });
}