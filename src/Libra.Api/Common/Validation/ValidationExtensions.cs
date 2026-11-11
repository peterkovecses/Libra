namespace Libra.Api.Common.Validation;

public static class ValidationExtensions
{
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder) where TRequest : class 
        => builder.AddEndpointFilter<ValidationFilter<TRequest>>();
}