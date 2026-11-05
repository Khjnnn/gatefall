$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
if(Test-Path -LiteralPath 'TestResults/v0185-ready-seed.sav'){throw 'Preserve the existing fixture; do not replace it'}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
$taskP=[Gatefall.Core.CampaignProfile]::new()
$taskP.MagicCores=62
$taskP.StoryMask=16383
$taskP.TutorialMask=16383
$taskP.LastReward=22
$taskP.LastRewardRun=[Guid]::NewGuid().ToString('N')
for($taskI=0;$taskI -lt 7;$taskI++){$taskP.BestStars[$taskI]=2;$taskP.FirstCleared[$taskI]=$true}
if(-not $taskP.Valid() -or -not $taskP.IsStageOpen(7) -or -not $taskP.ShouldShowLesson(7)){throw 'Fixture does not expose stage8 lesson'}
$taskFile=Join-Path $taskProject 'TestResults/v0185-ready-seed.sav'
[IO.File]::WriteAllBytes($taskFile,[Gatefall.Core.CampaignStore]::Encode($taskP))
[pscustomobject]@{created=(Get-Date).ToString('o');syntheticOnly=$true;magic=$taskP.MagicCores;stars=$taskP.StarsTotal;lastReward=$taskP.LastReward;rewardId=$taskP.LastRewardRun;lesson8Visible=$taskP.ShouldShowLesson(7);bytes=(Get-Item -LiteralPath $taskFile).Length;sha256=(Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant()}|ConvertTo-Json|Set-Content TestResults/v0185-ready-seed.json
