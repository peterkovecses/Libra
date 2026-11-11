# Libra

Libra is a library management demo built as a Minimal API. It follows the KISS principle and uses a vertical slice architecture to
organize code around features.

The application uses a file-based SQLite database, created and initialized at
`src/Libra.Api/App_Data/libra.db` on startup. It also demonstrates C# union types
with .NET 11.

Database initialization creates or migrates the schema and inserts demo data by default.
Tests can disable the demo data through `DatabaseInitializationOptions.SeedDemoData`;
schema initialization still runs.

## Tests

Run the integration tests from the repository root:

```sh
dotnet test tests/Libra.Api.IntegrationTests
```

Each test uses a separate in-memory SQLite database, initializes the real schema,
and inserts its own data. These tests do not exercise file-specific SQLite locking.

## Run

Install the .NET 11 SDK, then run from the repository root:

```sh
dotnet run --project src/Libra.Api --launch-profile https
```

The API host also serves the frontend at [https://localhost:7109](https://localhost:7109).
