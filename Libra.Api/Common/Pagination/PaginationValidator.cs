namespace Libra.Api.Common.Pagination;

public sealed class PaginationValidator
    : AbstractValidator<Pagination>
{
    public PaginationValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(Pagination.MinPageNumber);

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, Pagination.MaxPageSize);
    }
}