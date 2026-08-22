# AGENTS.md

## Build

```bash
# Full solution (all projects)
dotnet build DbShift.slnx

# Single project
dotnet build src/DbShift.CLI/DbShift.CLI.csproj
```

`TreatWarningsAsErrors` is enabled globally via `Directory.Build.props`. Zero warnings required.

Target framework: `net10.0`.

## Test

```bash
# Unit + SQLite integration tests (no external services)
dotnet test --filter "Category!=Integration"

# Real-database integration tests (Postgres/MySQL/SQL Server via Testcontainers, needs Docker)
dotnet test tests/DbShift.Engine.Tests --filter "Category=Integration"
```

Test count: 116 unit/SQLite tests across 13 test classes, plus 54 Docker-backed integration tests in `tests/DbShift.Engine.Tests/Integration/`.

## Lint / Format

No separate lint step — `TreatWarningsAsErrors` serves as the gate. The build is the lint.

## Project structure

| Project | Responsibility |
|---------|---------------|
| `DbShift.Core` | Entities, enums, value objects, interfaces. Zero dependencies. |
| `DbShift.Engine` | `ScriptParser`, `MigrationExecutor`, in-memory test doubles. |
| `DbShift.Infrastructure` | 4 database providers (PostgreSQL, SqlServer, MySQL, SQLite), relational implementations, `FileSystemConfigLoader`. |
| `DbShift.CLI` | `dbshift` executable using `Spectre.Console.Cli` 0.49.1. |

## Key conventions

- Custom exceptions in `DbShift.Core/Exceptions/DbShiftException.cs`: `DbShiftException` (base), `ScriptParseException`, `MigrationConfigurationException`, `UnsupportedProviderException`.
- In-memory test doubles in `DbShift.Engine/InMemory/InMemoryImplementations.cs`.
- Migration scripts: `V<digits>__Name.sql` (versioned), `R__Name.sql` (repeatable), `U<digits>__Name.sql` (rollback).
- SHA-256 hashing with LF normalization for checksums.
- 3 tracking tables: `__migration_history`, `__migration_lock`, `__migration_audit`.
