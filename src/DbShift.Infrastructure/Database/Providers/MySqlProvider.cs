using System.Data.Common;
using MySqlConnector;

namespace DbShift.Infrastructure.Database.Providers;

/// <summary>MySQL / MariaDB provider backed by MySqlConnector.</summary>
public sealed class MySqlProvider : IDatabaseProvider
{
    public string Name => "MySQL";

    public DbConnection CreateConnection(string connectionString)
        => new MySqlConnection(connectionString);

    public DbParameter CreateParameter(string name, object? value)
        => new MySqlParameter("@" + name, value ?? DBNull.Value);

    public string GetTrackingSchemaDdl() => """
        CREATE TABLE IF NOT EXISTS __migration_history (
            id                   CHAR(36)     NOT NULL PRIMARY KEY,
            version              VARCHAR(50)  NOT NULL,
            name                 VARCHAR(255) NOT NULL,
            script_name          VARCHAR(500) NOT NULL,
            script_hash          VARCHAR(64)  NOT NULL,
            migration_type       VARCHAR(20)  NOT NULL,
            category             VARCHAR(50)  NOT NULL,
            executed_by          VARCHAR(255) NOT NULL,
            executed_at_utc      DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
            execution_time_ms    INT          NOT NULL,
            environment          VARCHAR(50)  NOT NULL,
            status               VARCHAR(20)  NOT NULL,
            rollback_available   TINYINT(1)   NOT NULL DEFAULT 0,
            rollback_script_name VARCHAR(500),
            error_message        LONGTEXT,
            batch_number         INT          NOT NULL DEFAULT 1,
            checksum             VARCHAR(64)  NOT NULL,
            created_at_utc       DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
            UNIQUE KEY uk_migration_version_env (version, environment),
            UNIQUE KEY uk_migration_script_name (script_name, environment),
            INDEX idx_migration_history_env (environment),
            INDEX idx_migration_history_status (status),
            INDEX idx_migration_history_executed (executed_at_utc),
            INDEX idx_migration_history_version (version)
        );

        CREATE TABLE IF NOT EXISTS __migration_lock (
            id              CHAR(36)     NOT NULL PRIMARY KEY,
            lock_key        VARCHAR(100) NOT NULL,
            locked_by       VARCHAR(255) NOT NULL,
            locked_at_utc   DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
            expires_at_utc  DATETIME     NOT NULL,
            environment     VARCHAR(50)  NOT NULL,
            is_active       TINYINT(1)   NOT NULL DEFAULT 1,
            UNIQUE KEY uk_migration_lock_key (lock_key),
            INDEX idx_migration_lock_env_active (environment, is_active)
        );

        CREATE TABLE IF NOT EXISTS __migration_audit (
            id               CHAR(36)     NOT NULL PRIMARY KEY,
            action           VARCHAR(50)  NOT NULL,
            performed_by     VARCHAR(255) NOT NULL,
            performed_at_utc DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
            environment      VARCHAR(50)  NOT NULL,
            details          LONGTEXT,
            INDEX idx_migration_audit_date (performed_at_utc),
            INDEX idx_migration_audit_env (environment)
        );
        """;

    public string GetAcquireLockSql() => """
        INSERT INTO __migration_lock (id, lock_key, locked_by, locked_at_utc, expires_at_utc, environment, is_active)
        VALUES (@id, @lock_key, @locked_by, @locked_at_utc, @expires_at_utc, @environment, 1)
        ON DUPLICATE KEY UPDATE
            id             = IF(is_active = 0 OR expires_at_utc <= @now, VALUES(id), id),
            locked_by      = IF(is_active = 0 OR expires_at_utc <= @now, VALUES(locked_by), locked_by),
            locked_at_utc  = IF(is_active = 0 OR expires_at_utc <= @now, VALUES(locked_at_utc), locked_at_utc),
            expires_at_utc = IF(is_active = 0 OR expires_at_utc <= @now, VALUES(expires_at_utc), expires_at_utc),
            environment    = IF(is_active = 0 OR expires_at_utc <= @now, VALUES(environment), environment),
            is_active      = IF(is_active = 0 OR expires_at_utc <= @now, 1, is_active)
        """;
}
