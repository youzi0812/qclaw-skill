@echo off
setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File ".\start_local.ps1" -Mode noauth
if errorlevel 1 (
  echo.
  echo 启动失败，请检查上方报错信息。
  pause
)
endlocal
