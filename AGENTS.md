# AGENTS.md

Shared library for the g0v0 server v2 series. .NET 8 class library plus an NUnit test project.

## Layout

- `g0v0.Server.Common/` — library (`net8.0`, depends on `Microsoft.AspNetCore.App`).
- `g0v0.Server.Common.Tests/` — NUnit 4 tests; mirrors the library's folder layout.
- `g0v0.Server.Common.sln` — both projects.
- `Directory.Build.props` — applies analyzers, `EnforceCodeStyleInBuild`, and `GenerateDocumentationFile` to every
  project; do not duplicate those flags in `.csproj`.
- `global.json` pins SDK `8.0.0` with `rollForward: latestMinor`.

## Commands

Run from the repo root:

- Build: `dotnet build`
- Test all: `dotnet test`
- Single fixture: `dotnet test --filter "FullyQualifiedName~MySqlBeatmapRepositoryTests"`
- Format check (CI runs this): `dotnet format --verify-no-changes`
- Apply formatting fixes: `dotnet format`
- Strict build like CI: `dotnet build --configuration Release /warnaserror`

CI (`.github/workflows/ci.yml`) runs three jobs: build, `dotnet format --verify-no-changes` + `/warnaserror` build, and
tests. Format drift or any analyzer warning fails CI even though local builds do not — run `dotnet format` before
pushing.

## Code style (non-default)

Enforced by `.editorconfig`, `.globalconfig`, and `stylecop.json`:

- Every `.cs` file starts with the header
  `// Copyright (c) GooGuTeam. License under MIT License. See LICENSE in the project root for license information.`
  followed by a blank line.
- `using` directives go **outside** the namespace, `System.*` first, no blank lines between groups.
- File-scoped namespaces; LF line endings; 4-space indent; no final newline.
- Regions are used intentionally for grouping (SA1124 disabled). Private fields use `_underscoreCamelCase` (SA1309
  disabled).
- XML doc comments are required on public/exposed members (`documentExposedElements: true`). `<inheritdoc/>` is
  acceptable. Internal members are not required to be documented.
- `IDE0005` (unused usings) and `IDE0055` (formatting) are warnings → errors in CI.

## Configuration system

`Configuration/ConfigurationManager.cs` loads JSON from `{basePath}/config/{file}.json`:

- File name defaults to the snake_case of the class name; override with `[ConfigurationFile("name")]`.
- Uses **Newtonsoft.Json**, not `System.Text.Json`. Match its attribute conventions when adding config classes.
- `Reload<T>()` only copies properties marked `[Reloadable]`; new properties default to "set once at startup".
- `basePath` typically comes from an injected `IPathProvider`.

## Database

Dual-backend EF Core: MySQL (legacy v1 schema) and PostgreSQL (v2). Selection is runtime via
`GeneralConfiguration.UseLegacyDatabase`.

- Models live in `Database/Models/` and are shared. Per-backend code lives in `Database/MySQL/` and
  `Database/PostgreSQL/`.
- Both contexts use `UseSnakeCaseNamingConvention()` (EFCore.NamingConventions). Column/table names are snake_case in
  the DB regardless of C# casing.
- Backend-specific configurations are auto-applied by namespace:
  `g0v0.Server.Common.Database.{MySQL,PostgreSQL}.Configurations`. To register a new entity configuration just place it
  in that namespace.
- `AddRepositories(useLegacyDatabase)` in `Extensions/ServiceCollectionExtension.cs` uses **reflection** to wire
  `IFooRepository` → `FooRepository`. Contracts:
    - Interface must end with `Repository` and start with `I`. Implementation class name must equal the interface name
      without the leading `I`.
    - Implementation must implement the marker interface `IMySqlRepository` or `IPostgreSqlRepository` so the correct
      backend is selected.
    - If a new repository is not being resolved at runtime, check these two rules first.

### Migrations (PostgreSQL only)

Only PostgreSQL has a design-time factory (`PostgreSqlDbContextFactory`). To run EF tools you must provide a connection
string:

```
dotnet ef migrations add <Name> --project g0v0.Server.Common --startup-project g0v0.Server.Common -- --conn=<connection-string>
# or set DB_CONN env var
```

MySQL has no design-time factory; do not run `dotnet ef` against the MySQL context.

## DI helpers

`Extensions/ServiceCollectionExtension.cs` exposes `AddRepositories`, `AddRedis`, `AddStorage`,
`AddBackgroundTaskRunner`, `AddOAuthAuthentication`. Most of them require `ConfigurationManager` to already be
registered; `AddStorage` additionally needs `IPathProvider` for local storage. Register `ConfigurationManager` first.

## Tests

- NUnit 4 + `Microsoft.EntityFrameworkCore.InMemory`. No real database, Redis, or S3 is needed.
- Convention: each repository has paired `MySql*` and `PostgreSql*` fixtures using
  `UseInMemoryDatabase(Guid.NewGuid().ToString())` for isolation. When adding a new repository, add both.
- The test project sets `<NoWarn>SA0001;CS1591</NoWarn>` — XML doc warnings are intentionally suppressed in tests, do
  not "fix" them by adding doc comments everywhere.
