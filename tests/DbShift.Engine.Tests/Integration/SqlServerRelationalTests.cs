namespace DbShift.Engine.Tests.Integration;

public sealed class SqlServerRelationalTests : RelationalProviderContractTests<SqlServerFixture>
{
    public SqlServerRelationalTests(SqlServerFixture fixture) : base(fixture)
    {
    }
}
