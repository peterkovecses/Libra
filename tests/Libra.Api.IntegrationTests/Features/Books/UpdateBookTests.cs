namespace Libra.Api.IntegrationTests.Features.Books;

public sealed class UpdateBookTests : IntegrationTestBase
{
    [Fact]
    public async Task UpdateBook_WhenRequestIsValid_ShouldPersistTitleVersionAndAuthorChanges()
    {
        // Arrange
        const int bookId = 8;
        const string uri = "/api/books/8";
        const string expectedTitle = "Updated title";
        const string expectedVersion = "2";
        const long expectedStoredVersion = 2;
        int[] expectedAuthorIds = [1, 5];
        string[] expectedAuthors = ["Arthur C. Clarke", "George Orwell"];
        var request = new UpdateBookRequest(expectedTitle, expectedAuthorIds, "1");
        const HttpStatusCode expected = HttpStatusCode.OK;
        await SeedAuthorsAsync(
            new Author { Id = 1, Name = "George Orwell" },
            new Author { Id = 4, Name = "Isaac Asimov" },
            new Author { Id = 5, Name = "Arthur C. Clarke" });
        await SeedBookAsync(bookId, "Original title", 4, 5);

        // Act
        using var actual = await _client.PutAsJsonAsync(uri, request, TestContext.Current.CancellationToken);
        var book = await actual.Content.ReadFromJsonAsync<Book>(TestContext.Current.CancellationToken);
        var stored = await GetBookByIdAsync(bookId);

        // Assert
        Assert.Equal(expected, actual.StatusCode);
        Assert.NotNull(book);
        Assert.Equal(expectedTitle, book.Title);
        Assert.Equal(expectedVersion, book.Version);
        Assert.Equal(expectedAuthors, book.Authors.Select(author => author.Name));
        Assert.Equal(expectedTitle, stored.Title);
        Assert.Equal(expectedStoredVersion, stored.Version);
        Assert.Equal(expectedAuthorIds, stored.AuthorIds);
    }

    [Fact]
    public async Task UpdateBook_WhenAuthorDoesNotExist_ShouldRollBackTitleVersionAndAuthors()
    {
        // Arrange
        const int bookId = 8;
        const string uri = "/api/books/8";
        const string newTitle = "Must be rolled back";
        const int unknownAuthorId = 9999;
        const string authorIdsKey = "AuthorIds";
        const HttpStatusCode expected = HttpStatusCode.BadRequest;
        var request = new UpdateBookRequest(newTitle, [1, unknownAuthorId], "1");
        await SeedAuthorsAsync(
            new Author { Id = 1, Name = "George Orwell" },
            new Author { Id = 4, Name = "Isaac Asimov" },
            new Author { Id = 5, Name = "Arthur C. Clarke" });
        await SeedBookAsync(bookId, "Original title", 4, 5);
        var original = await GetBookByIdAsync(bookId);

        // Act
        using var actual = await _client.PutAsJsonAsync(uri, request, TestContext.Current.CancellationToken);
        var problem = await actual.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);
        var stored = await GetBookByIdAsync(bookId);

        // Assert
        Assert.Equal(expected, actual.StatusCode);
        Assert.NotNull(problem);
        Assert.Contains(authorIdsKey, problem.Errors.Keys);
        Assert.Contains(unknownAuthorId.ToString(), problem.Errors[authorIdsKey][0]);
        Assert.Equal(original.Title, stored.Title);
        Assert.Equal(original.Version, stored.Version);
        Assert.Equal(original.AuthorIds, stored.AuthorIds);
    }

    [Fact]
    public async Task UpdateBook_WhenVersionIsStale_ShouldReturnConflictAndPreserveWinningUpdate()
    {
        // Arrange
        const int bookId = 8;
        const string uri = "/api/books/8";
        const string winningTitle = "Winning update";
        const string staleTitle = "Stale update";
        const string originalVersion = "1";
        const long expectedVersion = 2;
        int[] expectedAuthorIds = [1, 5];
        var winningRequest = new UpdateBookRequest(winningTitle, expectedAuthorIds, originalVersion);
        var staleRequest = new UpdateBookRequest(staleTitle, [4], originalVersion);
        const HttpStatusCode expected = HttpStatusCode.Conflict;
        await SeedAuthorsAsync(
            new Author { Id = 1, Name = "George Orwell" },
            new Author { Id = 4, Name = "Isaac Asimov" },
            new Author { Id = 5, Name = "Arthur C. Clarke" });
        await SeedBookAsync(bookId, "Original title", 4, 5);
        using var winningResponse = await _client.PutAsJsonAsync(uri, winningRequest, TestContext.Current.CancellationToken);
        winningResponse.EnsureSuccessStatusCode();

        // Act
        using var actual = await _client.PutAsJsonAsync(uri, staleRequest, TestContext.Current.CancellationToken);
        var stored = await GetBookByIdAsync(bookId);

        // Assert
        Assert.Equal(expected, actual.StatusCode);
        Assert.Equal(winningTitle, stored.Title);
        Assert.Equal(expectedVersion, stored.Version);
        Assert.Equal(expectedAuthorIds, stored.AuthorIds);
    }

    [Fact]
    public async Task UpdateBook_WhenBookDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        const string uri = "/api/books/9999";
        const HttpStatusCode expected = HttpStatusCode.NotFound;
        var request = new UpdateBookRequest("Unchanged", [], "1");

        // Act
        using var actual = await _client.PutAsJsonAsync(uri, request, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, actual.StatusCode);
    }
}
