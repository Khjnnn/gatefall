$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot='TestResults/pulse-v0190-review'
foreach($taskRow in (Get-Content "$taskRoot/source-hashes-v0185.json" -Raw|ConvertFrom-Json)){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product changed before pulse selection'}}
$taskPrior=@((Get-Content TestResults/tower-density-v0185-stage9-baseline.json -Raw|ConvertFrom-Json).models)
$taskRuns=@{}
$taskTiming=@{}
foreach($taskName in @('baseline','window16','window12')) {
    $taskRuns[$taskName]=Get-Content "TestResults/pulse-v0190-$taskName.json" -Raw|ConvertFrom-Json
    $taskTiming[$taskName]=Get-Content "TestResults/pulse-v0190-$taskName-timing-samples.json" -Raw|ConvertFrom-Json
}
$taskCommon=@('Stage','Seed','Policy','Sites','StartingCredits','Towers','Spent','Kills','Fusions','FiredSites','FrequentFiringSites','PeakActive3Seconds','PeakProjectiles','State','Core')
foreach($taskName in @('baseline','window16','window12')) {
    $taskExpected=if($taskName -eq 'baseline'){3}else{9}
    if($taskRuns[$taskName].models.Count -ne $taskExpected -or $taskTiming[$taskName].models.Count -ne 3){throw 'Incomplete pulse observations'}
    foreach($taskSample in $taskTiming[$taskName].models) {
        $taskRow=@($taskRuns[$taskName].models|Where-Object {$_.Seed -eq $taskSample.Seed -and $_.Policy -eq $taskSample.Policy})[0]
        foreach($taskKey in $taskCommon+@('PeakEnemies','PeakEnemiesLate')){if($taskSample.$taskKey -ne $taskRow.$taskKey){throw "Timing observer changed $taskKey"}}
        if([math]::Abs($taskSample.Time-$taskRow.Time) -gt .0001 -or [math]::Abs($taskSample.LateEnemySeconds-$taskRow.LateEnemySeconds) -gt .001 -or ($taskSample.PurchaseLog -join '|') -ne ($taskRow.PurchaseLog -join '|')){throw 'Timing observer changed outcomes/ledger'}
    }
}
foreach($taskRow in $taskRuns['baseline'].models) {
    $taskOld=@($taskPrior|Where-Object {$_.Seed -eq $taskRow.Seed -and $_.Policy -eq $taskRow.Policy})[0]
    foreach($taskKey in $taskCommon){if($taskRow.$taskKey -ne $taskOld.$taskKey){throw "Baseline observer changed $taskKey"}}
    if([math]::Abs($taskRow.Time-$taskOld.Time) -gt .0001){throw 'Baseline observation changed time'}
}
$taskContracts=0
foreach($taskName in @('window16','window12')) {
    $taskWindow=if($taskName -eq 'window16'){16}else{12}
    foreach($taskFile in (Get-ChildItem "$taskRoot/baseline" -Filter '*.cs')){if($taskFile.Name -ne 'RaonCombat.cs' -and (Get-FileHash -LiteralPath "$taskRoot/$taskName/$($taskFile.Name)").Hash -ne (Get-FileHash -LiteralPath $taskFile.FullName).Hash){throw 'Non-schedule core changed'}}
    foreach($taskBefore in $taskRuns['baseline'].contractCases) {
        $taskAfter=@($taskRuns[$taskName].contractCases|Where-Object {$_.stage -eq $taskBefore.stage -and $_.crisis -eq $taskBefore.crisis})[0]
        if(($taskBefore.sockets|ConvertTo-Json -Compress) -ne ($taskAfter.sockets|ConvertTo-Json -Compress) -or $taskBefore.schedule.Count -ne $taskAfter.schedule.Count){throw 'Geometry or group count changed'}
        $taskAllowed=$taskBefore.stage -eq 9 -and -not $taskBefore.crisis
        for($taskI=0;$taskI -lt $taskBefore.schedule.Count;$taskI++) {
            $taskA=$taskBefore.schedule[$taskI];$taskB=$taskAfter.schedule[$taskI]
            foreach($taskKey in @('Wave','Lane','Kind','Count','Swarm','Warned')){if($taskA.$taskKey -ne $taskB.$taskKey){throw "Spawn identity changed: $taskKey"}}
            if(-not $taskAllowed -or $taskA.Wave -lt 3){if($taskA.Start -ne $taskB.Start -or $taskA.Interval -ne $taskB.Interval){throw 'Out-of-scope spawn timing changed'}}
        }
        if($taskAllowed) {
            foreach($taskWave in 0..5) {
                $taskA=@($taskBefore.schedule|Where-Object Wave -eq $taskWave);$taskB=@($taskAfter.schedule|Where-Object Wave -eq $taskWave)
                foreach($taskLane in 0,2){if([math]::Abs((($taskA|Where-Object Lane -eq $taskLane)|Measure-Object Start -Minimum).Minimum-(($taskB|Where-Object Lane -eq $taskLane)|Measure-Object Start -Minimum).Minimum) -gt .000001){throw 'First lane arrival/warning changed'}}
                if($taskWave -ge 3) {
                    $taskLast=($taskB|ForEach-Object {$_.Start+($_.Count-1)*$_.Interval}|Measure-Object -Maximum).Maximum
                    if([math]::Abs($taskLast-($taskWave*30+5)-$taskWindow) -gt .000001){throw 'Late wave window differs'}
                }
            }
        }
        $taskContracts++
    }
}
$taskStats=@()
foreach($taskName in @('baseline','window16','window12')) {
    $taskRows=if($taskName -eq 'baseline'){$taskPrior}else{$taskRuns[$taskName].models}
    foreach($taskRow in $taskRows){if($taskRow.State -ne 'Won' -or $taskRow.Core -ne 300 -or $taskRow.Kills -ne 624 -or $taskRow.Towers -ne 14 -or $taskRow.Sites -ne 24){throw 'Outcome/budget changed'}
        $taskOld=@($taskPrior|Where-Object {$_.Seed -eq $taskRow.Seed -and $_.Policy -eq $taskRow.Policy})[0]
        if($taskOld.Spent -ne $taskRow.Spent -or $taskOld.Fusions -ne $taskRow.Fusions){throw 'Final spend/fusion total differs'}
    }
    $taskStats+=[pscustomobject]@{name=$taskName;uniqueMetricConditions=$taskRows.Count;meanFired=($taskRows|Measure-Object FiredSites -Average).Average;meanFrequent=($taskRows|Measure-Object FrequentFiringSites -Average).Average;peakRecent=($taskRows|Measure-Object PeakActive3Seconds -Maximum).Maximum;peakProjectiles=($taskRows|Measure-Object PeakProjectiles -Maximum).Maximum;minClear=($taskRows|Measure-Object Time -Minimum).Minimum;maxClear=($taskRows|Measure-Object Time -Maximum).Maximum;minimumCore=($taskRows|Measure-Object Core -Minimum).Minimum}
}
$taskPressure=@();$taskLedger=@()
foreach($taskName in @('baseline','window16','window12')) {foreach($taskRow in $taskTiming[$taskName].models) {
    $taskPressure+=[pscustomobject]@{name=$taskName;seed=$taskRow.Seed;policy=$taskRow.Policy;peakAlive=$taskRow.PeakEnemiesLate;enemySecondsLate=$taskRow.LateEnemySeconds;quietSecondsLate=$taskRow.QuietSecondsLate;maxQuietGapLate=$taskRow.MaxQuietGapLate}
    if($taskName -eq 'baseline'){continue}
    $taskOld=@($taskTiming['baseline'].models|Where-Object Policy -eq $taskRow.Policy)[0]
    $taskBeforeActions=@($taskOld.PurchaseLog|ForEach-Object {$_ -replace '^\d+\.\d+ ',''})
    $taskAfterActions=@($taskRow.PurchaseLog|ForEach-Object {$_ -replace '^\d+\.\d+ ',''})
    $taskSameSet=(@($taskBeforeActions|Sort-Object)-join '|') -eq (@($taskAfterActions|Sort-Object)-join '|')
    $taskDeltas=@();$taskDifferences=@()
    foreach($taskA in $taskOld.PurchaseLog) {
        $taskAction=$taskA -replace '^\d+\.\d+ ',''
        $taskMatches=@($taskRow.PurchaseLog|Where-Object {($_ -replace '^\d+\.\d+ ','') -eq $taskAction})
        if($taskMatches.Count -eq 1){$taskDeltas+=[double]($taskMatches[0].Split(' ')[0])-[double]($taskA.Split(' ')[0])}
    }
    for($taskI=0;$taskI -lt [math]::Min($taskBeforeActions.Count,$taskAfterActions.Count);$taskI++){if($taskBeforeActions[$taskI] -ne $taskAfterActions[$taskI]){$taskDifferences+=[pscustomobject]@{index=$taskI;before=$taskOld.PurchaseLog[$taskI];after=$taskRow.PurchaseLog[$taskI]}}}
    $taskLedger+=[pscustomobject]@{name=$taskName;seed=7;policy=$taskRow.Policy;beforeActions=$taskBeforeActions.Count;afterActions=$taskAfterActions.Count;sameActionMultiset=$taskSameSet;sameActionSequence=($taskBeforeActions -join '|') -eq ($taskAfterActions -join '|');differingIndices=$taskDifferences;minMatchingActionTimeDelta=($taskDeltas|Measure-Object -Minimum).Minimum;maxMatchingActionTimeDelta=($taskDeltas|Measure-Object -Maximum).Maximum}
}}
[pscustomobject]@{created=(Get-Date).ToString('o');task=168;productAtSelection='0.18.5';historicalCurrentBaselineReused=9;baselinePressureSamples=3;newCandidateConditions=18;timingOnlyReplays=9;timingObserverParity=$true;normalCrisisContractPairs=$taskContracts;onlyNormalStage9LateTimingChanged=$true;firstLaneArrivalsPreserved=$true;allOutcomeCore=300;allOutcomeKills=624;allTowers=14;pairedFinalSpendPreserved=$true;stats=$taskStats;selectedPressureSeed7Only=$taskPressure;selectedPurchaseTimingSeed7Only=$taskLedger;provisionalCandidate='window12';reason='Higher firing participation and late population with same outcomes; retains measured longer quiet gaps as a tradeoff. Requires native verification before completion.';humanFunTested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json -Depth 8|Set-Content TestResults/pulse-v0190-comparison.json
'PASS: candidate timing/count/geometry scope across56 normal/crisis cases; selected read-only observations/ledgers match; baseline9 reused'
