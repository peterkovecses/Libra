namespace Libra.Api.Features.Books;

public static class UpdateBookEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapPut("books/{id}", HandleAsync)
            .WithName("UpdateBook")
            .Produces<Book>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithValidation<UpdateBookRequest>();

    private static async Task<IResult> HandleAsync(
        int id, UpdateBookRequest request, UpdateBookHandler handler, CancellationToken cancellationToken)
    {
        if (id <= 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Id"] = ["Id must be greater than 0."]
            });
        }

        var result = await handler.HandleAsync(id, request, cancellationToken);

        return result switch
        {
            UpdatedBook updated => Results.Ok(updated.Book),
            BookNotFound => Results.NotFound(),
            BookConflict => Results.Problem(statusCode: StatusCodes.Status409Conflict,
                title: "Conflict", detail: "The book has changed. Reload it before saving."),
            InvalidAuthors invalid => Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["AuthorIds"] = [$"Unknown author IDs: {string.Join(", ", invalid.Ids)}."]
            })
        };
    }
}

public sealed class UpdateBookHandler(IDbConnectionFactory connectionFactory)
{
    public async Task<UpdateBookResult> HandleAsync(int id, UpdateBookRequest request, CancellationToken cancellationToken)
    {
        var version = long.Parse(request.Version, CultureInfo.InvariantCulture);
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var updated = await UpdateTitleAsync(connection, transaction, id, request.Title, version, cancellationToken);
        if (!updated)
        {
            return await GetUpdateFailureAsync(connection, transaction, id, version, cancellationToken);
        }

        var invalidAuthorIds = await FindInvalidAuthorsAsync(connection, transaction, request.AuthorIds, cancellationToken);
        if (invalidAuthorIds.Length > 0)
        {
            return new InvalidAuthors(invalidAuthorIds);
        }

        await SyncAuthorsAsync(connection, transaction, id, request.AuthorIds, cancellationToken);
        var book = await ReadUpdatedBookAsync(connection, transaction, id, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new UpdatedBook(book);
    }

    private static async Task<bool> UpdateTitleAsync(
        DbConnection connection, DbTransaction transaction, int id, string title, long version,
        CancellationToken cancellationToken)
    {
        var changed = await connection.ExecuteAsync(new CommandDefinition(
            """
            UPDATE Books SET Title = @Title, Version = Version + 1
            WHERE Id = @Id AND Version = @Version AND Version < 9223372036854775807;
            """,
            new { Id = id, Title = title, Version = version }, transaction, cancellationToken: cancellationToken));

        return changed == 1;
    }

    private static async Task<UpdateBookResult> GetUpdateFailureAsync(
        DbConnection connection, DbTransaction transaction, int id, long version,
        CancellationToken cancellationToken)
    {
        var currentVersion = await connection.QuerySingleOrDefaultAsync<long?>(new CommandDefinition(
            "SELECT Version FROM Books WHERE Id = @Id;",
            new { Id = id }, transaction, cancellationToken: cancellationToken));

        if (currentVersion is null)
        {
            return new BookNotFound();
        }

        if (currentVersion == long.MaxValue && currentVersion == version)
        {
            throw new InvalidOperationException("Book version has reached its maximum value.");
        }

        return new BookConflict();
    }

    private static async Task<int[]> FindInvalidAuthorsAsync(
        DbConnection connection, DbTransaction transaction, int[] authorIds, CancellationToken cancellationToken)
    {
        var existingAuthors = (await connection.QueryAsync<int>(new CommandDefinition(
            "SELECT Id FROM Authors WHERE Id IN (SELECT value FROM json_each(@Ids));",
            new { Ids = JsonSerializer.Serialize(authorIds) }, transaction,
            cancellationToken: cancellationToken))).ToHashSet();

        return [.. authorIds.Where(authorId => !existingAuthors.Contains(authorId))];
    }

    private static async Task SyncAuthorsAsync(
        DbConnection connection, DbTransaction transaction, int bookId, int[] authorIds,
        CancellationToken cancellationToken)
    {
        var linkedAuthorIds = (await connection.QueryAsync<int>(new CommandDefinition(
            "SELECT AuthorId FROM BookAuthors WHERE BookId = @Id;", new { Id = bookId }, transaction,
            cancellationToken: cancellationToken))).ToHashSet();

        foreach (var removedId in linkedAuthorIds.Except(authorIds))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "DELETE FROM BookAuthors WHERE BookId = @Id AND AuthorId = @AuthorId;",
                new { Id = bookId, AuthorId = removedId }, transaction, cancellationToken: cancellationToken));
        }

        foreach (var addedId in authorIds.Except(linkedAuthorIds))
        {
            await connection.ExecuteAsync(new CommandDefinition(
                "INSERT INTO BookAuthors (BookId, AuthorId) VALUES (@Id, @AuthorId);",
                new { Id = bookId, AuthorId = addedId }, transaction, cancellationToken: cancellationToken));
        }
    }

    private static async Task<Book> ReadUpdatedBookAsync(
        DbConnection connection, DbTransaction transaction, int id, CancellationToken cancellationToken)
    {
        var books = await connection.QueryWithChildrenAsync<Book, Author, int>(
            new CommandDefinition(
                """
                SELECT b.Id, b.Title, CAST(b.Version AS TEXT) AS Version, a.Id, a.Name
                FROM Books b
                LEFT JOIN BookAuthors ba ON ba.BookId = b.Id
                LEFT JOIN Authors a ON a.Id = ba.AuthorId
                WHERE b.Id = @Id ORDER BY a.Name, a.Id;
                """, new { Id = id }, transaction, cancellationToken: cancellationToken),
            b => b.Id, (book, author) => book.Authors.Add(author));

        return books.Single();
    }
}

public record UpdateBookRequest(string Title, int[] AuthorIds, string Version);
public record UpdatedBook(Book Book);
public record BookNotFound;
public record BookConflict;
public record InvalidAuthors(int[] Ids);
public readonly union UpdateBookResult(UpdatedBook, BookNotFound, BookConflict, InvalidAuthors);

public sealed class UpdateBookValidator : AbstractValidator<UpdateBookRequest>
{
    public UpdateBookValidator()
    {
        RuleFor(x => x.Title).NotEmpty().Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Title must not be blank.");
        RuleFor(x => x.AuthorIds).NotNull();
        RuleForEach(x => x.AuthorIds).GreaterThan(0);
        RuleFor(x => x.AuthorIds).Must(ids => ids is null || ids.Distinct().Count() == ids.Length)
            .WithMessage("Author IDs must be unique.");
        RuleFor(x => x.Version).Must(IsVersion).WithMessage("Version must be a positive decimal integer string.");
    }

    private static bool IsVersion(string? value) =>
        value is { Length: > 0 } &&
        value.All(c => c is >= '0' and <= '9') &&
        long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var version) &&
        version > 0;
}