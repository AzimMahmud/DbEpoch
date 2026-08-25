using dbsh.Infrastructure.Database;
using dbsh.Infrastructure.Database.Providers;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;
using Xunit;

namespace dbsh.Engine.Tests.Integration;

/// <summary>
/// Starts a real database container once per test class and creates the dbsh tracking schema
/// against it, so <see cref="RelationalProviderContractTests{TFixture}"/> exercises the real
/// ADO.NET provider instead of SQLite. Requires Docker; fails (rather than skips) without it.
/// </summary>
public abstract class ContainerFixture : IAsyncLifetime
{
    public abstract IDatabaseProvider Provider { get; }

    public string ConnectionString { get; private set; } = string.Empty;

    protected abstract Task<string> StartContainerAsync();

    protected abstract Task StopContainerAsync();

    public async Task InitializeAsync()
    {
        ConnectionString = await StartContainerAsync();

        await using var connection = Provider.CreateConnection(ConnectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = Provider.GetTrackingSchemaDdl();
        await command.ExecuteNonQueryAsync();
    }

    public Task DisposeAsync() => StopContainerAsync();
}

public sealed class PostgreSqlFixture : ContainerFixture
{
    private PostgreSqlContainer? _container;

    public override IDatabaseProvider Provider { get; } = new PostgreSqlProvider();

    protected override async Task<string> StartContainerAsync()
    {
        _container = new PostgreSqlBuilder("postgres:16-alpine").Build();
        await _container.StartAsync();
        return _container.GetConnectionString();
    }

    protected override Task StopContainerAsync() =>
        _container is null ? Task.CompletedTask : _container.DisposeAsync().AsTask();
}

public sealed class MySqlFixture : ContainerFixture
{
    private MySqlContainer? _container;

    public override IDatabaseProvider Provider { get; } = new MySqlProvider();

    protected override async Task<string> StartContainerAsync()
    {
        _container = new MySqlBuilder("mysql:8.0").Build();
        await _container.StartAsync();
        return _container.GetConnectionString();
    }

    protected override Task StopContainerAsync() =>
        _container is null ? Task.CompletedTask : _container.DisposeAsync().AsTask();
}

public sealed class SqlServerFixture : ContainerFixture
{
    private MsSqlContainer? _container;

    public override IDatabaseProvider Provider { get; } = new SqlServerProvider();

    protected override async Task<string> StartContainerAsync()
    {
        _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();
        await _container.StartAsync();
        return _container.GetConnectionString();
    }

    protected override Task StopContainerAsync() =>
        _container is null ? Task.CompletedTask : _container.DisposeAsync().AsTask();
}
