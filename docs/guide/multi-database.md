# Multi-Database Setup

DbShift supports four databases. The only thing that changes is the connection string and the `provider` setting.

## Supported providers

| Provider | Version | Config value | Driver |
|----------|---------|-------------|--------|
| PostgreSQL | 12+ | `postgresql` | Npgsql 8 |
| SQL Server | 2016+ | `sqlserver` | Microsoft.Data.SqlClient 5 |
| MySQL / MariaDB | 8+ / 10.5+ | `mysql` | MySqlConnector 2 |
| SQLite | 3 | `sqlite` | Microsoft.Data.Sqlite 8 |

Switch providers by changing the `provider` field in `migration.json` or using `--provider` on the CLI.

## PostgreSQL

```bash
dbshift migrate -c "Host=localhost;Port=5432;Database=myapp;Username=postgres;Password=secret"
```

```json
{ "database": { "provider": "postgresql", "connectionString": "..." } }
```

PostgreSQL uses database schemas for module isolation: `"module".table_name`.

## SQL Server

```bash
dbshift migrate -c "Server=localhost;Database=myapp;User Id=sa;Password=secret;TrustServerCertificate=True"
```

```json
{ "database": { "provider": "sqlserver", "connectionString": "..." } }
```

SQL Server uses database schemas for module isolation: `[module].[table_name]`.

## MySQL

```bash
dbshift migrate -c "Server=localhost;Database=myapp;User=root;Password=secret"
```

```json
{ "database": { "provider": "mysql", "connectionString": "..." } }
```

MySQL does not support schemas. Module isolation uses table-name prefixes: `module__table_name`.

## SQLite

```bash
dbshift migrate -c "Data Source=./myapp.db"
```

```json
{ "database": { "provider": "sqlite", "connectionString": "..." } }
```

SQLite does not support schemas. Module isolation uses table-name prefixes: `module__table_name`.

## Override provider from CLI

Override the provider at runtime without changing config:

```bash
dbshift migrate -c "..." -p sqlserver
```

## Provider-specific SQL notes

When writing migrations, keep these differences in mind:

| Concern | PostgreSQL | SQL Server | MySQL | SQLite |
|---------|-----------|------------|-------|--------|
| Bool type | `BOOLEAN` | `BIT` | `TINYINT(1)` | `INTEGER` (0/1) |
| UUID type | `UUID` | `UNIQUEIDENTIFIER` | `CHAR(36)` | `TEXT` |
| Timestamp | `NOW()` | `GETUTCDATE()` | `UTC_TIMESTAMP()` | C# DateTime (ISO string) |
| ID generation | `gen_random_uuid()` | `NEWID()` | C# Guid | C# Guid |
| JSON storage | `TEXT` | `NVARCHAR(MAX)` | `LONGTEXT` | `TEXT` |
| String concat | `\|\|` or `CONCAT()` | `+` | `CONCAT()` | `\|\|` |
| Limit/offset | `LIMIT n OFFSET m` | `OFFSET m ROWS FETCH NEXT n ROWS ONLY` | `LIMIT n OFFSET m` | `LIMIT n OFFSET m` |
| ILIKE | `ILIKE` | use `LOWER()` | use `LOWER()` | use `LOWER()` |
| IF NOT EXISTS | `CREATE TABLE IF NOT EXISTS` | `IF NOT EXISTS (SELECT ...) CREATE TABLE ...` | `CREATE TABLE IF NOT EXISTS` | `CREATE TABLE IF NOT EXISTS` |
