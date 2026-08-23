# migrate

Apply pending migrations to the target environment. Aliases: `deploy`, `apply`.

**Database required:** Yes

## Usage

```bash
# Interactive (asks for confirmation)
dbshift migrate --connection-string "Host=localhost;Database=myapp;Username=postgres"

# Non-interactive (for automation)
dbshift migrate --connection-string "$DB_CONNECTION_STRING" --yes

# Specify environment (uses per-environment config)
dbshift migrate --environment production --yes

# Override batch size
dbshift migrate --batch-size 5

# Bypass deployment window check
dbshift migrate --environment production --force

# With approval gating
dbshift migrate --environment production --approver jane@corp.com --yes
```

## Options

| Option | Short | Description |
|--------|-------|-------------|
| `--executed-by` | `-u` | User performing the deployment |
| `--approver` | `-A` | Approver identity (required for approval-gated environments) |
| `--batch-size` | `-b` | Override migration batch size |
| `--force` | `-f` | Proceed even outside the deployment window |

Also accepts [global options](/reference/global-options).

## What happens during a deploy

1. **Lock acquisition** — acquires a row-level distributed lock to prevent concurrent runs
2. **Plan computation** — determines which migrations are pending
3. **Batch execution** — applies migrations in batches (configurable via `batchSize`)
4. **Status tracking** — each migration is recorded in `__migration_history`
5. **Audit logging** — every action logged in `__migration_audit`
6. **Lock release** — releases the distributed lock
7. **Result reporting** — shows what was applied, how long it took

## Approval gating

For environments with `requireApproval: true`, you must provide an approver:

```bash
dbshift migrate --environment production --approver jane@corp.com
```

Without `--approver`, the command fails with a clear message.

## Deployment windows

For environments with a configured `deploymentWindow`, the command checks:

- **Time range**: current time must be between `startTime` and `endTime`
- **Allowed days**: current day of week must be in `allowedDays`

```bash
# Outside the window -> blocked
dbshift migrate --environment production
# Error: Outside the configured deployment window.

# Override with --force
dbshift migrate --environment production --force
```

Times are evaluated against local time using invariant culture — `Mon`, `Tuesday`, or `TUESDAY` all match.

## On failure

- The error is recorded in `__migration_history`
- The transaction for that single script is rolled back
- If `stopOnFailure` is `true` (default), the deployment stops immediately
- Run `dbshift repair` to clear the failed state, then retry

## JSON output

```bash
dbshift migrate --json
```

```json
{
  "success": true,
  "applied": 3,
  "appliedMigrations": ["002", "003", "004"],
  "executionTimeMs": 1234
}
```
