$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Join-Path $taskProject 'Assets/Scripts/Presentation/ActiveCombatTiming.cs'),(Join-Path $taskProject 'Tests/ActiveCombatTimingChecks.cs'))
$taskResult=[Gatefall.Tests.ActiveCombatTimingChecks]::Run()
$taskResult|Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/active-combat-timing-results.txt')
$taskResult
