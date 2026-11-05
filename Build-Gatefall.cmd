@echo off
"C:\Users\Pro\.cache\codex-runtimes\codex-primary-runtime\dependencies\native\powershell\pwsh.exe" -NoProfile -File "%~dp0Tools\build-delivery.ps1"
echo.
if errorlevel 1 (echo Build failed. See TestResults logs.) else (echo Android delivery build completed.)
pause
