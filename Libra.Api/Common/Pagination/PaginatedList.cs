namespace Libra.Api.Common.Pagination;

public record PaginatedList<T>(
    IReadOnlyCollection<T> Items,
    int TotalCount,
    int PageNumber,
    int PageSize)
{
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > Pagination.MinPageNumber;
    public bool HasNextPage => PageNumber < TotalPages;
}