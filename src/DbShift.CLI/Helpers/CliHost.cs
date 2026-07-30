using DbShift.Core.Interfaces;
using DbShift.Core.ValueObjects;
using DbShift.Engine.Execution;
using DbShift.Engine.InMemory;
using DbShift.Engine.Parsing;
using DbShift.Infrastructure.Database;
using DbShift.Infrastructure.Database.Providers;
using DbShift.Infrastructure.FileSystem;
using Microsoft.Extensions.Logging;

namespace DbShift.CLI.Helpers;

/// <summary>
/// Composition root for a single CLI invocation. Resolves the database provider from
/// configuration and wires either live (relational) or in-memory implementations.
/// </summary>
public sealed class CliHost
{
    public MigrationExecutor Executor { get; }
    public IConfigLoader ConfigLoader { get; }
    public MigrationConfiguration? Config { get; }
    public string EnvironmentName { get; }
    public bool IsLive { get; }
    public string? ConnectionString { get; }
    public string ProviderName { get; }
    public string ScriptsPath { get; }
    public string BasePath { get; }

    private CliHost(MigrationExecutor executor, IConfigLoader configLoader, MigrationConfiguration? config,
        string environmentName, bool isLive, string? connectionString, string providerName, string scriptsPath, string basePath)
    {
        Executor = executor;
        ConfigLoader = configLoader;
        Config = config;
        EnvironmentName = environmentName;
        IsLive = isLive;
        ConnectionString = connectionString;
        ProviderName = providerName;
        ScriptsPath = scriptsPath;
        BasePath = basePath;
    }

    public static CliHost Create(CliHostOptions options)
    {
        var basePath = string.IsNullOrWhiteSpace(options.ConfigBasePath)
            ? Directory.GetCurrentDirectory()
            : Path.GetFullPath(options.ConfigBasePath);

        var configLoader = new FileSystemConfigLoader(basePath);

        MigrationConfiguration? config = null;
        try
        {
            config = configLoader.LoadMigrationConfiguration();
        }
        catch (Exception)
        {
            // Some commands (create, info, help) work fine without a config file.
        }

        var environment = string.IsNullOrWhiteSpace(options.EnvironmentName) ? "local" : options.EnvironmentName;

        var connectionString = ResolveConnectionString(options.ConnectionString, config, configLoader, environment);
        var providerName = !string.IsNullOrWhiteSpace(options.Provider) ? options.Provider : (config?.Provider ?? "postgresql");
        var scriptsPath = ResolveScriptsPath(basePath, config);
        var commandTimeout = config?.CommandTimeoutSeconds ?? 3600;

        var preferInMemory = options.UseInMemory || string.IsNullOrWhiteSpace(connectionString);
        var logger = new SpectreLogger<MigrationExecutor>(options.Verbose);

        IMigrationTracker tracker;
        IMigrationLockManager lockManager;
        IAuditLogger auditLogger;
        IEnvironmentProvider environmentProvider;
        IMigrationScriptExecutor? scriptExecutor;

        if (preferInMemory)
        {
            tracker = new InMemoryMigrationTracker();
            lockManager = new InMemoryMigrationLockManager();
            auditLogger = new InMemoryAuditLogger();
            environmentProvider = new InMemoryEnvironmentProvider();
            scriptExecutor = null;
        }
        else
        {
            var provider = DatabaseProviderFactory.Create(providerName);
            tracker = new RelationalMigrationTracker(provider, connectionString!);
            lockManager = new RelationalMigrationLockManager(provider, connectionString!);
            auditLogger = new RelationalAuditLogger(provider, connectionString!);
            environmentProvider = new ConfigEnvironmentProvider(configLoader);
            scriptExecutor = new RelationalMigrationExecutor(provider);
            providerName = provider.Name;
        }

        var executor = new MigrationExecutor(
            tracker, lockManager, new ScriptParser(), environmentProvider, auditLogger, logger,
            scriptExecutor, connectionString, commandTimeout, scriptsPath);

        return new CliHost(executor, configLoader, config, environment, !preferInMemory, connectionString, providerName, scriptsPath, basePath);
    }

    private static string? ResolveConnectionString(string? cliOverride, MigrationConfiguration? config, IConfigLoader configLoader, string environment)
    {
        if (!string.IsNullOrWhiteSpace(cliOverride))
        {
            return cliOverride;
        }

        var fromEnv = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        try
        {
            var envConfig = configLoader.LoadEnvironment(environment);
            if (!string.IsNullOrWhiteSpace(envConfig.Database.ConnectionString))
            {
                return envConfig.Database.ConnectionString;
            }
        }
        catch (Exception)
        {
            // Environment file may not exist; fall through to global config.
        }

        return string.IsNullOrWhiteSpace(config?.ConnectionString) ? null : config.ConnectionString;
    }

    private static string ResolveScriptsPath(string basePath, MigrationConfiguration? config)
    {
        if (config is null)
        {
            return Path.Combine(basePath, "Database", "Migrations");
        }

        var configured = config.ScriptsPath;
        return Path.IsPathRooted(configured)
            ? configured
            : Path.GetFullPath(Path.Combine(basePath, configured));
    }

    /// <summary>
    /// Routes engine log output through Spectre.Console. Warnings and errors are surfaced
    /// via <see cref="ConsoleHelper"/>; informational messages are shown only in verbose mode.
    /// All output is suppressed when <see cref="ConsoleHelper.UiSuppressed"/> is set (JSON mode).
    /// </summary>
    private sealed class SpectreLogger<T> : ILogger<T>
    {
        private readonly bool _verbose;

        public SpectreLogger(bool verbose = false)
        {
            _verbose = verbose;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) =>
            !ConsoleHelper.UiSuppressed && (logLevel >= LogLevel.Warning || (_verbose && logLevel >= LogLevel.Information));

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (ConsoleHelper.UiSuppressed)
            {
                return;
            }

            var message = formatter(state, exception);

            switch (logLevel)
            {
                case LogLevel.Information:
                    if (_verbose)
                    {
                        ConsoleHelper.PrintInfo(message);
                    }
                    break;
                case LogLevel.Warning:
                    ConsoleHelper.PrintWarning(message);
                    break;
                case LogLevel.Error or LogLevel.Critical:
                    ConsoleHelper.PrintError(message);
                    break;
            }
        }
    }
}
