using System.Data.Common;
using System.Text.RegularExpressions;
using MySqlConnector;

namespace DbEpoch.Infrastructure.Database.Providers;

/// <summary>MySQL / MariaDB provider backed by MySqlConnector.</summary>
public sealed class MySqlProvider : IDatabaseProvider
{
    public string Name => "MySQL";

    public bool SupportsSchemas => false;

    public DbConnection CreateConnection(string connectionString)
        => new MySqlConnection(connectionString);

    public DbParameter CreateParameter(string name, object? value)
        => new MySqlParameter("@" + name, value ?? DBNull.Value);

    public string GetTableName(string baseName, string? module = null)
        => string.IsNullOrEmpty(module) ? $"`{baseName}`" : $"`{ValidateModule(module)}__{baseName}`";

    public string GetTrackingSchemaDdl(string? module = null)
    {
        var h = GetTableName("__migration_history", module);
        var l = GetTableName("__migration_lock", module);
        var a = GetTableName("__migration_audit", module);

        return $"""
            CREATE TABLE IF NOT EXISTS {h} (
                `id`                   CHAR(36)     NOT NULL PRIMARY KEY,
                `version`              VARCHAR(50)  NOT NULL,
                `name`                 VARCHAR(255) NOT NULL,
                `script_name`          VARCHAR(500) NOT NULL,
                `script_hash`          VARCHAR(64)  NOT NULL,
                `migration_type`       VARCHAR(20)  NOT NULL,
                `category`             VARCHAR(50)  NOT NULL,
                `executed_by`          VARCHAR(255) NOT NULL,
                `executed_at_utc`      DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
                `execution_time_ms`    BIGINT       NOT NULL,
                `environment`          VARCHAR(50)  NOT NULL,
                `status`               VARCHAR(20)  NOT NULL,
                `rollback_available`   TINYINT(1)   NOT NULL DEFAULT 0,
                `rollback_script_name` VARCHAR(500) NULL,
                `error_message`        LONGTEXT     NULL,
                `batch_number`         INT          NOT NULL DEFAULT 1,
                `version_key`          VARCHAR(50)  GENERATED ALWAYS AS (IF(`version` = 'R', NULL, `version`)) STORED,
                UNIQUE KEY `uk_migration_script_name` (`script_name`, `environment`),
                UNIQUE KEY `uk_migration_version_env` (`version_key`, `environment`),
                INDEX `idx_migration_history_env` (`environment`),
                INDEX `idx_migration_history_status` (`status`),
                INDEX `idx_migration_history_executed` (`executed_at_utc`),
                INDEX `idx_migration_history_version` (`version`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS {l} (
                `id`              CHAR(36)     NOT NULL PRIMARY KEY,
                `lock_key`        VARCHAR(100) NOT NULL,
                `locked_by`       VARCHAR(255) NOT NULL,
                `locked_at_utc`   DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
                `expires_at_utc`  DATETIME     NOT NULL,
                `environment`     VARCHAR(50)  NOT NULL,
                `is_active`       TINYINT(1)   NOT NULL DEFAULT 1,
                UNIQUE KEY `uk_migration_lock_key` (`lock_key`),
                INDEX `idx_migration_lock_env_active` (`environment`, `is_active`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

            CREATE TABLE IF NOT EXISTS {a} (
                `id`               CHAR(36)     NOT NULL PRIMARY KEY,
                `action`           VARCHAR(50)  NOT NULL,
                `performed_by`     VARCHAR(255) NOT NULL,
                `performed_at_utc` DATETIME     NOT NULL DEFAULT (UTC_TIMESTAMP()),
                `environment`      VARCHAR(50)  NOT NULL,
                `details`          LONGTEXT     NULL,
                INDEX `idx_migration_audit_date` (`performed_at_utc`),
                INDEX `idx_migration_audit_env` (`environment`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
            """;
    }

    public string GetAcquireLockSql(string? module = null)
    {
        ValidateModule(module);
        var l = GetTableName("__migration_lock", module);
        return $"""
            INSERT INTO {l} (`id`, `lock_key`, `locked_by`, `locked_at_utc`, `expires_at_utc`, `environment`, `is_active`)
            VALUES (@id, @lock_key, @locked_by, @locked_at_utc, @expires_at_utc, @environment, 1)
            ON DUPLICATE KEY UPDATE
                `id`             = IF(`is_active` = 0 OR `expires_at_utc` <= @now, VALUES(`id`), `id`),
                `locked_by`      = IF(`is_active` = 0 OR `expires_at_utc` <= @now, VALUES(`locked_by`), `locked_by`),
                `locked_at_utc`  = IF(`is_active` = 0 OR `expires_at_utc` <= @now, VALUES(`locked_at_utc`), `locked_at_utc`),
                `expires_at_utc` = IF(`is_active` = 0 OR `expires_at_utc` <= @now, VALUES(`expires_at_utc`), `expires_at_utc`),
                `environment`    = IF(`is_active` = 0 OR `expires_at_utc` <= @now, VALUES(`environment`), `environment`),
                `is_active`      = IF(`is_active` = 0 OR `expires_at_utc` <= @now, 1, `is_active`)
            """;
    }

    private static string ValidateModule(string? module)
    {
        if (string.IsNullOrEmpty(module))
            return module!;
        if (!Regex.IsMatch(module, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            throw new InvalidOperationException($"Invalid module name '{module}'. Must contain only alphanumeric characters and underscores.");
        return module;
    }
}
