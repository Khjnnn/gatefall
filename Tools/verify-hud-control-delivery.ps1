# Read-only audit of recorded 0.28.10 evidence. Does not build, simulate or operate a device.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/hud-investment-20261106'
function Require($ok,[string]$why){if(-not $ok){throw $why}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()}
function Decode([string]$name){[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/$name.sav")))}
function ExactProfile($expected,[string]$name){Require ([Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($expected)) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $project "$root/$name.sav")))) ('Unexpected profile mutation '+$name)}
foreach($file in (Get-Content "$root/product-hashes-v02810.json" -Raw|ConvertFrom-Json)){Require ((Hash $file.file) -ceq $file.sha256) ('Product changed '+$file.file)}
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -ceq '0.28.10' -and $apk.versionCode -eq 127 -and $apk.bytes -eq 104812910 -and $apk.sha256 -ceq '12b49530f1e339c6ca698cb4e95665cd103c2c27dc6e3c7ffe9bb7939d5e1330') 'Wrong final APK'
foreach($path in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.28.10.apk',"$root/installed-v02810.apk")){Require ((Hash $path) -ceq $apk.sha256) ('APK mismatch '+$path)}
Require ((Hash 'Builds/Android/Gatefall-v0.28.9.apk') -ceq '0df65cd475ed0bcb556adc438653e64cf91a0e37d29dbe5ff896b5ab3fb82da2') 'Previous stable APK changed'
$elf=Get-Content "$root/elf-v02810-final.json" -Raw|ConvertFrom-Json
Require ($apk.signature -ceq 'verified' -and $apk.alignment16KB -ceq 'verified' -and $elf.elfLoad16KB -and $elf.nativeLibraries.Count -eq 3 -and $elf.sha256 -ceq $apk.sha256) 'Package checks incomplete'
foreach($name in @('competent-plan-review','degenerate-strategy-review')){$gate=(Get-Content ('TestResults/'+$name+'.json') -Raw|ConvertFrom-Json).gate;Require ($gate.enforced -and @($gate.violations).Count -eq 0) ('Strategy gate failed '+$name)}
Require ((Get-Content TestResults/mobile-results.txt -Raw).Contains('PASS: 32')) 'Mobile/HUD checks missing'
$before=@(Get-Content "$root/early-before.json" -Raw|ConvertFrom-Json);$after=@(Get-Content "$root/early-after.json" -Raw|ConvertFrom-Json)
Require ($before.Count -eq 12 -and $after.Count -eq 12) 'Early comparison count differs'
for($i=0;$i -lt 12;$i++){foreach($field in @('Stage','Seed','Frames','Crisis','Hash','Outcome')){Require ($before[$i].$field -ceq $after[$i].$field) ('Early state mismatch '+$i+'/'+$field)}}
$stats=Get-Content "$root/stats-scope.json" -Raw|ConvertFrom-Json
Require ($stats.statCells -eq 434 -and $stats.changed.Count -eq 16 -and $stats.unchangedOtherFields -and @($stats.changed|Where-Object{$_.field -cne 'MaxTargets' -or $_.kind -ne 1 -or $_.stage -lt 6 -or $_.level -notin @(2,3)}).Count -eq 0 -and (Hash 'Assets/Scripts/Core/TowerCatalog.cs') -ceq $stats.pricesRecipesHash) 'Binding scope/catalog changed'
$font=Get-Content TestResults/core-status-font-v02810.json -Raw|ConvertFrom-Json
Require ($font.labels -eq 7 -and $font.maxNameWidth -le 88 -and $font.maxNameHeight -le 17 -and $font.maxHpWidth -le 40 -and $font.maxHpHeight -le 21 -and $font.sealRoleWidth -le 150 -and $font.sealRoleHeight -le 20) 'Actual font exceeds HUD area'
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$a=Decode 'update-before-v02810'
Require ($a.Valid() -and $a.MagicCores -eq 214 -and $a.StarsTotal -eq 39 -and $a.Growth[0] -eq 5 -and ($a.Research -join ',') -ceq '3,2,2,0') 'Synthetic A differs'
foreach($name in @('update-after-v02810','candidate0-abandon','before-final-update','after-final-update')){Require ((Hash "$root/$name.sav") -ceq (Hash "$root/update-before-v02810.sav")) ('Update/Ready A changed '+$name)}
$lost=Decode 's10-ui-lost';$pendingA=Decode 's10-pending'
# s10-pending was the earlier rejected Ready attempt. The final two-tower trial's
# starting ID was not extracted; audit its retained result ID without inventing a before snapshot.
Require ($pendingA.PendingStage -eq 9 -and -not $pendingA.PendingCrisis -and $lost.LastRewardRun -cne $pendingA.PendingRun) 'Separated Ready/loss attempts conflated'
$expected=$a.Copy();$expected.LastReward=0;$expected.LastRewardRun='8cc43b711ace4154bd92f8ec47488097'
ExactProfile $expected 's10-ui-lost'
foreach($name in @('s10-retry-abandon','s12-abandon')){Require ((Hash "$root/$name.sav") -ceq (Hash "$root/s10-ui-lost.sav")) ('Partial abandon changed A '+$name)}
$bindingPending=Decode 's12-crisis-pending'
Require ($bindingPending.Valid() -and $bindingPending.PendingStage -eq 11 -and $bindingPending.PendingCrisis -and $bindingPending.PendingRun.Length -eq 32) 'Binding UI was not actual crisis stage12'
$c=Decode 'profileC-before';$pending=Decode 's14-pending';$result=Decode 'profileC-result'
Require ($c.Valid() -and $c.MagicCores -eq 244 -and $c.StarsTotal -eq 39 -and $c.Growth[0] -eq 4 -and ($c.Research -join ',') -ceq '2,1,1,1' -and $c.Loadout -eq 135 -and -not $c.FirstCleared[13] -and $c.TutorialMask -eq 0 -and $c.StoryMask -eq 0) 'Separate economic synthetic C differs'
Require ((Hash "$root/profileC-before.sav") -ceq (Hash "$root/profileC-loaded.sav")) 'C loaded snapshot differs'
Require ($pending.Valid() -and $pending.PendingStage -eq 13 -and -not $pending.PendingCrisis -and $pending.PendingRun.Length -eq 32) 'C prepared mode/ID differs'
$expected=$c.Copy();$expected.PendingRun=$pending.PendingRun;$expected.PendingStage=13;ExactProfile $expected 's14-pending'
$expected=$c.Copy();$expected.MagicCores+=36;$expected.BestStars[13]=3;$expected.FirstCleared[13]=$true;$expected.BestErosion[13]=0;$expected.TutorialMask=8192;$expected.LastReward=36;$expected.LastRewardRun=$pending.PendingRun
ExactProfile $expected 'profileC-result'
Require ($result.Valid() -and $result.MagicCores -eq 280 -and $result.StarsTotal -eq 42 -and $result.PendingRun -ceq '' -and $result.PendingStage -eq -1 -and -not $result.PendingCrisis) 'C result progression differs'
foreach($name in @('profileC-map','profileC-cold')){Require ((Hash "$root/$name.sav") -ceq (Hash "$root/profileC-result.sav")) ('C result persistence differs '+$name)}
$run=Get-Content "$root/last-run-v02810-stage14.txt" -Raw
Require ($run.Contains('Won elapsed=193.43 core=300 hunter=132 kills=944') -and $run.Contains('fusions=2') -and $run.Contains('allSpawned=True spawned=944 remainingEnemies=0 bossDefeated=False') -and $run.Contains('Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0') -and $run.Contains('stage=1-14 mode=Normal seed=1747464 hunter=0 growth=4 research=2,1,1,1 loadout=135 cards=1,0,0,0,0,0')) 'Native terminal/context/caps differ'
$purchases=@($run -split '\r?\n'|Where-Object{$_ -match '^\d+\.\d+ (build|upgrade|fusion) '})
Require ($purchases.Count -eq 21 -and @($purchases|Where-Object{$_ -match ' build '}).Count -eq 12 -and @($purchases|Where-Object{$_ -match ' upgrade '}).Count -eq 7 -and @($purchases|Where-Object{$_ -match ' fusion '}).Count -eq 2) 'Native ledger counts differ'
$spend=0;$levels=@{}
foreach($line in $purchases){
    if($line -match ' build (\w+) @(\d+)$'){$kind=[Enum]::Parse([Gatefall.Core.TowerKind],$Matches[1]);$site=[int]$Matches[2];Require (-not $levels.ContainsKey($site)) 'Build occupied site';$spend += [Gatefall.Core.TowerCatalog]::Get($kind).BuildCost;$levels[$site]=@{kind=$kind;level=1}}
    elseif($line -match ' upgrade (\w+) @(\d+)$'){$site=[int]$Matches[2];Require ($levels.ContainsKey($site) -and $levels[$site].kind.ToString() -ceq $Matches[1]) 'Upgrade kind differs';$spend += [Gatefall.Core.TowerCatalog]::Get($levels[$site].kind).UpgradePrice($levels[$site].level);$levels[$site].level++}
    elseif($line -match ' fusion \(Autocannon\+Binding\) -> Arc @4$'){Require ($levels[4].level -eq 2 -and $levels[1].kind -eq [Gatefall.Core.TowerKind]::Binding -and $levels[1].level -eq 2) 'Arc materials differ';$spend+=40;$levels.Remove(1);$levels[4]=@{kind=[Gatefall.Core.TowerKind]::Arc;level=1}}
    elseif($line -match ' fusion \(\(Autocannon\+Binding\)\+Rupture\) -> Seal @4$'){Require ($levels[4].kind -eq [Gatefall.Core.TowerKind]::Arc -and $levels[4].level -eq 2 -and $levels[7].kind -eq [Gatefall.Core.TowerKind]::Rupture -and $levels[7].level -eq 2) 'Seal materials differ';$spend+=80;$levels.Remove(7);$levels[4]=@{kind=[Gatefall.Core.TowerKind]::Seal;level=1}}
    else{throw 'Unknown native purchase'}
}
Require ($spend -eq 905 -and $levels.Count -eq 10 -and $levels[4].kind -eq [Gatefall.Core.TowerKind]::Seal -and $levels[7].kind -eq [Gatefall.Core.TowerKind]::Autocannon) 'Spend/Seal/refill reconciliation failed'
$native=Get-Content "$root/native-observations-v02810.json" -Raw|ConvertFrom-Json
Require ($native.stage14.fullBattle -and $native.stage14.spent -eq $spend -and $native.stage14.cards -ceq 'Ballistic,Credits,Credits' -and $native.stage14.instantCardCredits -eq 70 -and $native.bindingPartial.maxTargets -eq 8 -and -not $native.bindingPartial.fullBattle -and $native.hudBodyScreens -eq 3 -and $native.hudReadyBadgeScreens -eq 3) 'Native scope record differs'
foreach($screen in $native.screenEvidence){Require ((Hash $screen.file) -ceq $screen.sha256) ('Screenshot differs '+$screen.file)}
$cleanup=Get-Content "$root/cleanup-v02810.json" -Raw|ConvertFrom-Json
Require ($cleanup.normalRestored -and $cleanup.normalShutdown -and $cleanup.remainingOwnedProcesses -eq 0 -and $cleanup.reparsePoints -eq 0) 'Owned cleanup incomplete'
$diag=Get-Content "$root/diagnostics-v02810.json" -Raw|ConvertFrom-Json
Require ($diag.finalPids.Count -eq 3 -and @($diag.finalPids|Where-Object{$_.fatalLines -ne 0}).Count -eq 0 -and $diag.retainedPackageAnrLines -eq 0 -and $diag.retainedPackageCrashLines -eq 0) 'Retained diagnostics differ'
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;earlyFramePairs=12;statCells=434;changedMaxTargets=16;hudProjectionCases=42;hudBodyScreens=3;hudReadyBadgeScreens=3;nativeFullStage='1-14 Normal';seed=1747464;elapsed=193.43;spawned=944;remaining=0;core=300;hunter=132;builds=12;upgrades=7;fusions=2;spent=$spend;finalTowers=10;stars=3;reward=36;profileCCoresBefore=244;profileCCoresAfter=280;profileCStarsAfter=42;profileCSaveHash=(Hash "$root/profileC-cold.sav");profileCSaveBytes=324;updateAUnchanged=$true;partialALossReward=0;bindingPartialSeconds=28;ownedProcesses=0;scope='Current APK one normal stage14 emulator full battle plus separate HUD and binding partial tests. Candidate0 rejected; stage10 two-tower UI trial lost and was not a victory. Not all operations/crises, universal investment superiority, all towers simultaneously firing, human fun, physical Android/iOS, audio or performance. Startup/EGL/Surface unresolved.'}|ConvertTo-Json -Depth 5|Set-Content "$root/delivery-v02810.json" -Encoding utf8
'PASS0.28.10 HUD/body-badges/scoped-binding/package/nativeSeal-refill/ledger/reward/unlock/update-cold-preservation/owned-cleanup'
