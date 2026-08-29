# Architecture

dbsh is built as a layered .NET solution with clear separation of concerns.

## Project structure

```
dbsh/
  src/
    dbsh.Core/             Pure domain model (zero dependencies)
    dbsh.Engine/           Script parsing, migration execution
    dbsh.Infrastructure/   Database providers, file system config
    dbsh.CLI/              The dbsh executable
  tests/
    dbsh.Engine.Tests/     Unit + integration tests
```

## Layer diagram

```
┌──────────────────────────────────────────┐
│              dbsh.CLI                 │
│   Program.cs · Commands · ConsoleHelper  │
└──────────────────┬───────────────────────┘
                   │ uses
        ┌──────────┴─────────┐     ┌──────────────────┐
        │    Engine          │     │      Core         │
        │ ScriptParser       │────▶│ Entities · Enums  │
        │ MigrationExec      │     │ ValueObjects      │
        │ InMemory impls     │     └──────────────────┘
        └──────────┬─────────┘              ▲
                   │ implements             │ implements
    ┌──────────────┴────────────────────────┴───────────────┐
    │                  Infrastructure                       │
    │  Providers/                                           │
    │    PostgreSql · SqlServer · MySql · Sqlite · Oracle     │
    │  Relational{Tracker, LockManager, Executor, Audit}    │
    │  FileSystemConfigLoader                               │
    └───────────────────────────────────────────────────────┘
```

## Core (`dbsh.Core`)

Pure domain model with zero external dependencies.

| Namespace | Key Types |
|-----------|-----------|
| `Entities` | `MigrationRecord`, `MigrationAuditEntry`, `MigrationLock` |
| `Enums` | `MigrationStatus`, `MigrationType`, `AuditAction` |
| `Interfaces` | `IMigrationTracker`, `IMigrationLockManager`, `IAuditLogger`, `IEnvironmentProvider`, `IMigrationScriptExecutor`, `IConfigLoader` |
| `ValueObjects` | `MigrationConfiguration`, `EnvironmentConfiguration`, `DeploymentWindow`, `ParsedMigration`, `MigrationContext` |
| `Exceptions` | `dbshException`, `ScriptParseException`, `MigrationConfigurationException`, `UnsupportedProviderException` |

## Engine (`dbsh.Engine`)

Application core that orchestrates the migration workflow.

| Class | Responsibility |
|-------|---------------|
| `ScriptParser` | Parses Flyway-style filenames, generates SHA-256 hashes (LF-normalized), extracts metadata headers, validates content |
| `MigrationExecutor` | Orchestrates validation, planning, deployment, rollback, and repair. Coordinates tracker, lock manager, audit log, environment provider, and script executor |
| `InMemoryMigrationTracker` | In-memory test double for offline workflows |
| `InMemoryMigrationLockManager` | In-memory lock manager |
| `InMemoryAuditLogger` | In-memory audit logger |

## Infrastructure (`dbsh.Infrastructure`)

Database-specific implementations and file system access.

| Class | Responsibility |
|-------|---------------|
| `PostgreSqlProvider` | PostgreSQL via Npgsql. Schema-based module isolation. Lock: `INSERT ... ON CONFLICT DO UPDATE` |
| `SqlServerProvider` | SQL Server via Microsoft.Data.SqlClient. Schema-based module isolation. Lock: `MERGE ... WITH (HOLDLOCK)` |
| `MySqlProvider` | MySQL via MySqlConnector. Table-prefix module isolation. Lock: `INSERT ... ON DUPLICATE KEY UPDATE` |
| `SqliteProvider` | SQLite via Microsoft.Data.Sqlite. Table-prefix module isolation. Lock: `INSERT ... ON CONFLICT DO UPDATE` |
| `OracleProvider` | Oracle via Oracle.ManagedDataAccess.Core. Schema-based module isolation. Lock: `MERGE` |
| `RelationalMigrationTracker` | Provider-agnostic history tracking using `System.Data.Common` |
| `RelationalMigrationLockManager` | Row-based distributed lock with lease expiry |
| `FileSystemConfigLoader` | Loads `migration.json` and `environments/*.json`, expands `${VAR}` tokens |

## CLI (`dbsh.CLI`)

The `dbsh` executable built with Spectre.Console.Cli.

| Class | Responsibility |
|-------|---------------|
| `Program` | Entry point, command registration |
| `CliCommandBase` | Shared helpers for all commands |
| `CliHost` | Composition root — resolves providers, wires implementations |
| `ConsoleHelper` | Spectre.Console UI: banners, tables, spinners, gradient text |
| `Theme` | Color palette and glyphs |

## Adding a new provider

Every database-specific behavior is encapsulated behind `IDatabaseProvider`. Adding a new provider requires implementing this single interface. The `Relational*` classes use `System.Data.Common` base types (`DbConnection`, `DbCommand`, `DbDataReader`) so the same code path works across all engines.
