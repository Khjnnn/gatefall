$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$meta=Get-Content TestResults/android-apk-v0243-candidate.json -Raw|ConvertFrom-Json
if($meta.version -ne '0.24.3' -or $meta.versionCode -ne 74 -or $meta.signature -ne 'verified' -or $meta.alignment16KB -ne 'verified'){throw 'Release metadata differs'}
foreach($file in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.24.3.apk','TestResults/v0243-double-installed.apk')){if((Get-FileHash $file).Hash -ne $meta.sha256 -or (Get-Item $file).Length -ne 97406753){throw 'Installed/release APK differs'}}
$manifest=@(Get-Content TestResults/source-hashes-v0243.json -Raw|ConvertFrom-Json)
if($manifest.Count -ne 23){throw 'Unexpected manifest scope'}
foreach($row in $manifest){if((Get-FileHash $row.path).Hash -ne $row.sha256){throw 'Release source/asset changed'}}
$run=Get-Content TestResults/v0243-double-last-run.txt -Raw
foreach($fragment in @('Gatefall 0.24.3','Won elapsed=187.87 core=300 hunter=100 kills=816','Wave clear=True allSpawned=True spawned=816 remainingEnemies=0 bossDefeated=False','Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0','80.9 fusion (Autocannon+Marker) -> Observer @20','151.6 upgrade Rupture @3')){if(-not $run.Contains($fragment)){throw 'Native result/receipt missing'}}
$ledger=Get-Content TestResults/v0243-double-native-purchase-ledger.json -Raw|ConvertFrom-Json
if($ledger.builds -ne 11 -or $ledger.upgrades -ne 4 -or $ledger.fusions -ne 1 -or $ledger.pricedSpend -ne 670 -or $ledger.finalTowersDerivedFromBuildsMinusFusions -ne 10){throw 'Actual purchases differ'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$initial=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0243-double-before.sav')))
if(-not $initial.Valid() -or $initial.MagicCores -ne 232 -or $initial.StarsTotal -ne 42 -or $initial.LastReward -ne 36 -or $initial.PendingRun -ne '' -or (Get-FileHash TestResults/v0243-double-before.sav).Hash -ne '267e0de32e21823c114505d6eaf138af21ee72e2fdc97ce5912c56bc46c44f9d'){throw 'Before profile differs'}
$saves=@(foreach($file in @('v0243-double-result.sav','v0243-double-map.sav','v0243-double-relaunch.sav')){
    $path='TestResults/'+$file
    $p=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $path)))
    if(-not $p.Valid() -or (Get-Item $path).Length -ne 324 -or (Get-FileHash $path).Hash -ne 'a956e06f088d287999048419d0f04f76909d9024f3169141ad76fc0552597cc2' -or $p.MagicCores -ne 234 -or $p.StarsTotal -ne 42 -or $p.Loadout -ne 263 -or $p.SelectedHunter -ne 0 -or $p.LastReward -ne 2 -or $p.LastRewardRun -eq $initial.LastRewardRun -or $p.LastRewardRun -ne '4aac9c9dbbc14b4dae53454689b4ca65' -or $p.PendingRun -ne '' -or $p.PendingStage -ne -1 -or @($p.Growth|Where-Object {$_ -ne 0}).Count -or @($p.Research|Where-Object {$_ -ne 0}).Count -or @($p.FirstCleared|Where-Object {$_}).Count -ne 14){throw "Replay save differs $file"}
    for($i=0;$i -lt 14;$i++){if($p.FirstCleared[$i] -ne $initial.FirstCleared[$i] -or $p.BestStars[$i] -ne $initial.BestStars[$i]){throw 'Existing completion/stars changed'}}
    [ordered]@{path=$path;valid=$true;bytes=324;sha256=(Get-FileHash $path).Hash.ToLowerInvariant();magic=234;stars=42;loadout=263;replayReward=2;newRewardId=$p.LastRewardRun;pending=$p.PendingRun;all14ExistingCompletionsPreserved=$true}
})
$saves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0243-double-save-check.json
$diag=Get-Content TestResults/v0243-double-diagnostics.json -Raw|ConvertFrom-Json
if($diag.appPid -ne 4987 -or $diag.frameworkBefore -ne 580 -or $diag.frameworkAfter -ne 580 -or $diag.startupErrorCount -ne 5 -or $diag.postFirstPerfSurface -ne 4 -or $diag.postFirstPerfOther -ne 0 -or $diag.javaOrNativeFatalSignatures -ne 0 -or $diag.packageAnrOrCrash -ne 0 -or $diag.watchdogKills -ne 0 -or $diag.profileLinkAuditLines -ne 2){throw 'Diagnostics require review'}
$perf=Get-Content TestResults/v0243-double-active-performance.json -Raw|ConvertFrom-Json
if($perf.battleRunningWindows -ne 8 -or $perf.frameSamples -ne 3485 -or $perf.maxEnemiesAtWindowEnd -ne 83 -or $perf.framesOver33ms -ne 1){throw 'Sampled frame scope differs'}
$cleanup=Get-Content TestResults/qa-cleanup-v0243-double.json -Raw|ConvertFrom-Json
if($cleanup.serial -ne 'emulator-5650' -or -not $cleanup.normalExit -or -not $cleanup.normalPrioritiesRestored -or $cleanup.remainingProcesses -ne 0 -or -not $cleanup.diskFilesRemoved -or $cleanup.reparsePoints -ne 0 -or $cleanup.protectedDevicesTouched){throw 'Owned cleanup incomplete'}
foreach($name in @('ready','initial-seven','speed-live','card30','card90','observer-selection','observer-created','shatter-ready','current','map','relaunch')){if(-not(Test-Path ('Docs/QA/v050/v0243-double-'+$name+'.png'))){throw 'Missing observed screen'}}
[ordered]@{created=(Get-Date -Format o);task=259;version='0.24.3';code=74;sha256=$meta.sha256;bytes=97406753;newBuild=$false;productSourceChanges=0;nativeCompletedReplay=$true;stage=11;elapsed=187.87;kills=816;allSpawned=$true;remaining=0;core=300;hunter=100;stars=3;initialAllCompleteSyntheticProfile=$true;ledger=$ledger;saves=$saves;speed=@{twoTimesVisible=$true;initialOneTimesStartBeforeToggle=$true;lateDensityVideoRecorded=$false;speedOnOffCycleInThisRun=$false};cards=@{offers=3;observed30='Range';observed90='Ballistic';thirdIdentity='not recorded; prior intended Resume tap likely selected a card; no fixed-card claim'};qaCorrection=@{universalEmptyPoint2250_400=$false;rightPanelCanConsumePoint=$true;pauseControlAbsentWhileSocketMenu=$true;plannedShatterNotPerformed=$true;planned16TowersNotReached=$true;useHomeAfterShotForReview=$true};diagnostics=$diag;sampledEmulatorFrames=$perf;ownedCleanup=$cleanup;humanFunTested=$false;physicalDeviceTested=$false;iosTested=$false;audioReviewed=$false;wholeBattleVideo=$false;passed=$true}|ConvertTo-Json -Depth 14|Set-Content TestResults/delivery-consistency-v0243-double.json
'PASS v0.24.3 supplemental replay: 2x observed/10T670/Observer1/187.87s816 clear300; +2 and unchanged42stars/3 equal saves; same APK/source23; owned5650 cleanup. Third-card/late-video/phone claims excluded.'
