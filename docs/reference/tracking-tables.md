# Tracking Tables

`dbsh init` creates three tables in your database. These track migration state, prevent concurrent deploys, and maintain an audit trail.

## `__migration_history`

One row per applied migration per environment.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID/GUID | Primary key |
| `version` | VARCHAR | Migration version (e.g., `001`) or `R` for repeatables |
| `name` | VARCHAR | Migration name |
| `script_name` | VARCHAR | Filename (e.g., `V001__CreateUsers.sql`) |
| `script_hash` | VARCHAR | SHA-256 checksum of the script content |
| `migration_type` | VARCHAR | Schema, Data, Patch, or Repeatable |
| `category` | VARCHAR | Category (e.g., Schema, Data, Patch) |
| `executed_by` | VARCHAR | User or identity that ran the migration |
| `executed_at_utc` | TIMESTAMP | When the migration was applied |
| `execution_time_ms` | BIGINT | How long the migration took |
| `environment` | VARCHAR | Target environment name |
| `status` | VARCHAR | Completed, Failed, InProgress, or RolledBack |
| `rollback_available` | BOOLEAN | Whether a U script exists for this version |
| `rollback_script_name` | VARCHAR | Filename of the rollback script |
| `error_message` | TEXT | Error details if the migration failed |
| `batch_number` | INTEGER | Batch number for grouped executions |

**Unique constraints:**
- `(script_name, environment)` — each script applied once per environment
- `(version, environment)` WHERE `version <> 'R'` — one versioned migration per version per environment

## `__migration_lock`

Distributed lock preventing concurrent deploys.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID/GUID | Primary key |
| `lock_key` | VARCHAR | Lock identifier |
| `locked_by` | VARCHAR | Owner identity |
| `locked_at_utc` | TIMESTAMP | When the lock was acquired |
| `expires_at_utc` | TIMESTAMP | When the lock expires (lease-based) |
| `environment` | VARCHAR | Target environment |
| `is_active` | BOOLEAN | Whether the lock is currently held |

The lock uses lease-based expiry with automatic renewal during batch execution. This means a crashed deployment won't block future runs indefinitely — the lock expires after `lockTimeoutSeconds`.

## `__migration_audit`

Append-only audit trail of every action.

| Column | Type | Description |
|--------|------|-------------|
| `id` | UUID/GUID | Primary key |
| `action` | VARCHAR | Validate, DryRun, Deploy, Rollback, or Repair |
| `performed_by` | VARCHAR | User or identity |
| `performed_at_utc` | TIMESTAMP | When the action was performed |
| `environment` | VARCHAR | Target environment |
| `details` | TEXT | JSON or text details about the action |

## Engine-specific DDL

The tracking table DDL differs between providers:

| Concern | PostgreSQL | SQL Server | MySQL | SQLite | Oracle |
|---------|-----------|------------|-------|--------|--------|
| UUID column | `UUID` | `UNIQUEIDENTIFIER` | `CHAR(36)` | `TEXT` | `RAW(16)` |
| Boolean column | `BOOLEAN` | `BIT` | `TINYINT(1)` | `INTEGER` (0/1) | `NUMBER(1)` (0/1) |
| Timestamp column | `TIMESTAMPTZ` | `DATETIME2` | `DATETIME` | `TEXT` | `TIMESTAMP` |
| ID default | `gen_random_uuid()` | `NEWID()` | C# Guid | C# Guid | `SYS_GUID()` |
