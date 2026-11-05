$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskMeta=Get-Content TestResults/android-apk-v0210-candidate.json -Raw|ConvertFrom-Json
if($taskMeta.version -ne '0.21.0' -or $taskMeta.versionCode -ne 67 -or $taskMeta.signature -ne 'verified' -or $taskMeta.alignment16KB -ne 'verified'){throw 'Verified APK metadata differs'}
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.21.0.apk','TestResults/v0210-final-installed.apk')){
    if((Get-FileHash $taskPath).Hash -ne $taskMeta.sha256 -or (Get-Item $taskPath).Length -ne $taskMeta.bytes){throw 'Delivered/installed APK differs'}
}
foreach($taskR in(Get-Content TestResults/source-hashes-v0210.json -Raw|ConvertFrom-Json)){
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed during Native test'}
}
$taskRun=Get-Content TestResults/v0210-final-last-run.txt -Raw
foreach($taskLine in @('Gatefall 0.21.0','Won elapsed=187.12 core=300 hunter=100 kills=944','Wave clear=True allSpawned=True spawned=944 remainingEnemies=0 bossDefeated=False','Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0 advancedSeconds=0.00')){
    if(-not $taskRun.Contains($taskLine)){throw 'Native victory/rules/caps differ'}
}
$taskLedger=Get-Content TestResults/v0210-final-native-purchase-ledger.json -Raw|ConvertFrom-Json
if($taskLedger.builds -ne 17 -or $taskLedger.upgrades -ne 9 -or $taskLedger.fusions -ne 1 -or $taskLedger.sales -ne 0 -or
    $taskLedger.actualSpend -ne 1060 -or $taskLedger.catalogPricedReceipts.Count -ne 27 -or
    ($taskLedger.catalogPricedReceipts|Measure-Object cost -Sum).Sum -ne 1060 -or $taskLedger.finalTowers -ne 16){throw 'Priced purchase ledger differs'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskSaves=@(foreach($taskPhase in @('before','result','map','relaunch','post-abandon')){
    $taskPath="TestResults/v0210-final-$taskPhase.sav";$taskHash=(Get-FileHash $taskPath).Hash.ToLowerInvariant()
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    $taskBefore=$taskPhase -eq 'before'
    $taskExpectedHash=if($taskBefore){'c85b059454993f432d48e8179bc4334ae990a78b7f63c448577a97cd81e418a7'}else{'68c2a388276b4ca6959dbd1488b32e6a8223ba2fa9e3c797e4026a0d49518aef'}
    if($taskHash -ne $taskExpectedHash -or (Get-Item $taskPath).Length -ne 324 -or -not $taskP.Valid() -or $taskP.PendingRun -ne '' -or
        $taskP.Loadout -ne 263 -or $taskP.MagicCores -ne $(if($taskBefore){196}else{232}) -or $taskP.StarsTotal -ne $(if($taskBefore){39}else{42}) -or
        $taskP.FirstCleared[13] -ne (-not $taskBefore) -or $taskP.BestStars[13] -ne $(if($taskBefore){0}else{3}) -or $taskP.LastReward -ne $(if($taskBefore){34}else{36}) -or
        @($taskP.Growth|Where-Object {$_ -ne 0}).Count -or @($taskP.Research|Where-Object {$_ -ne 0}).Count){throw "Synthetic save changed: $taskPhase"}
    [pscustomobject]@{phase=$taskPhase;bytes=324;sha256=$taskHash;magic=$taskP.MagicCores;stars=$taskP.StarsTotal;loadout=263;stage14Clear=$taskP.FirstCleared[13];stage14Stars=$taskP.BestStars[13];reward=$taskP.LastReward;rewardId=$taskP.LastRewardRun;pending='';valid=$true}
})
if($taskSaves[0].rewardId -eq $taskSaves[1].rewardId -or @($taskSaves[1..4].rewardId|Select-Object -Unique).Count -ne 1){throw 'Reward identity did not persist'}
$taskSaves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0210-final-save-check.json
$taskPerf=Get-Content TestResults/v0210-final-active-performance.json -Raw|ConvertFrom-Json
if($taskPerf.appPidFilter -ne 4978 -or $taskPerf.frameSamples -ne 7973 -or $taskPerf.battleRunningWindows -ne 15 -or $taskPerf.worstMs -ne 50 -or $taskPerf.framesOver33ms -ne 1){throw 'Scoped timing observations differ'}
$taskD=Get-Content TestResults/v0210-final-diagnostics.json -Raw|ConvertFrom-Json
if($taskD.appPid -ne 4978 -or $taskD.frameworkPid -ne '583' -or $taskD.frameworkAfter -ne '583' -or $taskD.surface -ne 51 -or $taskD.other -ne 0 -or $taskD.packageAnr -ne 0 -or $taskD.watchdog -ne 0){throw 'Diagnostics require review'}
foreach($taskName in @('card30','card90','card150','clearance','clearance-reinforced','result-check','result-map','relaunch-map','unlocked-spear-lv1','unlocked-marker-lv1','unlocked-catalyst-needed','unlocked-observer-disabled','post-abandon-map')){
    if(-not(Test-Path "Docs/QA/v050/v0210-final-$taskName.png")){throw "Native evidence missing: $taskName"}
}
$taskProbe=Get-Content TestResults/v0210-final-video-probe.json -Raw|ConvertFrom-Json
if([math]::Abs([double]$taskProbe.format.duration-11.988256) -gt .001 -or $taskProbe.format.size -ne '9405182'){throw 'Representative video differs'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0210-final.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.normalExit -or $taskCleanup.remainingProcesses -ne 0 -or -not $taskCleanup.diskFilesRemoved -or $taskCleanup.protectedDevicesTouched){throw 'Owned cleanup incomplete'}
[ordered]@{
    created=(Get-Date -Format o);task=233;version=$taskMeta.version;code=$taskMeta.versionCode;bytes=$taskMeta.bytes;sha256=$taskMeta.sha256;newProductOrApk=$false
    nativeStage=14;nativeFullBattle=$true;elapsed=187.12;kills=944;allSpawned=$true;remaining=0;bossRequired=$false;bossDefeated=$false;core=300;hunter=100;stars=3;reward=36
    builds=17;upgrades=9;fusions=1;sales=0;purchases=27;spent=1060;finalTowers=16;firstFusion=63.85;finalCreditsDirectlyObserved=$false;finalCredits=$null
    cards=@('Blast','Blast','Ballistic');extraCardCredits=0;supplies=5;supplyCredits=500;catalystsGranted=2;catalystsConsumed=1;waveCalls=0
    hud301Remaining39=$true;hud306Remaining5=$true;post180Upgrades=@('182.6 Spear2','183.4 Spear20','184.2 Spear3','185.1 Spear7');upgradeCausalClearTimeClaim=$false
    manualSkillMovementSupportCallIntended=$false;exactModelReplay=$false;all16FiredOrSimultaneousClaim=$false
    towerDamage=@{Spear=13751.15;Rupture=4009.352;Arc=117.3198;Marker=8};markedHits=3;markBonus=6;markBonusAlreadyIncludedInAlliedDamage=$true
    resultMapRelaunchAndReadyAbandonSavesEqual=$true;magicBefore=196;magicAfter=232;starsBefore=39;starsAfter=42;observerUnlockedByActualFirstClear=$true
    postUnlockReady=@{builds=2;upgrades=2;spent=155;towers=2;credits=165;elapsed=0;Spear=0;Marker=1;catalysts=0;observerOptionVisible=$true;observerCatalystShortageRetained=$true;observerCreatedOrFired=$false}
    firstDisabledTapActuallySeal=@{x=1294;y=709;evidence='unlocked-disabled-block'};correctedObserverDisabledTap=@{x=595;y=709;evidence='unlocked-observer-disabled';selection=0;credits=165;towers=2};recipeReadinessReordersTiles=$true
    activeSamples=7973;activeWindows=15;reportedRoundedAverageMs=16.7;worstActiveMs=50;over33ms=1;liveMenusAndRecordingIncluded=$true;physicalOrUninterruptedFullFrameBenchmark=$false
    appPid=4978;postAppPid=9289;frameworkPid=583;postFirstPerfSurface=51;postFirstPerfOtherEF=0;capturedPackageAnr=0;capturedWatchdogKill=0;diagnosticScope=$taskD.scope;underlyingDiagnosticRootCausesResolved=$false
    representativeVideoSeconds=11.988256;extractedFrames=6;visuallyReviewedFrames=@(2,5);fullRunVideoOrAllFrameOrAudioReview=$false
    humanFunTested=$false;physicalDeviceTested=$false;iosTested=$false;ownedCleanup=$taskCleanup;passed=$true
}|ConvertTo-Json -Depth 8|Set-Content TestResults/delivery-consistency-v0210-final.json
'PASS: same v0.21.0 Native1-14/944-clear187.12/16towers1060/36reward+actualObserverUnlock/4same result saves/post-unlockReady shortage+input guard/7973 scoped samples/owned cleanup; no human or phone benchmark'
