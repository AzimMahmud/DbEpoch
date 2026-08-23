# Global Options

These options are available on every `dbshift` command.

## Options

| Option | Short | Description |
|--------|-------|-------------|
| `--environment <NAME>` | `-e` | Target environment (default: `local`) |
| `--provider <NAME>` | `-p` | Database provider: `postgresql`, `sqlserver`, `mysql`, `sqlite` |
| `--connection-string <CONN>` | `-c` | Override connection string |
| `--base-path <PATH>` | `-C` | Path to the repository root (config base) |
| `--in-memory` | `-i` | Force offline in-memory mode (no database) |
| `--yes` | `-y` | Skip interactive confirmation prompts |
| `--no-color` | | Disable colored output |
| `--json` | `-j` | Emit machine-readable JSON |
| `--verbose` | `-v` | Show detailed informational output |
| `--module <NAME>` | `-m` | Module name (subfolder under Database/Migrations/) |
| `--help` | `-h` | Show help |
| `--version` | | Show version |

## Examples

```bash
# Use a specific environment
dbshift migrate --environment production

# Override the provider
dbshift migrate -p sqlserver -c "Server=...;Database=...;..."

# Offline mode
dbshift plan --in-memory

# CI mode (no prompts, JSON output)
dbshift migrate --environment development --yes --json

# From a different repository root
dbshift validate --base-path /path/to/other/repo
```

## Environment variable expansion

All `${VAR}` tokens in configuration files are expanded from environment variables. This works with any environment variable, not just `DB_CONNECTION_STRING`.

```json
{
  "database": {
    "connectionString": "${MY_APP_DATABASE_URL}"
  }
}
```
