$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskApk=Get-Content TestResults/android-apk-v0203.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.20.3' -or $taskApk.versionCode -ne 60 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.20.3.apk','TestResults/v0203-lesson-installed.apk')){
    if((Get-FileHash $taskPath).Hash -ne $taskApk.sha256 -or (Get-Item $taskPath).Length -ne $taskApk.bytes){throw 'Built/installed APK mismatch'}
}
$taskOld=@(Get-Content TestResults/lesson-v0203-baseline/source-hashes.json -Raw|ConvertFrom-Json)
$taskChanged=@(foreach($taskR in (Get-Content TestResults/source-hashes-v0203.json -Raw|ConvertFrom-Json)){
    if((Get-FileHash -LiteralPath $taskR.path).Hash -ne $taskR.sha256){throw 'Current product source differs'}
    $taskB=@($taskOld|Where-Object path -eq $taskR.path)
    if($taskB.Count -ne 1){throw 'Missing unique prior product source'}
    if($taskB[0].sha256 -ne $taskR.sha256){$taskR.path}
})
if($taskChanged.Count -ne 2 -or 'Assets\Scripts\Core\Campaign.cs' -notin $taskChanged -or 'ProjectSettings\ProjectSettings.asset' -notin $taskChanged){throw 'Unexpected source changes'}
$taskOldCampaign=[IO.File]::ReadAllText((Join-Path (Get-Location) 'TestResults/lesson-v0203-baseline/Campaign.cs'))
if($taskOldCampaign.Replace('표식을 맞힌 적을 여러 공격 결계주가 함께 노리세요.','붕괴는 입구 쪽에 두세요. 표식과 창은 같은 길을 노리세요.') -cne [IO.File]::ReadAllText((Join-Path (Get-Location) 'Assets/Scripts/Core/Campaign.cs'))){throw 'Campaign differs beyond one lesson'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskHash='8735c3eb30d0209f52685ce1ad81fba26e4563ebb542ce7aadb811137f26f4c3'
$taskSeed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0203-lesson-seed.sav')))
foreach($taskPhase in @('seed','before-update','after-update','abandon','relaunch')){
    $taskPath="TestResults/v0203-lesson-$taskPhase.sav"
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if((Get-FileHash $taskPath).Hash -ne $taskHash -or (Get-Item $taskPath).Length -ne 324 -or -not $taskP.Valid() -or $taskP.MagicCores -ne 130 -or $taskP.StarsTotal -ne 33 -or $taskP.Loadout -ne 263 -or $taskP.LastReward -ne 30 -or $taskP.LastRewardRun -ne $taskSeed.LastRewardRun -or $taskP.FirstCleared[11] -or $taskP.IsStageOpen(12) -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1){throw "Profile preservation failed: $taskPhase"}
}
$taskUi=Get-Content TestResults/android-v0203-lesson-observations.json -Raw|ConvertFrom-Json
if(-not $taskUi.bothSentencesComplete -or $taskUi.readyScreens.Count -ne 3 -or $taskUi.fullBattle -or $taskUi.towers -ne 7 -or $taskUi.card30 -notlike 'Ballistic*' -or $taskUi.menuLive.beforeClock -ne '0:52' -or $taskUi.menuLive.afterClock -ne '0:57' -or -not $taskUi.abandonedNormally){throw 'Observation scope differs'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw 'Missing screenshot evidence'}}
$taskRaw=@(Get-Content TestResults/v0203-lesson-logcat-pre-relaunch.txt)
$taskApp=@($taskRaw|Where-Object {$_ -match '\s5118\s+\d+\s'})
$taskFirst=-1
for($taskI=0;$taskI -lt $taskApp.Count;$taskI++){if($taskApp[$taskI] -match 'GATEFALL PERF'){$taskFirst=$taskI;break}}
if($taskFirst -lt 0){throw 'Missing diagnostics boundary'}
$taskStartup=@($taskApp[0..$taskFirst]|Where-Object {$_ -match '\s[EF]\s'})
$taskLater=@($taskApp[($taskFirst+1)..($taskApp.Count-1)]|Where-Object {$_ -match '\s[EF]\s'})
$taskAnr=@($taskRaw|Where-Object {$_ -match 'ANR.*com\.gatefall\.prototype'})
if($taskStartup.Count -ne 5 -or $taskLater.Count -ne 6 -or @($taskLater|Where-Object {$_ -notmatch 'Surface'}).Count -or $taskAnr.Count -ne 0 -or -not($taskApp -match 'OpenGL ES 3.1')){throw 'Diagnostics scope differs'}
$taskMaxShots=0;$taskMaxHits=0
foreach($taskLine in $taskApp){if($taskLine -match 'GATEFALL PERF.*shots=([\d,]+) hits=([\d,]+)'){$taskMaxShots=[math]::Max($taskMaxShots,[int]($Matches[1].Split(',')[2]));$taskMaxHits=[math]::Max($taskMaxHits,[int]($Matches[2].Split(',')[2]))}}
if($taskMaxShots -ne 13 -or $taskMaxHits -ne 48){throw 'Native Rupture effect evidence differs'}
[pscustomobject]@{pid=5118;startupErrorLines=$taskStartup;postFirstPerfErrorLines=$taskLater;packageAnr=$taskAnr.Count;resolved=$false;performanceBenchmark=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/v0203-lesson-diagnostics.json
$taskCleanup=Get-Content TestResults/qa-cleanup-v0203.json -Raw|ConvertFrom-Json
if($taskCleanup.serial -ne 'emulator-5616' -or $taskCleanup.qemuPid -ne 26988 -or $taskCleanup.priorityRestored -ne 'Normal' -or $taskCleanup.processesAfterShutdown -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or -not $taskCleanup.protectedStage4Exists -or -not $taskCleanup.protectedBossExists -or -not $taskCleanup.removed -or (Test-Path -LiteralPath $taskCleanup.absoluteTarget)){throw 'Owned cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');task=196;version='0.20.3';versionCode=60;sha256=$taskApk.sha256;bytes=$taskApk.bytes;changedProductFiles=$taskChanged;onlyStage12LessonStringChanged=$true;combatEconomyAndPresentationUnchanged=$true;priorFullNativeStage12Version='0.20.2';newVersionFullBattle=$false;readyScreens=$taskUi.readyScreens;bothSentencesComplete=$true;nativeClock='0:57';towers=7;spend385InferredNotTerminalReceipt=$true;firstCard='Ballistic';frontRuptureSite=6;maxObservedRuptureShots=13;maxObservedRuptureHits=48;menuLive=$true;normalAbandonment=$true;updateAbandonRelaunchProfileIdentical=$true;profileBytes=324;profileHash=$taskHash;newReward=0;ownedAvdStopped=$true;ownedAvdRemoved=$true;startupDiagnosticLines=5;postFirstPerfSurfaceLines=6;packageAnr=0;diagnosisResolved=$false;performanceBenchmark=$false;humanReadabilityOrFunTested=$false;physicalAndroidTested=$false;iosTested=$false;audioReviewed=$false}|ConvertTo-Json -Depth 5|Set-Content TestResults/delivery-consistency-v0203.json
'PASS: v0.20.3 lesson-only/source/APK install/profile checks;3ready sizes/front Rupture13shots48hits/LIVE/normal abandonment. Short native acceptance, not a full battle or human fun test.'
