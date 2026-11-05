param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOut='TestResults/stage13-breadth-fusion-v0207-raw.json'
if(-not $UseRecorded){
    if(Test-Path $taskOut){throw 'Preserve breadth/fusion run; use -UseRecorded'}
    foreach($taskR in (Get-Content TestResults/source-hashes-v0207.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed'}}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'Tests/Stage13MarkerPositionReview.cs')+(Join-Path (Get-Location) 'Tests/TowerDensityReview.cs'))
    $taskNative=@(Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json)
    $taskActions=@(foreach($taskA in ($taskNative|Select-Object -First 11)){
        [Gatefall.Tests.Stage13MarkerPositionReview+Action]@{Time=$taskA.time;Command=$taskA.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskA.kind);Site=$taskA.site}
    })
    $taskSim=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[12];Loadout=263;MarkerUnlocked=$true;ObserverUnlocked=$false})
    $taskOrder=[Collections.Generic.List[int]]::new();foreach($taskA in ($taskActions|Where-Object Command -eq 'build')){if($taskA.Site -ne 16){$taskOrder.Add($taskA.Site)}}
    if($taskOrder.Count -ne 7 -or $taskActions[-1].Command -ne 'fusion'){throw 'Shared465/7 with Arc fixture differs'}
    foreach($taskA in ($taskNative|Select-Object -Skip 11|Where-Object action -eq 'build')){if(-not $taskOrder.Contains($taskA.site)){$taskOrder.Add($taskA.site)}}
    $taskMethod=[Gatefall.Tests.TowerDensityReview].GetMethod('Order',[Reflection.BindingFlags]'NonPublic,Static')
    foreach($taskSlot in $taskMethod.Invoke($null,@($taskSim))){if(-not $taskOrder.Contains($taskSlot)){$taskOrder.Add($taskSlot)}}
    if($taskOrder.Count -ne 24){throw '24 existing sites required'}
    $taskTimes=@(($taskNative|Select-Object -Skip 11|ForEach-Object time))+@(160.2,162,164,166)
    if($taskTimes.Count -ne 17){throw 'Seventeen new build times required'}
    for($taskI=0;$taskI -lt 17;$taskI++){$taskActions+=[Gatefall.Tests.Stage13MarkerPositionReview+Action]@{Time=$taskTimes[$taskI];Command='build';Kind=[Gatefall.Core.TowerKind]::Autocannon;Site=$taskOrder[7+$taskI]}}
    $taskRows=@(foreach($taskSeed in @(7,19,41)){
        $taskBattle=[Gatefall.Tests.Stage13MarkerPositionReview]::Run($taskSeed,14,$taskActions)
        if($taskBattle.Spent -gt 1145 -or $taskBattle.Towers -gt 24 -or $taskBattle.Spawned -gt 928){throw 'Breadth/fusion cap exceeded'}
        [ordered]@{seed=$taskSeed;policy='breadth24-plus-arc';battle=$taskBattle}
    })
    [ordered]@{created=(Get-Date -Format o);task=218;version='0.20.7';newModels=3;plannedSpend=1145;differenceFrom1050=95;plannedFinalTowers=24;plannedBuilds=25;plannedUpgrades=2;plannedFusions=1;slotOrder=$taskOrder.ToArray();actions=$taskActions;rows=$taskRows;productChanged=$false;newAndroid=$false}|ConvertTo-Json -Depth 12|Set-Content $taskOut
}
$taskData=Get-Content $taskOut -Raw|ConvertFrom-Json
$taskBase16=@((Get-Content TestResults/stage13-marker-v0204-models.json -Raw|ConvertFrom-Json).rows|Where-Object MarkerSite -eq 14)
$taskBase24=@((Get-Content TestResults/stage13-breadth-v0207-raw.json -Raw|ConvertFrom-Json).rows)
$taskRows=@(foreach($taskR in $taskData.rows){$taskA=$taskR.battle;$taskB=$taskBase16|Where-Object Seed -eq $taskR.seed;$taskC=($taskBase24|Where-Object seed -eq $taskR.seed).battle
    [ordered]@{seed=$taskR.seed;state=$taskA.State;core=$taskA.Core;time=$taskA.Time;spent=$taskA.Spent;credits=$taskA.Credits;towers=$taskA.Towers;completedActions=$taskA.CompletedActions;purchaseDelay=$taskA.MaximumPurchaseDelay;arcShots=$taskA.Observation.KindShots[3];arcDamage=$taskA.KindDamage[3];siteFired=$taskA.Observation.FiredSites;frequent=$taskA.Observation.FrequentFiringSites;recent3Peak=$taskA.Observation.PeakActive3Seconds;peakAlive=$taskA.Observation.PeakAlive;shots=($taskA.Observation.SiteShots|Measure-Object -Sum).Sum;sameCards16=(($taskA.Cards|ConvertTo-Json -Compress) -ceq ($taskB.Cards|ConvertTo-Json -Compress));sameCards24=(($taskA.Cards|ConvertTo-Json -Compress) -ceq ($taskC.Cards|ConvertTo-Json -Compress));baseline16Time=$taskB.Time;baseline24Time=$taskC.Time;baseline16Shots=($taskB.Observation.SiteShots|Measure-Object -Sum).Sum;baseline24Shots=($taskC.Observation.SiteShots|Measure-Object -Sum).Sum;siteShots=$taskA.Observation.SiteShots}
})
[ordered]@{created=(Get-Date -Format o);task=218;newModelsRecorded=3;summaryNewModels=0;baseline16Reused=3;baseline24Reused=3;rows=$taskRows;scope='Budget1145 differs by95 from both1050 baselines; purchase kinds/times and materials differ; lifetime site fire includes consumed materials';productChanged=$false;newApk=$false;newAvd=$false}|ConvertTo-Json -Depth 8|Set-Content TestResults/stage13-breadth-fusion-v0207-comparison.json
$taskRows|ForEach-Object {[pscustomobject]$_}|Select-Object seed,state,core,time,spent,credits,towers,completedActions,arcShots,arcDamage,siteFired,recent3Peak|Format-Table -AutoSize
