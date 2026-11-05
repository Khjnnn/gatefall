$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/MarkerBattleReview.cs'))
$result = [Gatefall.Tests.MarkerBattleReview]::RunAll()
$result | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/marker-battle-review.txt') -Encoding utf8
Write-Output $result
