$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=Join-Path $root 'TestResults/chapter2-completion-20261110'
$baseline=Join-Path $root 'TestResults/chapter2-design-20261107/profile-final-cold.sav'
New-Item $out -ItemType Directory -Force|Out-Null
Add-Type -Path @((Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName+"$root/Tests/ChapterContinuationReview.cs")
$report=[Gatefall.Tests.ChapterContinuationReview]::Run($out,$baseline)
$report|ConvertTo-Json -Depth 12|Set-Content "$out/model-sequence.json" -Encoding utf8
$report.Battles|Select-Object Stage,Outcome,End,Core,Spawned,Spent,Towers
$report.Fusion|Select-Object Stage,Outcome,End,Core,SealAt,Spent,Towers
