using System.ComponentModel;
using Spectre.Console.Cli;

namespace DbShift.CLI.Commands;

/// <summary>Shared options available on every command, inherited by per-command settings.</summary>
public abstract class GlobalSettings : CommandSettings
{
    [CommandOption("-e|--environment")]
    [Description("Target environment")]
    public string Environment { get; set; } = "local";

    [CommandOption("-p|--provider")]
    [Description("Database provider: postgresql | sqlserver | mysql | sqlite")]
    public string? Provider { get; set; }

    [CommandOption("-c|--connection-string")]
    [Description("Database connection string override")]
    public string? ConnectionString { get; set; }

    [CommandOption("--config")]
    [Description("Path to the repository root (config base)")]
    public string? Config { get; set; }

    [CommandOption("--in-memory")]
    [Description("Force offline in-memory mode (no database)")]
    public bool UseInMemory { get; set; }

    [CommandOption("-y|--yes")]
    [Description("Skip interactive prompts")]
    public bool AssumeYes { get; set; }

    [CommandOption("--no-color")]
    [Description("Disable colored output")]
    public bool NoColor { get; set; }

    [CommandOption("--json")]
    [Description("Emit machine-readable JSON")]
    public bool Json { get; set; }

    [CommandOption("--verbose")]
    [Description("Show detailed informational output")]
    public bool Verbose { get; set; }
}
