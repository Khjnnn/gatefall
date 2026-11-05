$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/MarkerOpeningReview.cs'))
$taskResult = [Gatefall.Tests.MarkerOpeningReview]::RunAll()
$taskResult | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/marker-opening-v01011.txt') -Encoding utf8
Write-Output $taskResult
