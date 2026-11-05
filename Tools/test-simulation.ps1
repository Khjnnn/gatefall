# Run in a fresh PowerShell process: pwsh -NoProfile -File .\Tools\test-simulation.ps1
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
$taskCore = Join-Path $taskProject 'Assets\Scripts\Core\DefenseSimulation.cs'
$taskChecks = Join-Path $taskProject 'Tests\SimulationChecks.cs'
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + $taskChecks) -ErrorAction Stop
$taskResult = [Gatefall.Tests.SimulationChecks]::RunAll()
$taskResultsDir = Join-Path $taskProject 'TestResults'
New-Item -ItemType Directory -Path $taskResultsDir -Force | Out-Null
$taskResult | Set-Content -LiteralPath (Join-Path $taskResultsDir 'simulation-results.txt') -Encoding utf8
Write-Output $taskResult
