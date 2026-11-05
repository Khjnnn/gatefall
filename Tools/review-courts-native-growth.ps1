param([ValidateSet(7,19,41)][int]$Seed=7,[ValidateSet('control','late-upgrade')][string]$Choice='control',[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$output="TestResults/courts-native-v0240-$Choice-seed$Seed.json"
if($UseRecorded){$r=Get-Content $output -Raw|ConvertFrom-Json;$r.model|Select-Object Seed,Choice,State,Core,Time,Spent,Credits,Towers,FinalFired,RecentPeak,MaxDelay,Cards|ConvertTo-Json -Depth 5;exit}
if(Test-Path $output){throw 'Preserve recorded model; use -UseRecorded'}
foreach($r in(Get-Content TestResults/source-hashes-v0240-candidate.json -Raw|ConvertFrom-Json)){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Verified product changed'}}
$root=Join-Path (Get-Location) "TestResults/courts-native-v0240-$Choice-seed$Seed-review"
if(Test-Path $root){throw 'Preserve existing snapshot'}
New-Item -ItemType Directory -Path $root|Out-Null
foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $file.FullName -Destination $root}
Copy-Item Tests/CourtsNativeGrowthReview.cs $root
Add-Type -Path (Get-ChildItem $root -Filter '*.cs').FullName
$ledger=Get-Content TestResults/v0240-native-purchase-ledger.json -Raw|ConvertFrom-Json
if($ledger.pricedSpend -ne 1095 -or $ledger.receipts.Count -ne 26 -or $ledger.finalTowersDerivedFromBuildsMinusFusions -ne 18){throw 'Native fixture differs'}
$fixture=@(foreach($r in $ledger.receipts){
    $line=$r.receipt;$c=[Gatefall.Tests.CourtsNativeGrowthReview+Command]::new()
    if($line -match '^(\d+\.\d+) (build|upgrade) (\w+) @(\d+)$'){$c.Time=[double]::Parse($Matches[1],[Globalization.CultureInfo]::InvariantCulture);$c.Action=$Matches[2];$c.Kind=[Gatefall.Core.TowerKind][Enum]::Parse([Gatefall.Core.TowerKind],$Matches[3]);$c.Site=[int]$Matches[4]}
    elseif($line -match '^(\d+\.\d+) fusion \(Autocannon\+(Binding|Rupture)\) -> (Arc|Shatter) @(\d+)$'){$c.Time=[double]::Parse($Matches[1],[Globalization.CultureInfo]::InvariantCulture);$c.Action='fusion';$c.Kind=[Gatefall.Core.TowerKind][Enum]::Parse([Gatefall.Core.TowerKind],$Matches[3]);$c.Site=[int]$Matches[4]}
    else{throw 'Unknown captured receipt'};$c
})
$fixture|ConvertTo-Json -Depth 5|Set-Content (Join-Path $root commands.json)
$row=[Gatefall.Tests.CourtsNativeGrowthReview]::Run($fixture,$Seed,($Choice -eq 'late-upgrade'))
[ordered]@{created=(Get-Date -Format o);task=252;newModels=1;actualNativeFixture='v0240-native-purchase-ledger.json';seedIsNotNativeRecordedSeed=$true;targetTimesNativeTenths=$true;maxSpend1095=$true;manualSkillMovementSupportCalls=$false;cardPreferences=@('Ballistic','Range','Ballistic');fallback='first offered';kindDamageIncludesConsumedMaterials=$true;siteShotsIncludeConsumedMaterials=$true;finalTowerShotsUseCurrentId=$true;productChanged=$false;newApkAvdNative=$false;model=$row}|ConvertTo-Json -Depth 9|Set-Content $output
if($row.CompletedOrders -ne 26 -or $row.Spent -ne $(if($Choice -eq 'control'){1095}else{1085}) -or $row.Towers -ne $(if($Choice -eq 'control'){18}else{17})){throw 'Raw preserved: actual orders/spend/count differ'}
$row|Select-Object Seed,Choice,State,Core,Time,Spent,Credits,Towers,FinalFired,RecentPeak,MaxDelay,Cards|ConvertTo-Json -Depth 5
