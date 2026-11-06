# Read recorded receipts only. Does not replay combat or operate a device.
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$r=Join-Path $root TestResults/chapter2-balance-20261106
Add-Type -Path (Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName
function Profile([string]$name){
 $p=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes("$r/$name"))
 if($null -eq $p -or -not $p.Valid()){throw "Invalid synthetic profile: $name"};return $p
}
$entry=Profile native-entry.sav
$battles=@()
foreach($wave in @(8,7)){
 $p=Profile "native-call$wave-result.sav"
 $log=Get-Content "$r/native-call$wave-last-run.txt" -Raw
 $stage=if($wave -eq 8){'2-6'}else{'2-3'}
 $expected=if($wave -eq 8){930}else{623}
 if($log -notmatch '^Gatefall 0\.30\.1\s+Won elapsed=([\d.]+) core=([\d.]+) hunter=([\d.]+)'){throw "Battle version/outcome differs: $wave"}
 $seconds=[double]$Matches[1];$core=[double]$Matches[2];$hunter=[double]$Matches[3]
 if($log -notmatch "allSpawned=True spawned=$expected remainingEnemies=0 bossDefeated=False" -or $log -notmatch "Context stage=$stage mode=Normal seed=(\d+)"){throw 'Wave victory/context differs'}
 $seed=[int]$Matches[1]
 if($log -notmatch 'Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=1 advancedSeconds=([\d.]+)'){throw 'Economy/call count differs'}
 $advance=[double]$Matches[1]
 if($log -notmatch "([\d.]+) wave=$wave advance=([\d.]+)"){throw 'Final call not recorded'}
 $callTime=[double]$Matches[1]
 $eventLog=Get-Content "$r/native-wave$wave-log.txt" -Raw
 if($eventLog -notmatch ('GATEFALL WAVE CALL index='+($wave-1)+' elapsed='+[regex]::Escape($callTime.ToString('0.00',[cultureinfo]::InvariantCulture)))){throw 'Native controller event missing'}
 $rewards=if($wave -eq 8){2}else{4}
 if($p.MagicCores -ne $entry.MagicCores+$rewards -or $p.LastReward -ne 2 -or $p.PendingRun -ne '' -or $p.PendingStage -ne -1 -or $p.PendingCrisis){throw 'Replay reward/pending differs'}
 foreach($f in @('Growth','Research','BestStars','CrisisStars','BestErosion','FirstCleared')){if(($p.$f -join ',') -cne ($entry.$f -join ',')){throw "Preserved profile field differs: $f"}}
 if($p.Loadout -ne $entry.Loadout -or $p.SelectedHunter -ne $entry.SelectedHunter){throw 'Loadout/hunter differs'}
 $battles+=@{stage=$stage;apk='0.30.1';finalWave=$wave;callAt=$callTime;advancedSeconds=$advance;seconds=$seconds;core=$core;hunter=$hunter;spawned=$expected;seed=$seed;reward=2}
}
$saveFiles=@('native-call7-result.sav','native-v0301-cold.sav','native-after-update.sav','native-v0302-cold.sav','native-ui-candidate0-abandon.sav','native-final-after-update.sav','native-v0302-abandon.sav','native-v0302-final-cold.sav')
$saveHash=(Get-FileHash "$r/$($saveFiles[0])").Hash.ToLowerInvariant()
foreach($f in $saveFiles){if((Get-FileHash "$r/$f").Hash.ToLowerInvariant() -cne $saveHash){throw "Update/abandon/cold save differs: $f"}}
$source=@(Get-Content "$r/product-before.json" -Raw|ConvertFrom-Json)
$changed=@($source|Where-Object{(Get-FileHash (Join-Path $root $_.path)).Hash.ToLowerInvariant() -cne $_.sha256})
if((($changed.path|Sort-Object) -join ',') -cne 'Assets/Scripts/Core/ChapterTwoCampaign.cs,Assets/Scripts/Presentation/MobileHud.cs'){throw 'Unexpected product source delta'}
if(@(Get-ChildItem "$root/Assets/Scripts/Core","$root/Assets/Scripts/Presentation" -Filter '*.cs' -Recurse).Count -ne $source.Count){throw 'Product source set differs'}
$before=Get-Content "$r/contracts-before.json" -Raw|ConvertFrom-Json
$after=Get-Content "$r/contracts-after.json" -Raw|ConvertFrom-Json
if($before.Count -ne 24 -or ($before.Hash -join ',') -cne ($after.Hash -join ',')){throw '24 gameplay data contracts differ'}
$models=Get-Content "$r/investment-raw.json" -Raw|ConvertFrom-Json
$follow=Get-Content "$r/investment-followup-raw.json" -Raw|ConvertFrom-Json
if($models.Count -ne 72 -or $follow.Count -ne 46 -or @($models|Where-Object Error).Count -or @($follow|Where-Object Error).Count){throw 'Recorded model set differs'}
$ui=Get-Content "$r/native-ui-v0302.json" -Raw|ConvertFrom-Json
if($ui.version -cne '0.30.2' -or $ui.fullBattle -or $ui.cityScreens.Count -ne 6 -or -not $ui.scatterArmorHint -or -not $ui.barrageArmorHint -or -not $ui.barrageRecipeTargets -or -not $ui.sealRecipeTargets -or -not $ui.liveMenus){throw 'UI observations incomplete'}
foreach($shot in $ui.screens){if(-not(Test-Path "$root/Docs/QA/v050/$shot.png")){throw "Missing observed screen $shot"}}
$apk=Get-Content "$r/apk-v0302.json" -Raw|ConvertFrom-Json
$oldInstalled=Get-Content "$r/installed-v0301.json" -Raw|ConvertFrom-Json
if($oldInstalled.sha256 -cne '6e98b1716e9fd97827a49d42394344a11d6fdb0e9be1be5ebf2eaf032d08b414'){throw 'Recorded battle install hash differs'}
$installed=Get-Content "$r/installed-v0302.json" -Raw|ConvertFrom-Json
$elf=Get-Content "$r/elf-v0302.json" -Raw|ConvertFrom-Json
$actual=(Get-FileHash "$root/Builds/Android/Gatefall-v0.30.2.apk").Hash.ToLowerInvariant()
if($actual -cne $apk.sha256 -or $actual -cne $installed.sha256 -or $actual -cne $ui.sha256 -or $actual -cne $elf.sha256 -or -not $elf.elfLoad16KB -or $apk.signature -cne 'verified' -or $apk.alignment16KB -cne 'verified'){throw 'APK receipts differ'}
if($actual -cne (Get-FileHash "$root/Builds/Android/Gatefall.apk").Hash.ToLowerInvariant()){throw 'APK alias differs'}
$cleanup=Get-Content "$r/cleanup.json" -Raw|ConvertFrom-Json
if($cleanup.serial -cne 'emulator-5698' -or -not $cleanup.normalEmulatorShutdown -or $cleanup.remainingOwnedProcesses -ne 0 -or $cleanup.protectedAndUserDevicesTouched){throw 'Owned device closeout incomplete'}
[ordered]@{status='PASS';version=$apk.version;versionCode=$apk.versionCode;bytes=$apk.bytes;sha256=$actual;battles=$battles;newVersionFullBattle=$false;newVersionUi=$true;staticGameplayContracts=24;modelConditions=72;followupConditionsIncludingTwoReplayControls=46;modelsPredateTextOnlyEdit=$true;installedHashMatches=$true;exactUpdateAbandonColdSave=$saveHash;preservedUserDevices=$true;scope='Two normal Android emulator battles on0.30.1, final7/8-wave controller events;0.30.2 city/role UI and exact update/abandon/cold saves. Fixed-policy growth/crisis comparisons are model evidence, not physical device/iOS/human fun/audio/FPS measurements';remaining='Broader pressure/placement-dependent capstone investment value and real-player/physical-device review;2-7+ outside this scope'}|ConvertTo-Json -Depth 8|Set-Content "$r/delivery.json" -Encoding utf8
Write-Output 'PASS Chapter2 balance delivery: recorded final calls, scoped models, preserved gameplay data, new role UI, installed APK and exact synthetic saves.'
