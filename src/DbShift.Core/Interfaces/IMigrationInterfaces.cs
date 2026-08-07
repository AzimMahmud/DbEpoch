using DbShift.Core.Entities;
using DbShift.Core.Enums;
using DbShift.Core.ValueObjects;

namespace DbShift.Core.Interfaces;

/// <summary>Persists and queries the migration history for an environment.</summary>
public interface IMigrationTracker
{
    Task AddAsync(MigrationRecord record, string? module = null, CancellationToken cancellationToken = default);
    Task<MigrationRecord?> GetByVersionAsync(string environment, string version, string? module = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MigrationRecord>> GetAllAsync(string environment, string? module = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MigrationRecord>> GetAppliedAsync(string environment, string? module = null, CancellationToken cancellationToken = default);
    Task UpdateStatusAsync(string environment, string version, MigrationStatus status, string? errorMessage, string? module = null, CancellationToken cancellationToken = default);
    Task DeleteAsync(string environment, string version, string? module = null, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string environment, string version, string? module = null, CancellationToken cancellationToken = default);
}

/// <summary>Manages distributed locks to serialise concurrent migration runs.</summary>
public interface IMigrationLockManager
{
    /// <summary>
    /// Atomically acquire a lease on <paramref name="lockKey"/> for <paramref name="environment"/>.
    /// Returns <langword="false"/> if an active, non-expired lease is held by another owner.
    /// Re-acquisition after release (or after expiry of a prior lease for the same key) is supported.
    /// </summary>
    Task<bool> AcquireAsync(string environment, string lockKey, string lockedBy, int timeoutSeconds, string? module = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Release the lease on <paramref name="lockKey"/>. Only the owner recorded by <see cref="AcquireAsync"/>
    /// (or any caller when <paramref name="lockedBy"/> is null/empty for backwards compatibility) may release it.
    /// </summary>
    Task ReleaseAsync(string environment, string lockKey, string? lockedBy = null, string? module = null, CancellationToken cancellationToken = default);

    /// <summary>Extend the lease on <paramref name="lockKey"/> by <paramref name="timeoutSeconds"/>. Only the recorded owner may renew. Returns <langword="false"/> if the lease is not held by <paramref name="lockedBy"/>.</summary>
    Task<bool> RenewAsync(string environment, string lockKey, string lockedBy, int timeoutSeconds, string? module = null, CancellationToken cancellationToken = default);

    Task<bool> IsActiveAsync(string environment, string lockKey, string? module = null, CancellationToken cancellationToken = default);
}

/// <summary>Records auditable migration actions.</summary>
public interface IAuditLogger
{
    Task LogAsync(MigrationAuditEntry entry, string? module = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MigrationAuditEntry>> GetHistoryAsync(string environment, int limit, string? module = null, CancellationToken cancellationToken = default);
}

/// <summary>Resolves environment-specific configuration.</summary>
public interface IEnvironmentProvider
{
    Task<EnvironmentConfiguration> GetEnvironmentAsync(string name, CancellationToken cancellationToken = default);
    Task<bool> EnvironmentExistsAsync(string name, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<string>> GetAvailableEnvironmentsAsync(CancellationToken cancellationToken = default);
}

/// <summary>Executes SQL scripts against the target database.</summary>
public interface IMigrationScriptExecutor
{
    Task<ScriptExecutionResult> ExecuteAsync(string connectionString, string sql, int timeoutSeconds, CancellationToken cancellationToken = default);
    Task EnsureTrackingSchemaAsync(string connectionString, string? module = null, CancellationToken cancellationToken = default);
}

/// <summary>Loads global and environment configuration from the file system.</summary>
public interface IConfigLoader
{
    MigrationConfiguration LoadMigrationConfiguration(string? basePath = null);
    EnvironmentConfiguration LoadEnvironment(string name, string? basePath = null);
    IReadOnlyList<string> GetAvailableEnvironments(string? basePath = null);
}

/// <summary>Outcome of executing a single SQL script.</summary>
public sealed class ScriptExecutionResult
{
    public bool IsSuccess { get; set; }
    public long ElapsedMs { get; set; }
    public string? ErrorMessage { get; set; }
}
