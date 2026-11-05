param([switch]$UseRecorded,[switch]$Seed19)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$part=if($Seed19){'seed19'}else{'pilot'}
$output="TestResults/north-marker-v0230-$part.json"
if($UseRecorded){Get-Content -LiteralPath $output -Raw;exit}
if(Test-Path -LiteralPath $output){throw 'Recorded model exists; use -UseRecorded'}
foreach($r in(Get-Content TestResults/source-hashes-v0230.json -Raw|ConvertFrom-Json)){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Current product differs from verified v0230'}}
$root=Join-Path (Get-Location) "TestResults/north-marker-v0230-review/$part"
New-Item -ItemType Directory -Path $root -Force|Out-Null
foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $file.FullName -Destination $root}
Copy-Item Tests/NorthMarkerPositionReview.cs $root
Add-Type -Path (Get-ChildItem $root -Filter '*.cs').FullName
$ledger=Get-Content TestResults/v0230-native-purchase-ledger.json -Raw|ConvertFrom-Json
if($ledger.pricedSpend -ne 1140 -or $ledger.receipts.Count -ne 28 -or $ledger.finalTowersDerivedFromBuildsMinusFusions -ne 20){throw 'Native fixture scope differs'}
$fixture=@(foreach($r in $ledger.receipts){
    $line=$r.receipt;$c=[Gatefall.Tests.NorthMarkerPositionReview+Command]::new()
    if($line -match '^(\d+\.\d+) (build|upgrade) (\w+) @(\d+)$'){
        $c.Time=[double]::Parse($Matches[1],[Globalization.CultureInfo]::InvariantCulture);$c.Action=$Matches[2];$c.Kind=[Gatefall.Core.TowerKind][Enum]::Parse([Gatefall.Core.TowerKind],$Matches[3]);$c.Site=[int]$Matches[4]
    }elseif($line -match '^(\d+\.\d+) fusion \(Autocannon\+Binding\) -> Arc @17$'){
        $c.Time=[double]::Parse($Matches[1],[Globalization.CultureInfo]::InvariantCulture);$c.Action='fusion';$c.Kind=[Gatefall.Core.TowerKind]::Arc;$c.Site=17
    }else{throw 'Unrecognized current native receipt'};$c
})
$fixture|ConvertTo-Json -Depth 5|Set-Content (Join-Path $root commands.json)
$seeds=if($Seed19){[int[]]@(19)}else{[int[]]@(7,41)}
$rows=[Gatefall.Tests.NorthMarkerPositionReview]::Models($fixture,$seeds,[int[]]@(21,20,0))
[ordered]@{created=(Get-Date -Format o);task=244;newModels=$rows.Count;nativeSource='v0230-native-purchase-ledger.json all28';nativeSpend=1140;modelTowers=20;targetTimesRoundedToNativeTenths=$true;nativeExactSeedOrReplay=$false;cardPreferences=@('Range','Blast','Ballistic');fallback='first offered';manualSkillMovementSupportCalls=$false;currentTowerIdRecentMetric=$true;lifetimeSiteMetricIncludesConsumedMaterials=$true;markerBonusAlreadyIncludedInAlliedDamage=$true;onlyMarkerBuildAndUpgradeSiteChanged=$true;productChanged=$false;newApkOrAvd=$false;models=$rows}|ConvertTo-Json -Depth 8|Set-Content $output
# Preserve raw before any post-run comparison can reject it.
if(@($rows|Where-Object {$_.Spent -ne 1140 -or $_.CompletedOrders -ne 28 -or $_.Towers -ne 20}).Count){throw 'Actual spend/order completion differs; raw preserved for separate review'}
$rows|Select-Object Seed,MarkerSite,State,Core,Time,Spent,Credits,MarkerShots,MarkerDamage,MarkedHits,MarkBonus,FinalFiredTowers,PeakRecent3,MaxPurchaseDelay,@{n='Cards';e={$_.Cards -join '/'}}|Format-Table -AutoSize
