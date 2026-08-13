using System.ComponentModel;
using DbShift.CLI.Helpers;
using Spectre.Console.Cli;

namespace DbShift.CLI.Commands;

public sealed class RepairCommand : CliCommandBase<RepairCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandOption("-V|--target-version")]
        [Description("Specific version to repair (omit to repair all failed migrations)")]
        public string? TargetVersion { get; set; }

        [CommandOption("--version", IsHidden = true)]
        [Description("Deprecated alias for --target-version")]
        public string? LegacyVersion { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var host = CreateHost(settings);
        var live = RequireLive(settings, host);
        if (live != 0)
        {
            return live;
        }

        var version = !string.IsNullOrWhiteSpace(settings.TargetVersion)
            ? settings.TargetVersion
            : settings.LegacyVersion;

        if (!string.IsNullOrWhiteSpace(settings.LegacyVersion) && string.IsNullOrWhiteSpace(settings.TargetVersion) && !settings.Json)
        {
            ConsoleHelper.PrintWarning("The --version option is deprecated; use --target-version instead.");
        }

        var label = string.IsNullOrWhiteSpace(version) ? "all failed migrations" : $"migration {version}";

        if (!settings.Json)
        {
            var title = host.Module is null
                ? $"Repairing '{host.EnvironmentName}'"
                : $"Repairing '{host.EnvironmentName}' (module: {host.Module})";
            ConsoleHelper.PrintHeader(title);
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
