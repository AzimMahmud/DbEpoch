using DbShift.Core.Enums;
using DbShift.Core.Exceptions;
using DbShift.Engine.Parsing;
using Xunit;

namespace DbShift.Engine.Tests;

public class ScriptParserEdgeCaseTests
{
    private readonly ScriptParser _parser = new();

    [Fact]
    public void Parse_MissingSeparator_ThrowsScriptParseException()
    {
        Assert.Throws<ScriptParseException>(
            () => _parser.Parse("/migrations/V001NoSeparator.sql", "CREATE TABLE t (id INT);"));
    }

    [Fact]
    public void Parse_EmptyVersionDigits_ThrowsScriptParseException()
    {
        Assert.Throws<ScriptParseException>(
            () => _parser.Parse("/migrations/V__EmptyVersion.sql", "CREATE TABLE t (id INT);"));
    }

    [Fact]
    public void Parse_FalsePositivePrefix_RejectedAsBadPrefix()
    {
        // "Video__X.sql" starts with 'V' but is not a valid versioned migration.
        Assert.Throws<ScriptParseException>(
            () => _parser.Parse("/migrations/Video__X.sql", "SELECT 1;"));
    }

    [Fact]
    public void Parse_UnknownPrefix_ThrowsScriptParseException()
    {
        Assert.Throws<ScriptParseException>(
            () => _parser.Parse("/migrations/X001__Unknown.sql", "SELECT 1;"));
    }

    [Fact]
    public void Parse_CrlfContent_ProducesSameHashAsLfContent()
    {
        var lfContent = "-- Migration: Test\nCREATE TABLE t (id INT);\n";
        var crlfContent = "-- Migration: Test\r\nCREATE TABLE t (id INT);\r\n";

        var lfResult = _parser.Parse("/migrations/V001__Test.sql", lfContent);
        var crlfResult = _parser.Parse("/migrations/V001__Test.sql", crlfContent);

        Assert.Equal(lfResult.Hash, crlfResult.Hash);
    }

    [Fact]
    public void GenerateHash_NormalizesLineEndings()
    {
        var lfContent = "CREATE TABLE t (id INT);\nINSERT INTO t VALUES (1);\n";
        var crlfContent = "CREATE TABLE t (id INT);\r\nINSERT INTO t VALUES (1);\r\n";

        Assert.Equal(_parser.GenerateHash(lfContent), _parser.GenerateHash(crlfContent));
    }

    [Fact]
    public void Parse_DataCategory_ReturnsDataFolder()
    {
        var result = _parser.Parse("/migrations/Data/V001__SeedData.sql", "INSERT INTO t VALUES (1);");
        Assert.Equal(MigrationType.Data, result.Type);
        Assert.Equal("Data", result.Category);
    }

    [Fact]
    public void Parse_PatchCategory_ReturnsPatchFolder()
    {
        var result = _parser.Parse("/migrations/Patch/V001__Hotfix.sql", "UPDATE t SET id = 2;");
        Assert.Equal(MigrationType.Patch, result.Type);
        Assert.Equal("Patch", result.Category);
    }

    [Fact]
    public void Parse_RollbackInSchemaFolder_StillDetectedByPrefix()
    {
        var result = _parser.Parse("/migrations/Schema/U001__Rollback_Table.sql", "DROP TABLE t;");
        Assert.Equal(MigrationType.Rollback, result.Type);
    }

    [Fact]
    public void Parse_LongTimestampVersion_ReturnsCorrectVersion()
    {
        var result = _parser.Parse("/migrations/Schema/V20260713143022__AddIndex.sql", "CREATE INDEX idx ON t (col);");
        Assert.Equal("20260713143022", result.Version);
    }

    [Fact]
    public void HasExecutableContent_OnlyWhitespace_ReturnsFalse()
    {
        Assert.False(_parser.HasExecutableContent("\n\n\r\n\t  \t\n"));
    }

    [Fact]
    public void HasExecutableContent_MixedCommentsAndBlankLines_ReturnsFalse()
    {
        Assert.False(_parser.HasExecutableContent("-- header\n\n   \n-- trailing\n"));
    }

    [Fact]
    public void ExtractDependencies_MultipleFormats_ReturnsAll()
    {
        var content = "-- Depends: V001__Base.sql, V002__Next.sql\nCREATE TABLE t (id INT);";
        var deps = _parser.ExtractDependencies(content);
        Assert.Equal(2, deps.Length);
    }

    [Fact]
    public void ExtractDependencies_SingleDependency_ReturnsOne()
    {
        var content = "-- depends: V001__Base.sql\nSELECT 1;";
        var deps = _parser.ExtractDependencies(content);
        Assert.Single(deps);
        Assert.Equal("V001__Base.sql", deps[0]);
    }
}
