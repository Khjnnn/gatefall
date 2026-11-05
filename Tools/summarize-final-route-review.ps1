$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$base=Get-Content TestResults/final-chicane-v0211-baseline-control.json -Raw|ConvertFrom-Json
$map=Get-Content TestResults/final-chicane-v0211-west-lower-connector-pilot.json -Raw|ConvertFrom-Json
$chosen=Get-Content TestResults/final-chicane-v0211-west-lower-connector-pilot-window12.json -Raw|ConvertFrom-Json
# Older raw runs serialized a typed SwitchParameter in the static loop. Normalize
# only the recorded wrapper; the saved observer source determines battle mode.
foreach($document in @($base,$chosen)){foreach($row in $document.static28){if($row.crisis -isnot [bool]){if($null -eq $row.crisis.IsPresent){throw 'Unknown crisis metadata shape'};$row.crisis=[bool]$row.crisis.IsPresent}}}
foreach($mode in @('pilot-window12','remainder-window12','control-opening-only-window12','mixed-rest-opening-only-window12')){
    $source=[IO.File]::ReadAllText("TestResults/final-chicane-v0211-review/west-lower-connector-$mode/AlleyDensityReview.cs")
    if($source.Contains('Crisis=true')){throw 'Expected normal source snapshot'}
}
foreach($variant in @('baseline','west-lower-connector')){if(-not [IO.File]::ReadAllText("TestResults/final-chicane-v0211-review/$variant-pilot-crisis/AlleyDensityReview.cs").Contains('Crisis=true')){throw 'Expected crisis source snapshot'}}
$normal=@($chosen.models)+@((Get-Content TestResults/final-chicane-v0211-west-lower-connector-remainder-window12.json -Raw|ConvertFrom-Json).models)
$old=@((Get-Content TestResults/tower-density-v0170.json -Raw|ConvertFrom-Json).models|Where-Object Stage -eq 14)
$opening=@((Get-Content TestResults/final-chicane-v0211-west-lower-connector-control-opening-only-window12.json -Raw|ConvertFrom-Json).models)+@((Get-Content TestResults/final-chicane-v0211-west-lower-connector-mixed-rest-opening-only-window12.json -Raw|ConvertFrom-Json).models)
$openingOld=@((Get-Content TestResults/final-chicane-v0211-baseline-mixed-all-opening-only.json -Raw|ConvertFrom-Json).models)
$openingMap=@((Get-Content TestResults/final-chicane-v0211-west-lower-connector-mixed-all-opening-only.json -Raw|ConvertFrom-Json).models)
$crisisBefore=@((Get-Content TestResults/final-chicane-v0211-baseline-pilot-crisis.json -Raw|ConvertFrom-Json).models)
$crisisAfter=@((Get-Content TestResults/final-chicane-v0211-west-lower-connector-pilot-crisis.json -Raw|ConvertFrom-Json).models)
if($normal.Count -ne 9 -or @($normal|Where-Object {$_.State -ne 'Won' -or $_.Core -ne 300 -or $_.Kills -ne 944 -or $_.Remaining -ne 0 -or -not $_.AllSpawned -or $_.Towers -lt 15 -or $_.Towers -gt 16 -or $_.SupplyCredits -ne 500 -or $_.CardOffers -ne 3 -or $_.Catalysts -ne 2}).Count){throw 'Selected normal scope/results differ'}
if($opening.Count -ne 3 -or @($opening|Where-Object {$_.Spent -ne 310 -or $_.Towers -ne 7 -or $_.Credits -ne 876 -or $_.State -ne 'Won'}).Count){throw 'Opening-only scope differs'}
if($crisisBefore.Count -ne 3 -or $crisisAfter.Count -ne 3 -or @($crisisAfter|Where-Object {$_.State -ne 'Won' -or $_.Core -ne 300}).Count){throw 'Selected crisis results differ'}
for($i=0;$i -lt 28;$i++){
    $a=$base.static28[$i];$b=$chosen.static28[$i]
    if($a.stage -ne $b.stage -or $a.crisis -ne $b.crisis){throw 'Static ordering differs'}
    if($a.stage -ne 14){if(($a|ConvertTo-Json -Depth 10 -Compress) -cne ($b|ConvertTo-Json -Depth 10 -Compress)){throw 'Other operation changed'};continue}
    if($b.sites.Count -ne 24 -or $a.startingCredits -ne $b.startingCredits -or $a.supply -ne $b.supply -or $a.scheduled -ne 944 -or $b.scheduled -ne 944 -or ($a.lanes -join ',') -ne ($b.lanes -join ',')){throw 'Stage14 count/economy/lane scope differs'}
    if(($a.roads[1]|ConvertTo-Json -Depth 5 -Compress) -cne ($b.roads[1]|ConvertTo-Json -Depth 5 -Compress)){throw 'East road changed'}
    if($a.schedule.Count -ne $b.schedule.Count){throw 'Schedule cell count differs'}
    for($j=0;$j -lt $a.schedule.Count;$j++){
        $x=$a.schedule[$j];$y=$b.schedule[$j]
        foreach($key in @('Wave','Lane','Kind','Count','Swarm')){if($x.$key -ne $y.$key){throw 'Schedule species/count/lane changed'}}
        if($a.crisis -or $x.Wave -lt 3){if(($x|ConvertTo-Json -Compress) -cne ($y|ConvertTo-Json -Compress)){throw 'Initial waves/crisis schedule changed'}}
    }
    foreach($wave in 0..5){foreach($lane in $a.lanes){$firstA=($a.schedule|Where-Object {$_.Wave -eq $wave -and $_.Lane -eq $lane}|Measure-Object Start -Minimum).Minimum;$firstB=($b.schedule|Where-Object {$_.Wave -eq $wave -and $_.Lane -eq $lane}|Measure-Object Start -Minimum).Minimum;if([math]::Abs($firstA-$firstB) -gt .000001){throw 'Entrance first spawn changed'}}}
}
$projection=Get-Content TestResults/final-road-header-v0211-west-lower-connector-pilot.json -Raw|ConvertFrom-Json
if(@($projection.rows|Where-Object {$_.roadCentreSamplesUnderHeader -ne 0 -or $_.roadStrokeSampleDiscsIntersectHeader -ne 0}).Count){throw 'Chosen road/header clash'}
$pairs=@(foreach($n in $normal){$o=@($old|Where-Object {$_.Seed -eq $n.Seed -and $_.Policy -eq $n.Policy})[0];[ordered]@{seed=$n.Seed;policy=$n.Policy;old=$o;candidate=$n;timeDifference=$n.Time-$o.Time;actualSpendDifference=$n.Spent-$o.Spent}})
[ordered]@{
    created=(Get-Date -Format o);tasks=@(237,238,239,240);decision='Select lower shared-spine + 12-second late pulse for product/Android validation; not yet delivered';newAndroidVerified=$false;productApplied=$false
    sourceVersion='0.21.1';historicalBaselineNormalRowsReused=9;selectedCurrentControlNew=1;historicalControlExactSharedMetricsTimeMatched=$true
    discardedWestChicane=$true;upperConnectorNotAdoptedBecauseHeaderCollision=$true;mapOnlyHeldBecauseOpeningPressureReduced=$true;window16PilotOnly=$true
    originalWestLength=39.828426;chosenWestLength=49.828426;eastLengthUnchanged=41.828426;sites=24;totalSitesUnchanged=308;scheduled=944;lateNormalArrivalWindow=12;initialThreeWavesAndFirstEntranceSpawnsUnchanged=$true;other13OperationsAndAllCrisisSchedulesUnchanged=$true;crisisGeometryChanged=$true
    oldFiredAverage=($old|Measure-Object FiredSites -Average).Average;newFiredAverage=($normal|Measure-Object FiredSites -Average).Average;oldFrequentAverage=($old|Measure-Object FrequentFiringSites -Average).Average;newFrequentAverage=($normal|Measure-Object FrequentFiringSites -Average).Average;oldSocketRecentMax=($old|Measure-Object PeakActive3Seconds -Maximum).Maximum;newSocketRecentMax=($normal|Measure-Object PeakActive3Seconds -Maximum).Maximum;newCurrentIdRecentMax=($normal|Measure-Object CurrentIdPeakRecent3 -Maximum).Maximum
    pairs=$pairs;openingBefore=$openingOld;openingMapOnly=$openingMap;openingSelected=$opening;crisisSeed7ThreePoliciesBefore=$crisisBefore;crisisSeed7ThreePoliciesAfter=$crisisAfter;headerProjection=$projection
    limitations=@('Same geometry-ranked purchase rules, not fixed world positions or actual purchase times','Candidate mixed seed19 ends with15 towers, other eight16; do not claim all16','Opening7 costs310 and leaves876 unspent; all selected opening conditions still win so reinforcement is not survival-required in these models','Automatic skill includes Hunter dodge, first-offer cards; not human idle play or Native exact replay','Three tested crisis policies use seed7 only','Last-spawn compression contributes to faster clearing; do not attribute all to geometry','Current-ID recent metric and legacy socket metric differ','Road stroke/header check is a sampled static projection, not Android visual proof','Physical device/iOS/audio/human fun untested')
    rawMetadataCorrection='A static loop reused typed Crisis switch: raw static28 crisis is IsPresent wrapper and top-level crisis became true. Normalized recorded wrappers for this scope comparison; actual model modes verified from preserved observer source. No battle rerun or raw overwrite.'
}|ConvertTo-Json -Depth 14|Set-Content TestResults/final-spine-v0211-model-comparison.json
'PASS: recorded-only final route/pulse scope and limited model comparison; candidate awaits product/Android validation'
