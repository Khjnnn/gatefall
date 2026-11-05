param([ValidateSet(11,12)][int]$Stage=12)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskPrefix=if($Stage -eq 12){'west-crossing'}else{'marker-courts'}
$taskVariant=if($Stage -eq 12){'north-flank'}else{'east-return'}
$taskBase=Get-Content "TestResults/$taskPrefix-v0201-baseline.json" -Raw|ConvertFrom-Json
$taskCandidate=Get-Content "TestResults/$taskPrefix-v0201-$taskVariant.json" -Raw|ConvertFrom-Json
$taskHistorical=Get-Content TestResults/tower-density-v0170.json -Raw|ConvertFrom-Json
foreach($taskFile in (Get-Content TestResults/source-hashes-v0201.json -Raw|ConvertFrom-Json)){if((Get-FileHash -LiteralPath $taskFile.path).Hash.ToLowerInvariant() -ne $taskFile.sha256){throw 'Product source changed during analysis'}}
$taskRows=@(foreach($taskB in $taskBase.models){
    $taskMatches=@($taskCandidate.models|Where-Object {$_.Seed -eq $taskB.Seed -and $_.Policy -eq $taskB.Policy})
    $taskOldMatches=@($taskHistorical.models|Where-Object {$_.Stage -eq $Stage -and $_.Seed -eq $taskB.Seed -and $_.Policy -eq $taskB.Policy})
    if($taskMatches.Count -ne 1 -or $taskOldMatches.Count -ne 1){throw 'Missing unique pair'}
    $taskC=$taskMatches[0];$taskOld=$taskOldMatches[0]
    foreach($taskProp in @('Sites','StartingCredits','Towers','Spent','Kills','Fusions','FiredSites','FrequentFiringSites','PeakActive3Seconds','PeakProjectiles','State','Core')){if($taskB.$taskProp -ne $taskOld.$taskProp){throw "Historical baseline differs: $taskProp"}}
    if([math]::Abs($taskB.Time-$taskOld.Time) -gt .0001){throw 'Historical time differs'}
    [pscustomobject]@{seed=$taskB.Seed;policy=$taskB.Policy;baselineFired=$taskB.FiredSites;candidateFired=$taskC.FiredSites;baselineFrequent=$taskB.FrequentFiringSites;candidateFrequent=$taskC.FrequentFiringSites;baselineRecent=$taskB.PeakActive3Seconds;candidateRecent=$taskC.PeakActive3Seconds;baselineTime=$taskB.Time;candidateTime=$taskC.Time;baselineSpent=$taskB.Spent;candidateSpent=$taskC.Spent;baselineCredits=$taskB.Credits;candidateCredits=$taskC.Credits;baselineIdle=$taskB.IdleSeconds;candidateIdle=$taskC.IdleSeconds;baselinePeak=$taskB.PeakEnemies;candidatePeak=$taskC.PeakEnemies;baselineTowers=$taskB.Towers;candidateTowers=$taskC.Towers;baselineCore=$taskB.Core;candidateCore=$taskC.Core;baselineKills=$taskB.Kills;candidateKills=$taskC.Kills;state=$taskC.State}
})
$taskChanged=@(for($taskI=0;$taskI -lt 28;$taskI++){
    $taskB=$taskBase.staticConstruction28[$taskI];$taskC=$taskCandidate.staticConstruction28[$taskI]
    if(($taskB|ConvertTo-Json -Depth 8 -Compress) -cne ($taskC|ConvertTo-Json -Depth 8 -Compress)){
        if($taskB.stage -ne $Stage){throw 'Unrelated static change'}
        foreach($taskProp in @('schedule','startingCredits','supply','scheduled','lanes')){if(($taskB.$taskProp|ConvertTo-Json -Depth 6 -Compress) -cne ($taskC.$taskProp|ConvertTo-Json -Depth 6 -Compress)){throw "Scope changed: $taskProp"}}
        [pscustomobject]@{stage=$taskB.stage;crisis=$taskB.crisis}
    }
})
if($taskRows.Count -ne 9 -or $taskChanged.Count -ne 2){throw 'Comparison scope differs'}
$taskAggregates=[ordered]@{}
foreach($taskMetric in @('Fired','Frequent','Time','Spent','Credits','Idle','Peak')){
    $taskAggregates[$taskMetric]=[pscustomobject]@{baseline=($taskRows|Measure-Object ("baseline"+$taskMetric) -Average).Average;candidate=($taskRows|Measure-Object ("candidate"+$taskMetric) -Average).Average}
}
if($taskAggregates.Fired.candidate -ge $taskAggregates.Fired.baseline -and $taskAggregates.Frequent.candidate -ge $taskAggregates.Frequent.baseline){throw 'Reassess rejection; participation is no longer lower'}
[pscustomobject]@{created=(Get-Date).ToString('o');stage=$Stage;candidate=$taskVariant;newNormalModels=18;historicalBaseline9Parity=$true;staticPairs=28;changedStaticPairs=$taskChanged;crisisBattleModels=0;adopted=$false;reason='Participation fell under the selected purchase policies; density or shape novelty alone does not justify adoption.';aggregates=$taskAggregates;rows=$taskRows;productChanged=$false;newApk=$false;newAvd=$false;scope='Same geometry-ranked purchasing policy, not fixed world sites/times. No human fun or global optimum claim.'}|ConvertTo-Json -Depth 8|Set-Content "TestResults/$taskPrefix-v0201-comparison.json"
[pscustomobject]@{stage=$Stage;adopted=$false;firedBefore=$taskAggregates.Fired.baseline;firedAfter=$taskAggregates.Fired.candidate;frequentBefore=$taskAggregates.Frequent.baseline;frequentAfter=$taskAggregates.Frequent.candidate;idleBefore=$taskAggregates.Idle.baseline;idleAfter=$taskAggregates.Idle.candidate;spentBefore=$taskAggregates.Spent.baseline;spentAfter=$taskAggregates.Spent.candidate}|Format-List
