param([ValidateSet('baseline','speed115','speed125','pack2','pack3')][string]$Candidate,[ValidateSet('scope','pilot','expand')][string]$Mode='pilot',[ValidateSet('speed','packets')][string]$Family='speed')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
if(($Family -eq 'speed' -and $Candidate -like 'pack*') -or ($Family -eq 'packets' -and $Candidate -notlike 'pack*')){throw 'Candidate/family mismatch; reuse recorded baseline for packet review'}
$taskRoot=Join-Path (Get-Location) "TestResults/stage12-$Family-v0202-review"
$taskCore=Join-Path $taskRoot $Candidate
$taskOutput="TestResults/stage12-$Family-v0202-$Candidate-$Mode.json"
if(Test-Path -LiteralPath $taskOutput){throw 'Preserve recorded observations'}
if($Candidate -eq 'baseline' -and $Mode -eq 'expand'){throw 'Reuse the existing24 baseline conditions; only selected observation replays are needed'}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0202.json -Raw|ConvertFrom-Json)){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product changed during review'}}
Add-Type -Path @((Get-ChildItem -LiteralPath $taskCore -Filter '*.cs').FullName+(Join-Path $taskRoot 'StageThreatObservation.cs')+(Join-Path $taskRoot 'MarkerBattleReview.cs'))
if($Mode -eq 'scope'){
    $taskSpawn=[Gatefall.Core.DefenseSimulation].GetMethod('Spawn',[Reflection.BindingFlags]'NonPublic,Instance')
    $taskContracts=@();$taskStats=@();$taskActualHeavy=@()
    foreach($taskStage in [Gatefall.Core.CampaignCatalog]::Stages){foreach($taskCrisis in @($false,$true)){
        $taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=$taskStage;Crisis=$taskCrisis})
        $taskContracts+=[pscustomobject]@{stage=$taskStage.Index+1;crisis=$taskCrisis;startingCredits=$taskG.Credits;supply=$taskG.WaveSupplyCredits;sockets=@($taskG.BoardSockets|Select-Object X,Y);roads=@(for($taskLane=0;$taskLane -lt $taskG.Roads.Length;$taskLane++){[pscustomobject]@{lane=$taskLane;points=@($taskG.Roads[$taskLane]|Select-Object X,Y)}});schedule=@($taskG.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm,Warned)}
        if($taskStage.Index -eq 11 -and -not $taskCrisis){$taskActualHeavy=@($taskG.Schedule|Where-Object {$_.Wave -ge 3 -and $_.Kind -in @([Gatefall.Core.EnemyKind]::Brute,[Gatefall.Core.EnemyKind]::Siege)}|Select-Object Wave,Lane,Kind,Count,Start,Interval)}
        foreach($taskWave in 0..5){foreach($taskKind in [Enum]::GetValues([Gatefall.Core.EnemyKind])){
            $taskGroup=[Gatefall.Core.SpawnGroup]@{Wave=$taskWave;Lane=$taskG.OpenLanes[0];Kind=$taskKind;Count=1}
            $taskSpawn.Invoke($taskG,@($taskGroup))|Out-Null
            $taskE=$taskG.Enemies[$taskG.Enemies.Count-1]
            $taskStats+=[pscustomobject]@{stage=$taskStage.Index+1;crisis=$taskCrisis;wave=$taskWave;kind=$taskKind.ToString();hp=$taskE.Health;speed=$taskE.Speed}
            $taskG.Enemies.Clear()
        }}
    }}
    [pscustomobject]@{created=(Get-Date).ToString('o');candidate=$Candidate;staticCases=28;spawnProbeCells=$taskStats.Count;contracts=$taskContracts;spawnStats=$taskStats;actualLateStage12Heavy=$taskActualHeavy;newBattleConditions=0}|ConvertTo-Json -Depth 10|Set-Content $taskOutput
    "Recorded ${Candidate} 28 static cases/$($taskStats.Count) spawn probes; not battle runs.";exit
}
$taskBases=@{7=(Get-Content TestResults/marker-stage12-cap7-v0202.json -Raw|ConvertFrom-Json);16=(Get-Content TestResults/marker-stage12-cap16-v0202.json -Raw|ConvertFrom-Json)}
$taskRun=[Gatefall.Tests.MarkerBattleReview].GetMethod('Run',[Reflection.BindingFlags]'NonPublic,Static')
$taskRows=@(foreach($taskSeed in @(7,19,41)){foreach($taskPlan in @('spears','marker','cluster_spears','cluster_marker')){foreach($taskCap in @(7,16)){
    $taskPilot=$taskSeed -in @(7,41) -and $taskPlan -in @('spears','cluster_marker')
    if(($Mode -eq 'pilot' -and -not $taskPilot) -or ($Mode -eq 'expand' -and $taskPilot)){continue}
    [Gatefall.Tests.MarkerBattleReview]::TowerLimit=$taskCap
    $taskSummary=$taskRun.Invoke($null,@([int]11,[string]$taskPlan,[int]$taskSeed,[bool]$true))
    $taskG=[Gatefall.Tests.MarkerBattleReview]::Last;$taskAudit=[Gatefall.Tests.MarkerBattleReview]::Audit
    if($taskG.State.ToString() -notin @('Won','Lost') -or $taskG.Towers.Count -gt $taskCap){throw 'Incomplete/out-of-policy result'}
    $taskPrevious=@($taskBases[$taskCap].rows|Where-Object {$_.seed -eq $taskSeed -and $_.plan -eq $taskPlan})
    if($taskPrevious.Count -ne 1){throw 'Missing unique current baseline'}
    $taskB=$taskPrevious[0];$taskPurchases=@($taskG.Telemetry.Purchases.ToArray())
    $taskCards=[int[]]$taskG.Cards.Clone()
    if($Candidate -eq 'baseline'){
        if($taskG.State.ToString() -ne $taskB.state -or [math]::Abs($taskG.Elapsed-$taskB.time) -gt .0001 -or $taskG.MinimumCoreHealth -ne $taskB.core -or $taskG.PlayerHealth -ne $taskB.hunter -or $taskG.Credits -ne $taskB.credits -or $taskG.Telemetry.CreditsSpent -ne $taskB.spent -or $taskG.Kills -ne $taskB.kills -or $taskG.Towers.Count -ne $taskB.towers -or $taskG.Telemetry.MarkedHits -ne $taskB.markedHits -or [math]::Abs($taskG.Telemetry.MarkBonusDamage-$taskB.effectiveMarkBonus) -gt .001){throw 'Read-only observer changed baseline'}
        foreach($taskField in @(@($taskPurchases,$taskB.purchases),@($taskCards,$taskB.cards),@($taskG.Telemetry.TowerDamage,$taskB.kindDamage))){if(($taskField[0]|ConvertTo-Json -Compress) -cne ($taskField[1]|ConvertTo-Json -Compress)){throw 'Baseline purchase/card/damage parity differs'}}
    }
    [pscustomobject]@{seed=$taskSeed;plan=$taskPlan;cap=$taskCap;state=$taskG.State.ToString();time=$taskG.Elapsed;core=$taskG.MinimumCoreHealth;hunter=$taskG.PlayerHealth;spent=$taskG.Telemetry.CreditsSpent;credits=$taskG.Credits;towers=$taskG.Towers.Count;kills=$taskG.Kills;spawned=$taskG.Telemetry.Spawned;remaining=$taskG.Enemies.Count;allSpawned=$taskG.AllWavesSpawned;markedHits=$taskG.Telemetry.MarkedHits;effectiveMarkBonus=$taskG.Telemetry.MarkBonusDamage;kindDamage=$taskG.Telemetry.TowerDamage;cards=$taskCards;purchases=$taskPurchases;supplies=$taskG.SuppliesGranted;cardOffers=$taskG.Telemetry.CardOffers;catalysts=$taskG.Telemetry.CoresGranted;pressure=$taskAudit;baselineCore=$taskB.core;baselineTime=$taskB.time;baselineSpent=$taskB.spent;baselineCardsEqual=(($taskCards|ConvertTo-Json -Compress) -ceq ($taskB.cards|ConvertTo-Json -Compress));rawSummary=$taskSummary}
}}})
$taskExpected=if($Mode -eq 'pilot'){8}else{16}
if($taskRows.Count -ne $taskExpected){throw 'Wrong selected condition count'}
[pscustomobject]@{created=(Get-Date).ToString('o');candidate=$Candidate;family=$Family;phase=$Mode;sourceVersion='0.20.2';newCandidateConditions=$(if($Candidate -eq 'baseline'){0}else{$taskRows.Count});selectedBaselineObservationReplays=$(if($Candidate -eq 'baseline'){$taskRows.Count}else{0});baseline24Reused=$true;onlyLate12HeavySpeedChanged=($Family -eq 'speed' -and $Candidate -ne 'baseline');onlyStage12LatePacketTimingChanged=($Family -eq 'packets');rows=$taskRows;productChanged=$false;newApk=$false;newAvd=$false;humanPlaytested=$false;scope='Same upgrade-first purchase opportunity rules, first offered card and no Hunter skill. Geometric heavy/swarm proximity within5.2 after90s is an observer, not causal delay or human fun.'}|ConvertTo-Json -Depth 9|Set-Content $taskOutput
$taskRows|Select-Object seed,plan,cap,state,core,time,spent,@{n='fired';e={$_.pressure.FiredSites}},@{n='nearSwarm';e={[math]::Round($_.pressure.LateHeavyNearSwarmSeconds,2)}},@{n='lastHeavy';e={[math]::Round($_.pressure.LastHeavyKill,2)}}|Format-Table -AutoSize
