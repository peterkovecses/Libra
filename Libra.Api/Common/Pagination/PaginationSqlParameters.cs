namespace Libra.Api.Common.Pagination;

public static class PaginationSqlParameters
{
    public static object Create(Pagination pagination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(
            pagination.PageNumber, Pagination.MinPageNumber);

        ArgumentOutOfRangeException.ThrowIfLessThan(
            pagination.PageSize, 1);

        ArgumentOutOfRangeException.ThrowIfGreaterThan(
            pagination.PageSize, Pagination.MaxPageSize);

        return new
        {
            pagination.PageSize,
            pagination.Offset
        };
    }
}