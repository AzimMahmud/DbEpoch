# DbShift v2.0.1

A database-agnostic migration tool for .NET — PostgreSQL, SQL Server, MySQL, and SQLite from one CLI.

## Installation

**Linux / macOS** (Homebrew not required):

```bash
curl -fsSL https://github.com/AzimMahmud/dbshift/releases/download/v2.0.1/install.sh | bash
```

**Windows (PowerShell):**

```powershell
powershell -c "iwr -Uri https://github.com/AzimMahmud/dbshift/releases/download/v2.0.1/install.ps1 | iex"
```

**.NET global tool:**

```bash
dotnet tool install --global DbShift
```

Manual: download the archive for your platform below, extract, and put `dbshift` (`dbshift.exe`) on your PATH.

## Updating

Re-run the same install command above — it overwrites the existing binary in place and won't duplicate the PATH entry. Nothing else needs migrating between versions.

## Uninstalling

Both install scripts double as uninstallers — they remove the binary and the PATH entry they added.

```bash
# Linux / macOS
UNINSTALL=1 bash -c "$(curl -fsSL https://github.com/AzimMahmud/dbshift/releases/download/v2.0.1/install.sh)"

# Windows
iwr -Uri https://github.com/AzimMahmud/dbshift/releases/download/v2.0.1/install.ps1 -OutFile install.ps1
.\install.ps1 -Uninstall
```

## Downloads

| Platform | Package |
|----------|---------|
| Windows x64 | `dbshift-windows-x64.zip` |
| Linux x64 | `dbshift-linux-x64.tar.gz` |
| Linux arm64 | `dbshift-linux-arm64.tar.gz` |
| macOS x64 | `dbshift-macos-x64.tar.gz` |
| macOS arm64 | `dbshift-macos-arm64.tar.gz` |
| NuGet | `DbShift.2.0.1.nupkg` |

Self-contained .NET 10 single-file binaries — no runtime install required.

> **macOS:** first run may be blocked by Gatekeeper — run `xattr -d com.apple.quarantine dbshift` (or `sudo xattr -rd com.apple.quarantine /usr/local/bin/dbshift`).

## What's New

### Documentation

- **VitePress documentation site** — full documentation deployed to GitHub Pages covering installation, all commands, configuration, multi-database setup, script conventions, tracking tables, architecture, and CI/CD integration.
- **Custom light/dark theme** — branded VitePress theme with teal-blue gradient palette, styled nav, feature cards, and sidebar.
- **GitHub Pages deployment workflow** — auto-deploys docs on push to `main`.
- **README rewritten** — condensed from 608 to 150 lines with a professional structure: centered header, feature highlights, command table with docs links, and clear installation instructions.

### Design

- **Nav improved** — icon-only logo in nav bar, styled hover/active states, border divider.
- **Favicon fixed** — now uses the square icon SVG, displaying correctly in browser tabs.

## Verify

Checksums are in `SHA256SUMS`.

```bash
curl -fsSL https://github.com/AzimMahmud/dbshift/releases/download/v2.0.1/SHA256SUMS | sha256sum -c -
```

## Changelog

See [CHANGELOG.md](https://github.com/AzimMahmud/dbshift/blob/v2.0.1/CHANGELOG.md) for the complete, itemized list.
