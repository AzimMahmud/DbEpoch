# rollback

Roll back one or more previously applied migrations using `U` scripts.

**Database required:** Yes

## Usage

```bash
# Roll back the last migration
dbshift rollback --environment local

# Roll back the last 3 migrations
dbshift rollback --environment production --count 3

# Roll back a specific version
dbshift rollback --environment production --target-version 003

# Non-interactive
dbshift rollback --environment production --yes
```

## Options

| Option | Short | Description |
|--------|-------|-------------|
| `--target-version` | `-V` | Specific version to roll back (default: last applied) |
| `--count` | | Number of recent migrations to roll back (default: `1`) |
| `--executed-by` | `-u` | User performing the rollback |

Also accepts [global options](/reference/global-options).

## Prerequisites

Each forward migration must have a paired rollback script:

```
Database/Migrations/Schema/V001__CreateUsersTable.sql
Database/Migrations/Rollback/U001__Rollback_CreateUsersTable.sql
```

The version must match (without the `V`/`U` prefix).

If no `U` script exists for a migration, rollback fails with a clear message.

## Rollback order

When rolling back multiple migrations (`--count` or `--target-version`), they are undone in **reverse chronological order** — most recent first.

## Notes

- Rollback executes the `U` script in a transaction
- The migration record is updated to `RolledBack` status in `__migration_history`
- The audit trail records the rollback action
- Some environments may have `allowRollback: false` — check your environment config
