$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$meta=Get-Content TestResults/android-apk-v0241-candidate.json -Raw|ConvertFrom-Json
if($meta.version -ne '0.24.1' -or $meta.versionCode -ne 72 -or $meta.signature -ne 'verified' -or $meta.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($path in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.24.1.apk','TestResults/v0241-installed.apk')){if((Get-FileHash $path).Hash -ne $meta.sha256 -or (Get-Item $path).Length -ne $meta.bytes){throw 'APK/installed bytes differ'}}
if((Get-FileHash Builds/Android/Gatefall-v0.24.0.apk).Hash -ne 'ca6b0e342562a66cc3cf596751790a5277cabca557af9a8de045e9c5ce773e12'){throw 'Verified previous APK missing'}
$beforeSources=Get-Content TestResults/road-direction-v0241-baseline/source-hashes-v0240.json -Raw|ConvertFrom-Json
$now=@(Get-Content TestResults/source-hashes-v0241-candidate.json -Raw|ConvertFrom-Json)
if($now.Count -ne 20){throw 'Product manifest differs'}
$changes=@(foreach($r in $now){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Candidate product changed'};$old=@($beforeSources|Where-Object path -eq $r.path);if($old.Count -ne 1){throw 'Missing baseline path'};if($old[0].sha256 -ne $r.sha256){$r.path}})
if(@(Compare-Object $changes @('Assets\Scripts\Presentation\ArenaView.Ground.cs','ProjectSettings\ProjectSettings.asset')).Count){throw 'Unexpected product edits'}
$ground=Get-Content Assets/Scripts/Presentation/ArenaView.Ground.cs -Raw
foreach($fragment in @('marking % 3 == 1','V2.Distance(p, road[next]) > .85f','V2.Distance(p, road[next - 1]) > .85f','RoadMark(tail + side, tip); RoadMark(tail - side, tip);')){if(-not $ground.Contains($fragment)){throw 'Direction markup implementation differs'}}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$initial=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0241-before.sav')))
$saves=@(foreach($phase in @('before','after-update','after-ready-abandon','after-live-abandon','after-two-ready','after-qa','relaunch')){
    $path="TestResults/v0241-$phase.sav";$p=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $path)))
    if((Get-FileHash $path).Hash -ne '267e0de32e21823c114505d6eaf138af21ee72e2fdc97ce5912c56bc46c44f9d' -or (Get-Item $path).Length -ne 324 -or -not $p.Valid() -or $p.MagicCores -ne 232 -or $p.StarsTotal -ne 42 -or $p.Loadout -ne 263 -or $p.SelectedHunter -ne 0 -or $p.LastReward -ne 36 -or $p.LastRewardRun -ne $initial.LastRewardRun -or -not $p.FirstCleared[13] -or $p.PendingRun -ne '' -or $p.PendingStage -ne -1 -or @($p.Growth|Where-Object {$_ -ne 0}).Count -or @($p.Research|Where-Object {$_ -ne 0}).Count){throw "Preserved profile differs: $phase"}
    [ordered]@{phase=$phase;valid=$true;bytes=324;sha256=(Get-FileHash $path).Hash.ToLowerInvariant();magic=$p.MagicCores;stars=$p.StarsTotal;loadout=$p.Loadout;lastReward=$p.LastReward;lastRewardRun=$p.LastRewardRun;pending=$p.PendingRun;newReward=0}
});$saves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0241-save-check.json
$native=Get-Content TestResults/v0241-native-observations.json -Raw|ConvertFrom-Json
if($native.fullBattleCompleted -or $native.videoRecorded -or $native.readyThreeSizesStage -ne 10 -or ($native.readyStages2400 -join ',') -ne '5,10,14' -or $native.shortLiveTowers -ne 4 -or $native.shortLiveBuildSpend -ne 160 -or $native.menuLiveHudBefore -ne '0:19' -or $native.menuLiveHudAfter -ne '0:24' -or $native.newReward -ne 0){throw 'Native scope differs'}
foreach($shot in @('stage14-ready-2400','stage10-ready-2400','stage10-ready-1920','stage10-ready-1280','stage10-running','stage10-live-menu','stage5-ready-2400','task256-lv1-menu','task256-lv2-menu','relaunch-city')){if(-not(Test-Path "Docs/QA/v050/v0241-$shot.png")){throw 'Missing native image'}}
$qa=Get-Content TestResults/qa-profile-menu-v0241.json -Raw|ConvertFrom-Json
if($qa.spearLevel1Tiles -ne 6 -or $qa.spearLevel2Tiles -ne 5 -or $qa.generatedUpgrade.x -ne 1101 -or $qa.generatedUpgrade.y -ne 873 -or $qa.nativeAfter.spearLevel -ne 2 -or $qa.nativeAfter.credits -ne 250 -or -not $qa.default24CsvSame -or $qa.productChanged){throw 'QA profile menu proof differs'}
if(@(Compare-Object (Get-Content TestResults/v0241-stage10-map-actions.csv) (Get-Content TestResults/v0241-task256-default-map-actions.csv)).Count){throw 'Default coordinate output differs'}
$projection=Get-Content TestResults/v0241-unlocked-stage10-projection.json -Raw|ConvertFrom-Json
if($projection.menuTiles.spearLevel1 -ne 6 -or $projection.menuTiles.spearLevel2 -ne 5 -or -not $projection.profile.observerUnlocked -or $projection.profile.loadout -ne 263){throw 'Explicit profile projection differs'}
$diag=Get-Content TestResults/v0241-diagnostics.json -Raw|ConvertFrom-Json
if($diag.appPid -ne 5285 -or $diag.frameworkBefore -ne 582 -or $diag.frameworkAfter -ne 582 -or $diag.startupErrorCount -ne 5 -or $diag.postFirstPerfSurface -ne 0 -or $diag.postFirstPerfOther -ne 0 -or $diag.javaOrNativeFatalSignatures -ne 0 -or $diag.packageAnrOrCrash -ne 0 -or $diag.watchdogKills -ne 0 -or $diag.profileLinkAuditLines -ne 8){throw 'Diagnostic observation differs'}
$perf=Get-Content TestResults/v0241-active-performance.json -Raw|ConvertFrom-Json
if($perf.frameSamples -ne 815 -or $perf.battleRunningWindows -ne 2 -or $perf.framesOver33ms -ne 0){throw 'Short sampled window scope differs'}
$cleanup=Get-Content TestResults/qa-cleanup-v0241.json -Raw|ConvertFrom-Json
if($cleanup.serial -ne 'emulator-5646' -or -not $cleanup.normalExit -or -not $cleanup.normalPrioritiesRestored -or $cleanup.remainingProcesses -ne 0 -or -not $cleanup.diskFilesRemoved -or $cleanup.reparsePoints -ne 0 -or $cleanup.protectedDevicesTouched){throw 'Owned cleanup differs'}
if(-not (Get-Content TestResults/build-v0241-delivery.txt -Raw).Contains('ANDROID DELIVERY COMPLETE:')){throw 'Build completion missing'}
[ordered]@{created=(Get-Date -Format o);tasks=@(255,256);version=$meta.version;code=$meta.versionCode;bytes=$meta.bytes;sha256=$meta.sha256;changedProductFiles=$changes;allSixCoreFilesUnchanged=$true;native=$native;qaMenu=$qa;saves=$saves;actualBeforeUpdateAndAbandonsAndNoPushRelaunchIdentical=$true;shortSamples=$perf;diagnostics=$diag;ownedCleanup=$cleanup;previousV0240Stage5FullBattleIsNotThisVersion=$true;humanFunTested=$false;physicalAndroidTested=$false;iosTested=$false;audioReviewed=$false;all14MapsNativeReviewed=$false;performanceNoRegressionConclusion=$false;passed=$true}|ConvertTo-Json -Depth 12|Set-Content TestResults/delivery-consistency-v0241.json
'PASS: v0.24.1/code72 road chevrons; unchanged six cores;2/3/4-lane Ready and short4-tower LIVE;7equal actual saves; profile-aware QA upgrade; signature16KB/installed hash/owned cleanup.'
