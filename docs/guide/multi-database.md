# Multi-Database Setup

dbsh supports eight databases. The only thing that changes is the connection string and the `provider` setting.

## Supported providers

| Provider | Version | Config value | Driver |
|----------|---------|-------------|--------|
| PostgreSQL | 12+ | `postgresql` | Npgsql 8 |
| SQL Server | 2016+ | `sqlserver` | Microsoft.Data.SqlClient 5 |
| MySQL / MariaDB | 8+ / 10.5+ | `mysql` | MySqlConnector 2 |
| SQLite | 3 | `sqlite` | Microsoft.Data.Sqlite 8 |
| Oracle | 19+ | `oracle` | Oracle.ManagedDataAccess.Core 23 |
| CockroachDB | 22+ | `cockroachdb` | Npgsql (wire-compatible) |
| YugabyteDB | 2.14+ | `yugabyte` | Npgsql (wire-compatible) |
| Amazon Aurora | — | `aurora` | Npgsql / MySqlConnector (wire-compatible) |

Switch providers by changing the `provider` field in `migration.json` or using `--provider` on the CLI.

## PostgreSQL

```bash
dbsh migrate -c "Host=localhost;Port=5432;Database=myapp;Username=postgres;Password=secret"
```

```json
{ "database": { "provider": "postgresql", "connectionString": "..." } }
```

PostgreSQL uses database schemas for module isolation: `"module".table_name`.

## SQL Server

```bash
dbsh migrate -c "Server=localhost;Database=myapp;User Id=sa;Password=secret;TrustServerCertificate=True"
```

```json
{ "database": { "provider": "sqlserver", "connectionString": "..." } }
```

SQL Server uses database schemas for module isolation: `[module].[table_name]`.

## MySQL

```bash
dbsh migrate -c "Server=localhost;Database=myapp;User=root;Password=secret"
```

```json
{ "database": { "provider": "mysql", "connectionString": "..." } }
```

MySQL does not support schemas. Module isolation uses table-name prefixes: `module__table_name`.

## SQLite

```bash
dbsh migrate -c "Data Source=./myapp.db"
```

```json
{ "database": { "provider": "sqlite", "connectionString": "..." } }
```

SQLite does not support schemas. Module isolation uses table-name prefixes: `module__table_name`.

## Oracle

```bash
dbsh migrate -c "Data Source=localhost:1521/ORCL;User Id=myuser;Password=secret"
```

```json
{ "database": { "provider": "oracle", "connectionString": "..." } }
```

Oracle uses database schemas (user accounts) for module isolation: `module.table_name`.

## CockroachDB

CockroachDB is wire-compatible with PostgreSQL. Use the `cockroachdb` or `crdb` provider alias.

```bash
dbsh migrate -p cockroachdb -c "Host=localhost;Port=26257;Database=myapp;Username=root;Password=secret;SslMode=Require"
```

```json
{ "database": { "provider": "cockroachdb", "connectionString": "..." } }
```

Uses the same SQL syntax and schema support as PostgreSQL.

## YugabyteDB

YugabyteDB is wire-compatible with PostgreSQL. Use the `yugabyte` or `yugabytedb` provider alias.

```bash
dbsh migrate -p yugabyte -c "Host=localhost;Port=5433;Database=myapp;Username=yugabyte;Password=secret"
```

```json
{ "database": { "provider": "yugabyte", "connectionString": "..." } }
```

Uses the same SQL syntax and schema support as PostgreSQL.

## Amazon Aurora

Amazon Aurora is wire-compatible with PostgreSQL and MySQL. Use the `aurora` provider alias, or `aurora-postgresql` / `aurora-mysql` to force a specific engine.

```bash
# Aurora PostgreSQL
dbsh migrate -p aurora -c "Host=my-cluster.cluster-xxxx.us-east-1.rds.amazonaws.com;Port=5432;Database=myapp;Username=postgres;Password=secret"

# Aurora MySQL
dbsh migrate -p aurora-mysql -c "Host=my-cluster.cluster-xxxx.us-east-1.rds.amazonaws.com;Port=3306;Database=myapp;Username=admin;Password=secret"
```

```json
{ "database": { "provider": "aurora", "connectionString": "..." } }
```

Uses the same SQL syntax and schema support as the underlying engine (PostgreSQL or MySQL).

## Override provider from CLI

Override the provider at runtime without changing config:

```bash
dbsh migrate -c "..." -p sqlserver
```

## Provider-specific SQL notes

When writing migrations, keep these differences in mind:

| Concern | PostgreSQL | SQL Server | MySQL | SQLite | Oracle |
|---------|-----------|------------|-------|--------|--------|
| Bool type | `BOOLEAN` | `BIT` | `TINYINT(1)` | `INTEGER` (0/1) | `NUMBER(1)` (0/1) |
| UUID type | `UUID` | `UNIQUEIDENTIFIER` | `CHAR(36)` | `TEXT` | `RAW(16)` / `SYS_GUID()` |
| Timestamp | `NOW()` | `GETUTCDATE()` | `UTC_TIMESTAMP()` | C# DateTime (ISO string) | `SYSTIMESTAMP` |
| ID generation | `gen_random_uuid()` | `NEWID()` | C# Guid | C# Guid | `SYS_GUID()` |
| JSON storage | `TEXT` | `NVARCHAR(MAX)` | `LONGTEXT` | `TEXT` | `CLOB` |
| String concat | `\|\|` or `CONCAT()` | `+` | `CONCAT()` | `\|\|` | `\|\|` or `CONCAT()` |
| Limit/offset | `LIMIT n OFFSET m` | `OFFSET m ROWS FETCH NEXT n ROWS ONLY` | `LIMIT n OFFSET m` | `LIMIT n OFFSET m` | `FETCH FIRST n ROWS ONLY` |
| ILIKE | `ILIKE` | use `LOWER()` | use `LOWER()` | use `LOWER()` | use `LOWER()` |
| IF NOT EXISTS | `CREATE TABLE IF NOT EXISTS` | `IF NOT EXISTS (SELECT ...) CREATE TABLE ...` | `CREATE TABLE IF NOT EXISTS` | `CREATE TABLE IF NOT EXISTS` | PL/SQL `DECLARE` block |
