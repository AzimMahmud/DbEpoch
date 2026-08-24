# Configuration

DbEpoch uses a two-tier configuration: a global `migration.json` file and per-environment overrides.

## Global config

`Database/Config/migration.json`:

```json
{
  "migration": {
    "version": "1.0.0",
    "database": {
      "provider": "postgresql",
      "connectionString": "${DB_CONNECTION_STRING}"
    },
    "scripts": {
      "path": "./Database/Migrations",
      "pattern": "*.sql"
    },
    "tracking": {
      "schema": "public",
      "tableName": "__migration_history"
    },
    "execution": {
      "lockTimeoutSeconds": 300,
      "commandTimeoutSeconds": 3600,
      "batchSize": 10,
      "stopOnFailure": true
    },
    "approval": {
      "requireApproval": ["staging", "production"]
    }
  }
}
```

### Options reference

| Option | Default | Description |
|--------|---------|-------------|
| `version` | `1.0.0` | Configuration schema version |
| `database.provider` | — | Database engine: `postgresql`, `sqlserver`, `mysql`, or `sqlite` |
| `database.connectionString` | — | Connection string (supports `${VAR}` expansion) |
| `scripts.path` | `./Database/Migrations` | Relative path to migration scripts |
| `scripts.pattern` | `*.sql` | File glob for migration scripts |
| `tracking.schema` | `public` | Database schema for tracking tables |
| `tracking.tableName` | `__migration_history` | Base name for tracking tables |
| `execution.lockTimeoutSeconds` | `300` | Distributed lock timeout |
| `execution.commandTimeoutSeconds` | `3600` | Per-SQL-command timeout |
| `execution.batchSize` | `10` | Migrations per batch |
| `execution.stopOnFailure` | `true` | Halt on first failure |
| `approval.requireApproval` | `[]` | Environments requiring approval |

## Per-environment files

`Database/Config/environments/<name>.json`:

```json
{
  "name": "production",
  "database": {
    "connectionString": "${PROD_DB_CONNECTION_STRING}"
  },
  "migration": {
    "requireApproval": true,
    "allowRollback": true,
    "lockTimeoutSeconds": 300,
    "maxBatchSize": 5
  },
  "deploymentWindow": {
    "enabled": true,
    "startTime": "02:00",
    "endTime": "06:00",
    "allowedDays": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"]
  }
}
```

### Environment options

| Option | Description |
|--------|-------------|
| `name` | Environment name (must match the filename) |
| `database.connectionString` | Override connection string for this environment |
| `database.host`, `port`, `name`, `schema` | Individual connection components |
| `migration.requireApproval` | Require an approver identity to deploy |
| `migration.allowRollback` | Allow rollback operations |
| `migration.lockTimeoutSeconds` | Override lock timeout |
| `migration.maxBatchSize` | Maximum migrations per batch |
| `deploymentWindow.enabled` | Enable time-based deployment gating |
| `deploymentWindow.startTime` | Window opens at (HH:mm, local time) |
| `deploymentWindow.endTime` | Window closes at (HH:mm, local time) |
| `deploymentWindow.allowedDays` | Days of week when deploys are allowed |

## Environment variable expansion

All `${VAR}` tokens in configuration files are expanded from environment variables at load time. This means secrets never need to be committed to source control.

```json
{
  "database": {
    "connectionString": "${DB_CONNECTION_STRING}"
  }
}
```

```bash
export DB_CONNECTION_STRING="Host=localhost;Database=myapp;Username=postgres"
DbEpoch migrate
```

## Connection string resolution order

When `DbEpoch migrate` runs, the connection string is resolved in this order:

1. `--connection-string` CLI flag (highest priority)
2. `DB_CONNECTION_STRING` environment variable
3. `environments/<name>.json` -> `database.connectionString`
4. `migration.json` -> `database.connectionString` (lowest priority)

Each step is skipped if the value is not set or empty. The first non-empty value wins.

## Validating your config

```bash
DbEpoch info
```

Shows your current configuration, resolved provider, available environments, and file paths.
