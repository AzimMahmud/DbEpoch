# Changelog

All notable changes to the DbShift project will be documented in this file.

## [Unreleased]

## [2.0.0] — 2026-08-22

### Added

- **Real-database integration tests** — 54 new tests in `tests/DbShift.Engine.Tests/Integration/` run the `RelationalMigrationTracker`/`RelationalMigrationLockManager`/`RelationalAuditLogger` contract against live PostgreSQL, MySQL, and SQL Server containers via Testcontainers, closing the gap where only SQLite had real-database coverage. Tagged `Category=Integration`; requires Docker and is excluded from the default `dotnet test` run.
- **CodeQL security scanning** — `.github/workflows/codeql.yml` runs `security-and-quality` analysis on push/PR to `main` and weekly on a schedule.
- **README: Updating / Uninstalling sections** — documents re-running the installer to upgrade in place (verified: no duplicate PATH entry, clean binary overwrite) and manual removal steps for Linux/macOS/Windows, since no automated uninstaller exists yet.

### Changed

- **`ci.yml`** — added a dedicated `integration-tests` job (ubuntu-latest) for the new Testcontainers-backed suite; the cross-OS `build` matrix now excludes `Category=Integration` from its `dotnet test` run; added an explicit least-privilege `permissions: contents: read` block.

### Breaking

- **Dropped net6.0/net8.0 targets — net10.0 only.** `Directory.Build.props` and every workflow now target `net10.0` exclusively (previously multi-targeted `net6.0;net8.0;net10.0` for broad `dotnet tool install` compatibility). Building from source, running as a `dotnet tool`, and CI all require the **.NET 10 SDK/runtime**. `global.json` now pins `10.0.100` with `latestFeature` roll-forward (was `6.0.100`/`latestMajor`). The PolySharp polyfill dependency (needed only for net6.0) was removed. Self-contained release binaries are unaffected — they already bundled `net10.0`.

### Fixed

- **Interactive banner showed .NET runtime/OS info** — `dbshift new`'s interactive wizard printed the .NET `FrameworkDescription` and `OSDescription` on every run via `ConsoleHelper.PrintBanner()`. Removed; the banner now shows only branding and supported providers.
- **Concurrent deploys possible after a lock lease expired mid-deploy** — `MigrationExecutor.DeployAsync` renewed the distributed lock before each batch but discarded the `bool` result; if the lease had already expired and been stolen by another process, the deploy kept executing and recording migrations with no lock held at all, letting two `dbshift migrate` runs race against the same environment. Now stops immediately and reports the lock loss instead. Covered by `DeployFlowTests.Deploy_LockLostBetweenBatches_StopsAndReportsFailure`.
- **Non-`DbException` faults crashed the whole deploy instead of failing one migration** — `RelationalMigrationExecutor.ExecuteAsync` only caught `DbException`, so e.g. cancellation-adjacent provider faults propagated uncaught out of `DeployAsync`, skipping the structured `DeployResult` entirely. Broadened to catch any non-cancellation exception; genuine cancellation (`OperationCanceledException`) still propagates so it isn't misrecorded as a migration failure.
- **`install.sh` always exited 1 despite a successful install** — `workdir` was declared `local` inside `download_release`, but the `trap 'rm -rf "$workdir"' EXIT` fires after the function returns, so under `set -u` the trap failed with `workdir: unbound variable` on every run. `workdir` is no longer `local`.
- **`install.ps1` printed no status text** — `Info`/`Ok`/`Warn`/`Err` referenced `$_` without declaring a parameter, so every call like `Info "Detected: $platform"` printed just the icon with nothing after it (the argument was never bound to `$_` outside a pipeline). All four now take an explicit `$Message` parameter.

## [1.1.0] — 2026-07-30

### Added

- **`--verbose` flag** — `dbshift migrate/rollback/status/create/repair --verbose` shows `Information`-level log messages (script execution, checksums, lock acquisition). Propagated via `GlobalSettings.Verbose` → `CliHostOptions.Verbose` → `SpectreLogger<T>`.
- **`strictAudit` mode** — `MigrationExecutor` constructor accepts a `strictAudit` parameter; when `true`, `AuditSafe()` re-throws audit failures instead of silently swallowing them.
- **`InvalidateCache()`** — `MigrationExecutor.InvalidateCache()` forces re-reading migration scripts from disk on the next operation.
- **`ParseAsync(string, CancellationToken)`** — `ScriptParser.ParseAsync(filePath, ct)` for async file I/O with cancellation support.
- **`DiscoverAllCoreAsync(CancellationToken)`** — `MigrationExecutor.DiscoverAllCoreAsync(ct)` for async script discovery.
- **`ProviderSqlHelper`** — extracted 7 provider-specific SQL template methods from `NewCommand.cs` (~467→~372 lines), reducing duplication.
- **SQLite integration tests** — 16 new tests in `SqliteRelationalTests.cs` covering `RelationalMigrationTracker`, `RelationalMigrationLockManager`, and `RelationalAuditLogger` against a real SQLite file database.
- **Enhanced test doubles** — `FakeScriptExecutor` now supports configurable delay, `failOnSqlContaining` trigger, `ExecutionCount` and `ExecutedSql` tracking. `FailingScriptExecutor` tracks `ExecutionCount`.

### Changed

- **`OrderKey` doc comment** — added explanation of zero-padded version ordering safety.
- **Self-contained binary framework** — release builds now target `net10.0` (was `net8.0`).

### Fixed

- **SQLite lock manager** — `RelationalMigrationLockManager.ReleaseAsync` and `RenewAsync` were missing the `@true` parameter binding, causing `SqliteException` at runtime. Only SQLite was affected; other providers ignored the unused parameter.

## [1.0.0] — 2026-06-17

### Added

#### Professional tooling
- **CI workflow** (`.github/workflows/ci.yml`) — build, test, code coverage on ubuntu/windows/macos for every push and PR. Produces NuGet packages on main pushes.
- **Release workflow** (`.github/workflows/release.yml`) — triggered by `v*` tags. Builds self-contained binaries for 5 platforms (win-x64, linux-x64, linux-arm64, osx-x64, osx-arm64), packages as `.zip`/`.tar.gz`, publishes to NuGet, creates a GitHub Release with release notes and all artifacts.
- **Dependabot** (`.github/dependabot.yml`) — weekly dependency updates for NuGet and GitHub Actions.
- **Issue templates** — structured bug report and feature request forms via `.github/ISSUE_TEMPLATE/`.
- **PR template** (`.github/pull_request_template.md`) — checklist for contributors covering build, tests, changelog, and docs.
- **EditorConfig** (`.editorconfig`) — consistent indentation and line endings across all file types.
- **CONTRIBUTING.md** — development setup, code conventions, commit style, and PR process.
- **CODE_OF_CONDUCT.md** — standard Contributor Covenant v2.1.
- **SECURITY.md** — vulnerability reporting process and supported versions.
- **Install scripts** — `install.sh` (Linux/macOS curl-bash) and `install.ps1` (Windows iwr-iex). Auto-detect platform, download from GitHub Releases, install to PATH.
- **Build scripts** — `publish.sh` (Linux/macOS) and `publish.ps1` (Windows) for building self-contained binaries locally.
- `dist/` added to `.gitignore`.

#### CLI — project scaffolding
- `dbshift new` — interactive project scaffold. Run without flags to be prompted for project name, database provider, and output directory. Creates the full directory tree, config files, per-environment settings, example migrations (provider-specific SQL), templates, `.gitignore`, and a GitHub Actions CI pipeline.
- `scaffold` / `init-project` aliases for `new`.

#### CLI — command-specific help
- `dbshift <command> --help` now shows options specific to that command, plus the global options (without duplication).

#### CLI — onboarding screen
- `dbshift` (no arguments) now shows a "Quick start" panel with the three most common commands before the full help table.

#### Multi-database provider support
- `IDatabaseProvider` interface with four implementations:
  - `PostgreSqlProvider` — PostgreSQL 12+ (Npgsql)
  - `SqlServerProvider` — SQL Server 2016+ (Microsoft.Data.SqlClient)
  - `MySqlProvider` — MySQL 8+ / MariaDB 10.5+ (MySqlConnector)
  - `SqliteProvider` — SQLite 3 (Microsoft.Data.Sqlite)
- `DatabaseProviderFactory` — resolves the correct provider by string alias.
- Provider override via `--provider` CLI flag or `migration.json → database.provider`.

#### Provider-agnostic infrastructure
- `RelationalMigrationTracker` — DELETE+INSERT upsert pattern (works on all four engines).
- `RelationalMigrationLockManager` — C# date math for lock expiry (no provider-specific SQL).
- `RelationalMigrationExecutor` — transaction-bound SQL execution via `System.Data.Common`.
- `RelationalAuditLogger` — parameterized INSERT for audit trail.
- `ConfigEnvironmentProvider` — environment config from JSON files.

#### Professional project assets
- `README.md` — comprehensive GitHub open-source README with badges, Quick Start, command table, script conventions, config reference, architecture diagram, multi-database explanation, and installation guide.
- `LICENSE` — MIT license.
- `CHANGELOG.md` — this file.
- `Directory.Build.props` — package metadata (authors, copyright, license).
- `docs/USAGE.md` — complete end-to-end usage guide covering installation, setup, migrations, rollbacks, multi-database, CI/CD, approval gates, deployment windows, and troubleshooting.

### Changed

#### Renamed from "DatabaseMigrationPlatform" (dbpilot) to "DbShift"
- Solution: `DbShift.sln`
- Source projects: `DbShift.Core`, `DbShift.Engine`, `DbShift.Infrastructure`, `DbShift.CLI`
- Test project: `DbShift.Engine.Tests`
- Tool command: `dbshift` (was `migration`)
- Package: `DbShift` (was `dbpilot`)
- All namespaces, directories, and project references updated.

#### Configuration
- `migration.json` — added `database.provider` field.
- Environment JSON files — added to `Database/Config/environments/`.
- Connection string resolution: `--connection-string` > `DB_CONNECTION_STRING` env var > config file.

### Removed

- All PostgreSQL-specific infrastructure classes:
  - `PostgresMigrationTracker.cs`
  - `PostgresMigrationLockManager.cs`
  - `PostgresMigrationExecutor.cs`
  - `PostgresAuditLogger.cs`
  - `PostgresEnvironmentProvider.cs`
  - `TrackingSchema.cs`
- Empty directories: `Engine/Rollback/`, `Engine/Tracking/`, `Engine/Validation/`, `Infrastructure/Git/`, `CLI/Output/`, `docs/` (old).
- Old `README.md` and `LICENSE` files (replaced).

### Fixed

- `dbshift <command> --help` now correctly shows command-specific help (was showing global help for all commands).
- Help output no longer duplicates global options when showing command-specific help.
- JSON output is now emitted cleanly without decorative UI text.
- Migration template files generate correct `{{NAME}}` placeholders (used by `dbshift create`).

### Security

- No connection strings, secrets, or tokens are stored in the repository.
- All connection strings use environment variable expansion (`${VAR}`).
