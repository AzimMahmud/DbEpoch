# history

Show the audit trail for an environment. Alias: `audit`.

**Database required:** Yes

## Usage

```bash
DbEpoch history
DbEpoch history --environment production --limit 50
DbEpoch history --json
```

## Options

| Option | Short | Description |
|--------|-------|-------------|
| `--limit` | `-l` | Maximum entries to display (default: `25`) |

Also accepts [global options](/reference/global-options).

## What it shows

The audit trail records every action performed against the database:

- **Validate** — script validation runs
- **DryRun** — plan computations
- **Deploy** — migration applications
- **Rollback** — rollback operations
- **Repair** — failed migration repairs

## Example output

```
 Audit Trail

  Environment:    production
  Showing:         10 of 45 entries

  #  Action    Performed By     At                      Details
    ────────  ────────  ────────  ────────  ────────
  1  Deploy    jane@corp.com    2026-06-17 14:30:00     Applied 3 migrations (002-004)
  2  Deploy    deploy-bot       2026-06-15 10:00:00     Applied 1 migration (001)
  3  Rollback  jane@corp.com    2026-06-16 16:45:00     Rolled back 004
```

## JSON output

```bash
DbEpoch history --json
```

Returns an array of audit entries with `action`, `performedBy`, `performedAtUtc`, `environment`, and `details` fields.
