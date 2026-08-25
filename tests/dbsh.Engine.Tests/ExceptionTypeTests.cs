using dbsh.Core.Exceptions;
using dbsh.Engine.Parsing;
using Xunit;

namespace dbsh.Engine.Tests;

public class ExceptionTypeTests
{
    private readonly ScriptParser _parser = new();

    [Fact]
    public void ScriptParseException_IsDbshException()
    {
        try
        {
            _parser.Parse("/migrations/BadName.sql", "SELECT 1;");
            Assert.Fail("Expected ScriptParseException");
        }
        catch (ScriptParseException ex)
        {
            Assert.IsAssignableFrom<dbshException>(ex);
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
    public void MigrationConfigurationException_IsDbshException()
    {
        var ex = new MigrationConfigurationException("test");
        Assert.IsAssignableFrom<dbshException>(ex);
    }

    [Fact]
    public void UnsupportedProviderException_IsDbshException()
    {
        var ex = new UnsupportedProviderException("test");
        Assert.IsAssignableFrom<dbshException>(ex);
    }
}
