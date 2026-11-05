$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot='TestResults/distribution-v0190-review'
foreach($taskRow in (Get-Content "$taskRoot/source-hashes-v0185.json" -Raw|ConvertFrom-Json)) {
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Product changed: $($taskRow.path)"}
}
foreach($taskFile in (Get-ChildItem Assets/Scripts/Core -Filter '*.cs')) {
    if((Get-FileHash -LiteralPath $taskFile.FullName).Hash -ne (Get-FileHash -LiteralPath "$taskRoot/baseline/$($taskFile.Name)").Hash){throw 'Current/baseline core mismatch'}
    if($taskFile.Name -ne 'BattlefieldLayouts.cs' -and (Get-FileHash -LiteralPath "$taskRoot/north-merge/$($taskFile.Name)").Hash -ne (Get-FileHash -LiteralPath "$taskRoot/baseline/$($taskFile.Name)").Hash){throw 'Candidate changed a non-layout core'}
}
if((Get-FileHash Tests/TowerDensityReview.cs).Hash -ne (Get-FileHash "$taskRoot/TowerDensityReview.cs").Hash){throw 'Purchase policy changed during review'}
$taskOld=@((Get-Content TestResults/tower-density-v0170.json -Raw|ConvertFrom-Json).models|Where-Object Stage -eq 9)
$taskBase=@((Get-Content TestResults/tower-density-v0185-stage9-baseline.json -Raw|ConvertFrom-Json).models)
$taskNew=@((Get-Content TestResults/tower-density-v0190-stage9-north-merge.json -Raw|ConvertFrom-Json).models)
if($taskOld.Count -ne 9 -or $taskBase.Count -ne 9 -or $taskNew.Count -ne 9){throw 'Incomplete selected-stage models'}
$taskKeys=@('Sites','StartingCredits','Towers','Spent','Kills','Fusions','FiredSites','FrequentFiringSites','PeakActive3Seconds','PeakProjectiles','State','Core')
$taskPairs=@()
foreach($taskBefore in $taskBase) {
    $taskHistorical=@($taskOld|Where-Object {$_.Seed -eq $taskBefore.Seed -and $_.Policy -eq $taskBefore.Policy})
    $taskAfter=@($taskNew|Where-Object {$_.Seed -eq $taskBefore.Seed -and $_.Policy -eq $taskBefore.Policy})
    if($taskHistorical.Count -ne 1 -or $taskAfter.Count -ne 1){throw 'Missing unique seed/policy pair'}
    foreach($taskKey in $taskKeys){if($taskHistorical[0].$taskKey -ne $taskBefore.$taskKey){throw "Historical baseline changed: $taskKey"}}
    if([math]::Abs($taskHistorical[0].Time-$taskBefore.Time) -gt .0001){throw 'Historical baseline time changed'}
    $taskAfter=$taskAfter[0]
    foreach($taskRow in @($taskBefore,$taskAfter)){if($taskRow.Kills -ne 624 -or $taskRow.Towers -ne 14 -or $taskRow.Sites -ne 24 -or $taskRow.StartingCredits -ne 280 -or $taskRow.State -ne 'Won' -or $taskRow.Core -ne 300){throw 'Changed budget/outcome'} }
    if($taskBefore.Spent -ne $taskAfter.Spent -or $taskBefore.Fusions -ne $taskAfter.Fusions){throw 'Purchase totals differ'}
    $taskPairs+=[pscustomobject]@{seed=$taskBefore.Seed;policy=$taskBefore.Policy;spent=$taskBefore.Spent;fusions=$taskBefore.Fusions;beforeFired=$taskBefore.FiredSites;afterFired=$taskAfter.FiredSites;beforeFrequent=$taskBefore.FrequentFiringSites;afterFrequent=$taskAfter.FrequentFiringSites;beforePeakRecent=$taskBefore.PeakActive3Seconds;afterPeakRecent=$taskAfter.PeakActive3Seconds;beforeClear=$taskBefore.Time;afterClear=$taskAfter.Time;clearDelta=$taskAfter.Time-$taskBefore.Time}
}
$taskMaps=@{}
foreach($taskTag in @('baseline','north-merge')){$taskMaps[$taskTag]=Get-Content "TestResults/distribution-v0190-$taskTag-map.json" -Raw|ConvertFrom-Json}
if(($taskMaps['baseline'].schedule|ConvertTo-Json -Depth 5 -Compress) -ne ($taskMaps['north-merge'].schedule|ConvertTo-Json -Depth 5 -Compress)){throw 'Spawn times/counts/kinds/lanes changed'}
$taskStats=@(foreach($taskSet in @(@{name='baseline';rows=$taskBase},@{name='north-merge';rows=$taskNew})) {
    [pscustomobject]@{name=$taskSet.name;conditions=$taskSet.rows.Count;meanFired=($taskSet.rows|Measure-Object FiredSites -Average).Average;meanFrequent=($taskSet.rows|Measure-Object FrequentFiringSites -Average).Average;peakRecent=($taskSet.rows|Measure-Object PeakActive3Seconds -Maximum).Maximum;minClear=($taskSet.rows|Measure-Object Time -Minimum).Minimum;maxClear=($taskSet.rows|Measure-Object Time -Maximum).Maximum;minimumCore=($taskSet.rows|Measure-Object Core -Minimum).Minimum}
})
if($taskStats[1].meanFired -ge $taskStats[0].meanFired -or $taskStats[1].meanFrequent -ge $taskStats[0].meanFrequent){throw 'Review decision needs updating'}
[pscustomobject]@{created=(Get-Date).ToString('o');task=167;productVersion='0.18.5';productUnchanged=$true;newApk=$false;newAvd=$false;currentBaselineModels=9;historicalNineMetricsMatch=$true;candidateModels=9;onlySelectedStage=9;policy='same purchase rules/seeds/card/skill policy; geometric ordering adapts placement to each layout, not identical world positions or purchase timestamps';identicalFullSpawnSchedule=$true;stats=$taskStats;pairs=$taskPairs;southSpine=[pscustomobject]@{geometryRejected=$true;reason='Insufficient clear tower sites in stage 9';modelConditions=0;recordedFromEarlierToolOutput=$true};northMerge=[pscustomobject]@{geometryValid=$true;roads=@($taskMaps['north-merge'].roads|ForEach-Object {$_.length});sites=24;minimumGap=$taskMaps['north-merge'].minimumGap;minimumRoad=$taskMaps['north-merge'].minimumRoad;maximumRoad=$taskMaps['north-merge'].maximumRoad;decision='rejected: fewer participating/frequent firing sites and longer cleanup; isolated maximum8 does not justify adoption'};decision='retain current product';humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false;nextHypothesis='At unchanged baseline geometry and624 total, test one small later-wave swarm timing concentration separately; do not conflate it with a map change or add systems.'}|ConvertTo-Json -Depth 8|Set-Content TestResults/distribution-v0190-comparison.json
$taskApk=Get-Content TestResults/android-apk-v0185.json -Raw|ConvertFrom-Json
foreach($taskFile in @('Builds/Android/Gatefall-v0.18.5.apk','Builds/Android/Gatefall.apk')){if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256){throw 'APK changed during analysis'}}
'PASS: selected stage9 baseline9 matches old9; candidate9 schedule/spend parity; both candidates rejected; product/APK unchanged'
