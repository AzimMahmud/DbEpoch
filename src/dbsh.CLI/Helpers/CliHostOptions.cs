namespace dbsh.CLI.Helpers;

/// <summary>Decouples CliHost from any specific CLI framework's context type.</summary>
public sealed record CliHostOptions(
    string EnvironmentName,
    string? Provider,
    string? ConnectionString,
    string? ConfigBasePath,
    bool UseInMemory,
    bool Verbose = false,
    string? Module = null);
