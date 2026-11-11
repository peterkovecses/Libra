namespace Libra.Api.Common.Pagination;

public sealed class Pagination
{
    public const int MinPageNumber = 1;
    public const int MaxPageSize = 100;

    public int PageNumber { get; init; }
    public int PageSize { get; init; }

    public long Offset => (long)(PageNumber - 1) * PageSize;
}