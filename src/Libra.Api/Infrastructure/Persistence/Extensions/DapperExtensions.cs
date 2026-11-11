namespace Libra.Api.Infrastructure.Persistence.Extensions;

/// <summary>
/// Provides Dapper query helpers for mapping joined rows to parent objects and their children.
/// </summary>
public static class DapperExtensions
{
    /// <summary>
    /// Executes a query and combines rows with the same parent key into a single parent object.
    /// </summary>
    /// <typeparam name="TParent">The type mapped from the parent columns.</typeparam>
    /// <typeparam name="TChild">The type mapped from the child columns.</typeparam>
    /// <typeparam name="TKey">The non-null type of the parent key.</typeparam>
    /// <param name="connection">The database connection used to execute the query.</param>
    /// <param name="command">The Dapper command, including its parameters and cancellation token.</param>
    /// <param name="parentKeySelector">Selects the key used to identify a parent across rows.</param>
    /// <param name="addChildAction">
    /// Adds a non-null child to the retained parent. The caller is responsible for handling duplicate children.
    /// </param>
    /// <param name="splitOn">
    /// The column name at which Dapper starts mapping the child; defaults to <c>Id</c>.
    /// </param>
    /// <returns>A task containing the distinct parent objects with their mapped children.</returns>
    /// <remarks>
    /// The query must select parent columns before child columns. For an outer join, the first child
    /// column must be null when no child exists so Dapper does not create a child for that row.
    /// </remarks>
    public static async Task<List<TParent>> QueryWithChildrenAsync<TParent, TChild, TKey>(
        this IDbConnection connection,
        CommandDefinition command,
        Func<TParent, TKey> parentKeySelector,
        Action<TParent, TChild> addChildAction,
        string splitOn = "Id")
        where TParent : notnull where TKey : notnull
    {
        var parents = new ParentCollection<TParent, TChild, TKey>(parentKeySelector, addChildAction);

        await connection.QueryAsync<TParent, TChild, TParent>(
            command,
            parents.Add,
            splitOn: splitOn);

        return parents.Items;
    }

    /// <summary>
    /// Executes a query and combines its total count and joined parent-child rows into a paginated list.
    /// </summary>
    /// <typeparam name="TParent">The reference type mapped from the parent columns.</typeparam>
    /// <typeparam name="TChild">The type mapped from the child columns.</typeparam>
    /// <typeparam name="TKey">The non-null type of the parent key.</typeparam>
    /// <param name="connection">The database connection used to execute the query.</param>
    /// <param name="command">The Dapper command, including its parameters and cancellation token.</param>
    /// <param name="pagination">The page number and page size included in the result.</param>
    /// <param name="parentKeySelector">Selects the key used to identify a parent across rows.</param>
    /// <param name="addChildAction">
    /// Adds a non-null child to the retained parent. The caller is responsible for handling duplicate children.
    /// </param>
    /// <param name="splitOn">
    /// The comma-separated column names at which Dapper starts mapping the parent and child;
    /// defaults to <c>Id,Id</c>.
    /// </param>
    /// <returns>A task containing the distinct parents, their children, and the pagination metadata.</returns>
    /// <exception cref="InvalidOperationException">The query returned no rows containing a total count.</exception>
    /// <remarks>
    /// The query must return at least one row with a <c>TotalCount</c> column followed by parent and child
    /// columns, and the total count must be the same on every row. The first parent and child columns must
    /// be null when their joined rows are absent. An absent parent produces no item, allowing an empty page
    /// to retain its total count. The caller is responsible for filtering, ordering, and paging parents
    /// before joining children.
    /// </remarks>
    public static async Task<PaginatedList<TParent>> QueryPaginatedWithChildrenAsync<TParent, TChild, TKey>(
        this IDbConnection connection,
        CommandDefinition command,
        Pagination pagination,
        Func<TParent, TKey> parentKeySelector,
        Action<TParent, TChild> addChildAction,
        string splitOn = "Id,Id")
        where TParent : class where TKey : notnull
    {
        var parents = new ParentCollection<TParent, TChild, TKey>(parentKeySelector, addChildAction);
        int? totalCount = null;

        await connection.QueryAsync<PaginationCount, TParent, TChild, TParent?>(
            command,
            (count, parent, child) =>
            {
                totalCount = count.TotalCount;

                return parent is null ? null : parents.Add(parent, child);
            },
            splitOn: splitOn);

        return new PaginatedList<TParent>(
            parents.Items,
            totalCount ?? throw new InvalidOperationException("The pagination query did not return a total count."),
            pagination.PageNumber,
            pagination.PageSize);
    }

    private sealed class PaginationCount
    {
        public int TotalCount { get; init; }
    }

    private sealed class ParentCollection<TParent, TChild, TKey>(
        Func<TParent, TKey> parentKeySelector,
        Action<TParent, TChild> addChildAction)
        where TParent : notnull where TKey : notnull
    {
        private readonly Dictionary<TKey, TParent> _parents = new();

        public List<TParent> Items => _parents.Values.ToList();

        public TParent Add(TParent parent, TChild child)
        {
            var key = parentKeySelector(parent);
            if (!_parents.TryGetValue(key, out var existingParent))
            {
                existingParent = parent;
                _parents.Add(key, existingParent);
            }

            if (child is not null)
            {
                addChildAction(existingParent, child);
            }

            return existingParent;
        }
    }
}