using dbsh.CLI.Helpers;
using dbsh.Core.Enums;
using Spectre.Console.Cli;

namespace dbsh.CLI.Commands;

public sealed class StatusCommand : CliCommandBase<StatusCommand.Settings>
{
    public sealed class Settings : GlobalSettings { }

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
                ? $"Migration status for '{host.EnvironmentName}'"
                : $"Migration status for '{host.EnvironmentName}' (module: {host.Module})";
            ConsoleHelper.PrintHeader(title);
        }

        var status = await ConsoleHelper.RunWithSpinner("Querying migration history",
            () => host.Executor.GetStatusAsync(host.EnvironmentName));

        if (settings.Json)
        {
            WriteJson(new
            {
                success = true,
                environment = status.Environment,
                total = status.Total,
                applied = status.Applied,
                pending = status.Pending,
                failed = status.Failed,
                migrations = status.Records.Select(r => new
                {
                    version = r.Version,
                    name = r.Name,
                    type = r.Type.ToString(),
                    status = r.Status.ToString(),
                    executedAtUtc = r.ExecutedAtUtc,
                    executionTimeMs = r.ExecutionTimeMs
                })
            });
            return 0;
        }

        ConsoleHelper.PrintSummary("Summary", new[]
        {
            ("environment", status.Environment, Theme.Text),
            ("total", status.Total.ToString(), Theme.Text),
            ("applied", status.Applied.ToString(), Theme.Success),
            ("pending", status.Pending.ToString(), Theme.Warning),
            ("failed", status.Failed.ToString(), status.Failed == 0 ? Theme.Text : Theme.Danger)
        });

        if (status.Records.Count == 0)
        {
            ConsoleHelper.PrintInfo("No migrations have been recorded for this environment yet.");
            return 0;
        }

        ConsoleHelper.PrintMigrationTable("Migrations",
            status.Records.Select(r => (r.Version, r.Name, r.Type.ToString(), r.Status.ToString(),
                r.Status == MigrationStatus.Failed
                    ? (r.ErrorMessage ?? string.Empty)
                    : (r.ExecutedAtUtc == default ? string.Empty : r.ExecutedAtUtc.ToString("u")))));

        return 0;
    }
}
