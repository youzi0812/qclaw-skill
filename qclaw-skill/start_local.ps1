param(
    [ValidateSet("noauth", "auth")]
    [string]$Mode = "noauth",
    [string]$ApiKey = "demo-key",
    [string]$BindHost = "127.0.0.1",
    [int]$Port = 8000,
    [int]$MaxAutoPortTries = 10,
    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"
Set-Location -Path $PSScriptRoot

$env:PYTHONIOENCODING = "utf-8"
$env:PYTHONUTF8 = "1"

function Test-PortInUse {
    param(
        [string]$HostName,
        [int]$PortNumber
    )
    $client = New-Object System.Net.Sockets.TcpClient
    try {
        $iar = $client.BeginConnect($HostName, $PortNumber, $null, $null)
        $connected = $iar.AsyncWaitHandle.WaitOne(500)
        if (-not $connected) {
            return $false
        }
        $client.EndConnect($iar)
        return $true
    } catch {
        return $false
    } finally {
        $client.Close()
    }
}

Write-Host "=== SoulCore QClaw Local Start ===" -ForegroundColor Cyan
Write-Host "Path: $PSScriptRoot"

if (-not $SkipInstall) {
    Write-Host ""
    Write-Host "[1/3] Installing dependencies..." -ForegroundColor Yellow
    python -m pip install -r "requirements.txt"
    if ($LASTEXITCODE -ne 0) {
        throw "Dependency install failed."
    }
}

Write-Host ""
Write-Host "[2/3] Configuring auth mode..." -ForegroundColor Yellow
if ($Mode -eq "auth") {
    $env:SOULCORE_API_KEY = $ApiKey
    Write-Host "Auth mode: ON (x-api-key required)" -ForegroundColor Green
} else {
    if (Test-Path Env:SOULCORE_API_KEY) {
        Remove-Item Env:SOULCORE_API_KEY
    }
    Write-Host "Auth mode: OFF" -ForegroundColor Green
}

Write-Host ""
Write-Host "[3/3] Starting API server..." -ForegroundColor Yellow
if ($MaxAutoPortTries -lt 1) {
    throw "MaxAutoPortTries must be >= 1"
}

$selectedPort = $Port
$found = $false
for ($i = 0; $i -lt $MaxAutoPortTries; $i++) {
    $candidate = $Port + $i
    if (-not (Test-PortInUse -HostName $BindHost -PortNumber $candidate)) {
        $selectedPort = $candidate
        $found = $true
        break
    }
}
if (-not $found) {
    throw "Port range $Port~$($Port + $MaxAutoPortTries - 1) is already in use. Please free a port and retry."
}
if ($selectedPort -ne $Port) {
    Write-Host "Port $Port is occupied. Auto-switched to $selectedPort." -ForegroundColor Yellow
}
Write-Host "URL: http://$BindHost`:$selectedPort"
Write-Host "Press Ctrl+C to stop."
Write-Host ""

python -m uvicorn server.app:app --host $BindHost --port $selectedPort
