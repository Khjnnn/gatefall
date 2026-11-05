# In-memory reward/purchase fixtures, not simulated or Android battle completions.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path $project 'Tests/ProgressionEconomyReview.cs'))
$result=[Gatefall.Tests.ProgressionEconomyReview]::Run()
[ordered]@{version='0.28.6';scope='In-memory terminal-state economic transactions; no battle simulation, Android completion or human difficulty claim';newCombatModels=0;newNativeRuns=0;result=$result;coreHashes=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}})}|ConvertTo-Json -Depth 9|Set-Content "$root/progression-economy-v0286.json" -Encoding utf8
"PASS economic paths:$($result.CompletedPaths) /purchase-completion-roundtrip steps:$($result.Transactions),322first-clear income,all recommendations affordable including hero switching; optional early overgrowth deficit:$($result.EarlyResearchDeficit),hypothetical repeat wins:$($result.RecoveryWins). No combat completion claim."
