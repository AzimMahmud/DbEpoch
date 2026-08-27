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

## Install dbsh in CI

::: code-group

```bash [curl (Linux/macOS)]
curl -fsSL https://github.com/AzimMahmud/dbsh/releases/latest/download/install.sh | bash
```

```powershell [PowerShell (Windows)]
powershell -c "iwr -Uri https://github.com/AzimMahmud/dbsh/releases/latest/download/install.ps1 | iex"
```

```bash [.NET tool]
dotnet tool install --global dbsh
```

:::

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
      - run: curl -fsSL https://github.com/AzimMahmud/dbsh/releases/latest/download/install.sh | bash
      - run: dbsh validate --json

  deploy-dev:
    needs: validate
    if: github.ref == 'refs/heads/main'
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - run: curl -fsSL https://github.com/AzimMahmud/dbsh/releases/latest/download/install.sh | bash
      - run: dbsh migrate -e development --yes --json
        env:
          DB_CONNECTION_STRING: ${{ secrets.DEV_DB_CONNECTION_STRING }}
```

## Azure DevOps

```yaml
steps:
  - script: curl -fsSL https://github.com/AzimMahmud/dbsh/releases/latest/download/install.sh | bash
    displayName: 'Install dbsh'

  - script: dbsh validate --json
    displayName: 'Validate migrations'

  - script: dbsh migrate -e development --yes
    displayName: 'Deploy to dev'
    env:
      DB_CONNECTION_STRING: $(DEV_DB_CONNECTION_STRING)
```

## GitLab CI

```yaml
dbsh-validate:
  stage: validate
  before_script:
    - curl -fsSL https://github.com/AzimMahmud/dbsh/releases/latest/download/install.sh | bash
  script:
    - dbsh validate --json

dbsh-deploy:
  stage: deploy
  before_script:
    - curl -fsSL https://github.com/AzimMahmud/dbsh/releases/latest/download/install.sh | bash
  script:
    - dbsh migrate -e development --yes --json
  variables:
    DB_CONNECTION_STRING: $DEV_DB_CONNECTION_STRING
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
- run: dbsh validate --json
  id: validate

- run: echo "Validation passed"
  if: steps.validate.outcome == 'success'
```
