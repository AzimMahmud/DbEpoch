using DbShift.Core.Enums;
using DbShift.Core.Interfaces;
using DbShift.Core.ValueObjects;
using DbShift.Engine.Execution;
using DbShift.Engine.InMemory;
using DbShift.Engine.Parsing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DbShift.Engine.Tests;

public class DeployFlowTests
{
    private readonly InMemoryMigrationTracker _tracker = new();
    private readonly InMemoryMigrationLockManager _lockManager = new();
    private readonly InMemoryAuditLogger _auditLogger = new();
    private readonly ConfigurableEnvironmentProvider _envProvider = new();
    private readonly ScriptParser _parser = new();

    private MigrationExecutor CreateExecutor(string scriptsPath, IMigrationScriptExecutor? executor = null, string? connection = null)
    {
        return new MigrationExecutor(
            _tracker, _lockManager, _parser, _envProvider, _auditLogger,
            NullLogger<MigrationExecutor>.Instance,
            executor, connection, scriptsPath: scriptsPath);
    }

    [Fact]
    public async Task Deploy_NoConnection_ReturnsError()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");

        var executor = CreateExecutor(dir.Path);
        var result = await executor.DeployAsync(new MigrationContext { Environment = "development" });

        Assert.False(result.IsSuccess);
        Assert.Contains("connection", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deploy_NoScripts_ReturnsSuccess()
    {
        using var dir = new TempScriptsDirectory();
        var executor = CreateExecutor(dir.Path, new FakeScriptExecutor(), "fake");
        var result = await executor.DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(0, result.TotalApplied);
    }

    [Fact]
    public async Task Deploy_WithPendingScripts_AppliesAll()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/V002__AddColumn.sql", "ALTER TABLE t ADD COLUMN name VARCHAR(100);");

        var executor = CreateExecutor(dir.Path, new FakeScriptExecutor(), "fake");
        var result = await executor.DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.TotalApplied);
        Assert.Contains("001", result.AppliedMigrations);
        Assert.Contains("002", result.AppliedMigrations);
    }

    [Fact]
    public async Task Deploy_AlreadyApplied_NotReApplied()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/V002__AddColumn.sql", "ALTER TABLE t ADD COLUMN name VARCHAR(100);");

        var content1 = "CREATE TABLE t (id INT);";
        var hash1 = _parser.GenerateHash(content1);
        await _tracker.AddAsync(new Core.Entities.MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateTable.sql",
            ScriptHash = hash1,
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path, new FakeScriptExecutor(), "fake");
        var result = await executor.DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.TotalApplied);
        Assert.Contains("002", result.AppliedMigrations);
    }

    [Fact]
    public async Task Deploy_ScriptFails_StopOnFailure()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/V002__AddColumn.sql", "ALTER TABLE t ADD COLUMN name VARCHAR(100);");

        var executor = CreateExecutor(dir.Path, new FailingScriptExecutor(), "fake");
        var result = await executor.DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester",
            StopOnFailure = true
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("001", result.FailedMigrations);
        Assert.Equal(0, result.TotalApplied);
    }

    [Fact]
    public async Task Deploy_LockLostBetweenBatches_StopsAndReportsFailure()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/V002__AddColumn.sql", "ALTER TABLE t ADD COLUMN name VARCHAR(100);");

        var lockManager = new FakeLockManager { RenewSucceeds = false };
        var scriptExecutor = new FakeScriptExecutor();
        var executor = new MigrationExecutor(
            _tracker, lockManager, _parser, _envProvider, _auditLogger,
            NullLogger<MigrationExecutor>.Instance,
            scriptExecutor, "fake", scriptsPath: dir.Path);

        var result = await executor.DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester",
            BatchSize = 1
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("lock", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, result.TotalApplied);
        Assert.Equal(0, scriptExecutor.ExecutionCount);
    }

    [Fact]
    public async Task Deploy_ApprovalRequired_NoApprover_Refuses()
    {
        _envProvider.RequireApproval = true;

        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");

        var executor = CreateExecutor(dir.Path, new FakeScriptExecutor(), "fake");
        var result = await executor.DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("approver", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Deploy_ApprovalRequired_WithApprover_Proceeds()
    {
        _envProvider.RequireApproval = true;

        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");

        var executor = CreateExecutor(dir.Path, new FakeScriptExecutor(), "fake");
        var result = await executor.DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester",
            Approver = "admin"
        });

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task DryRun_WithScripts_ReturnsCorrectCount()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/V002__AddColumn.sql", "ALTER TABLE t ADD COLUMN name VARCHAR(100);");
        dir.WriteScript("Schema/V003__AddIndex.sql", "CREATE INDEX idx ON t (name);");

        var executor = CreateExecutor(dir.Path);
        var result = await executor.DryRunAsync(new MigrationContext { Environment = "development" });

        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.ExecutionPlan!.TotalCount);
    }

    [Fact]
    public async Task Validate_WithScripts_ReturnsValidAndCheckedCount()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/V002__AddColumn.sql", "ALTER TABLE t ADD COLUMN name VARCHAR(100);");

        var executor = CreateExecutor(dir.Path);
        var result = await executor.ValidateAsync("development");

        Assert.True(result.IsValid);
        Assert.Equal(2, result.ScriptsChecked);
    }

    [Fact]
    public async Task Validate_DuplicateVersion_ReturnsError()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Data/V001__SeedTable.sql", "INSERT INTO t VALUES (1);");

        var executor = CreateExecutor(dir.Path);
        var result = await executor.ValidateAsync("development");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate migration version '001'", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_MissingDependency_ReturnsError()
    {
        using var dir = new TempScriptsDirectory();
        var content = "-- Depends: V999__Missing.sql\nCREATE TABLE t (id INT);";
        dir.WriteScript("Schema/V001__CreateTable.sql", content);

        var executor = CreateExecutor(dir.Path);
        var result = await executor.ValidateAsync("development");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("V999__Missing.sql", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Validate_CommentOnlyScript_ReturnsError()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "-- just a comment");

        var executor = CreateExecutor(dir.Path);
        var result = await executor.ValidateAsync("development");

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("no executable content", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetStatus_ReturnsCorrectCounts()
    {
        await _tracker.AddAsync(new Core.Entities.MigrationRecord
        {
            Version = "001", ScriptName = "V001.sql", ScriptHash = "a",
            Status = MigrationStatus.Completed, Environment = "development"
        });
        await _tracker.AddAsync(new Core.Entities.MigrationRecord
        {
            Version = "002", ScriptName = "V002.sql", ScriptHash = "b",
            Status = MigrationStatus.Failed, Environment = "development"
        });

        using var dir = new TempScriptsDirectory();
        var executor = CreateExecutor(dir.Path);
        var status = await executor.GetStatusAsync("development");

        Assert.Equal(2, status.Total);
        Assert.Equal(1, status.Applied);
        Assert.Equal(1, status.Failed);
    }

    [Fact]
    public async Task Repair_AllFailed_ClearsFailedRecords()
    {
        await _tracker.AddAsync(new Core.Entities.MigrationRecord
        {
            Version = "001", ScriptName = "V001.sql", ScriptHash = "a",
            Status = MigrationStatus.Failed, Environment = "development"
        });
        await _tracker.AddAsync(new Core.Entities.MigrationRecord
        {
            Version = "002", ScriptName = "V002.sql", ScriptHash = "b",
            Status = MigrationStatus.Completed, Environment = "development"
        });

        using var dir = new TempScriptsDirectory();
        var executor = CreateExecutor(dir.Path);
        var result = await executor.RepairAsync("development", version: null);

        Assert.True(result.IsSuccess);
        Assert.Contains("001", result.RepairedMigrations);
        Assert.DoesNotContain("002", result.RepairedMigrations);
    }
}
