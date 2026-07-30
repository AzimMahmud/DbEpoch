namespace DbShift.CLI.Helpers;

public static class ProviderSqlHelper
{
    public static string IdType(string provider) => provider.ToLowerInvariant() switch
    {
        "sqlserver" => "UNIQUEIDENTIFIER",
        "mysql" => "CHAR(36)",
        "sqlite" => "TEXT",
        _ => "UUID"
    };

    public static string DefaultId(string provider) => provider.ToLowerInvariant() switch
    {
        "sqlserver" => "DEFAULT NEWID()",
        "mysql" => "",
        "sqlite" => "DEFAULT (lower(hex(randomblob(16))))",
        _ => "DEFAULT gen_random_uuid()"
    };

    public static string BoolType(string provider) => provider.ToLowerInvariant() switch
    {
        "sqlserver" => "BIT",
        "mysql" => "TINYINT(1)",
        "sqlite" => "INTEGER",
        _ => "BOOLEAN"
    };

    public static string BoolTrue(string provider) => provider.ToLowerInvariant() switch
    {
        "sqlserver" => "1",
        "mysql" => "1",
        "sqlite" => "1",
        _ => "TRUE"
    };

    public static string TimestampType(string provider) => provider.ToLowerInvariant() switch
    {
        "sqlserver" => "DATETIME2",
        "mysql" => "DATETIME",
        "sqlite" => "TEXT",
        _ => "TIMESTAMPTZ"
    };

    public static string DefaultNow(string provider) => provider.ToLowerInvariant() switch
    {
        "sqlserver" => "DEFAULT GETUTCDATE()",
        "mysql" => "DEFAULT UTC_TIMESTAMP",
        "sqlite" => "",
        _ => "DEFAULT NOW()"
    };

    public static string UniqueIndex(string provider) => provider.ToLowerInvariant() switch
    {
        "sqlserver" => "UNIQUE NONCLUSTERED INDEX",
        _ => "UNIQUE INDEX"
    };
}
