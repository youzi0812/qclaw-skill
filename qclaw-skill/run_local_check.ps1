param(
    [ValidateSet("noauth", "auth")]
    [string]$Mode = "noauth",
    [string]$ApiKey = "demo-key"
)

$ErrorActionPreference = "Stop"

function Invoke-Step {
    param(
        [string]$Title,
        [scriptblock]$Action
    )
    Write-Host ""
    Write-Host "=== $Title ===" -ForegroundColor Cyan
    & $Action
}

Set-Location -Path $PSScriptRoot

$env:PYTHONIOENCODING = "utf-8"
$env:PYTHONUTF8 = "1"

$serverProc = $null

try {
    if ($Mode -eq "auth") {
        $env:SOULCORE_API_KEY = $ApiKey
        Write-Host "鉴权模式已启用，API Key: $ApiKey" -ForegroundColor Yellow
    } else {
        if (Test-Path Env:SOULCORE_API_KEY) {
            Remove-Item Env:SOULCORE_API_KEY
        }
        Write-Host "无鉴权模式（默认）" -ForegroundColor Yellow
    }

    Invoke-Step "启动本地服务" {
        $serverProc = Start-Process -FilePath "python" `
            -ArgumentList "-m uvicorn server.app:app --host 127.0.0.1 --port 8000" `
            -WorkingDirectory $PSScriptRoot `
            -PassThru
    }

    Invoke-Step "等待健康检查就绪" {
        $ready = $false
        for ($i = 0; $i -lt 30; $i++) {
            try {
                $null = Invoke-RestMethod -Uri "http://127.0.0.1:8000/v1/health" -Method Get -TimeoutSec 2
                $ready = $true
                break
            } catch {
                Start-Sleep -Seconds 1
            }
        }
        if (-not $ready) {
            throw "服务未在预期时间内启动成功。"
        }
    }

    Invoke-Step "运行 smoke check" {
        python "demo/smoke_check.py"
        if ($LASTEXITCODE -ne 0) {
            throw "smoke_check 失败。"
        }
    }

    Invoke-Step "运行单元测试" {
        python -m unittest discover -s tests -p "test_*.py"
        if ($LASTEXITCODE -ne 0) {
            throw "单元测试失败。"
        }
    }

    Invoke-Step "运行全链路演示" {
        python "demo/demo_e2e_full.py"
        if ($LASTEXITCODE -ne 0) {
            throw "全链路演示失败。"
        }
    }

    Write-Host ""
    Write-Host "全部检查通过。" -ForegroundColor Green
} finally {
    if ($serverProc -and -not $serverProc.HasExited) {
        Stop-Process -Id $serverProc.Id -Force
        Write-Host ""
        Write-Host "已停止本地服务（PID: $($serverProc.Id)）。" -ForegroundColor DarkGray
    }
}
