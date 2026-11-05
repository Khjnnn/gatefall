param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskRaw='TestResults/stage13-idle-order-v0207-raw.json'
$taskBasePath='TestResults/stage13-breadth-fusion-v0207-raw.json'
$taskBase=Get-Content $taskBasePath -Raw|ConvertFrom-Json
if(-not $UseRecorded){
    if(Test-Path $taskRaw){throw 'Preserve recorded pilot; use -UseRecorded'}
    foreach($taskR in (Get-Content TestResults/source-hashes-v0207.json -Raw|ConvertFrom-Json)){
        if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Current product differs from recorded baseline'}
    }
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'Tests/Stage13MarkerPositionReview.cs'))
    $taskActions=@(for($taskI=0;$taskI -lt $taskBase.actions.Count;$taskI++){
        $taskA=$taskBase.actions[$taskI];$taskSite=[int]$taskA.Site
        if($taskI -ge 11){if($taskSite -eq 3){$taskSite=22}elseif($taskSite -eq 22){$taskSite=3}}
        [Gatefall.Tests.Stage13MarkerPositionReview+Action]@{Time=$taskA.Time;Command=$taskA.Command;Kind=[Gatefall.Core.TowerKind][int]$taskA.Kind;Site=$taskSite}
    })
    $taskRows=@(foreach($taskSeed in @(7,41)){
        [ordered]@{seed=$taskSeed;battle=[Gatefall.Tests.Stage13MarkerPositionReview]::Run($taskSeed,14,$taskActions)}
    })
    [ordered]@{created=(Get-Date -Format o);task=220;version='0.20.7';baselinePath=$taskBasePath;baselineSha256=(Get-FileHash $taskBasePath).Hash.ToLowerInvariant();newModels=2;swapSites=@(3,22);sharedPrefixPurchases=11;actions=$taskActions;rows=$taskRows;productChanged=$false;newAndroid=$false}|ConvertTo-Json -Depth 12|Set-Content $taskRaw
}
$taskData=Get-Content $taskRaw -Raw|ConvertFrom-Json
if((Get-FileHash $taskBasePath).Hash -ne $taskData.baselineSha256){throw 'Recorded baseline changed'}
$taskIdle=@(foreach($taskB in $taskBase.rows){
    $taskShots=$taskB.battle.Observation.SiteShots
    $taskUnused=@(for($taskSite=0;$taskSite -lt 24;$taskSite++){if($taskShots[$taskSite] -eq 0){$taskSite}})
    $taskOrders=@(foreach($taskSite in $taskUnused){
        $taskPurchase=@($taskB.battle.Purchases|Where-Object {$_ -match (' build Autocannon @'+$taskSite+'$')}|Select-Object -Last 1)
        [pscustomobject]@{site=$taskSite;receipt=$(if($taskPurchase.Count){$taskPurchase[0]}else{$null});lifetimeShots=0}
    })
    [ordered]@{seed=$taskB.seed;zeroLifetimeSiteShots=$taskUnused;finalSpearBuildsAtZeroSites=$taskOrders;scope='Site lifetime includes consumed Binding16; no direct wasted-damage or refundable-loss claim'}
})
$taskRows=@(foreach($taskR in $taskData.rows){
    $taskA=$taskR.battle;$taskB=($taskBase.rows|Where-Object seed -eq $taskR.seed).battle
    if($taskA.Spent -ne 1145 -or $taskA.Towers -ne 24 -or $taskA.CompletedActions -ne 28 -or
        (($taskA.Cards|ConvertTo-Json -Compress) -cne ($taskB.Cards|ConvertTo-Json -Compress))){throw 'Candidate policy/card/budget differs'}
    [ordered]@{seed=$taskR.seed;state=$taskA.State;core=$taskA.Core;time=$taskA.Time;spent=$taskA.Spent;credits=$taskA.Credits;towers=$taskA.Towers;delay=$taskA.MaximumPurchaseDelay;
        fired=$taskA.Observation.FiredSites;frequent=$taskA.Observation.FrequentFiringSites;recent3=$taskA.Observation.PeakActive3Seconds;shots=($taskA.Observation.SiteShots|Measure-Object -Sum).Sum;peakAlive=$taskA.Observation.PeakAlive;
        oldTime=$taskB.Time;oldFired=$taskB.Observation.FiredSites;oldFrequent=$taskB.Observation.FrequentFiringSites;oldRecent3=$taskB.Observation.PeakActive3Seconds;oldShots=($taskB.Observation.SiteShots|Measure-Object -Sum).Sum;oldPeakAlive=$taskB.Observation.PeakAlive;
        site3Shots=$taskA.Observation.SiteShots[3];site22Shots=$taskA.Observation.SiteShots[22];oldSite3Shots=$taskB.Observation.SiteShots[3];oldSite22Shots=$taskB.Observation.SiteShots[22];kindDamage=$taskA.KindDamage;oldKindDamage=$taskB.KindDamage;siteShots=$taskA.Observation.SiteShots}
})
[ordered]@{created=(Get-Date -Format o);task=220;newModelsRecorded=2;summaryNewModels=0;baseline3Reused=$true;idle=$taskIdle;rows=$taskRows;
    scope='Only post-fusion spear3/22 build order swapped; fixed target times, actual purchase delay may differ; not Native exact replay or universal placement advice';productChanged=$false;newApk=$false;newAvd=$false}|ConvertTo-Json -Depth 10|Set-Content TestResults/stage13-idle-order-v0207-comparison.json
$taskRows|ForEach-Object {[pscustomobject]$_}|Select-Object seed,state,core,time,fired,oldFired,frequent,oldFrequent,recent3,oldRecent3,shots,oldShots|Format-Table -AutoSize
