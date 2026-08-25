using dbsh.Core.Interfaces;
using dbsh.Core.ValueObjects;
using dbsh.Engine.InMemory;

namespace dbsh.Engine.Tests;

/// <summary>
/// Creates a temp directory tree for migration scripts and cleans up on dispose.
/// </summary>
public sealed class TempScriptsDirectory : IDisposable
{
    private readonly string _tempDir;

    public string Path { get; }

    public TempScriptsDirectory()
    {
        _tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "dbsh-tests-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        Path = _tempDir;
    }

    public void WriteScript(string relativePath, string content)
    {
        var fullPath = System.IO.Path.Combine(_tempDir, relativePath);
        var dir = System.IO.Path.GetDirectoryName(fullPath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllText(fullPath, content);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }
}

/// <summary>
/// Fake script executor with configurable success, delay, and tracking.
/// Allows testing deploy flows and timing without a real database.
/// </summary>
public sealed class FakeScriptExecutor : IMigrationScriptExecutor
{
    private readonly int _delayMs;
    private readonly string _failOnSqlContaining;

    public int ExecutionCount { get; private set; }
    public List<string> ExecutedSql { get; } = new();

    public FakeScriptExecutor(int delayMs = 0, string? failOnSqlContaining = null)
    {
        _delayMs = delayMs;
        _failOnSqlContaining = failOnSqlContaining ?? string.Empty;
    }

    public async Task<ScriptExecutionResult> ExecuteAsync(string connectionString, string sql, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        if (_delayMs > 0)
        {
            await Task.Delay(_delayMs, cancellationToken);
        }

        ExecutionCount++;
        ExecutedSql.Add(sql);

        if (!string.IsNullOrEmpty(_failOnSqlContaining) && sql.Contains(_failOnSqlContaining, StringComparison.OrdinalIgnoreCase))
        {
            return new ScriptExecutionResult { IsSuccess = false, ElapsedMs = _delayMs, ErrorMessage = $"Simulated failure for SQL containing '{_failOnSqlContaining}'" };
        }

        return new ScriptExecutionResult { IsSuccess = true, ElapsedMs = _delayMs + 5 };
    }

    public Task EnsureTrackingSchemaAsync(string connectionString, string? module = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Wraps <see cref="InMemoryEnvironmentProvider"/> but allows overriding settings
/// such as <c>RequireApproval</c> for specific test scenarios.
/// </summary>
public sealed class ConfigurableEnvironmentProvider : IEnvironmentProvider
{
    private readonly InMemoryEnvironmentProvider _inner = new();
    public bool RequireApproval { get; set; }

    public async Task<EnvironmentConfiguration> GetEnvironmentAsync(string name, CancellationToken cancellationToken = default)
    {
        var env = await _inner.GetEnvironmentAsync(name, cancellationToken);
        env.Migration.RequireApproval = RequireApproval;
        return env;
    }

    public Task<bool> EnvironmentExistsAsync(string name, CancellationToken cancellationToken = default)
        => _inner.EnvironmentExistsAsync(name, cancellationToken);

    public Task<IReadOnlyList<string>> GetAvailableEnvironmentsAsync(CancellationToken cancellationToken = default)
        => _inner.GetAvailableEnvironmentsAsync(cancellationToken);
}

/// <summary>
/// Lock manager whose <see cref="RenewAsync"/> outcome is controlled by the test, to simulate
/// a lease that expires and gets stolen by another process mid-deploy. Acquire/Release always
/// succeed so the scenario under test is purely "renewal reports the lease is gone".
/// </summary>
public sealed class FakeLockManager : IMigrationLockManager
{
    public bool RenewSucceeds { get; set; } = true;
    public int RenewCallCount { get; private set; }

    public Task<bool> AcquireAsync(string environment, string lockKey, string lockedBy, int timeoutSeconds, string? module = null, CancellationToken cancellationToken = default)
        => Task.FromResult(true);

    public Task ReleaseAsync(string environment, string lockKey, string? lockedBy = null, string? module = null, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<bool> RenewAsync(string environment, string lockKey, string lockedBy, int timeoutSeconds, string? module = null, CancellationToken cancellationToken = default)
    {
        RenewCallCount++;
        return Task.FromResult(RenewSucceeds);
    }

    public Task<bool> IsActiveAsync(string environment, string lockKey, string? module = null, CancellationToken cancellationToken = default)
        => Task.FromResult(true);
}

/// <summary>
/// Fake script executor that always fails. Used to test deploy error handling.
/// Tracks execution count for assertions.
/// </summary>
public sealed class FailingScriptExecutor : IMigrationScriptExecutor
{
    public int ExecutionCount { get; private set; }

    public Task<ScriptExecutionResult> ExecuteAsync(string connectionString, string sql, int timeoutSeconds, CancellationToken cancellationToken = default)
    {
        ExecutionCount++;
        return Task.FromResult(new ScriptExecutionResult { IsSuccess = false, ErrorMessage = "Simulated execution failure" });
    }

    public Task EnsureTrackingSchemaAsync(string connectionString, string? module = null, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}

