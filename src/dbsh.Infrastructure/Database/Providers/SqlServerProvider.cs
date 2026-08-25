using System.Data.Common;
using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;

namespace dbsh.Infrastructure.Database.Providers;

/// <summary>SQL Server provider backed by Microsoft.Data.SqlClient.</summary>
public sealed class SqlServerProvider : IDatabaseProvider
{
    public string Name => "SQL Server";

    public bool SupportsSchemas => true;

    public DbConnection CreateConnection(string connectionString)
        => new SqlConnection(connectionString);

    public DbParameter CreateParameter(string name, object? value)
        => new SqlParameter("@" + name, value ?? DBNull.Value);

    public string GetTableName(string baseName, string? module = null)
        => string.IsNullOrEmpty(module) ? baseName : $"[{ValidateModule(module)}].{baseName}";

    public string GetTrackingSchemaDdl(string? module = null)
    {
        ValidateModule(module);
        var h = Q("__migration_history", module);
        var l = Q("__migration_lock", module);
        var a = Q("__migration_audit", module);
        var schema = string.IsNullOrEmpty(module) ? string.Empty
            : $"IF NOT EXISTS (SELECT 1 FROM [sys].[schemas] WHERE [name] = N'{ValidateModule(module)}')\n    EXEC('CREATE SCHEMA [{ValidateModule(module)}]');\n\n";

        return $"""
            {schema}IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'__migration_history')
            CREATE TABLE {h} (
                [id]                   UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                [version]              NVARCHAR(50)     NOT NULL,
                [name]                 NVARCHAR(255)    NOT NULL,
                [script_name]          NVARCHAR(500)    NOT NULL,
                [script_hash]          NVARCHAR(64)     NOT NULL,
                [migration_type]       NVARCHAR(20)     NOT NULL,
                [category]             NVARCHAR(50)     NOT NULL,
                [executed_by]          NVARCHAR(255)    NOT NULL,
                [executed_at_utc]      DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                [execution_time_ms]    BIGINT           NOT NULL,
                [environment]          NVARCHAR(50)     NOT NULL,
                [status]               NVARCHAR(20)     NOT NULL,
                [rollback_available]   BIT              NOT NULL DEFAULT 0,
                [rollback_script_name] NVARCHAR(500)    NULL,
                [error_message]        NVARCHAR(MAX)    NULL,
                [batch_number]         INT              NOT NULL DEFAULT 1,
                CONSTRAINT [pk_migration_history] PRIMARY KEY CLUSTERED ([id]),
                CONSTRAINT [uk_migration_script_name] UNIQUE ([script_name], [environment])
            );

            IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'uk_migration_version_env' AND [object_id] = OBJECT_ID(N'{h}'))
            CREATE UNIQUE INDEX [uk_migration_version_env] ON {h}([version], [environment]) WHERE [version] <> N'R';

            IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'idx_migration_history_env' AND [object_id] = OBJECT_ID(N'{h}'))
            CREATE INDEX [idx_migration_history_env] ON {h}([environment]);
            IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'idx_migration_history_status' AND [object_id] = OBJECT_ID(N'{h}'))
            CREATE INDEX [idx_migration_history_status] ON {h}([status]);
            IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'idx_migration_history_executed' AND [object_id] = OBJECT_ID(N'{h}'))
            CREATE INDEX [idx_migration_history_executed] ON {h}([executed_at_utc]);
            IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'idx_migration_history_version' AND [object_id] = OBJECT_ID(N'{h}'))
            CREATE INDEX [idx_migration_history_version] ON {h}([version]);

            IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'__migration_lock')
            CREATE TABLE {l} (
                [id]              UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                [lock_key]        NVARCHAR(100)    NOT NULL,
                [locked_by]       NVARCHAR(255)    NOT NULL,
                [locked_at_utc]   DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                [expires_at_utc]  DATETIME2        NOT NULL,
                [environment]     NVARCHAR(50)     NOT NULL,
                [is_active]       BIT              NOT NULL DEFAULT 1,
                CONSTRAINT [pk_migration_lock] PRIMARY KEY CLUSTERED ([id]),
                CONSTRAINT [uk_migration_lock_key] UNIQUE ([lock_key])
            );

            IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'idx_migration_lock_env_active' AND [object_id] = OBJECT_ID(N'{l}'))
            CREATE INDEX [idx_migration_lock_env_active] ON {l}([environment], [is_active]);

            IF NOT EXISTS (SELECT 1 FROM [INFORMATION_SCHEMA].[TABLES] WHERE [TABLE_NAME] = N'__migration_audit')
            CREATE TABLE {a} (
                [id]               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
                [action]           NVARCHAR(50)     NOT NULL,
                [performed_by]     NVARCHAR(255)    NOT NULL,
                [performed_at_utc] DATETIME2        NOT NULL DEFAULT GETUTCDATE(),
                [environment]      NVARCHAR(50)     NOT NULL,
                [details]          NVARCHAR(MAX)    NULL,
                CONSTRAINT [pk_migration_audit] PRIMARY KEY CLUSTERED ([id])
            );

            IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'idx_migration_audit_date' AND [object_id] = OBJECT_ID(N'{a}'))
            CREATE INDEX [idx_migration_audit_date] ON {a}([performed_at_utc]);
            IF NOT EXISTS (SELECT 1 FROM [sys].[indexes] WHERE [name] = N'idx_migration_audit_env' AND [object_id] = OBJECT_ID(N'{a}'))
            CREATE INDEX [idx_migration_audit_env] ON {a}([environment]);
            """;
    }

    public string GetAcquireLockSql(string? module = null)
    {
        ValidateModule(module);
        var l = Q("__migration_lock", module);
        return $"""
            MERGE {l} WITH (HOLDLOCK) AS [target]
            USING (SELECT @lock_key AS [lock_key]) AS [source]
            ON ([target].[lock_key] = [source].[lock_key])
            WHEN MATCHED AND ([target].[is_active] = 0 OR [target].[expires_at_utc] <= @now) THEN
                UPDATE SET [target].[id]            = @id,
                           [target].[locked_by]     = @locked_by,
                           [target].[locked_at_utc] = @locked_at_utc,
                           [target].[expires_at_utc]= @expires_at_utc,
                           [target].[environment]   = @environment,
                           [target].[is_active]     = 1
            WHEN NOT MATCHED THEN
                INSERT ([id], [lock_key], [locked_by], [locked_at_utc], [expires_at_utc], [environment], [is_active])
                VALUES (@id, @lock_key, @locked_by, @locked_at_utc, @expires_at_utc, @environment, 1);
            """;
    }

    private static string Q(string name, string? module)
        => string.IsNullOrEmpty(module) ? $"[dbo].[{name}]" : $"[{ValidateModule(module)}].[{name}]";

    private static string ValidateModule(string? module)
    {
        if (string.IsNullOrEmpty(module))
            return module!;
        if (!Regex.IsMatch(module, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            throw new InvalidOperationException($"Invalid module name '{module}'. Must contain only alphanumeric characters and underscores.");
        return module;
    }
}
