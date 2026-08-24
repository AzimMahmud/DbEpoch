using System.Data.Common;
using DbEpoch.Core.Interfaces;
using DbEpoch.Infrastructure.Database.Providers;

namespace DbEpoch.Infrastructure.Database;

/// <summary>
/// Row-based distributed lock manager backed by the <c>__migration_lock</c> table.
/// Uses a provider-specific atomic UPSERT (see <see cref="IDatabaseProvider.GetAcquireLockSql"/>)
/// so that a lease may be re-acquired after release or expiry, with the UNIQUE constraint on
/// <c>lock_key</c> providing race-free acquisition across concurrent callers.
/// </summary>
public sealed class RelationalMigrationLockManager : IMigrationLockManager
{
    private readonly IDatabaseProvider _provider;
    private readonly string _connectionString;
    private readonly string _lockTable;

    public RelationalMigrationLockManager(IDatabaseProvider provider, string connectionString, string? module = null)
    {
        _provider = provider;
        _connectionString = connectionString;
        _lockTable = provider.GetTableName("__migration_lock", module);
    }

    public async Task<bool> AcquireAsync(string environment, string lockKey, string lockedBy, int timeoutSeconds, string? module = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiresAt = now.AddSeconds(Math.Max(1, timeoutSeconds));

        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = _provider.GetAcquireLockSql(module);
        command.Parameters.Add(_provider.CreateParameter("id", Guid.NewGuid()));
        command.Parameters.Add(_provider.CreateParameter("lock_key", lockKey));
        command.Parameters.Add(_provider.CreateParameter("locked_by", lockedBy));
        command.Parameters.Add(_provider.CreateParameter("locked_at_utc", now));
        command.Parameters.Add(_provider.CreateParameter("expires_at_utc", expiresAt));
        command.Parameters.Add(_provider.CreateParameter("environment", environment));
        command.Parameters.Add(_provider.CreateParameter("true", true));
        command.Parameters.Add(_provider.CreateParameter("false", false));
        command.Parameters.Add(_provider.CreateParameter("now", now));

        // Best-effort cleanup of long-dead rows so the table does not grow unbounded.
        // Failure here must not block acquisition; the UPSERT below is the source of truth.
        await PurgeExpiredAsync(connection, cancellationToken);

        try
        {
            var affected = await command.ExecuteNonQueryAsync(cancellationToken);
            return affected > 0;
        }
        catch (DbException)
        {
            // Any provider-side failure (unique violation on a race we lost, transient, etc.)
            // is treated as "could not acquire".
            return false;
        }
    }

    public async Task ReleaseAsync(string environment, string lockKey, string? lockedBy = null, string? module = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        if (string.IsNullOrEmpty(lockedBy))
        {
            command.CommandText = $"""
                UPDATE {_lockTable}
                SET is_active = @false
                WHERE lock_key = @lock_key AND environment = @environment AND is_active = @true
                """;
        }
        else
        {
            // Owner-scoped release: only the recorded owner may release the lease.
            command.CommandText = $"""
                UPDATE {_lockTable}
                SET is_active = @false
                WHERE lock_key = @lock_key AND environment = @environment AND is_active = @true AND locked_by = @locked_by
                """;
            command.Parameters.Add(_provider.CreateParameter("locked_by", lockedBy));
        }
        command.Parameters.Add(_provider.CreateParameter("lock_key", lockKey));
        command.Parameters.Add(_provider.CreateParameter("environment", environment));
        command.Parameters.Add(_provider.CreateParameter("true", true));
        command.Parameters.Add(_provider.CreateParameter("false", false));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> RenewAsync(string environment, string lockKey, string lockedBy, int timeoutSeconds, string? module = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(lockedBy))
        {
            return false;
        }

        var newExpiry = DateTime.UtcNow.AddSeconds(Math.Max(1, timeoutSeconds));

        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            UPDATE {_lockTable}
            SET expires_at_utc = @expires_at_utc
            WHERE lock_key = @lock_key AND environment = @environment AND is_active = @true AND locked_by = @locked_by
            """;
        command.Parameters.Add(_provider.CreateParameter("expires_at_utc", newExpiry));
        command.Parameters.Add(_provider.CreateParameter("lock_key", lockKey));
        command.Parameters.Add(_provider.CreateParameter("environment", environment));
        command.Parameters.Add(_provider.CreateParameter("true", true));
        command.Parameters.Add(_provider.CreateParameter("locked_by", lockedBy));

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);
        return affected > 0;
    }

    public async Task<bool> IsActiveAsync(string environment, string lockKey, string? module = null, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;

        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            SELECT CASE WHEN EXISTS (
                SELECT 1 FROM {_lockTable}
                WHERE lock_key = @lock_key AND environment = @environment AND is_active = @true AND expires_at_utc > @now
            ) THEN 1 ELSE 0 END
            """;
        command.Parameters.Add(_provider.CreateParameter("lock_key", lockKey));
        command.Parameters.Add(_provider.CreateParameter("environment", environment));
        command.Parameters.Add(_provider.CreateParameter("true", true));
        command.Parameters.Add(_provider.CreateParameter("now", now));

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null && Convert.ToBoolean(scalar);
    }

    private async Task PurgeExpiredAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText = $"DELETE FROM {_lockTable} WHERE is_active = @false AND expires_at_utc < @cutoff";
            command.Parameters.Add(_provider.CreateParameter("false", false));
            command.Parameters.Add(_provider.CreateParameter("cutoff", DateTime.UtcNow.AddDays(-30)));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (DbException)
        {
            // Purge is best-effort; ignore.
        }
    }
}
