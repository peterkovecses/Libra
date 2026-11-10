namespace Libra.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        services.AddScoped<GetBooksHandler>();
        
        return services;
    }
}