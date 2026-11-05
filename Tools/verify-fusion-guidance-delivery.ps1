$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0182.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.18.2' -or $taskApk.versionCode -ne 50 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'Release metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.18.2.apk','Builds/Android/Gatefall.apk','TestResults/v0182-installed.apk')) {
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw "APK differs: $taskFile"}
}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0182.json -Raw|ConvertFrom-Json)) {
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Source differs: $($taskRow.path)"}
}
foreach($taskFile in (Get-ChildItem Assets/Scripts/Core -Filter '*.cs')) {
    if((Get-FileHash -LiteralPath $taskFile.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path TestResults/fusion-v0182-baseline/Core $taskFile.Name)).Hash){throw 'Core changed'}
}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
$taskOriginal=(Get-FileHash TestResults/v0181-update-after.sav).Hash
foreach($taskFile in @('v0182-update-before.sav','v0182-update-after.sav','v0182-candidate0-after.sav','v0182-candidate1-after.sav','v0182-release-update-before.sav','v0182-release-update-after.sav','v0182-release-after.sav')) {
    $taskPath=Join-Path TestResults $taskFile
    if((Get-FileHash -LiteralPath $taskPath).Hash -ne $taskOriginal){throw "Earned synthetic profile differs: $taskFile"}
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject $taskPath)))
    if(-not $taskP.Valid() -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or $taskP.PendingCrisis -or $taskP.MagicCores -ne 62 -or $taskP.StarsTotal -ne 15 -or $taskP.LastReward -ne 22 -or -not $taskP.IsStageOpen(7)){throw 'Profile validity/reward differs'}
}
$taskModels=(Get-Content TestResults/fusion-position-v0182-models.json -Raw|ConvertFrom-Json).rows
if($taskModels.Count -ne 6 -or @($taskModels|Where-Object {$_.Spent -ne 940 -or $_.Towers -ne 14 -or $_.Core -ne 300 -or $_.Kills -ne 552 -or $_.State -ne 'Won' -or $_.CompletedPurchases -ne 21}).Count){throw 'Position comparison differs'}
if(@($taskModels|Where-Object {$_.Anchor -eq 0 -and $_.ArcShots -ne 0}).Count -or @($taskModels|Where-Object {$_.Anchor -eq 11 -and $_.ArcShots -le 0}).Count){throw 'Position observation differs'}
$taskUi=Get-Content TestResults/android-v0182-fusion-observations.json -Raw|ConvertFrom-Json
if(-not $taskUi.singlePartnerPreview -or -not $taskUi.rearPreview -or -not $taskUi.frontPreview -or -not $taskUi.cancelPreservedMaterials -or -not $taskUi.fusedAtSelectedFront -or -not $taskUi.previewCombatContinued -or -not $taskUi.noticeAvoidedPreview -or $taskUi.displayedPreviewRange -ne '5.9'){throw 'Native release UI observations incomplete'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing UI evidence: $taskShot"}}
$taskErrors=Get-Content TestResults/android-v0182-errors.json -Raw|ConvertFrom-Json
if($taskErrors.postFirstPerfErrors -ne 0 -or -not $taskErrors.graphics31Created){throw 'Native diagnostics differ'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0182.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.directoryAbsent -or $taskCleanup.priorityRestored -ne 'Normal' -or -not $taskCleanup.protectedDevicesReceivedNoInput -or -not $taskCleanup.protectedBossCall -or -not $taskCleanup.protectedStage4Followup){throw 'Owned cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;sha256=$taskApk.sha256;sourceInstalledApkMatch=$true;coreFilesUnchanged=6;pairedModels=3;sameBudget=940;rearArcShots=@($taskModels|Where-Object Anchor -eq 0|ForEach-Object ArcShots);frontArcShots=@($taskModels|Where-Object Anchor -eq 11|ForEach-Object ArcShots);nativeSinglePartnerPreview=$true;nativeRearAndFrontPreview=$true;nativeFusionAtFront=$true;nativeNoticeAvoidance=$true;completedSyntheticProfileUnchanged=$true;newRewardGranted=0;startupDiagnostics=$taskErrors.rawStartupErrorLines;postFirstPerfErrors=0;ownedAvdCleaned=$true;nativeFullBattleThisVersion=$false;bindingValueResolved=$false;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/delivery-consistency-v0182.json
Write-Output 'PASS0.18.2 APK/source/install; six unchanged core files; same-budget position models; native preview/cancel/front fusion/notice avoidance; update and abandon save preservation; owned cleanup.'
