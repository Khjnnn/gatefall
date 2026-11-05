param([ValidateSet(16,12)][int]$Window=16)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskVariant="window$Window"
$taskOutput="TestResults/marker-pulse-v0201-$taskVariant.json"
if(Test-Path -LiteralPath $taskOutput){throw 'Preserve recorded candidate models'}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0201.json -Raw|ConvertFrom-Json)){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product differs'}}
$taskRoot=Join-Path (Get-Location) 'TestResults/marker-pulse-v0201-review'
$taskDir=Join-Path $taskRoot $taskVariant
if(Test-Path -LiteralPath $taskDir){throw 'Preserve isolated source snapshot'}
New-Item -ItemType Directory -Path $taskDir -Force|Out-Null
foreach($taskFile in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){
    $taskPrior=Join-Path (Get-Location) ('TestResults/marker-courts-v0201-review/baseline/'+$taskFile.Name)
    if((Get-FileHash $taskFile.FullName).Hash -ne (Get-FileHash $taskPrior).Hash){throw 'Stage11 baseline core parity lost'}
    Copy-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskDir $taskFile.Name)
}
$taskPath=Join-Path $taskDir 'RaonCombat.cs'
$taskOriginal=[IO.File]::ReadAllText($taskPath).Replace("`r`n","`n")
$taskTransforms=@(
    @('if(stage.Index==8 && !Rules.Crisis && wave>=3)arrivalWindow=12.0;',"if(stage.Index==8 && !Rules.Crisis && wave>=3)arrivalWindow=12.0;`n                    if(stage.Index==10 && !Rules.Crisis && wave>=3)arrivalWindow=$Window.0;"),
    @('if(stage.Index==8 && !Rules.Crisis && wave>=3) {','if((stage.Index==8 || stage.Index==10) && !Rules.Crisis && wave>=3) {')
)
$taskText=$taskOriginal
foreach($taskT in $taskTransforms){if(([regex]::Matches($taskText,[regex]::Escape($taskT[0]))).Count -ne 1){throw 'Ambiguous pulse insertion'};$taskText=$taskText.Replace($taskT[0],$taskT[1])}
$taskReverse=$taskText
for($taskI=$taskTransforms.Count-1;$taskI -ge 0;$taskI--){$taskReverse=$taskReverse.Replace($taskTransforms[$taskI][1],$taskTransforms[$taskI][0])}
if($taskReverse -cne $taskOriginal){throw 'Unexpected source change'}
[IO.File]::WriteAllText($taskPath,$taskText)
$taskObserver=Join-Path (Get-Location) 'TestResults/marker-courts-v0201-review/AlleyDensityReview.cs'
Add-Type -Path @((Get-ChildItem -LiteralPath $taskDir -Filter '*.cs').FullName+$taskObserver)
$taskStatic=@(foreach($taskStage in 0..13){foreach($taskCrisis in @($false,$true)){
    $taskS=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[$taskStage];Crisis=$taskCrisis})
    [pscustomobject]@{stage=$taskStage+1;crisis=$taskCrisis;sites=@($taskS.BoardSockets|Select-Object X,Y);lanes=$taskS.OpenLanes;roads=@(foreach($taskLane in $taskS.OpenLanes){[pscustomobject]@{lane=$taskLane;points=@($taskS.Roads[$taskLane]|Select-Object X,Y)}});startingCredits=$taskS.Credits;supply=$taskS.WaveSupplyCredits;scheduled=(@($taskS.Schedule)|Measure-Object Count -Sum).Sum;schedule=@($taskS.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm)}
}})
$taskBase=Get-Content TestResults/marker-courts-v0201-baseline.json -Raw|ConvertFrom-Json
$taskCellsChanged=0;$taskChangedPairs=0
for($taskI=0;$taskI -lt 28;$taskI++){
    $taskB=$taskBase.staticConstruction28[$taskI];$taskC=$taskStatic[$taskI]
    if($taskB.stage -ne 11 -or $taskB.crisis){if(($taskB|ConvertTo-Json -Depth 8 -Compress) -cne ($taskC|ConvertTo-Json -Depth 8 -Compress)){throw 'Unrelated stage/crisis changed'};continue}
    $taskChangedPairs++
    foreach($taskProp in @('sites','roads','lanes','startingCredits','supply','scheduled')){if(($taskB.$taskProp|ConvertTo-Json -Depth 7 -Compress) -cne ($taskC.$taskProp|ConvertTo-Json -Depth 7 -Compress)){throw 'Geometry/economy/count changed'}}
    if($taskB.schedule.Count -ne $taskC.schedule.Count){throw 'Formation count differs'}
    for($taskJ=0;$taskJ -lt $taskB.schedule.Count;$taskJ++){
        $taskX=$taskB.schedule[$taskJ];$taskY=$taskC.schedule[$taskJ]
        foreach($taskProp in @('Wave','Lane','Kind','Count','Swarm')){if($taskX.$taskProp -ne $taskY.$taskProp){throw 'Type/lane/count differs'}}
        if($taskX.Wave -lt 3){if(($taskX|ConvertTo-Json -Compress) -cne ($taskY|ConvertTo-Json -Compress)){throw 'First3 waves changed'}}
        elseif($taskX.Start -ne $taskY.Start -or $taskX.Interval -ne $taskY.Interval){$taskCellsChanged++}
    }
    foreach($taskWave in 3..5){foreach($taskLane in $taskB.lanes){
        $taskFirstB=($taskB.schedule|Where-Object {$_.Wave -eq $taskWave -and $_.Lane -eq $taskLane}|Measure-Object Start -Minimum).Minimum
        $taskFirstC=($taskC.schedule|Where-Object {$_.Wave -eq $taskWave -and $_.Lane -eq $taskLane}|Measure-Object Start -Minimum).Minimum
        if([math]::Abs($taskFirstB-$taskFirstC) -gt .000001){throw 'First lane warning/spawn moved'}
    }}
}
if($taskChangedPairs -ne 1 -or $taskCellsChanged -lt 1){throw 'Missing bounded change'}
$taskRows=[Gatefall.Tests.AlleyDensityReview]::Models(10)
[pscustomobject]@{created=(Get-Date).ToString('o');variant=$taskVariant;newNormalModels=9;baseline9Reused=$true;staticPairs=28;changedPairs=1;changedScheduleCells=$taskCellsChanged;firstThreeWavesAndFirstLaneStartsPreserved=$true;geometryEconomyTypesCountsPreserved=$true;staticConstruction28=$taskStatic;models=$taskRows;scope='Current standard three purchase policies/first-offer cards/automatic Hunter skill; no Marker equipped by these baseline policies, so not a marker investment test. No fixed purchase-time or human-fun claim.'}|ConvertTo-Json -Depth 9|Set-Content $taskOutput
$taskRows|Group-Object Policy|ForEach-Object{[pscustomobject]@{window=$Window;policy=$_.Name;wins=@($_.Group|Where-Object State -eq Won).Count;meanFired=[math]::Round(($_.Group|Measure-Object FiredSites -Average).Average,2);meanFrequent=[math]::Round(($_.Group|Measure-Object FrequentFiringSites -Average).Average,2);idle=[math]::Round(($_.Group|Measure-Object IdleSeconds -Average).Average,2);minimumCore=($_.Group|Measure-Object Core -Minimum).Minimum;peakEnemies=($_.Group|Measure-Object PeakEnemies -Maximum).Maximum}}|Format-Table
