# Installation

No .NET SDK or runtime required for the binary install methods.

## One-liner (recommended)

::: code-group

```bash [Linux / macOS]
curl -fsSL https://github.com/AzimMahmud/dbsh/releases/latest/download/install.sh | bash
```

```powershell [Windows (PowerShell)]
powershell -c "iwr -Uri https://github.com/AzimMahmud/dbsh/releases/latest/download/install.ps1 | iex"
```

:::

These scripts detect your OS and architecture, download the correct pre-built binary, and add it to your `PATH` automatically.

## Manual download

| Platform | Download |
|----------|----------|
| Windows x64 | [`dbsh-windows-x64.zip`](https://github.com/AzimMahmud/dbsh/releases/latest/download/dbsh-windows-x64.zip) (~38 MB) |
| Windows ARM64 | [`dbsh-windows-arm64.zip`](https://github.com/AzimMahmud/dbsh/releases/latest/download/dbsh-windows-arm64.zip) (~37 MB) |
| Linux x64 | [`dbsh-linux-x64.tar.gz`](https://github.com/AzimMahmud/dbsh/releases/latest/download/dbsh-linux-x64.tar.gz) (~38 MB) |
| Linux ARM64 | [`dbsh-linux-arm64.tar.gz`](https://github.com/AzimMahmud/dbsh/releases/latest/download/dbsh-linux-arm64.tar.gz) (~36 MB) |
| Linux musl x64 (Alpine) | [`dbsh-linux-musl-x64.tar.gz`](https://github.com/AzimMahmud/dbsh/releases/latest/download/dbsh-linux-musl-x64.tar.gz) (~38 MB) |
| macOS x64 | [`dbsh-macos-x64.tar.gz`](https://github.com/AzimMahmud/dbsh/releases/latest/download/dbsh-macos-x64.tar.gz) (~38 MB) |
| macOS ARM64 | [`dbsh-macos-arm64.tar.gz`](https://github.com/AzimMahmud/dbsh/releases/latest/download/dbsh-macos-arm64.tar.gz) (~36 MB) |

Extract and place `dbsh` (or `dbsh.exe` on Windows) anywhere on your `PATH`.

## .NET global tool

Requires the [.NET 10 SDK/runtime](https://dotnet.microsoft.com/download/dotnet/10.0).

```bash
dotnet tool install --global dbsh
dbsh --version
```

::: tip
The self-contained binary (one-liner / manual download) does not require .NET. The `dotnet tool` method is convenient if you already have the SDK installed.
:::

## Build from source

```bash
git clone https://github.com/AzimMahmud/dbsh.git
cd dbsh
.\publish.ps1          # Windows -> dist\dbsh.exe
./publish.sh           # Linux/macOS -> dist/dbsh
```

## Verify

```bash
dbsh --version
# -> dbsh v2.1.4
# -> database migrations for .NET
```

## Finding the binary

::: code-group

```bash [Linux / macOS]
which -a dbsh
```

```powershell [Windows]
where.exe dbsh
```

:::

`which -a` lists every match on PATH, not just the first. This matters because it's possible to have multiple copies on PATH (e.g. one from the installer, another installed manually) — only the first one in PATH order actually runs.

## Updating

Re-run the same one-liner you used to install. It overwrites the existing binary in place and won't add a duplicate PATH entry.

```bash
# Linux / macOS
curl -fsSL https://github.com/AzimMahmud/dbsh/releases/latest/download/install.sh | bash

# Windows (PowerShell)
powershell -c "iwr -Uri https://github.com/AzimMahmud/dbsh/releases/latest/download/install.ps1 | iex"
```

## Uninstalling

Both install scripts double as uninstallers.

::: code-group

```bash [Linux / macOS]
UNINSTALL=1 bash -c "$(curl -fsSL https://github.com/AzimMahmud/dbsh/releases/latest/download/install.sh)"
```

```powershell [Windows]
iwr -Uri https://github.com/AzimMahmud/dbsh/releases/latest/download/install.ps1 -OutFile install.ps1
.\install.ps1 -Uninstall
```

:::

If you installed via `dotnet tool install --global dbsh`, use `dotnet tool uninstall --global dbsh` instead.
