$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0183.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.18.3' -or $taskApk.versionCode -ne 51 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.18.3.apk','Builds/Android/Gatefall.apk','TestResults/v0183-installed.apk')) {
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw "APK differs: $taskFile"}
}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0183.json -Raw|ConvertFrom-Json)) {
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Source differs: $($taskRow.path)"}
}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0182.json -Raw|ConvertFrom-Json)) {
    if($taskRow.path -notin @('Assets\Scripts\Presentation\MobileHud.cs','ProjectSettings\ProjectSettings.asset') -and (Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Unexpected product source change: $($taskRow.path)"}
}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
$taskOriginal=(Get-FileHash TestResults/v0181-update-after.sav).Hash
foreach($taskFile in @('v0183-update-before.sav','v0183-update-after.sav','v0183-after.sav')) {
    $taskPath=Join-Path TestResults $taskFile
    if((Get-FileHash -LiteralPath $taskPath).Hash -ne $taskOriginal){throw 'Earned synthetic profile changed'}
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject $taskPath)))
    if(-not $taskP.Valid() -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or $taskP.PendingCrisis -or $taskP.MagicCores -ne 62 -or $taskP.StarsTotal -ne 15 -or $taskP.LastReward -ne 22 -or -not $taskP.IsStageOpen(7)){throw 'Profile progress differs'}
}
$taskPending=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject 'TestResults/v0183-pending.sav')))
if(-not $taskPending.Valid() -or $taskPending.PendingRun.Length -ne 32 -or $taskPending.PendingStage -ne 6){throw 'Native pending fixture missing'}
$taskUi=Get-Content TestResults/android-v0183-panel-observations.json -Raw|ConvertFrom-Json
foreach($taskFlag in @('oldReproducedInPreparation','readyDisabledRepeatedTapBlocked','readyPanelGapAndHeaderBlocked','enabledUpgradeWorked','outsidePadSwitchWorked','constructionPanelHeaderBlocked','enabledBuildWorked','outsideCloseWorked','liveDisabledAndGapBlocked','normalAbandonProfileEqualOriginal')){if(-not $taskUi.$taskFlag){throw "Native observation missing: $taskFlag"}}
if($taskUi.oldDisabledRecipeSelectedBackgroundPad -ne 23 -or $taskUi.liveClockBefore -ne '0:05' -or $taskUi.liveClockAfter -ne '0:09' -or $taskUi.liveCreditsUnchanged -ne 85 -or $taskUi.liveTowersUnchanged -ne 3){throw 'Native focused run differs'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing screenshot: $taskShot"}}
if((Get-Content TestResults/mobile-results.txt -Raw) -notmatch 'PASS: 27 mobile input/layout checks'){throw 'Mobile checks missing'}
$taskErrors=Get-Content TestResults/android-v0183-errors.json -Raw|ConvertFrom-Json
if($taskErrors.postFirstPerfErrors -ne 0 -or -not $taskErrors.graphics31Created -or $taskErrors.packageAnrLines -ne 0){throw 'Unexpected runtime diagnostics'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0183.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.directoryAbsent -or $taskCleanup.priorityRestored -ne 'Normal' -or -not $taskCleanup.protectedDevicesReceivedNoInput -or -not $taskCleanup.protectedBossCall -or -not $taskCleanup.protectedStage4Followup -or $taskCleanup.otherProcessesChanged){throw 'Owned cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;sha256=$taskApk.sha256;sourceInstalledApkMatch=$true;coreFilesUnchanged=6;onlyProductCodeChanged='MobileHud socket panel input ordering';mobileChecks=27;oldNativeBugReproduced=$true;nativeReadyAndRunningBlocked=$true;nativeEnabledAndOutsideActionsPreserved=$true;nativeLiveClockBefore='0:05';nativeLiveClockAfter='0:09';updateAndAbandonSaveEqual=$true;newRewardGranted=0;startupDiagnostics=$taskErrors.rawStartupErrorLines;postFirstPerfErrors=0;ownedAvdCleaned=$true;nativeFullBattleThisVersion=$false;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json|Set-Content TestResults/delivery-consistency-v0183.json
Write-Output 'PASS0.18.3 APK/source/install; old native bug reproduced; ready/live panel blocking; enabled/outside actions; unchanged core and profile; diagnostics and owned cleanup.'
