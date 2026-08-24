using System.Collections.Concurrent;
using DbEpoch.Core.Entities;
using DbEpoch.Core.Enums;
using DbEpoch.Core.Interfaces;
using DbEpoch.Core.ValueObjects;

namespace DbEpoch.Engine.InMemory;

/// <summary>In-memory <see cref="IMigrationTracker"/> used for tests and offline workflows.</summary>
public sealed class InMemoryMigrationTracker : IMigrationTracker
{
    // Keyed on (environment, script_name) so that multiple repeatable migrations
    // (which all share version "R") can coexist in the same environment. The
    // UNIQUE(version, environment) invariant for versioned migrations is enforced
    // inside AddAsync by evicting any prior row sharing the same version.
    private readonly ConcurrentDictionary<(string Env, string ScriptName), MigrationRecord> _store = new();

    public Task AddAsync(MigrationRecord record, string? module = null, CancellationToken cancellationToken = default)
    {
        record.Environment ??= string.Empty;

        // Versioned migrations are identified by version: a renamed file keeps the
        // same version, so evict any prior record sharing this (env, version).
        // Repeatables (version "R") are identified solely by script_name, so they
        // are left alone here and keyed directly below.
        if (!string.Equals(record.Version, "R", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var key in _store.Keys
                         .Where(k => k.Env == record.Environment
                                     && string.Equals(_store[k].Version, record.Version, StringComparison.OrdinalIgnoreCase))
                         .ToList())
            {
                _store.TryRemove(key, out _);
            }
        }

        _store[(record.Environment, record.ScriptName)] = record;
        return Task.CompletedTask;
    }

    public Task<MigrationRecord?> GetByVersionAsync(string environment, string version, string? module = null, CancellationToken cancellationToken = default)
    {
        var record = _store.Values.FirstOrDefault(r =>
            r.Environment == environment
            && string.Equals(r.Version, version, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(record);
    }

    public Task<IReadOnlyList<MigrationRecord>> GetAllAsync(string environment, string? module = null, CancellationToken cancellationToken = default)
    {
        var items = _store.Values.Where(r => r.Environment == environment)
            .OrderBy(r => r.Version)
            .ToList();
        return Task.FromResult<IReadOnlyList<MigrationRecord>>(items);
    }

    public Task<IReadOnlyList<MigrationRecord>> GetAppliedAsync(string environment, string? module = null, CancellationToken cancellationToken = default)
    {
        var items = _store.Values
            .Where(r => r.Environment == environment && r.Status == MigrationStatus.Completed)
            .OrderByDescending(r => r.Version)
            .ToList();
        return Task.FromResult<IReadOnlyList<MigrationRecord>>(items);
    }

    public Task UpdateStatusAsync(string environment, string version, MigrationStatus status, string? errorMessage, string? module = null, CancellationToken cancellationToken = default)
    {
        foreach (var record in _store.Values.Where(r =>
                     r.Environment == environment
                     && string.Equals(r.Version, version, StringComparison.OrdinalIgnoreCase)))
        {
            record.Status = status;
            record.ErrorMessage = errorMessage;
        }

        return Task.CompletedTask;
    }

    public Task DeleteAsync(string environment, string version, string? module = null, CancellationToken cancellationToken = default)
    {
        var keys = _store.Keys
            .Where(k => k.Env == environment
                        && _store.TryGetValue(k, out var r)
                        && string.Equals(r.Version, version, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var key in keys)
        {
            _store.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string environment, string version, string? module = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_store.Values.Any(r =>
            r.Environment == environment
            && string.Equals(r.Version, version, StringComparison.OrdinalIgnoreCase)));
    }
}

/// <summary>In-memory <see cref="IMigrationLockManager"/> used for tests and offline workflows.</summary>
public sealed class InMemoryMigrationLockManager : IMigrationLockManager
{
    private readonly object _gate = new();
    private readonly Dictionary<(string Env, string Key), MigrationLock> _locks = new();

    public Task<bool> AcquireAsync(string environment, string lockKey, string lockedBy, int timeoutSeconds, string? module = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var key = (environment, lockKey);

        lock (_gate)
        {
            if (_locks.TryGetValue(key, out var existing) && existing.IsActive && existing.ExpiresAtUtc > now)
            {
                return Task.FromResult(false);
            }

            _locks[key] = new MigrationLock
            {
                LockKey = lockKey,
                LockedBy = lockedBy,
                LockedAtUtc = now,
                ExpiresAtUtc = now.AddSeconds(Math.Max(1, timeoutSeconds)),
                Environment = environment,
                IsActive = true
            };
            return Task.FromResult(true);
        }
    }

    public Task ReleaseAsync(string environment, string lockKey, string? lockedBy = null, string? module = null, CancellationToken cancellationToken = default)
    {
        lock (_gate)
        {
            if (_locks.TryGetValue((environment, lockKey), out var existing) && existing.IsActive)
            {
                if (string.IsNullOrEmpty(lockedBy) || string.Equals(existing.LockedBy, lockedBy, StringComparison.Ordinal))
                {
                    existing.IsActive = false;
                }
            }
        }
        return Task.CompletedTask;
    }

    public Task<bool> RenewAsync(string environment, string lockKey, string lockedBy, int timeoutSeconds, string? module = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(lockedBy))
        {
            return Task.FromResult(false);
        }

        lock (_gate)
        {
            if (_locks.TryGetValue((environment, lockKey), out var existing)
                && existing.IsActive
                && string.Equals(existing.LockedBy, lockedBy, StringComparison.Ordinal))
            {
                existing.ExpiresAtUtc = DateTime.UtcNow.AddSeconds(Math.Max(1, timeoutSeconds));
                return Task.FromResult(true);
            }
        }

        return Task.FromResult(false);
    }

    public Task<bool> IsActiveAsync(string environment, string lockKey, string? module = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        lock (_gate)
        {
            return Task.FromResult(_locks.TryGetValue((environment, lockKey), out var existing)
                                   && existing.IsActive
                                   && existing.ExpiresAtUtc > now);
        }
    }
}

/// <summary>In-memory <see cref="IAuditLogger"/> used for tests and offline workflows.</summary>
public sealed class InMemoryAuditLogger : IAuditLogger
{
    private readonly ConcurrentQueue<MigrationAuditEntry> _entries = new();

    public Task LogAsync(MigrationAuditEntry entry, string? module = null, CancellationToken cancellationToken = default)
    {
        _entries.Enqueue(entry);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<MigrationAuditEntry>> GetHistoryAsync(string environment, int limit, string? module = null, CancellationToken cancellationToken = default)
    {
        var items = _entries
            .Where(e => e.Environment == environment)
            .OrderByDescending(e => e.PerformedAtUtc)
            .Take(limit)
            .ToList();
        return Task.FromResult<IReadOnlyList<MigrationAuditEntry>>(items);
    }
}

/// <summary>In-memory <see cref="IEnvironmentProvider"/> used for tests and offline workflows.</summary>
public sealed class InMemoryEnvironmentProvider : IEnvironmentProvider
{
    private readonly HashSet<string> _environments = new(StringComparer.OrdinalIgnoreCase) { "development", "local", "qa", "production" };

    public Task<EnvironmentConfiguration> GetEnvironmentAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new EnvironmentConfiguration
        {
            Name = name,
            Database = new DatabaseEndpoint { Host = "localhost", Port = 5432, Name = $"{name}_db", Schema = "public" },
            Migration = new MigrationEnvironmentSettings { RequireApproval = false, AllowRollback = true, LockTimeoutSeconds = 30, MaxBatchSize = 10 }
        });
    }

    public Task<bool> EnvironmentExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(_environments.Contains(name));
    }

    public Task<IReadOnlyList<string>> GetAvailableEnvironmentsAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<string>>(_environments.OrderBy(e => e).ToArray());
    }
}
