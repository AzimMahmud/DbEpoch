# new

Scaffold a complete dbsh project structure. Aliases: `scaffold`, `init-project`.

**Database required:** No

## Usage

::: code-group

```bash [Interactive]
dbsh new
```

```bash [Non-interactive]
dbsh new --name MyApp --provider postgresql
```

```bash [With output directory]
dbsh new --name MyApp --provider sqlserver --output ./my-db-project
```

```bash [JSON mode for CI]
dbsh new --name MyApp --json
```

:::

When called without flags, `dbsh new` enters interactive mode:

1. Prompts for your **project name** (default: `MyApp`)
2. Lets you **select the database provider** from a list
3. Asks whether to use the **current directory** or a different one
4. Detects existing projects and asks about **overwriting files**

## Options

| Option | Short | Description |
|--------|-------|-------------|
| `--name` | `-n` | Project name (default: `MyApp`) |
| `--output` | `-o` | Output directory (default: current directory) |
| `--force` | `-f` | Overwrite existing files |

Also accepts [global options](/reference/global-options) like `--provider`, `--json`, etc.

## What it creates

```
Database/
  Config/
    migration.json                     # global config
    environments/
      local.json
      development.json
      staging.json
      production.json
  Migrations/
    Schema/
      .gitkeep
      V001__Example_Users.sql          # provider-specific example
    Data/
      .gitkeep
    Patch/
      .gitkeep
    Rollback/
      .gitkeep
      U001__Example_Users.sql          # example rollback
  Templates/
    schema_migration.sql
    data_migration.sql
    patch_migration.sql
    rollback_migration.sql
    repeatable_migration.sql
.github/workflows/
  database-migration.yml               # GitHub Actions CI pipeline
.gitignore
```

The example migration (`V001__Example_Users.sql`) is generated with correct SQL for your chosen provider — proper column types, defaults, and index syntax.

## Modular monoliths

When prompted, you can enter comma-separated module names. Each module gets its own subfolder under `Database/Migrations/` with schema-based isolation (PostgreSQL/SQL Server) or table-prefix naming (MySQL/SQLite).

## After scaffolding

1. Set your connection string (env var or edit `migration.json`)
2. Run `dbsh create --name YourMigration --type schema` to add scripts
3. Write your SQL
4. Run `dbsh migrate`
