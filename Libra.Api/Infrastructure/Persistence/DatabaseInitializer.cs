namespace Libra.Api.Infrastructure.Persistence;

public sealed class DatabaseInitializer(
    IDbConnectionFactory connectionFactory)
{
    public async Task InitializeAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            await connectionFactory.OpenConnectionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                SqliteSchema.Create,
                cancellationToken: cancellationToken));

        await using var transaction =
            await connection.BeginTransactionAsync(cancellationToken);

        await connection.ExecuteAsync(
            new CommandDefinition(
                SqliteSeed.Insert,
                transaction: transaction,
                cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);
    }
}