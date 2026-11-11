namespace Libra.Api.Infrastructure.Persistence.Extensions;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        string contentRootPath)
    {
        var databaseDirectory = Path.Combine(
            contentRootPath,
            "App_Data");

        Directory.CreateDirectory(databaseDirectory);

        var connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = Path.Combine(
                databaseDirectory,
                "libra.db"),

            ForeignKeys = true
        }.ToString();

        services.AddSingleton<IDbConnectionFactory>(
            _ => new SqliteConnectionFactory(connectionString));

        services.AddSingleton<DatabaseInitializer>();

        return services;
    }
}