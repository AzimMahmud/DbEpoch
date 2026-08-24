# AGENTS.md

## Build

```bash
# Full solution (all projects)
dotnet build DbEpoch.slnx

# Single project
dotnet build src/DbEpoch.CLI/DbEpoch.CLI.csproj
```

`TreatWarningsAsErrors` is enabled globally via `Directory.Build.props`. Zero warnings required.

Target framework: `net10.0`.

## Test

```bash
# Unit + SQLite integration tests (no external services)
dotnet test --filter "Category!=Integration"

# Real-database integration tests (Postgres/MySQL/SQL Server via Testcontainers, needs Docker)
dotnet test tests/DbEpoch.Engine.Tests --filter "Category=Integration"
```

Test count: 116 unit/SQLite tests across 13 test classes, plus 54 Docker-backed integration tests in `tests/DbEpoch.Engine.Tests/Integration/`.

## Lint / Format

No separate lint step â€” `TreatWarningsAsErrors` serves as the gate. The build is the lint.

## Project structure

| Project | Responsibility |
|---------|---------------|
| `DbEpoch.Core` | Entities, enums, value objects, interfaces. Zero dependencies. |
| `DbEpoch.Engine` | `ScriptParser`, `MigrationExecutor`, in-memory test doubles. |
| `DbEpoch.Infrastructure` | 4 database providers (PostgreSQL, SqlServer, MySQL, SQLite), relational implementations, `FileSystemConfigLoader`. |
| `DbEpoch.CLI` | `DbEpoch` executable using `Spectre.Console.Cli` 0.49.1. |

## Key conventions

- Custom exceptions in `DbEpoch.Core/Exceptions/DbEpochException.cs`: `DbEpochException` (base), `ScriptParseException`, `MigrationConfigurationException`, `UnsupportedProviderException`.
- In-memory test doubles in `DbEpoch.Engine/InMemory/InMemoryImplementations.cs`.
- Migration scripts: `V<digits>__Name.sql` (versioned), `R__Name.sql` (repeatable), `U<digits>__Name.sql` (rollback).
- SHA-256 hashing with LF normalization for checksums.
- 3 tracking tables: `__migration_history`, `__migration_lock`, `__migration_audit`.
