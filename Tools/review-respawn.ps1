$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/RespawnBattleReview.cs'))
$taskReport = [Gatefall.Tests.RespawnBattleReview]::Run()
$taskReport | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/respawn-battle-review.txt') -Encoding utf8
Write-Output $taskReport
