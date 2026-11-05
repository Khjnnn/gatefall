$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
# Recorded geometry and current input engine only. No ADB or new battle.
$p=Get-Content TestResults/v0243-panel-guard-projection.json -Raw|ConvertFrom-Json
if($p.stage -ne 11 -or $p.screenWidth -ne 2400 -or $p.safe.height -ne 929 -or $p.profile.loadout -ne 263 -or $p.projectedPanels.Count -ne 96){throw 'Wrong recorded layout'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$layout=[Gatefall.Core.MobileLayout]::new(2400,1080,[Gatefall.Core.Box2]::new(0,0,2400,929))
function RefPoint([double]$x,[double]$y){return [Gatefall.Core.V2]::new(($x-$p.originX)/$p.scale,($y-$p.originY)/$p.scale)}
function Tap([Gatefall.Core.MobileInput]$qaTouch,[Gatefall.Core.V2]$point){
    $qaTouch.Reset()
    $qaTouch.Pointer(7,[Gatefall.Core.PointerPhase]::Begin,$point)
    $qaTouch.Pointer(7,[Gatefall.Core.PointerPhase]::End,$point)
    if($qaTouch.Actions.Count -gt 1){throw 'Unexpected duplicate action'}
    if($qaTouch.Actions.Count){return [int]$qaTouch.Actions[0]}else{return 0}
}
$hud=Get-Content Assets/Scripts/Presentation/MobileHud.cs -Raw
foreach($text in @('controller.TouchInput.Modal = controller.Menu != GameMenu.None','new TouchRegion(0, activeTowerPanel)','new TouchRegion(100, new Box2(0, 0, W, H))','if (controller.Menu == GameMenu.None && (game.State == RunState.Running || game.IsPreparing))')){if(-not $hud.Contains($text)){throw 'HUD registration/order assumptions changed; review actual source'}}
$rows=@(foreach($panel in @($p.projectedPanels|Where-Object {($_.slot -eq 3 -and $_.kind -eq 'ruptureLv1') -or ($_.slot -eq 21 -and $_.kind -eq 'spearLv2')})){
    $qaTouch=[Gatefall.Core.MobileInput]::new($layout)
    $qaTouch.SetState([Gatefall.Core.RunState]::Running)
    $qaTouch.Modal=$true
    $box=[Gatefall.Core.Box2]::new(($panel.x-$p.originX)/$p.scale,($panel.y-$p.originY)/$p.scale,$panel.width/$p.scale,$panel.height/$p.scale)
    # No button overlaps either tested point. This is the observed panel-body/catch-all order.
    $qaTouch.Regions.Add([Gatefall.Core.TouchRegion]::new(0,$box))
    $qaTouch.Regions.Add([Gatefall.Core.TouchRegion]::new(100,[Gatefall.Core.Box2]::new(0,0,$layout.Width,$layout.Height)))
    $blocked=Tap $qaTouch (RefPoint 2250 400)
    $pauseCloses=Tap $qaTouch (RefPoint 2327 214)
    $outsideCloses=Tap $qaTouch (RefPoint 200 400)
    $qaTouch.Modal=$false;$qaTouch.Regions.Clear()
    $pauseVisible=Tap $qaTouch (RefPoint 2327 214)
    if(-not $box.Contains((RefPoint 2250 400)) -or $blocked -ne 0 -or $pauseCloses -ne 100 -or $outsideCloses -ne 100 -or $pauseVisible -ne [int][Gatefall.Core.MobileAction]::Pause){throw 'Observed QA counterexample changed'}
    [ordered]@{slot=$panel.slot;kind=$panel.kind;panel=$panel;oldEmptyPointAction=$blocked;hiddenPausePointAction=$pauseCloses;outsideAction=$outsideCloses;visiblePauseAction=$pauseVisible;fourChecksPassed=$true}
})
if($rows.Count -ne 2){throw 'Two actual counterexample panels required'}
if((Get-Content TestResults/v0243-double-stage11-map-actions.csv -Raw) -cne (Get-Content TestResults/v0243-panel-guard-map-actions.csv -Raw)){throw 'Old24 CSV changed'}
foreach($r in (Get-Content TestResults/source-hashes-v0243.json -Raw|ConvertFrom-Json)){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Product changed during QA correction'}}
[ordered]@{created=(Get-Date -Format o);task=260;staticInputCases=8;nativeRuns=0;newApk=0;newAvd=0;productChanged=$false;original24CsvUnchanged=$true;universalEmptyPointRejected=$true;reviewMethod='qa-android -Shot NAME -BackgroundAfterShot; foreground/wait2s/inspect current Pause/card before input';cases=$rows;limits='Checks actual input engine with recorded two-panel geometry. No new Android test, universal menu location, card identity, screenshot recognition or HOME recovery automation claim.';passed=$true}|ConvertTo-Json -Depth 8|Set-Content TestResults/qa-panel-guard-v0243-review.json
'PASS8 input boundary cases:2250,400 blocked/right panels; hidden Pause point closes only; visible Pause works. Same24 CSV/product23; no new Native/APK/AVD.'
