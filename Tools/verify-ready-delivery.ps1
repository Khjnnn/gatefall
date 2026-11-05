$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0185.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.18.5' -or $taskApk.versionCode -ne 53 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.18.5.apk','Builds/Android/Gatefall.apk','TestResults/v0185-ready-installed.apk')) {
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw "APK differs: $taskFile"}
}
$taskSources=@(Get-Content TestResults/source-hashes-v0185.json -Raw|ConvertFrom-Json)
$taskBefore=@(Get-Content TestResults/ready-v0185-baseline/source-hashes.json -Raw|ConvertFrom-Json)
$taskChanged=@()
foreach($taskRow in $taskSources) {
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Source differs: $($taskRow.path)"}
    $taskOld=@($taskBefore|Where-Object {$_.path -eq $taskRow.path})
    if($taskOld.Count -ne 1){throw 'Missing unique baseline source'}
    if($taskOld[0].sha256 -ne $taskRow.sha256){$taskChanged+=$taskRow.path}
}
if($taskChanged.Count -ne 2 -or -not ($taskChanged -contains 'Assets\Scripts\Presentation\MobileHud.cs') -or -not ($taskChanged -contains 'ProjectSettings\ProjectSettings.asset')){throw 'Unexpected product changes'}
$taskOldHud=[IO.File]::ReadAllText((Join-Path $taskProject 'TestResults/ready-v0185-baseline/MobileHud.cs'))
$taskHud=[IO.File]::ReadAllText((Join-Path $taskProject 'Assets/Scripts/Presentation/MobileHud.cs'))
if($taskOldHud.Replace('controller.LessonText, 15, Paper','controller.LessonText.Replace(". ", ".\n"), 15, Paper') -ne $taskHud){throw 'HUD changed beyond the displayed sentence break'}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
$taskSeed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject 'TestResults/v0185-ready-seed.sav')))
if(-not $taskSeed.Valid() -or $taskSeed.MagicCores -ne 62 -or $taskSeed.StarsTotal -ne 14 -or -not $taskSeed.ShouldShowLesson(7)){throw 'Synthetic baseline differs'}
$taskSeedHash=(Get-FileHash TestResults/v0185-ready-seed.sav).Hash.ToLowerInvariant()
foreach($taskCase in @('before-update','after-update','after-abandon','relaunch')) {
    $taskFile="TestResults/v0185-ready-$taskCase.sav"
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject $taskFile)))
    if(-not $taskP.Valid() -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or $taskP.MagicCores -ne 62 -or $taskP.StarsTotal -ne 14 -or $taskP.LastReward -ne 22 -or $taskP.LastRewardRun -ne $taskSeed.LastRewardRun -or $taskP.FirstCleared[7] -or (Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskSeedHash){throw "Save/reward changed: $taskCase"}
}
$taskUi=Get-Content TestResults/android-v0185-ready-observations.json -Raw|ConvertFrom-Json
if($taskUi.baselineNarrowOrphan -ne '다.' -or $taskUi.finalLines -ne 2 -or -not $taskUi.finalSentencesComplete -or -not $taskUi.lessonHiddenAfterStart -or $taskUi.towerMenuLiveClockFirst -ne '0:04' -or $taskUi.towerMenuLiveClockSecond -ne '0:09' -or $taskUi.fullBattleOnThisApk){throw 'Native scope differs'}
foreach($taskShot in @($taskUi.nativeScreens.shot)+@($taskUi.baselineScreenshots)+@($taskUi.inputScreenshots)){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing screenshot: $taskShot"}}
if((Get-Content TestResults/unity-android-v0185.log -Raw) -notmatch 'GATEFALL VALIDATION PASSED'){throw 'Missing Unity validation'}
$taskPid=[int](Get-Content TestResults/v0185-ready-app-pid.txt)
$taskRaw=@(Get-Content TestResults/v0185-ready-logcat.txt)
$taskApp=@($taskRaw|Where-Object {$_ -match "\s$taskPid\s+\d+\s"})
$taskFirstPerf=-1
for($taskI=0;$taskI -lt $taskApp.Count;$taskI++){if($taskApp[$taskI] -match 'GATEFALL PERF'){$taskFirstPerf=$taskI;break}}
if($taskFirstPerf -lt 0){throw 'Missing app observation boundary'}
$taskStartup=@($taskApp[0..$taskFirstPerf]|Where-Object {$_ -match '\s[EF]\s'})
$taskLater=@($taskApp[($taskFirstPerf+1)..($taskApp.Count-1)]|Where-Object {$_ -match '\s[EF]\s'})
$taskAnr=@($taskRaw|Where-Object {$_ -match 'ANR.*com\.gatefall\.prototype'})
$taskGraphics=@($taskApp|Where-Object {$_ -match 'Version:.*OpenGL ES 3\.1'}).Count -gt 0
if($taskStartup.Count -ne 5 -or $taskLater.Count -ne 0 -or $taskAnr.Count -ne 0 -or -not $taskGraphics){throw 'Unexpected app diagnostics; inspect raw log'}
[pscustomobject]@{appPid=$taskPid;firstPerfLine=$taskApp[$taskFirstPerf];rawStartupErrorLines=$taskStartup;startupErrorCount=$taskStartup.Count;graphics31Created=$taskGraphics;postFirstPerfErrors=$taskLater.Count;packageAnrLines=$taskAnr.Count;rootCauseResolved=$false;physicalImpactKnown=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/android-v0185-errors.json
$taskCleanup=Get-Content TestResults/qa-cleanup-v0185-ready.json -Raw|ConvertFrom-Json
if($taskCleanup.name -ne 'GatefallReadyV185' -or $taskCleanup.serial -ne 'emulator-5600' -or $taskCleanup.priorityAfter -ne 'Normal' -or $taskCleanup.processesAfterShutdown -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or -not $taskCleanup.removed -or -not $taskCleanup.protectedStage4Exists -or -not $taskCleanup.protectedBossExists -or (Test-Path -LiteralPath $taskCleanup.absoluteTarget)){throw 'Owned cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;bytes=$taskApk.bytes;sha256=$taskApk.sha256;installedApkMatch=$true;changedProductFiles=$taskChanged;coreFilesUnchanged=6;nativeStage='1-8';nativeAspectScreens=3;baselineOrphanReproduced=$true;finalCompleteSentenceLines=2;nativeMenuClockFirst='0:04';nativeMenuClockSecond='0:09';approximatelyCombatSeconds=11;fullBattleOnThisApk=$false;fixtureBytes=324;fixtureMagic=62;fixtureStars=14;priorReward=22;priorRewardId=$taskSeed.LastRewardRun;fixtureSha256=$taskSeedHash;updateAbandonRelaunchHashEqual=$true;ownedAvdCleaned=$true;startupDiagnosticCount=5;postFirstPerfErrors=0;packageAnrLines=0;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/delivery-consistency-v0185.json
'PASS: ready sentence layout native evidence, APK/update/save and owned cleanup; six core files unchanged'
