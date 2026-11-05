$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0171.json -Raw|ConvertFrom-Json
foreach($taskFile in @('Builds/Android/Gatefall-v0.17.1.apk','Builds/Android/Gatefall.apk','TestResults/v0171-installed.apk')) {
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256){throw "APK differs: $taskFile"}
}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0171.json -Raw|ConvertFrom-Json)) {
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Source changed: $($taskRow.path)"}
}
if((Get-FileHash TestResults/v0171-update-before.sav).Hash -ne (Get-FileHash TestResults/v0171-update-after.sav).Hash){throw 'Completed synthetic save changed on update'}
$taskParity=Get-Content TestResults/combat-parity-v0171.json -Raw|ConvertFrom-Json
if(-not $taskParity.simulationAndBalanceUnchanged){throw 'Combat parity missing'}
$taskProfile=Get-Content TestResults/v0171-update-profile-summary.json -Raw|ConvertFrom-Json
if(-not $taskProfile.valid -or $taskProfile.magic -ne 68 -or $taskProfile.stars -ne 21 -or -not $taskProfile.stage11Open -or -not $taskProfile.markerUnlocked -or $taskProfile.pending){throw 'Prior completed profile differs'}
$taskSmoke=Get-Content TestResults/android-v0171-hud-smoke.json -Raw|ConvertFrom-Json
if(-not $taskSmoke.northRiftUncovered -or -not $taskSmoke.speedIgnoredWhilePaused -or $taskSmoke.pauseClockBefore -ne $taskSmoke.pauseClockAfter -or $taskSmoke.pauseCreditsBefore -ne $taskSmoke.pauseCreditsAfter){throw 'Native focused UI smoke incomplete'}
$taskErrors=Get-Content TestResults/android-v0171-errors.json -Raw|ConvertFrom-Json
if($taskErrors.afterFirstPerfErrorLines -ne 0 -or -not $taskErrors.graphics31Created){throw 'Unexpected focused runtime errors'}
$taskAfter=Get-Content TestResults/v0171-hud-after-summary.json -Raw|ConvertFrom-Json
if(-not $taskAfter.valid -or $taskAfter.magic -ne 40 -or $taskAfter.stars -ne 18 -or $taskAfter.reward -ne 0){throw 'Focused-run profile differs'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0171.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.directoryAbsent -or $taskCleanup.priorityRestored -ne 'Normal' -or -not $taskCleanup.protectedDevicesReceivedNoInput){throw 'Owned cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;sha256=$taskApk.sha256;apkSourceInstalledMatch=$true;completedSyntheticSaveEqual=$true;simulationUnchangedFrom0170=$true;nativeFocusedHud=$taskSmoke;startupDiagnostics=$taskErrors.rawErrorLines;postFirstPerfErrors=$taskErrors.afterFirstPerfErrorLines;normalAbandonPendingRetained=([string]$taskAfter.pending).Length -gt 0;ownedAvdCleaned=$true;newFullClearPerformed=$false;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json -Depth 5|Set-Content TestResults/delivery-consistency-v0171.json
Write-Output 'PASS0.17.1 APK/source/install/completed-save parity and focused Android1-2-1/north/pause/cleanup. Full clear stays attributed to0.17.0; startup diagnostics and abandon-pending are retained.'
