# repair

Re-queue one or all failed migrations so they can be retried.

**Database required:** Yes

## Usage

```bash
# Repair all failed migrations
DbEpoch repair

# Repair a specific failed migration
DbEpoch repair --target-version 005
```

## Options

| Option | Short | Description |
|--------|-------|-------------|
| `--target-version` | `-V` | Specific version to repair (omit to repair all failed) |

Also accepts [global options](/reference/global-options).

## What it does

`repair` removes the failed record(s) from `__migration_history`, allowing the migration(s) to be retried on the next `DbEpoch migrate`.

## Important

`repair` does **NOT** undo any database changes the failed script may have made. If the script partially executed before failing, you need to clean up those changes manually before retrying.

## When to use

1. A migration fails during `DbEpoch migrate`
2. Fix the SQL in the migration file
3. Clean up any partial changes in the database (if applicable)
4. Run `DbEpoch repair` to clear the failed state
5. Run `DbEpoch migrate` again to retry
