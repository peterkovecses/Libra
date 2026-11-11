namespace Libra.Api.IntegrationTests.Infrastructure;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<DatabaseInitializer>
{
    private readonly string _contentRoot = Path.Combine(Path.GetTempPath(), $"libra-integration-{Guid.NewGuid():N}");
    private readonly string _connectionString;
    private readonly SqliteConnection _keeper;

    public ApiWebApplicationFactory()
    {
        Directory.CreateDirectory(_contentRoot);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = $"libra-{Guid.NewGuid():N}",
            Mode = SqliteOpenMode.Memory,
            Cache = SqliteCacheMode.Shared,
            ForeignKeys = true,
            Pooling = false
        }.ToString();
        _keeper = new SqliteConnection(_connectionString);
        _keeper.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseContentRoot(_contentRoot);
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton<IDbConnectionFactory>(new SqliteConnectionFactory(_connectionString));
            services.Configure<DatabaseInitializationOptions>(options => options.SeedDemoData = false);
        });
    }

    public override async ValueTask DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            await _keeper.DisposeAsync();
            if (Directory.Exists(_contentRoot))
            {
                Directory.Delete(_contentRoot, recursive: true);
            }
        }
    }
}
