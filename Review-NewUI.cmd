@echo off
cd /d "%~dp0"
pwsh -NoProfile -File "%~dp0Tools\open-soft-ui-review.ps1"
if errorlevel 1 pause
