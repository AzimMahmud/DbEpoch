using DbShift.Core.Entities;
using DbShift.Core.Enums;
using DbShift.Core.ValueObjects;
using DbShift.Engine.Execution;
using DbShift.Engine.InMemory;
using DbShift.Engine.Parsing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DbShift.Engine.Tests;

public class RepeatableMigrationTests
{
    private readonly InMemoryMigrationTracker _tracker = new();
    private readonly InMemoryMigrationLockManager _lockManager = new();
    private readonly InMemoryAuditLogger _auditLogger = new();
    private readonly InMemoryEnvironmentProvider _envProvider = new();
    private readonly ScriptParser _parser = new();

    private MigrationExecutor CreateExecutor(string scriptsPath)
    {
        return new MigrationExecutor(
            _tracker, _lockManager, _parser, _envProvider, _auditLogger,
            NullLogger<MigrationExecutor>.Instance, scriptsPath: scriptsPath);
    }

    [Fact]
    public async Task DryRun_NewRepeatable_ShowsAsPending()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/R__CreateView.sql", "CREATE OR REPLACE VIEW v AS SELECT 1;");

        var executor = CreateExecutor(dir.Path);
        var result = await executor.DryRunAsync(new MigrationContext { Environment = "development" });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ExecutionPlan!.TotalCount);
        Assert.Equal(MigrationType.Repeatable, result.ExecutionPlan.Items[0].Type);
    }

    [Fact]
    public async Task DryRun_AppliedRepeatableSameHash_NotPending()
    {
        var content = "CREATE OR REPLACE VIEW v AS SELECT 1;";
        var hash = _parser.GenerateHash(content);

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "R",
            ScriptName = "R__CreateView.sql",
            ScriptHash = hash,
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/R__CreateView.sql", content);

        var executor = CreateExecutor(dir.Path);
        var result = await executor.DryRunAsync(new MigrationContext { Environment = "development" });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.ExecutionPlan!.TotalCount);
    }

    [Fact]
    public async Task DryRun_AppliedRepeatableChangedHash_IsPendingAgain()
    {
        var oldContent = "CREATE OR REPLACE VIEW v AS SELECT 1;";
        var oldHash = _parser.GenerateHash(oldContent);

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "R",
            ScriptName = "R__CreateView.sql",
            ScriptHash = oldHash,
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/R__CreateView.sql", "CREATE OR REPLACE VIEW v AS SELECT 2;");

        var executor = CreateExecutor(dir.Path);
        var result = await executor.DryRunAsync(new MigrationContext { Environment = "development" });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ExecutionPlan!.TotalCount);
    }

    [Fact]
    public async Task DryRun_VersionedAndRepeatable_VersionedOrderedFirst()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__ CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/R__CreateView.sql", "CREATE OR REPLACE VIEW v AS SELECT 1;");

        var executor = CreateExecutor(dir.Path);
        var result = await executor.DryRunAsync(new MigrationContext { Environment = "development" });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ExecutionPlan!.TotalCount);
        Assert.Equal(MigrationType.Schema, result.ExecutionPlan.Items[0].Type);
        Assert.Equal("001", result.ExecutionPlan.Items[0].Version);
        Assert.Equal(MigrationType.Repeatable, result.ExecutionPlan.Items[1].Type);
    }

    [Fact]
    public async Task DryRun_MultipleRepeatables_AllShown()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/R__Alpha.sql", "CREATE OR REPLACE VIEW a AS SELECT 1;");
        dir.WriteScript("Schema/R__Beta.sql", "CREATE OR REPLACE VIEW b AS SELECT 1;");

        var executor = CreateExecutor(dir.Path);
        var result = await executor.DryRunAsync(new MigrationContext { Environment = "development" });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.ExecutionPlan!.TotalCount);
    }

    [Fact]
    public async Task Deploy_Repeatable_ListedInPendingRepeatables()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/R__CreateView.sql", "CREATE OR REPLACE VIEW v AS SELECT 1;");

        var executor = new MigrationExecutor(
            _tracker, _lockManager, _parser, _envProvider, _auditLogger,
            NullLogger<MigrationExecutor>.Instance,
            new FakeScriptExecutor(), "fake-connection", scriptsPath: dir.Path);

        var result = await executor.DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.TotalApplied);
        Assert.Contains(result.PendingRepeatables, f => f == "R__CreateView.sql");
    }

    [Fact]
    public async Task Deploy_MultipleRepeatables_SecondDeployIsNoOp()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/R__Alpha.sql", "CREATE OR REPLACE VIEW a AS SELECT 1;");
        dir.WriteScript("Schema/R__Beta.sql", "CREATE OR REPLACE VIEW b AS SELECT 1;");

        MigrationExecutor Create()
            => new MigrationExecutor(
                _tracker, _lockManager, _parser, _envProvider, _auditLogger,
                NullLogger<MigrationExecutor>.Instance,
                new FakeScriptExecutor(), "fake-connection", scriptsPath: dir.Path);

        var first = await Create().DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester"
        });
        Assert.True(first.IsSuccess);
        Assert.Equal(2, first.TotalApplied);

        var second = await Create().DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester"
        });
        Assert.True(second.IsSuccess);
        Assert.Equal(0, second.TotalApplied);
    }
}
