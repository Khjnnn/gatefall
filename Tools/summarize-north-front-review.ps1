$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$base=@((Get-Content TestResults/north-marker-v0230-pilot.json -Raw|ConvertFrom-Json).models)+@((Get-Content TestResults/north-marker-v0230-seed19.json -Raw|ConvertFrom-Json).models)
$rows=@((Get-Content TestResults/north-front-v0230-pilot.json -Raw|ConvertFrom-Json).models)+@((Get-Content TestResults/north-front-v0230-seed19.json -Raw|ConvertFrom-Json).models)
$pairs=@(foreach($r in $rows){
    $b=@($base|Where-Object {$_.Seed -eq $r.Seed -and $_.MarkerSite -eq 21})[0]
    foreach($a in @($b,$r)){if($a.State -ne 'Won' -or $a.Kills -ne 944 -or -not $a.AllSpawned -or $a.Remaining -ne 0 -or $a.Spent -ne 1140 -or $a.Credits -ne 46 -or $a.Towers -ne 20 -or $a.CompletedOrders -ne 28){throw 'Completion/fixture differs'}}
    $normalized=@($r.PurchaseLog|ForEach-Object {$_ -replace '(build Autocannon) @0$','$1 @12'})
    if(@(Compare-Object $b.PurchaseLog $normalized -SyncWindow 0).Count -or ($b.Cards -join '/') -ne ($r.Cards -join '/')){throw 'Only spear12 position should change'}
    for($i=0;$i -lt 28;$i++){if([math]::Abs($b.ActualTimes[$i]-$r.ActualTimes[$i]) -gt .0001){throw 'Actual purchase times differ'}}
    [ordered]@{seed=$r.Seed;baseline=$b;candidate=$r;actualTimesEqual0001=$true}
})
$lanes=Get-Content TestResults/north-front-v0230-lanes.json -Raw|ConvertFrom-Json
if(-not $lanes.equivalenceAuditPassed -or $lanes.equivalenceAuditPending -or $lanes.baseline.LaneDamage[0] -ne 0 -or $lanes.baseline.LaneDamage[2] -ne 0 -or $lanes.candidate.LaneDamage[0] -ne 0 -or $lanes.candidate.LaneDamage[2] -ne 0){throw 'Observed lane/equivalence differs'}
[ordered]@{created=(Get-Date -Format o);task=245;newCandidates=3;baseline3Reused=$true;extraReadOnlyEventObservations=2;productChanged=$false;newApkAvdNative=$false;onlyPurchaseMoved='98.3 build Autocannon12 to0';same28TimesPrices1140And20Towers=$true;nativeExactReplay=$false;seed41NorthDamage=@(184.2,37);seed41NorthHurtEvents=@(13,2);counterfactualCardSeedNotCausalProof=$true;decision='Keep product: existing range circle/short-north lesson allows strategic reinforcement. Next compare one expansion purchase against north upgrade under same budget cap.';pairs=$pairs;laneObservation=$lanes}|ConvertTo-Json -Depth 13|Set-Content TestResults/north-front-v0230-comparison.json
'PASS recorded three position pairs plus two read-only lane observations; no new simulation or product change.'
