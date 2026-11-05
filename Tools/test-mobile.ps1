$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/MobileChecks.cs'))
$taskResult = [Gatefall.Tests.MobileChecks]::RunAll()
$taskResult | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults\mobile-results.txt') -Encoding utf8
Write-Output $taskResult
