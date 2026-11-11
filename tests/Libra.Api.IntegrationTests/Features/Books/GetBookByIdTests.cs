namespace Libra.Api.IntegrationTests.Features.Books;

public sealed class GetBookByIdTests : IntegrationTestBase
{
    [Fact]
    public async Task GetBookById_WhenBookExists_ShouldReturnBookWithOrderedAuthors()
    {
        // Arrange
        const string uri = "/api/books/8";
        const int expectedId = 8;
        const string expectedTitle = "2001: A Space Odyssey";
        const string expectedVersion = "1";
        string[] expectedAuthors = ["Arthur C. Clarke", "Isaac Asimov"];
        await SeedAuthorsAsync(
            new Author { Id = 4, Name = "Isaac Asimov" },
            new Author { Id = 5, Name = "Arthur C. Clarke" });
        await SeedBookAsync(expectedId, expectedTitle, 4, 5);

        // Act
        var actual = await _client.GetFromJsonAsync<Book>(uri, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expectedId, actual.Id);
        Assert.Equal(expectedTitle, actual.Title);
        Assert.Equal(expectedVersion, actual.Version);
        Assert.Equal(expectedAuthors, actual.Authors.Select(author => author.Name));
    }

    [Fact]
    public async Task GetBookById_WhenBookDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        const string uri = "/api/books/9999";
        const HttpStatusCode expected = HttpStatusCode.NotFound;

        // Act
        using var actual = await _client.GetAsync(uri, TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, actual.StatusCode);
    }
}
