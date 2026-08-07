using System.Data.Common;
using System.Diagnostics;
using DbShift.Core.Interfaces;
using DbShift.Infrastructure.Database.Providers;

namespace DbShift.Infrastructure.Database;

/// <summary>
/// Executes raw SQL migration scripts inside an idempotent transaction.
/// Uses <see cref="System.Data.Common"/> abstractions so it works with any provider.
/// </summary>
public sealed class RelationalMigrationExecutor : IMigrationScriptExecutor
{
    private readonly IDatabaseProvider _provider;
    private readonly string? _module;

    public RelationalMigrationExecutor(IDatabaseProvider provider, string? module = null)
    {
        _provider = provider;
        _module = module;
    }

    public async Task<ScriptExecutionResult> ExecuteAsync(string connectionString, string sql, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await using var connection = _provider.CreateConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await using var command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandTimeout = Math.Max(1, timeoutSeconds);
                command.CommandText = sql;
                await command.ExecuteNonQueryAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                // Explicit rollback is more defensive than relying on transaction-dispose semantics,
                // which are subtle on some providers when the connection is in a faulted state.
                try { await transaction.RollbackAsync(cancellationToken); }
                catch { /* rollback failure is not interesting compared to the original error */ }
                throw;
            }

            return new ScriptExecutionResult { IsSuccess = true, ElapsedMs = stopwatch.ElapsedMilliseconds };
        }
        catch (DbException ex)
        {
            // Include the exception type so callers and logs can distinguish e.g. a timeout from
            // a constraint violation without losing information the way ex.Message alone would.
            return new ScriptExecutionResult
            {
                IsSuccess = false,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                ErrorMessage = $"{ex.GetType().Name}: {ex.Message}"
            };
        }
    }

    public async Task EnsureTrackingSchemaAsync(string connectionString, string? module = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _provider.CreateConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandTimeout = 300;
        command.CommandText = _provider.GetTrackingSchemaDdl(module ?? _module);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
