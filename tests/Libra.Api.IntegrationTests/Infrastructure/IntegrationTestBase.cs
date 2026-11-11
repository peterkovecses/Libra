namespace Libra.Api.IntegrationTests.Infrastructure;

public abstract class IntegrationTestBase : IAsyncLifetime
{
    private readonly ApiWebApplicationFactory _factory = new();
    protected HttpClient _client = null!;

    public async ValueTask InitializeAsync()
    {
        try
        {
            _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
        }
        catch
        {
            await _factory.DisposeAsync();
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _client?.Dispose();
        await _factory.DisposeAsync();
    }

    protected async Task SeedAuthorsAsync(params Author[] authors)
    {
        await using var connection = await OpenConnectionAsync();
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Authors (Id, Name) VALUES (@Id, @Name);",
            authors, cancellationToken: TestContext.Current.CancellationToken));
    }

    protected async Task SeedBookAsync(int id, string title, params int[] authorIds)
    {
        await using var connection = await OpenConnectionAsync();
        await using var transaction = await connection.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO Books (Id, Title) VALUES (@Id, @Title);",
            new { Id = id, Title = title }, transaction, cancellationToken: TestContext.Current.CancellationToken));
        await connection.ExecuteAsync(new CommandDefinition(
            "INSERT INTO BookAuthors (BookId, AuthorId) VALUES (@BookId, @AuthorId);",
            authorIds.Select(authorId => new { BookId = id, AuthorId = authorId }),
            transaction, cancellationToken: TestContext.Current.CancellationToken));
        await transaction.CommitAsync(TestContext.Current.CancellationToken);
    }

    protected async Task<BookState> GetBookByIdAsync(int id)
    {
        await using var connection = await OpenConnectionAsync();
        var book = await connection.QuerySingleAsync<BookRow>(new CommandDefinition(
            "SELECT Title, Version FROM Books WHERE Id = @Id;",
            new { Id = id }, cancellationToken: TestContext.Current.CancellationToken));
        var authorIds = await connection.QueryAsync<int>(new CommandDefinition(
            "SELECT AuthorId FROM BookAuthors WHERE BookId = @Id ORDER BY AuthorId;",
            new { Id = id }, cancellationToken: TestContext.Current.CancellationToken));
        return new BookState(book.Title, book.Version, [.. authorIds]);
    }

    private Task<System.Data.Common.DbConnection> OpenConnectionAsync() =>
        _factory.Services.GetRequiredService<IDbConnectionFactory>()
            .OpenConnectionAsync(TestContext.Current.CancellationToken);

    private sealed class BookRow
    {
        public required string Title { get; init; }
        public long Version { get; init; }
    }

    protected sealed record BookState(string Title, long Version, int[] AuthorIds);
}
