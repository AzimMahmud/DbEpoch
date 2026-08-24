# Script Conventions

DbEpoch uses Flyway-style naming for migration scripts.

## File patterns

### Versioned migrations (`V` prefix)

```
Database/Migrations/Schema/V001__CreateUsersTable.sql
Database/Migrations/Schema/V002__AddEmailColumn.sql
Database/Migrations/Schema/V20260617120000__CreateOrders.sql
```

- `V` = versioned (applied once)
- Version can be a sequence (`001`, `002`) or a UTC timestamp (`20260617120000`)
- `__` (double underscore) separates the version from the name
- The name should be PascalCase, underscores allowed

### Repeatable migrations (`R` prefix)

```
Database/Migrations/Schema/R__RefreshUserView.sql
```

- `R` = repeatable (re-applied every time the checksum changes)
- No version number

### Rollback scripts (`U` prefix)

```
Database/Migrations/Rollback/U001__Rollback_CreateUsersTable.sql
```

- `U` = undo
- The version must match the forward migration's version (without the `V` prefix)
- Paired with their forward counterparts

## Directory structure

```
Database/Migrations/
  Schema/          versioned DDL (V prefix)
  Data/            seed/migration data (V prefix)
  Patch/           hotfix DDL/DML (V prefix)
  Rollback/        undo scripts (U prefix)
```

## Metadata headers

Optional metadata in the SQL file:

```sql
-- Migration: CreateUsersTable
-- Author: jane.doe
-- Created: 2025-01-15
-- Description: Creates the users table
-- Depends: V000__InitialSetup.sql
```

| Header | Required | Description |
|--------|----------|-------------|
| `-- Migration:` | No | Migration name |
| `-- Author:` | No | Who wrote this migration |
| `-- Created:` | No | Creation date |
| `-- Description:` | No | What this migration does |
| `-- Depends:` | No | Comma-separated list of prerequisite scripts |

## Checksums

DbEpoch computes a SHA-256 checksum for each script. The checksum is computed after normalizing line endings to LF. This ensures cross-platform consistency.

If a previously-applied script is edited in place, the checksum changes, and `DbEpoch migrate` will refuse to deploy (checksum drift detection). You must either:

1. Create a new migration instead of editing existing ones, or
2. Use `DbEpoch repair` to clear the history, then re-apply
