$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot=Join-Path $taskProject 'TestResults/heavy-speed-v0190-review'
# This analysis checkpoint requires the pre-adoption product. Use recorded output after adoption.
foreach($taskRow in @(Get-Content TestResults/source-hashes-v0190.json -Raw|ConvertFrom-Json)){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product changed after model selection; use preserved comparison instead'} }
$taskData=@{}
foreach($taskName in @('baseline','speed125','speed140')){$taskData[$taskName]=Get-Content "TestResults/heavy-speed-v0190-$taskName.json" -Raw|ConvertFrom-Json}
$taskBase=$taskData['baseline']
if($taskBase.contracts.Count -ne 28 -or $taskBase.spawnStats.Count -ne 840){throw 'Baseline construction scope differs'}
$taskScope=@()
foreach($taskName in @('speed125','speed140')){
    $taskC=$taskData[$taskName];$taskScale=if($taskName -eq 'speed125'){1.25}else{1.40};$taskChanged=0
    if((ConvertTo-Json -Depth 9 -Compress -InputObject $taskC.contracts) -cne (ConvertTo-Json -Depth 9 -Compress -InputObject $taskBase.contracts)){throw 'Geometry/schedule changed'}
    if($taskC.spawnStats.Count -ne 840){throw 'Spawn-stat scope differs'}
    for($taskI=0;$taskI -lt 840;$taskI++){
        $taskA=$taskBase.spawnStats[$taskI];$taskB=$taskC.spawnStats[$taskI]
        foreach($taskField in @('stage','crisis','wave','kind','hp')){if($taskA.$taskField -ne $taskB.$taskField){throw 'HP/type/fixture changed'}}
        $taskExpected=$taskA.stage -eq 9 -and -not $taskA.crisis -and $taskA.wave -ge 3 -and $taskA.kind -in @('Brute','Siege')
        $taskSpeed=if($taskExpected){$taskA.speed*$taskScale}else{$taskA.speed}
        if([math]::Abs($taskSpeed-$taskB.speed) -gt .000001){throw 'Speed isolation differs'}
        if($taskExpected){$taskChanged++}
    }
    if($taskChanged -ne 6){throw 'Expected six affected spawn-stat cells'}
    foreach($taskCore in Get-ChildItem -LiteralPath (Join-Path $taskRoot 'baseline') -Filter '*.cs'){
        $taskOther=Join-Path $taskRoot "$taskName/$($taskCore.Name)"
        $taskA=[IO.File]::ReadAllText($taskCore.FullName);$taskB=[IO.File]::ReadAllText($taskOther)
        if($taskCore.Name -eq 'DefenseSimulation.cs'){
            $taskLine="`n                    if(Rules.Stage.Index==8 && !Rules.Crisis && g.Wave>=3 && (g.Kind==EnemyKind.Brute || g.Kind==EnemyKind.Siege))speed*=$('{0:F2}' -f $taskScale)f;"
            if($taskB.Replace($taskLine,'') -ne $taskA){throw 'Candidate code changed beyond speed condition'}
        }elseif($taskA -ne $taskB){throw 'Other candidate core differs'}
    }
    $taskScope+=[pscustomobject]@{candidate=$taskName;constructionCases=28;spawnStatCells=840;affectedCells=$taskChanged;otherCellsUnchanged=834;geometryAndScheduleEqual=$true;hpAndRewardsEqual=$true}
}
$taskInvestment=Get-Content TestResults/investment-v0190-raw.json -Raw|ConvertFrom-Json
foreach($taskCase in $taskBase.models){
    $taskA=$taskCase.result
    $taskB=if($taskCase.policy -eq 'low6'){@($taskInvestment.newCases|Where-Object {$_.budgetCap -eq 270 -and $_.model.Seed -eq 7})[0].model}else{$taskInvestment.fullSeed7Observation}
    foreach($taskField in @('Stage','Seed','State','Core','Towers','Spent','Kills','Fusions','FiredSites','FrequentFiringSites','PeakActive3Seconds','PeakProjectiles','PeakEnemies','EndingCredits','CoreDamageEvents','CoreContacts','SkillUses')){if($taskA.$taskField -ne $taskB.$taskField){throw "Selected baseline parity differs: $taskField"}}
    if([math]::Abs($taskA.Time-$taskB.Time) -gt .0001 -or (ConvertTo-Json -Compress -InputObject $taskA.PurchaseLog) -cne (ConvertTo-Json -Compress -InputObject $taskB.PurchaseLog) -or (ConvertTo-Json -Compress -InputObject $taskA.SiteShots) -cne (ConvertTo-Json -Compress -InputObject $taskB.SiteShots)){throw 'Selected baseline time/ledger/shots differ'}
}
$taskSupplement=Get-Content TestResults/heavy-speed-v0190-speed125-supplemental.json -Raw|ConvertFrom-Json
$taskRows=@()
foreach($taskName in @('speed125','speed140')){
    $taskModels=@($taskData[$taskName].models)
    if($taskName -eq 'speed125'){$taskModels+=@($taskSupplement.models)}
    foreach($taskCase in $taskModels){
        $taskR=$taskCase.result
        if($taskR.Stage -ne 9 -or $taskR.Sites -ne 24 -or $taskR.StartingCredits -ne 280){throw 'Candidate fixture differs'}
        if($taskCase.policy -eq 'low6' -and ($taskR.Towers -ne 6 -or $taskR.Spent -ne 270)){throw 'Initial-six policy differs'}
        if($taskCase.policy -eq 'limited7' -and ($taskR.Towers -ne 7 -or $taskR.Spent -ne 380)){throw 'Limited-seven policy differs'}
        if($taskCase.policy -in @('full14','many','upgrade') -and ($taskR.Towers -ne 14 -or $taskR.State -ne 'Won' -or $taskR.Core -ne 300)){throw 'Reinforcement strategy regression'}
        if($taskR.State -eq 'Won' -and $taskR.Kills -ne 624){throw 'Incomplete victory'}
        if($taskR.PSObject.Properties.Name -contains 'AllSpawned'){
            if($taskR.State -eq 'Won' -and (-not $taskR.AllSpawned -or $taskR.Remaining -ne 0 -or $taskR.Spawned -ne 624)){throw 'Victory without annihilation'}
            if($taskR.SupplyCredits -gt 500 -or $taskR.CardOffers -gt 3 -or $taskR.CatalystsGranted -gt 2){throw 'Resource caps differ'}
        }
        $taskRows+=[pscustomobject]@{candidate=$taskName;seed=$taskR.Seed;policy=$taskCase.policy;state=$taskR.State;core=$taskR.Core;time=$taskR.Time;towers=$taskR.Towers;spent=$taskR.Spent;kills=$taskR.Kills;fired=$taskR.FiredSites;frequent=$taskR.FrequentFiringSites;peakRecent=$taskR.PeakActive3Seconds;peakEnemies=$taskR.PeakEnemies;coreDamageEvents=$taskR.CoreDamageEvents;contacts=$taskR.CoreContacts;overlapHeavySeconds=$taskR.LateHeavyWithLightSeconds;peakOverlappingHeavy=$taskR.PeakHeavyWithLight;lastHeavyKill=$taskR.LastHeavyKillTime;lastLightKill=$taskR.LastLightKillTime}
    }
}
if($taskRows.Count -ne 21){throw 'Unexpected unique candidate sample count'}
$taskOriginalModels=(Get-Content TestResults/pulse-v0190-window12.json -Raw|ConvertFrom-Json).models
$taskPairs=@(foreach($taskR in @($taskRows|Where-Object {$_.candidate -eq 'speed125' -and $_.policy -in @('full14','many','upgrade')})){
    $taskPolicy=if($taskR.policy -eq 'full14'){'mixed'}else{$taskR.policy}
    $taskB=@($taskOriginalModels|Where-Object {$_.Policy -eq $taskPolicy -and $_.Seed -eq $taskR.seed})[0]
    if($taskB.Spent -ne $taskR.spent){throw 'Paired final spending differs; inspect before adopting'}
    [pscustomobject]@{seed=$taskR.seed;policy=$taskPolicy;spent=$taskR.spent;baselineFired=$taskB.FiredSites;fired=$taskR.fired;baselineFrequent=$taskB.FrequentFiringSites;frequent=$taskR.frequent;baselinePeakRecent=$taskB.PeakActive3Seconds;peakRecent=$taskR.peakRecent;baselineTime=$taskB.Time;time=$taskR.time;baselineCore=$taskB.Core;core=$taskR.core}
})
[pscustomobject]@{created=(Get-Date).ToString('o');task=170;productAtSelection='0.19.0';selectedCandidate='speed125';selection='Moderate late-heavy speed produces a six/seven/fourteen investment gradient; speed140 applies earlier pressure and is rejected.';newUniqueCandidateConditions=21;baselineConditionsReused=15;fullBaselineCasesReused=9;lowBudgetBaselineCasesReused=6;selectedBaselineObservationReplays=2;selectedBaselineParity=$true;scope=$taskScope;rows=$taskRows;fullNinePairs=$taskPairs;baselineOverlapSeed7=$taskBase.models;limits=@('One fixed mixed opening at six/seven towers and three full purchasing policies; not all placements or human fun.','5.2-distance overlap is a world-space proxy, not actual shared tower hits or exact arrival time.','Full reinforcement seed7 overlap decreases129.85 to122; do not claim a universal overlap increase.','Cumulative overlap and last-kill time of lost runs are censored at defeat.','Matching purchasing policy does not freeze all purchase times; final paired spending is equal but timelines are retained in raw ledgers.');humanPlaytested=$false;newAndroidVerified=$false}|ConvertTo-Json -Depth 10|Set-Content TestResults/heavy-speed-v0190-comparison.json
$taskRows|Group-Object candidate,policy|ForEach-Object{[pscustomobject]@{group=$_.Name;cases=$_.Count;wins=@($_.Group|Where-Object state -eq Won).Count;minimumCore=($_.Group|Measure-Object core -Minimum).Minimum;fired=($_.Group|Measure-Object fired -Average).Average;timeMin=($_.Group|Measure-Object time -Minimum).Minimum;timeMax=($_.Group|Measure-Object time -Maximum).Maximum}}|Format-Table -AutoSize
'PASS: candidate speed/HP/schedule scope, baseline observation parity, 21 new conditions and paired nine-strategy reinforcement results'
