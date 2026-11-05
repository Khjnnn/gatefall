$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/CampaignChecks.cs'))
$result = [Gatefall.Tests.CampaignChecks]::RunAll((Join-Path $taskProject 'TestResults'))
$result | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/campaign-results.txt') -Encoding utf8
Write-Output $result
