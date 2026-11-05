$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/Stage4FusionTimingReview.cs'))
$taskResult = [Gatefall.Tests.Stage4FusionTimingReview]::RunAll()
$taskResult | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/stage4-fusion-timing-v01013.txt') -Encoding utf8
Write-Output $taskResult
