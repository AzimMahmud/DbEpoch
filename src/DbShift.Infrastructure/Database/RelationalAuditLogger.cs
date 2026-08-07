using System.Data.Common;
using DbShift.Core.Entities;
using DbShift.Core.Enums;
using DbShift.Core.Interfaces;
using DbShift.Infrastructure.Database.Providers;

namespace DbShift.Infrastructure.Database;

/// <summary>Provider-agnostic audit logger that records actions in <c>__migration_audit</c>.</summary>
public sealed class RelationalAuditLogger : IAuditLogger
{
    private readonly IDatabaseProvider _provider;
    private readonly string _connectionString;
    private readonly string _auditTable;

    public RelationalAuditLogger(IDatabaseProvider provider, string connectionString, string? module = null)
    {
        _provider = provider;
        _connectionString = connectionString;
        _auditTable = provider.GetTableName("__migration_audit", module);
    }

    public async Task LogAsync(MigrationAuditEntry entry, string? module = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO {_auditTable}
                (id, action, performed_by, performed_at_utc, environment, details)
            VALUES
                (@id, @action, @performed_by, @performed_at_utc, @environment, @details)
            """;
        command.Parameters.Add(_provider.CreateParameter("id", entry.Id));
        command.Parameters.Add(_provider.CreateParameter("action", entry.Action.ToString()));
        command.Parameters.Add(_provider.CreateParameter("performed_by", entry.PerformedBy));
        command.Parameters.Add(_provider.CreateParameter("performed_at_utc", entry.PerformedAtUtc));
        command.Parameters.Add(_provider.CreateParameter("environment", entry.Environment));
        command.Parameters.Add(_provider.CreateParameter("details", (object?)entry.Details ?? DBNull.Value));

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<MigrationAuditEntry>> GetHistoryAsync(string environment, int limit, string? module = null, CancellationToken cancellationToken = default)
    {
        await using var connection = _provider.CreateConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT * FROM {_auditTable} WHERE environment = @environment ORDER BY performed_at_utc DESC";
        command.Parameters.Add(_provider.CreateParameter("environment", environment));

        var entries = new List<MigrationAuditEntry>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var detailsOrdinal = reader.GetOrdinal("details");
            entries.Add(new MigrationAuditEntry
            {
                Id = reader.GetGuid(reader.GetOrdinal("id")),
                Action = Enum.TryParse<AuditAction>(reader.GetString(reader.GetOrdinal("action")), true, out var a) ? a : AuditAction.Validate,
                PerformedBy = reader.GetString(reader.GetOrdinal("performed_by")),
                PerformedAtUtc = reader.GetDateTime(reader.GetOrdinal("performed_at_utc")),
                Environment = reader.GetString(reader.GetOrdinal("environment")),
                Details = reader.IsDBNull(detailsOrdinal) ? null : reader.GetString(detailsOrdinal)
            });
        }

        // Apply the limit in C# so we don't need engine-specific TOP / FETCH / LIMIT syntax.
        return entries.Take(Math.Max(1, limit)).ToList();
    }
}
