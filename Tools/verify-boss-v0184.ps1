$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0184.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.18.4' -or $taskApk.versionCode -ne 52 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.18.4.apk','Builds/Android/Gatefall.apk','TestResults/v0184-boss-installed.apk')) {
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw "APK differs: $taskFile"}
}
$taskSources=@(Get-Content TestResults/source-hashes-v0184.json -Raw|ConvertFrom-Json)
foreach($taskRow in $taskSources) {
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Product source changed: $($taskRow.path)"}
}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
$taskProfiles=@{}
foreach($taskCase in @('seed','result','city','relaunch')) {
    $taskPath=Join-Path $taskProject "TestResults/v0184-boss-$taskCase.sav"
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes($taskPath))
    if(-not $taskP.Valid()){throw "Invalid profile: $taskCase"}
    $taskProfiles[$taskCase]=$taskP
}
$taskSeed=$taskProfiles['seed']
if($taskSeed.MagicCores -ne 40 -or $taskSeed.StarsTotal -ne 10 -or $taskSeed.FirstSealComplete -or -not $taskSeed.IsStageOpen(5) -or $taskSeed.IsStageOpen(6)){throw 'Synthetic baseline differs'}
foreach($taskCase in @('result','city','relaunch')) {
    $taskP=$taskProfiles[$taskCase]
    if($taskP.MagicCores -ne 60 -or $taskP.StarsTotal -ne 13 -or $taskP.BestStars[5] -ne 3 -or -not $taskP.FirstCleared[5] -or $taskP.LastReward -ne 20 -or $taskP.LastRewardRun.Length -ne 32 -or $taskP.LastRewardRun -eq $taskSeed.LastRewardRun -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or $taskP.PendingCrisis -or -not $taskP.FirstSealComplete -or -not $taskP.IsStageOpen(6) -or -not $taskP.IsHunterOpen(2)){throw "Reward/unlock/pending differs: $taskCase"}
    if((Get-FileHash "TestResults/v0184-boss-$taskCase.sav").Hash -ne (Get-FileHash TestResults/v0184-boss-result.sav).Hash){throw "Profile changed on return/relaunch: $taskCase"}
}
$taskRun=Get-Content TestResults/v0184-boss-last-run.txt -Raw
foreach($taskExpected in @('Gatefall 0.18.4','Won elapsed=180.75 core=278 hunter=68 kills=883','fusions=1','Wave clear=True allSpawned=True spawned=883 remainingEnemies=0 bossDefeated=True','Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=1 advancedSeconds=5.63','141.37 wave=6 advance=5.63')) {
    if(-not $taskRun.Contains($taskExpected)){throw "Run evidence differs: $taskExpected"}
}
$taskPurchases=@($taskRun -split '\r?\n'|Where-Object {$_ -match '^\d+\.\d+ (build|upgrade|fusion)'})
if($taskPurchases.Count -ne 21){throw 'Purchase ledger incomplete'}
$taskUi=Get-Content TestResults/android-v0184-boss-observations.json -Raw|ConvertFrom-Json
if(-not $taskUi.nativeFullBattle -or -not $taskUi.bossWarningTextVisible -or -not $taskUi.bossRedCircleVisible -or -not $taskUi.bossDefeatedHudVisibleBeforeVictory -or $taskUi.cleanupAtShown180Seconds.remaining -ne 2 -or -not $taskUi.cleanupAtShown180Seconds.stillRunning -or $taskUi.grossSpend -ne 940 -or $taskUi.towers -ne 14){throw 'Native observations incomplete'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing screenshot: $taskShot"}}
$taskVideo=Get-Content TestResults/v0184-boss-video.json -Raw|ConvertFrom-Json
if($taskVideo.format.duration -lt 23 -or $taskVideo.format.duration -gt 24 -or $taskVideo.streams[0].width -ne 1920 -or $taskVideo.streams[0].height -ne 864){throw 'Representative video metadata differs'}
$taskPid=[int](Get-Content TestResults/v0184-boss-app-pid.txt)
$taskRaw=@(Get-Content TestResults/v0184-boss-logcat.txt)
$taskApp=@($taskRaw|Where-Object {$_ -match "\s$taskPid\s+\d+\s"})
$taskFirstPerf=-1
for($taskI=0;$taskI -lt $taskApp.Count;$taskI++){if($taskApp[$taskI] -match 'GATEFALL PERF'){$taskFirstPerf=$taskI;break}}
if($taskFirstPerf -lt 0){throw 'Missing app performance observation boundary'}
$taskStartup=@($taskApp[0..$taskFirstPerf]|Where-Object {$_ -match '\s[EF]\s'})
$taskLater=@($taskApp[($taskFirstPerf+1)..($taskApp.Count-1)]|Where-Object {$_ -match '\s[EF]\s'})
$taskAnr=@($taskRaw|Where-Object {$_ -match 'ANR in com\.gatefall\.prototype|ANR.*com\.gatefall\.prototype'})
$taskGraphics=@($taskApp|Where-Object {$_ -match 'Version:.*OpenGL ES 3\.1'}).Count -gt 0
if($taskStartup.Count -ne 5 -or $taskLater.Count -ne 0 -or $taskAnr.Count -ne 0 -or -not $taskGraphics){throw 'Unexpected diagnostics; inspect preserved raw log'}
$taskErrors=[pscustomobject]@{appPid=$taskPid;firstPerfLine=$taskApp[$taskFirstPerf];startupErrorCount=$taskStartup.Count;rawStartupErrorLines=$taskStartup;graphics31Created=$taskGraphics;postFirstPerfErrors=$taskLater.Count;packageAnrLines=$taskAnr.Count;rootCauseResolved=$false;physicalImpactKnown=$false}
$taskErrors|ConvertTo-Json -Depth 5|Set-Content TestResults/android-v0184-boss-errors.json
$taskP=$taskProfiles['result']
$taskCleanup=Get-Content TestResults/qa-cleanup-v0184-boss.json -Raw|ConvertFrom-Json
if($taskCleanup.name -ne 'GatefallBossV184' -or $taskCleanup.serial -ne 'emulator-5598' -or $taskCleanup.qemuPid -ne 30716 -or $taskCleanup.priorityRestored -ne 'Normal' -or $taskCleanup.processesAfterShutdown -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or -not $taskCleanup.removed -or -not $taskCleanup.protectedStage4Exists -or -not $taskCleanup.protectedBossExists -or (Test-Path -LiteralPath $taskCleanup.absoluteTarget)){throw 'Owned AVD cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');valid=$taskP.Valid();baselineBytes=(Get-Item TestResults/v0184-boss-seed.sav).Length;resultBytes=(Get-Item TestResults/v0184-boss-result.sav).Length;baselineCores=$taskSeed.MagicCores;cores=$taskP.MagicCores;baselineStars=$taskSeed.StarsTotal;stars=$taskP.StarsTotal;bestStage6=$taskP.BestStars[5];lastReward=$taskP.LastReward;rewardId=$taskP.LastRewardRun;pending=$taskP.PendingRun;pendingStage=$taskP.PendingStage;firstSeal=$taskP.FirstSealComplete;hunter2Open=$taskP.IsHunterOpen(2);crisisEligible=$taskP.FirstSealComplete;stage7Open=$taskP.IsStageOpen(6);returnAndRelaunchHashEqual=$true;sha256=(Get-FileHash TestResults/v0184-boss-result.sav).Hash.ToLowerInvariant()}|ConvertTo-Json|Set-Content TestResults/v0184-boss-save-check.json
[pscustomobject]@{created=(Get-Date).ToString('o');version='0.18.4';versionCode=52;sha256=$taskApk.sha256;productUnchanged=$true;sourceFilesChecked=$taskSources.Count;installedApkEqual=$true;nativeStage='1-6';nativeFullBattle=$true;elapsed=180.75;spawned=883;kills=883;remaining=0;bossDefeated=$true;core=278;hunter=68;towers=14;grossSpend=940;purchaseActions=$taskPurchases.Count;fusions=1;calls=1;callTime=141.37;advanceSeconds=5.63;supplies=5;supplyCredits=500;cardOffers=3;catalystsGranted=2;warningTextAndCircleObserved=$true;bossDeathBeforeWinObserved=$true;remaining2At180Observed=$true;stars=3;reward=20;saveAndUnlockVerified=$true;returnAndRelaunchHashEqual=$true;startupDiagnosticCount=5;postFirstPerfErrors=0;packageAnrLines=0;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json|Set-Content TestResults/delivery-consistency-v0184-boss.json
'PASS: current APK 1-6 native boss/call/cleanup/reward/relaunch evidence; no product change'
