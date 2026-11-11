namespace Libra.Api.Features.Books;

public static class GetBooksEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("books", HandleAsync)
            .WithName("GetBooks")
            .WithDescription("Gets books with search, sorting and pagination.")
            .Produces<PaginatedList<Book>>()
            .ProducesValidationProblem()
            .WithValidation<Pagination>()
            .WithValidation<GetBooksOptions>();

    private static async Task<IResult> HandleAsync(
        [AsParameters] Pagination pagination,
        [AsParameters] GetBooksOptions options,
        GetBooksHandler handler,
        CancellationToken cancellationToken)
    {
        var books = await handler.HandleAsync(
            pagination,
            options,
            cancellationToken);

        return Results.Ok(books);
    }
}

public sealed class GetBooksOptions
{
    public string? Search { get; init; }
    public string? SortBy { get; init; }
    public string? SortDirection { get; init; }
}

public sealed class GetBooksOptionsValidator : AbstractValidator<GetBooksOptions>
{
    public GetBooksOptionsValidator()
    {
        RuleFor(x => x.Search).MaximumLength(200);
        RuleFor(x => x.SortBy).Must(value => value is null or "title" or "id");
        RuleFor(x => x.SortDirection).Must(value => value is null or "asc" or "desc");
    }
}

public sealed class GetBooksHandler(IDbConnectionFactory connectionFactory)
{
    public async Task<PaginatedList<Book>> HandleAsync(
        Pagination pagination,
        GetBooksOptions options,
        CancellationToken cancellationToken)
    {
        var paginationParameters = PaginationSqlParameters.Create(pagination);
        var parameters = new DynamicParameters(paginationParameters);
        parameters.Add("Search", options.Search?.Trim() ?? "");

        await using var connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        var sql = CreateSql(options);

        return await GetBooksAsync(
            connection,
            sql,
            parameters,
            pagination,
            cancellationToken);
    }

    private static string CreateSql(GetBooksOptions options)
    {
        var (orderBy, resultOrderBy) = (options.SortBy ?? "title", options.SortDirection ?? "asc") switch
        {
            ("title", "asc") => ("Title COLLATE NOCASE ASC, Id ASC", "b.Title COLLATE NOCASE ASC, b.Id ASC"),
            ("title", "desc") => ("Title COLLATE NOCASE DESC, Id ASC", "b.Title COLLATE NOCASE DESC, b.Id ASC"),
            ("id", "asc") => ("Id ASC", "b.Id ASC"),
            ("id", "desc") => ("Id DESC", "b.Id DESC"),
            _ => throw new ArgumentException("Invalid book sort options.", nameof(options))
        };

        return $"""
        WITH FilteredBooks AS
        (
            SELECT b.Id, b.Title, b.Version
            FROM Books b
            WHERE @Search = ''
                OR instr(lower(b.Title), lower(@Search)) > 0
                OR EXISTS
                (
                    SELECT 1
                    FROM BookAuthors ba
                    JOIN Authors a ON a.Id = ba.AuthorId
                    WHERE ba.BookId = b.Id
                        AND instr(lower(a.Name), lower(@Search)) > 0
                )
        ),
        BookCount AS
        (
            SELECT COUNT(*) AS TotalCount
            FROM FilteredBooks
        ),
        PagedBooks AS
        (
            SELECT Id, Title, Version
            FROM FilteredBooks
            ORDER BY {orderBy}
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
        ORDER BY {resultOrderBy}, a.Name, a.Id;
        """;
    }

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