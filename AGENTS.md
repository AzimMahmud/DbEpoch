# AGENTS.md

## Build

```bash
# Full solution (all projects, all target frameworks)
dotnet build DbShift.slnx

# Single project
dotnet build src/DbShift.CLI/DbShift.CLI.csproj
```

`TreatWarningsAsErrors` is enabled globally via `Directory.Build.props`. Zero warnings required.

Target frameworks: `net6.0`, `net8.0`, `net10.0`.

## Test

```bash
# All frameworks
dotnet test

# Single framework
dotnet test tests/DbShift.Engine.Tests/DbShift.Engine.Tests.csproj --framework net10.0
```

Test count: 89 tests across 10 test classes.

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
