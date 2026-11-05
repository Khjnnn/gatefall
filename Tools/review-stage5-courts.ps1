param([ValidateSet('baseline','north-hinge','north-run')][string]$Variant='baseline',[switch]$Pilot,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskTag=$Variant+$(if($Pilot){'-pilot'}else{''})
$taskOutput="TestResults/courts-v0207-$taskTag.json"
if($UseRecorded){Get-Content $taskOutput -Raw;exit}
if(Test-Path $taskOutput){throw 'Preserve recorded models; use -UseRecorded'}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0207.json -Raw|ConvertFrom-Json)){
    if((Get-FileHash $taskRow.path).Hash -ne $taskRow.sha256){throw 'Product changed before analysis'}
}
$taskRoot=Join-Path (Get-Location) 'TestResults/courts-v0207-review'
$taskDir=Join-Path $taskRoot $taskTag
if(Test-Path $taskDir){throw 'Preserve isolated source snapshot'}
New-Item -ItemType Directory -Path $taskDir -Force|Out-Null
foreach($taskFile in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskDir $taskFile.Name)}
$taskOriginal=[IO.File]::ReadAllText((Join-Path (Get-Location) 'Assets/Scripts/Core/BattlefieldLayouts.cs')).Replace("`r`n","`n")
$taskBefore=@'
            L("tower-outer-courts",3,
              P(-23,-4,-17,-4,-17,5,-9,5,-9,-3,-3,-3,0,0),none,
              P(23,5,17,5,17,-4,9,-4,9,3,3,3,0,0)),
'@
$taskAfter=@'
            L("tower-offset-north-hinge",3,
              P(-23,4,-18,4,-18,-5,-9,-5,-9,3,0,3,0,0),none,
              P(23,-5,16,-5,16,3,0,3,0,0)),
'@
if($Variant -eq 'north-run'){
    $taskAfter=@'
            L("tower-offset-shared-run",3,
              P(-23,4,-18,4,-18,-5,-9,-5,-9,3,5,3,5,-2,0,0),none,
              P(23,-5,16,-5,16,3,5,3,5,-2,0,0)),
'@
}
if(([regex]::Matches($taskOriginal,[regex]::Escape($taskBefore))).Count -ne 1){throw 'Selected stage5 layout ambiguous'}
if($Variant -ne 'baseline'){
    $taskCandidate=$taskOriginal.Replace($taskBefore,$taskAfter)
    if($taskCandidate.Replace($taskAfter,$taskBefore) -cne $taskOriginal){throw 'Changes outside one layout'}
    [IO.File]::WriteAllText((Join-Path $taskDir 'BattlefieldLayouts.cs'),$taskCandidate)
}
$taskObserver=Join-Path (Get-Location) 'TestResults/alley-v0192-review/AlleyDensityReview.cs'
$taskObserverCopy=Join-Path $taskRoot 'AlleyDensityReview.cs'
if(Test-Path $taskObserverCopy){
    if((Get-FileHash $taskObserverCopy).Hash -ne (Get-FileHash $taskObserver).Hash){throw 'Read-only observer differs'}
}else{Copy-Item -LiteralPath $taskObserver -Destination $taskObserverCopy}
Add-Type -Path @((Get-ChildItem $taskDir -Filter '*.cs').FullName+$taskObserverCopy)
$taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[4]})
if($taskG.BoardSockets.Length -ne 24 -or $taskG.Credits -ne 280){throw 'Selected stage sites/start differs'}
$taskRoads=@(foreach($taskLane in $taskG.OpenLanes){[pscustomobject]@{lane=$taskLane;length=[Gatefall.Core.BattlefieldLayouts]::RoadLength($taskG.Roads[$taskLane]);points=@($taskG.Roads[$taskLane]|Select-Object X,Y)}})
$taskMinRoad=1000.0;$taskMaxRoad=0.0;$taskMinGap=1000.0
for($taskI=0;$taskI -lt $taskG.BoardSockets.Length;$taskI++){
    $taskPoint=$taskG.BoardSockets[$taskI];$taskNear=1000.0
    foreach($taskLane in $taskG.OpenLanes){$taskNear=[math]::Min($taskNear,[Gatefall.Core.BattlefieldLayouts]::RoadDistance($taskPoint,$taskG.Roads[$taskLane]))}
    if($taskNear -lt 2.14 -or $taskNear -gt 3.26){throw 'Invalid clear/reachable site'}
    $taskMinRoad=[math]::Min($taskMinRoad,$taskNear);$taskMaxRoad=[math]::Max($taskMaxRoad,$taskNear)
    for($taskJ=0;$taskJ -lt $taskI;$taskJ++){
        $taskGap=[Gatefall.Core.V2]::Distance($taskPoint,$taskG.BoardSockets[$taskJ]);if($taskGap -lt 2.59){throw 'Overlapping sites'};$taskMinGap=[math]::Min($taskMinGap,$taskGap)
    }
}
$taskStatic=@(foreach($taskStage in 0..13){foreach($taskCrisis in @($false,$true)){
    $taskS=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[$taskStage];Crisis=$taskCrisis})
    [pscustomobject]@{stage=$taskStage+1;crisis=$taskCrisis;sites=@($taskS.BoardSockets|Select-Object X,Y);lanes=$taskS.OpenLanes;roads=@(foreach($taskLane in $taskS.OpenLanes){[pscustomobject]@{lane=$taskLane;points=@($taskS.Roads[$taskLane]|Select-Object X,Y)}});startingCredits=$taskS.Credits;supply=$taskS.WaveSupplyCredits;scheduled=(@($taskS.Schedule)|Measure-Object Count -Sum).Sum;schedule=@($taskS.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm)}
}})
$taskRows=if($Pilot){
    $taskRun=[Gatefall.Tests.AlleyDensityReview].GetMethod('Run',[Reflection.BindingFlags]'NonPublic,Static')
    @(foreach($taskPolicy in @('many','mixed','upgrade')){$taskRun.Invoke($null,@(4,7,$taskPolicy))})
}else{[Gatefall.Tests.AlleyDensityReview]::Models(4)}
if($taskRows.Count -ne $(if($Pilot){3}else{9})){throw 'Expected selected-stage model count differs'}
[ordered]@{created=(Get-Date -Format o);task=221;variant=$Variant;pilot=[bool]$Pilot;newBattleModels=$taskRows.Count;geometryValid=$true;snapshot=[ordered]@{stage=5;layout=[Gatefall.Core.BattlefieldLayouts]::Get($taskG.Rules.Stage).Id;credits=$taskG.Credits;sites=24;scheduled=(@($taskG.Schedule)|Measure-Object Count -Sum).Sum;roads=$taskRoads;minimumRoad=$taskMinRoad;maximumRoad=$taskMaxRoad;minimumGap=$taskMinGap};staticConstruction28=$taskStatic;models=$taskRows;scope='Geometry-ranked purchase policy, first-offer cards and automatic Hunter skill including dodge; world sites/times and spend may differ. Not human fun.'}|ConvertTo-Json -Depth 10|Set-Content $taskOutput
$taskRows|Group-Object Policy|ForEach-Object{[pscustomobject]@{variant=$Variant;policy=$_.Name;wins=@($_.Group|Where-Object State -eq Won).Count;fired=($_.Group|Measure-Object FiredSites -Average).Average;frequent=($_.Group|Measure-Object FrequentFiringSites -Average).Average;recentMax=($_.Group|Measure-Object PeakActive3Seconds -Maximum).Maximum;coreMin=($_.Group|Measure-Object Core -Minimum).Minimum}}|Format-Table -AutoSize
