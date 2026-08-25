using dbsh.Core.Entities;
using dbsh.Core.Enums;
using dbsh.Core.ValueObjects;
using dbsh.Engine.Execution;
using dbsh.Engine.InMemory;
using dbsh.Engine.Parsing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace dbsh.Engine.Tests;

public class RollbackFlowTests
{
    private readonly InMemoryMigrationTracker _tracker = new();
    private readonly InMemoryMigrationLockManager _lockManager = new();
    private readonly InMemoryAuditLogger _auditLogger = new();
    private readonly InMemoryEnvironmentProvider _envProvider = new();
    private readonly ScriptParser _parser = new();

    private MigrationExecutor CreateExecutor(string scriptsPath, bool withConnection = true)
    {
        return new MigrationExecutor(
            _tracker, _lockManager, _parser, _envProvider, _auditLogger,
            NullLogger<MigrationExecutor>.Instance,
            withConnection ? new FakeScriptExecutor() : null,
            withConnection ? "fake-connection" : null,
            scriptsPath: scriptsPath);
    }

    [Fact]
    public async Task Rollback_WithScript_RollsBackSuccessfully()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/U001__Rollback_Table.sql", "DROP TABLE IF EXISTS t;");

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateTable.sql",
            ScriptHash = _parser.GenerateHash("CREATE TABLE t (id INT);"),
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path);
        var result = await executor.RollbackAsync(new RollbackRequest
        {
            Version = "last",
            Count = 1,
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.True(result.IsSuccess);
        Assert.Contains("001", result.RolledBackMigrations);
    }

    [Fact]
    public async Task Rollback_NoRollbackScript_ReturnsError()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateTable.sql",
            ScriptHash = "abc",
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path);
        var result = await executor.RollbackAsync(new RollbackRequest
        {
            Version = "last",
            Count = 1,
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("No rollback script", result.ErrorMessage!);
    }

    [Fact]
    public async Task Rollback_NoConnection_ReturnsError()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/U001__Rollback_Table.sql", "DROP TABLE IF EXISTS t;");

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateTable.sql",
            ScriptHash = "abc",
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path, withConnection: false);
        var result = await executor.RollbackAsync(new RollbackRequest
        {
            Version = "last",
            Count = 1,
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("connection", result.ErrorMessage!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Rollback_SpecificVersion_RollsBackOnlyThatVersion()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/U001__Rollback_Table.sql", "DROP TABLE IF EXISTS t;");
        dir.WriteScript("Schema/V002__AddColumn.sql", "ALTER TABLE t ADD COLUMN name TEXT;");
        dir.WriteScript("Schema/U002__Rollback_AddColumn.sql", "ALTER TABLE t DROP COLUMN name;");

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateTable.sql",
            ScriptHash = "a",
            Status = MigrationStatus.Completed,
            Environment = "development"
        });
        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "002",
            ScriptName = "V002__AddColumn.sql",
            ScriptHash = "b",
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path);
        var result = await executor.RollbackAsync(new RollbackRequest
        {
            Version = "001",
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.True(result.IsSuccess);
        Assert.Contains("001", result.RolledBackMigrations);
        Assert.DoesNotContain("002", result.RolledBackMigrations);
    }

    [Fact]
    public async Task Rollback_CountTwo_RollsBackTwoMigrations()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/U001__Rollback_Table.sql", "DROP TABLE IF EXISTS t;");
        dir.WriteScript("Schema/V002__AddColumn.sql", "ALTER TABLE t ADD COLUMN name TEXT;");
        dir.WriteScript("Schema/U002__Rollback_AddColumn.sql", "ALTER TABLE t DROP COLUMN name;");

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateTable.sql",
            ScriptHash = "a",
            Status = MigrationStatus.Completed,
            Environment = "development"
        });
        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "002",
            ScriptName = "V002__AddColumn.sql",
            ScriptHash = "b",
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path);
        var result = await executor.RollbackAsync(new RollbackRequest
        {
            Version = "last",
            Count = 2,
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.RolledBackMigrations.Count);
    }

    [Fact]
    public async Task Rollback_NoAppliedMigrations_ReturnsSuccess()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");

        var executor = CreateExecutor(dir.Path);
        var result = await executor.RollbackAsync(new RollbackRequest
        {
            Version = "last",
            Count = 1,
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.True(result.IsSuccess);
        Assert.Empty(result.RolledBackMigrations);
    }

    [Fact]
    public async Task Rollback_AcquiresAndReleasesLock()
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateTable.sql", "CREATE TABLE t (id INT);");
        dir.WriteScript("Schema/U001__Rollback_Table.sql", "DROP TABLE IF EXISTS t;");

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateTable.sql",
            ScriptHash = "a",
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path);
        await executor.RollbackAsync(new RollbackRequest
        {
            Version = "last",
            Count = 1,
            Environment = "development",
            ExecutedBy = "tester"
        });

        var isActive = await _lockManager.IsActiveAsync("development", "migration:development");
        Assert.False(isActive);
    }
}
