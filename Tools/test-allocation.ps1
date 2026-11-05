# Run in a fresh PowerShell process: pwsh -NoProfile -File .\Tools\test-allocation.ps1
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName+(Join-Path $taskProject 'Tests/AllocationChecks.cs'))
$taskResult=[Gatefall.Tests.AllocationChecks]::RunAll()
$taskResultsDir=Join-Path $taskProject 'TestResults'
New-Item -ItemType Directory -Path $taskResultsDir -Force|Out-Null
$taskResult|Set-Content -LiteralPath (Join-Path $taskResultsDir 'allocation-results.txt') -Encoding utf8
Write-Output $taskResult
