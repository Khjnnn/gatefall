$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName+(Join-Path $taskProject 'Tests/FusionReachabilityChecks.cs'))
$taskResult=[Gatefall.Tests.FusionReachabilityChecks]::Run()
$taskResult|Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/fusion-reachability-results.txt')
$taskResult
