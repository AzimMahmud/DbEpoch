using DbEpoch.Core.Exceptions;
using DbEpoch.Engine.Parsing;
using Xunit;

namespace DbEpoch.Engine.Tests;

public class ExceptionTypeTests
{
    private readonly ScriptParser _parser = new();

    [Fact]
    public void ScriptParseException_IsDbEpochException()
    {
        try
        {
            _parser.Parse("/migrations/BadName.sql", "SELECT 1;");
            Assert.Fail("Expected ScriptParseException");
        }
        catch (ScriptParseException ex)
        {
            Assert.IsAssignableFrom<DbEpochException>(ex);
        }
    }

    [Fact]
    public void ScriptParseException_ContainsFileName()
    {
        try
        {
            _parser.Parse("/migrations/BadName.sql", "SELECT 1;");
            Assert.Fail("Expected ScriptParseException");
        }
        catch (ScriptParseException ex)
        {
            Assert.Contains("BadName.sql", ex.Message);
        }
    }

    [Fact]
    public void MigrationConfigurationException_IsDbEpochException()
    {
        var ex = new MigrationConfigurationException("test");
        Assert.IsAssignableFrom<DbEpochException>(ex);
    }

    [Fact]
    public void UnsupportedProviderException_IsDbEpochException()
    {
        var ex = new UnsupportedProviderException("test");
        Assert.IsAssignableFrom<DbEpochException>(ex);
    }
}
