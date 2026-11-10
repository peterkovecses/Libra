namespace Libra.Api.Features.Books;

public static class GetBooksEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("books", HandleAsync)
            .WithName("GetBooks")
            .WithDescription("Gets all books.")
            .Produces<List<Book>>()
            .ProducesValidationProblem();

    private static async Task<IResult> HandleAsync(
        GetBooksHandler handler,
        CancellationToken cancellationToken)
    {
        var books = await handler.HandleAsync(cancellationToken);
        
        return Results.Ok(books);
    }
}

public sealed class GetBooksHandler(IDbConnectionFactory connectionFactory)
{
    public async Task<List<Book>> HandleAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var sql = CreateSql();

        return await GetBooksAsync(connection, sql, cancellationToken);
    }

    private static string CreateSql() =>
        """
        SELECT
            b.Id,
            b.Title,
            a.Id,
            a.Name
        FROM Books b
        LEFT JOIN BookAuthors ba ON ba.BookId = b.Id
        LEFT JOIN Authors a ON a.Id = ba.AuthorId
        ORDER BY b.Title, a.Name;
        """;

    private static async Task<List<Book>> GetBooksAsync(DbConnection connection, string sql, CancellationToken cancellationToken) =>
        await connection.QueryWithChildrenAsync<Book, Author, int>(
            new CommandDefinition(sql, cancellationToken: cancellationToken),
            parentKeySelector: b => b.Id,
            addChildAction: (book, author) =>
            {
                if (author.Id != 0 && book.Authors.All(a => a.Id != author.Id))
                {
                    book.Authors.Add(author);
                }
            });
}