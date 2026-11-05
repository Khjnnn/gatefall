$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0184.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.18.4' -or $taskApk.versionCode -ne 52 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.18.4.apk','Builds/Android/Gatefall.apk','TestResults/v0184-installed.apk')) {
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw "APK differs: $taskFile"}
}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0184.json -Raw|ConvertFrom-Json)) {
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Source differs: $($taskRow.path)"}
}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0183.json -Raw|ConvertFrom-Json)) {
    if($taskRow.path -notin @('Assets\Scripts\Presentation\MobileHud.cs','ProjectSettings\ProjectSettings.asset') -and (Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Unexpected product code change'}
}
$taskModels=Get-Content TestResults/binding-position-v0183-models.json -Raw|ConvertFrom-Json
if($taskModels.baselineRerun -or $taskModels.baseline.Count -ne 3 -or $taskModels.rows.Count -ne 9 -or (Get-FileHash Tests/FusionPositionReview.cs).Hash.ToLowerInvariant() -ne $taskModels.purchaseFixtureSha256){throw 'Model provenance differs'}
foreach($taskRow in $taskModels.rows){if($taskRow.State -ne 'Won' -or $taskRow.Kills -ne 552 -or $taskRow.Core -ne 300 -or $taskRow.Spent -ne 940 -or $taskRow.Towers -ne 14 -or $taskRow.FinalCredits -ne 120 -or $taskRow.CompletedPurchases -ne 21 -or $taskRow.MaxPurchaseDelay -gt .051){throw 'Controlled position condition differs'}}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
$taskOriginal=(Get-FileHash TestResults/v0181-update-after.sav).Hash
foreach($taskFile in @('v0184-update-before.sav','v0184-update-after.sav','v0184-after.sav')) {
    $taskPath=Join-Path TestResults $taskFile
    if((Get-FileHash -LiteralPath $taskPath).Hash -ne $taskOriginal){throw 'Earned synthetic profile changed'}
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject $taskPath)))
    if(-not $taskP.Valid() -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or $taskP.PendingCrisis -or $taskP.MagicCores -ne 62 -or $taskP.StarsTotal -ne 15 -or $taskP.LastReward -ne 22 -or -not $taskP.IsStageOpen(7)){throw 'Profile progress differs'}
}
$taskUi=Get-Content TestResults/android-v0184-binding-observations.json -Raw|ConvertFrom-Json
if($taskUi.bindingSocket -ne 16 -or $taskUi.level1TargetText -ne '최대 3→4명' -or $taskUi.level2TargetText -ne '최대 4명' -or -not $taskUi.textVisibleWithoutOverlap -or -not $taskUi.slowRingObserved -or $taskUi.otherControlTowersBuilt -or -not $taskUi.normalAbandonSaveEqualOriginal){throw 'Native observation differs'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw 'Missing native screenshot'}}
$taskCensus=Get-Content TestResults/android-v0184-census.json -Raw|ConvertFrom-Json
if($taskCensus[0].bindingShots -ne 52 -or $taskCensus[1].bindingShots -ne 78 -or $taskCensus[1].bindingHits -ne 78 -or $taskCensus[1].arcShots -ne 0){throw 'Native effect counters differ'}
$taskErrors=Get-Content TestResults/android-v0184-errors.json -Raw|ConvertFrom-Json
if($taskErrors.postFirstPerfErrors -ne 0 -or -not $taskErrors.graphics31Created -or $taskErrors.packageAnrLines -ne 0){throw 'Unexpected native diagnostics'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0184.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.directoryAbsent -or $taskCleanup.priorityRestored -ne 'Normal' -or -not $taskCleanup.protectedDevicesReceivedNoInput -or -not $taskCleanup.protectedBossCall -or -not $taskCleanup.protectedStage4Followup){throw 'Owned cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;sha256=$taskApk.sha256;sourceInstalledApkMatch=$true;coreFilesUnchanged=6;newPositionModels=9;reusedBaselineModels=3;sameModelSpend=940;modelTowers=14;allModelCore=300;nativeBindingSocket=16;nativeTowers=6;nativeBindingShotEffects=78;nativeBindingHitEffects=78;level1AndLevel2TextVerified=$true;updateAndAbandonSaveEqual=$true;startupDiagnostics=$taskErrors.rawStartupErrorLines;postFirstPerfErrors=0;ownedAvdCleaned=$true;nativeFullBattleThisVersion=$false;bindingInvestmentSuperiorityProven=$false;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json|Set-Content TestResults/delivery-consistency-v0184.json
Write-Output 'PASS0.18.4 APK/source/install; nine controlled placement models and reused baseline; native binding firing/slow ring/stat caps; earned save preserved; diagnostics and owned cleanup.'
