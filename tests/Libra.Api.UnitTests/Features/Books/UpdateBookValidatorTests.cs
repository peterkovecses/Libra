using Libra.Api.Features.Books;

namespace Libra.Api.UnitTests.Features.Books;

public class UpdateBookValidatorTests
{
    private const string ValidTitle = "A book";
    private const string ValidVersion = "1";
    private const int ValidAuthorId = 1;

    [Fact]
    public void Validate_WhenRequestIsValid_ShouldBeValid()
    {
        // Arrange
        var sut = new UpdateBookValidator();
        var request = new UpdateBookRequest(ValidTitle, [ValidAuthorId], ValidVersion);
        const bool expected = true;

        // Act
        var actual = sut.Validate(request);

        // Assert
        Assert.Equal(expected, actual.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenTitleIsEmptyOrWhitespace_ShouldIdentifyTitle(string title)
    {
        // Arrange
        var sut = new UpdateBookValidator();
        var request = new UpdateBookRequest(title, [ValidAuthorId], ValidVersion);
        const string expectedPropertyName = nameof(UpdateBookRequest.Title);

        // Act
        var actual = sut.Validate(request);

        // Assert
        Assert.Contains(actual.Errors, error => error.PropertyName == expectedPropertyName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenAuthorIdIsNotPositive_ShouldIdentifyAuthorIds(int authorId)
    {
        // Arrange
        var sut = new UpdateBookValidator();
        var authorIds = new[] { authorId };
        var request = new UpdateBookRequest(ValidTitle, authorIds, ValidVersion);
        const string expectedPropertyName = $"{nameof(UpdateBookRequest.AuthorIds)}[0]";

        // Act
        var actual = sut.Validate(request);

        // Assert
        Assert.Contains(actual.Errors, error => error.PropertyName == expectedPropertyName);
    }

    [Fact]
    public void Validate_WhenAuthorIdsContainDuplicates_ShouldIdentifyAuthorIds()
    {
        // Arrange
        var sut = new UpdateBookValidator();
        var duplicateAuthorIds = new[] { ValidAuthorId, ValidAuthorId };
        var request = new UpdateBookRequest(ValidTitle, duplicateAuthorIds, ValidVersion);
        const string expectedPropertyName = nameof(UpdateBookRequest.AuthorIds);

        // Act
        var actual = sut.Validate(request);

        // Assert
        Assert.Contains(actual.Errors, error => error.PropertyName == expectedPropertyName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("+1")]
    [InlineData(" 1")]
    [InlineData("1.0")]
    [InlineData("9223372036854775808")]
    [InlineData("١")]
    public void Validate_WhenVersionIsNotPositiveAsciiInt64_ShouldIdentifyVersion(string version)
    {
        // Arrange
        var sut = new UpdateBookValidator();
        var request = new UpdateBookRequest(ValidTitle, [ValidAuthorId], version);
        const string expectedPropertyName = nameof(UpdateBookRequest.Version);

        // Act
        var actual = sut.Validate(request);

        // Assert
        Assert.Contains(actual.Errors, error => error.PropertyName == expectedPropertyName);
    }
}
