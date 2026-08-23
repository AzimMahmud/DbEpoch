<div align="center">

<img src="https://raw.githubusercontent.com/AzimMahmud/dbshift/main/.github/assets/icon.svg" alt="DbShift" width="120">

# DbShift

**Database migrations that ship.**

A Flyway-style migration tool for **PostgreSQL**, **SQL Server**, **MySQL**, and **SQLite**.

[![CI](https://github.com/AzimMahmud/dbshift/actions/workflows/ci.yml/badge.svg)](https://github.com/AzimMahmud/dbshift/actions/workflows/ci.yml)
[![Release](https://img.shields.io/github/v/release/AzimMahmud/dbshift?include_prereleases&color=success)](https://github.com/AzimMahmud/dbshift/releases)
[![NuGet](https://img.shields.io/nuget/v/DbShift?logo=nuget&logoColor=white)](https://www.nuget.org/packages/DbShift)
[![.NET](https://img.shields.io/badge/.NET-10.0-512bd4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](https://github.com/AzimMahmud/dbshift/blob/main/LICENSE)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-brightgreen.svg)](https://github.com/AzimMahmud/dbshift/blob/main/CONTRIBUTING.md)

<br>

[Documentation](https://azimmahmud.github.io/dbshift/) · [Report Bug](https://github.com/AzimMahmud/dbshift/issues) · [Request Feature](https://github.com/AzimMahmud/dbshift/issues)

</div>

---

## What is DbShift?

DbShift is a SQL-first database migration tool for .NET. You write plain `.sql` files, and DbShift tracks, validates, and applies them across environments — with built-in safety features for production use.

**Why not Entity Framework migrations?** EF migrations are tightly coupled to your application code and hard to control in production. DbShift keeps migrations as standalone SQL files that live in your repository, work with any language or framework, and give you full control over what runs and when.

## Features

- **SQL-first** — Plain `.sql` files. No embedded DSL, no XML, no surprises.
- **Multi-database** — PostgreSQL, SQL Server, MySQL, and SQLite. Switch providers without changing your workflow.
- **Safe by design** — Distributed locks, approval gates, deployment windows, and audit trails.
- **CI-friendly** — Every command supports `--json` output and deterministic exit codes.
- **Works offline** — Validate, plan, and scaffold without a database connection.
- **Checksum integrity** — SHA-256 checksums detect when previously-applied scripts are edited in place.

## Quick Start

```bash
# Install (no .NET required)
curl -fsSL https://github.com/AzimMahmud/dbshift/releases/latest/download/install.sh | bash

# Scaffold a project
dbshift new --name MyApp --provider postgresql

# Create a migration
dbshift create --name CreateUsersTable --type schema

# Validate & preview
dbshift validate
dbshift plan

# Deploy
dbshift migrate -c "Host=localhost;Database=myapp;Username=postgres"
```

## Installation

No .NET SDK or runtime required — the binary is self-contained.

```bash
# Linux / macOS
curl -fsSL https://github.com/AzimMahmud/dbshift/releases/latest/download/install.sh | bash
```

```powershell
# Windows (PowerShell)
powershell -c "iwr -Uri https://github.com/AzimMahmud/dbshift/releases/latest/download/install.ps1 | iex"
```

```bash
# .NET global tool (requires .NET 10 SDK)
dotnet tool install --global DbShift
```

### Manual Download

| Platform | Download |
|----------|----------|
| Windows x64 | [`dbshift-windows-x64.zip`](https://github.com/AzimMahmud/dbshift/releases/latest/download/dbshift-windows-x64.zip) |
| Linux x64 | [`dbshift-linux-x64.tar.gz`](https://github.com/AzimMahmud/dbshift/releases/latest/download/dbshift-linux-x64.tar.gz) |
| Linux arm64 | [`dbshift-linux-arm64.tar.gz`](https://github.com/AzimMahmud/dbshift/releases/latest/download/dbshift-linux-arm64.tar.gz) |
| macOS x64 | [`dbshift-macos-x64.tar.gz`](https://github.com/AzimMahmud/dbshift/releases/latest/download/dbshift-macos-x64.tar.gz) |
| macOS arm64 | [`dbshift-macos-arm64.tar.gz`](https://github.com/AzimMahmud/dbshift/releases/latest/download/dbshift-macos-arm64.tar.gz) |

## Commands

| Command | Description | DB Required |
|---------|-------------|:-----------:|
| [`new`](https://azimmahmud.github.io/dbshift/commands/new) | Scaffold a complete project | No |
| [`create`](https://azimmahmud.github.io/dbshift/commands/create) | Create a migration script | No |
| [`validate`](https://azimmahmud.github.io/dbshift/commands/validate) | Check scripts for errors | No |
| [`plan`](https://azimmahmud.github.io/dbshift/commands/plan) | Preview execution plan | No |
| [`info`](https://azimmahmud.github.io/dbshift/commands/info) | Show configuration | No |
| [`init`](https://azimmahmud.github.io/dbshift/commands/init) | Create tracking tables | Yes |
| [`migrate`](https://azimmahmud.github.io/dbshift/commands/migrate) | Apply pending migrations | Yes |
| [`status`](https://azimmahmud.github.io/dbshift/commands/status) | Show migration status | Yes |
| [`rollback`](https://azimmahmud.github.io/dbshift/commands/rollback) | Undo migrations | Yes |
| [`repair`](https://azimmahmud.github.io/dbshift/commands/repair) | Fix failed migrations | Yes |
| [`history`](https://azimmahmud.github.io/dbshift/commands/history) | View audit trail | Yes |

Run `dbshift <command> --help` for command-specific options.

## Configuration

DbShift uses a two-tier JSON configuration:

```jsonc
// Database/Config/migration.json
{
  "migration": {
    "database": {
      "provider": "postgresql",
      "connectionString": "${DB_CONNECTION_STRING}"
    },
    "scripts": {
      "path": "./Database/Migrations"
    },
    "execution": {
      "batchSize": 10,
      "stopOnFailure": true
    }
  }
}
```

Per-environment overrides in `Database/Config/environments/<name>.json` with `${VAR}` expansion for secrets. See the [Configuration Guide](https://azimmahmud.github.io/dbshift/guide/configuration) for details.

## Supported Databases

| Provider | Version | Config Value |
|----------|---------|--------------|
| PostgreSQL | 12+ | `postgresql` |
| SQL Server | 2016+ | `sqlserver` |
| MySQL / MariaDB | 8+ / 10.5+ | `mysql` |
| SQLite | 3 | `sqlite` |

## Documentation

Full documentation is available at **[azimmahmud.github.io/dbshift](https://azimmahmud.github.io/dbshift/)**.

## Building from Source

```bash
git clone https://github.com/AzimMahmud/dbshift.git
cd dbshift
dotnet build DbShift.slnx
dotnet test DbShift.slnx --filter "Category!=Integration"
```

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

## Contributing

Contributions are welcome! Please see [CONTRIBUTING.md](https://github.com/AzimMahmud/dbshift/blob/main/CONTRIBUTING.md) for guidelines.

## License

[MIT](https://github.com/AzimMahmud/blob/main/LICENSE)
