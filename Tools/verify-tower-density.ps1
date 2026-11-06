param([string]$Tag='')
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
if(-not $Tag){$taskText=Get-Content (Join-Path $taskProject 'ProjectSettings/ProjectSettings.asset') -Raw;$Tag='v'+[regex]::Match($taskText,'(?m)^  bundleVersion: (\S+)').Groups[1].Value.Replace('.','')}
Add-Type -Path @((Get-ChildItem (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName)
$taskOld=Get-Content (Join-Path $taskProject 'Tests/Fixtures/map-sites-v0160.json') -Raw|ConvertFrom-Json
$taskPlan=Get-Content (Join-Path $taskProject 'Docs/Design/route-iteration-plan.json') -Raw|ConvertFrom-Json
$taskExpectedSites=@(14,18,14,24,24,24,24,22,24,24,24,24,24,24)
$taskRows=@();$taskPreserved=0;$taskExpectedPreserved=0;$taskTotal=0
foreach($taskStage in [Gatefall.Core.CampaignCatalog]::LegacyStages){
    $taskIndex=$taskStage.Index
    $taskLayout=[Gatefall.Core.BattlefieldLayouts]::Get($taskStage)
    $taskChanged=$taskLayout.Id -ne $taskOld[$taskIndex].layoutId
    if($taskChanged -and -not @($taskPlan.changed_layouts|Where-Object {$_.stage_index -eq $taskIndex -and $_.layout_id -eq $taskLayout.Id}).Count){throw "Undeclared layout change $taskIndex"}
    if(-not $taskChanged){$taskExpectedPreserved+=$taskOld[$taskIndex].points.Count}
    $taskRules=[Gatefall.Core.CampaignBattle]@{Stage=$taskStage}
    $taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]@{SpawnEnemies=$false;OfferCards=$false},7,$taskRules)
    $taskStart=if($taskIndex -lt 3){240}elseif($taskIndex -lt 9){280}else{320}
    $taskSupply=if($taskIndex -lt 3){80}else{100}
    if($taskG.Credits -ne $taskStart -or $taskG.BoardSockets.Length -ne $taskExpectedSites[$taskIndex]){throw "Stage$($taskIndex+1) economy or site count mismatch"}
    $taskMinRoad=100.0;$taskMinSpacing=100.0
    for($taskI=0;$taskI -lt $taskG.BoardSockets.Length;$taskI++){
        $taskPoint=$taskG.BoardSockets[$taskI]
        $taskDistance=100.0
        foreach($taskLane in $taskG.OpenLanes){$taskDistance=[math]::Min($taskDistance,[Gatefall.Core.BattlefieldLayouts]::RoadDistance($taskPoint,$taskG.Roads[$taskLane]))}
        $taskReach=if($taskIndex -ge 4){3.4}else{4.8}
        if($taskDistance -lt 2.1 -or $taskDistance -gt $taskReach -or $taskPoint.Length -lt 4.05){throw "Unsafe or unreachable site $taskIndex/$taskI"}
        $taskMinRoad=[math]::Min($taskMinRoad,$taskDistance)
        for($taskJ=0;$taskJ -lt $taskI;$taskJ++){$taskGap=[Gatefall.Core.V2]::Distance($taskPoint,$taskG.BoardSockets[$taskJ]);if($taskGap -lt 2.55){throw 'Touch sites overlap'};$taskMinSpacing=[math]::Min($taskMinSpacing,$taskGap)}
        if(-not $taskChanged -and $taskI -lt $taskOld[$taskIndex].points.Count){$taskBefore=$taskOld[$taskIndex].points[$taskI];if([math]::Abs($taskPoint.X-$taskBefore.x) -gt .0001 -or [math]::Abs($taskPoint.Y-$taskBefore.y) -gt .0001){throw "Original site moved $taskIndex/$taskI"};$taskPreserved++}
    }
    $taskG.Start();$taskG.Tick(200,[Gatefall.Core.V2]::new(0,0))
    if($taskG.SuppliesGranted -ne 5 -or $taskG.Credits -ne $taskStart+5*$taskSupply){throw 'Supply amount/cap mismatch'}
    $taskFunded=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]@{StartingCredits=100000;SpawnEnemies=$false;OfferCards=$false},7,$taskRules)
    $taskReason=''
    # Prove every site individually (build, then sell), then the staged simultaneous limit itself.
    $taskLimit=$taskFunded.TowerLimit
    if($taskLimit -ne $(if($taskIndex -ge 9){14}elseif($taskIndex -ge 6){12}else{0})){throw "Stage$($taskIndex+1) tower limit mismatch: $taskLimit"}
    for($taskI=0;$taskI -lt $taskFunded.BoardSockets.Length;$taskI++){
        if(-not $taskFunded.TryBuild($taskI,[Gatefall.Core.TowerKind]::Autocannon,"density-$taskIndex-$taskI",[ref]$taskReason)){throw "Cannot build site $taskI : $taskReason"}
        if($taskLimit -gt 0 -and -not $taskFunded.TrySell($taskFunded.TowerAt($taskI).Id,"density-sell-$taskIndex-$taskI",[ref]$taskReason)){throw "Cannot clear site $taskI : $taskReason"}
    }
    if($taskLimit -eq 0 -and $taskFunded.Towers.Count -ne $taskExpectedSites[$taskIndex]){throw 'Full board construction incomplete'}
    if($taskLimit -gt 0){
        for($taskI=0;$taskI -lt $taskLimit;$taskI++){if(-not $taskFunded.TryBuild($taskI,[Gatefall.Core.TowerKind]::Autocannon,"density-limit-$taskIndex-$taskI",[ref]$taskReason)){throw "Cannot build up to the limit: $taskReason"}}
        if($taskFunded.TryBuild($taskLimit,[Gatefall.Core.TowerKind]::Autocannon,"density-over-$taskIndex",[ref]$taskReason)){throw 'Tower limit not enforced'}
    }
    $taskRows += [pscustomobject]@{stage=$taskIndex+1;layout=$taskLayout.Id;declaredRouteChange=$taskChanged;sites=$taskG.BoardSockets.Length;start=$taskStart;waveSupply=$taskSupply;totalWaveSupply=5*$taskSupply;towerLimit=$taskLimit;minRoadDistance=$taskMinRoad;minSpacing=$taskMinSpacing;everySocketConstructible=$true}
    $taskTotal+=$taskG.BoardSockets.Length
}
if($taskTotal -ne 308 -or $taskPreserved -ne $taskExpectedPreserved){throw 'Wrong site totals'}
[pscustomobject]@{total=$taskTotal;originalSitesPreserved=$taskPreserved;expectedPreserved=$taskExpectedPreserved;operations=$taskRows;scope='Geometry, short-range coverage, unchanged-layout original coordinates, quiet supply caps and full-board construction. Declared route redesigns intentionally regenerate their sites. Android and human play are separate.'}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $taskProject "TestResults/tower-density-$Tag-checks.json")
$taskRows|ConvertTo-Json|Set-Content (Join-Path $taskProject "TestResults/tower-density-$Tag-operations.json")
Write-Output "PASS 308 road-clear reachable sites /$taskPreserved unchanged-layout original coordinates preserved /every socket constructible /14 capped budgets /post-seal limits12 then14 enforced."
