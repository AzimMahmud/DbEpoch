namespace DbEpoch.Core.Exceptions;

/// <summary>Base type for all DbEpoch-specific exceptions. Catch this to handle any tool error.</summary>
public abstract class DbEpochException : Exception
{
    protected DbEpochException(string message) : base(message) { }
    protected DbEpochException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Thrown when a migration configuration file is missing, invalid, or contains an unsafe value.</summary>
public sealed class MigrationConfigurationException : DbEpochException
{
    public MigrationConfigurationException(string message) : base(message) { }
    public MigrationConfigurationException(string message, Exception inner) : base(message, inner) { }
}

/// <summary>Thrown when a migration script filename does not match the expected naming convention.</summary>
public sealed class ScriptParseException : DbEpochException
{
    public ScriptParseException(string message) : base(message) { }
}

/// <summary>Thrown when the requested database provider is not recognised.</summary>
public sealed class UnsupportedProviderException : DbEpochException
{
    public UnsupportedProviderException(string message) : base(message) { }
}
