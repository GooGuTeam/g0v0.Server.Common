# AGENTS.md

Shared library for the g0v0 server v2 series. .NET 10 class library plus an NUnit test project.

## Layout

- `g0v0.Server.Common/` — library (`net10.0`, depends on `Microsoft.AspNetCore.App`).
- `g0v0.Server.Common.Tests/` — NUnit 4 tests; mirrors the library's folder layout.
- `g0v0.Server.Common.sln` — both projects.
- `Directory.Build.props` — applies analyzers, `EnforceCodeStyleInBuild`, and `GenerateDocumentationFile` to every
  project; do not duplicate those flags in `.csproj`.
- `global.json` pins SDK `10.0.0` with `rollForward: latestMinor`.

## Commands

Run from the repo root:

- Build: `dotnet build`
- Test all: `dotnet test`
- Single fixture: `dotnet test --filter "FullyQualifiedName~BeatmapRepositoryTests"`
- Format check (CI runs this): `dotnet format --verify-no-changes`
- Apply formatting fixes: `dotnet format`
- Install InspectCode: `dotnet tool install --global JetBrains.ReSharper.GlobalTools --version 2026.2.1`
- InspectCode: `jb inspectcode g0v0.Server.Common.sln --no-build --no-updates --severity=WARNING --swea --output=artifacts/inspectcode.sarif`
- Strict build like CI: `dotnet build --no-restore --configuration Release /warnaserror`

CI (`.github/workflows/ci.yml`) runs three jobs: build, code style/quality, and tests. The code-style job runs
`dotnet format --verify-no-changes`, a `/warnaserror` build, and warning-level JetBrains InspectCode. It prints every
warning/error and uploads the SARIF report. Format drift or any analyzer/InspectCode warning fails CI.

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
- Shared config classes live here and are consumed by the servers: `GeneralConfig`, `GameConfig`, `ChatConfig`,
  `VerificationConfig` (login verification plus the client IP settings), `StorageConfiguration`, `GeoIPConfig` and
  `RegistrationConfig`. Add a new one here rather than in a server project, so every server reads the same schema; the
  JSON itself still sits in each server's content root.

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

### Multiplayer room entities

Multiplayer room state is persisted through three shared models: `Room`, `Playlist` (table `room_playlists` on MySQL,
`playlists` on PostgreSQL) and `RoomParticipatedUser`. `IRoomRepository` is the single persistence contract used by the
Realtime and Stable servers; both backends implement the full set of room/playlist/participant operations, including
`AnyScoreTokenExistsFor` (guards `RemovePlaylistItem` against deleting items with associated score tokens).

- `Room.Source` identifies the creating server using `ServerIdentify`. Both backends store the enum name in an
  indexed `varchar(32)` column with a `Realtime` default for historical rooms. `CreateRoom` and `GetRoom` require
  an explicit source; `GetRoom` filters by both ID and source. Source is not a mutable room setting. PostgreSQL uses
  the `AddRoomSource` EF migration; legacy MySQL requires the `84cceb652fe5` migration from `osu_lazer_api` before deploy.
- MySQL mappings live in `Database/MySQL/Configurations/RoomConfig.cs` and `PlaylistConfig.cs`. Only fields EF Core
  cannot infer are mapped explicitly: the `rooms`/`room_playlists` table names, native MySQL enum columns for
  `category`/`type`/`queue_mode`/`status`/`win_condition`, `datetime` columns for `DateTimeOffset` values, JSON columns
  for mod lists (empty lists are serialized as `[]`, never NULL — osu-web's `PlaylistModel` rejects null mods), and the
  ignored `max_participants`/`tournament_mode` columns which do not exist in the legacy schema. Everything else is left
  to EF Core conventions — do not duplicate derivable mappings.
- The MySQL schema is the legacy lazer API schema (see `/home/mingxuangame/code/osu_lazer_api/app/database/` and its
  migrations). Semantics follow `app/router/lio.py` (LegacyIO): room creation stores `participant_count = 1` without a
  host participant row; joining deduplicates active participation records (keeping the earliest, refreshing
  `joined_at`); leaving is idempotent, updates `participant_count`, and ends the room (`ends_at`) when no active
  participants remain.
- PostgreSQL uses `Database/PostgreSQL/Configurations/RoomConfig.cs` and `PlaylistConfig.cs` with string-stored enums
  and `jsonb` mod columns, plus a dedicated EF migration for the three tables.
- `Room.Id`, `Playlist.RoomId` and `RoomParticipatedUser.RoomId` are `long` on both backends. Do not widen the
  `users.id`/`user_id` integer columns when adding room relations.

### User registration and the legacy user columns

`POST /users` in the Stable server creates accounts through `IUserRepository.CreateAsync(user, statisticsModes, ...)`,
which writes the account and its per-mode statistics rows in one transaction. Registration is the only writer that
needs every `NOT NULL` column of `lazer_users`, so the model and the MySQL mapping have to stay aligned with the legacy
table (see `/home/mingxuangame/code/osu_lazer_api/migrations/versions/2025-08-10_fdb3822a30ba_init.py` and later
alterations).

- `User` maps only the columns this server reads or writes: identity, username, email, password hash, country, supporter
  and activity flags, `last_visit`, `join_date`, `support_level`, and the two ruleset preferences.
- The ruleset preferences (`playmode`, `g0v0_playmode`) are `int` on both backends. MySQL stores them as native
  `gamemode` enum columns, so `Database/MySQL/Configurations/UserConfig.cs` converts through
  `ConfigurationHelper.ConvertModeToDatabaseValue`/`ConvertDatabaseValueToMode` — the same encoding `Score.Mode` uses.
  Never write a mode ID that is missing from the enum domain; the insert is rejected.
- The remaining legacy `NOT NULL` columns (`priv`, `has_supported`, `max_blocks`, `max_friends`, `comments_count`,
  `post_count`, `is_admin`, `is_gmt`, `is_qat`, `is_bng`) are **shadow properties** of the MySQL mapping.
  `UserConfig.SupplyLegacyColumns` fills them for an insert and marks them unmodified for an update; calling it is
  mandatory in **every** MySQL user write (`CreateAsync` with and without statistics, and `UpdateAsync`), because EF
  would otherwise insert NULL (rejected by MySQL) or reset the stored privilege flags and limits. The insert branch is
  covered by `UserRepositoryTests.CreateAsync_OnTheLegacySchema_*`; the update branch cannot be observed through the
  in-memory provider (it does not round-trip shadow values attached by `DbSet.Update`), so preserve it by review.
  Do not write `null` into any of them: the Python lazer API reads those columns untyped.
- `Email` is nullable in the model and unique-indexed, matching a legacy table whose column is `NOT NULL` but whose
  pre-registration rows have no address. Registration always writes one.
- Registration seeds statistics through `Utils/UserStatisticsDefaults.GetModes`: modes 0-3 always, plus 4/6/7 when
  `GameConfig.EnableRelax` and 5 when `EnableAutopilot`. Keep that list in step with the modes the score pipeline can
  persist.
- Password hashing lives in `Utils/UserPasswordHasher`: osu! stores a bcrypt hash of the **hex MD5 digest** the
  client sends, never of the plain text. `HashPassword`/`HashDigest` write it, `VerifyDigest` checks the digest a login
  carries and treats a malformed stored hash as a mismatch. Do not call `BCrypt.Net.BCrypt` directly from a server
  project; a plain-text hash silently makes every login of that account fail.
- Username, email and password acceptance live in `Utils/UserCredentialValidator`; the servers call it instead of
  duplicating the rules. The password policy is a strength check (8-32 characters, more than three distinct characters,
  at least two of lowercase/uppercase/digit/symbol), deliberately replacing bancho.py's disallowed-word list;
  `RegistrationConfig.DisallowedUsernames` defaults to the lazer API's `banned_name` list. The violation messages are
  shown to the player verbatim, so keep their wording.

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
`AddBackgroundTaskRunner`, `AddOAuthAuthentication` and `AddClientIp`. Most of them require `ConfigurationManager` to
already be registered; `AddStorage` additionally needs `IPathProvider` for local storage. Register
`ConfigurationManager` first.

## Client IP resolution

`ClientIp/` recovers the client address behind reverse proxies and CDNs. It is modelled on `osu_lazer_api`'s
`get_client_ip` and follows the same trust rules:

- `ClientIpResolver` reads the **connection peer** from `HttpContext.Connection`. The host must not run
  `UseForwardedHeaders` (or an equivalent rewrite): once the peer carries the forwarded value, the resolver cannot
  verify that the headers came from a proxy.
- Forwarding headers are used only when the peer is inside `VerificationConfig.TrustedProxies`. `X-Forwarded-For` is
  walked from right to left and the first untrusted hop wins, so entries pre-filled by the client are unreachable.
  `X-Real-IP` is a fallback only when there is no `X-Forwarded-For`.
- `CF-Connecting-IP` is authoritative only when the last hop is a published Cloudflare edge range.
- `TrustedProxySourceRefreshService` loads `VerificationConfig.TrustedProxySources` at startup and on an interval. A
  failed or empty refresh keeps the previous per-URL snapshot, and catch-all ranges (`/0`) are rejected so a publisher
  cannot make every client trusted.
- Addresses are normalized (IPv4-mapped IPv6 becomes IPv4) and returned as strings; `127.0.0.1` is the fallback when
  the peer is missing.

Servers should inject `IClientIpResolver` and call `Resolve(context)` instead of reading
`Connection.RemoteIpAddress`; `ClientIp/ClientIpResolverTests`, `ProxyRangeParserTests` and
`TrustedProxySourceRefreshServiceTests` cover the trust, parsing and refresh branches.

## Tests

- NUnit 4 + `Microsoft.EntityFrameworkCore.InMemory`. No real database, Redis, or S3 is needed.
- Convention: one fixture per repository, parameterised over both backends with
  `[TestFixture(RepositoryBackend.MySql)]` + `[TestFixture(RepositoryBackend.PostgreSql)]` (see
  `Database/Repository/RepositoryTestContexts.cs`). The fixture holds the interface type and builds the
  backend implementation in `SetUp`; entities are reached through `_context.Set<T>()`. When adding a new
  repository, add one fixture — never a per-backend copy.
- The test project sets `<NoWarn>SA0001;SA1600;SA1602;CS1591</NoWarn>` — XML doc warnings are intentionally suppressed in tests, do
  not "fix" them by adding doc comments everywhere.

### Test quality bar

A test earns its place only by exercising a **branch of production code**. Write the assertion so that deleting or
breaking the production line under test fails the test. `Database/Repository/BeatmapRepositoryTests.cs` is the
reference shape: a few tests, each driving one real behaviour.

Do not add tests that only restate a declaration. The following were removed as part of this cleanup and must not
come back:

- **Constants and constructor echoes** — `new ConfigFileAttribute("x")` then `FileName == "x"`, or reading a
  `[AttributeUsage]` back off the attribute class. The compiler already enforces these.
- **Attribute-presence reflection** — "the attribute I just applied can be read back" /
  `XxxAttributeTests` that only inspect `AttributeTargets`. `ReloadableAttribute`'s real behaviour
  (reload touches only marked properties) is covered by `ConfigManagerTests.Reload_*`.
- **Marker-interface assertions** — `Assert.That(_repository, Is.InstanceOf<IMySqlRepository>())`. The contract
  that matters is the DI wiring, covered by `ServiceCollectionExtensionTests.AddRepositories_*`.
- **Schema-shape assertions** — reading `GetTableName()`/`GetColumnName()`/`GetColumnType()` off an entity type
  to assert the MySQL/PostgreSQL mapping. That tests "which database are we", not the repository.
- **Tautologies** — `Is.Not.Null` on a value a neighbouring assertion already proves `Is.Empty`, or
  `Assert.DoesNotThrow(() => new X())` duplicating a test that asserts the actual result.
- **Tests of test doubles** — exercising a `Fake*`/`Stub*` helper in `*.Tests` instead of production code.
- **Permanently ignored tests** — `[Ignore]` with a reason that will never change is dead weight; delete it rather
  than leaving it skipped forever.

Avoid duplicates:

- One behaviour, one test. If two tests share a body verbatim, keep the one with the more informative name/comment.
- No per-backend copies. Both backends are covered by the single parameterised fixture; where a backend genuinely
  differs (MySQL ignores `tournament_mode` because the legacy schema has no such column; MySQL notification names
  are stored `UPPER_SNAKE_CASE`), branch inside the test with an explicit `backend == RepositoryBackend.MySql`
  and a comment saying why.
- No boundary-value sweeps (`int.MinValue`/`-1`/`0`/`1`/`int.MaxValue`) when the implementation funnels them
  through one code path; one representative case is enough.
- A test name must match its assertion. `..._ShouldSucceedForPartialMatch` that asserts failure is a bug in the
  test, not a comment.