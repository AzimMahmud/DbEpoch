using dbsh.Core.Exceptions;

namespace dbsh.Infrastructure.Database.Providers;

/// <summary>
/// Resolves the correct <see cref="IDatabaseProvider"/> from a config string.
/// Accepted aliases are lenient: "postgres", "npgsql", "pgsql" all map to PostgreSQL.
/// </summary>
public static class DatabaseProviderFactory
{
    public static IDatabaseProvider Create(string? providerName)
    {
        var key = (providerName ?? string.Empty).Trim().ToLowerInvariant();

        return key switch
        {
            "postgresql" or "postgres" or "npgsql" or "pgsql" => new PostgreSqlProvider(),
            "sqlserver" or "mssql" or "sql-server" or "sql server" => new SqlServerProvider(),
            "mysql" or "mariadb" or "maria" => new MySqlProvider(),
            "sqlite" => new SqliteProvider(),
            "cockroachdb" or "crdb" => new PostgreSqlProvider(),
            "yugabyte" or "yugabytedb" => new PostgreSqlProvider(),
            "aurora" or "aurora-postgresql" => new PostgreSqlProvider(),
            "aurora-mysql" => new MySqlProvider(),
            "oracle" or "oracledb" or "odp.net" => new OracleProvider(),
            "" => new PostgreSqlProvider(),
            _ => throw new UnsupportedProviderException(
                $"Unknown database provider '{providerName}'. " +
                "Supported providers: postgresql, sqlserver, mysql, sqlite, cockroachdb, yugabyte, aurora, oracle.")
        };
    }
}
