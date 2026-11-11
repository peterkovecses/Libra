namespace Libra.Api.Common.Validation;

public class ValidationFilter<TRequest>(IValidator<TRequest> validator) : IEndpointFilter where TRequest : class
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();

        if (request is null)
        {
            return Results.Problem(
                title: "Validation Error",
                detail: $"Could not find request of type {typeof(TRequest).Name}. Use [AsParameters] attribute.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var validationResult = await validator.ValidateAsync(request);

        if (validationResult.IsValid) return await next(context);
        var errors = validationResult.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray()
            );

        return Results.ValidationProblem(errors);

    }
}