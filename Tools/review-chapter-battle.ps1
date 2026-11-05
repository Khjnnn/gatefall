param([switch]$PostBoss)
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/ChapterBattleReview.cs'))
$result = if($PostBoss) { [Gatefall.Tests.ChapterBattleReview]::RunPostBoss() } else { [Gatefall.Tests.ChapterBattleReview]::RunAll() }
$taskFile = if($PostBoss) { 'TestResults/post-boss-stage7-v01018.txt' } else { 'TestResults/chapter-battle-review.txt' }
$result | Set-Content -LiteralPath (Join-Path $taskProject $taskFile) -Encoding utf8
Write-Output $result
