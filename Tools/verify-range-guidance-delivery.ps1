$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskApk=Get-Content TestResults/android-apk-v0201.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.20.1' -or $taskApk.versionCode -ne 58 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.20.1.apk','Builds/Android/Gatefall.apk','TestResults/v0201-installed.apk')){
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw 'APK delivery/install differs'}
}
$taskOld=@(Get-Content TestResults/source-hashes-v0200.json -Raw|ConvertFrom-Json)
$taskNew=@(Get-Content TestResults/source-hashes-v0201.json -Raw|ConvertFrom-Json)
$taskChanged=@(foreach($taskRow in $taskNew){
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product changed after build'}
    $taskPrevious=@($taskOld|Where-Object path -eq $taskRow.path)
    if($taskPrevious.Count -ne 1){throw 'Missing unique baseline'}
    if($taskPrevious[0].sha256 -ne $taskRow.sha256){$taskRow.path}
})
if($taskChanged.Count -ne 2 -or @($taskChanged|Where-Object {$_ -notin @('Assets\Scripts\Presentation\MobileHud.cs','ProjectSettings\ProjectSettings.asset')}).Count){throw 'Unexpected product change'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskSeedHash=(Get-FileHash TestResults/v0201-range-seed.sav).Hash.ToLowerInvariant()
foreach($taskFile in @('v0201-range-seed.sav','v0201-before-update.sav','v0201-after-update.sav','v0201-after-abandon.sav','v0201-relaunch.sav')){
    $taskPath=Join-Path TestResults $taskFile
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if((Get-FileHash -LiteralPath $taskPath).Hash.ToLowerInvariant() -ne $taskSeedHash -or (Get-Item $taskPath).Length -ne 324 -or -not $taskP.Valid() -or $taskP.MagicCores -ne 86 -or $taskP.StarsTotal -ne 17 -or $taskP.BestStars[7] -ne 3 -or $taskP.LastReward -ne 24 -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or -not $taskP.IsStageOpen(8)){throw 'Update/abandon/relaunch altered earned profile'}
}
$taskUi=Get-Content TestResults/android-v0201-range-observations.json -Raw|ConvertFrom-Json
if($taskUi.fullBattle -or $taskUi.rearTowerCount -ne 0 -or $taskUi.rearFusionCount -ne 0 -or $taskUi.frontTowerCount -ne 3 -or $taskUi.frontFusionCount -ne 4 -or -not $taskUi.readyCountHidden -or -not $taskUi.cancelPreservedMaterials -or -not $taskUi.actualArcAt17 -or $taskUi.catalystBefore -ne 1 -or $taskUi.catalystAfter -ne 0 -or $taskUi.newReward -ne 0){throw 'Native observation scope differs'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing native evidence: $taskShot"}}
# The pre-abandon log isolates the tested process; a relaunch has a different PID/startup boundary.
$taskRaw=@(Get-Content TestResults/v0201-logcat-pre-abandon.txt)
$taskPid=4979
$taskApp=@($taskRaw|Where-Object {$_ -match "\s$taskPid\s+\d+\s"})
$taskFirstPerf=-1
for($taskI=0;$taskI -lt $taskApp.Count;$taskI++){if($taskApp[$taskI] -match 'GATEFALL PERF'){$taskFirstPerf=$taskI;break}}
if($taskFirstPerf -lt 0){throw 'Missing native observation boundary'}
$taskStartup=@($taskApp[0..$taskFirstPerf]|Where-Object {$_ -match '\s[EF]\s'})
$taskLater=@($taskApp[($taskFirstPerf+1)..($taskApp.Count-1)]|Where-Object {$_ -match '\s[EF]\s'})
$taskAnr=@($taskRaw|Where-Object {$_ -match 'ANR.*com\.gatefall\.prototype'})
$taskGraphics=@($taskApp|Where-Object {$_ -match 'Version:.*OpenGL ES 3\.1'}).Count -gt 0
$taskFx=@($taskApp|Where-Object {$_ -match 'GATEFALL PERF.*shots=207,52,3,3,0,0,0,0,0,0 hits=207,52,2,7,0,0,0,0,0,0'})
if(-not $taskFx.Count -or $taskStartup.Count -ne 5 -or $taskAnr.Count -ne 0 -or -not $taskGraphics){throw 'Inspect raw diagnostic/shot evidence'}
[pscustomobject]@{appPid=$taskPid;firstPerfLine=$taskApp[$taskFirstPerf];rawStartupErrorLines=$taskStartup;startupErrorCount=$taskStartup.Count;postFirstPerfErrors=$taskLater.Count;postFirstPerfErrorLines=$taskLater;packageAnrLines=$taskAnr.Count;graphics31Created=$taskGraphics;rootCauseResolved=$false;physicalImpactKnown=$false;scope='Focused UI test before abandon/relaunch, including HOME/focus changes. Frame values include background intervals and are not a benchmark.'}|ConvertTo-Json -Depth 4|Set-Content TestResults/android-v0201-errors.json
$taskCleanup=Get-Content TestResults/qa-cleanup-v0201.json -Raw|ConvertFrom-Json
if($taskCleanup.processesAfterShutdown -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or -not $taskCleanup.protectedBossExists -or -not $taskCleanup.protectedStage4Exists){throw 'Owned shutdown/protected paths differ'}
if($taskCleanup.removed){
    if(Test-Path -LiteralPath $taskCleanup.absoluteTarget){throw 'Cleanup claims removal but path exists'}
} else {
    # Host redirection can keep two tiny log handles open after QEMU/launcher shutdown.
    # This is a disclosed housekeeping remainder, not evidence of a running AVD or failed APK.
    $taskRemainder=@(Get-ChildItem -LiteralPath $taskCleanup.absoluteTarget -Force)
    if($taskCleanup.phase -ne 'delayed-log-removal' -or $taskRemainder.Count -ne 2 -or @($taskRemainder|Where-Object {$_.PSIsContainer -or $_.Name -notin @('emulator-err.log','emulator-out.log') -or $_.Length -gt 1024 -or ($_.Attributes -band [IO.FileAttributes]::ReparsePoint)}).Count){throw 'Unexpected cleanup remainder'}
}
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;sha256=$taskApk.sha256;bytes=$taskApk.bytes;changedProductFiles=$taskChanged;unchangedCoreFiles=6;sourceInstalledApkMatch=$true;nativeStage='1-8';approximatelyCombatSeconds=97;fullBattleOnThisApk=$false;currentRangeCountVerified=$true;readyCountHidden=$true;rearTowerAndFusionCount=0;frontTowerCount=3;frontFusionCount=4;cancelPreservedMaterials=$true;actualArcAt17=$true;nativeArcShots=3;nativeArcHits=7;newReward=0;earnedMagic=86;earnedStars=17;priorReward=24;saveSha256=$taskSeedHash;updateAbandonRelaunchSaveIdentical=$true;startupDiagnosticLines=$taskStartup.Count;postFirstPerfErrors=$taskLater.Count;packageAnr=$taskAnr.Count;ownedAvdStopped=$true;ownedAvdCleaned=[bool]$taskCleanup.removed;cleanupLogRemovalDeferred=(-not $taskCleanup.removed);humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false;investmentValueResolved=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/delivery-consistency-v0201.json
'PASS 0.20.1 product delivery/native UI/save; diagnostics and exact cleanup remainder recorded separately.'
