namespace Libra.Api.Domain.Models;

public class Book
{
    public int Id { get; init; }
    public required string Title { get; init; }
    public string Version { get; init; } = "";
    public List<Author> Authors { get; init; } = [];
}