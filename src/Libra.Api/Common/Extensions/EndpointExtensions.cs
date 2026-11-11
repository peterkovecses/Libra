namespace Libra.Api.Common.Extensions;

public static class EndpointExtensions
{
    public static IEndpointRouteBuilder MapEndpoints(this IEndpointRouteBuilder app)
    {
        var bookGroup = app.MapGroup("api")
            .WithTags("Book");

        GetBooksEndpoint.MapEndpoint(bookGroup);
        GetBookByIdEndpoint.MapEndpoint(bookGroup);
        UpdateBookEndpoint.MapEndpoint(bookGroup);
        GetAuthorsEndpoint.MapEndpoint(app.MapGroup("api").WithTags("Author"));
        
        return app;
    }
}