# Read-only evidence audit; does not open a device, start combat, or modify an account file.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
function Require($ok,[string]$why){if(-not $ok){throw $why}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()}
foreach($file in (Get-Content "$root/product-hashes-v0285.json" -Raw|ConvertFrom-Json)){Require ((Hash $file.file) -ceq $file.sha256) ('Changed source '+$file.file)}
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -eq '0.28.5' -and $apk.versionCode -eq 122 -and $apk.sha256 -ceq '72b2af9614e6751d1b81db533b408d311cd88dd75bd9bf527925e6fd1641a2bc') 'Wrong current release'
foreach($file in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.28.5.apk',"$root/installed-v0285.apk")){Require ((Hash $file) -ceq $apk.sha256) ('APK differs '+$file)}
Require ((Hash 'Builds/Android/Gatefall-v0.28.4.apk') -ceq '12d801bffb65c7c65168499e7e112d7b2faf6352152074a567baec6008b32806') 'Previous stable APK changed'
$elf=Get-Content "$root/elf-v0285.json" -Raw|ConvertFrom-Json
Require ($apk.signature -eq 'verified' -and $apk.alignment16KB -eq 'verified' -and $elf.elfLoad16KB -and $elf.sha256 -ceq $apk.sha256) 'Package verification incomplete'
$font=Get-Content TestResults/growth-hint-font-v0285.json -Raw|ConvertFrom-Json
Require ($font.labels -eq 2712 -and $font.maxWidth -le 344 -and $font.maxHeight -le 22) 'Growth hint font validation incomplete'
Require ((Get-Content TestResults/account-growth-results.txt -Raw).Contains('65536 non-mutating selected-family research hints')) 'Growth selection checks missing'
$equivalence=Get-Content "$root/research-hint-equivalence-v0285.json" -Raw|ConvertFrom-Json
Require ($equivalence.early.equal -and $equivalence.early.cases -eq 26 -and $equivalence.lateEquivalent -and $equivalence.lateCaseCount -eq 1) 'Battle equivalence incomplete'
$delta=@(Get-Content "$root/core-delta-v0285.json" -Raw|ConvertFrom-Json)
Require ($delta.Count -eq 6 -and @($delta|Where-Object same).Count -eq 5 -and @($delta|Where-Object{-not $_.same})[0].file -eq 'Campaign.cs') 'Unexpected core changes'
foreach($name in @('competent-plan-review','degenerate-strategy-review')){$gate=(Get-Content ('TestResults/'+$name+'.json') -Raw|ConvertFrom-Json).gate;Require ($gate.enforced -and @($gate.violations).Count -eq 0) ('Strategy gate failed '+$name)}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
function Profile([string]$name){[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/$name-v0285.sav")))}
function Matches($expected,[string]$name){[Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($expected)) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $project "$root/$name-v0285.sav")))}
$before=Profile update-before
Require ($before.Valid() -and $before.MagicCores -eq 226 -and $before.StarsTotal -eq 39 -and $before.Loadout -eq 263 -and ($before.Research -join ',') -ceq '3,0,0,0' -and $before.Growth[0] -eq 5 -and -not $before.FirstCleared[13] -and $before.LastReward -eq 34) 'Initial synthetic account differs'
Require ((Hash "$root/update-before-v0285.sav") -ceq (Hash "$root/update-after-v0285.sav")) 'Update changed account'
$service=[Gatefall.Core.CampaignService]::new($before,$null)
Require ($service.Research(2) -and $service.Research(2) -and (Matches $service.Profile after-blast)) 'Blast purchase/account preservation differs'
Require ($service.Research(1) -and $service.Research(1) -and (Matches $service.Profile after-control)) 'Control purchase/account preservation differs'
$after=$service.Profile.Copy();Require ($after.MagicCores -eq 214 -and ($after.Research -join ',') -ceq '3,2,2,0') 'Research costs or ranks differ'
$ward=$after.Copy();$ward.Loadout=387;Require (Matches $ward with-ward) 'Loadout changed unrelated fields'
foreach($file in @('after-loadout','relaunch','abandon','relaunch-after-abandon')){Require (Matches $after $file) ('Cold/restore/abandon changed account '+$file)}
$pending=Profile pending;Require ($pending.PendingStage -eq 13 -and -not $pending.PendingCrisis -and $pending.PendingRun -ne '') 'Preparation was not normal1-14'
$expectedPending=$after.Copy();$expectedPending.PendingRun=$pending.PendingRun;$expectedPending.PendingStage=13
Require (Matches $expectedPending pending) 'Preparation changed unrelated fields'
$native=Get-Content "$root/research-hint-native-observations-v0285.json" -Raw|ConvertFrom-Json
Require ($native.version -eq '0.28.5' -and -not $native.fullBattle -and $native.runningCombatSeconds -eq 0 -and $native.researchSpent -eq 12 -and $native.newReward -eq 0) 'Native scope differs'
foreach($screen in $native.screenEvidence){Require ((Hash $screen.file) -ceq $screen.sha256) 'Native screenshot changed/missing'}
$cleanup=Get-Content "$root/cleanup-v0285.json" -Raw|ConvertFrom-Json
Require ($cleanup.normalRestored -and $cleanup.normalShutdown -and $cleanup.remainingOwnedProcesses -eq 0 -and $cleanup.reparsePoints -eq 0) 'Owned emulator cleanup incomplete'
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;earlyEquivalent=26;lateEquivalent=1;researchHintSelections=65536;fontLabels=2712;fontMaxWidth=$font.maxWidth;
    nativeStage='Normal1-14 preparation only';nativeFullBattle=$false;runningCombatSeconds=0;researchPurchases=4;researchSpent=12;coresBefore=226;coresAfter=214;stars=39;historicalReward=34;newReward=0;restoredLoadout=263;
    updateBytes=(Get-Item "$root/update-before-v0285.sav").Length;updateHash=(Hash "$root/update-before-v0285.sav");updatedSame=$true;postPurchaseBytes=(Get-Item "$root/relaunch-after-abandon-v0285.sav").Length;postPurchaseHash=(Hash "$root/relaunch-after-abandon-v0285.sav");restoredAbandonColdSame=$true;
    scope='City guidance/purchase/loadout/three-size missing-family hint and normal preparation/abandonment. No new APK whole-battle victory, human fun, physical-device/iOS/audio or performance claim.'
}|ConvertTo-Json -Depth 6|Set-Content "$root/delivery-v0285.json" -Encoding utf8
'PASS0.28.5:65536 guidance cases,2712 font labels,26early+1late equality,package/install16KB,native research purchases/loadout/preparation and exact account preservation'
