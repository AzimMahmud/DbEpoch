using System.Data.Common;
using Microsoft.Data.Sqlite;

namespace DbShift.Infrastructure.Database.Providers;

/// <summary>SQLite provider backed by Microsoft.Data.Sqlite.</summary>
public sealed class SqliteProvider : IDatabaseProvider
{
    public string Name => "SQLite";

    public DbConnection CreateConnection(string connectionString)
        => new SqliteConnection(connectionString);

    public DbParameter CreateParameter(string name, object? value)
        => new SqliteParameter("@" + name, value ?? DBNull.Value);

    public string GetTrackingSchemaDdl() => """
        CREATE TABLE IF NOT EXISTS __migration_history (
            id                   TEXT NOT NULL PRIMARY KEY,
            version              TEXT NOT NULL,
            name                 TEXT NOT NULL,
            script_name          TEXT NOT NULL,
            script_hash          TEXT NOT NULL,
            migration_type       TEXT NOT NULL,
            category             TEXT NOT NULL,
            executed_by          TEXT NOT NULL,
            executed_at_utc      TEXT NOT NULL,
            execution_time_ms    INTEGER NOT NULL,
            environment          TEXT NOT NULL,
            status               TEXT NOT NULL,
            rollback_available   INTEGER NOT NULL DEFAULT 0,
            rollback_script_name TEXT,
            error_message        TEXT,
            batch_number         INTEGER NOT NULL DEFAULT 1,
            checksum             TEXT NOT NULL,
            created_at_utc       TEXT NOT NULL
        );
        CREATE UNIQUE INDEX IF NOT EXISTS uk_migration_version_env ON __migration_history(version, environment);
        CREATE UNIQUE INDEX IF NOT EXISTS uk_migration_script_name ON __migration_history(script_name, environment);
        CREATE INDEX IF NOT EXISTS idx_migration_history_env      ON __migration_history(environment);
        CREATE INDEX IF NOT EXISTS idx_migration_history_status   ON __migration_history(status);
        CREATE INDEX IF NOT EXISTS idx_migration_history_executed ON __migration_history(executed_at_utc);
        CREATE INDEX IF NOT EXISTS idx_migration_history_version  ON __migration_history(version);

        CREATE TABLE IF NOT EXISTS __migration_lock (
            id              TEXT NOT NULL PRIMARY KEY,
            lock_key        TEXT NOT NULL,
            locked_by       TEXT NOT NULL,
            locked_at_utc   TEXT NOT NULL,
            expires_at_utc  TEXT NOT NULL,
            environment     TEXT NOT NULL,
            is_active       INTEGER NOT NULL DEFAULT 1
        );
        CREATE UNIQUE INDEX IF NOT EXISTS uk_migration_lock_key ON __migration_lock(lock_key);
        CREATE INDEX IF NOT EXISTS idx_migration_lock_env_active ON __migration_lock(environment, is_active);

        CREATE TABLE IF NOT EXISTS __migration_audit (
            id               TEXT NOT NULL PRIMARY KEY,
            action           TEXT NOT NULL,
            performed_by     TEXT NOT NULL,
            performed_at_utc TEXT NOT NULL,
            environment      TEXT NOT NULL,
            details          TEXT
        );
        CREATE INDEX IF NOT EXISTS idx_migration_audit_date ON __migration_audit(performed_at_utc);
        CREATE INDEX IF NOT EXISTS idx_migration_audit_env  ON __migration_audit(environment);
        """;

    public string GetAcquireLockSql() => """
        INSERT INTO __migration_lock (id, lock_key, locked_by, locked_at_utc, expires_at_utc, environment, is_active)
        VALUES (@id, @lock_key, @locked_by, @locked_at_utc, @expires_at_utc, @environment, @true)
        ON CONFLICT(lock_key) DO UPDATE
        SET id            = excluded.id,
            locked_by     = excluded.locked_by,
            locked_at_utc = excluded.locked_at_utc,
            expires_at_utc= excluded.expires_at_utc,
            environment   = excluded.environment,
            is_active     = excluded.is_active
        WHERE __migration_lock.is_active = @false OR __migration_lock.expires_at_utc <= @now
        """;
}
