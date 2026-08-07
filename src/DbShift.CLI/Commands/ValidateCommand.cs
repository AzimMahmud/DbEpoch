using System.ComponentModel;
using DbShift.CLI.Helpers;
using DbShift.Core.ValueObjects;
using Spectre.Console.Cli;

namespace DbShift.CLI.Commands;

public sealed class ValidateCommand : CliCommandBase<ValidateCommand.Settings>
{
    public sealed class Settings : GlobalSettings { }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var host = CreateHost(settings);
        if (!settings.Json)
        {
            var title = host.Module is null
                ? $"Validating migrations for '{host.EnvironmentName}'"
                : $"Validating migrations for '{host.EnvironmentName}' (module: {host.Module})";
            ConsoleHelper.PrintHeader(title);
        }

        ValidationResult result;
        try
        {
            result = await ConsoleHelper.RunWithSpinner("Scanning and validating scripts",
                () => host.Executor.ValidateAsync(host.EnvironmentName));
        }
        catch (Exception ex)
        {
            return Fail(settings, ex.Message);
        }

        if (settings.Json)
        {
            WriteJson(new
            {
                success = result.IsValid,
                environment = host.EnvironmentName,
                scriptsChecked = result.ScriptsChecked,
                errors = result.Errors,
                warnings = result.Warnings
            });
            return result.IsValid ? 0 : 1;
        }

        ConsoleHelper.PrintSummary("Result", new[]
        {
            ("scripts checked", result.ScriptsChecked.ToString(), Theme.Text),
            ("errors", result.Errors.Count.ToString(), result.Errors.Count == 0 ? Theme.Success : Theme.Danger),
            ("warnings", result.Warnings.Count.ToString(), result.Warnings.Count == 0 ? Theme.Text : Theme.Warning),
            ("status", result.IsValid ? "VALID" : "INVALID", result.IsValid ? Theme.Success : Theme.Danger)
        });

        if (result.Errors.Count > 0)
        {
            ConsoleHelper.PrintList("Errors", result.Errors);
        }
        if (result.Warnings.Count > 0)
        {
            ConsoleHelper.PrintList("Warnings", result.Warnings);
        }

        if (result.IsValid)
        {
            ConsoleHelper.PrintSuccess("All migration scripts are valid.");
        }
        else
        {
            ConsoleHelper.PrintError($"{result.Errors.Count} validation error(s) found.");
        }
        return result.IsValid ? 0 : 1;
    }
}
