using DbShift.Core.Exceptions;
using DbShift.Engine.Parsing;
using Xunit;

namespace DbShift.Engine.Tests;

public class ExceptionTypeTests
{
    private readonly ScriptParser _parser = new();

    [Fact]
    public void ScriptParseException_IsDbShiftException()
    {
        try
        {
            _parser.Parse("/migrations/BadName.sql", "SELECT 1;");
            Assert.Fail("Expected ScriptParseException");
        }
        catch (ScriptParseException ex)
        {
            Assert.IsAssignableFrom<DbShiftException>(ex);
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
    public void MigrationConfigurationException_IsDbShiftException()
    {
        var ex = new MigrationConfigurationException("test");
        Assert.IsAssignableFrom<DbShiftException>(ex);
    }

    [Fact]
    public void UnsupportedProviderException_IsDbShiftException()
    {
        var ex = new UnsupportedProviderException("test");
        Assert.IsAssignableFrom<DbShiftException>(ex);
    }
}
