# dbsh v2.1.2

## Installation

```bash
# Linux / macOS
curl -fsSL https://github.com/AzimMahmud/dbsh/releases/download/v2.1.2/install.sh | bash

# Windows
powershell -c "iwr -Uri https://github.com/AzimMahmud/dbsh/releases/download/v2.1.2/install.ps1 | iex"
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
curl -fsSL https://github.com/AzimMahmud/dbsh/releases/download/v2.1.2/SHA256SUMS | sha256sum -c -
```

## Changelog

- Fixed Unicode encoding issues in install scripts (box-drawing characters, em dashes, status icons)
- Updated release packaging with proper UTF-8 encoding for cross-platform compatibility
- Improved install script display for better terminal rendering

See [CHANGELOG.md](https://github.com/AzimMahmud/dbsh/blob/v2.1.2/CHANGELOG.md) for details.
