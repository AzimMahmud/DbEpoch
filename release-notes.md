# dbsh v2.1.4

## Installation

```bash
# Linux / macOS
curl -fsSL https://github.com/AzimMahmud/dbsh/releases/download/v2.1.4/install.sh | bash

# Windows
powershell -c "iwr -Uri https://github.com/AzimMahmud/dbsh/releases/download/v2.1.4/install.ps1 | iex"
```

The install scripts automatically verify the downloaded archive against `SHA256SUMS` published alongside this release.

> **.NET global tool** — install with `dotnet tool install --global dbsh` once the package is published to NuGet.

> **macOS users:** the first time you run `dbsh`, Gatekeeper may quarantine the binary. The install script removes the quarantine attribute automatically.

## Downloads

| Platform | Package |
|----------|--------|
| Windows x64 | `dbsh-windows-x64.zip` |
| Windows ARM64 | `dbsh-windows-arm64.zip` |
| Linux x64 | `dbsh-linux-x64.tar.gz` |
| Linux ARM64 | `dbsh-linux-arm64.tar.gz` |
| Linux musl x64 (Alpine) | `dbsh-linux-musl-x64.tar.gz` |
| macOS x64 | `dbsh-macos-x64.tar.gz` |
| macOS ARM64 | `dbsh-macos-arm64.tar.gz` |

## Verifying integrity

Every archive is checksummed in `SHA256SUMS` (attached to this release). The install scripts verify it automatically; to verify manually:

```bash
curl -fsSL https://github.com/AzimMahmud/dbsh/releases/download/v2.1.4/SHA256SUMS | sha256sum -c -
```

## Changelog

### Documentation

- Updated README to list all 8 supported databases (added Oracle, CockroachDB, YugabyteDB, Aurora)
- Updated architecture docs to include OracleProvider in the Infrastructure layer
- Updated tracking tables DDL reference with Oracle column types (RAW(16), NUMBER(1), TIMESTAMP, SYS_GUID())
- Updated VitePress site description and nav version badge
- Updated `--provider` CLI help text to list all supported providers
- Updated `dbsh init` and `dbsh new` docs to mention Oracle for schema-based module isolation

See [CHANGELOG.md](https://github.com/AzimMahmud/dbsh/blob/v2.1.4/CHANGELOG.md) for details.
