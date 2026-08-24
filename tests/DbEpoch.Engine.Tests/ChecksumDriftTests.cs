using DbEpoch.Core.Entities;
using DbEpoch.Core.Enums;
using DbEpoch.Core.ValueObjects;
using DbEpoch.Engine.Execution;
using DbEpoch.Engine.InMemory;
using DbEpoch.Engine.Parsing;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace DbEpoch.Engine.Tests;

public class ChecksumDriftTests
{
    private readonly InMemoryMigrationTracker _tracker = new();
    private readonly InMemoryMigrationLockManager _lockManager = new();
    private readonly InMemoryAuditLogger _auditLogger = new();
    private readonly InMemoryEnvironmentProvider _envProvider = new();
    private readonly ScriptParser _parser = new();

    private MigrationExecutor CreateExecutor(string scriptsPath, bool withConnection = false)
    {
        return new MigrationExecutor(
            _tracker, _lockManager, _parser, _envProvider, _auditLogger,
            NullLogger<MigrationExecutor>.Instance,
            withConnection ? new FakeScriptExecutor() : null,
            withConnection ? "fake-connection" : null,
            scriptsPath: scriptsPath);
    }

    private async Task SeedAppliedMigration(string version, string scriptName, string content)
    {
        using var dir = new TempScriptsDirectory();
        dir.WriteScript($"Schema/{scriptName}", content);

        var hash = _parser.GenerateHash(content);

        // Record with a DIFFERENT hash to simulate drift
        await _tracker.AddAsync(new MigrationRecord
        {
            Version = version,
            ScriptName = scriptName,
            ScriptHash = "0".PadLeft(64, '0'),
            Status = MigrationStatus.Completed,
            Environment = "development"
        });
    }

    [Fact]
    public async Task GetStatus_DriftDetected_ListsDriftedMigrations()
    {
        var content = "CREATE TABLE users (id INT);";
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateUsers.sql", content);

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateUsers.sql",
            ScriptHash = "deadbeef",
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path);
        var status = await executor.GetStatusAsync("development");

        Assert.Contains("001", status.DriftedMigrations);
    }

    [Fact]
    public async Task GetStatus_NoDrift_DoesNotListMigration()
    {
        var content = "CREATE TABLE users (id INT);";
        var hash = _parser.GenerateHash(content);

        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateUsers.sql", content);

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateUsers.sql",
            ScriptHash = hash,
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path);
        var status = await executor.GetStatusAsync("development");

        Assert.Empty(status.DriftedMigrations);
    }

    [Fact]
    public async Task Deploy_DriftDetected_RefusesWithoutForce()
    {
        var content = "CREATE TABLE users (id INT);";
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateUsers.sql", content);
        dir.WriteScript("Schema/V002__AddOrders.sql", "CREATE TABLE orders (id INT);");

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateUsers.sql",
            ScriptHash = "deadbeef",
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path, withConnection: true);
        var result = await executor.DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester"
        });

        Assert.False(result.IsSuccess);
        Assert.Contains("001", result.DriftedMigrations);
        Assert.Contains("Checksum drift", result.ErrorMessage);
    }

    [Fact]
    public async Task Deploy_DriftWithForce_Proceeds()
    {
        var content = "CREATE TABLE users (id INT);";
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateUsers.sql", content);
        dir.WriteScript("Schema/V002__AddOrders.sql", "CREATE TABLE orders (id INT);");

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateUsers.sql",
            ScriptHash = "deadbeef",
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path, withConnection: true);
        var result = await executor.DeployAsync(new MigrationContext
        {
            Environment = "development",
            ExecutedBy = "tester",
            Force = true
        });

        Assert.True(result.IsSuccess);
        Assert.Contains("002", result.AppliedMigrations);
    }

    [Fact]
    public async Task DryRun_DriftSurfacesWarning()
    {
        var content = "CREATE TABLE users (id INT);";
        using var dir = new TempScriptsDirectory();
        dir.WriteScript("Schema/V001__CreateUsers.sql", content);
        dir.WriteScript("Schema/V002__AddOrders.sql", "CREATE TABLE orders (id INT);");

        await _tracker.AddAsync(new MigrationRecord
        {
            Version = "001",
            ScriptName = "V001__CreateUsers.sql",
            ScriptHash = "deadbeef",
            Status = MigrationStatus.Completed,
            Environment = "development"
        });

        var executor = CreateExecutor(dir.Path);
        var result = await executor.DryRunAsync(new MigrationContext { Environment = "development" });

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.ExecutionPlan!.TotalCount);
    }
}
