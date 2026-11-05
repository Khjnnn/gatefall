$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskNames=@('baseline','west-hook','north-court','three-entrances','three-clear-courts')
$taskSets=@{};foreach($taskName in $taskNames){$taskSets[$taskName]=Get-Content ("TestResults/alley-v0192-"+$taskName+'.json') -Raw|ConvertFrom-Json}
$taskBase=$taskSets['baseline'];$taskHistorical=@((Get-Content TestResults/tower-density-v0170.json -Raw|ConvertFrom-Json).models|Where-Object Stage -eq 8)
foreach($taskB in $taskBase.models){
    $taskOld=@($taskHistorical|Where-Object {$_.Seed -eq $taskB.Seed -and $_.Policy -eq $taskB.Policy})
    if($taskOld.Count -ne 1){throw 'Missing historical pair'}
    foreach($taskKey in @('Sites','StartingCredits','Towers','Spent','Kills','Fusions','FiredSites','FrequentFiringSites','PeakActive3Seconds','PeakProjectiles','State','Core')){if($taskOld[0].$taskKey -ne $taskB.$taskKey){throw "Baseline differs: $taskKey"}}
    if([math]::Abs($taskOld[0].Time-$taskB.Time) -gt .0001){throw 'Baseline time differs'}
}
foreach($taskName in $taskNames){
    $taskD=$taskSets[$taskName]
    if(-not $taskD.geometryValid -or $taskD.models.Count -ne 9 -or $taskD.snapshot.sites -ne 22 -or $taskD.snapshot.scheduled -ne 612){throw 'Incomplete geometry or model data'}
    $taskFields=if($taskName -in @('three-entrances','three-clear-courts')){@('Wave','Kind','Count','Start','Interval','Swarm')}else{@('Wave','Lane','Kind','Count','Start','Interval','Swarm')}
    if((ConvertTo-Json -Compress -Depth 5 -InputObject @($taskBase.snapshot.schedule|Select-Object $taskFields)) -cne (ConvertTo-Json -Compress -Depth 5 -InputObject @($taskD.snapshot.schedule|Select-Object $taskFields))){throw "Spawn schedule differs beyond declared scope: $taskName"}
    foreach($taskM in $taskD.models){
        $taskB=@($taskBase.models|Where-Object {$_.Seed -eq $taskM.Seed -and $_.Policy -eq $taskM.Policy})[0]
        if($taskM.State -ne 'Won' -or $taskM.Core -ne 300 -or $taskM.Kills -ne 612 -or $taskM.Spawned -ne 612 -or -not $taskM.AllSpawned -or $taskM.Remaining -ne 0 -or $taskM.Towers -ne 14 -or $taskM.Spent -ne $taskB.Spent -or $taskM.CardOffers -ne 3 -or $taskM.Catalysts -ne 2 -or $taskM.SupplyCredits -ne 500){throw 'Outcome/purchase/resource parity differs'}
    }
}
$taskScopeBase=Get-Content TestResults/alley-v0192-scope-baseline.json -Raw|ConvertFrom-Json
$taskScopeFinal=Get-Content TestResults/alley-v0192-scope-three-clear-courts.json -Raw|ConvertFrom-Json
$taskChanged=@()
for($taskI=0;$taskI -lt 28;$taskI++){
    $taskB=$taskScopeBase.rows[$taskI];$taskA=$taskScopeFinal.rows[$taskI]
    if((ConvertTo-Json -Depth 7 -Compress -InputObject $taskB) -cne (ConvertTo-Json -Depth 7 -Compress -InputObject $taskA)){$taskChanged+=@($taskB|Select-Object stage,crisis)}
    if($taskB.stage -eq 8){if((ConvertTo-Json -Depth 5 -Compress -InputObject @($taskB.schedule|Select-Object Wave,Kind,Count,Start,Interval,Swarm)) -cne (ConvertTo-Json -Depth 5 -Compress -InputObject @($taskA.schedule|Select-Object Wave,Kind,Count,Start,Interval,Swarm))){throw 'Stage8 crisis/normal timing/count/types changed'}}
}
if($taskChanged.Count -ne 2 -or @($taskChanged|Where-Object stage -ne 8).Count){throw 'Unrelated operation changed'}
if(($taskScopeFinal.rows|Where-Object {-not $_.crisis}|ForEach-Object {$_.sites.Count}|Measure-Object -Sum).Sum -ne 308){throw 'Total sites differ'}
foreach($taskM in $taskScopeFinal.crisisModels){if($taskM.State -ne 'Won' -or $taskM.Core -ne 300 -or $taskM.Kills -ne 612 -or $taskM.Remaining -ne 0 -or -not $taskM.AllSpawned -or $taskM.Spent -ne 1040){throw 'Selected crisis outcome differs'}}
$taskStats=@(foreach($taskName in $taskNames){$taskD=$taskSets[$taskName];[pscustomobject]@{variant=$taskName;roads=@($taskD.snapshot.roads|ForEach-Object length);meanFired=($taskD.models|Measure-Object FiredSites -Average).Average;meanFrequent=($taskD.models|Measure-Object FrequentFiringSites -Average).Average;maxRecent3=($taskD.models|Measure-Object PeakActive3Seconds -Maximum).Maximum;meanIdleSeconds=($taskD.models|Measure-Object IdleSeconds -Average).Average;meanEnemySeconds=($taskD.models|Measure-Object EnemySeconds -Average).Average;maxEnemies=($taskD.models|Measure-Object PeakEnemies -Maximum).Maximum;clearMin=($taskD.models|Measure-Object Time -Minimum).Minimum;clearMax=($taskD.models|Measure-Object Time -Maximum).Maximum;coreMin=($taskD.models|Measure-Object Core -Minimum).Minimum}})
[pscustomobject]@{created=(Get-Date).ToString('o');tasks=@(179,180,181);analysisProductBase='0.19.2';normalCurrentBaseline=9;historicalMetricPairs=9;newNormalCandidates=36;selectedCrisisBaseline=3;selectedCrisisCandidates=6;staticConstructionSnapshots=84;finalStaticComparisonPairs=28;changedStaticPairs=$taskChanged;totalSites=308;normalScheduleIdenticalExceptLaneForThirdEntrance=$true;stats=$taskStats;selected='three-clear-courts';candidate0VisualFailure='Northy6/Easty4 centerlines2 apart; curbs look joined in actual Android Ready. Eastbend3 gives minimum3 and passes negative/positive geometry checks.';decision='Apply clear-courts as0.20.0 Android acceptance candidate; do not claim released until native battle/save checks finish';limitations=@('Same seed/card/skill and geometry-ranked purchase rules; not identical world positions or purchase times.','Site firing aggregates before/after fusion at a site, not simultaneous firing or individual final-tower effectiveness.','Enemy-seconds/idle/peak are density proxies, not causal enjoyment measurements.','Normal and crisis share geometry and lane allocation; only one selected crisis purchase policy/three seeds tested.');newAndroidAcceptanceComplete=$false}|ConvertTo-Json -Depth 7|Set-Content TestResults/alley-v0200-comparison.json
'PASS historical/current parity;45normal+9crisis model records;28final static pairs;612 kind/timing and308sites preserved. No resimulation.'
