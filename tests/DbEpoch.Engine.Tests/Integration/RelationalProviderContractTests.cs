using System.Runtime.CompilerServices;
using DbEpoch.Core.Entities;
using DbEpoch.Core.Enums;
using DbEpoch.Infrastructure.Database;
using Xunit;

namespace DbEpoch.Engine.Tests.Integration;

/// <summary>
/// Provider-agnostic contract tests for <see cref="RelationalMigrationTracker"/>,
/// <see cref="RelationalMigrationLockManager"/>, and <see cref="RelationalAuditLogger"/> against
/// a real containerized database. Mirrors <see cref="SqliteRelationalTests"/>. Each test gets a
/// fresh environment name via <see cref="Env"/> so the class can share one container/database.
/// </summary>
[Trait("Category", "Integration")]
public abstract class RelationalProviderContractTests<TFixture> : IClassFixture<TFixture>
    where TFixture : ContainerFixture
{
    private readonly TFixture _fixture;

    protected RelationalProviderContractTests(TFixture fixture)
    {
        _fixture = fixture;
    }

    private static string Env([CallerMemberName] string? caller = null) => $"{caller}_{Guid.NewGuid():N}";

    // â”€â”€ MigrationTracker â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task Tracker_AddAndGetByVersion()
    {
        var env = Env();
        var tracker = new RelationalMigrationTracker(_fixture.Provider, _fixture.ConnectionString);

        var record = new MigrationRecord
        {
            Version = "001",
            Name = "CreateUsers",
            ScriptName = "V001__CreateUsers.sql",
            ScriptHash = "abc123",
            Type = MigrationType.Schema,
            Category = "Schema",
            ExecutedBy = "tester",
            Environment = env,
            Status = MigrationStatus.Completed
        };

        await tracker.AddAsync(record);
        var fetched = await tracker.GetByVersionAsync(env, "001");

        Assert.NotNull(fetched);
        Assert.Equal("001", fetched!.Version);
        Assert.Equal("CreateUsers", fetched.Name);
        Assert.Equal(MigrationStatus.Completed, fetched.Status);
    }

    [Fact]
    public async Task Tracker_GetByVersion_NotFound_ReturnsNull()
    {
        var env = Env();
        var tracker = new RelationalMigrationTracker(_fixture.Provider, _fixture.ConnectionString);

        var fetched = await tracker.GetByVersionAsync(env, "999");
        Assert.Null(fetched);
    }

    [Fact]
    public async Task Tracker_GetAll_ReturnsAllForEnvironment()
    {
        var env = Env();
        var otherEnv = Env();
        var tracker = new RelationalMigrationTracker(_fixture.Provider, _fixture.ConnectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "001", ScriptName = "a.sql", ScriptHash = "h1", Environment = env, Status = MigrationStatus.Completed });
        await tracker.AddAsync(new MigrationRecord { Version = "002", ScriptName = "b.sql", ScriptHash = "h2", Environment = env, Status = MigrationStatus.Failed });
        await tracker.AddAsync(new MigrationRecord { Version = "003", ScriptName = "c.sql", ScriptHash = "h3", Environment = otherEnv, Status = MigrationStatus.Completed });

        var envRecords = await tracker.GetAllAsync(env);
        Assert.Equal(2, envRecords.Count);

        var otherEnvRecords = await tracker.GetAllAsync(otherEnv);
        Assert.Single(otherEnvRecords);
    }

    [Fact]
    public async Task Tracker_GetApplied_ReturnsOnlyCompleted()
    {
        var env = Env();
        var tracker = new RelationalMigrationTracker(_fixture.Provider, _fixture.ConnectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "001", ScriptName = "a.sql", ScriptHash = "h1", Environment = env, Status = MigrationStatus.Completed });
        await tracker.AddAsync(new MigrationRecord { Version = "002", ScriptName = "b.sql", ScriptHash = "h2", Environment = env, Status = MigrationStatus.Failed });
        await tracker.AddAsync(new MigrationRecord { Version = "003", ScriptName = "c.sql", ScriptHash = "h3", Environment = env, Status = MigrationStatus.Completed });

        var applied = await tracker.GetAppliedAsync(env);
        Assert.Equal(2, applied.Count);
        Assert.All(applied, r => Assert.Equal(MigrationStatus.Completed, r.Status));
    }

    [Fact]
    public async Task Tracker_Add_Idempotent_ReplacesExisting()
    {
        var env = Env();
        var tracker = new RelationalMigrationTracker(_fixture.Provider, _fixture.ConnectionString);

        var record = new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__Old.sql",
            ScriptHash = "old_hash",
            Environment = env,
            Status = MigrationStatus.Completed,
            ExecutedBy = "user1"
        };
        await tracker.AddAsync(record);

        var updated = new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__New.sql",
            ScriptHash = "new_hash",
            Environment = env,
            Status = MigrationStatus.Failed,
            ExecutedBy = "user2"
        };
        await tracker.AddAsync(updated);

        var fetched = await tracker.GetByVersionAsync(env, "001");
        Assert.NotNull(fetched);
        Assert.Equal("new_hash", fetched!.ScriptHash);
        Assert.Equal(MigrationStatus.Failed, fetched.Status);
        Assert.Equal("user2", fetched.ExecutedBy);
    }

    [Fact]
    public async Task Tracker_UpdateStatus()
    {
        var env = Env();
        var tracker = new RelationalMigrationTracker(_fixture.Provider, _fixture.ConnectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "001", ScriptName = "a.sql", ScriptHash = "h1", Environment = env, Status = MigrationStatus.InProgress, ExecutedBy = "tester" });
        await tracker.UpdateStatusAsync(env, "001", MigrationStatus.Completed, null);

        var fetched = await tracker.GetByVersionAsync(env, "001");
        Assert.NotNull(fetched);
        Assert.Equal(MigrationStatus.Completed, fetched!.Status);
    }

    [Fact]
    public async Task Tracker_Delete_RemovesRecord()
    {
        var env = Env();
        var tracker = new RelationalMigrationTracker(_fixture.Provider, _fixture.ConnectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "001", ScriptName = "a.sql", ScriptHash = "h1", Environment = env, Status = MigrationStatus.Completed, ExecutedBy = "tester" });
        await tracker.DeleteAsync(env, "001");

        var fetched = await tracker.GetByVersionAsync(env, "001");
        Assert.Null(fetched);
    }

    [Fact]
    public async Task Tracker_Exists_ReturnsCorrectBoolean()
    {
        var env = Env();
        var tracker = new RelationalMigrationTracker(_fixture.Provider, _fixture.ConnectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "001", ScriptName = "a.sql", ScriptHash = "h1", Environment = env, Status = MigrationStatus.Completed, ExecutedBy = "tester" });

        Assert.True(await tracker.ExistsAsync(env, "001"));
        Assert.False(await tracker.ExistsAsync(env, "999"));
    }

    [Fact]
    public async Task Tracker_Add_MultipleRepeatables_Coexist()
    {
        var env = Env();
        var tracker = new RelationalMigrationTracker(_fixture.Provider, _fixture.ConnectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "R", ScriptName = "R__Alpha.sql", ScriptHash = "h1", Type = MigrationType.Repeatable, Environment = env, Status = MigrationStatus.Completed, ExecutedBy = "tester" });
        await tracker.AddAsync(new MigrationRecord { Version = "R", ScriptName = "R__Beta.sql", ScriptHash = "h2", Type = MigrationType.Repeatable, Environment = env, Status = MigrationStatus.Completed, ExecutedBy = "tester" });

        var all = await tracker.GetAllAsync(env);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task Tracker_Add_Repeatable_ReplacesByScriptName()
    {
        var env = Env();
        var tracker = new RelationalMigrationTracker(_fixture.Provider, _fixture.ConnectionString);

        await tracker.AddAsync(new MigrationRecord { Version = "R", ScriptName = "R__Alpha.sql", ScriptHash = "old_hash", Type = MigrationType.Repeatable, Environment = env, Status = MigrationStatus.Completed, ExecutedBy = "tester" });
        await tracker.AddAsync(new MigrationRecord { Version = "R", ScriptName = "R__Alpha.sql", ScriptHash = "new_hash", Type = MigrationType.Repeatable, Environment = env, Status = MigrationStatus.Completed, ExecutedBy = "tester" });

        var all = await tracker.GetAllAsync(env);
        Assert.Single(all);
        Assert.Equal("new_hash", all[0].ScriptHash);
    }

    // â”€â”€ LockManager â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task LockManager_AcquireAndRelease()
    {
        var env = Env();
        var locks = new RelationalMigrationLockManager(_fixture.Provider, _fixture.ConnectionString);

        var acquired = await locks.AcquireAsync(env, "test-lock", "owner1", 30);
        Assert.True(acquired);

        var isActive = await locks.IsActiveAsync(env, "test-lock");
        Assert.True(isActive);

        await locks.ReleaseAsync(env, "test-lock", "owner1");

        isActive = await locks.IsActiveAsync(env, "test-lock");
        Assert.False(isActive);
    }

    [Fact]
    public async Task LockManager_DoubleAcquire_SecondFails()
    {
        var env = Env();
        var locks = new RelationalMigrationLockManager(_fixture.Provider, _fixture.ConnectionString);

        var first = await locks.AcquireAsync(env, "test-lock", "owner1", 30);
        Assert.True(first);

        var second = await locks.AcquireAsync(env, "test-lock", "owner2", 30);
        Assert.False(second);
    }

    [Fact]
    public async Task LockManager_ReleaseAndReacquire()
    {
        var env = Env();
        var locks = new RelationalMigrationLockManager(_fixture.Provider, _fixture.ConnectionString);

        await locks.AcquireAsync(env, "test-lock", "owner1", 30);
        await locks.ReleaseAsync(env, "test-lock", "owner1");

        var reacquired = await locks.AcquireAsync(env, "test-lock", "owner2", 30);
        Assert.True(reacquired);
    }

    [Fact]
    public async Task LockManager_Renew_ExtendsExpiry()
    {
        var env = Env();
        var locks = new RelationalMigrationLockManager(_fixture.Provider, _fixture.ConnectionString);

        await locks.AcquireAsync(env, "test-lock", "owner1", 30);
        var renewed = await locks.RenewAsync(env, "test-lock", "owner1", 60);
        Assert.True(renewed);
    }

    [Fact]
    public async Task LockManager_Renew_WrongOwner_Fails()
    {
        var env = Env();
        var locks = new RelationalMigrationLockManager(_fixture.Provider, _fixture.ConnectionString);

        await locks.AcquireAsync(env, "test-lock", "owner1", 30);
        var renewed = await locks.RenewAsync(env, "test-lock", "imposter", 60);
        Assert.False(renewed);
    }

    // â”€â”€ AuditLogger â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    [Fact]
    public async Task AuditLogger_LogAndGetHistory()
    {
        var env = Env();
        var audit = new RelationalAuditLogger(_fixture.Provider, _fixture.ConnectionString);

        await audit.LogAsync(new MigrationAuditEntry { Action = AuditAction.Deploy, PerformedBy = "tester", Environment = env, Details = "applied 2 migrations" });
        await audit.LogAsync(new MigrationAuditEntry { Action = AuditAction.Validate, PerformedBy = "ci-bot", Environment = env, Details = "validated 5 scripts" });

        var history = await audit.GetHistoryAsync(env, 10);
        Assert.Equal(2, history.Count);
        Assert.Contains(history, e => e.Action == AuditAction.Deploy);
        Assert.Contains(history, e => e.Action == AuditAction.Validate);
    }

    [Fact]
    public async Task AuditLogger_GetHistory_RespectsLimit()
    {
        var env = Env();
        var audit = new RelationalAuditLogger(_fixture.Provider, _fixture.ConnectionString);

        for (var i = 0; i < 5; i++)
        {
            await audit.LogAsync(new MigrationAuditEntry { Action = AuditAction.Validate, PerformedBy = "bot", Environment = env, Details = $"entry {i}" });
        }

        var history = await audit.GetHistoryAsync(env, 2);
        Assert.Equal(2, history.Count);
    }

    [Fact]
    public async Task AuditLogger_GetHistory_FiltersByEnvironment()
    {
        var env = Env();
        var otherEnv = Env();
        var audit = new RelationalAuditLogger(_fixture.Provider, _fixture.ConnectionString);

        await audit.LogAsync(new MigrationAuditEntry { Action = AuditAction.Deploy, PerformedBy = "u1", Environment = env, Details = "deploy" });
        await audit.LogAsync(new MigrationAuditEntry { Action = AuditAction.Deploy, PerformedBy = "u2", Environment = otherEnv, Details = "deploy" });

        var envHistory = await audit.GetHistoryAsync(env, 10);
        Assert.Single(envHistory);

        var otherEnvHistory = await audit.GetHistoryAsync(otherEnv, 10);
        Assert.Single(otherEnvHistory);
    }
}
