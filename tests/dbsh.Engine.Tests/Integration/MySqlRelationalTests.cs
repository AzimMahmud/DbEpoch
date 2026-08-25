namespace dbsh.Engine.Tests.Integration;

public sealed class MySqlRelationalTests : RelationalProviderContractTests<MySqlFixture>
{
    public MySqlRelationalTests(MySqlFixture fixture) : base(fixture)
    {
    }
}
