param([ValidateSet('baseline','west-hook','north-court','three-entrances','three-clear-courts')][string]$Variant)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskOutput="TestResults/alley-v0192-$Variant.json"
if(Test-Path -LiteralPath $taskOutput){throw 'Preserve recorded results'}
$taskRoot=Join-Path $taskProject 'TestResults/alley-v0192-review'
$taskCore=Join-Path $taskRoot $Variant
Add-Type -Path @((Get-ChildItem -LiteralPath $taskCore -Filter '*.cs').FullName+(Join-Path $taskRoot 'AlleyDensityReview.cs'))
try {
    $taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[7]})
} catch {
    [pscustomobject]@{created=(Get-Date).ToString('o');variant=$Variant;geometryValid=$false;reason=$_.Exception.Message;newBattleModels=0}|ConvertTo-Json|Set-Content $taskOutput
    "REJECTED $Variant before combat: $($_.Exception.Message)";exit 0
}
if($taskG.BoardSockets.Length -ne 22 -or $taskG.Credits -ne 280 -or (@($taskG.Schedule)|Measure-Object Count -Sum).Sum -ne 612){throw 'Stage8 sites/start/budget changed'}
$taskMinRoad=1000.0;$taskMaxRoad=0.0;$taskMinGap=1000.0
for($taskI=0;$taskI -lt $taskG.BoardSockets.Length;$taskI++){
    $taskPoint=$taskG.BoardSockets[$taskI];$taskNear=1000.0
    foreach($taskLane in $taskG.OpenLanes){$taskNear=[math]::Min($taskNear,[Gatefall.Core.BattlefieldLayouts]::RoadDistance($taskPoint,$taskG.Roads[$taskLane]))}
    if($taskNear -lt 2.14 -or $taskNear -gt 3.26 -or [math]::Abs($taskPoint.X) -gt 24 -or [math]::Abs($taskPoint.Y) -gt 12){throw 'Invalid socket clearance/reach/bounds'}
    $taskMinRoad=[math]::Min($taskMinRoad,$taskNear);$taskMaxRoad=[math]::Max($taskMaxRoad,$taskNear)
    for($taskJ=0;$taskJ -lt $taskI;$taskJ++){$taskGap=[Gatefall.Core.V2]::Distance($taskPoint,$taskG.BoardSockets[$taskJ]);if($taskGap -lt 2.59){throw 'Overlapping tower sites'};$taskMinGap=[math]::Min($taskMinGap,$taskGap)}
}
$taskRoads=@(foreach($taskLane in $taskG.OpenLanes){[pscustomobject]@{lane=$taskLane;length=[Gatefall.Core.BattlefieldLayouts]::RoadLength($taskG.Roads[$taskLane]);points=@($taskG.Roads[$taskLane]|Select-Object X,Y)}})
$taskSnapshot=[pscustomobject]@{stage=8;name=$taskG.Rules.Stage.Name;layout=[Gatefall.Core.BattlefieldLayouts]::Get($taskG.Rules.Stage).Id;credits=$taskG.Credits;sites=$taskG.BoardSockets.Length;scheduled=(@($taskG.Schedule)|Measure-Object Count -Sum).Sum;schedule=@($taskG.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm);roads=$taskRoads;sockets=@($taskG.BoardSockets|Select-Object X,Y);minimumGap=$taskMinGap;minimumRoad=$taskMinRoad;maximumRoad=$taskMaxRoad}
$taskRows=[Gatefall.Tests.AlleyDensityReview]::Models(7)
if($taskRows.Count -ne 9 -or @($taskRows|Where-Object State -NotIn @('Won','Lost')).Count){throw 'Incomplete model runs'}
[pscustomobject]@{created=(Get-Date).ToString('o');variant=$Variant;geometryValid=$true;newBattleModels=9;snapshot=$taskSnapshot;models=$taskRows;scope='Same geometry-ranked purchase policy, first-offer cards, automatic Hunter skill; not fixed world sites/time or human fun. Observations only; original policy reverse-matched.'}|ConvertTo-Json -Depth 8|Set-Content $taskOutput
$taskRows|Group-Object Policy|ForEach-Object{[pscustomobject]@{variant=$Variant;policy=$_.Name;wins=@($_.Group|Where-Object State -eq Won).Count;meanFired=[math]::Round(($_.Group|Measure-Object FiredSites -Average).Average,2);meanFrequent=[math]::Round(($_.Group|Measure-Object FrequentFiringSites -Average).Average,2);minCore=($_.Group|Measure-Object Core -Minimum).Minimum;maxRecent3s=($_.Group|Measure-Object PeakActive3Seconds -Maximum).Maximum}}|Format-Table
