namespace Libra.Api.Infrastructure.Persistence.Extensions;

public static class DapperExtensions
{
    public static async Task<List<TParent>> QueryWithChildrenAsync<TParent, TChild, TKey>(
        this IDbConnection connection,
        CommandDefinition command,
        Func<TParent, TKey> parentKeySelector,
        Action<TParent, TChild> addChildAction,
        string splitOn = "Id")
        where TParent : notnull where TKey : notnull
    {
        var cache = new Dictionary<TKey, TParent>();

        await connection.QueryAsync<TParent, TChild, TParent>(
            command,
            (parent, child) =>
            {
                var key = parentKeySelector(parent);
                if (!cache.TryGetValue(key, out var existingParent))
                {
                    existingParent = parent;
                    cache.Add(key, existingParent);
                }

                if (child is not null)
                {
                    addChildAction(existingParent, child);
                }

                return existingParent;
            },
            splitOn: splitOn);

        return cache.Values.ToList();
    }
}