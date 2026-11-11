namespace Libra.Api.Features.Books;

public static class GetAuthorsEndpoint
{
    public static void MapEndpoint(IEndpointRouteBuilder app) =>
        app.MapGet("authors", HandleAsync)
            .WithName("GetAuthors")
            .Produces<PaginatedList<Author>>()
            .ProducesValidationProblem()
            .WithValidation<Pagination>();

    private static async Task<IResult> HandleAsync(
        [AsParameters] Pagination pagination,
        GetAuthorsHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(await handler.HandleAsync(pagination, cancellationToken));
}

public sealed class GetAuthorsHandler(IDbConnectionFactory connectionFactory)
{
    public async Task<PaginatedList<Author>> HandleAsync(Pagination pagination, CancellationToken cancellationToken)
    {
        var parameters = PaginationSqlParameters.Create(pagination);
        await using var connection = await connectionFactory.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            "SELECT COUNT(*) FROM Authors;", transaction: transaction, cancellationToken: cancellationToken));
        var authors = await connection.QueryAsync<Author>(new CommandDefinition(
            "SELECT Id, Name FROM Authors ORDER BY Name, Id LIMIT @PageSize OFFSET @Offset;",
            parameters, transaction, cancellationToken: cancellationToken));
        await transaction.CommitAsync(cancellationToken);
        return new PaginatedList<Author>(authors.AsList(), count, pagination.PageNumber, pagination.PageSize);
    }
}
