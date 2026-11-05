param([string]$CoreDirectory,[string]$Tag)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
if(-not $CoreDirectory -or $Tag -notmatch '^[a-zA-Z0-9_-]+$'){throw 'Explicit core and plain tag required'}
Add-Type -Path @((Get-ChildItem -LiteralPath $CoreDirectory -Filter '*.cs').FullName)
$taskStage=[Gatefall.Core.CampaignCatalog]::Stages[8]
$taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=$taskStage})
if($taskG.BoardSockets.Length -ne 24 -or $taskG.Credits -ne 280 -or (@($taskG.Schedule)|Measure-Object Count -Sum).Sum -ne 624){throw 'Changed stage9 sites/start/budget'}
$taskMinimumGap=1000.0;$taskMinimumRoad=1000.0;$taskMaximumRoad=0.0
for($taskI=0;$taskI -lt $taskG.BoardSockets.Length;$taskI++) {
    $taskP=$taskG.BoardSockets[$taskI]
    $taskNear=1000.0
    foreach($taskLane in $taskG.OpenLanes){$taskNear=[math]::Min($taskNear,[Gatefall.Core.BattlefieldLayouts]::RoadDistance($taskP,$taskG.Roads[$taskLane]))}
    if($taskNear -lt 2.14 -or $taskNear -gt 3.26 -or [math]::Abs($taskP.X) -gt 24 -or [math]::Abs($taskP.Y) -gt 12){throw 'Invalid clear/reachable socket'}
    $taskMinimumRoad=[math]::Min($taskMinimumRoad,$taskNear);$taskMaximumRoad=[math]::Max($taskMaximumRoad,$taskNear)
    for($taskJ=0;$taskJ -lt $taskI;$taskJ++){$taskGap=[Gatefall.Core.V2]::Distance($taskP,$taskG.BoardSockets[$taskJ]);if($taskGap -lt 2.59){throw 'Overlapping touch sites'};$taskMinimumGap=[math]::Min($taskMinimumGap,$taskGap)}
}
$taskRoads=@(foreach($taskLane in $taskG.OpenLanes){[pscustomobject]@{lane=$taskLane;length=[Gatefall.Core.BattlefieldLayouts]::RoadLength($taskG.Roads[$taskLane]);points=@($taskG.Roads[$taskLane]|Select-Object X,Y)}})
$taskSnapshot=[pscustomobject]@{tag=$Tag;stage=9;name=$taskStage.Name;layout=[Gatefall.Core.BattlefieldLayouts]::Get($taskStage).Id;credits=$taskG.Credits;supply=$taskG.WaveSupplyCredits;sites=$taskG.BoardSockets.Length;roads=$taskRoads;sockets=@($taskG.BoardSockets|Select-Object X,Y);scheduled=(@($taskG.Schedule)|Measure-Object Count -Sum).Sum;schedule=@($taskG.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm);minimumGap=$taskMinimumGap;minimumRoad=$taskMinimumRoad;maximumRoad=$taskMaximumRoad}
$taskSnapshot|ConvertTo-Json -Depth 8|Set-Content -LiteralPath "TestResults/distribution-$Tag-map.json"
"PASS $Tag /24 sites /624 budget /gap=$taskMinimumGap /road=$taskMinimumRoad..$taskMaximumRoad /lengths=$(($taskRoads|ForEach-Object {$_.length}) -join ',')"
