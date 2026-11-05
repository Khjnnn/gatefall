param([ValidateSet('baseline','north-flank','east-return')][string]$Variant='baseline',[ValidateSet(11,12)][int]$Stage=12,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskPrefix=if($Stage -eq 12){'west-crossing'}else{'marker-courts'}
if(($Variant -eq 'north-flank' -and $Stage -ne 12) -or ($Variant -eq 'east-return' -and $Stage -ne 11)){throw 'Candidate/stage mismatch'}
$taskOutput="TestResults/$taskPrefix-v0201-$Variant.json"
if($UseRecorded){Get-Content -LiteralPath $taskOutput -Raw;exit}
if(Test-Path -LiteralPath $taskOutput){throw 'Preserve recorded models; use -UseRecorded'}
$taskManifest=Get-Content TestResults/source-hashes-v0201.json -Raw|ConvertFrom-Json
foreach($taskRow in $taskManifest){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product differs before analysis'}}
$taskRoot=Join-Path (Get-Location) "TestResults/$taskPrefix-v0201-review"
$taskDir=Join-Path $taskRoot $Variant
if(Test-Path -LiteralPath $taskDir){throw 'Preserve isolated source snapshot'}
New-Item -ItemType Directory -Path $taskDir -Force|Out-Null
foreach($taskFile in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskDir $taskFile.Name)}
$taskOriginal=[IO.File]::ReadAllText((Join-Path (Get-Location) 'Assets/Scripts/Core/BattlefieldLayouts.cs')).Replace("`r`n","`n")
$taskBefore=@'
            L("west-crossing-long-flank",0,
              P(-23,-3,-18,-3,-18,4,-10,4,-10,-3,-4,-3,0,0),
              P(-2,11,-2,7,7,7,7,-2,2,-2,0,0),none),
'@
$taskAfter=@'
            L("west-crossing-north-flank",0,
              P(-23,-3,-18,-3,-18,4,-10,4,-10,-3,-4,-3,0,0),
              P(-2,11,-2,7,14,7,14,-2,2,-2,0,0),none),
'@
if($Stage -eq 11){
    $taskBefore=@'
            L("marker-twin-u-courts",3,
              P(-23,4,-16,4,-16,-4,-7,-4,-7,2,-2,2,0,0),none,
              P(23,4,16,4,16,-4,7,-4,7,2,2,2,0,0)),
'@
    $taskAfter=@'
            L("marker-offset-return-courts",3,
              P(-23,4,-16,4,-16,-4,-7,-4,-7,2,-2,2,0,0),none,
              P(23,-6,15,-6,15,3,8,3,8,-3,3,-3,0,0)),
'@
}
if(([regex]::Matches($taskOriginal,[regex]::Escape($taskBefore))).Count -ne 1){throw 'Selected stage source ambiguous'}
if($Variant -ne 'baseline'){
    $taskCandidate=$taskOriginal.Replace($taskBefore,$taskAfter)
    if($taskCandidate.Replace($taskAfter,$taskBefore) -cne $taskOriginal){throw 'Candidate changes more than one layout'}
    [IO.File]::WriteAllText((Join-Path $taskDir 'BattlefieldLayouts.cs'),$taskCandidate)
}
# Reuse the previously reverse-verified read-only observer, with its original purchase/skill policy.
$taskObserver=Join-Path (Get-Location) 'TestResults/alley-v0192-review/AlleyDensityReview.cs'
$taskObserverCopy=Join-Path $taskRoot 'AlleyDensityReview.cs'
if(Test-Path -LiteralPath $taskObserverCopy){if((Get-FileHash $taskObserverCopy).Hash -ne (Get-FileHash $taskObserver).Hash){throw 'Observer differs'}}else{Copy-Item -LiteralPath $taskObserver -Destination $taskObserverCopy}
Add-Type -Path @((Get-ChildItem -LiteralPath $taskDir -Filter '*.cs').FullName+$taskObserverCopy)
$taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[$Stage-1]})
$taskRoads=@(foreach($taskLane in $taskG.OpenLanes){[pscustomobject]@{lane=$taskLane;length=[Gatefall.Core.BattlefieldLayouts]::RoadLength($taskG.Roads[$taskLane]);points=@($taskG.Roads[$taskLane]|Select-Object X,Y)}})
$taskMinRoad=1000.0;$taskMaxRoad=0.0;$taskMinGap=1000.0
for($taskI=0;$taskI -lt $taskG.BoardSockets.Length;$taskI++){
    $taskPoint=$taskG.BoardSockets[$taskI];$taskNear=1000.0
    foreach($taskLane in $taskG.OpenLanes){$taskNear=[math]::Min($taskNear,[Gatefall.Core.BattlefieldLayouts]::RoadDistance($taskPoint,$taskG.Roads[$taskLane]))}
    if($taskNear -lt 2.14 -or $taskNear -gt 3.26 -or [math]::Abs($taskPoint.X) -gt 24 -or [math]::Abs($taskPoint.Y) -gt 12){throw 'Invalid socket clearance/reach/bounds'}
    $taskMinRoad=[math]::Min($taskMinRoad,$taskNear);$taskMaxRoad=[math]::Max($taskMaxRoad,$taskNear)
    for($taskJ=0;$taskJ -lt $taskI;$taskJ++){$taskGap=[Gatefall.Core.V2]::Distance($taskPoint,$taskG.BoardSockets[$taskJ]);if($taskGap -lt 2.59){throw 'Overlapping sites'};$taskMinGap=[math]::Min($taskMinGap,$taskGap)}
}
$taskStatic=@(foreach($taskStage in 0..13){foreach($taskCrisis in @($false,$true)){
    $taskS=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[$taskStage];Crisis=$taskCrisis})
    [pscustomobject]@{stage=$taskStage+1;crisis=$taskCrisis;sites=@($taskS.BoardSockets|Select-Object X,Y);lanes=$taskS.OpenLanes;roads=@(foreach($taskLane in $taskS.OpenLanes){[pscustomobject]@{lane=$taskLane;points=@($taskS.Roads[$taskLane]|Select-Object X,Y)}});startingCredits=$taskS.Credits;supply=$taskS.WaveSupplyCredits;scheduled=(@($taskS.Schedule)|Measure-Object Count -Sum).Sum;schedule=@($taskS.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm)}
}})
$taskRows=[Gatefall.Tests.AlleyDensityReview]::Models($Stage-1)
if($taskRows.Count -ne 9 -or @($taskRows|Where-Object State -NotIn @('Won','Lost')).Count){throw 'Incomplete selected-stage models'}
[pscustomobject]@{created=(Get-Date).ToString('o');variant=$Variant;newBattleModels=9;geometryValid=$true;snapshot=[pscustomobject]@{stage=$Stage;layout=[Gatefall.Core.BattlefieldLayouts]::Get($taskG.Rules.Stage).Id;credits=$taskG.Credits;sites=$taskG.BoardSockets.Length;scheduled=(@($taskG.Schedule)|Measure-Object Count -Sum).Sum;roads=$taskRoads;minimumRoad=$taskMinRoad;maximumRoad=$taskMaxRoad;minimumSiteGap=$taskMinGap};staticConstruction28=$taskStatic;models=$taskRows;scope='Same geometry-ranked purchase rules/first-offer cards/automatic Hunter skill; actual world sites, purchase times and potentially spend differ. Not human fun.'}|ConvertTo-Json -Depth 9|Set-Content $taskOutput
$taskRows|Group-Object Policy|ForEach-Object{[pscustomobject]@{variant=$Variant;policy=$_.Name;wins=@($_.Group|Where-Object State -eq Won).Count;meanFired=[math]::Round(($_.Group|Measure-Object FiredSites -Average).Average,2);meanFrequent=[math]::Round(($_.Group|Measure-Object FrequentFiringSites -Average).Average,2);idle=[math]::Round(($_.Group|Measure-Object IdleSeconds -Average).Average,2);minimumCore=($_.Group|Measure-Object Core -Minimum).Minimum;maxRecent3s=($_.Group|Measure-Object PeakActive3Seconds -Maximum).Maximum}}|Format-Table
