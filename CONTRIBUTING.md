# Contributing to dbsh

Thanks for your interest! Here's how to get started.

## Development setup

```bash
git clone https://github.com/AzimMahmud/dbsh.git
cd dbsh
dotnet restore
dotnet build
dotnet test --filter "Category!=Integration"
```

Requirements:
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — `global.json` pins `10.0.100` with `latestFeature` roll-forward
- PowerShell 7+ (Windows) or bash (Linux/macOS) for the build scripts
- Docker (optional) — only needed to run the real-database integration tests: `dotnet test tests/dbsh.Engine.Tests --filter "Category=Integration"`

## Project structure

```
src/
├── dbsh.Core/          domain model, no dependencies
├── dbsh.Engine/        script parser, migration executor, in-memory doubles
├── dbsh.Infrastructure/ providers, relational implementations, config loading
└── dbsh.CLI/           executable, argument parsing, Spectre.Console.Cli UI
tests/
└── dbsh.Engine.Tests/  116 tests: parser, executor, config loader, locks, exceptions
    └── Integration/        54 tests: same tracker/lock/audit contract against real
                             PostgreSQL/MySQL/SQL Server containers (needs Docker)
```

## Code conventions

- **Language:** C# 12, nullable enabled, file-scoped namespaces.
- **Style:** Follow existing patterns. No BOM, LF line endings.
- **Warnings:** `TreatWarningsAsErrors` is enforced. Your code must compile with zero warnings.
- **Tests:** Every PR should include or update tests. `dotnet test` must pass.
- **No comments:** Production code should be self-documenting. Use meaningful names.

## Making changes

1. Fork and create a feature branch from `main`.
2. Make your changes. Keep them focused â€” one change per PR.
3. Run `dotnet build` and `dotnet test` â€” both must pass cleanly.
4. If adding a new command, add it to the help table and docs.
5. Open a PR against `main`.

## Adding a new database provider

1. Create a class implementing `IDatabaseProvider` in `Infrastructure/Database/Providers/`.
2. Add the NuGet package reference to `dbsh.Infrastructure.csproj`.
3. Register it in `DatabaseProviderFactory.CreateProvider()`.
4. Add the provider config value to the README table.
5. Update provider-specific SQL helpers in `CLI/Helpers/ProviderSqlHelper.cs`.

## Commit messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat: add SQLite provider support
fix: resolve duplicate version detection in ScriptParser
docs: update README with new command table
ci: add macOS to build matrix
```

## PR checklist

Before submitting:

- [ ] Code builds with zero warnings
- [ ] All existing tests pass
- [ ] New tests added for any new behaviour
- [ ] Documentation updated (README, docs/, or inline XML docs)
- [ ] CHANGELOG.md updated under "Unreleased"
- [ ] PR title follows Conventional Commits

## Questions?

Open a [Discussion](https://github.com/AzimMahmud/dbsh/discussions) or an Issue.
