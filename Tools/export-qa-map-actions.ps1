param([int]$Stage,[string]$Tag,[double]$SafeLeft=136,[ValidateRange(640,4096)][int]$Width=2400,[ValidateRange(360,2160)][int]$Height=1080,
    [double]$SafeBottom=0,[double]$SafeWidth=0,[double]$SafeHeight=0,
    [int]$Loadout=135,[switch]$MarkerUnlocked,[switch]$ObserverUnlocked,[string]$ProfilePath='')
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
if($Stage -lt 4 -or $Stage -gt 20 -or $Tag -notmatch '^[a-zA-Z0-9_-]+$'){throw 'Explicit expanded stage/plain tag required'}
if($SafeWidth -eq 0){$SafeWidth=$Width-$SafeLeft};if($SafeHeight -eq 0){$SafeHeight=$Height-$SafeBottom}
if($Width -le $Height -or $SafeLeft -lt 0 -or $SafeBottom -lt 0 -or $SafeWidth -le 0 -or $SafeHeight -le 0 -or $SafeLeft+$SafeWidth -gt $Width -or $SafeBottom+$SafeHeight -gt $Height){throw 'Current landscape dimensions and complete valid safe area required'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskProfileSource=$null
if($ProfilePath){
    foreach($flag in @('Loadout','MarkerUnlocked','ObserverUnlocked')){
        if($PSBoundParameters.ContainsKey($flag)){throw 'ProfilePath determines loadout/unlocks; do not mix manual profile flags'}
    }
    $taskProfileFile=(Resolve-Path -LiteralPath $ProfilePath).Path
    $taskProfile=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes($taskProfileFile))
    if($null -eq $taskProfile -or -not $taskProfile.Valid()){throw 'Invalid profile snapshot; no coordinates exported'}
    $taskProfileRules=([Gatefall.Core.CampaignService]::new($taskProfile,$null)).Battle($Stage-1,$false)
    $Loadout=$taskProfileRules.Loadout;$MarkerUnlocked=$taskProfileRules.MarkerUnlocked;$ObserverUnlocked=$taskProfileRules.ObserverUnlocked
    $taskProfileSource=@{file=$taskProfileFile;sha256=(Get-FileHash -LiteralPath $taskProfileFile).Hash.ToLowerInvariant();readOnly=$true}
}
$scatterUnlocked=if($taskProfileSource){$taskProfile.FirstCleared[15]}else{$false}
$barrageUnlocked=if($taskProfileSource){$taskProfile.FirstCleared[17]}else{$false}
$taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[$Stage-1];Loadout=$Loadout;MarkerUnlocked=[bool]$MarkerUnlocked;ObserverUnlocked=[bool]$ObserverUnlocked;ScatterUnlocked=$scatterUnlocked;BarrageUnlocked=$barrageUnlocked})
# Panel width follows the same available families and unlock flags as the observed profile.
# Since v0.27.21 recipe order follows the catalog, filtered by stage and loadout.
$taskBuildKinds=@([Gatefall.Core.TowerKind]::Autocannon,[Gatefall.Core.TowerKind]::Binding,[Gatefall.Core.TowerKind]::Rupture,[Gatefall.Core.TowerKind]::Barrier,[Gatefall.Core.TowerKind]::Marker,[Gatefall.Core.TowerKind]::Scatter)|Where-Object {$taskG.Allows($_)}
$taskReachable=$taskG.ReachableTowerFamilies()
function RecipeCount([Gatefall.Core.TowerKind]$kind){
    return @([Gatefall.Core.TowerCatalog]::Recipes|Where-Object {
        $other=if($_.First -eq $kind){$_.Second}else{$_.First}
        ($_.First -eq $kind -or $_.Second -eq $kind) -and $taskG.Allows($_.Result) -and $taskG.Allows($other) -and (($taskReachable -band (1 -shl [int]$other)) -ne 0)
    }).Count
}
$taskSpearRecipes=RecipeCount ([Gatefall.Core.TowerKind]::Autocannon)
$taskArcRecipes=RecipeCount ([Gatefall.Core.TowerKind]::Arc)
function TowerWidth([int]$tiles){return [math]::Max(352,28+$tiles*140+($tiles-1)*8)}
$taskBuildWidth=[math]::Max(160,28+$taskBuildKinds.Count*124+($taskBuildKinds.Count-1)*8)
# World pads use the full camera. Menus use the actual safe area, including its top origin.
# The reference layout is the same Unity-free class used by the game, not a width-only estimate.
$taskLayout=[Gatefall.Core.MobileLayout]::new($Width,$Height,[Gatefall.Core.Box2]::new($SafeLeft,$SafeBottom,$SafeWidth,$SafeHeight))
$taskScale=[double]$taskLayout.Scale;$taskOrigin=[double]$taskLayout.Origin.X;$taskOriginY=[double]$taskLayout.Origin.Y
$taskWidth=[double]$taskLayout.Width;$taskRefHeight=[double]$taskLayout.Height
$taskHalfHeight=[Math]::Max(12.6,24.8/($Width/[double]$Height));$taskWorldScale=$Height/(2*$taskHalfHeight)
$taskCameraY=if($taskG.ExpandedArena){0}else{.35}
function Panel([double]$screenX,[double]$screenY,[double]$width,[double]$height){
    $taskX=[math]::Max(12,[math]::Min($taskWidth-$width-12,($screenX-$taskOrigin)/$taskScale-$width/2))
    $taskReferenceY=($screenY-$taskOriginY)/$taskScale
    $taskY=if($taskReferenceY -gt $taskRefHeight*.5){$taskReferenceY-$height-2.9*$taskWorldScale/$taskScale}else{$taskReferenceY+.9*$taskWorldScale/$taskScale}
    $taskY=[math]::Max(70,[math]::Min($taskRefHeight-$height-10,$taskY))
    return @(($taskOrigin+$taskX*$taskScale),($taskOriginY+$taskY*$taskScale))
}
$taskPanels=[System.Collections.Generic.List[object]]::new()
$taskRows=@(for($taskI=0;$taskI -lt $taskG.BoardSockets.Length;$taskI++){
    $taskP=$taskG.BoardSockets[$taskI];$taskX=$Width/2.0+$taskP.X*$taskWorldScale;$taskY=$Height/2.0-($taskP.Y-$taskCameraY)*$taskWorldScale
    $taskBuild=Panel $taskX $taskY $taskBuildWidth 196
    $taskTower=Panel $taskX $taskY (TowerWidth 3) 250
    $taskFusion=Panel $taskX $taskY (TowerWidth 3) 250
    $taskArc=Panel $taskX $taskY (TowerWidth 3) 250
    $taskRecipe=Panel $taskX $taskY 464 (78+[math]::Max(1,[math]::Ceiling($taskSpearRecipes/2.0))*116)
    # Bounds are evidence for the current profile, not universal empty-space recommendations.
    foreach($spec in @(
        @{kind='build';point=$taskBuild;width=$taskBuildWidth;height=196},
        @{kind='spearLv1';point=$taskTower;width=(TowerWidth 3);height=250},
        @{kind='spearLv2';point=$taskFusion;width=(TowerWidth 3);height=250},
        @{kind='ruptureLv1';point=(Panel $taskX $taskY (TowerWidth 3) 250);width=(TowerWidth 3);height=250}
    )){
        $taskPanels.Add([pscustomobject]@{slot=$taskI;kind=$spec.kind;x=$spec.point[0];y=$spec.point[1];width=$spec.width*$taskScale;height=$spec.height*$taskScale})
    }
    [pscustomobject]@{slot=$taskI;x=$taskP.X;y=$taskP.Y;padX=[math]::Round($taskX);padY=[math]::Round($taskY);buildX=[math]::Round($taskBuild[0]+76*$taskScale);buildY=[math]::Round($taskBuild[1]+112*$taskScale);bindX=[math]::Round($taskBuild[0]+208*$taskScale);rupX=[math]::Round($taskBuild[0]+340*$taskScale);upgradeX=[math]::Round($taskTower[0]+121*$taskScale);upgradeY=[math]::Round($taskTower[1]+135*$taskScale);fusionX=[math]::Round($taskFusion[0]+343*$taskScale);fusionY=[math]::Round($taskFusion[1]+135*$taskScale);arcUpgradeX=[math]::Round($taskArc[0]+121*$taskScale);arcUpgradeY=[math]::Round($taskArc[1]+135*$taskScale);recipe0X=[math]::Round($taskRecipe[0]+120*$taskScale);recipe1X=[math]::Round($taskRecipe[0]+344*$taskScale);recipeRow0Y=[math]::Round($taskRecipe[1]+120*$taskScale);recipeRow1Y=[math]::Round($taskRecipe[1]+236*$taskScale)}
})
$taskRows|Export-Csv -NoTypeInformation ("TestResults/"+$Tag+'-map-actions.csv')
$taskControls=@(foreach($taskAction in @([Gatefall.Core.MobileAction]::Start,[Gatefall.Core.MobileAction]::Pause,[Gatefall.Core.MobileAction]::Resume,[Gatefall.Core.MobileAction]::Speed)){
    $taskBox=$taskLayout.Button($taskAction)
    [pscustomobject]@{action=$taskAction.ToString();x=[math]::Round($taskOrigin+($taskBox.X+$taskBox.Width/2)*$taskScale);y=[math]::Round($taskOriginY+($taskBox.Y+$taskBox.Height/2)*$taskScale)}
})
[ordered]@{stage=$Stage;screenWidth=$Width;screenHeight=$Height;safe=@{left=$SafeLeft;bottom=$SafeBottom;width=$SafeWidth;height=$SafeHeight};scale=$taskScale;originX=$taskOrigin;originY=$taskOriginY;referenceWidth=$taskWidth;referenceHeight=$taskRefHeight;controls=$taskControls;sourceProfile=$taskProfileSource;profile=@{loadout=$Loadout;markerUnlocked=[bool]$MarkerUnlocked;observerUnlocked=[bool]$ObserverUnlocked;buildOrder=@($taskBuildKinds|ForEach-Object{$_.ToString()})};menuTiles=@{build=$taskBuildKinds.Count;spearLevel1=3;spearLevel2=3;arcLevel1=3;recipeOptions=$taskSpearRecipes};projectedPanels=$taskPanels;reviewProtocol='No universal empty point. For a long review use qa-android -Shot NAME -BackgroundAfterShot; after foreground wait2s and inspect actual Pause/card. Explicit Pause remains visible in LIVE tower/recipe menus. Projection does not identify the current screen.';assumptions='Stage4 overview camera or other stage full camera. Use an extracted ProfilePath snapshot to derive actual loadout/unlocks,or explicitly supply all manual flags. v0.25:Tower menu has3 actions; fusionX opens recipes. Recipe cell coordinates are Spear options only, identity/order must be inspected. Actual partner readiness/sorting/card/current menu must still be inspected.'}|ConvertTo-Json -Depth 5|Set-Content ("TestResults/"+$Tag+'-projection.json')
$taskRows|Select-Object -First 6|Format-Table
"Assumed build tiles=$($taskBuildKinds.Count), loadout=$Loadout, marker=$([bool]$MarkerUnlocked), observer=$([bool]$ObserverUnlocked). Build order: $($taskBuildKinds -join ', '). Compare with the actual menu before sending taps."
"Coordinate projection only: $Width x $Height /safe $SafeLeft,$SafeBottom,$SafeWidth,$SafeHeight /origin $taskOrigin,$taskOriginY /camera-half-height $taskHalfHeight. Confirm current screen, card and actual menu tile count."
