$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$meta=Get-Content TestResults/android-apk-v0220-candidate.json -Raw|ConvertFrom-Json
if($meta.version -ne '0.22.0' -or $meta.versionCode -ne 69 -or $meta.signature -ne 'verified' -or $meta.alignment16KB -ne 'verified'){throw 'Android metadata differs'}
foreach($path in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.22.0.apk','TestResults/v0220-installed.apk')){if((Get-FileHash $path).Hash -ne $meta.sha256 -or (Get-Item $path).Length -ne $meta.bytes){throw 'Installed/delivered APK differs'}}
$old=Get-Content TestResults/final-spine-v0220-baseline/source-hashes-v0211.json -Raw|ConvertFrom-Json
$current=@(Get-Content TestResults/source-hashes-v0220.json -Raw|ConvertFrom-Json)
if($current.Count -ne 20){throw 'Product manifest scope differs'}
$changes=@(foreach($r in $current){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Current sources changed'};$prior=@($old|Where-Object path -eq $r.path);if($prior.Count -ne 1){throw 'Missing baseline path'};if($prior[0].sha256 -ne $r.sha256){$r.path}})
if(@(Compare-Object $changes @('Assets\Scripts\Core\BattlefieldLayouts.cs','Assets\Scripts\Core\RaonCombat.cs','ProjectSettings\ProjectSettings.asset')).Count){throw 'Unexpected product scope'}
$snapshot='TestResults/final-chicane-v0211-review/west-lower-connector-pilot-window12/'
if([IO.File]::ReadAllText('Assets/Scripts/Core/BattlefieldLayouts.cs') -cne [IO.File]::ReadAllText($snapshot+'BattlefieldLayouts.cs')){throw 'Layout differs from selected model'}
$expectedRaon=[IO.File]::ReadAllText($snapshot+'RaonCombat.cs').Replace('arrivalWindow=12.0;if(stage.Index==13','arrivalWindow=12.0;'+[Environment]::NewLine+'                    if(stage.Index==13')
if([IO.File]::ReadAllText('Assets/Scripts/Core/RaonCombat.cs') -cne $expectedRaon){throw 'Combat differs from selected model'}
$routes=Get-Content Docs/Design/route-iteration-plan.json -Raw|ConvertFrom-Json
if(@($routes.changed_layouts|Where-Object {$_.stage_index -eq 13 -and $_.layout_id -eq 'observer-offset-shared-spine'}).Count -ne 1){throw 'Route declaration missing'}
$model=Get-Content TestResults/final-spine-v0211-model-comparison.json -Raw|ConvertFrom-Json
if($model.scheduled -ne 944 -or $model.pairs.Count -ne 9 -or -not $model.other13OperationsAndAllCrisisSchedulesUnchanged -or -not $model.initialThreeWavesAndFirstEntranceSpawnsUnchanged -or $model.newAndroidVerified){throw 'Model record scope differs'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$before=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0220-before.sav')))
foreach($phase in @('before','after-update')){if((Get-FileHash "TestResults/v0220-$phase.sav").Hash -ne 'c85b059454993f432d48e8179bc4334ae990a78b7f63c448577a97cd81e418a7'){throw 'Actual update profile differs'}}
$saves=@(foreach($phase in @('result','map','relaunch')){
    $path="TestResults/v0220-$phase.sav";$p=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $path)))
    if((Get-FileHash $path).Hash -ne '95cfcf3787c861658b0565b5ed2371d81346836b034d203f16056e8304ab86c0' -or (Get-Item $path).Length -ne 324 -or -not $p.Valid() -or $p.MagicCores -ne 232 -or $p.StarsTotal -ne 42 -or $p.Loadout -ne 263 -or $p.SelectedHunter -ne 0 -or $p.LastReward -ne 36 -or $p.LastRewardRun -eq $before.LastRewardRun -or $p.LastRewardRun.Length -eq 0 -or $p.BestStars[13] -ne 3 -or -not $p.FirstCleared[13] -or $p.PendingRun -ne '' -or $p.PendingStage -ne -1 -or @($p.Growth|Where-Object {$_ -ne 0}).Count -or @($p.Research|Where-Object {$_ -ne 0}).Count){throw "Reward/persistence differs: $phase"}
    [ordered]@{phase=$phase;bytes=324;sha256=(Get-FileHash $path).Hash.ToLowerInvariant();valid=$true;magic=232;stars=42;loadout=263;stage14FirstClear=$true;bestStars=3;observerUnlocked=$true;lastReward=36;lastRewardRun=$p.LastRewardRun;pending=''}
})
$saves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0220-save-check.json
$run=Get-Content TestResults/v0220-last-run.txt -Raw
foreach($pattern in @('Gatefall 0.22.0','Won elapsed=183.62 core=300 hunter=100 kills=944','allSpawned=True spawned=944 remainingEnemies=0 bossDefeated=False','Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0','Mark hits=57 effective bonus damage=168.0')){if(-not $run.Contains($pattern)){throw 'Native run evidence differs'}}
$ledger=Get-Content TestResults/v0220-native-purchase-ledger.json -Raw|ConvertFrom-Json
if($ledger.receipts.Count -ne 26 -or $ledger.builds -ne 21 -or $ledger.upgrades -ne 4 -or $ledger.fusions -ne 1 -or $ledger.pricedSpend -ne 1080 -or $ledger.finalTowersDerivedFromBuildsMinusFusions -ne 20 -or $ledger.sales -ne 0){throw 'Native ledger differs'}
$ui=Get-Content TestResults/v0220-native-observations.json -Raw|ConvertFrom-Json
if(-not $ui.fullBattleCompleted -or $ui.readyOnlyScreens.Count -ne 3 -or $ui.card30 -ne 'Ballistic' -or $ui.card90 -ne 'Blast' -or $ui.card150 -ne 'Credits35' -or $ui.initialSevenCost -ne 315 -or $ui.runningAllThreeSizesTested){throw 'Native UI scope differs'}
foreach($shot in @('ready-2400','ready-1920','ready-1280','card90-actual','twenty-towers-pause','dense20-running','dense20-pause','result','cleared-map','relaunch-city')){if(-not(Test-Path "Docs/QA/v050/v0220-$shot.png")){throw 'Missing native evidence'}}
$perf=Get-Content TestResults/v0220-active-performance.json -Raw|ConvertFrom-Json
if($perf.appPidFilter -ne 5377 -or $perf.battleRunningWindows -ne 18 -or $perf.frameSamples -ne 6844 -or $perf.weightedAverageMs -ne 16.7 -or $perf.worstMs -ne 33.3 -or $perf.framesOver33ms -ne 0 -or $perf.maxEnemiesAtWindowEnd -ne 101){throw 'Active performance scope differs'}
$diag=Get-Content TestResults/v0220-diagnostics.json -Raw|ConvertFrom-Json
if($diag.appPid -ne 5377 -or $diag.frameworkBefore -ne 579 -or $diag.frameworkAfter -ne 579 -or $diag.startupErrorCount -ne 8 -or $diag.postFirstPerfSurface -ne 33 -or $diag.postFirstPerfOther -ne 0 -or $diag.packageAnr -ne 0 -or $diag.watchdogKills -ne 0){throw 'Diagnostics require review'}
$cleanup=Get-Content TestResults/qa-cleanup-v0220.json -Raw|ConvertFrom-Json
if($cleanup.serial -ne 'emulator-5640' -or -not $cleanup.normalPrioritiesRestored -or -not $cleanup.normalExit -or $cleanup.remainingProcesses -ne 0 -or -not $cleanup.diskFilesRemoved -or $cleanup.reparsePoints -ne 0 -or $cleanup.protectedDevicesTouched){throw 'Owned cleanup differs'}
[ordered]@{created=(Get-Date -Format o);task=241;version=$meta.version;code=$meta.versionCode;bytes=$meta.bytes;sha256=$meta.sha256;changedProductFiles=$changes;selectedModelRecord='final-spine-v0211-model-comparison.json';modelFalseFlagIsPreNativeSnapshot=$true;native=$ui;ledger=$ledger;saves=$saves;actualBeforeAfterUpdateIdentical=$true;resultMapNoPushRelaunchIdentical=$true;activePerformance=$perf;diagnostics=$diag;ownedCleanup=$cleanup;video=@{path='Docs/QA/v050/v0220-dense20.mp4';width=1920;height=864;seconds=11.993022;bytes=6383036;extractedFrames=6;visuallyReviewedFrames=@(2,5);wholeRunReviewed=$false;audioReviewed=$false};allTwentyTowersFiredOrSimultaneous=$false;nativeExactModelReplay=$false;other13NewVersionFullBattles=$false;crisisNativeBattle=$false;humanFunTested=$false;physicalAndroidTested=$false;iosTested=$false;diagnosticRootCauseResolved=$false;passed=$true}|ConvertTo-Json -Depth 12|Set-Content TestResults/delivery-consistency-v0220.json
'PASS: v0.22.0 selected route/pulse; actual20 towers/183.62s944 clear/core300;36reward/3equal saves; installed hash/signature16KB/source scope/owned cleanup'
