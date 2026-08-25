using System.Data.Common;

namespace dbsh.Infrastructure.Database.Providers;

/// <summary>
/// Abstraction over a specific database engine. Provides connection creation,
/// parameter construction, and engine-specific tracking-schema DDL so that the
/// tracker, lock manager, audit logger, and executor can stay fully generic.
/// </summary>
public interface IDatabaseProvider
{
    /// <summary>Human-readable name (e.g. "PostgreSQL", "SQL Server").</summary>
    string Name { get; }

    /// <summary>Creates a ready-to-open <see cref="DbConnection"/> for this engine.</summary>
    DbConnection CreateConnection(string connectionString);

    /// <summary>Creates a parameter, converting null values to <see cref="DBNull.Value"/>.</summary>
    DbParameter CreateParameter(string name, object? value);

    /// <summary>
    /// Returns the complete DDL that creates all three tracking tables, idempotently.
    /// When <paramref name="module"/> is non-null, tables are created in a module-specific
    /// schema (PostgreSQL/SQL Server) or with a module prefix (MySQL/SQLite).
    /// </summary>
    string GetTrackingSchemaDdl(string? module = null);

    /// <summary>
    /// Provider-specific atomic UPSERT used to acquire a distributed lock. The statement must
    /// reference these bind parameters (names are stable across providers):
    /// <list type="bullet">
    /// <item><c>@id</c>, <c>@lock_key</c>, <c>@locked_by</c>, <c>@locked_at_utc</c>, <c>@expires_at_utc</c>, <c>@environment</c>, <c>@true</c>, <c>@false</c>, <c>@now</c>.</item>
    /// </list>
    /// The statement must atomically INSERT a new lease when none exists, or UPDATE the existing
    /// row <em>only when it is inactive or expired</em>. Affected rows must be <c>1</c> when the
    /// lease was acquired and <c>0</c> when an active, non-expired lease already exists for the key.
    /// When <paramref name="module"/> is non-null, the table reference is schema-qualified or prefixed.
    /// </summary>
    string GetAcquireLockSql(string? module = null);

    /// <summary>
    /// Returns the correctly qualified table name for the given <paramref name="baseName"/>
    /// (e.g. "__migration_history"). When <paramref name="module"/> is non-null, the name
    /// includes the module schema or prefix. When null, the bare name is returned.
    /// </summary>
    string GetTableName(string baseName, string? module = null);

    /// <summary>
    /// Returns <see langword="true"/> when this provider supports native database schemas
    /// (PostgreSQL, SQL Server). MySQL and SQLite return <see langword="false"/>.
    /// </summary>
    bool SupportsSchemas { get; }
}
