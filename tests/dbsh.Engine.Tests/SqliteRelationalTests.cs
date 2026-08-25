using System.Data.Common;
using dbsh.Core.Entities;
using dbsh.Core.Enums;
using dbsh.Infrastructure.Database;
using dbsh.Infrastructure.Database.Providers;
using Xunit;

namespace dbsh.Engine.Tests;

/// <summary>
/// Integration tests for the relational (SQLite-backed) implementations of
/// <see cref="IMigrationTracker"/>, <see cref="IMigrationLockManager"/>, and
/// <see cref="IAuditLogger"/>. Uses a file-based SQLite database so no external
/// infrastructure is required, while still exercising the real ADO.NET code paths.
/// </summary>
public class SqliteRelationalTests : IDisposable
{
    private readonly string _connectionString;
    private readonly SqliteProvider _provider = new();
    private DbConnection? _keepAliveConnection;

    public SqliteRelationalTests()
    {
        // Each test class instance gets its own private database file so tests are isolated.
        var dbPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"dbsh-sqlite-test-{Guid.NewGuid():N}.db");
        _connectionString = $"Data Source={dbPath}";
    }

    public void Dispose()
    {
        if (_keepAliveConnection is not null)
        {
            _keepAliveConnection.Close();
            _keepAliveConnection.Dispose();
        }

        // Extract the file path from the connection string and clean up.
        try
        {
            var builder = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(_connectionString);
            if (File.Exists(builder.DataSource))
            {
                File.Delete(builder.DataSource);
            }
        }
        catch { }
    }

    private async Task EnsureSchemaAsync()
    {
        if (_keepAliveConnection is not null) return;

        _keepAliveConnection = _provider.CreateConnection(_connectionString);
        await _keepAliveConnection.OpenAsync();

        await using var cmd = _keepAliveConnection.CreateCommand();
        cmd.CommandText = _provider.GetTrackingSchemaDdl();
        await cmd.ExecuteNonQueryAsync();
    }

    // ── MigrationTracker ──────────────────────────────────────────────

    [Fact]
    public async Task Tracker_AddAndGetByVersion()
    {
        await EnsureSchemaAsync();
        var tracker = new RelationalMigrationTracker(_provider, _connectionString);

        var record = new MigrationRecord
        {
            Version = "001",
            Name = "CreateUsers",
            ScriptName = "V001__CreateUsers.sql",
            ScriptHash = "abc123",
            Type = MigrationType.Schema,
            Category = "Schema",
            ExecutedBy = "tester",
            Environment = "development",
            Status = MigrationStatus.Completed
        };

        await tracker.AddAsync(record);
        var fetched = await tracker.GetByVersionAsync("development", "001");

        Assert.NotNull(fetched);
        Assert.Equal("001", fetched!.Version);
        Assert.Equal("CreateUsers", fetched.Name);
        Assert.Equal(MigrationStatus.Completed, fetched.Status);
    }

    [Fact]
    public async Task Tracker_GetByVersion_NotFound_ReturnsNull()
    {
        await EnsureSchemaAsync();
        var tracker = new RelationalMigrationTracker(_provider, _connectionString);

        var fetched = await tracker.GetByVersionAsync("development", "999");
        Assert.Null(fetched);
    }

    [Fact]
    public async Task Tracker_GetAll_ReturnsAllForEnvironment()
    {
        await EnsureSchemaAsync();
        var tracker = new RelationalMigrationTracker(_provider, _connectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "001", ScriptName = "a.sql", ScriptHash = "h1", Environment = "dev", Status = MigrationStatus.Completed });
        await tracker.AddAsync(new MigrationRecord { Version = "002", ScriptName = "b.sql", ScriptHash = "h2", Environment = "dev", Status = MigrationStatus.Failed });
        await tracker.AddAsync(new MigrationRecord { Version = "003", ScriptName = "c.sql", ScriptHash = "h3", Environment = "prod", Status = MigrationStatus.Completed });

        var devRecords = await tracker.GetAllAsync("dev");
        Assert.Equal(2, devRecords.Count);

        var prodRecords = await tracker.GetAllAsync("prod");
        Assert.Single(prodRecords);
    }

    [Fact]
    public async Task Tracker_GetApplied_ReturnsOnlyCompleted()
    {
        await EnsureSchemaAsync();
        var tracker = new RelationalMigrationTracker(_provider, _connectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "001", ScriptName = "a.sql", ScriptHash = "h1", Environment = "dev", Status = MigrationStatus.Completed });
        await tracker.AddAsync(new MigrationRecord { Version = "002", ScriptName = "b.sql", ScriptHash = "h2", Environment = "dev", Status = MigrationStatus.Failed });
        await tracker.AddAsync(new MigrationRecord { Version = "003", ScriptName = "c.sql", ScriptHash = "h3", Environment = "dev", Status = MigrationStatus.Completed });

        var applied = await tracker.GetAppliedAsync("dev");
        Assert.Equal(2, applied.Count);
        Assert.All(applied, r => Assert.Equal(MigrationStatus.Completed, r.Status));
    }

    [Fact]
    public async Task Tracker_Add_Idempotent_ReplacesExisting()
    {
        await EnsureSchemaAsync();
        var tracker = new RelationalMigrationTracker(_provider, _connectionString);

        var record = new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__Old.sql",
            ScriptHash = "old_hash",
            Environment = "dev",
            Status = MigrationStatus.Completed,
            ExecutedBy = "user1"
        };

        await tracker.AddAsync(record);

        var updated = new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__New.sql",
            ScriptHash = "new_hash",
            Environment = "dev",
            Status = MigrationStatus.Failed,
            ExecutedBy = "user2"
        };

        await tracker.AddAsync(updated);

        var fetched = await tracker.GetByVersionAsync("dev", "001");
        Assert.NotNull(fetched);
        Assert.Equal("new_hash", fetched!.ScriptHash);
        Assert.Equal(MigrationStatus.Failed, fetched.Status);
        Assert.Equal("user2", fetched.ExecutedBy);
    }

    [Fact]
    public async Task Tracker_UpdateStatus()
    {
        await EnsureSchemaAsync();
        var tracker = new RelationalMigrationTracker(_provider, _connectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "001", ScriptName = "a.sql", ScriptHash = "h1", Environment = "dev", Status = MigrationStatus.InProgress, ExecutedBy = "tester" });
        await tracker.UpdateStatusAsync("dev", "001", MigrationStatus.Completed, null);

        var fetched = await tracker.GetByVersionAsync("dev", "001");
        Assert.NotNull(fetched);
        Assert.Equal(MigrationStatus.Completed, fetched!.Status);
    }

    [Fact]
    public async Task Tracker_Delete_RemovesRecord()
    {
        await EnsureSchemaAsync();
        var tracker = new RelationalMigrationTracker(_provider, _connectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "001", ScriptName = "a.sql", ScriptHash = "h1", Environment = "dev", Status = MigrationStatus.Completed, ExecutedBy = "tester" });
        await tracker.DeleteAsync("dev", "001");

        var fetched = await tracker.GetByVersionAsync("dev", "001");
        Assert.Null(fetched);
    }

    [Fact]
    public async Task Tracker_Exists_ReturnsCorrectBoolean()
    {
        await EnsureSchemaAsync();
        var tracker = new RelationalMigrationTracker(_provider, _connectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "001", ScriptName = "a.sql", ScriptHash = "h1", Environment = "dev", Status = MigrationStatus.Completed, ExecutedBy = "tester" });

        Assert.True(await tracker.ExistsAsync("dev", "001"));
        Assert.False(await tracker.ExistsAsync("dev", "999"));
    }

    [Fact]
    public async Task Tracker_Add_MultipleRepeatables_Coexist()
    {
        await EnsureSchemaAsync();
        var tracker = new RelationalMigrationTracker(_provider, _connectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "R", ScriptName = "R__Alpha.sql", ScriptHash = "h1", Type = MigrationType.Repeatable, Environment = "dev", Status = MigrationStatus.Completed, ExecutedBy = "tester" });
        await tracker.AddAsync(new MigrationRecord { Version = "R", ScriptName = "R__Beta.sql", ScriptHash = "h2", Type = MigrationType.Repeatable, Environment = "dev", Status = MigrationStatus.Completed, ExecutedBy = "tester" });

        var all = await tracker.GetAllAsync("dev");
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task Tracker_Add_Repeatable_ReplacesByScriptName()
    {
        await EnsureSchemaAsync();
        var tracker = new RelationalMigrationTracker(_provider, _connectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "R", ScriptName = "R__Alpha.sql", ScriptHash = "old_hash", Type = MigrationType.Repeatable, Environment = "dev", Status = MigrationStatus.Completed, ExecutedBy = "tester" });
        await tracker.AddAsync(new MigrationRecord { Version = "R", ScriptName = "R__Alpha.sql", ScriptHash = "new_hash", Type = MigrationType.Repeatable, Environment = "dev", Status = MigrationStatus.Completed, ExecutedBy = "tester" });

        var all = await tracker.GetAllAsync("dev");
        Assert.Single(all);
        Assert.Equal("new_hash", all[0].ScriptHash);
    }

    // ── LockManager ──────────────────────────────────────────────────

    [Fact]
    public async Task LockManager_AcquireAndRelease()
    {
        await EnsureSchemaAsync();
        var locks = new RelationalMigrationLockManager(_provider, _connectionString);

        var acquired = await locks.AcquireAsync("dev", "test-lock", "owner1", 30);
        Assert.True(acquired);

        var isActive = await locks.IsActiveAsync("dev", "test-lock");
        Assert.True(isActive);

        await locks.ReleaseAsync("dev", "test-lock", "owner1");

        isActive = await locks.IsActiveAsync("dev", "test-lock");
        Assert.False(isActive);
    }

    [Fact]
    public async Task LockManager_DoubleAcquire_SecondFails()
    {
        await EnsureSchemaAsync();
        var locks = new RelationalMigrationLockManager(_provider, _connectionString);

        var first = await locks.AcquireAsync("dev", "test-lock", "owner1", 30);
        Assert.True(first);

        var second = await locks.AcquireAsync("dev", "test-lock", "owner2", 30);
        Assert.False(second);
    }

    [Fact]
    public async Task LockManager_ReleaseAndReacquire()
    {
        await EnsureSchemaAsync();
        var locks = new RelationalMigrationLockManager(_provider, _connectionString);

        await locks.AcquireAsync("dev", "test-lock", "owner1", 30);
        await locks.ReleaseAsync("dev", "test-lock", "owner1");

        var reacquired = await locks.AcquireAsync("dev", "test-lock", "owner2", 30);
        Assert.True(reacquired);
    }

    [Fact]
    public async Task LockManager_Renew_ExtendsExpiry()
    {
        await EnsureSchemaAsync();
        var locks = new RelationalMigrationLockManager(_provider, _connectionString);

        await locks.AcquireAsync("dev", "test-lock", "owner1", 30);
        var renewed = await locks.RenewAsync("dev", "test-lock", "owner1", 60);
        Assert.True(renewed);
    }

    [Fact]
    public async Task LockManager_Renew_WrongOwner_Fails()
    {
        await EnsureSchemaAsync();
        var locks = new RelationalMigrationLockManager(_provider, _connectionString);

        await locks.AcquireAsync("dev", "test-lock", "owner1", 30);
        var renewed = await locks.RenewAsync("dev", "test-lock", "imposter", 60);
        Assert.False(renewed);
    }

    // ── AuditLogger ──────────────────────────────────────────────────

    [Fact]
    public async Task AuditLogger_LogAndGetHistory()
    {
        await EnsureSchemaAsync();
        var audit = new RelationalAuditLogger(_provider, _connectionString);

        await audit.LogAsync(new MigrationAuditEntry
        {
            Action = AuditAction.Deploy,
            PerformedBy = "tester",
            Environment = "dev",
            Details = "applied 2 migrations"
        });

        await audit.LogAsync(new MigrationAuditEntry
        {
            Action = AuditAction.Validate,
            PerformedBy = "ci-bot",
            Environment = "dev",
            Details = "validated 5 scripts"
        });

        var history = await audit.GetHistoryAsync("dev", 10);
        Assert.Equal(2, history.Count);
        Assert.Contains(history, e => e.Action == AuditAction.Deploy);
        Assert.Contains(history, e => e.Action == AuditAction.Validate);
    }

    [Fact]
    public async Task AuditLogger_GetHistory_RespectsLimit()
    {
        await EnsureSchemaAsync();
        var audit = new RelationalAuditLogger(_provider, _connectionString);

        for (var i = 0; i < 5; i++)
        {
            await audit.LogAsync(new MigrationAuditEntry
            {
                Action = AuditAction.Validate,
                PerformedBy = "bot",
                Environment = "dev",
                Details = $"entry {i}"
            });
        }

        var history = await audit.GetHistoryAsync("dev", 2);
        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task AuditLogger_GetHistory_FiltersByEnvironment()
    {
        await EnsureSchemaAsync();
        var audit = new RelationalAuditLogger(_provider, _connectionString);

        await audit.LogAsync(new MigrationAuditEntry { Action = AuditAction.Deploy, PerformedBy = "u1", Environment = "dev", Details = "dev deploy" });
        await audit.LogAsync(new MigrationAuditEntry { Action = AuditAction.Deploy, PerformedBy = "u2", Environment = "prod", Details = "prod deploy" });

        var devHistory = await audit.GetHistoryAsync("dev", 10);
        Assert.Single(devHistory);

        var prodHistory = await audit.GetHistoryAsync("prod", 10);
        Assert.Single(prodHistory);
    }
}
