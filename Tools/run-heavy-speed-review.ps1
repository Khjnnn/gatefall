param([ValidateSet('baseline','speed125','speed140')][string]$Candidate)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot=Join-Path $taskProject 'TestResults/heavy-speed-v0190-review'
$taskOutput="TestResults/heavy-speed-v0190-$Candidate.json"
if(Test-Path -LiteralPath $taskOutput){throw 'Preserve prior raw output'}
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskRoot $Candidate) -Filter '*.cs').FullName+(Join-Path $taskRoot 'InvestmentNecessityReview.cs'))
$taskSeeds=if($Candidate -eq 'baseline'){@(7)}else{@(7,19,41)}
$taskRows=@(foreach($taskSeed in $taskSeeds){foreach($taskCap in @(270,[int]::MaxValue)){
    [pscustomobject]@{policy=$(if($taskCap -eq 270){'low6'}else{'full14'});result=[Gatefall.Tests.InvestmentNecessityReview]::Run(8,$taskSeed,'mixed',$taskCap)}
}})
# Save battle observations before additional contract checks, even if those fail.
[pscustomobject]@{created=(Get-Date).ToString('o');candidate=$Candidate;models=$taskRows}|ConvertTo-Json -Depth 9|Set-Content -LiteralPath $taskOutput
$taskSpawn=[Gatefall.Core.DefenseSimulation].GetMethod('Spawn',[Reflection.BindingFlags]'NonPublic,Instance')
$taskContracts=@();$taskStats=@()
foreach($taskCrisis in @($false,$true)){foreach($taskStage in [Gatefall.Core.CampaignCatalog]::Stages){
    $taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=$taskStage;Crisis=$taskCrisis})
    $taskRoads=@(for($taskLane=0;$taskLane -lt $taskG.Roads.Length;$taskLane++){[pscustomobject]@{lane=$taskLane;points=@($taskG.Roads[$taskLane]|Select-Object X,Y)}})
    $taskContracts+=[pscustomobject]@{stage=$taskStage.Index+1;crisis=$taskCrisis;sockets=@($taskG.BoardSockets|Select-Object X,Y);roads=$taskRoads;schedule=@($taskG.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm,Warned)}
    for($taskWave=0;$taskWave -lt 6;$taskWave++){foreach($taskKind in [Enum]::GetValues([Gatefall.Core.EnemyKind])){
        $taskGroup=[Gatefall.Core.SpawnGroup]@{Wave=$taskWave;Lane=$taskG.OpenLanes[0];Kind=$taskKind;Count=1}
        $taskSpawn.Invoke($taskG,@($taskGroup))|Out-Null
        $taskE=$taskG.Enemies[$taskG.Enemies.Count-1]
        $taskStats+=[pscustomobject]@{stage=$taskStage.Index+1;crisis=$taskCrisis;wave=$taskWave;kind=$taskKind.ToString();hp=$taskE.Health;speed=$taskE.Speed}
        $taskG.Enemies.Clear()
    }}
}}
[pscustomobject]@{created=(Get-Date).ToString('o');candidate=$Candidate;models=$taskRows;contracts=$taskContracts;spawnStats=$taskStats;newCombatConditions=$(if($Candidate -eq 'baseline'){0}else{6});selectedObservationReplays=$(if($Candidate -eq 'baseline'){2}else{0});humanPlaytested=$false}|ConvertTo-Json -Depth 10|Set-Content -LiteralPath $taskOutput
$taskRows|ForEach-Object{[pscustomobject]@{candidate=$Candidate;seed=$_.result.Seed;policy=$_.policy;state=$_.result.State;core=$_.result.Core;time=$_.result.Time;towers=$_.result.Towers;spent=$_.result.Spent;fired=$_.result.FiredSites;frequent=$_.result.FrequentFiringSites;peakRecent=$_.result.PeakActive3Seconds;contacts=$_.result.CoreContacts;overlap=$_.result.LateHeavyWithLightSeconds}}|Format-Table -AutoSize
