@echo off
setlocal
cd /d "%~dp0"
chcp 65001 >nul
echo [QClaw] One-click install and start
powershell -NoProfile -ExecutionPolicy Bypass -File ".\quick_start.ps1" -Mode noauth
if errorlevel 1 (
  echo.
  echo 启动失败，请检查上方报错信息。
  pause
)
endlocal
