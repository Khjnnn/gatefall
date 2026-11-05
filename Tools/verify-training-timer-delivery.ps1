$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$meta=Get-Content TestResults/android-apk-v0243-candidate.json -Raw|ConvertFrom-Json
if($meta.version -ne '0.24.3' -or $meta.versionCode -ne 74 -or $meta.signature -ne 'verified' -or $meta.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($path in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.24.3.apk','TestResults/v0243-installed.apk')){if((Get-FileHash $path).Hash -ne $meta.sha256 -or (Get-Item $path).Length -ne $meta.bytes){throw 'Final APK/installed mismatch'}}
if((Get-FileHash Builds/Android/Gatefall-v0.24.1.apk).Hash -ne '8f6f04fbeb191b17c65d0d1c78de4b31d7e39b094759a40d504b8daf2a8f458e' -or (Get-FileHash Builds/Android/Gatefall-v0.24.2.apk).Hash -ne '461d100672400f1b47f175a9538ab7a177b85583a69f00287d6dbae6ffdd66c5' -or (Get-FileHash TestResults/v0242-installed.apk).Hash -ne '461d100672400f1b47f175a9538ab7a177b85583a69f00287d6dbae6ffdd66c5'){throw 'Verified fallback/interim APK not preserved'}
$old=Get-Content TestResults/training-ground-v0242-baseline/source-hashes-v0241.json -Raw|ConvertFrom-Json
$middle=Get-Content TestResults/source-hashes-v0242-candidate.json -Raw|ConvertFrom-Json
$now=@(Get-Content TestResults/source-hashes-v0243-candidate.json -Raw|ConvertFrom-Json)
if($now.Count -ne 23){throw 'Manifest coverage differs'}
$changed=@(foreach($r in $now){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Final source/asset changed'};$a=@($old|Where-Object path -eq $r.path);if($a.Count -eq 0 -or $a[0].sha256 -ne $r.sha256){$r.path}})
$expected=@('Assets\Scripts\Presentation\ArenaView.Ground.cs','Assets\Scripts\Presentation\MobileHud.cs','ProjectSettings\ProjectSettings.asset','Assets\Resources\Art\V12\district-training-ground.png','Assets\Resources\Art\V12\district-training-ground.png.meta','Assets\Editor\PrototypeTools.cs')
if(@(Compare-Object $changed $expected).Count){throw 'Unexpected release scope'}
$interimChanged=@(foreach($r in $now){$a=@($middle|Where-Object path -eq $r.path);if($a.Count -ne 1){throw 'Interim manifest missing path'};if($a[0].sha256 -ne $r.sha256){$r.path}})
if(@(Compare-Object $interimChanged @('Assets\Scripts\Presentation\MobileHud.cs','ProjectSettings\ProjectSettings.asset')).Count){throw 'Unexpected interim-to-final changes'}
$hud=[IO.File]::ReadAllText('Assets/Scripts/Presentation/MobileHud.cs')
$original=[IO.File]::ReadAllText('TestResults/timer-contrast-v0243-baseline/MobileHud.cs')
$a='"경과 " + Clock((float)game.Elapsed), 11, Muted, TextAnchor.MiddleCenter, 1);'
$b='"경과 " + Clock((float)game.Elapsed), 11, Paper, TextAnchor.MiddleCenter, 1, true);'
if($original.Replace($a,$b) -cne $hud -or $original -ceq $hud){throw 'Timer changed beyond one style argument line'}
if(-not (Get-Content Assets/Scripts/Presentation/ArenaView.Ground.cs -Raw).Contains('stage == 10 ? art.Texture("district-training-ground") ?? art.Texture("district-ground") : art.Texture("district-ground")')){throw 'Ground is no longer limited to1-11'}
$source='C:\Users\Pro\.codex\generated_images\01a16b41-6b49-79d1-8d27-9610f5bde295\exec-f6c987c8-f6e9-4ab1-9b62-fc2bfd0f274e.png'
if((Get-FileHash Assets/Resources/Art/V12/district-training-ground.png).Hash -ne (Get-FileHash $source).Hash){throw 'Project asset differs from generated output'}
$probe=Get-Content TestResults/training-ground-v0242-image-probe.json -Raw|ConvertFrom-Json
if($probe.streams[0].width -ne 1774 -or $probe.streams[0].height -ne 887 -or $probe.streams[0].pix_fmt -ne 'rgb24'){throw 'Source image differs'}
$assetMeta=Get-Content Assets/Resources/Art/V12/district-training-ground.png.meta -Raw
if($assetMeta -notmatch 'maxTextureSize: 512' -or $assetMeta -match 'maxTextureSize: 2048'){throw 'Import cap differs'}
if(-not (Get-Content TestResults/unity-android.log -Raw).Contains('GATEFALL TERRAIN district-training-ground width=512 height=256 max=512')){throw 'Actual imported dimensions missing'}
foreach($fragment in @('ANDROID DELIVERY COMPLETE:','Number of signers: 1')){if(-not (Get-Content TestResults/build-v0243-delivery.txt -Raw).Contains($fragment)){throw 'Final Android verification incomplete'}}
$regression=Get-Content TestResults/build-v0242-delivery.txt -Raw
foreach($fragment in @('PASS 308 road-clear reachable sites','PASS all6 original per-kind wave budgets preserved.','PASS: 59 wave-call invariants.','Unity Android build failed')){if(-not $regression.Contains($fragment)){throw 'Pre-terrain-failure regression provenance missing'}}
if(-not (Get-Content TestResults/unity-v0242-terrain-count-failed.log -Raw).Contains('Missing v0.12 terrain art') -or -not (Get-Content TestResults/build-v0242-delivery-final.txt -Raw).Contains('ANDROID DELIVERY COMPLETE:')){throw 'First failure/recovery evidence missing'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$initial=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0243-before.sav')))
$saves=@(foreach($path in @('TestResults/v0242-before.sav','TestResults/v0242-after-update.sav','TestResults/v0242-after-native.sav','TestResults/v0243-before.sav','TestResults/v0243-after-update.sav','TestResults/v0243-after-native.sav','TestResults/v0243-after-other-ready.sav','TestResults/v0243-relaunch.sav')){
    $p=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $path)))
    if((Get-FileHash $path).Hash -ne '267e0de32e21823c114505d6eaf138af21ee72e2fdc97ce5912c56bc46c44f9d' -or (Get-Item $path).Length -ne 324 -or -not $p.Valid() -or $p.MagicCores -ne 232 -or $p.StarsTotal -ne 42 -or $p.Loadout -ne 263 -or $p.SelectedHunter -ne 0 -or $p.LastReward -ne 36 -or $p.LastRewardRun -ne $initial.LastRewardRun -or -not $p.FirstCleared[13] -or $p.PendingRun -ne '' -or $p.PendingStage -ne -1 -or @($p.Growth|Where-Object {$_ -ne 0}).Count -or @($p.Research|Where-Object {$_ -ne 0}).Count){throw "Actual saved profile differs $path"}
    [ordered]@{path=$path;valid=$true;bytes=324;sha256=(Get-FileHash $path).Hash.ToLowerInvariant();magic=232;stars=42;loadout=263;lastReward=36;lastRewardRun=$p.LastRewardRun;pending=$p.PendingRun;newReward=0}
});$saves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0243-save-check.json
$ui=Get-Content TestResults/v0243-native-observations.json -Raw|ConvertFrom-Json
if($ui.finalVersionFullBattle -or $ui.videoRecorded -or $ui.interimV0242ReadyScreens.Count -ne 3 -or $ui.finalV0243RunningScreens.Count -ne 3 -or $ui.finalTowers -ne 4 -or $ui.finalBuildSpend -ne 165 -or $ui.cardsReached -ne 0 -or -not $ui.stage10OriginalFloorObserved -or $ui.newReward -ne 0){throw 'Native coverage differs'}
foreach($shot in @('v0242-before-stage11-ready','v0242-stage11-ready-2400','v0242-stage11-ready-1920','v0242-stage11-ready-1280','v0242-marker-live-menu','v0243-timer-running-2400','v0243-timer-running-1920','v0243-timer-running-1280','v0243-stage10-original-floor','v0243-relaunch-city')){if(-not(Test-Path "Docs/QA/v050/$shot.png")){throw 'Missing actual image'}}
$diag=Get-Content TestResults/v0243-diagnostics.json -Raw|ConvertFrom-Json
if($diag.appPid -ne 7641 -or $diag.frameworkBefore -ne 584 -or $diag.frameworkAfter -ne 584 -or $diag.startupErrorCount -ne 5 -or $diag.postFirstPerfSurface -ne 6 -or $diag.postFirstPerfOther -ne 0 -or $diag.javaOrNativeFatalSignatures -ne 0 -or $diag.packageAnrOrCrash -ne 0 -or $diag.watchdogKills -ne 0 -or $diag.profileLinkAuditLines -ne 4){throw 'Actual diagnostics require review'}
$perf=Get-Content TestResults/v0243-active-performance.json -Raw|ConvertFrom-Json
if($perf.frameSamples -ne 121 -or $perf.battleRunningWindows -ne 1 -or $perf.maxEnemiesAtWindowEnd -ne 0){throw 'Tiny sample scope differs'}
$cleanup=Get-Content TestResults/qa-cleanup-v0243.json -Raw|ConvertFrom-Json
if($cleanup.serial -ne 'emulator-5648' -or -not $cleanup.normalExit -or -not $cleanup.normalPrioritiesRestored -or $cleanup.remainingProcesses -ne 0 -or -not $cleanup.diskFilesRemoved -or $cleanup.reparsePoints -ne 0 -or $cleanup.protectedDevicesTouched){throw 'Owned cleanup incomplete'}
[ordered]@{created=(Get-Date -Format o);tasks=@(257,258);version=$meta.version;code=$meta.versionCode;bytes=$meta.bytes;sha256=$meta.sha256;changedTrackedFiles=$changed;allSixCoreFilesUnchangedFromV0241=$true;asset=@{path='Assets/Resources/Art/V12/district-training-ground.png';sourceWidth=1774;sourceHeight=887;importWidth=512;importHeight=256;builtinImageGen=$true;promptRecord='Docs/Design/GATEFALL_TRAINING_GROUND_ASSET_2026-11-01.md';generatedBytesCopiedUnedited=$true;onlyStage11=$true};firstBuildFailurePreserved=$true;regressionReusedWithUnchangedCore=$true;interimV0242IsSeparateEvidence=$true;native=$ui;saves=$saves;diagnostics=$diag;sampledFramesNotPerformanceEvidence=$perf;ownedCleanup=$cleanup;newCombatModels=0;newVersionFullBattle=$false;humanFunTested=$false;physicalDeviceTested=$false;iosTested=$false;audioReviewed=$false;performanceConclusion=$false;passed=$true}|ConvertTo-Json -Depth 12|Set-Content TestResults/delivery-consistency-v0243.json
'PASS v0.24.3/code74: new own training-floor/512x256; timer style only; interimReady3/finalRunning3; actual8 equal saves; signature16KB/installed hash/source23/owned cleanup. No full battle or performance claim.'
