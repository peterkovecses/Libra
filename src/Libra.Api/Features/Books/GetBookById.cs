namespace Libra.Api.Features.Books;

public static class GetBookByIdEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("books/{id}", HandleAsync)
            .WithName("GetBookById")
            .WithDescription("Gets a book by its ID.")
            .Produces<Book?>()
            .ProducesValidationProblem()
            .WithValidation<GetBookByIdRequest>();

    private static async Task<IResult> HandleAsync(
        GetBookByIdHandler handler,
        [AsParameters] GetBookByIdRequest request,
        CancellationToken cancellationToken)
    {
        var book = await handler.HandleAsync(request.Id, cancellationToken);
        
        return book is null 
            ? Results.NotFound() 
            : Results.Ok(book);
    }
}

public class GetBookByIdHandler(IDbConnectionFactory connectionFactory)
{
    public async Task<Book?> HandleAsync(int id, CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        var sql = CreateSql();

        return await GetBookByIdAsync(connection, sql, id, cancellationToken);
    }

    private static string CreateSql() =>
        """
        SELECT
            b.Id,
            b.Title,
            CAST(b.Version AS TEXT) AS Version,
            a.Id,
            a.Name
        FROM Books b
        LEFT JOIN BookAuthors ba ON ba.BookId = b.Id
        LEFT JOIN Authors a ON a.Id = ba.AuthorId
        WHERE b.Id = @Id
        ORDER BY a.Name, a.Id;
        """;

    private static async Task<Book?> GetBookByIdAsync(DbConnection connection, string sql, int id, CancellationToken cancellationToken)
    {
        var books = await connection.QueryWithChildrenAsync<Book, Author, int>(
            new CommandDefinition(sql, new { Id = id }, cancellationToken: cancellationToken),
            parentKeySelector: b => b.Id,
            addChildAction: (book, author) =>
            {
                if (author.Id != 0 && book.Authors.All(a => a.Id != author.Id))
                {
                    book.Authors.Add(author);
                }
            });

        return books.FirstOrDefault();
    }
}

public record GetBookByIdRequest(int Id);

public class GetBookByIdValidator : AbstractValidator<GetBookByIdRequest>
{
    public GetBookByIdValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0)
            .WithMessage("Id must be greater than 0.");
    }
}