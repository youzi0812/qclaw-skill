param(
    [ValidateSet("noauth", "auth")]
    [string]$Mode = "noauth",
    [string]$ApiKey = "demo-key"
)

$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

Write-Host "=== QClaw One-Click Installer ===" -ForegroundColor Cyan

Write-Host "[1/2] Installing skill to local agents..." -ForegroundColor Yellow
npx skills add $PSScriptRoot -g -y
if ($LASTEXITCODE -ne 0) {
    throw "Skill installation failed. Please check network/npm environment."
}

Write-Host "[2/2] Starting local backend..." -ForegroundColor Yellow
Set-Location -Path (Join-Path $PSScriptRoot "qclaw-skill")
if ($Mode -eq "auth") {
    .\start_local.ps1 -Mode auth -ApiKey $ApiKey
} else {
    .\start_local.ps1 -Mode noauth
}
