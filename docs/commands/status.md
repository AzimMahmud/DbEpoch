# status

Show migration status for an environment.

**Database required:** Yes

## Usage

```bash
DbEpoch status
DbEpoch status --environment production
DbEpoch status --json    # machine-readable for CI
```

Also accepts [global options](/reference/global-options).

## Example output

```
 Migration Status

  Environment:    local
  Total:          12
  Applied:        10
  Pending:        1
  Failed:         1

  #  Version  Name                  Status       Type     Executed At           Duration
  â”€  â”€â”€â”€â”€â”€â”€â”€  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”€â”€â”€â”€â”€â”€â”€  â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€  â”€â”€â”€â”€â”€â”€â”€â”€
  1  001      CreateUsers           Completed    Schema   2026-06-15 10:23:00   145ms
  2  002      CreateRoles           Completed    Schema   2026-06-15 10:23:01   89ms
  3  003      SeedRoles             Completed    Data     2026-06-15 10:23:02   234ms
  ...
  11  011      AddOrders             Failed       Schema   2026-06-17 14:00:00   56ms
  12  012      AddOrderItems         Pending      Schema   â€”                     â€”
```

## Status values

| Status | Color | Meaning |
|--------|-------|---------|
| Completed | Green | Successfully applied |
| Pending | Yellow | Waiting to be applied |
| Failed | Red | Failed during execution |
| RolledBack | Violet | Undone via rollback |
| InProgress | Blue | Currently being applied |

## JSON output

```bash
DbEpoch status --json
```

```json
{
  "success": true,
  "environment": "local",
  "total": 12,
  "applied": 10,
  "pending": 1,
  "failed": 1,
  "migrations": [...]
}
```
