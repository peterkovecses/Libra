namespace Libra.Api.IntegrationTests.Features.Books;

public sealed class GetBooksTests : IntegrationTestBase
{
    [Fact]
    public async Task GetBooks_WhenPageContainsBookWithMultipleAuthors_ShouldReturnPagedBooksWithAuthors()
    {
        // Arrange
        const string uri = "/api/books?pageNumber=1&pageSize=2";
        const int expectedTotalCount = 3;
        const int expectedPageNumber = 1;
        const int expectedPageSize = 2;
        string[] expectedTitles = ["1984", "2001: A Space Odyssey"];
        string[] expectedAuthors = ["Arthur C. Clarke", "Isaac Asimov"];
        await SeedAuthorsAsync(
            new Author { Id = 1, Name = "George Orwell" },
            new Author { Id = 4, Name = "Isaac Asimov" },
            new Author { Id = 5, Name = "Arthur C. Clarke" });
        await SeedBookAsync(8, "2001: A Space Odyssey", 4, 5);
        await SeedBookAsync(1, "1984", 1);
        await SeedBookAsync(2, "Animal Farm", 1);

        // Act
        var actual = await _client.GetFromJsonAsync<PaginatedList<Book>>(uri, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expectedTotalCount, actual.TotalCount);
        Assert.Equal(expectedPageNumber, actual.PageNumber);
        Assert.Equal(expectedPageSize, actual.PageSize);
        Assert.Equal(expectedTitles, actual.Items.Select(book => book.Title));
        Assert.Equal(expectedAuthors, actual.Items.Last().Authors.Select(author => author.Name));
    }

    [Fact]
    public async Task GetBooks_WhenPageIsBeyondLastPage_ShouldReturnEmptyItemsAndTotalCount()
    {
        // Arrange
        const string uri = "/api/books?pageNumber=2&pageSize=2";
        const int expectedTotalCount = 1;
        await SeedBookAsync(1, "1984");

        // Act
        var actual = await _client.GetFromJsonAsync<PaginatedList<Book>>(uri, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(actual);
        Assert.Empty(actual.Items);
        Assert.Equal(expectedTotalCount, actual.TotalCount);
    }

    [Fact]
    public async Task GetBooks_WhenPaginationIsInvalid_ShouldReturnValidationProblem()
    {
        // Arrange
        const string uri = "/api/books?pageNumber=0&pageSize=101";
        const HttpStatusCode expected = HttpStatusCode.BadRequest;
        const string expectedPageNumberError = "PageNumber";
        const string expectedPageSizeError = "PageSize";

        // Act
        using var actual = await _client.GetAsync(uri, TestContext.Current.CancellationToken);
        var problem = await actual.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, actual.StatusCode);
        Assert.NotNull(problem);
        Assert.Contains(expectedPageNumberError, problem.Errors.Keys);
        Assert.Contains(expectedPageSizeError, problem.Errors.Keys);
    }
}
