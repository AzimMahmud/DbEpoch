# DbEpoch v2.1.0

A database-agnostic migration tool for .NET — PostgreSQL, SQL Server, MySQL, and SQLite from one CLI.

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

Re-run the same install command above — it overwrites the existing binary in place and won't duplicate the PATH entry. Nothing else needs migrating between versions.

## Uninstalling

Both install scripts double as uninstallers — they remove the binary and the PATH entry they added.

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
| Linux x64 | `DbEpoch-linux-x64.tar.gz` |
| Linux ARM64 | `DbEpoch-linux-arm64.tar.gz` |
| macOS x64 | `DbEpoch-macos-x64.tar.gz` |
| macOS ARM64 | `DbEpoch-macos-arm64.tar.gz` |
| NuGet | `DbEpoch.2.1.0.nupkg` |

Self-contained .NET 10 single-file binaries — no runtime install required.

> **macOS:** first run may be blocked by Gatekeeper — the install script now handles this automatically.

## What's New

### Added

- **Windows ARM64 build** — `DbEpoch-windows-arm64.zip` now included in releases.
- **Alpine Linux (musl) build** — `DbEpoch-linux-musl-x64.tar.gz` now included in releases for Alpine and musl-based distros.
- **macOS Gatekeeper auto-fix** — install script now automatically removes the quarantine attribute so `DbEpoch` runs immediately after install.
- **Windows Git Bash/WSL detection** — install script detects Windows environments and directs users to `install.ps1` instead of failing with "Unsupported OS".
- **New logo assets** — dark/light variants at 64x64, 128x128, 256x256, and 512x512 sizes in dbepoch_logos/ folder. Updated root logo.png, docs favicon, and NuGet package icon.

### Fixed

- **Version mismatch** — `DbEpoch --version` now reads the version from assembly metadata instead of a hardcoded string, ensuring it always matches the installed version.
- **Mojibake in docs** — fixed broken UTF-8 characters (em dashes, middle dots, box-drawing chars) across all documentation files.

### Changed

- **Docs nav logo** — icon + gradient "DbEpoch" text, dark/light theme swap.
- **Browser favicon** — uses square icon instead of full logo.
- **NuGet package icon** — updated to 256px light variant with `<RepositoryType>git</RepositoryType>` for repo linking.

## Verify

Checksums are in `SHA256SUMS`.

```bash
curl -fsSL https://github.com/AzimMahmud/DbEpoch/releases/download/v2.1.0/SHA256SUMS | sha256sum -c -
```

## Changelog

See [CHANGELOG.md](https://github.com/AzimMahmud/DbEpoch/blob/v2.1.0/CHANGELOG.md) for the complete, itemized list.

## Assets

| File | Size | SHA256 |
|------|------|--------|
| DbEpoch-linux-arm64.tar.gz | 33.53 MB | fc62e2ea9b6b05eba58b92f543d2dddb1768639be247d14e4d6805a8ad24dc96 |
| DbEpoch-linux-x64.tar.gz | 35.20 MB | 3aa6471d31a5d6e69d54b165e61b8ca2dff7c8b68058b2185154a1bbafed81de |
| DbEpoch-macos-arm64.tar.gz | 33.76 MB | 948bbaab2260eef3319b39c49c7c8148d8ba09d45fea531175eef57b728d8ff2 |
| DbEpoch-macos-x64.tar.gz | 35.60 MB | aa05f85952f8daea95890481db978e2deebae3ff173358d5e3ff68c990f9c5d1 |
| DbEpoch-windows-x64.zip | 34.68 MB | 15423118fe012971731f17bc14d879ceb4a14cb96bad9e071cd2411a0b2a9999 |
| DbEpoch.2.1.0.nupkg | 38.42 MB | 9a54605eee5a5fab3180a84e82b4cf39d4fa87a279e18ac25c16538ac711f504 |
| install.ps1 | 0.01 MB | 24f3aadb745e6791ad01f933c97cd04df43880c394f01c69f03cb4273f7af9bb |
| install.sh | 0.01 MB | 1399d41eee133384cae58ff022d542ea22cd0f56f9d0be5ddd9d0520c0d2bd64 |
| SHA256SUMS | 0.00 MB | 5565dad07d9aba0df0e911d71b55bcd0fb5ea9a9c5f9cd2414a7bb751fadbfb2 |