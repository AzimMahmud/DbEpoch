#!/usr/bin/env pwsh
# DbShift – official Windows install script
# Usage:
#   powershell -c "iwr -Uri https://github.com/AzimMahmud/dbshift/releases/latest/download/install.ps1 | iex"
#   pwsh -c "iwr -Uri https://github.com/AzimMahmud/dbshift/releases/latest/download/install.ps1 | iex"
#
# Uninstall (download first — a switch like -Uninstall can't be passed through `iwr | iex`):
#   iwr -Uri https://github.com/AzimMahmud/dbshift/releases/latest/download/install.ps1 -OutFile install.ps1
#   .\install.ps1 -Uninstall

param(
    [string]$Repo = "AzimMahmud/dbshift",
    [string]$Version = "latest",
    [string]$InstallDir = "$env:LOCALAPPDATA\DbShift",
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

function Info($Message) { Write-Host "  > $Message" -ForegroundColor Cyan }
function Ok($Message)   { Write-Host "  ✓ $Message" -ForegroundColor Green }
function Warn($Message) { Write-Host "  ⚠ $Message" -ForegroundColor Yellow }
function Err($Message)  { Write-Host "  ✗ $Message" -ForegroundColor Red; exit 1 }

# ── uninstall ────────────────────────────────────────────────────────────
if ($Uninstall) {
    Write-Host ""
    Write-Host "  ╭──────────────────────────────────────╮" -ForegroundColor Cyan
    Write-Host "  │  DbShift — database migration tool   │" -ForegroundColor Cyan
    Write-Host "  ╰──────────────────────────────────────╯" -ForegroundColor Cyan
    Write-Host ""

    if (Test-Path $InstallDir) {
        Remove-Item -LiteralPath $InstallDir -Recurse -Force
        Ok "Removed $InstallDir"
    } else {
        Warn "No DbShift installation found at $InstallDir"
    }

    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($currentPath -and ($currentPath -split ';' -contains $InstallDir)) {
        $newPath = (($currentPath -split ';' | Where-Object { $_ -and $_ -ne $InstallDir }) -join ';')
        [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
        Ok "Removed $InstallDir from user PATH"
    }

    Write-Host ""
    Info "DbShift removed. Restart your shell to fully clear it from PATH."
    Write-Host ""
    exit 0
}

# ── architecture detection ────────────────────────────────────────────────
$arch = switch ([Environment]::ProcessorArchitecture) {
    "X64"   { "x64" }
    "Arm64" { "arm64" }
    default { Err "Unsupported architecture: $([Environment]::ProcessorArchitecture)" }
}
$platform = "windows-$arch"

# ── download URL ──────────────────────────────────────────────────────────
# Strip an optional leading "v" so "v1.0.0" and "1.0.0" both work.
$Version = $Version.TrimStart("v", "V")
if ($Version -eq "latest") {
    $base = "https://github.com/$Repo/releases/latest/download"
} else {
    $base = "https://github.com/$Repo/releases/download/v$Version"
}
$url = "$base/dbshift-$platform.zip"
$sumsUrl = "$base/SHA256SUMS"

# ── download ──────────────────────────────────────────────────────────────
Write-Host ""
Write-Host "  ╭──────────────────────────────────────╮" -ForegroundColor Cyan
Write-Host "  │  DbShift — database migration tool   │" -ForegroundColor Cyan
Write-Host "  ╰──────────────────────────────────────╯" -ForegroundColor Cyan
Write-Host ""

Info "Detected: $platform"
Info "Downloading dbshift for $platform..."

$zip = "$env:TEMP\dbshift.zip"
try {
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
} catch {
    Err "Download failed: $url`n  $_"
}
Ok "Downloaded"

# ── integrity check ──────────────────────────────────────────────────────
$assetName = "dbshift-$platform.zip"
$expectedHash = $null
try {
    $sums = Invoke-WebRequest -Uri $sumsUrl -UseBasicParsing
    foreach ($line in ($sums.Content -split "`n")) {
        if ($line -match "^\s*([0-9a-fA-F]{64})\s+\*?$([regex]::Escape($assetName))\s*$") {
            $expectedHash = $matches[1].ToLowerInvariant()
            break
        }
    }
} catch {
    # SHA256SUMS not published for this release; warn and continue.
}
if ($expectedHash) {
    $actualHash = (Get-FileHash -Path $zip -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($actualHash -ne $expectedHash) {
        Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
        Err "Checksum mismatch for $assetName`n  expected: $expectedHash`n  actual:   $actualHash"
    }
    Ok "Checksum verified"
} else {
    Warn "SHA256SUMS not found at $sumsUrl; skipping integrity verification."
}

# ── extract ───────────────────────────────────────────────────────────────
Info "Extracting..."
if (Test-Path $InstallDir) {
    Remove-Item -LiteralPath $InstallDir -Recurse -Force -ErrorAction SilentlyContinue
}
$null = New-Item -ItemType Directory -Path $InstallDir -Force
try {
    Expand-Archive -Path $zip -DestinationPath $InstallDir -Force
} catch {
    Err "Extraction failed: $_"
}
Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
Ok "Extracted to $InstallDir"

# ── add to PATH ───────────────────────────────────────────────────────────
$currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
if ($currentPath -notlike "*$InstallDir*") {
    $newPath = "$InstallDir;$currentPath"
    [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
    # Also update current session
    $env:Path = "$InstallDir;$env:Path"
    Ok "Added to user PATH: $InstallDir"
} else {
    Info "$InstallDir already on PATH"
}

# ── verify ────────────────────────────────────────────────────────────────
$exe = Join-Path $InstallDir "dbshift.exe"
if (Test-Path $exe) {
    $version = & $exe --version 2>&1 | Select-Object -First 1
    Ok $version
} else {
    Warn "Binary not found at $exe"
}

Write-Host ""
Info "Run 'dbshift --help' to get started."
Write-Host ""
