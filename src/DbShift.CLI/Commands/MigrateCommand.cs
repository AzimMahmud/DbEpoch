using System.ComponentModel;
using System.Diagnostics;
using DbShift.CLI.Helpers;
using DbShift.Core.ValueObjects;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DbShift.CLI.Commands;

public sealed class MigrateCommand : CliCommandBase<MigrateCommand.Settings>
{
    public sealed class Settings : GlobalSettings
    {
        [CommandOption("-u|--executed-by")]
        [Description("User performing the deployment")]
        public string? ExecutedBy { get; set; }

        [CommandOption("--approver")]
        [Description("Approver identity (required for approval-gated environments)")]
        public string? Approver { get; set; }

        [CommandOption("-b|--batch-size")]
        [Description("Override the migration batch size")]
        public int? BatchSize { get; set; }

        [CommandOption("-f|--force")]
        [Description("Proceed even outside the deployment window")]
        public bool Force { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var host = CreateHost(settings);
        var live = RequireLive(settings, host);
        if (live != 0)
        {
            return live;
        }

        if (!TryResolveEnvironment(settings, host, out var environment))
        {
            return Fail(settings, $"Environment '{host.EnvironmentName}' is not configured.");
        }

        if (!settings.Json)
        {
            ConsoleHelper.PrintHeader($"Deploying migrations to '{host.EnvironmentName}'");
        }

        if (!settings.Force && environment.DeploymentWindow is { Enabled: true } window)
        {
            if (!IsWithinDeploymentWindow(window, out var reason))
            {
                return Fail(settings, $"Outside the configured deployment window. {reason} (override with --force)");
            }
        }

        var executedBy = settings.ExecutedBy ?? Environment.UserName;

        var planningContext = new MigrationContext { Environment = host.EnvironmentName, ExecutedBy = executedBy };
        var dryRun = await ConsoleHelper.RunWithSpinner("Computing pending migrations", () => host.Executor.DryRunAsync(planningContext));
        if (!dryRun.IsSuccess)
        {
            return Fail(settings, dryRun.ErrorMessage ?? "Failed to compute the execution plan.");
        }

        var plan = dryRun.ExecutionPlan!;
        if (plan.TotalCount == 0)
        {
            if (settings.Json)
            {
                WriteJson(new { success = true, applied = 0, message = "up to date" });
            }
            else
            {
                ConsoleHelper.PrintSuccess("The database is up to date - nothing to deploy.");
            }
            return 0;
        }

        if (!settings.Json)
        {
            ConsoleHelper.PrintMigrationTable("Pending migrations",
                plan.Items.Select(i => (i.Version, i.Name, i.Type.ToString(), "Pending", i.HasRollback ? "rollback available" : "no rollback")));
        }

        var approver = await ResolveApproverAsync(settings, environment);
        if (environment.Migration.RequireApproval && string.IsNullOrWhiteSpace(approver))
        {
            return Fail(settings, "This environment requires approval. Provide --approver or confirm interactively.");
        }

        if (!settings.Json && !settings.AssumeYes)
        {
            if (!ConsoleHelper.Confirm($"Apply {plan.TotalCount} migration(s) to '{host.EnvironmentName}'?", false))
            {
                ConsoleHelper.PrintWarning("Deployment cancelled.");
                return 1;
            }
        }

        var deployContext = new MigrationContext
        {
            Environment = host.EnvironmentName,
            ExecutedBy = executedBy,
            Approver = approver,
            BatchSize = settings.BatchSize,
            StopOnFailure = true,
            Force = settings.Force,
            SkipApproval = settings.AssumeYes
        };

        var stopwatch = Stopwatch.StartNew();
        var result = await ConsoleHelper.RunWithSpinner($"Applying {plan.TotalCount} migration(s)", () => host.Executor.DeployAsync(deployContext));
        stopwatch.Stop();

        if (settings.Json)
        {
            WriteJson(new
            {
                success = result.IsSuccess,
                environment = host.EnvironmentName,
                applied = result.TotalApplied,
                appliedMigrations = result.AppliedMigrations,
                failedMigrations = result.FailedMigrations,
                elapsedMs = stopwatch.ElapsedMilliseconds,
                error = result.ErrorMessage
            });
            return result.IsSuccess ? 0 : 1;
        }

        ConsoleHelper.PrintSummary("Deployment result", new[]
        {
            ("environment", host.EnvironmentName, Theme.Text),
            ("applied", result.TotalApplied.ToString(), Theme.Success),
            ("failed", result.FailedMigrations.Count.ToString(), result.FailedMigrations.Count == 0 ? Theme.Text : Theme.Danger),
            ("elapsed", $"{stopwatch.Elapsed.TotalSeconds:0.00}s", Theme.Muted),
            ("approver", approver ?? "n/a", Theme.Muted)
        });

        if (result.AppliedMigrations.Count > 0)
        {
            ConsoleHelper.PrintList("Applied", result.AppliedMigrations);
        }
        if (result.FailedMigrations.Count > 0)
        {
            ConsoleHelper.PrintList("Failed", result.FailedMigrations);
        }

        if (result.IsSuccess)
        {
            ConsoleHelper.PrintSuccess($"Successfully applied {result.TotalApplied} migration(s).");
            return 0;
        }

        ConsoleHelper.PrintError(result.ErrorMessage ?? "Deployment completed with errors.");
        return 1;
    }

    private async Task<string?> ResolveApproverAsync(Settings settings, EnvironmentConfiguration environment)
    {
        if (!environment.Migration.RequireApproval)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(settings.Approver))
        {
            return settings.Approver;
        }

        if (settings.Json || settings.AssumeYes)
        {
            return null;
        }

        var approver = AnsiConsole.Ask<string>($"[bold {Theme.Primary}]Approver identity:[/] ");
        await Task.CompletedTask;
        return approver;
    }

    private static bool IsWithinDeploymentWindow(DeploymentWindow window, out string reason)
    {
        var now = DateTime.Now;
        reason = string.Empty;

        if (window.AllowedDays.Count > 0)
        {
            var today = now.DayOfWeek.ToString();
            if (!window.AllowedDays.Contains(today, StringComparer.OrdinalIgnoreCase))
            {
                reason = $"Today ({today}) is not an allowed day. Allowed: {string.Join(", ", window.AllowedDays)}.";
                return false;
            }
        }

        if (TimeSpan.TryParse(window.StartTime, out var start) && TimeSpan.TryParse(window.EndTime, out var end))
        {
            var time = now.TimeOfDay;
            if (time < start || time > end)
            {
                reason = $"Current time {now:HH:mm} is outside {window.StartTime}-{window.EndTime}.";
                return false;
            }
        }

        return true;
    }
}
