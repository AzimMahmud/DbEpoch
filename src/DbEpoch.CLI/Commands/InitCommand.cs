using DbEpoch.CLI.Helpers;
using Spectre.Console.Cli;

namespace DbEpoch.CLI.Commands;

public sealed class InitCommand : CliCommandBase<InitCommand.Settings>
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
            ConsoleHelper.PrintHeader($"Initialising tracking schema on '{host.EnvironmentName}'");
        }

        var result = await ConsoleHelper.RunWithSpinner("Creating tracking tables", () => host.Executor.InitAsync());

        if (settings.Json)
        {
            WriteJson(new { success = result.IsSuccess, created = result.CreatedObjects, error = result.ErrorMessage });
            return result.IsSuccess ? 0 : 1;
        }

        if (!result.IsSuccess)
        {
            return Fail(settings, result.ErrorMessage ?? "Failed to create tracking schema.");
        }

        ConsoleHelper.PrintList("Created tables", result.CreatedObjects);
        ConsoleHelper.PrintSuccess("Tracking schema is ready.");
        return 0;
    }
}
