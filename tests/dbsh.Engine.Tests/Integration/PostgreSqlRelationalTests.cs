namespace dbsh.Engine.Tests.Integration;

public sealed class PostgreSqlRelationalTests : RelationalProviderContractTests<PostgreSqlFixture>
{
    public PostgreSqlRelationalTests(PostgreSqlFixture fixture) : base(fixture)
    {
    }
}
