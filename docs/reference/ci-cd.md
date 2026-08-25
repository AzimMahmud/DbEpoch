# CI/CD Integration

Every `dbsh` command supports `--json` for machine-readable output and deterministic exit codes.

## JSON output

```bash
# Validate
dbsh validate --json
# { "success": true, "scriptsChecked": 16, "errors": [], "warnings": [] }

# Deploy
dbsh migrate --json
# { "success": true, "applied": 3, "appliedMigrations": ["001", "002", "003"], ... }

# Status
dbsh status --json
# { "success": true, "applied": 3, "pending": 1, "failed": 0, ... }
```

Exit codes: `0` = success, `1` = error.

## GitHub Actions

```yaml
name: Database Migration

on:
  push:
    branches: [main]

jobs:
  validate:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet build dbsh.slnx -c Release
      - run: dotnet test dbsh.slnx -c Release --no-build
      - run: dotnet run --project src/dbsh.CLI -- validate --json

  deploy-dev:
    needs: validate
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - run: dotnet run --project src/dbsh.CLI -- migrate -e development --yes --json
        env:
          DB_CONNECTION_STRING: ${{ secrets.DEV_DB_CONNECTION_STRING }}
```

## Azure DevOps

```yaml
steps:
  - script: dotnet run --project src/dbsh.CLI -- validate --json
    displayName: 'Validate migrations'

  - script: dotnet run --project src/dbsh.CLI -- migrate -e development --yes
    displayName: 'Deploy to dev'
    env:
      DB_CONNECTION_STRING: $(DEV_DB_CONNECTION_STRING)
```

## Best practices

| Practice | Why |
|----------|-----|
| Run `validate` first | Fast, no database needed, catches issues early |
| Use `--json` | Structured output for logging and parsing |
| Use `--yes` | Skip interactive prompts in automation |
| Store secrets in secrets/variable groups | Never commit connection strings |
| Use `--in-memory` for validation-only steps | No database dependency in CI |
| Promote through environments | dev -> staging -> prod with approvals at each gate |
| Use `--force` sparingly | Bypasses deployment window and safety checks |

## Exit codes

| Code | Meaning |
|------|---------|
| `0` | Success |
| `1` | Error (migration failed, validation error, lock not acquired, etc.) |

Use these in CI conditionals:

```yaml
- run: dotnet run --project src/dbsh.CLI -- validate --json
  id: validate

- run: echo "Validation passed"
  if: steps.validate.outcome == 'success'
```
