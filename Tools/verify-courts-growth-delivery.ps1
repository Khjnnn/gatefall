$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$meta=Get-Content TestResults/android-apk-v0240-candidate.json -Raw|ConvertFrom-Json
if($meta.version -ne '0.24.0' -or $meta.versionCode -ne 71 -or $meta.signature -ne 'verified' -or $meta.alignment16KB -ne 'verified'){throw 'Android metadata differs'}
foreach($path in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.24.0.apk','TestResults/v0240-installed.apk')){if((Get-FileHash $path).Hash -ne $meta.sha256 -or (Get-Item $path).Length -ne $meta.bytes){throw 'Installed/delivered APK differs'}}
if((Get-FileHash Builds/Android/Gatefall-v0.23.0.apk).Hash -ne 'b4d4873174a3d16226a3909e05aaac3429246f51e3ab700706a3f8db293943e2'){throw 'Verified fallback not preserved'}
$old=Get-Content TestResults/courts-growth-v0240-baseline/source-hashes-v0230.json -Raw|ConvertFrom-Json
$current=@(Get-Content TestResults/source-hashes-v0240-candidate.json -Raw|ConvertFrom-Json)
if($current.Count -ne 20){throw 'Product manifest scope differs'}
$changes=@(foreach($r in $current){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Current source changed'};$prior=@($old|Where-Object path -eq $r.path);if($prior.Count -ne 1){throw 'Missing baseline path'};if($prior[0].sha256 -ne $r.sha256){$r.path}})
if(@(Compare-Object $changes @('Assets\Scripts\Core\RaonCombat.cs','Assets\Scripts\Core\DefenseSimulation.cs','ProjectSettings\ProjectSettings.asset')).Count){throw 'Unexpected product scope'}
foreach($file in @('RaonCombat.cs','DefenseSimulation.cs')){if([IO.File]::ReadAllText("Assets/Scripts/Core/$file") -cne [IO.File]::ReadAllText("TestResults/courts-heavy-v0230-speed115-review/candidate-core/$file")){throw 'Selected candidate differs'}}
$models=Get-Content TestResults/courts-heavy-v0230-comparison.json -Raw|ConvertFrom-Json
if($models.selectedSpeedMultiplier -ne 1.15 -or $models.selectedPackSpan -ne 10 -or $models.actualExistingHeavyCounts.total -ne 19 -or $models.newCandidateCombatModels -ne 18 -or -not $models.baseline12ReusedFrom249 -or -not $models.early3Other13CrisisGeometryHpSchedulesEconomyExactToWindow10Baseline -or $models.newAndroidVerified){throw 'Model point-in-time evidence differs'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$before=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0240-before.sav')))
foreach($phase in @('before','after-update')){if((Get-FileHash "TestResults/v0240-$phase.sav").Hash -ne '4da5b60e728b5df3f78fbc24dc6dc7043df101d569c5decfffe0e23cea922c0d'){throw 'Actual update profile differs'}}
$saves=@(foreach($phase in @('result','map','relaunch')){
    $path="TestResults/v0240-$phase.sav";$p=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $path)))
    if((Get-FileHash $path).Hash -ne '3cfb7601282bc6cf6442c4bf6fc73ad4fdb79143fdfc59d7b935542f5292552a' -or (Get-Item $path).Length -ne 324 -or -not $p.Valid() -or $p.MagicCores -ne 58 -or $p.StarsTotal -ne 15 -or $p.Loadout -ne 135 -or $p.SelectedHunter -ne 0 -or $p.LastReward -ne 18 -or $p.LastRewardRun -eq $before.LastRewardRun -or $p.LastRewardRun.Length -eq 0 -or $p.BestStars[4] -ne 3 -or -not $p.FirstCleared[4] -or -not $p.IsStageOpen(5) -or $p.PendingRun -ne '' -or $p.PendingStage -ne -1 -or @($p.Growth|Where-Object {$_ -ne 0}).Count -or @($p.Research|Where-Object {$_ -ne 0}).Count){throw "Reward/persistence differs: $phase"}
    [ordered]@{phase=$phase;bytes=324;sha256=(Get-FileHash $path).Hash.ToLowerInvariant();valid=$p.Valid();magic=$p.MagicCores;stars=$p.StarsTotal;loadout=$p.Loadout;stage5FirstClear=$p.FirstCleared[4];stage6Open=$p.IsStageOpen(5);bestStars=$p.BestStars[4];lastReward=$p.LastReward;lastRewardRun=$p.LastRewardRun;pending=$p.PendingRun}
});$saves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0240-save-check.json
$run=Get-Content TestResults/v0240-last-run.txt -Raw
foreach($pattern in @('Gatefall 0.24.0','Won elapsed=173.77 core=300 hunter=100 kills=780','allSpawned=True spawned=780 remainingEnemies=0 bossDefeated=False','Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0','129.6 fusion (Autocannon+Rupture) -> Shatter @2','131.4 build Rupture @20')){if(-not $run.Contains($pattern)){throw 'Native run differs'}}
$ledger=Get-Content TestResults/v0240-native-purchase-ledger.json -Raw|ConvertFrom-Json
if($ledger.receipts.Count -ne 26 -or $ledger.builds -ne 20 -or $ledger.upgrades -ne 4 -or $ledger.fusions -ne 2 -or $ledger.pricedSpend -ne 1095 -or $ledger.finalTowersDerivedFromBuildsMinusFusions -ne 18 -or $ledger.sales -ne 0){throw 'Native ledger differs'}
$ui=Get-Content TestResults/v0240-native-observations.json -Raw|ConvertFrom-Json
if(-not $ui.fullBattleCompleted -or $ui.readyOnlyScreens.Count -ne 3 -or $ui.card30 -ne 'Ballistic' -or $ui.card90 -ne 'Range' -or $ui.card150 -ne 'Ballistic' -or $ui.initialSixCost -ne 270 -or $ui.runningAllThreeSizesTested){throw 'UI evidence scope differs'}
foreach($shot in @('ready-2400','ready-1920','ready-1280','card30','card90','card150','arc-pick','shatter-created','shatter-refill-pause','seventeen-pause','eighteen-pause','map','relaunch-city')){if(-not(Test-Path "Docs/QA/v050/v0240-$shot.png")){throw 'Missing native image'}}
$perf=Get-Content TestResults/v0240-active-performance.json -Raw|ConvertFrom-Json
if($perf.appPidFilter -ne 4865 -or $perf.battleRunningWindows -ne 17 -or $perf.frameSamples -ne 6415 -or $perf.worstMs -ne 33.3 -or $perf.framesOver33ms -ne 0 -or $perf.maxEnemiesAtWindowEnd -ne 73){throw 'Performance scope differs'}
$diag=Get-Content TestResults/v0240-diagnostics.json -Raw|ConvertFrom-Json
if($diag.appPid -ne 4865 -or $diag.frameworkBefore -ne 576 -or $diag.frameworkAfter -ne 576 -or $diag.startupErrorCount -ne 8 -or $diag.postFirstPerfSurface -ne 9 -or $diag.postFirstPerfOther -ne 0 -or $diag.packageAnrOrCrash -ne 0 -or $diag.watchdogKills -ne 0 -or $diag.profileLinkAuditLines -ne 3){throw 'Diagnostics need review'}
$cleanup=Get-Content TestResults/qa-cleanup-v0240.json -Raw|ConvertFrom-Json
if($cleanup.serial -ne 'emulator-5644' -or -not $cleanup.normalPrioritiesRestored -or -not $cleanup.normalExit -or $cleanup.remainingProcesses -ne 0 -or -not $cleanup.diskFilesRemoved -or $cleanup.reparsePoints -ne 0 -or $cleanup.protectedDevicesTouched){throw 'Owned cleanup differs'}
$build=Get-Content TestResults/build-v0240-delivery.txt -Raw
if(-not $build.Contains('Gatefall-v0.24.0.apk') -or -not $build.Contains('ANDROID DELIVERY COMPLETE')){throw 'Build completion missing'}
$video=Get-Content TestResults/v0240-video-probe.json -Raw|ConvertFrom-Json
if($video.streams[0].width -ne 1920 -or $video.streams[0].height -ne 864 -or [double]$video.format.duration -ne 11.825178 -or [long]$video.format.size -ne 8403180){throw 'Recorded video differs'}
[ordered]@{created=(Get-Date -Format o);task=251;version=$meta.version;code=$meta.versionCode;bytes=$meta.bytes;sha256=$meta.sha256;changedProductFiles=$changes;modelRecord='courts-heavy-v0230-comparison.json';modelFalseFlagIsPreNativeSnapshot=$true;native=$ui;ledger=$ledger;saves=$saves;actualBeforeAfterUpdateIdentical=$true;resultMapNoPushRelaunchIdentical=$true;activePerformance=$perf;diagnostics=$diag;ownedCleanup=$cleanup;video=@{path='Docs/QA/v050/v0240-shatter.mp4';width=1920;height=864;seconds=11.825178;bytes=8403180;extractedFrames=6;visuallyReviewedFrames=@(2,5);wholeRunReviewed=$false;audioReviewed=$false};allEighteenTowersFiredOrSimultaneous=$false;nativeExactModelReplay=$false;other13NewVersionFullBattles=$false;crisisNativeBattle=$false;humanFunTested=$false;physicalAndroidTested=$false;iosTested=$false;diagnosticRootCauseResolved=$false;passed=$true}|ConvertTo-Json -Depth 13|Set-Content TestResults/delivery-consistency-v0240.json
'PASS: v0.24.0 original1-5/780; actual18 towers/two fusions/173.77s clear/core300;18reward/equal saves; installed hash/signature16KB/source scope/owned cleanup'
