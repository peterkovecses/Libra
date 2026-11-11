namespace Libra.Api.IntegrationTests.Features.Books;

public sealed class GetAuthorsTests : IntegrationTestBase
{
    [Fact]
    public async Task GetAuthors_WhenRequestingSecondPage_ShouldReturnAuthorsOrderedByName()
    {
        // Arrange
        const string uri = "/api/authors?pageNumber=2&pageSize=3";
        const int expectedTotalCount = 5;
        const int expectedPageNumber = 2;
        string[] expectedNames = ["Dan Brown", "David Thomas"];
        await SeedAuthorsAsync(
            new Author { Id = 1, Name = "David Thomas" },
            new Author { Id = 2, Name = "Agatha Christie" },
            new Author { Id = 3, Name = "Dan Brown" },
            new Author { Id = 4, Name = "Arthur C. Clarke" },
            new Author { Id = 5, Name = "Aldous Huxley" });

        // Act
        var actual = await _client.GetFromJsonAsync<PaginatedList<Author>>(uri, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expectedTotalCount, actual.TotalCount);
        Assert.Equal(expectedPageNumber, actual.PageNumber);
        Assert.Equal(expectedNames, actual.Items.Select(author => author.Name));
    }
}
