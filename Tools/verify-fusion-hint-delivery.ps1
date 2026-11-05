# Audit recorded 0.28.9 delivery only. No build, battle, device input or save mutation.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
function Require($ok,[string]$why){if(-not $ok){throw $why}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()}
function Decode([string]$name){[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/$name-v0289.sav")))}
foreach($file in (Get-Content "$root/product-hashes-v0289.json" -Raw|ConvertFrom-Json)){Require ((Hash $file.file) -ceq $file.sha256) ('Product changed '+$file.file)}
$delta=@(Get-Content "$root/product-delta-v0289.json" -Raw|ConvertFrom-Json)
Require (@($delta|Where-Object{-not $_.same}).Count -eq 2 -and @($delta|Where-Object{$_.file -match '[\\/]Presentation[\\/]' -and -not $_.same}).Count -eq 0) 'Unexpected product/UI delta'
$preflight=Get-Content "$root/fusion-hint-preflight-v0289.json" -Raw|ConvertFrom-Json
$campaign=Get-Content Assets/Scripts/Core/Campaign.cs -Raw
Require ($campaign.Contains($preflight.candidate) -and $campaign.Replace($preflight.candidate,$preflight.old) -ceq (Get-Content "$root/Campaign-before-v0289.cs" -Raw)) 'Campaign change exceeds one Lesson string'
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -ceq '0.28.9' -and $apk.versionCode -eq 126 -and $apk.bytes -eq 100458217 -and $apk.sha256 -ceq '0df65cd475ed0bcb556adc438653e64cf91a0e37d29dbe5ff896b5ab3fb82da2') 'Wrong delivery APK'
foreach($name in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.28.9.apk',"$root/installed-v0289.apk")){Require ((Hash $name) -ceq $apk.sha256) ('APK hash differs '+$name)}
Require ((Hash 'Builds/Android/Gatefall-v0.28.8.apk') -ceq '4603a243df9e2432a1666ea6b5a5f244702e3fefa83f23e9b2ef108d700a6685') 'Prior stable changed'
$elf=Get-Content "$root/elf-v0289.json" -Raw|ConvertFrom-Json
Require ($apk.signature -ceq 'verified' -and $apk.alignment16KB -ceq 'verified' -and $elf.elfLoad16KB -and $elf.nativeLibraries.Count -eq 3 -and $elf.sha256 -ceq $apk.sha256) 'Package checks incomplete'
foreach($name in @('competent-plan-review','degenerate-strategy-review')){$gate=(Get-Content ('TestResults/'+$name+'.json') -Raw|ConvertFrom-Json).gate;Require ($gate.enforced -and @($gate.violations).Count -eq 0) ('Strategy gate failed '+$name)}
$font=(Get-Content TestResults/city-briefing-font-v0289.json -Raw|ConvertFrom-Json).rows|Where-Object{$_.stage -ceq '1-10'}
Require ($font.deploymentHeight -le 70 -and $font.deploymentHeight -gt 0) 'City Lesson exceeds actual font area'
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$a=Decode 'update-before'
Require ($a.Valid() -and $a.MagicCores -eq 214 -and $a.StarsTotal -eq 39 -and $a.Growth[0] -eq 5 -and ($a.Research -join ',') -ceq '3,2,2,0') 'City-only synthetic A differs'
foreach($name in @('update-after','cold-city')){Require ((Hash "$root/$name-v0289.sav") -ceq (Hash "$root/update-before-v0289.sav")) ('City A save changed '+$name)}
$b=Decode 'stage10-native-before';$pending=Decode 'stage10-native-pending';$after=Decode 'stage10-native-result'
Require ($b.Valid() -and $b.MagicCores -eq 120 -and $b.StarsTotal -eq 27 -and $b.Growth[0] -eq 3 -and ($b.Research -join ',') -ceq '2,2,2,2' -and $b.Loadout -eq 135 -and -not $b.FirstCleared[9] -and -not $b.IsStageOpen(10)) 'Separate economic synthetic B differs'
Require ((Hash "$root/stage10-native-loaded-v0289.sav") -ceq (Hash "$root/stage10-native-before-v0289.sav")) 'B injection/loaded snapshot differs'
Require ($pending.Valid() -and $pending.PendingStage -eq 9 -and -not $pending.PendingCrisis -and $pending.PendingRun.Length -eq 32) 'Prepared stage/mode/ID differs'
$expected=$b.Copy();$expected.PendingRun=$pending.PendingRun;$expected.PendingStage=9
Require ([Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($expected)) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $project "$root/stage10-native-pending-v0289.sav")))) 'Preparation changed unrelated B fields'
$expected=$b.Copy();$expected.MagicCores+=28;$expected.BestStars[9]=3;$expected.FirstCleared[9]=$true;$expected.BestErosion[9]=0;$expected.TutorialMask=512;$expected.LastReward=28;$expected.LastRewardRun=$pending.PendingRun
Require ($after.Valid() -and $after.IsStageOpen(10) -and $after.PendingRun -ceq '' -and $after.PendingStage -eq -1 -and -not $after.PendingCrisis) 'Result progress/pending invalid'
Require ([Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($expected)) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $project "$root/stage10-native-result-v0289.sav")))) 'Result changed unrelated profile fields'
foreach($name in @('map','cold')){Require ((Hash "$root/stage10-native-$name-v0289.sav") -ceq (Hash "$root/stage10-native-result-v0289.sav")) ('Result save changed '+$name)}
$run=Get-Content "$root/last-run-v0289-stage10.txt" -Raw
Require ($run.Contains('Won elapsed=201.66 core=300 hunter=124 kills=864') -and $run.Contains('allSpawned=True spawned=864 remainingEnemies=0 bossDefeated=False') -and $run.Contains('Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0') -and $run.Contains('stage=1-10 mode=Normal seed=827504 hunter=0 growth=3 research=2,2,2,2 loadout=135 cards=2,0,0,1,0,0')) 'Terminal/context/caps differ'
$purchases=@($run -split '\r?\n'|Where-Object{$_ -match '^\d+\.\d+ (build|upgrade|fusion) '})
Require ($purchases.Count -eq 25 -and @($purchases|Where-Object{$_ -match ' build '}).Count -eq 15 -and @($purchases|Where-Object{$_ -match ' upgrade '}).Count -eq 9 -and @($purchases|Where-Object{$_ -match ' fusion '}).Count -eq 1) 'Actual purchase ledger differs'
$spend=0;$levels=@{};$count=0
foreach($line in $purchases){
    if($line -match ' build (\w+) @(\d+)$'){$kind=[Enum]::Parse([Gatefall.Core.TowerKind],$Matches[1]);$site=[int]$Matches[2];$spend += [Gatefall.Core.TowerCatalog]::Get($kind).BuildCost;$levels[$site]=@{kind=$kind;level=1};$count++}
    elseif($line -match ' upgrade (\w+) @(\d+)$'){$site=[int]$Matches[2];$spend += [Gatefall.Core.TowerCatalog]::Get($levels[$site].kind).UpgradePrice($levels[$site].level);$levels[$site].level++}
    elseif($line -match ' fusion \(Rupture\+Autocannon\) -> Shatter @15$'){$spend+=45;$levels.Remove(13);$levels[15]=@{kind=[Gatefall.Core.TowerKind]::Shatter;level=1};$count--}
    else{throw 'Unexpected ledger command'}
}
Require ($spend -eq 1030 -and $count -eq 14 -and $levels.Count -eq 14) 'Catalog price/final count reconciliation failed'
$native=Get-Content "$root/fusion-hint-native-v0289.json" -Raw|ConvertFrom-Json
Require ($native.stage10.fullBattle -and $native.stage10.spent -eq $spend -and $native.stage10.finalTowers -eq 14 -and $native.stage10.cards -ceq 'Ballistic,Range,Ballistic') 'Recorded native scope differs'
foreach($screen in $native.screenEvidence){Require ((Hash $screen.file) -ceq $screen.sha256) 'Screenshot missing/changed'}
$cleanup=Get-Content "$root/cleanup-v0289.json" -Raw|ConvertFrom-Json
Require ($cleanup.normalRestored -and $cleanup.normalShutdown -and $cleanup.remainingOwnedProcesses -eq 0 -and $cleanup.reparsePoints -eq 0) 'Owned cleanup incomplete'
$diag=Get-Content "$root/diagnostics-v0289.json" -Raw|ConvertFrom-Json
Require ($diag.finalPids.Count -eq 4 -and @($diag.finalPids|Where-Object{$_.fatalLines -ne 0}).Count -eq 0 -and $diag.retainedPackageAnrLines -eq 0) 'Retained process diagnostics differ'
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;productFilesChanged=2;change='Stage1-10 Lesson text/version only';nativeFullStage='1-10 Normal';seed=827504;elapsed=201.66;spawned=864;remaining=0;core=300;hunter=124;builds=15;upgrades=9;fusions=1;spent=$spend;finalTowers=14;stars=3;reward=28;profileBCoresBefore=120;profileBCoresAfter=148;profileBStarsAfter=30;profileBSaveHash=(Hash "$root/stage10-native-cold-v0289.sav");profileBSaveBytes=324;separateProfileAUnchanged=$true;guideScreens=3;scope='One current APK first-clear Android emulator battle plus separate city UI/update test. Not all stages/crises,all towers firing,human fun,physical Android/iOS,audio or performance proof. Both representative clips retain a right-side construction menu. Startup/EGL/Surface unresolved.'}|ConvertTo-Json -Depth 5|Set-Content "$root/delivery-v0289.json" -Encoding utf8
'PASS0.28.9 Lesson-only/package/font/nativeStage10/actual-ledger/first-reward/unlock/two-separate-synthetic-profiles/cold-preservation/owned-cleanup'
