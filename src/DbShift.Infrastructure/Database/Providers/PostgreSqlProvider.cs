using System.Data.Common;
using System.Text.RegularExpressions;
using Npgsql;

namespace DbShift.Infrastructure.Database.Providers;

/// <summary>PostgreSQL provider backed by Npgsql.</summary>
public sealed class PostgreSqlProvider : IDatabaseProvider
{
    public string Name => "PostgreSQL";

    public bool SupportsSchemas => true;

    public DbConnection CreateConnection(string connectionString)
        => new NpgsqlConnection(connectionString);

    public DbParameter CreateParameter(string name, object? value)
        => new NpgsqlParameter(name, value ?? DBNull.Value);

    public string GetTableName(string baseName, string? module = null)
        => string.IsNullOrEmpty(module) ? baseName : $"\"{ValidateModule(module)}\".{baseName}";

    public string GetTrackingSchemaDdl(string? module = null)
    {
        ValidateModule(module);
        var h = Q("__migration_history", module);
        var l = Q("__migration_lock", module);
        var a = Q("__migration_audit", module);
        var schema = string.IsNullOrEmpty(module) ? string.Empty : $"CREATE SCHEMA IF NOT EXISTS \"{ValidateModule(module)}\";\n\n";

        return $"""
            {schema}CREATE TABLE IF NOT EXISTS {h} (
                id                   UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                version              VARCHAR(50)  NOT NULL,
                name                 VARCHAR(255) NOT NULL,
                script_name          VARCHAR(500) NOT NULL,
                script_hash          VARCHAR(64)  NOT NULL,
                migration_type       VARCHAR(20)  NOT NULL,
                category             VARCHAR(50)  NOT NULL,
                executed_by          VARCHAR(255) NOT NULL,
                executed_at_utc      TIMESTAMP    NOT NULL DEFAULT NOW(),
                execution_time_ms    BIGINT       NOT NULL,
                environment          VARCHAR(50)  NOT NULL,
                status               VARCHAR(20)  NOT NULL,
                rollback_available   BOOLEAN      NOT NULL DEFAULT FALSE,
                rollback_script_name VARCHAR(500),
                error_message        TEXT,
                batch_number         INTEGER      NOT NULL DEFAULT 1,
                CONSTRAINT uk_migration_version_env  UNIQUE (version, environment),
                CONSTRAINT uk_migration_script_name  UNIQUE (script_name, environment)
            );
            CREATE INDEX IF NOT EXISTS idx_migration_history_env      ON {h}(environment);
            CREATE INDEX IF NOT EXISTS idx_migration_history_status   ON {h}(status);
            CREATE INDEX IF NOT EXISTS idx_migration_history_executed ON {h}(executed_at_utc);
            CREATE INDEX IF NOT EXISTS idx_migration_history_version  ON {h}(version);

            CREATE TABLE IF NOT EXISTS {l} (
                id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                lock_key        VARCHAR(100) NOT NULL,
                locked_by       VARCHAR(255) NOT NULL,
                locked_at_utc   TIMESTAMP    NOT NULL DEFAULT NOW(),
                expires_at_utc  TIMESTAMP    NOT NULL,
                environment     VARCHAR(50)  NOT NULL,
                is_active       BOOLEAN      NOT NULL DEFAULT TRUE,
                CONSTRAINT uk_migration_lock_key UNIQUE (lock_key)
            );
            CREATE INDEX IF NOT EXISTS idx_migration_lock_env_active ON {l}(environment, is_active);

            CREATE TABLE IF NOT EXISTS {a} (
                id               UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                action           VARCHAR(50)  NOT NULL,
                performed_by     VARCHAR(255) NOT NULL,
                performed_at_utc TIMESTAMP    NOT NULL DEFAULT NOW(),
                environment      VARCHAR(50)  NOT NULL,
                details          TEXT
            );
            CREATE INDEX IF NOT EXISTS idx_migration_audit_date ON {a}(performed_at_utc);
            CREATE INDEX IF NOT EXISTS idx_migration_audit_env  ON {a}(environment);
            """;
    }

    public string GetAcquireLockSql(string? module = null)
    {
        ValidateModule(module);
        var l = Q("__migration_lock", module);
        return $"""
            INSERT INTO {l} (id, lock_key, locked_by, locked_at_utc, expires_at_utc, environment, is_active)
            VALUES (@id, @lock_key, @locked_by, @locked_at_utc, @expires_at_utc, @environment, @true)
            ON CONFLICT (lock_key) DO UPDATE
            SET id            = EXCLUDED.id,
                locked_by     = EXCLUDED.locked_by,
                locked_at_utc = EXCLUDED.locked_at_utc,
                expires_at_utc= EXCLUDED.expires_at_utc,
                environment   = EXCLUDED.environment,
                is_active     = EXCLUDED.is_active
            WHERE {l}.is_active = @false OR {l}.expires_at_utc <= @now
            """;
    }

    private static string Q(string name, string? module)
        => string.IsNullOrEmpty(module) ? name : $"\"{ValidateModule(module)}\".{name}";

    private static string ValidateModule(string? module)
    {
        if (string.IsNullOrEmpty(module))
            return module!;
        if (!Regex.IsMatch(module, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            throw new InvalidOperationException($"Invalid module name '{module}'. Must contain only alphanumeric characters and underscores.");
        return module;
    }
}
