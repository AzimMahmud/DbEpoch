using System.Data.Common;

namespace DbShift.Infrastructure.Database.Providers;

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

    /// <summary>Returns the complete DDL that creates all three tracking tables, idempotently.</summary>
    string GetTrackingSchemaDdl();

    /// <summary>
    /// Provider-specific atomic UPSERT used to acquire a distributed lock. The statement must
    /// reference these bind parameters (names are stable across providers):
    /// <list type="bullet">
    /// <item><c>@id</c>, <c>@lock_key</c>, <c>@locked_by</c>, <c>@locked_at_utc</c>, <c>@expires_at_utc</c>, <c>@environment</c>, <c>@true</c>, <c>@false</c>, <c>@now</c>.</item>
    /// </list>
    /// The statement must atomically INSERT a new lease when none exists, or UPDATE the existing
    /// row <em>only when it is inactive or expired</em>. Affected rows must be <c>1</c> when the
    /// lease was acquired and <c>0</c> when an active, non-expired lease already exists for the key.
    /// </summary>
    string GetAcquireLockSql();
}
