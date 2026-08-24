#!/usr/bin/env pwsh
# DbEpoch â€“ official Windows install script
# Usage:
#   powershell -c "iwr -Uri https://github.com/AzimMahmud/DbEpoch/releases/latest/download/install.ps1 | iex"
#   pwsh -c "iwr -Uri https://github.com/AzimMahmud/DbEpoch/releases/latest/download/install.ps1 | iex"
#
# Uninstall (download first â€” a switch like -Uninstall can't be passed through `iwr | iex`):
#   iwr -Uri https://github.com/AzimMahmud/DbEpoch/releases/latest/download/install.ps1 -OutFile install.ps1
#   .\install.ps1 -Uninstall

param(
    [string]$Repo = "AzimMahmud/DbEpoch",
    [string]$Version = "latest",
    [string]$InstallDir = "$env:LOCALAPPDATA\DbEpoch",
    [switch]$Uninstall
)

$ErrorActionPreference = "Stop"

function Info($Message) { Write-Host "  > $Message" -ForegroundColor Cyan }
function Ok($Message)   { Write-Host "  âœ“ $Message" -ForegroundColor Green }
function Warn($Message) { Write-Host "  âš  $Message" -ForegroundColor Yellow }
function Err($Message)  { Write-Host "  âœ— $Message" -ForegroundColor Red; exit 1 }

# â”€â”€ uninstall â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
if ($Uninstall) {
    Write-Host ""
    Write-Host "  â•­â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â•®" -ForegroundColor Cyan
    Write-Host "  â”‚  DbEpoch â€” database migration tool   â”‚" -ForegroundColor Cyan
    Write-Host "  â•°â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â•¯" -ForegroundColor Cyan
    Write-Host ""

    if (Test-Path $InstallDir) {
        try {
            Remove-Item -LiteralPath $InstallDir -Recurse -Force
            Ok "Removed $InstallDir"
        } catch {
            Err "Could not remove $InstallDir (access denied). It's likely a system-wide install; try re-running from an elevated (Administrator) PowerShell.`n  $_"
        }
    } else {
        Warn "No DbEpoch installation found at $InstallDir"
    }

    $currentPath = [Environment]::GetEnvironmentVariable("Path", "User")
    if ($currentPath -and ($currentPath -split ';' -contains $InstallDir)) {
        $newPath = (($currentPath -split ';' | Where-Object { $_ -and $_ -ne $InstallDir }) -join ';')
        [Environment]::SetEnvironmentVariable("Path", $newPath, "User")
        Ok "Removed $InstallDir from user PATH"
    }

    Write-Host ""
    Info "DbEpoch removed. Restart your shell to fully clear it from PATH."
    Write-Host ""
    exit 0
}

# â”€â”€ architecture detection â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
$arch = switch ([Environment]::ProcessorArchitecture) {
    "X64"   { "x64" }
    "Arm64" { "arm64" }
    default { Err "Unsupported architecture: $([Environment]::ProcessorArchitecture)" }
}
$platform = "windows-$arch"

# â”€â”€ download URL â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
# Strip an optional leading "v" so "v1.0.0" and "1.0.0" both work.
$Version = $Version.TrimStart("v", "V")
if ($Version -eq "latest") {
    $base = "https://github.com/$Repo/releases/latest/download"
} else {
    $base = "https://github.com/$Repo/releases/download/v$Version"
}
$url = "$base/DbEpoch-$platform.zip"
$sumsUrl = "$base/SHA256SUMS"

# â”€â”€ download â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
Write-Host ""
Write-Host "  â•­â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â•®" -ForegroundColor Cyan
Write-Host "  â”‚  DbEpoch â€” database migration tool   â”‚" -ForegroundColor Cyan
Write-Host "  â•°â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â•¯" -ForegroundColor Cyan
Write-Host ""

Info "Detected: $platform"
Info "Downloading DbEpoch for $platform..."

$zip = "$env:TEMP\DbEpoch.zip"
try {
    Invoke-WebRequest -Uri $url -OutFile $zip -UseBasicParsing
} catch {
    Err "Download failed: $url`n  $_"
}
Ok "Downloaded"

# â”€â”€ integrity check â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
$assetName = "DbEpoch-$platform.zip"
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

# â”€â”€ extract â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

# â”€â”€ add to PATH â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
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

# â”€â”€ verify â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
$exe = Join-Path $InstallDir "DbEpoch.exe"
if (Test-Path $exe) {
    $version = & $exe --version 2>&1 | Select-Object -First 1
    Ok $version
} else {
    Warn "Binary not found at $exe"
}

Write-Host ""
Info "Run 'DbEpoch --help' to get started."
Write-Host ""
