namespace Libra.Api.Infrastructure.Persistence;

internal static class SqliteSchema
{
    public const string Create = """
                                 CREATE TABLE IF NOT EXISTS Books (
                                     Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                     Title TEXT NOT NULL
                                 );

                                 CREATE TABLE IF NOT EXISTS Authors (
                                     Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                     Name TEXT NOT NULL
                                 );

                                 CREATE TABLE IF NOT EXISTS BookAuthors (
                                     BookId INTEGER NOT NULL,
                                     AuthorId INTEGER NOT NULL,

                                     PRIMARY KEY (BookId, AuthorId),

                                     FOREIGN KEY (BookId)
                                         REFERENCES Books(Id)
                                         ON DELETE CASCADE,

                                     FOREIGN KEY (AuthorId)
                                         REFERENCES Authors(Id)
                                         ON DELETE CASCADE
                                 );

                                 CREATE TABLE IF NOT EXISTS BookCopies (
                                     Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                     BookId INTEGER NOT NULL,

                                     FOREIGN KEY (BookId)
                                         REFERENCES Books(Id)
                                         ON DELETE CASCADE
                                 );

                                 CREATE TABLE IF NOT EXISTS Members (
                                     Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                     FirstName TEXT NOT NULL,
                                     LastName TEXT NOT NULL,
                                     Email TEXT NOT NULL UNIQUE
                                 );

                                 CREATE TABLE IF NOT EXISTS Loans (
                                     Id INTEGER PRIMARY KEY AUTOINCREMENT,
                                     BookCopyId INTEGER NOT NULL,
                                     MemberId INTEGER NOT NULL,
                                     BorrowedAt TEXT NOT NULL,
                                     DueDate TEXT NOT NULL,
                                     ReturnedAt TEXT,

                                     FOREIGN KEY (BookCopyId)
                                         REFERENCES BookCopies(Id),

                                     FOREIGN KEY (MemberId)
                                         REFERENCES Members(Id)
                                 );

                                 CREATE INDEX IF NOT EXISTS IX_Books_Title_Id
                                     ON Books(Title, Id);

                                 CREATE INDEX IF NOT EXISTS IX_BookCopies_BookId
                                     ON BookCopies(BookId);

                                 CREATE INDEX IF NOT EXISTS IX_Loans_BookCopyId
                                     ON Loans(BookCopyId);

                                 CREATE INDEX IF NOT EXISTS IX_Loans_MemberId
                                     ON Loans(MemberId);
                                 """;
}