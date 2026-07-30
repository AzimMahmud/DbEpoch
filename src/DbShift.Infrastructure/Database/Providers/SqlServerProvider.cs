using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace DbShift.Infrastructure.Database.Providers;

/// <summary>SQL Server provider backed by Microsoft.Data.SqlClient.</summary>
public sealed class SqlServerProvider : IDatabaseProvider
{
    public string Name => "SQL Server";

    public DbConnection CreateConnection(string connectionString)
        => new SqlConnection(connectionString);

    public DbParameter CreateParameter(string name, object? value)
        => new SqlParameter("@" + name, value ?? DBNull.Value);

    public string GetTrackingSchemaDdl() => """
        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__migration_history')
        CREATE TABLE __migration_history (
            id                   UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
            version              NVARCHAR(50)   NOT NULL,
            name                 NVARCHAR(255)  NOT NULL,
            script_name          NVARCHAR(500)  NOT NULL,
            script_hash          NVARCHAR(64)   NOT NULL,
            migration_type       NVARCHAR(20)   NOT NULL,
            category             NVARCHAR(50)   NOT NULL,
            executed_by          NVARCHAR(255)  NOT NULL,
            executed_at_utc      DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
            execution_time_ms    INT             NOT NULL,
            environment          NVARCHAR(50)   NOT NULL,
            status               NVARCHAR(20)   NOT NULL,
            rollback_available   BIT             NOT NULL DEFAULT 0,
            rollback_script_name NVARCHAR(500),
            error_message        NVARCHAR(MAX),
            batch_number         INT             NOT NULL DEFAULT 1,
            checksum             NVARCHAR(64)   NOT NULL,
            created_at_utc       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
            CONSTRAINT uk_migration_version_env UNIQUE (version, environment),
            CONSTRAINT uk_migration_script_name UNIQUE (script_name, environment)
        );

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_migration_history_env' AND object_id = OBJECT_ID('__migration_history'))
        CREATE INDEX idx_migration_history_env ON __migration_history(environment);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_migration_history_status' AND object_id = OBJECT_ID('__migration_history'))
        CREATE INDEX idx_migration_history_status ON __migration_history(status);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_migration_history_executed' AND object_id = OBJECT_ID('__migration_history'))
        CREATE INDEX idx_migration_history_executed ON __migration_history(executed_at_utc);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_migration_history_version' AND object_id = OBJECT_ID('__migration_history'))
        CREATE INDEX idx_migration_history_version ON __migration_history(version);

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__migration_lock')
        CREATE TABLE __migration_lock (
            id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
            lock_key        NVARCHAR(100)  NOT NULL UNIQUE,
            locked_by       NVARCHAR(255)  NOT NULL,
            locked_at_utc   DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
            expires_at_utc  DATETIME2       NOT NULL,
            environment     NVARCHAR(50)   NOT NULL,
            is_active       BIT             NOT NULL DEFAULT 1
        );

        IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '__migration_audit')
        CREATE TABLE __migration_audit (
            id               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
            action           NVARCHAR(50)   NOT NULL,
            performed_by     NVARCHAR(255)  NOT NULL,
            performed_at_utc DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
            environment      NVARCHAR(50)   NOT NULL,
            details          NVARCHAR(MAX)
        );

        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_migration_audit_date' AND object_id = OBJECT_ID('__migration_audit'))
        CREATE INDEX idx_migration_audit_date ON __migration_audit(performed_at_utc);
        IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'idx_migration_audit_env' AND object_id = OBJECT_ID('__migration_audit'))
        CREATE INDEX idx_migration_audit_env ON __migration_audit(environment);
        """;

    public string GetAcquireLockSql() => """
        MERGE __migration_lock WITH (HOLDLOCK) AS target
        USING (SELECT @lock_key AS lock_key) AS source
        ON (target.lock_key = source.lock_key)
        WHEN MATCHED AND (target.is_active = 0 OR target.expires_at_utc <= @now) THEN
            UPDATE SET id            = @id,
                       locked_by     = @locked_by,
                       locked_at_utc = @locked_at_utc,
                       expires_at_utc= @expires_at_utc,
                       environment   = @environment,
                       is_active     = 1
        WHEN NOT MATCHED THEN
            INSERT (id, lock_key, locked_by, locked_at_utc, expires_at_utc, environment, is_active)
            VALUES (@id, @lock_key, @locked_by, @locked_at_utc, @expires_at_utc, @environment, 1);
        """;
}
