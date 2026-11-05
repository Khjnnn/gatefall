$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$meta=Get-Content TestResults/android-apk-v0230-candidate.json -Raw|ConvertFrom-Json
if($meta.version -ne '0.23.0' -or $meta.versionCode -ne 70 -or $meta.signature -ne 'verified' -or $meta.alignment16KB -ne 'verified'){throw 'Android metadata differs'}
foreach($path in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.23.0.apk','TestResults/v0230-installed.apk')){if((Get-FileHash $path).Hash -ne $meta.sha256 -or (Get-Item $path).Length -ne $meta.bytes){throw 'Installed/delivered APK differs'}}
if((Get-FileHash Builds/Android/Gatefall-v0.22.0.apk).Hash -ne '424f44c9e431b7c0eae686d4e6e6ff1e30518033dda5a3e9e10ab3e433879ffc'){throw 'Verified fallback not preserved'}
$old=Get-Content TestResults/final-north-v0230-baseline/source-hashes-v0220.json -Raw|ConvertFrom-Json
$current=@(Get-Content TestResults/source-hashes-v0230-candidate.json -Raw|ConvertFrom-Json)
if($current.Count -ne 20){throw 'Product manifest scope differs'}
$changes=@(foreach($r in $current){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Current sources changed'};$prior=@($old|Where-Object path -eq $r.path);if($prior.Count -ne 1){throw 'Missing baseline path'};if($prior[0].sha256 -ne $r.sha256){$r.path}})
if(@(Compare-Object $changes @('Assets\Scripts\Core\BattlefieldLayouts.cs','Assets\Scripts\Core\Campaign.cs','Assets\Scripts\Core\RaonCombat.cs','ProjectSettings\ProjectSettings.asset')).Count){throw 'Unexpected product scope'}
$snapshot='TestResults/final-north-short-v0220-review/candidate-core/'
if([IO.File]::ReadAllText('Assets/Scripts/Core/BattlefieldLayouts.cs') -cne [IO.File]::ReadAllText($snapshot+'BattlefieldLayouts.cs')){throw 'Layout differs from selected model'}
$selectedRaon=@(Get-Content ($snapshot+'RaonCombat.cs')|ForEach-Object {$_.Trim()}) -join "`n"
$actualRaon=@(Get-Content Assets/Scripts/Core/RaonCombat.cs|Where-Object {$_ -notmatch '// Preserve established pulse cell times with the extra short northern road\.'}|ForEach-Object {$_.Trim()}) -join "`n"
if($selectedRaon -cne $actualRaon){throw 'Raon differs beyond documented comment/indentation'}
$expectedCampaign=[IO.File]::ReadAllText($snapshot+'Campaign.cs').Replace('양쪽 균열과 정예를 막으면 관측창 조합이 해금됩니다.','북쪽 지름길과 서·동 우회로를 막으면 관측창 조합이 해금됩니다.').Replace('표식을 편성했다면 입구 쪽 창과 함께 두세요. 클리어하면 관측창 조합이 해금됩니다.','짧은 북쪽을 먼저 지키고 서·동을 보강하세요. 표식과 창은 같은 길을 노리세요.')
if([IO.File]::ReadAllText('Assets/Scripts/Core/Campaign.cs') -cne $expectedCampaign){throw 'Campaign differs beyond two reviewed strings'}
$routes=Get-Content Docs/Design/route-iteration-plan.json -Raw|ConvertFrom-Json
if(@($routes.changed_layouts|Where-Object {$_.stage_index -eq 13 -and $_.layout_id -eq 'observer-three-short-north'}).Count -ne 1){throw 'Route declaration missing'}
$scope=Get-Content TestResults/final-north-short-v0220-scope.json -Raw|ConvertFrom-Json
$model=Get-Content TestResults/final-north-short-v0220-comparison.json -Raw|ConvertFrom-Json
if($scope.staticInitializations -ne 28 -or -not $scope.other13OperationsExact -or -not $scope.existingWestEastRoadsExact -or -not $scope.allCellKindCountTimeIntervalSwarmUnchanged -or $scope.scheduled -ne 944 -or $scope.totalSites -ne 308 -or -not $scope.existingEntranceFirstTimesMayChange -or $model.newNormalModels -ne 9 -or $model.newCrisisModels -ne 3 -or $model.newOpeningModels -ne 2 -or $model.newAndroidVerified){throw 'Model/static scope differs'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$before=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0230-before.sav')))
foreach($phase in @('before','after-update')){if((Get-FileHash "TestResults/v0230-$phase.sav").Hash -ne 'c85b059454993f432d48e8179bc4334ae990a78b7f63c448577a97cd81e418a7'){throw 'Actual update profile differs'}}
$saves=@(foreach($phase in @('result','map','relaunch')){
    $path="TestResults/v0230-$phase.sav";$p=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $path)))
    if((Get-FileHash $path).Hash -ne '267e0de32e21823c114505d6eaf138af21ee72e2fdc97ce5912c56bc46c44f9d' -or (Get-Item $path).Length -ne 324 -or -not $p.Valid() -or $p.MagicCores -ne 232 -or $p.StarsTotal -ne 42 -or $p.Loadout -ne 263 -or $p.SelectedHunter -ne 0 -or $p.LastReward -ne 36 -or $p.LastRewardRun -eq $before.LastRewardRun -or $p.LastRewardRun.Length -eq 0 -or $p.BestStars[13] -ne 3 -or -not $p.FirstCleared[13] -or $p.PendingRun -ne '' -or $p.PendingStage -ne -1 -or @($p.Growth|Where-Object {$_ -ne 0}).Count -or @($p.Research|Where-Object {$_ -ne 0}).Count){throw "Reward/persistence differs: $phase"}
    [ordered]@{phase=$phase;bytes=324;sha256=(Get-FileHash $path).Hash.ToLowerInvariant();valid=$true;magic=232;stars=42;loadout=263;stage14FirstClear=$true;bestStars=3;observerUnlocked=$true;lastReward=36;lastRewardRun=$p.LastRewardRun;pending=''}
})
$saves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0230-save-check.json
$run=Get-Content TestResults/v0230-last-run.txt -Raw
foreach($pattern in @('Gatefall 0.23.0','Won elapsed=175.60 core=300 hunter=100 kills=944','allSpawned=True spawned=944 remainingEnemies=0 bossDefeated=False','Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0','Mark hits=23 effective bonus damage=48.0')){if(-not $run.Contains($pattern)){throw 'Native run evidence differs'}}
$ledger=Get-Content TestResults/v0230-native-purchase-ledger.json -Raw|ConvertFrom-Json
if($ledger.receipts.Count -ne 28 -or $ledger.builds -ne 21 -or $ledger.upgrades -ne 6 -or $ledger.fusions -ne 1 -or $ledger.pricedSpend -ne 1140 -or $ledger.finalTowersDerivedFromBuildsMinusFusions -ne 20 -or $ledger.sales -ne 0){throw 'Native ledger differs'}
$ui=Get-Content TestResults/v0230-native-observations.json -Raw|ConvertFrom-Json
if(-not $ui.fullBattleCompleted -or $ui.readyOnlyScreens.Count -ne 3 -or $ui.card30 -ne 'Range' -or $ui.card90 -ne 'Blast' -or $ui.card150 -ne 'Ballistic' -or $ui.initialSevenCost -ne 310 -or $ui.runningAllThreeSizesTested){throw 'Native UI scope differs'}
foreach($shot in @('ready-2400','ready-1920','ready-1280','card30','arc-ready-menu','card150','arc-pick','nineteen-pause','dense20-running','result','cleared-map','relaunch-city')){if(-not(Test-Path "Docs/QA/v050/v0230-$shot.png")){throw 'Missing native evidence'}}
$perf=Get-Content TestResults/v0230-active-performance.json -Raw|ConvertFrom-Json
if($perf.appPidFilter -ne 6959 -or $perf.battleRunningWindows -ne 17 -or $perf.frameSamples -ne 7902 -or $perf.weightedAverageMs -ne 16.7 -or $perf.worstMs -ne 33.3 -or $perf.framesOver33ms -ne 0 -or $perf.maxEnemiesAtWindowEnd -ne 65){throw 'Active performance scope differs'}
$diag=Get-Content TestResults/v0230-diagnostics.json -Raw|ConvertFrom-Json
if($diag.appPid -ne 6959 -or $diag.frameworkBefore -ne 582 -or $diag.frameworkAfter -ne 582 -or $diag.startupErrorCount -ne 8 -or $diag.postFirstPerfSurface -ne 12 -or $diag.postFirstPerfOther -ne 0 -or $diag.packageAnrOrCrash -ne 0 -or $diag.watchdogKills -ne 0 -or $diag.profileLinkAuditLines -ne 3){throw 'Diagnostics require review'}
$cleanup=Get-Content TestResults/qa-cleanup-v0230.json -Raw|ConvertFrom-Json
if($cleanup.serial -ne 'emulator-5642' -or -not $cleanup.normalPrioritiesRestored -or -not $cleanup.normalExit -or $cleanup.remainingProcesses -ne 0 -or -not $cleanup.diskFilesRemoved -or $cleanup.reparsePoints -ne 0 -or $cleanup.protectedDevicesTouched){throw 'Owned cleanup differs'}
$build=Get-Content TestResults/build-v0230-delivery-final.txt -Raw
if(-not $build.Contains('Gatefall-v0.23.0.apk') -or -not $build.Contains('ANDROID DELIVERY COMPLETE')){throw 'True candidate build log missing'}
[ordered]@{created=(Get-Date -Format o);task=243;version=$meta.version;code=$meta.versionCode;bytes=$meta.bytes;sha256=$meta.sha256;changedProductFiles=$changes;selectedModelRecord='final-north-short-v0220-comparison.json';modelFalseFlagIsPreNativeSnapshot=$true;scope=$scope;native=$ui;ledger=$ledger;saves=$saves;actualBeforeAfterUpdateIdentical=$true;resultMapNoPushRelaunchIdentical=$true;activePerformance=$perf;diagnostics=$diag;ownedCleanup=$cleanup;video=@{path='Docs/QA/v050/v0230-dense20.mp4';width=1920;height=864;seconds=11.994433;bytes=6776371;extractedFrames=6;visuallyReviewedFrames=@(2,5);includesResultTail=$true;wholeRunReviewed=$false;audioReviewed=$false};allTwentyTowersFiredOrSimultaneous=$false;nativeExactModelReplay=$false;other13NewVersionFullBattles=$false;crisisNativeBattle=$false;humanFunTested=$false;physicalAndroidTested=$false;iosTested=$false;diagnosticRootCauseResolved=$false;preSourceDuplicateIsNotV0230Evidence=$true;passed=$true}|ConvertTo-Json -Depth 13|Set-Content TestResults/delivery-consistency-v0230.json
'PASS: v0.23.0 three entrances/24 sites; actual20 towers/175.60s944 clear/core300;36reward/3equal saves; installed hash/signature16KB/source scope/owned cleanup'
