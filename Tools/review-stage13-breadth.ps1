param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOut='TestResults/stage13-breadth-v0207-raw.json'
$taskPrior=@((Get-Content TestResults/stage13-marker-v0204-models.json -Raw|ConvertFrom-Json).rows|Where-Object MarkerSite -eq 14)
if(-not $UseRecorded){
    if(Test-Path $taskOut){throw 'Preserve breadth results; use -UseRecorded'}
    foreach($taskR in (Get-Content TestResults/source-hashes-v0207.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed'}}
    if(-not (Get-Content TestResults/stage13-call-v0207-raw.json -Raw|ConvertFrom-Json).controlMatchesOutcomeLedgerCardsDamageShots){throw 'Current no-call baseline not verified'}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'Tests/Stage13MarkerPositionReview.cs')+(Join-Path (Get-Location) 'Tests/TowerDensityReview.cs'))
    $taskNative=@(Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json)
    $taskActions=@(foreach($taskA in ($taskNative|Select-Object -First 7)){
        [Gatefall.Tests.Stage13MarkerPositionReview+Action]@{Time=$taskA.time;Command=$taskA.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskA.kind);Site=$taskA.site}
    })
    $taskSim=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[12];Loadout=263;MarkerUnlocked=$true;ObserverUnlocked=$false})
    $taskOrder=[Collections.Generic.List[int]]::new();foreach($taskA in $taskActions){$taskOrder.Add($taskA.Site)}
    foreach($taskA in ($taskNative|Select-Object -Skip 7|Where-Object action -eq 'build')){if(-not $taskOrder.Contains($taskA.site)){$taskOrder.Add($taskA.site)}}
    $taskMethod=[Gatefall.Tests.TowerDensityReview].GetMethod('Order',[Reflection.BindingFlags]'NonPublic,Static')
    foreach($taskSlot in $taskMethod.Invoke($null,@($taskSim))){if(-not $taskOrder.Contains($taskSlot)){$taskOrder.Add($taskSlot)}}
    if($taskOrder.Count -ne 24){throw '24 distinct existing sites required'}
    $taskActions+=[Gatefall.Tests.Stage13MarkerPositionReview+Action]@{Time=31.6;Command='upgrade';Kind=[Gatefall.Core.TowerKind]::Rupture;Site=4}
    $taskTimes=@(($taskNative|Select-Object -Skip 8|ForEach-Object time))+@(160.2)
    if($taskTimes.Count -ne 17){throw 'Seventeen additional build times required'}
    for($taskI=0;$taskI -lt 17;$taskI++){$taskActions+=[Gatefall.Tests.Stage13MarkerPositionReview+Action]@{Time=$taskTimes[$taskI];Command='build';Kind=[Gatefall.Core.TowerKind]::Autocannon;Site=$taskOrder[7+$taskI]}}
    $taskRows=@(foreach($taskSeed in @(7,19,41)){
        $taskBattle=[Gatefall.Tests.Stage13MarkerPositionReview]::Run($taskSeed,14,$taskActions)
        if($taskBattle.Spent -gt 1050 -or $taskBattle.Towers -gt 24 -or $taskBattle.Spawned -gt 928){throw 'Breadth cap exceeded'}
        [ordered]@{seed=$taskSeed;policy='breadth24';battle=$taskBattle}
    })
    [ordered]@{created=(Get-Date -Format o);task=217;version='0.20.7';newModels=3;baselineReused=3;currentNoCallControlReusedFromTask=215;maximumSpend=1050;plannedFinalTowers=24;plannedCost=1050;slotOrder=$taskOrder.ToArray();actions=$taskActions;rows=$taskRows;productChanged=$false;newAndroid=$false}|ConvertTo-Json -Depth 12|Set-Content $taskOut
}
$taskData=Get-Content $taskOut -Raw|ConvertFrom-Json
$taskRows=@(foreach($taskR in $taskData.rows){$taskA=$taskR.battle;$taskB=$taskPrior|Where-Object Seed -eq $taskR.seed
    [ordered]@{seed=$taskR.seed;state=$taskA.State;core=$taskA.Core;baselineCore=$taskB.Core;time=$taskA.Time;baselineTime=$taskB.Time;spent=$taskA.Spent;baselineSpent=$taskB.Spent;credits=$taskA.Credits;towers=$taskA.Towers;baselineTowers=$taskB.Towers;completedActions=$taskA.CompletedActions;purchaseDelay=$taskA.MaximumPurchaseDelay;fired=$taskA.Observation.FiredSites;baselineFired=$taskB.Observation.FiredSites;frequent=$taskA.Observation.FrequentFiringSites;baselineFrequent=$taskB.Observation.FrequentFiringSites;recent3Peak=$taskA.Observation.PeakActive3Seconds;baselineRecent3Peak=$taskB.Observation.PeakActive3Seconds;peakAlive=$taskA.Observation.PeakAlive;baselinePeakAlive=$taskB.Observation.PeakAlive;sameCards=(($taskA.Cards|ConvertTo-Json -Compress) -ceq ($taskB.Cards|ConvertTo-Json -Compress));siteShots=$taskA.Observation.SiteShots}
})
[ordered]@{created=(Get-Date -Format o);task=217;newModelsRecorded=3;summaryNewModels=0;baselineReused=3;rows=$taskRows;scope='Same initial seven towers and final1050 spend cap, but purchase kinds, locations and times differ; not a pure tower-count causal comparison';productChanged=$false;newApk=$false;newAvd=$false}|ConvertTo-Json -Depth 8|Set-Content TestResults/stage13-breadth-v0207-comparison.json
$taskRows|ForEach-Object {[pscustomobject]$_}|Select-Object seed,state,core,time,spent,towers,completedActions,fired,baselineFired,recent3Peak,baselineRecent3Peak|Format-Table -AutoSize
