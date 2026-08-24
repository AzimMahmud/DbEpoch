using DbEpoch.CLI.Commands;
using DbEpoch.CLI.Helpers;
using Spectre.Console;
using Spectre.Console.Cli;

var app = new CommandApp();

app.Configure(config =>
{
    config.SetApplicationName("DbEpoch");
    config.SetApplicationVersion(FileVersionInfo.GetVersionString());
    config.PropagateExceptions();

    config.AddCommand<NewCommand>("new")
        .WithDescription("Scaffold a complete DbEpoch project structure in the current directory.")
        .WithAlias("scaffold")
        .WithAlias("init-project");

    config.AddCommand<InitCommand>("init")
        .WithDescription("Create the migration tracking schema on the target database.");

    config.AddCommand<ValidateCommand>("validate")
        .WithDescription("Validate migration scripts (naming, syntax, duplicates, dependencies).");

    config.AddCommand<StatusCommand>("status")
        .WithDescription("Show the migration status for an environment.");

    config.AddCommand<PlanCommand>("plan")
        .WithDescription("Compute and display the pending migration execution plan (dry-run).")
        .WithAlias("dry-run");

    config.AddCommand<MigrateCommand>("migrate")
        .WithDescription("Apply pending migrations to the target environment.")
        .WithAlias("deploy")
        .WithAlias("apply");

    config.AddCommand<RollbackCommand>("rollback")
        .WithDescription("Roll back one or more previously applied migrations.");

    config.AddCommand<RepairCommand>("repair")
        .WithDescription("Repair the migration history (re-queue a failed migration).");

    config.AddCommand<HistoryCommand>("history")
        .WithDescription("Show the audit history for an environment.")
        .WithAlias("audit");

    config.AddCommand<CreateCommand>("create")
        .WithDescription("Scaffold a new migration script from a template.");

    config.AddCommand<InfoCommand>("info")
        .WithDescription("Show configuration, environments and repository details.")
        .WithAlias("config");
});

// Handle --no-color before Spectre.Console starts rendering.
var noColor = args.Contains("--no-color", StringComparer.OrdinalIgnoreCase);
if (noColor)
{
    try
    {
        AnsiConsole.Console = AnsiConsole.Create(new AnsiConsoleSettings { ColorSystem = ColorSystemSupport.NoColors });
    }
    catch (Exception) { /* ignore - best effort */ }
}

// Suppress interactive UI when --json is active.
ConsoleHelper.UiSuppressed = args.Contains("--json", StringComparer.OrdinalIgnoreCase);

try
{
    return await app.RunAsync(args);
}
catch (Exception ex)
{
    ConsoleHelper.RenderException(ex);
    return 1;
}
