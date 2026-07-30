using System.Data.Common;
using Npgsql;

namespace DbShift.Infrastructure.Database.Providers;

/// <summary>PostgreSQL provider backed by Npgsql.</summary>
public sealed class PostgreSqlProvider : IDatabaseProvider
{
    public string Name => "PostgreSQL";

    public DbConnection CreateConnection(string connectionString)
        => new NpgsqlConnection(connectionString);

    public DbParameter CreateParameter(string name, object? value)
        => new NpgsqlParameter(name, value ?? DBNull.Value);

    public string GetTrackingSchemaDdl() => """
        CREATE TABLE IF NOT EXISTS __migration_history (
            id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            version              VARCHAR(50)  NOT NULL,
            name                 VARCHAR(255) NOT NULL,
            script_name          VARCHAR(500) NOT NULL,
            script_hash          VARCHAR(64)  NOT NULL,
            migration_type       VARCHAR(20)  NOT NULL,
            category             VARCHAR(50)  NOT NULL,
            executed_by          VARCHAR(255) NOT NULL,
            executed_at_utc      TIMESTAMP    NOT NULL DEFAULT NOW(),
            execution_time_ms    INTEGER      NOT NULL,
            environment          VARCHAR(50)  NOT NULL,
            status               VARCHAR(20)  NOT NULL,
            rollback_available   BOOLEAN      NOT NULL DEFAULT FALSE,
            rollback_script_name VARCHAR(500),
            error_message        TEXT,
            batch_number         INTEGER      NOT NULL DEFAULT 1,
            checksum             VARCHAR(64)  NOT NULL,
            created_at_utc       TIMESTAMP    NOT NULL DEFAULT NOW(),
            CONSTRAINT uk_migration_version_env  UNIQUE (version, environment),
            CONSTRAINT uk_migration_script_name  UNIQUE (script_name, environment)
        );
        CREATE INDEX IF NOT EXISTS idx_migration_history_env      ON __migration_history(environment);
        CREATE INDEX IF NOT EXISTS idx_migration_history_status   ON __migration_history(status);
        CREATE INDEX IF NOT EXISTS idx_migration_history_executed ON __migration_history(executed_at_utc);
        CREATE INDEX IF NOT EXISTS idx_migration_history_version  ON __migration_history(version);

        CREATE TABLE IF NOT EXISTS __migration_lock (
            id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            lock_key        VARCHAR(100) NOT NULL UNIQUE,
            locked_by       VARCHAR(255) NOT NULL,
            locked_at_utc   TIMESTAMP    NOT NULL DEFAULT NOW(),
            expires_at_utc  TIMESTAMP    NOT NULL,
            environment     VARCHAR(50)  NOT NULL,
            is_active       BOOLEAN      NOT NULL DEFAULT TRUE
        );
        CREATE INDEX IF NOT EXISTS idx_migration_lock_env_active ON __migration_lock(environment, is_active);

        CREATE TABLE IF NOT EXISTS __migration_audit (
            id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            action           VARCHAR(50)  NOT NULL,
            performed_by     VARCHAR(255) NOT NULL,
            performed_at_utc TIMESTAMP    NOT NULL DEFAULT NOW(),
            environment      VARCHAR(50)  NOT NULL,
            details          TEXT
        );
        CREATE INDEX IF NOT EXISTS idx_migration_audit_date ON __migration_audit(performed_at_utc);
        CREATE INDEX IF NOT EXISTS idx_migration_audit_env  ON __migration_audit(environment);
        """;

    public string GetAcquireLockSql() => """
        INSERT INTO __migration_lock (id, lock_key, locked_by, locked_at_utc, expires_at_utc, environment, is_active)
        VALUES (@id, @lock_key, @locked_by, @locked_at_utc, @expires_at_utc, @environment, @true)
        ON CONFLICT (lock_key) DO UPDATE
        SET id            = EXCLUDED.id,
            locked_by     = EXCLUDED.locked_by,
            locked_at_utc = EXCLUDED.locked_at_utc,
            expires_at_utc= EXCLUDED.expires_at_utc,
            environment   = EXCLUDED.environment,
            is_active     = EXCLUDED.is_active
        WHERE __migration_lock.is_active = @false OR __migration_lock.expires_at_utc <= @now
        """;
}
