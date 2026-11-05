$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0192.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.19.2' -or $taskApk.versionCode -ne 56 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.19.2.apk','Builds/Android/Gatefall.apk','TestResults/v0192-installed.apk')){if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw 'APK source/install differs'}}
$taskOld=@(Get-Content TestResults/source-hashes-v0191.json -Raw|ConvertFrom-Json)
$taskNew=@(Get-Content TestResults/source-hashes-v0192.json -Raw|ConvertFrom-Json)
$taskChanged=@(foreach($taskRow in $taskNew){
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product changed after build'}
    $taskPrevious=@($taskOld|Where-Object path -eq $taskRow.path)
    if($taskPrevious.Count -ne 1){throw 'Missing unique source baseline'}
    if($taskPrevious[0].sha256 -ne $taskRow.sha256){$taskRow.path}
})
if($taskChanged.Count -ne 2 -or @($taskChanged|Where-Object {$_ -notin @('Assets\Scripts\Presentation\MobileHud.cs','ProjectSettings\ProjectSettings.asset')}).Count){throw 'Unexpected product difference'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskSeedHash=(Get-FileHash TestResults/v0192-fusion-seed.sav).Hash
foreach($taskFile in @('v0192-before-update.sav','v0192-after-update.sav')){if((Get-FileHash (Join-Path TestResults $taskFile)).Hash -ne $taskSeedHash){throw 'Update changed synthetic profile'}}
$taskEarnedHash=(Get-FileHash TestResults/v0192-before-attemptB.sav).Hash
foreach($taskFile in @('v0192-before-attemptB.sav','v0192-B-after.sav','v0192-relaunch.sav','v0192-relaunch-loaded.sav')){
    $taskPath=Join-Path $taskProject ('TestResults/'+$taskFile)
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes($taskPath))
    if((Get-FileHash $taskPath).Hash -ne $taskEarnedHash -or -not $taskP.Valid() -or $taskP.MagicCores -ne 112 -or $taskP.StarsTotal -ne 19 -or $taskP.BestStars[8] -ne 3 -or $taskP.LastReward -ne 26 -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or -not $taskP.IsStageOpen(9)){throw 'Earned profile/normal abandonment/relaunch differs'}
}
$taskRun=Get-Content TestResults/v0192-attemptA-last-run.txt -Raw
if($taskRun -notmatch 'Won elapsed=192.12 core=300 hunter=100 kills=624 firstFusion=-1 fusions=0' -or $taskRun -notmatch 'allSpawned=True spawned=624 remainingEnemies=0'){throw 'AttemptA scope differs'}
if((Get-FileHash TestResults/v0192-B-stale-last-run.txt).Hash -ne (Get-FileHash TestResults/v0192-attemptA-last-run.txt).Hash){throw 'Stale A telemetry unexpectedly differs'}
$taskUi=Get-Content TestResults/android-v0192-lane-observations.json -Raw|ConvertFrom-Json
if(-not $taskUi.actualFusionAt9 -or -not $taskUi.anchor12ThenCancelThenAnchor9 -or -not $taskUi.cancelConsumedNoMaterialOrCatalyst -or -not $taskUi.singleEligiblePartnerStillRequiredExplicitTap -or $taskUi.attemptB.nativePurchaseTelemetryAvailable -or $taskUi.attemptB.fullBattle -or $taskUi.attemptB.newReward -ne 0 -or $taskUi.attemptA.fusionStrategyValidated){throw 'Native UI scope differs'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing UI evidence: $taskShot"}}
$taskErrors=Get-Content TestResults/android-v0192-errors.json -Raw|ConvertFrom-Json
if($taskErrors.startupErrorCount -ne 5 -or $taskErrors.postFirstPerfErrors -ne 3 -or $taskErrors.packageAnrLines -ne 0 -or -not $taskErrors.graphics31Created){throw 'Native diagnostic scope differs'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0192.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.removed -or (Test-Path -LiteralPath $taskCleanup.absoluteTarget) -or $taskCleanup.processesAfterShutdown -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or -not $taskCleanup.protectedBossExists -or -not $taskCleanup.protectedStage4Exists){throw 'Owned cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;sha256=$taskApk.sha256;bytes=$taskApk.bytes;changedProductFiles=$taskChanged;unchangedCoreFiles=6;sourceInstalledApkMatch=$true;updateSyntheticProfileUnchanged=$true;attemptATerminalTelemetry='192.12seconds624kills/core300/noFusion; result screen not observed';nativePreviewScreens=$taskUi.previewTextFullyDisplayed;nativeFusionAt9=$true;attemptBCompleteBattle=$false;nativePurchaseLogForB=$false;attemptBNewReward=0;earnedSaveSha256=$taskEarnedHash.ToLowerInvariant();abandonAndRelaunchPreserved=$true;startupDiagnostics=5;postFirstPerfSurfaceErrors=3;packageAnr=0;ownedAvdCleaned=$true;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/delivery-consistency-v0192.json
'PASS0.19.2 APK/source/install/update/save; native preview/cancel/east9 fusion; A/B scope separated; diagnostic3Surface lines preserved; owned cleanup'
