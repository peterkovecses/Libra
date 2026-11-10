namespace Libra.Api.Contracts;

public interface IDbConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(
        CancellationToken cancellationToken);
}