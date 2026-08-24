# init

Create the migration tracking schema (3 tables) on the target database.

**Database required:** Yes

## Usage

```bash
DbEpoch init --connection-string "Host=localhost;Database=myapp;Username=postgres;Password=secret"
```

## Tables created

| Table | Purpose |
|-------|---------|
| `__migration_history` | One row per applied migration per environment |
| `__migration_lock` | Distributed lock preventing concurrent deploys |
| `__migration_audit` | Append-only audit trail |

The DDL is idempotent — if the tables already exist, `init` won't modify them.

## When to run

Run `DbEpoch init` once per database, before your first `DbEpoch migrate`. You don't need to run it again afterward.

## Notes

- The schema name comes from `tracking.schema` in `migration.json` (default: `public`)
- Table names come from `tracking.tableName` (default: `__migration_history`)
- DDL is engine-specific: UUID, boolean, and timestamp types differ between PostgreSQL, SQL Server, MySQL, and SQLite
