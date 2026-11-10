namespace Libra.Api.Infrastructure.Persistence;

internal static class SqliteSeed
{
    public const string Insert = """
                                 INSERT OR IGNORE INTO Authors (Id, Name)
                                 VALUES
                                     (1, 'George Orwell'),
                                     (2, 'J. R. R. Tolkien'),
                                     (3, 'Frank Herbert');

                                 INSERT OR IGNORE INTO Books (Id, Title)
                                 VALUES
                                     (1, '1984'),
                                     (2, 'Animal Farm'),
                                     (3, 'The Hobbit'),
                                     (4, 'Dune');

                                 INSERT OR IGNORE INTO BookAuthors (BookId, AuthorId)
                                 VALUES
                                     (1, 1),
                                     (2, 1),
                                     (3, 2),
                                     (4, 3);

                                 INSERT OR IGNORE INTO Members
                                     (Id, FirstName, LastName, Email)
                                 VALUES
                                     (1, 'John', 'Smith', 'john@example.com'),
                                     (2, 'Jane', 'Doe', 'jane@example.com');
                                 """;
}