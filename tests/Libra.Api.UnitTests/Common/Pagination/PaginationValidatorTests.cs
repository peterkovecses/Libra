using PaginationModel = Libra.Api.Common.Pagination.Pagination;
using PaginationValidatorSut = Libra.Api.Common.Pagination.PaginationValidator;

namespace Libra.Api.UnitTests.Common.Pagination;

public class PaginationValidatorTests
{
    private const int MinimumPageSize = 1;

    [Theory]
    [InlineData(PaginationModel.MinPageNumber, MinimumPageSize)]
    [InlineData(PaginationModel.MinPageNumber, PaginationModel.MaxPageSize)]
    public void Validate_WhenPaginationIsWithinLimits_ShouldBeValid(int pageNumber, int pageSize)
    {
        // Arrange
        var sut = new PaginationValidatorSut();
        var pagination = new PaginationModel { PageNumber = pageNumber, PageSize = pageSize };
        const bool expected = true;

        // Act
        var actual = sut.Validate(pagination);

        // Assert
        Assert.Equal(expected, actual.IsValid);
    }

    [Theory]
    [InlineData(PaginationModel.MinPageNumber - 1, MinimumPageSize, nameof(PaginationModel.PageNumber))]
    [InlineData(PaginationModel.MinPageNumber, MinimumPageSize - 1, nameof(PaginationModel.PageSize))]
    [InlineData(PaginationModel.MinPageNumber, PaginationModel.MaxPageSize + 1, nameof(PaginationModel.PageSize))]
    public void Validate_WhenPaginationIsOutsideLimits_ShouldIdentifyInvalidProperty(
        int pageNumber, int pageSize, string expectedPropertyName)
    {
        // Arrange
        var sut = new PaginationValidatorSut();
        var pagination = new PaginationModel { PageNumber = pageNumber, PageSize = pageSize };

        // Act
        var actual = sut.Validate(pagination);

        // Assert
        Assert.Contains(actual.Errors, error => error.PropertyName == expectedPropertyName);
    }
}
