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

    [Fact]
    public async Task GetBooks_WhenSearchingTitlesAndAuthors_ShouldReturnFilteredCountAndAllAuthors()
    {
        // Arrange
        const string uri = "/api/books?pageNumber=1&pageSize=1&search=%20ORWELL%20";
        const int expectedTotalCount = 2;
        const int expectedId = 1;
        string[] expectedAuthors = ["George Orwell", "Other Author"];
        await SeedAuthorsAsync(
            new Author { Id = 1, Name = "George Orwell" },
            new Author { Id = 2, Name = "Other Author" });
        await SeedBookAsync(1, "Animal Farm", 1, 2);
        await SeedBookAsync(2, "Orwell's World", 2);
        await SeedBookAsync(3, "Unrelated");

        // Act
        var actual = await _client.GetFromJsonAsync<PaginatedList<Book>>(uri, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expectedTotalCount, actual.TotalCount);
        var book = Assert.Single(actual.Items);
        Assert.Equal(expectedId, book.Id);
        Assert.Equal(expectedAuthors, book.Authors.Select(author => author.Name));
    }

    [Fact]
    public async Task GetBooks_WhenFilteredPageIsBeyondLastPage_ShouldReturnFilteredCount()
    {
        // Arrange
        const string uri = "/api/books?pageNumber=2&pageSize=1&search=match";
        const int expectedTotalCount = 1;
        await SeedBookAsync(1, "Matching");
        await SeedBookAsync(2, "Other");

        // Act
        var actual = await _client.GetFromJsonAsync<PaginatedList<Book>>(uri, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(actual);
        Assert.Empty(actual.Items);
        Assert.Equal(expectedTotalCount, actual.TotalCount);
    }

    [Fact]
    public async Task GetBooks_WhenMultipleAuthorsMatch_ShouldCountTheBookOnce()
    {
        // Arrange
        const string uri = "/api/books?pageNumber=1&pageSize=10&search=smith";
        const int expectedTotalCount = 1;
        const int expectedAuthorCount = 2;
        await SeedAuthorsAsync(
            new Author { Id = 1, Name = "Jane Smith" },
            new Author { Id = 2, Name = "John Smith" });
        await SeedBookAsync(1, "A Book", 1, 2);

        // Act
        var actual = await _client.GetFromJsonAsync<PaginatedList<Book>>(uri, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expectedTotalCount, actual.TotalCount);
        Assert.Equal(expectedAuthorCount, Assert.Single(actual.Items).Authors.Count);
    }

    [Fact]
    public async Task GetBooks_WhenSearchHasNoMatches_ShouldReturnEmptyPage()
    {
        // Arrange
        const string uri = "/api/books?pageNumber=1&pageSize=10&search=missing";
        const int expectedTotalCount = 0;
        await SeedBookAsync(1, "A Book");

        // Act
        var actual = await _client.GetFromJsonAsync<PaginatedList<Book>>(uri, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(actual);
        Assert.Empty(actual.Items);
        Assert.Equal(expectedTotalCount, actual.TotalCount);
    }

    [Fact]
    public async Task GetBooks_WhenSearchExceedsLimit_ShouldReturnValidationProblem()
    {
        // Arrange
        const int searchLength = 201;
        const string expectedError = "Search";
        const HttpStatusCode expected = HttpStatusCode.BadRequest;
        var uri = $"/api/books?pageNumber=1&pageSize=10&search={new string('a', searchLength)}";

        // Act
        using var actual = await _client.GetAsync(uri, TestContext.Current.CancellationToken);
        var problem = await actual.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, actual.StatusCode);
        Assert.NotNull(problem);
        Assert.Contains(expectedError, problem.Errors.Keys);
    }

    [Theory]
    [InlineData("%25", "100% Complete")]
    [InlineData("_", "Under_score")]
    [InlineData("%27%20OR%201%3D1--", "Quote ' OR 1=1--")]
    public async Task GetBooks_WhenSearchingSpecialCharacters_ShouldMatchThemLiterally(string search, string expectedTitle)
    {
        // Arrange
        var uri = $"/api/books?pageNumber=1&pageSize=10&search={search}";
        await SeedBookAsync(1, expectedTitle);
        await SeedBookAsync(2, "Unrelated");

        // Act
        var actual = await _client.GetFromJsonAsync<PaginatedList<Book>>(uri, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(actual);
        Assert.Equal(expectedTitle, Assert.Single(actual.Items).Title);
        Assert.Equal(1, actual.TotalCount);
    }

    [Theory]
    [InlineData("title", "asc", 2, 1, 3)]
    [InlineData("title", "desc", 1, 3, 2)]
    [InlineData("id", "asc", 1, 2, 3)]
    [InlineData("id", "desc", 3, 2, 1)]
    public async Task GetBooks_WhenSortingAcrossPages_ShouldKeepDeterministicOrder(
        string sortBy, string sortDirection, int firstId, int secondId, int thirdId)
    {
        // Arrange
        var firstPageUri = $"/api/books?pageNumber=1&pageSize=2&sortBy={sortBy}&sortDirection={sortDirection}";
        var secondPageUri = $"/api/books?pageNumber=2&pageSize=2&sortBy={sortBy}&sortDirection={sortDirection}";
        int[] expected = [firstId, secondId, thirdId];
        await SeedBookAsync(1, "Same");
        await SeedBookAsync(2, "Before");
        await SeedBookAsync(3, "Same");

        // Act
        var firstPage = await _client.GetFromJsonAsync<PaginatedList<Book>>(firstPageUri, TestContext.Current.CancellationToken);
        var secondPage = await _client.GetFromJsonAsync<PaginatedList<Book>>(secondPageUri, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(expected, firstPage.Items.Concat(secondPage.Items).Select(book => book.Id));
    }

    [Theory]
    [InlineData("", 3, 1, 4, 2)]
    [InlineData("&sortBy=title&sortDirection=asc", 3, 1, 4, 2)]
    [InlineData("&sortBy=title&sortDirection=desc", 2, 1, 4, 3)]
    public async Task GetBooks_WhenSortingMixedCaseTitlesAcrossFilteredPages_ShouldIgnoreCaseAndBreakTiesById(
        string sorting, int firstId, int secondId, int thirdId, int fourthId)
    {
        // Arrange
        var firstPageUri = $"/api/books?pageNumber=1&pageSize=2&search=writer{sorting}";
        var secondPageUri = $"/api/books?pageNumber=2&pageSize=2&search=writer{sorting}";
        int[] expectedFirstPage = [firstId, secondId];
        int[] expectedSecondPage = [thirdId, fourthId];
        const int expectedTotalCount = 4;
        await SeedAuthorsAsync(new Author { Id = 1, Name = "Writer" });
        await SeedBookAsync(1, "Book", 1);
        await SeedBookAsync(2, "zebra", 1);
        await SeedBookAsync(3, "APPLE", 1);
        await SeedBookAsync(4, "bOOK", 1);
        await SeedBookAsync(5, "Not included");

        // Act
        var firstPage = await _client.GetFromJsonAsync<PaginatedList<Book>>(firstPageUri, TestContext.Current.CancellationToken);
        var secondPage = await _client.GetFromJsonAsync<PaginatedList<Book>>(secondPageUri, TestContext.Current.CancellationToken);

        // Assert
        Assert.NotNull(firstPage);
        Assert.NotNull(secondPage);
        Assert.Equal(expectedTotalCount, firstPage.TotalCount);
        Assert.Equal(expectedTotalCount, secondPage.TotalCount);
        Assert.Equal(expectedFirstPage, firstPage.Items.Select(book => book.Id));
        Assert.Equal(expectedSecondPage, secondPage.Items.Select(book => book.Id));
    }

    [Theory]
    [InlineData("sortBy=author", "SortBy")]
    [InlineData("sortBy=title%20DESC", "SortBy")]
    [InlineData("sortDirection=down", "SortDirection")]
    public async Task GetBooks_WhenSortOptionIsInvalid_ShouldReturnValidationProblem(string query, string expectedError)
    {
        // Arrange
        var uri = $"/api/books?pageNumber=1&pageSize=10&{query}";
        const HttpStatusCode expected = HttpStatusCode.BadRequest;

        // Act
        using var actual = await _client.GetAsync(uri, TestContext.Current.CancellationToken);
        var problem = await actual.Content.ReadFromJsonAsync<ValidationProblemDetails>(TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(expected, actual.StatusCode);
        Assert.NotNull(problem);
        Assert.Contains(expectedError, problem.Errors.Keys);
    }
}
