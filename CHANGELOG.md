# Changelog

All notable changes to the DbEpoch project will be documented in this file.

## [Unreleased]

## [2.1.0] - 2026-08-24

### Added

- **Windows ARM64 build** - DbEpoch-windows-arm64.zip now included in releases.
- **Alpine Linux (musl) build** - DbEpoch-linux-musl-x64.tar.gz now included in releases for Alpine and musl-based distros.
- **macOS Gatekeeper auto-fix** - install script now automatically removes the quarantine attribute so DbEpoch runs immediately after install.
- **Windows Git Bash/WSL detection** - install script detects Windows environments and directs users to install.ps1 instead of failing with "Unsupported OS".
- **New logo assets** - dark/light variants at 64x64, 128x128, 256x256, and 512x512 sizes in dbepoch_logos/ folder. Updated root logo.png, docs favicon, and NuGet package icon.

### Fixed

- **Version mismatch** - DbEpoch --version now reads the version from assembly metadata instead of a hardcoded string, ensuring it always matches the installed version.
- **Mojibake in docs** - fixed broken UTF-8 characters (em dashes, middle dots, box-drawing chars) across all documentation files.

### Changed

- **Docs nav logo** - icon + gradient "DbEpoch" text, dark/light theme swap.
- **Browser favicon** - uses square icon instead of full logo.
- **NuGet package icon** - updated to 256px light variant with `<RepositoryType>git</RepositoryType>` for repo linking.
## [2.0.1] â€” 2026-08-23

### Added

- **VitePress documentation site** â€” full documentation site deployed to GitHub Pages via GitHub Actions, covering installation, all commands, configuration, multi-database setup, script conventions, tracking tables, architecture, and CI/CD integration.
- **Custom light/dark theme** â€” branded VitePress theme with teal-blue gradient palette, styled nav, feature cards, and sidebar.
- **GitHub Pages deployment workflow** â€” `.github/workflows/docs.yml` auto-deploys the docs site on push to `main`.
- **`gh-pages` branch** â€” manual deployment option via `npm run docs:deploy`.

### Changed

- **README rewritten** â€” condensed from 608 to 150 lines with a professional structure: centered header, feature highlights, command table with docs links, and clear installation instructions.
- **Nav design improved** â€” icon-only logo in nav bar, styled hover/active states, border divider.

### Fixed

- **Favicon** â€” now uses the square icon SVG instead of the full-width logo, displaying correctly in browser tabs.

## [2.0.0] â€” 2026-08-22

### Added

- **Real-database integration tests** â€” 54 new tests in `tests/DbEpoch.Engine.Tests/Integration/` run the `RelationalMigrationTracker`/`RelationalMigrationLockManager`/`RelationalAuditLogger` contract against live PostgreSQL, MySQL, and SQL Server containers via Testcontainers, closing the gap where only SQLite had real-database coverage. Tagged `Category=Integration`; requires Docker and is excluded from the default `dotnet test` run.
- **CodeQL security scanning** â€” `.github/workflows/codeql.yml` runs `security-and-quality` analysis on push/PR to `main` and weekly on a schedule.
- **`install.sh --uninstall` / `install.ps1 -Uninstall`** â€” both installer scripts now double as uninstallers: remove the binary and strip the PATH entry they added (`# Added by DbEpoch installer` block in `.zshrc`/`.bashrc`, or the matching entry in the Windows user `PATH`). Idempotent â€” safe to run when nothing is installed. `install.sh` also accepts `UNINSTALL=1` as an env-var alternative to the `--uninstall`/`-u` flag, consistent with its existing `REPO`/`VERSION`/`INSTALL_DIR` overrides.
- **README: Updating / Uninstalling sections** â€” documents re-running the installer to upgrade in place (verified: no duplicate PATH entry, clean binary overwrite), the new automated uninstall commands, a `which -a DbEpoch` / `where.exe DbEpoch` tip for detecting duplicate installs across different directories, and how to handle a system-wide (root-owned) install.
- **Uninstall now fails gracefully on permission-denied** â€” `install.sh --uninstall` against a root-owned/system-wide install previously crashed with a raw `rm: Permission denied`; it now reports a clear message with the exact `sudo rm ...` command to run. `install.ps1 -Uninstall` gets the equivalent fix (points to running an elevated PowerShell).
- **NuGet package icon** â€” `.github/assets/icon.png` (128Ã—128, rendered from the existing `icon.svg`), wired up via `<PackageIcon>` in `DbEpoch.CLI.csproj`. Package ID `DbEpoch` confirmed available on NuGet.org. Verified end-to-end: packed, installed as a real global tool from the local `.nupkg` (`dotnet tool install --global DbEpoch --add-source ...`), ran `DbEpoch --version`/`--help` successfully.

### Changed

- **`ci.yml`** â€” added a dedicated `integration-tests` job (ubuntu-latest) for the new Testcontainers-backed suite; the cross-OS `build` matrix now excludes `Category=Integration` from its `dotnet test` run; added an explicit least-privilege `permissions: contents: read` block.

### Breaking

- **Dropped net6.0/net8.0 targets â€” net10.0 only.** `Directory.Build.props` and every workflow now target `net10.0` exclusively (previously multi-targeted `net6.0;net8.0;net10.0` for broad `dotnet tool install` compatibility). Building from source, running as a `dotnet tool`, and CI all require the **.NET 10 SDK/runtime**. `global.json` now pins `10.0.100` with `latestFeature` roll-forward (was `6.0.100`/`latestMajor`). The PolySharp polyfill dependency (needed only for net6.0) was removed. Self-contained release binaries are unaffected â€” they already bundled `net10.0`.

### Fixed

- **Interactive banner showed .NET runtime/OS info** â€” `DbEpoch new`'s interactive wizard printed the .NET `FrameworkDescription` and `OSDescription` on every run via `ConsoleHelper.PrintBanner()`. Removed; the banner now shows only branding and supported providers.
- **Concurrent deploys possible after a lock lease expired mid-deploy** â€” `MigrationExecutor.DeployAsync` renewed the distributed lock before each batch but discarded the `bool` result; if the lease had already expired and been stolen by another process, the deploy kept executing and recording migrations with no lock held at all, letting two `DbEpoch migrate` runs race against the same environment. Now stops immediately and reports the lock loss instead. Covered by `DeployFlowTests.Deploy_LockLostBetweenBatches_StopsAndReportsFailure`.
- **Non-`DbException` faults crashed the whole deploy instead of failing one migration** â€” `RelationalMigrationExecutor.ExecuteAsync` only caught `DbException`, so e.g. cancellation-adjacent provider faults propagated uncaught out of `DeployAsync`, skipping the structured `DeployResult` entirely. Broadened to catch any non-cancellation exception; genuine cancellation (`OperationCanceledException`) still propagates so it isn't misrecorded as a migration failure.
- **`install.sh` always exited 1 despite a successful install** â€” `workdir` was declared `local` inside `download_release`, but the `trap 'rm -rf "$workdir"' EXIT` fires after the function returns, so under `set -u` the trap failed with `workdir: unbound variable` on every run. `workdir` is no longer `local`.
- **`install.ps1` printed no status text** â€” `Info`/`Ok`/`Warn`/`Err` referenced `$_` without declaring a parameter, so every call like `Info "Detected: $platform"` printed just the icon with nothing after it (the argument was never bound to `$_` outside a pipeline). All four now take an explicit `$Message` parameter.
- **README links/images that would break on the NuGet.org package page** â€” the embedded README is rendered standalone there (nothing else from the repo ships in the package), so relative links (`docs/USAGE.md`, `LICENSE`) and the relative logo `<img src=".github/assets/logo.svg">` all resolved to nothing. Converted to absolute GitHub URLs. Also fixed a stale `.NET 6.0 | 8.0 | 10.0` badge (left over from the net10-only migration) with a dead `()` link.

## [1.1.0] â€” 2026-07-30

### Added

- **`--verbose` flag** â€” `DbEpoch migrate/rollback/status/create/repair --verbose` shows `Information`-level log messages (script execution, checksums, lock acquisition). Propagated via `GlobalSettings.Verbose` â†’ `CliHostOptions.Verbose` â†’ `SpectreLogger<T>`.
- **`strictAudit` mode** â€” `MigrationExecutor` constructor accepts a `strictAudit` parameter; when `true`, `AuditSafe()` re-throws audit failures instead of silently swallowing them.
- **`InvalidateCache()`** â€” `MigrationExecutor.InvalidateCache()` forces re-reading migration scripts from disk on the next operation.
- **`ParseAsync(string, CancellationToken)`** â€” `ScriptParser.ParseAsync(filePath, ct)` for async file I/O with cancellation support.
- **`DiscoverAllCoreAsync(CancellationToken)`** â€” `MigrationExecutor.DiscoverAllCoreAsync(ct)` for async script discovery.
- **`ProviderSqlHelper`** â€” extracted 7 provider-specific SQL template methods from `NewCommand.cs` (~467â†’~372 lines), reducing duplication.
- **SQLite integration tests** â€” 16 new tests in `SqliteRelationalTests.cs` covering `RelationalMigrationTracker`, `RelationalMigrationLockManager`, and `RelationalAuditLogger` against a real SQLite file database.
- **Enhanced test doubles** â€” `FakeScriptExecutor` now supports configurable delay, `failOnSqlContaining` trigger, `ExecutionCount` and `ExecutedSql` tracking. `FailingScriptExecutor` tracks `ExecutionCount`.

### Changed

- **`OrderKey` doc comment** â€” added explanation of zero-padded version ordering safety.
- **Self-contained binary framework** â€” release builds now target `net10.0` (was `net8.0`).

### Fixed

- **SQLite lock manager** â€” `RelationalMigrationLockManager.ReleaseAsync` and `RenewAsync` were missing the `@true` parameter binding, causing `SqliteException` at runtime. Only SQLite was affected; other providers ignored the unused parameter.

## [1.0.0] â€” 2026-06-17

### Added

#### Professional tooling
- **CI workflow** (`.github/workflows/ci.yml`) â€” build, test, code coverage on ubuntu/windows/macos for every push and PR. Produces NuGet packages on main pushes.
- **Release workflow** (`.github/workflows/release.yml`) â€” triggered by `v*` tags. Builds self-contained binaries for 5 platforms (win-x64, linux-x64, linux-arm64, osx-x64, osx-arm64), packages as `.zip`/`.tar.gz`, publishes to NuGet, creates a GitHub Release with release notes and all artifacts.
- **Dependabot** (`.github/dependabot.yml`) â€” weekly dependency updates for NuGet and GitHub Actions.
- **Issue templates** â€” structured bug report and feature request forms via `.github/ISSUE_TEMPLATE/`.
- **PR template** (`.github/pull_request_template.md`) â€” checklist for contributors covering build, tests, changelog, and docs.
- **EditorConfig** (`.editorconfig`) â€” consistent indentation and line endings across all file types.
- **CONTRIBUTING.md** â€” development setup, code conventions, commit style, and PR process.
- **CODE_OF_CONDUCT.md** â€” standard Contributor Covenant v2.1.
- **SECURITY.md** â€” vulnerability reporting process and supported versions.
- **Install scripts** â€” `install.sh` (Linux/macOS curl-bash) and `install.ps1` (Windows iwr-iex). Auto-detect platform, download from GitHub Releases, install to PATH.
- **Build scripts** â€” `publish.sh` (Linux/macOS) and `publish.ps1` (Windows) for building self-contained binaries locally.
- `dist/` added to `.gitignore`.

#### CLI â€” project scaffolding
- `DbEpoch new` â€” interactive project scaffold. Run without flags to be prompted for project name, database provider, and output directory. Creates the full directory tree, config files, per-environment settings, example migrations (provider-specific SQL), templates, `.gitignore`, and a GitHub Actions CI pipeline.
- `scaffold` / `init-project` aliases for `new`.

#### CLI â€” command-specific help
- `DbEpoch <command> --help` now shows options specific to that command, plus the global options (without duplication).

#### CLI â€” onboarding screen
- `DbEpoch` (no arguments) now shows a "Quick start" panel with the three most common commands before the full help table.

#### Multi-database provider support
- `IDatabaseProvider` interface with four implementations:
  - `PostgreSqlProvider` â€” PostgreSQL 12+ (Npgsql)
  - `SqlServerProvider` â€” SQL Server 2016+ (Microsoft.Data.SqlClient)
  - `MySqlProvider` â€” MySQL 8+ / MariaDB 10.5+ (MySqlConnector)
  - `SqliteProvider` â€” SQLite 3 (Microsoft.Data.Sqlite)
- `DatabaseProviderFactory` â€” resolves the correct provider by string alias.
- Provider override via `--provider` CLI flag or `migration.json â†’ database.provider`.

#### Provider-agnostic infrastructure
- `RelationalMigrationTracker` â€” DELETE+INSERT upsert pattern (works on all four engines).
- `RelationalMigrationLockManager` â€” C# date math for lock expiry (no provider-specific SQL).
- `RelationalMigrationExecutor` â€” transaction-bound SQL execution via `System.Data.Common`.
- `RelationalAuditLogger` â€” parameterized INSERT for audit trail.
- `ConfigEnvironmentProvider` â€” environment config from JSON files.

#### Professional project assets
- `README.md` â€” comprehensive GitHub open-source README with badges, Quick Start, command table, script conventions, config reference, architecture diagram, multi-database explanation, and installation guide.
- `LICENSE` â€” MIT license.
- `CHANGELOG.md` â€” this file.
- `Directory.Build.props` â€” package metadata (authors, copyright, license).
- `docs/USAGE.md` â€” complete end-to-end usage guide covering installation, setup, migrations, rollbacks, multi-database, CI/CD, approval gates, deployment windows, and troubleshooting.

### Changed

#### Renamed from "DatabaseMigrationPlatform" (dbpilot) to "DbEpoch"
- Solution: `DbEpoch.sln`
- Source projects: `DbEpoch.Core`, `DbEpoch.Engine`, `DbEpoch.Infrastructure`, `DbEpoch.CLI`
- Test project: `DbEpoch.Engine.Tests`
- Tool command: `DbEpoch` (was `migration`)
- Package: `DbEpoch` (was `dbpilot`)
- All namespaces, directories, and project references updated.

#### Configuration
- `migration.json` â€” added `database.provider` field.
- Environment JSON files â€” added to `Database/Config/environments/`.
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

- `DbEpoch <command> --help` now correctly shows command-specific help (was showing global help for all commands).
- Help output no longer duplicates global options when showing command-specific help.
- JSON output is now emitted cleanly without decorative UI text.
- Migration template files generate correct `{{NAME}}` placeholders (used by `DbEpoch create`).

### Security

- No connection strings, secrets, or tokens are stored in the repository.
- All connection strings use environment variable expansion (`${VAR}`).
