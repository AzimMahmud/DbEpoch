using System.Data.Common;
using System.Runtime.CompilerServices;
using DbShift.Core.Entities;
using DbShift.Core.Enums;
using DbShift.Core.Interfaces;
using DbShift.Infrastructure.Database.Providers;

namespace DbShift.Infrastructure.Database;

/// <summary>
/// Provider-agnostic migration history tracker. Uses <see cref="System.Data.Common"/>
/// abstractions so the same code works against PostgreSQL, SQL Server, MySQL, and SQLite.
/// </summary>
public sealed class RelationalMigrationTracker : IMigrationTracker
{
    private readonly IDatabaseProvider _provider;
    private readonly string _connectionString;
    private readonly string _historyTable;

    public RelationalMigrationTracker(IDatabaseProvider provider, string connectionString, string? module = null)
    {
        _provider = provider;
        _connectionString = connectionString;
        _historyTable = provider.GetTableName("__migration_history", module);
    }

    public async Task AddAsync(MigrationRecord record, string? module = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // The DELETE+INSERT pair must be atomic: if INSERT fails (e.g. constraint
        // violation) we must not have already destroyed the prior history row.
        //
        // The replacement key depends on the migration kind:
        //   - Versioned migrations are identified by (version, environment): a renamed
        //     file keeps the same version, so we DELETE by version to preserve the
        //     UNIQUE(version, environment) invariant.
        //   - Repeatable migrations all share version "R" and are identified by
        //     (script_name, environment): we DELETE by script_name so multiple
        //     repeatables can coexist in the same environment.
        var deleteKey = string.Equals(record.Version, "R", StringComparison.OrdinalIgnoreCase)
            ? "script_name = @script_name"
            : "version = @version";

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = $"""
            DELETE FROM {_historyTable} WHERE environment = @environment AND {deleteKey};

            INSERT INTO {_historyTable}
                (id, version, name, script_name, script_hash, migration_type, category, executed_by,
                 executed_at_utc, execution_time_ms, environment, status, rollback_available,
                 rollback_script_name, error_message, batch_number)
            VALUES
                (@id, @version, @name, @script_name, @script_hash, @migration_type, @category, @executed_by,
                 @executed_at_utc, @execution_time_ms, @environment, @status, @rollback_available,
                 @rollback_script_name, @error_message, @batch_number)
            """;
        AddParameters(command, record);
        await command.ExecuteNonQueryAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<MigrationRecord?> GetByVersionAsync(string environment, string version, string? module = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {_historyTable} WHERE environment = @environment AND version = @version";
        command.Parameters.Add(_provider.CreateParameter("environment", environment));
        command.Parameters.Add(_provider.CreateParameter("version", version));

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<MigrationRecord>> GetAllAsync(string environment, string? module = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {_historyTable} WHERE environment = @environment ORDER BY version";
        command.Parameters.Add(_provider.CreateParameter("environment", environment));

        return await ReadListAsync(command, cancellationToken);
    }

    public async Task<IReadOnlyList<MigrationRecord>> GetAppliedAsync(string environment, string? module = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {_historyTable} WHERE environment = @environment AND status = @status ORDER BY version DESC";
        command.Parameters.Add(_provider.CreateParameter("environment", environment));
        command.Parameters.Add(_provider.CreateParameter("status", MigrationStatus.Completed.ToString()));

        return await ReadListAsync(command, cancellationToken);
    }

    public async Task UpdateStatusAsync(string environment, string version, MigrationStatus status, string? errorMessage, string? module = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"UPDATE {_historyTable} SET status = @status, error_message = @error_message WHERE environment = @environment AND version = @version";
        command.Parameters.Add(_provider.CreateParameter("status", status.ToString()));
        command.Parameters.Add(_provider.CreateParameter("error_message", (object?)errorMessage ?? DBNull.Value));
        command.Parameters.Add(_provider.CreateParameter("environment", environment));
        command.Parameters.Add(_provider.CreateParameter("version", version));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteAsync(string environment, string version, string? module = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {_historyTable} WHERE environment = @environment AND version = @version";
        command.Parameters.Add(_provider.CreateParameter("environment", environment));
        command.Parameters.Add(_provider.CreateParameter("version", version));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string environment, string version, string? module = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {_historyTable} WHERE environment = @environment AND version = @version) THEN 1 ELSE 0 END";
        command.Parameters.Add(_provider.CreateParameter("environment", environment));
        command.Parameters.Add(_provider.CreateParameter("version", version));

        var scalar = await command.ExecuteScalarAsync(cancellationToken);
        return scalar is not null && Convert.ToBoolean(scalar);
    }

    private async Task<IReadOnlyList<MigrationRecord>> ReadListAsync(DbCommand command, CancellationToken cancellationToken)
    {
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var list = new List<MigrationRecord>();
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(Map(reader));
        }
        return list;
    }

    private void AddParameters(DbCommand command, MigrationRecord r)
    {
        command.Parameters.Add(_provider.CreateParameter("id", r.Id));
        command.Parameters.Add(_provider.CreateParameter("version", r.Version));
        command.Parameters.Add(_provider.CreateParameter("name", r.Name));
        command.Parameters.Add(_provider.CreateParameter("script_name", r.ScriptName));
        command.Parameters.Add(_provider.CreateParameter("script_hash", r.ScriptHash));
        command.Parameters.Add(_provider.CreateParameter("migration_type", r.Type.ToString()));
        command.Parameters.Add(_provider.CreateParameter("category", r.Category));
        command.Parameters.Add(_provider.CreateParameter("executed_by", r.ExecutedBy));
        command.Parameters.Add(_provider.CreateParameter("executed_at_utc", r.ExecutedAtUtc));
        command.Parameters.Add(_provider.CreateParameter("execution_time_ms", r.ExecutionTimeMs));
        command.Parameters.Add(_provider.CreateParameter("environment", r.Environment));
        command.Parameters.Add(_provider.CreateParameter("status", r.Status.ToString()));
        command.Parameters.Add(_provider.CreateParameter("rollback_available", r.RollbackAvailable));
        command.Parameters.Add(_provider.CreateParameter("rollback_script_name", (object?)r.RollbackScriptName ?? DBNull.Value));
        command.Parameters.Add(_provider.CreateParameter("error_message", (object?)r.ErrorMessage ?? DBNull.Value));
        command.Parameters.Add(_provider.CreateParameter("batch_number", r.BatchNumber));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static MigrationRecord Map(DbDataReader r) => new()
    {
        Id = r.GetGuid(r.GetOrdinal("id")),
        Version = r.GetString(r.GetOrdinal("version")),
        Name = r.GetString(r.GetOrdinal("name")),
        ScriptName = r.GetString(r.GetOrdinal("script_name")),
        ScriptHash = r.GetString(r.GetOrdinal("script_hash")),
        Type = Enum.TryParse<MigrationType>(r.GetString(r.GetOrdinal("migration_type")), true, out var t) ? t : MigrationType.Schema,
        Category = r.GetString(r.GetOrdinal("category")),
        ExecutedBy = r.GetString(r.GetOrdinal("executed_by")),
        ExecutedAtUtc = r.GetDateTime(r.GetOrdinal("executed_at_utc")),
        ExecutionTimeMs = r.GetInt64(r.GetOrdinal("execution_time_ms")),
        Environment = r.GetString(r.GetOrdinal("environment")),
        Status = Enum.TryParse<MigrationStatus>(r.GetString(r.GetOrdinal("status")), true, out var s) ? s : MigrationStatus.Pending,
        RollbackAvailable = r.GetBoolean(r.GetOrdinal("rollback_available")),
        RollbackScriptName = r.IsDBNull(r.GetOrdinal("rollback_script_name")) ? null : r.GetString(r.GetOrdinal("rollback_script_name")),
        ErrorMessage = r.IsDBNull(r.GetOrdinal("error_message")) ? null : r.GetString(r.GetOrdinal("error_message")),
        BatchNumber = r.GetInt32(r.GetOrdinal("batch_number"))
    };
}
