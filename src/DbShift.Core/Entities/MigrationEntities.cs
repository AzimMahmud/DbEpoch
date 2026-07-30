using DbShift.Core.Enums;

namespace DbShift.Core.Entities;

/// <summary>
/// A persisted row in <c>__migration_history</c> describing the execution of one
/// migration script within a single environment.
/// </summary>
public sealed class MigrationRecord
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Version { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ScriptName { get; set; } = string.Empty;
    public string ScriptHash { get; set; } = string.Empty;
    public MigrationType Type { get; set; } = MigrationType.Schema;
    public string Category { get; set; } = string.Empty;
    public string ExecutedBy { get; set; } = string.Empty;
    public DateTime ExecutedAtUtc { get; set; } = DateTime.UtcNow;
    public long ExecutionTimeMs { get; set; }
    public string Environment { get; set; } = string.Empty;
    public MigrationStatus Status { get; set; } = MigrationStatus.Pending;
    public bool RollbackAvailable { get; set; }
    public string? RollbackScriptName { get; set; }
    public string? ErrorMessage { get; set; }
    public int BatchNumber { get; set; } = 1;
    public string Checksum { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>A persisted row in <c>__migration_audit</c> describing a single auditable action.</summary>
public sealed class MigrationAuditEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public AuditAction Action { get; set; }
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAtUtc { get; set; } = DateTime.UtcNow;
    public string Environment { get; set; } = string.Empty;
    public string? Details { get; set; }
}

/// <summary>A persisted row in <c>__migration_lock</c> used for distributed concurrency control.</summary>
public sealed class MigrationLock
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string LockKey { get; set; } = string.Empty;
    public string LockedBy { get; set; } = string.Empty;
    public DateTime LockedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAtUtc { get; set; }
    public string Environment { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
