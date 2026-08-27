using System.Data.Common;
using System.Text.RegularExpressions;
using Oracle.ManagedDataAccess.Client;

namespace dbsh.Infrastructure.Database.Providers;

/// <summary>Oracle Database provider backed by Oracle.ManagedDataAccess.Core.</summary>
public sealed class OracleProvider : IDatabaseProvider
{
    public string Name => "Oracle";

    public bool SupportsSchemas => true;

    public DbConnection CreateConnection(string connectionString)
        => new OracleConnection(connectionString);

    public DbParameter CreateParameter(string name, object? value)
        => new OracleParameter(":" + name, value ?? DBNull.Value);

    public string GetTableName(string baseName, string? module = null)
        => string.IsNullOrEmpty(module) ? baseName : $"{ValidateModule(module)}.{baseName}";

    public string GetTrackingSchemaDdl(string? module = null)
    {
        ValidateModule(module);
        var h = Q("__migration_history", module);
        var l = Q("__migration_lock", module);
        var a = Q("__migration_audit", module);
        var schema = string.IsNullOrEmpty(module) ? string.Empty
            : $"BEGIN\n    EXECUTE IMMEDIATE 'CREATE USER {ValidateModule(module)} IDENTIFIED BY temp1234';\n    EXECUTE IMMEDIATE 'GRANT CONNECT, RESOURCE TO {ValidateModule(module)}';\nEXCEPTION\n    WHEN OTHERS THEN\n        IF SQLCODE != -1920 THEN RAISE; END IF;\nEND;\n\n";

        return $"""
            {schema}DECLARE
                v_cnt NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_cnt FROM all_tables WHERE table_name = UPPER('__migration_history') AND owner = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA');
                IF v_cnt = 0 THEN
                    EXECUTE IMMEDIATE 'CREATE TABLE {h} (
                        id                   RAW(16)        DEFAULT SYS_GUID() NOT NULL PRIMARY KEY,
                        version              VARCHAR2(50)   NOT NULL,
                        name                 VARCHAR2(255)  NOT NULL,
                        script_name          VARCHAR2(500)  NOT NULL,
                        script_hash          VARCHAR2(64)   NOT NULL,
                        migration_type       VARCHAR2(20)   NOT NULL,
                        category             VARCHAR2(50)   NOT NULL,
                        executed_by          VARCHAR2(255)  NOT NULL,
                        executed_at_utc      TIMESTAMP      DEFAULT SYSTIMESTAMP NOT NULL,
                        execution_time_ms    NUMBER(19)     NOT NULL,
                        environment          VARCHAR2(50)   NOT NULL,
                        status               VARCHAR2(20)   NOT NULL,
                        rollback_available   NUMBER(1)      DEFAULT 0 NOT NULL,
                        rollback_script_name VARCHAR2(500),
                        error_message        CLOB,
                        batch_number         NUMBER(10)     DEFAULT 1 NOT NULL,
                        CONSTRAINT uk_migration_script_name UNIQUE (script_name, environment)
                    )';
                END IF;
            END;
            /

            DECLARE
                v_cnt NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_cnt FROM all_indexes WHERE index_name = UPPER('uk_migration_version_env') AND table_owner = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA');
                IF v_cnt = 0 THEN
                    EXECUTE IMMEDIATE 'CREATE UNIQUE INDEX uk_migration_version_env ON {h}(version, environment) WHERE version != ''R''';
                END IF;
            END;
            /
            CREATE INDEX IF NOT EXISTS idx_migration_history_env ON {h}(environment);
            CREATE INDEX IF NOT EXISTS idx_migration_history_status ON {h}(status);
            CREATE INDEX IF NOT EXISTS idx_migration_history_executed ON {h}(executed_at_utc);
            CREATE INDEX IF NOT EXISTS idx_migration_history_version ON {h}(version);

            DECLARE
                v_cnt NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_cnt FROM all_tables WHERE table_name = UPPER('__migration_lock') AND owner = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA');
                IF v_cnt = 0 THEN
                    EXECUTE IMMEDIATE 'CREATE TABLE {l} (
                        id              RAW(16)        DEFAULT SYS_GUID() NOT NULL PRIMARY KEY,
                        lock_key        VARCHAR2(100)  NOT NULL,
                        locked_by       VARCHAR2(255)  NOT NULL,
                        locked_at_utc   TIMESTAMP      DEFAULT SYSTIMESTAMP NOT NULL,
                        expires_at_utc  TIMESTAMP      NOT NULL,
                        environment     VARCHAR2(50)   NOT NULL,
                        is_active       NUMBER(1)      DEFAULT 1 NOT NULL,
                        CONSTRAINT uk_migration_lock_key UNIQUE (lock_key)
                    )';
                END IF;
            END;
            /
            CREATE INDEX IF NOT EXISTS idx_migration_lock_env_active ON {l}(environment, is_active);

            DECLARE
                v_cnt NUMBER;
            BEGIN
                SELECT COUNT(*) INTO v_cnt FROM all_tables WHERE table_name = UPPER('__migration_audit') AND owner = SYS_CONTEXT('USERENV', 'CURRENT_SCHEMA');
                IF v_cnt = 0 THEN
                    EXECUTE IMMEDIATE 'CREATE TABLE {a} (
                        id               RAW(16)        DEFAULT SYS_GUID() NOT NULL PRIMARY KEY,
                        action           VARCHAR2(50)   NOT NULL,
                        performed_by     VARCHAR2(255)  NOT NULL,
                        performed_at_utc TIMESTAMP      DEFAULT SYSTIMESTAMP NOT NULL,
                        environment      VARCHAR2(50)   NOT NULL,
                        details          CLOB
                    )';
                END IF;
            END;
            /
            CREATE INDEX IF NOT EXISTS idx_migration_audit_date ON {a}(performed_at_utc);
            CREATE INDEX IF NOT EXISTS idx_migration_audit_env ON {a}(environment);
            """;
    }

    public string GetAcquireLockSql(string? module = null)
    {
        ValidateModule(module);
        var l = Q("__migration_lock", module);
        return $"""
            MERGE INTO {l} target
            USING (SELECT :lock_key AS lock_key FROM dual) source
            ON (target.lock_key = source.lock_key)
            WHEN MATCHED THEN
                UPDATE SET
                    target.id            = :id,
                    target.locked_by     = :locked_by,
                    target.locked_at_utc = :locked_at_utc,
                    target.expires_at_utc= :expires_at_utc,
                    target.environment   = :environment,
                    target.is_active     = 1
                WHERE target.is_active = 0 OR target.expires_at_utc <= :now
            WHEN NOT MATCHED THEN
                INSERT (id, lock_key, locked_by, locked_at_utc, expires_at_utc, environment, is_active)
                VALUES (:id, :lock_key, :locked_by, :locked_at_utc, :expires_at_utc, :environment, 1)
            """;
    }

    private static string Q(string name, string? module)
        => string.IsNullOrEmpty(module) ? name : $"{ValidateModule(module)}.{name}";

    private static string ValidateModule(string? module)
    {
        if (string.IsNullOrEmpty(module))
            return module!;
        if (!Regex.IsMatch(module, @"^[a-zA-Z_][a-zA-Z0-9_]*$"))
            throw new InvalidOperationException($"Invalid module name '{module}'. Must contain only alphanumeric characters and underscores.");
        return module;
    }
}
