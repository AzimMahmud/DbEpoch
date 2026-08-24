using DbEpoch.Core.Exceptions;
using DbEpoch.Core.Interfaces;
using DbEpoch.Core.ValueObjects;

namespace DbEpoch.Infrastructure.Database;

/// <summary>
/// Resolves environment configuration by delegating to the file-system config loader.
/// Works with any database provider since it only reads JSON config files.
/// </summary>
public sealed class ConfigEnvironmentProvider : IEnvironmentProvider
{
    private readonly IConfigLoader _configLoader;

    public ConfigEnvironmentProvider(IConfigLoader configLoader)
    {
        _configLoader = configLoader;
    }

    public Task<EnvironmentConfiguration> GetEnvironmentAsync(string name, CancellationToken cancellationToken = default)
        => Task.FromResult(_configLoader.LoadEnvironment(name));

    public Task<bool> EnvironmentExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            _configLoader.LoadEnvironment(name);
            return Task.FromResult(true);
        }
        catch (MigrationConfigurationException)
        {
            return Task.FromResult(false);
        }
    }

    public Task<IReadOnlyList<string>> GetAvailableEnvironmentsAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(_configLoader.GetAvailableEnvironments());
}
