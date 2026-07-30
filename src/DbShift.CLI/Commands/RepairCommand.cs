using System.ComponentModel;
using DbShift.CLI.Helpers;
using Spectre.Console.Cli;

namespace DbShift.CLI.Commands;

public sealed class RepairCommand : CliCommandBase<RepairCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandOption("-V|--version")]
        [Description("Specific version to repair (omit to repair all failed migrations)")]
        public string? Version { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var host = CreateHost(settings);
        var live = RequireLive(settings, host);
        if (live != 0)
        {
            return live;
        }

        var version = settings.Version;
        var label = string.IsNullOrWhiteSpace(version) ? "all failed migrations" : $"migration {version}";

        if (!settings.Json)
        {
            ConsoleHelper.PrintHeader($"Repairing '{host.EnvironmentName}'");
        }

        var result = await ConsoleHelper.RunWithSpinner($"Repairing {label}", () => host.Executor.RepairAsync(host.EnvironmentName, version));

        if (settings.Json)
        {
            WriteJson(new { success = result.IsSuccess, repaired = result.RepairedMigrations, error = result.ErrorMessage });
            return result.IsSuccess ? 0 : 1;
        }

        if (result.RepairedMigrations.Count > 0)
        {
            foreach (var repaired in result.RepairedMigrations)
            {
                ConsoleHelper.PrintSuccess($"Repaired migration '{repaired}'.");
            }
        }
        else
        {
            ConsoleHelper.PrintInfo("No failed migrations need repair.");
        }
        return result.IsSuccess ? 0 : 1;
    }
}
