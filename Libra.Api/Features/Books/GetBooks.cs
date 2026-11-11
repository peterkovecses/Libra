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

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(
                "SELECT COUNT(*) FROM Books;",
                cancellationToken: cancellationToken));

        var sql = CreateSql();

        var books = await GetBooksAsync(
            connection,
            sql,
            parameters,
            cancellationToken);

        return new PaginatedList<Book>(
            books,
            totalCount,
            pagination.PageNumber,
            pagination.PageSize);
    }

    private static string CreateSql() =>
        """
        WITH PagedBooks AS
        (
            SELECT Id, Title
            FROM Books
            ORDER BY Title, Id
            LIMIT @PageSize OFFSET @Offset
        )
        SELECT
            b.Id,
            b.Title,
            a.Id,
            a.Name
        FROM PagedBooks b
        LEFT JOIN BookAuthors ba ON ba.BookId = b.Id
        LEFT JOIN Authors a ON a.Id = ba.AuthorId
        ORDER BY b.Title, b.Id, a.Name;
        """;

    private static async Task<List<Book>> GetBooksAsync(
        DbConnection connection,
        string sql,
        object parameters,
        CancellationToken cancellationToken) =>
        await connection.QueryWithChildrenAsync<Book, Author, int>(
            new CommandDefinition(
                sql,
                parameters,
                cancellationToken: cancellationToken),
            parentKeySelector: b => b.Id,
            addChildAction: (book, author) =>
            {
                if (author.Id != 0 && book.Authors.All(a => a.Id != author.Id))
                {
                    book.Authors.Add(author);
                }
            });
}