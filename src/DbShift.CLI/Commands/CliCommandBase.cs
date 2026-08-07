using System.Text.Json;
using DbShift.CLI.Helpers;
using DbShift.Core.ValueObjects;
using Spectre.Console.Cli;

namespace DbShift.CLI.Commands;

/// <summary>Shared helpers for all commands: host wiring, environment/live gating and JSON output.</summary>
public abstract class CliCommandBase<TSettings> : AsyncCommand<TSettings> where TSettings : GlobalSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    protected CliHost CreateHost(GlobalSettings settings)
    {
        return CliHost.Create(new CliHostOptions(
            settings.Environment,
            settings.Provider,
            settings.ConnectionString,
            settings.Config,
            settings.UseInMemory,
            settings.Verbose,
            settings.Module));
    }

    protected int Fail(GlobalSettings settings, string message)
    {
        if (settings.Json)
        {
            WriteJson(new { success = false, error = message });
            return 1;
        }
        ConsoleHelper.RenderException(new InvalidOperationException(message));
        return 1;
    }

    protected bool TryResolveEnvironment(GlobalSettings settings, CliHost host, out EnvironmentConfiguration environment)
    {
        try
        {
            environment = host.ConfigLoader.LoadEnvironment(host.EnvironmentName);
            return true;
        }
        catch (Exception ex)
        {
            environment = null!;
            if (!settings.Json)
            {
                ConsoleHelper.PrintError(ex.Message);
            }
            return false;
        }
    }

    protected int RequireLive(GlobalSettings settings, CliHost host)
    {
        if (host.IsLive)
        {
            return 0;
        }
        return Fail(settings, "This command requires a live database connection. Set a connection string via --connection-string, the DB_CONNECTION_STRING environment variable, or migration.json.");
    }

    protected void WriteJson(object value)
    {
        Console.Out.WriteLine(JsonSerializer.Serialize(value, value.GetType(), JsonOptions));
    }
}
