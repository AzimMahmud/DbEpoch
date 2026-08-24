# DbEpoch v2.1.0

A database-agnostic migration tool for .NET â€” PostgreSQL, SQL Server, MySQL, and SQLite from one CLI.

## Installation

**Linux / macOS** (Homebrew not required):

```bash
curl -fsSL https://github.com/AzimMahmud/DbEpoch/releases/download/v2.1.0/install.sh | bash
```

**Windows (PowerShell):**

```powershell
powershell -c "iwr -Uri https://github.com/AzimMahmud/DbEpoch/releases/download/v2.1.0/install.ps1 | iex"
```

**.NET global tool:**

```bash
dotnet tool install --global DbEpoch
```

Manual: download the archive for your platform below, extract, and put `DbEpoch` (`DbEpoch.exe`) on your PATH.

## Updating

Re-run the same install command above â€” it overwrites the existing binary in place and won't duplicate the PATH entry. Nothing else needs migrating between versions.

## Uninstalling

Both install scripts double as uninstallers â€” they remove the binary and the PATH entry they added.

```bash
# Linux / macOS
UNINSTALL=1 bash -c "$(curl -fsSL https://github.com/AzimMahmud/DbEpoch/releases/download/v2.1.0/install.sh)"

# Windows
iwr -Uri https://github.com/AzimMahmud/DbEpoch/releases/download/v2.1.0/install.ps1 -OutFile install.ps1
.\install.ps1 -Uninstall
```

## Downloads

| Platform | Package |
|----------|---------|
| Windows x64 | `DbEpoch-windows-x64.zip` |
| Windows ARM64 | `DbEpoch-windows-arm64.zip` |
| Linux x64 | `DbEpoch-linux-x64.tar.gz` |
| Linux ARM64 | `DbEpoch-linux-arm64.tar.gz` |
| Linux musl x64 (Alpine) | `DbEpoch-linux-musl-x64.tar.gz` |
| macOS x64 | `DbEpoch-macos-x64.tar.gz` |
| macOS ARM64 | `DbEpoch-macos-arm64.tar.gz` |
| NuGet | `DbEpoch.2.1.0.nupkg` |

Self-contained .NET 10 single-file binaries â€” no runtime install required.

> **macOS:** first run may be blocked by Gatekeeper â€” the install script now handles this automatically.

## What's New

### Added

- **Windows ARM64 build** â€” `DbEpoch-windows-arm64.zip` now included in releases.
- **Alpine Linux (musl) build** â€” `DbEpoch-linux-musl-x64.tar.gz` now included in releases for Alpine and musl-based distros.
- **macOS Gatekeeper auto-fix** â€” install script now automatically removes the quarantine attribute so `DbEpoch` runs immediately after install.
- **Windows Git Bash/WSL detection** â€” install script detects Windows environments and directs users to `install.ps1` instead of failing with "Unsupported OS".

### Fixed

- **Version mismatch** â€” `DbEpoch --version` now reads the version from assembly metadata instead of a hardcoded string, ensuring it always matches the installed version.

## Verify

Checksums are in `SHA256SUMS`.

```bash
curl -fsSL https://github.com/AzimMahmud/DbEpoch/releases/download/v2.1.0/SHA256SUMS | sha256sum -c -
```

## Changelog

See [CHANGELOG.md](https://github.com/AzimMahmud/DbEpoch/blob/v2.1.0/CHANGELOG.md) for the complete, itemized list.
