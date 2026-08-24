using DbEpoch.Core.Enums;
using DbEpoch.Core.Exceptions;
using DbEpoch.Engine.Parsing;
using Xunit;

namespace DbEpoch.Engine.Tests;

public class ScriptParserTests
{
    private readonly ScriptParser _parser = new();

    [Fact]
    public void Parse_VersionBasedMigration_ReturnsCorrectMigration()
    {
        // Arrange
        var filePath = "/Database/Migrations/Schema/V001__CreateUserTable.sql";
        var content = "-- Migration: CreateUserTable\nCREATE TABLE users (id UUID PRIMARY KEY);";

        // Act
        var result = _parser.Parse(filePath, content);

        // Assert
        Assert.Equal("001", result.Version);
        Assert.Equal("CreateUserTable", result.Name);
        Assert.Equal(MigrationType.Schema, result.Type);
        Assert.Equal("Schema", result.Category);
        Assert.NotEmpty(result.Hash);
    }

    [Fact]
    public void Parse_TimestampBasedMigration_ReturnsCorrectMigration()
    {
        // Arrange
        var filePath = "/Database/Migrations/Schema/V202601150001__CreateUserTable.sql";
        var content = "-- Migration: CreateUserTable\nCREATE TABLE users (id UUID PRIMARY KEY);";

        // Act
        var result = _parser.Parse(filePath, content);

        // Assert
        Assert.Equal("202601150001", result.Version);
        Assert.Equal("CreateUserTable", result.Name);
        Assert.Equal(MigrationType.Schema, result.Type);
    }

    [Fact]
    public void Parse_RepeatableMigration_ReturnsRepeatableType()
    {
        // Arrange
        var filePath = "/Database/Migrations/Schema/R__CreateView.sql";
        var content = "-- Repeatable: CreateView\nCREATE OR REPLACE VIEW user_view AS SELECT * FROM users;";

        // Act
        var result = _parser.Parse(filePath, content);

        // Assert
        Assert.Equal("R", result.Version);
        Assert.Equal("CreateView", result.Name);
        Assert.Equal(MigrationType.Repeatable, result.Type);
        Assert.True(result.IsRepeatable);
    }

    [Fact]
    public void Parse_RollbackMigration_ReturnsRollbackType()
    {
        // Arrange
        var filePath = "/Database/Migrations/Rollback/U001__Rollback_CreateUserTable.sql";
        var content = "-- Rollback: CreateUserTable\nDROP TABLE IF EXISTS users;";

        // Act
        var result = _parser.Parse(filePath, content);

        // Assert
        Assert.Equal("001", result.Version);
        Assert.Equal("Rollback_CreateUserTable", result.Name);
        Assert.Equal(MigrationType.Rollback, result.Type);
    }

    [Fact]
    public void Parse_InvalidFileName_ThrowsScriptParseException()
    {
        // Arrange
        var filePath = "/Database/Migrations/Schema/InvalidFormat.sql";
        var content = "-- Invalid";

        // Act & Assert
        Assert.Throws<ScriptParseException>(() => _parser.Parse(filePath, content));
    }

    [Fact]
    public void GenerateHash_SameContent_ReturnsSameHash()
    {
        // Arrange
        var content = "CREATE TABLE users (id UUID PRIMARY KEY);";

        // Act
        var hash1 = _parser.GenerateHash(content);
        var hash2 = _parser.GenerateHash(content);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void GenerateHash_DifferentContent_ReturnsDifferentHash()
    {
        // Arrange
        var content1 = "CREATE TABLE users (id UUID PRIMARY KEY);";
        var content2 = "CREATE TABLE orders (id UUID PRIMARY KEY);";

        // Act
        var hash1 = _parser.GenerateHash(content1);
        var hash2 = _parser.GenerateHash(content2);

        // Assert
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HasExecutableContent_EmptyContent_ReturnsFalse()
    {
        Assert.False(_parser.HasExecutableContent(""));
        Assert.False(_parser.HasExecutableContent("   "));
    }

    [Fact]
    public void HasExecutableContent_CommentOnlyContent_ReturnsFalse()
    {
        // A script containing only comments is a no-op at the database and should be flagged.
        Assert.False(_parser.HasExecutableContent("-- just a comment"));
        Assert.False(_parser.HasExecutableContent("-- line one\n-- line two\n"));
    }

    [Fact]
    public void HasExecutableContent_WithSql_ReturnsTrue()
    {
        Assert.True(_parser.HasExecutableContent("CREATE TABLE test (id INT);"));
        Assert.True(_parser.HasExecutableContent("-- header\nCREATE TABLE test (id INT);"));
    }

    [Fact]
    public void ExtractDependencies_WithDependencies_ReturnsDependencies()
    {
        // Arrange
        var content = @"-- Depends: V001__CreateTable.sql, V002__AddColumn.sql
CREATE TABLE test (id INT);";

        // Act
        var dependencies = _parser.ExtractDependencies(content);

        // Assert
        Assert.Equal(2, dependencies.Length);
        Assert.Contains("V001__CreateTable.sql", dependencies);
        Assert.Contains("V002__AddColumn.sql", dependencies);
    }

    [Fact]
    public void ExtractDependencies_WithoutDependencies_ReturnsEmptyArray()
    {
        // Arrange
        var content = "CREATE TABLE test (id INT);";

        // Act
        var dependencies = _parser.ExtractDependencies(content);

        // Assert
        Assert.Empty(dependencies);
    }
}
