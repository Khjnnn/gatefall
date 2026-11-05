param([ValidateSet('baseline','three-entrances','three-clear-courts')][string]$Variant)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot=Join-Path $taskProject 'TestResults/alley-v0192-review'
$taskCore=Join-Path $taskRoot $Variant
$taskHarness=Get-Content (Join-Path $taskRoot 'AlleyDensityReview.cs') -Raw
$taskNeedle='new CampaignBattle {Stage=CampaignCatalog.Stages[index]}'
if(([regex]::Matches($taskHarness,[regex]::Escape($taskNeedle))).Count -ne 1){throw 'Crisis transform ambiguous'}
$taskObserver=$taskHarness.Replace('class AlleyDensityReview','class AlleyCrisisReview').Replace($taskNeedle,'new CampaignBattle {Stage=CampaignCatalog.Stages[index],Crisis=true}')
$taskObserverPath=Join-Path $taskRoot ('AlleyCrisisReview-'+$Variant+'.cs')
Set-Content -LiteralPath $taskObserverPath -Value $taskObserver -NoNewline
Add-Type -Path @((Get-ChildItem -LiteralPath $taskCore -Filter '*.cs').FullName+$taskObserverPath)
$taskRows=@(foreach($taskStage in 0..13){foreach($taskCrisis in @($false,$true)){
    $taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[$taskStage];Crisis=$taskCrisis})
    [pscustomobject]@{stage=$taskStage+1;crisis=$taskCrisis;sites=@($taskG.BoardSockets|Select-Object X,Y);lanes=$taskG.OpenLanes;roads=@(foreach($taskLane in $taskG.OpenLanes){[pscustomobject]@{lane=$taskLane;points=@($taskG.Roads[$taskLane]|Select-Object X,Y)}});startingCredits=$taskG.Credits;supply=$taskG.WaveSupplyCredits;scheduled=(@($taskG.Schedule)|Measure-Object Count -Sum).Sum;schedule=@($taskG.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm)}
}})
$taskRun=[Gatefall.Tests.AlleyCrisisReview].GetMethod('Run',[Reflection.BindingFlags]'Static,NonPublic')
$taskCrisisModels=@(foreach($taskSeed in @(7,19,41)){$taskRun.Invoke($null,@([int]7,[int]$taskSeed,'mixed'))})
[pscustomobject]@{created=(Get-Date).ToString('o');variant=$Variant;staticConstructionPairs=28;rows=$taskRows;newCrisisModels=3;crisisModels=$taskCrisisModels}|ConvertTo-Json -Depth 9|Set-Content ("TestResults/alley-v0192-scope-"+$Variant+'.json')
$taskCrisisModels|Select-Object Stage,Seed,Policy,State,Core,Time,Spent,Towers,Kills,FiredSites,FrequentFiringSites|Format-Table
