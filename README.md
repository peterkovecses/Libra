# Libra

Libra is a library management demo built as a Minimal API. It follows the KISS principle and uses a vertical slice architecture to
organize code around features.

The application uses a file-based SQLite database, created and initialized at
`Libra.Api/App_Data/libra.db` on startup. It also demonstrates C# union types
with .NET 11.

## Run

Install the .NET 11 SDK, then run from the repository root:

```sh
dotnet run --project Libra.Api --launch-profile https
```

The API host also serves the frontend at [https://localhost:7109](https://localhost:7109).
