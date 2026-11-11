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

        await using (var schemaTransaction = await connection.BeginTransactionAsync(cancellationToken))
        {
            var columns = await connection.QueryAsync<string>(
                new CommandDefinition(
                    "SELECT name FROM pragma_table_info('Books');",
                    transaction: schemaTransaction,
                    cancellationToken: cancellationToken));
            if (!columns.Contains("Version", StringComparer.OrdinalIgnoreCase))
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    "ALTER TABLE Books ADD COLUMN Version INTEGER NOT NULL DEFAULT 1;",
                    transaction: schemaTransaction,
                    cancellationToken: cancellationToken));
            }

            await schemaTransaction.CommitAsync(cancellationToken);
        }

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