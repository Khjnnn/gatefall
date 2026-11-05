$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
foreach($taskR in (Get-Content TestResults/source-hashes-v0203.json -Raw|ConvertFrom-Json)){if((Get-FileHash -LiteralPath $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed during pre-implementation review'}}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/NorthHudPlacementReview.cs'))
$taskAlpha=Get-Content TestResults/north-hud-v0203-rift-alpha.json -Raw|ConvertFrom-Json
$taskScreens=@(@{w=2400;h=1080;safe=132},@{w=1920;h=1080;safe=105},@{w=1280;h=720;safe=70})
$taskRows=@(foreach($taskScreen in $taskScreens){foreach($taskStage in [Gatefall.Core.CampaignCatalog]::Stages){
    $taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=$taskStage})
    $taskL=[Gatefall.Core.MobileLayout]::new($taskScreen.w,$taskScreen.h,[Gatefall.Core.Box2]::new($taskScreen.safe,0,($taskScreen.w-$taskScreen.safe),$taskScreen.h))
    $taskOrtho=[math]::Max(12.6,24.8/($taskScreen.w/$taskScreen.h));$taskCameraY=if($taskG.ExpandedArena){0}else{.35}
    $taskWorldScale=$taskScreen.h/(2*$taskOrtho)
    $taskPortals=[Collections.Generic.List[Gatefall.Core.Box2]]::new()
    foreach($taskLane in $taskG.OpenLanes){
        $taskP=$taskG.GatePositions[$taskLane]
        $taskPx=$taskScreen.w/2+$taskP.X*$taskWorldScale;$taskPy=$taskScreen.h/2-($taskP.Y-$taskCameraY)*$taskWorldScale
        $taskB=$taskAlpha.thresholdBounds.'64';$taskFactor=1.55*$taskWorldScale/58
        $taskX=($taskPx+($taskB[0]-.5*$taskAlpha.width)*$taskFactor-$taskScreen.safe)/$taskL.Scale
        $taskY=($taskPy+($taskB[1]-(1-.47)*$taskAlpha.height)*$taskFactor)/$taskL.Scale
        $taskR=[Gatefall.Core.Box2]::new($taskX,$taskY,(($taskB[2]-$taskB[0])*$taskFactor/$taskL.Scale),(($taskB[3]-$taskB[1])*$taskFactor/$taskL.Scale))
        # Measured opaque warning extent fits in a centered5.1x3.2-world-unit guard.
        $taskCenterX=($taskPx-$taskScreen.safe)/$taskL.Scale;$taskCenterY=$taskPy/$taskL.Scale
        $taskHalfX=2.55*$taskWorldScale/$taskL.Scale;$taskHalfY=1.6*$taskWorldScale/$taskL.Scale
        $taskGuard=[Gatefall.Core.Box2]::new(($taskCenterX-$taskHalfX),($taskCenterY-$taskHalfY),(2*$taskHalfX),(2*$taskHalfY))
        if($taskR.X -lt $taskGuard.X -or ($taskR.X+$taskR.Width) -gt ($taskGuard.X+$taskGuard.Width) -or $taskR.Y -lt $taskGuard.Y -or ($taskR.Y+$taskR.Height) -gt ($taskGuard.Y+$taskGuard.Height)){throw 'Art warning ink exceeds the proposed guard'}
        if($taskGuard.Y -lt 62 -and ($taskGuard.Y+$taskGuard.Height) -gt 0){$taskPortals.Add($taskGuard)}
    }
    $taskOriginal=[Gatefall.Core.Box2]::new(($taskL.Width/2-90.5),4,181,52)
    $taskCandidate=[Gatefall.Tests.NorthHudPlacementReview]::Candidate($taskL.Width,$taskPortals)
    $taskOldCollisions=@($taskPortals|Where-Object {[Gatefall.Tests.NorthHudPlacementReview]::Overlap($taskOriginal,$_)}).Count
    $taskNewCollisions=@($taskPortals|Where-Object {[Gatefall.Tests.NorthHudPlacementReview]::Overlap($taskCandidate,$_)}).Count
    [pscustomobject]@{stage=$taskStage.Index+1;screen="$($taskScreen.w)x$($taskScreen.h)";safeLeft=$taskScreen.safe;referenceWidth=$taskL.Width;portals=@($taskPortals|Select-Object X,Y,Width,Height);original=($taskOriginal|Select-Object X,Y,Width,Height);candidate=($taskCandidate|Select-Object X,Y,Width,Height);originalCollisions=$taskOldCollisions;candidateCollisions=$taskNewCollisions;shift=$taskCandidate.Center.X-$taskOriginal.Center.X;compact=($taskCandidate.Width -lt 181);changed=($taskCandidate.X -ne $taskOriginal.X -or $taskCandidate.Width -ne $taskOriginal.Width);candidateWithinHeaderGap=($taskCandidate.X -ge 344 -and ($taskCandidate.X+$taskCandidate.Width) -le ($taskL.Width-322))}
}})
[pscustomobject]@{created=(Get-Date).ToString('o');task=197;sourceVersion='0.20.3';layouts=$taskRows.Count;phase='product-outside prototype';spriteAlphaThreshold=64;spriteScale=1.55;transparencyBoundingApproximation=$true;runningHeaderOnly=$true;readyLessonAndBossHudNotCovered=$true;rows=$taskRows;productChanged=$false;newBuild=$false;newAvd=$false;androidCandidateVerified=$false}|ConvertTo-Json -Depth 8|Set-Content TestResults/north-hud-v0203-review.json
$taskRows|Where-Object {$_.originalCollisions -or $_.candidateCollisions}|Select-Object stage,screen,originalCollisions,candidateCollisions,shift,compact,candidateWithinHeaderGap|Format-Table -AutoSize
