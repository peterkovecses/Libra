namespace Libra.Api.Infrastructure.Extensions;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        string contentRootPath)
    {
        services.AddPersistence(contentRootPath);

        return services;
    }
}