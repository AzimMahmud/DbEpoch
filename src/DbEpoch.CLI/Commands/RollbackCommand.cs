using System.ComponentModel;
using DbEpoch.CLI.Helpers;
using DbEpoch.Core.ValueObjects;
using Spectre.Console.Cli;

namespace DbEpoch.CLI.Commands;

public sealed class RollbackCommand : CliCommandBase<RollbackCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandOption("-V|--target-version")]
        [Description("Specific version to roll back (default: last)")]
        public string? TargetVersion { get; set; }

        [CommandOption("--version", IsHidden = true)]
        [Description("Deprecated alias for --target-version")]
        public string? LegacyVersion { get; set; }

        [CommandOption("--count")]
        [Description("Number of recent migrations to roll back")]
        public int Count { get; set; } = 1;

        [CommandOption("-u|--executed-by")]
        [Description("User performing the rollback")]
        public string? ExecutedBy { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var host = CreateHost(settings);
        var live = RequireLive(settings, host);
        if (live != 0)
        {
            return live;
        }

        if (!settings.Json)
        {
            var title = host.Module is null
                ? $"Rolling back migrations on '{host.EnvironmentName}'"
                : $"Rolling back migrations on '{host.EnvironmentName}' (module: {host.Module})";
            ConsoleHelper.PrintHeader(title);
        }

        var status = await host.Executor.GetStatusAsync(host.EnvironmentName);
        if (status.Applied == 0)
        {
            if (settings.Json)
            {
                WriteJson(new { success = true, rolledBack = 0 });
            }
            else
            {
                ConsoleHelper.PrintInfo("No applied migrations to roll back.");
            }
            return 0;
        }

        var version = !string.IsNullOrWhiteSpace(settings.TargetVersion)
            ? settings.TargetVersion
            : settings.LegacyVersion;

        if (!string.IsNullOrWhiteSpace(settings.LegacyVersion) && string.IsNullOrWhiteSpace(settings.TargetVersion) && !settings.Json)
        {
            ConsoleHelper.PrintWarning("The --version option is deprecated; use --target-version instead.");
        }

        var request = new RollbackRequest
        {
            Version = string.IsNullOrWhiteSpace(version) ? "last" : version,
            Count = settings.Count,
            Environment = host.EnvironmentName,
            ExecutedBy = settings.ExecutedBy ?? Environment.UserName
        };

        if (!settings.Json)
        {
            var label = request.Version.Equals("last", StringComparison.OrdinalIgnoreCase)
                ? $"the last {request.Count} migration(s)"
                : $"migration '{request.Version}'";
            ConsoleHelper.PrintWarning($"This will roll back {label} on '{host.EnvironmentName}'.");
            if (!settings.AssumeYes && !ConsoleHelper.Confirm("Proceed with rollback?", false))
            {
                ConsoleHelper.PrintWarning("Rollback cancelled.");
                return 1;
            }
        }

        var result = await ConsoleHelper.RunWithSpinner("Rolling back migrations", () => host.Executor.RollbackAsync(request));

        if (settings.Json)
        {
            WriteJson(new { success = result.IsSuccess, rolledBack = result.RolledBackMigrations, error = result.ErrorMessage });
            return result.IsSuccess ? 0 : 1;
        }

        if (result.RolledBackMigrations.Count > 0)
        {
            ConsoleHelper.PrintList("Rolled back", result.RolledBackMigrations);
        }
        if (result.IsSuccess)
        {
            ConsoleHelper.PrintSuccess($"Rolled back {result.RolledBackMigrations.Count} migration(s).");
            return 0;
        }
        ConsoleHelper.PrintError(result.ErrorMessage ?? "Rollback failed.");
        return 1;
    }
}
