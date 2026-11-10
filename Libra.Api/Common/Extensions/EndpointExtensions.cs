namespace Libra.Api.Common.Extensions;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var bookGroup = app.MapGroup("api")
            .WithTags("Book");

        GetBooksEndpoint.MapEndpoint(bookGroup);
        
        return app;
    }
}