$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$parts=@(Get-Content TestResults/north-marker-v0230-pilot.json -Raw|ConvertFrom-Json;Get-Content TestResults/north-marker-v0230-seed19.json -Raw|ConvertFrom-Json)
$rows=@($parts|ForEach-Object models)
if($rows.Count -ne 9){throw 'Expected nine recorded comparisons'}
$pairs=@(foreach($seed in @(7,19,41)){
    $control=@($rows|Where-Object {$_.Seed -eq $seed -and $_.MarkerSite -eq 21})[0]
    foreach($site in @(20,0)){
        $candidate=@($rows|Where-Object {$_.Seed -eq $seed -and $_.MarkerSite -eq $site})[0]
        foreach($r in @($control,$candidate)){if($r.State -ne 'Won' -or $r.Kills -ne 944 -or -not $r.AllSpawned -or $r.Remaining -ne 0 -or $r.Spent -ne 1140 -or $r.Credits -ne 46 -or $r.Towers -ne 20 -or $r.CompletedOrders -ne 28){throw 'Recorded completion/scope differs'}}
        if(($control.Cards -join '/') -ne ($candidate.Cards -join '/')){throw 'Pair cards differ'}
        $normalized=@($candidate.PurchaseLog|ForEach-Object {$_ -replace "((?:build|upgrade) Marker) @$site$",'$1 @21'})
        if(@(Compare-Object $control.PurchaseLog $normalized -SyncWindow 0).Count){throw 'Purchases differ beyond marker position'}
        for($i=0;$i -lt 28;$i++){if([math]::Abs($control.ActualTimes[$i]-$candidate.ActualTimes[$i]) -gt .0001){throw 'Actual purchase time differs'}}
        [ordered]@{seed=$seed;candidateSite=$site;cards=$control.Cards;control=$control;candidate=$candidate;onlyMarkerBuildAndUpgradePositionChanged=$true;actualPurchaseTimesEqualWithin0001=$true}
    }
})
[ordered]@{created=(Get-Date -Format o);task=244;newModels=9;newApkAvdNative=$false;productChanged=$false;fixedTwentyTowerFixtureSpend=1140;allPairs28PurchasesSameTimesWithin0001=$true;nativeExactReplay=$false;environmentTickCountSeedNotKnown=$true;marker20SupportRoleImprovesButNoGlobalWinOrTimeGain=$true;coreCandidate0ImprovesOnlySelectedSeed41WithoutLinkedBonus=$true;twentySimultaneouslyFired=$false;markerBonusAlreadyIncludedInAlliedDamage=$true;decision='No product/stat/UI change: existing range and shared-fire lesson already describe the choice. Follow up a single spare spear moved toward the short north road when Range is absent.';pairs=$pairs}|ConvertTo-Json -Depth 12|Set-Content TestResults/north-marker-v0230-comparison.json
'PASS recorded nine models: fixed28 orders/1140/20 towers; only marker position; same cards and actual purchase times. No new simulation or product change.'
