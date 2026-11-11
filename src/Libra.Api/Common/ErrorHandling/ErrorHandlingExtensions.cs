namespace Libra.Api.Common.ErrorHandling;

public static class ErrorHandlingExtensions
{
    public static TBuilder AddErrorHandling<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        builder.Services.AddProblemDetails();

        builder.Services.AddExceptionHandler<BadRequestExceptionHandler>();
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

        builder.Services.Configure<RouteHandlerOptions>(options =>
        {
            options.ThrowOnBadRequest = true;
        });

        return builder;
    }

    public static IApplicationBuilder UseErrorHandling(
        this IApplicationBuilder app)
    {
        app.UseExceptionHandler();

        app.UseStatusCodePages(async context =>
        {
            var response = context.HttpContext.Response;

            if (response.ContentType?.Contains("application/json") != true &&
                response.StatusCode is >= 400 and < 600)
            {
                response.ContentType = "application/problem+json";

                var problemDetails = new ProblemDetails
                {
                    Status = response.StatusCode,
                    Type = GetTypeForStatusCode(response.StatusCode),
                    Title = GetTitleForStatusCode(response.StatusCode)
                };

                await response.WriteAsJsonAsync(problemDetails);
            }
        });

        return app;
    }

    private static string GetTypeForStatusCode(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        405 => "https://tools.ietf.org/html/rfc9110#section-15.5.6",
        409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
        500 => "https://tools.ietf.org/html/rfc9110#section-15.6.1",
        502 => "https://tools.ietf.org/html/rfc9110#section-15.6.3",
        503 => "https://tools.ietf.org/html/rfc9110#section-15.6.4",
        _ => "about:blank"
    };

    private static string GetTitleForStatusCode(int statusCode) => statusCode switch
    {
        400 => "Bad Request",
        401 => "Unauthorized",
        403 => "Forbidden",
        404 => "Not Found",
        405 => "Method Not Allowed",
        409 => "Conflict",
        422 => "Unprocessable Entity",
        500 => "Internal Server Error",
        502 => "Bad Gateway",
        503 => "Service Unavailable",
        _ => "Error"
    };
}