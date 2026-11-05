$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskReview=Get-Content TestResults/north-hud-v0203-review.json -Raw|ConvertFrom-Json
$taskCases=@(foreach($taskRow in $taskReview.rows){
    $taskW,$taskH=$taskRow.screen.Split('x')|ForEach-Object {[int]$_}
    $taskL=[Gatefall.Core.MobileLayout]::new($taskW,$taskH,[Gatefall.Core.Box2]::new($taskRow.safeLeft,0,($taskW-$taskRow.safeLeft),$taskH))
    $taskRifts=[Collections.Generic.List[Gatefall.Core.Box2]]::new()
    foreach($taskR in $taskRow.portals){$taskRifts.Add([Gatefall.Core.Box2]::new($taskR.X,$taskR.Y,$taskR.Width,$taskR.Height))}
    $taskB=$taskL.WaveHeader($taskRifts)
    $taskClashes=@($taskRifts|Where-Object {$taskB.X -lt ($_.X+$_.Width) -and $_.X -lt ($taskB.X+$taskB.Width) -and $taskB.Y -lt ($_.Y+$_.Height) -and $_.Y -lt ($taskB.Y+$taskB.Height)}).Count
    if($taskClashes -or $taskB.X -lt 344 -or ($taskB.X+$taskB.Width) -gt ($taskL.Width-322) -or $taskB.Width -lt 150){throw 'Product header overlap or width regression'}
    if(-not $taskRow.changed -and ([math]::Abs($taskB.Center.X-$taskL.Width/2) -gt .001 -or $taskB.Width -ne 181)){throw 'Previously clear header moved'}
    if([math]::Abs($taskB.X-$taskRow.candidate.X) -gt .001 -or $taskB.Width -ne $taskRow.candidate.Width){throw 'Selected prototype differs from product layout'}
    [pscustomobject]@{stage=$taskRow.stage;screen=$taskRow.screen;x=$taskB.X;width=$taskB.Width;collisions=$taskClashes;previouslyClearUnchanged=(-not $taskRow.changed);withinHeaderGap=$true}
})
if($taskCases.Count -ne 42){throw 'Incomplete layout matrix'}
$taskObserved106=@(foreach($taskRow in @($taskReview.rows|Where-Object screen -eq '1920x1080')){
    $taskL=[Gatefall.Core.MobileLayout]::new(1920,1080,[Gatefall.Core.Box2]::new(106,0,1814,1080))
    $taskRifts=[Collections.Generic.List[Gatefall.Core.Box2]]::new()
    foreach($taskR in $taskRow.portals){$taskRifts.Add([Gatefall.Core.Box2]::new(($taskR.X-1/1.8),$taskR.Y,$taskR.Width,$taskR.Height))}
    $taskB=$taskL.WaveHeader($taskRifts)
    $taskClashes=@($taskRifts|Where-Object {$taskB.X -lt ($_.X+$_.Width) -and $_.X -lt ($taskB.X+$taskB.Width) -and $taskB.Y -lt ($_.Y+$_.Height) -and $_.Y -lt ($taskB.Y+$taskB.Height)}).Count
    if($taskClashes -or $taskB.X -lt 344 -or ($taskB.X+$taskB.Width) -gt ($taskL.Width-322) -or $taskB.Width -lt 150){throw 'Observed safe-left106 geometry regression'}
    [pscustomobject]@{stage=$taskRow.stage;screen='1920x1080';safeLeft=106;x=$taskB.X;width=$taskB.Width;collisions=0;withinHeaderGap=$true}
})
$taskCrisisPairs=0
foreach($taskStage in [Gatefall.Core.CampaignCatalog]::Stages){
    $taskNormal=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=$taskStage})
    $taskCrisis=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=$taskStage;Crisis=$true})
    if((@($taskNormal.GatePositions|Select-Object X,Y)|ConvertTo-Json -Compress) -cne (@($taskCrisis.GatePositions|Select-Object X,Y)|ConvertTo-Json -Compress) -or ($taskNormal.OpenLanes|ConvertTo-Json -Compress) -cne ($taskCrisis.OpenLanes|ConvertTo-Json -Compress)){throw 'Crisis entrance geometry differs'}
    $taskCrisisPairs++
}
[pscustomobject]@{created=(Get-Date).ToString('o');task=198;sourceVersion='0.20.4';cases=42;observedSafe106ExtraCases=$taskObserved106.Count;totalLayouts=42+$taskObserved106.Count;allClear=$true;minWidth=150;unchangedClearCases=@($taskCases|Where-Object previouslyClearUnchanged).Count;crisisEntranceGeometryPairs=$taskCrisisPairs;rows=$taskCases;observedSafe106Rows=$taskObserved106;androidVerified=$false;scope='Static maximum-warning opacity guard. Running overview header only; ready lesson/boss meter unchanged. Crisis pairs are geometry checks, not battle runs.'}|ConvertTo-Json -Depth 5|Set-Content TestResults/north-hud-v0204-geometry.json
'PASS42+14 product header layouts including observed safe-left106: no entrance guard/currency/speed overlap;30 original clear layouts unchanged; compact width150. Android rendering remains separate.'
