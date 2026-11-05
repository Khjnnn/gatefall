$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/Stage6BossReview.cs'))
$taskResult = [Gatefall.Tests.Stage6BossReview]::RunAll()
$taskResult | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/stage6-boss-v01017.txt') -Encoding utf8
Write-Output $taskResult
