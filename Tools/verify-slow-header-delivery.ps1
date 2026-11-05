# Read-only delivery audit. Does not start a device, battle, build or edit a player save.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
function Require($ok,[string]$why){if(-not $ok){throw $why}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()}
foreach($file in (Get-Content "$root/product-hashes-v0286.json" -Raw|ConvertFrom-Json)){Require ((Hash $file.file) -ceq $file.sha256) ('Changed source '+$file.file)}
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -eq '0.28.6' -and $apk.versionCode -eq 123 -and $apk.sha256 -ceq 'acf8bcb4368d02d3677ef3c27ec10a690bdf38f211c9c7799354a297c7dd29cc') 'Wrong release'
foreach($file in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.28.6.apk',"$root/installed-v0286.apk")){Require ((Hash $file) -ceq $apk.sha256) ('APK differs '+$file)}
Require ((Hash 'Builds/Android/Gatefall-v0.28.5.apk') -ceq '72b2af9614e6751d1b81db533b408d311cd88dd75bd9bf527925e6fd1641a2bc') 'Previous stable APK changed'
$elf=Get-Content "$root/elf-v0286.json" -Raw|ConvertFrom-Json
Require ($apk.signature -eq 'verified' -and $apk.alignment16KB -eq 'verified' -and $elf.elfLoad16KB -and $elf.sha256 -ceq $apk.sha256) 'Package verification incomplete'
$font=Get-Content TestResults/slow-resistance-font-v0286.json -Raw|ConvertFrom-Json
Require ($font.titles -eq 16 -and $font.maxTitleWidth -le 170 -and $font.hintActualWidth -le 158 -and $font.maxHeight -le 28) 'Actual font fit incomplete'
$delta=@(Get-Content "$root/core-delta-v0286.json" -Raw|ConvertFrom-Json)
Require ($delta.Count -eq 6 -and @($delta|Where-Object same).Count -eq 6) 'Combat core changed'
foreach($name in @('competent-plan-review','degenerate-strategy-review')){$gate=(Get-Content ('TestResults/'+$name+'.json') -Raw|ConvertFrom-Json).gate;Require ($gate.enforced -and @($gate.violations).Count -eq 0) ('Strategy gate failed '+$name)}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$beforeBytes=[IO.File]::ReadAllBytes((Join-Path $project "$root/update-before-v0286.sav"))
$before=[Gatefall.Core.CampaignStore]::Decode($beforeBytes)
Require ($before.Valid() -and $before.MagicCores -eq 214 -and $before.StarsTotal -eq 39 -and $before.Loadout -eq 263 -and ($before.Research -join ',') -ceq '3,2,2,0' -and $before.Growth[0] -eq 5 -and -not $before.FirstCleared[13] -and $before.LastReward -eq 34 -and $before.PendingRun -eq '' -and -not $before.PendingCrisis) 'Synthetic initial account differs'
foreach($name in @('update-after','abandon','cold')){Require ((Hash "$root/$name-v0286.sav") -ceq (Hash "$root/update-before-v0286.sav")) ('Save changed '+$name)}
$pending=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/pending-v0286.sav")))
Require ($pending.Valid() -and $pending.PendingStage -eq 13 -and $pending.PendingRun -ne '' -and -not $pending.PendingCrisis) 'Native stage not normal1-14'
$expected=$before.Copy();$expected.PendingRun=$pending.PendingRun;$expected.PendingStage=13
Require ([Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($expected)) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $project "$root/pending-v0286.sav")))) 'Running changed unrelated account fields'
$native=Get-Content "$root/slow-header-native-v0286.json" -Raw|ConvertFrom-Json
Require ($native.version -eq '0.28.6' -and -not $native.fullBattle -and $native.combatSpent -eq 275 -and $native.readyTowers -eq 3 -and $native.newReward -eq 0 -and $native.cardsChosen -eq 0 -and $native.firstLiveClock -eq '0:08' -and $native.secondLiveClock -eq '0:14') 'Native scope differs'
foreach($screen in $native.screenEvidence){Require ((Hash $screen.file) -ceq $screen.sha256) 'Screenshot changed/missing'}
$cleanup=Get-Content "$root/cleanup-v0286.json" -Raw|ConvertFrom-Json
Require ($cleanup.normalRestored -and $cleanup.normalShutdown -and $cleanup.remainingOwnedProcesses -eq 0 -and $cleanup.reparsePoints -eq 0) 'Owned cleanup incomplete'
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;coreFilesUnchanged=6;fontSlowTitles=16;nativeHeaderKinds=@('Binding','Autocannon');nativeFullBattle=$false;nativeApproxSeconds=15;builds=3;upgrades=3;towers=3;combatSpent=275;newReward=0;accountCores=214;normalStars=39;historicalReward=34;saveBytes=$beforeBytes.Length;updateAbandonColdSame=$true;saveHash=(Hash "$root/cold-v0286.sav");scope='Binding Lv1/Lv2 three-size header fit, no-hint spear control, LIVE0:08→0:14, normal abandonment/cold preservation; other slow family fonts checked but not Android menus. No full battle, human fun, physical Android/iOS/audio or performance claim.'}|ConvertTo-Json -Depth 5|Set-Content "$root/delivery-v0286.json" -Encoding utf8
'PASS0.28.6:6cores unchanged,16 actual-font titles,three-size Binding menus,LIVE and upgrade/close,package/install16KB,update/abandon/cold exact save preservation'
