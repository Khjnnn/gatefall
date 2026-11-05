$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/EarlyTowerRoleReview.cs'))
$taskResult = [Gatefall.Tests.EarlyTowerRoleReview]::RunAll()
$taskResult | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/early-tower-role-v01016.txt') -Encoding utf8
Write-Output $taskResult
