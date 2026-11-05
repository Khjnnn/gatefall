$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0190.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.19.0' -or $taskApk.versionCode -ne 54 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.19.0.apk','Builds/Android/Gatefall.apk','TestResults/v0190-installed.apk')) {
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw "APK differs: $taskFile"}
}
$taskSources=@(Get-Content TestResults/source-hashes-v0190.json -Raw|ConvertFrom-Json)
$taskBefore=@(Get-Content TestResults/source-hashes-v0185.json -Raw|ConvertFrom-Json)
$taskChanged=@()
foreach($taskRow in $taskSources) {
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Source differs: $($taskRow.path)"}
    $taskOld=@($taskBefore|Where-Object {$_.path -eq $taskRow.path})
    if($taskOld.Count -ne 1){throw 'Missing unique baseline source'}
    if($taskOld[0].sha256 -ne $taskRow.sha256){$taskChanged+=$taskRow.path}
}
$taskExpectedChanges=@('Assets\Scripts\Core\Campaign.cs','Assets\Scripts\Core\RaonCombat.cs','ProjectSettings\ProjectSettings.asset')
if($taskChanged.Count -ne 3 -or @($taskChanged|Where-Object {$_ -notin $taskExpectedChanges}).Count -ne 0){throw 'Unexpected product changes'}
$taskCurrentCombat=[IO.File]::ReadAllText((Join-Path $taskProject 'Assets/Scripts/Core/RaonCombat.cs')).Replace("`r`n","`n")
$taskCandidateCombat=[IO.File]::ReadAllText((Join-Path $taskProject 'TestResults/pulse-v0190-review/window12/RaonCombat.cs')).Replace("`r`n","`n")
if($taskCurrentCombat -ne $taskCandidateCombat){throw 'Product differs from reviewed combat candidate'}
$taskCampaign=[IO.File]::ReadAllText((Join-Path $taskProject 'Assets/Scripts/Core/Campaign.cs')).Replace("`r`n","`n")
$taskCampaign=$taskCampaign.Replace('후반에는 정예와 마수가 양쪽에서 짧게 몰려옵니다.','정예가 섞인 무리가 양쪽에서 접근합니다.').Replace('후반에는 양쪽에서 짧게 몰려옵니다. 붕괴의 범위 공격과 정예를 상대할 화력을 준비하세요.','정예를 상대할 화력을 남겨 두세요.')
$taskBaselineCampaign=[IO.File]::ReadAllText((Join-Path $taskProject 'TestResults/pulse-v0190-review/baseline/Campaign.cs')).Replace("`r`n","`n")
if($taskCampaign -ne $taskBaselineCampaign){throw 'Campaign changed beyond two stage-9 text strings'}
$taskComparison=Get-Content TestResults/pulse-v0190-comparison.json -Raw|ConvertFrom-Json
if($taskComparison.normalCrisisContractPairs -ne 56 -or -not $taskComparison.onlyNormalStage9LateTimingChanged -or -not $taskComparison.firstLaneArrivalsPreserved -or -not $taskComparison.timingObserverParity -or -not $taskComparison.pairedFinalSpendPreserved -or $taskComparison.allOutcomeCore -ne 300 -or $taskComparison.allOutcomeKills -ne 624){throw 'Recorded candidate scope differs'}
if((Get-Content TestResults/unity-android-v0190.log -Raw) -notmatch 'GATEFALL VALIDATION PASSED'){throw 'Missing Unity validation'}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
$taskSeed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject 'TestResults/v0190-pulse-seed.sav')))
if(-not $taskSeed.Valid() -or $taskSeed.MagicCores -ne 86 -or $taskSeed.StarsTotal -ne 16 -or $taskSeed.FirstCleared[8] -or -not $taskSeed.IsStageOpen(8) -or $taskSeed.IsStageOpen(9) -or $taskSeed.LastReward -ne 24){throw 'Synthetic seed differs'}
$taskSeedHash=(Get-FileHash TestResults/v0190-pulse-seed.sav).Hash.ToLowerInvariant()
foreach($taskCase in @('before-update','after-update','attempt-a-abandoned')) {
    $taskFile="TestResults/v0190-$taskCase.sav"
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskSeedHash){throw "Update/abandon changed seed: $taskCase"}
}
$taskResultHash=(Get-FileHash TestResults/v0190-result.sav).Hash.ToLowerInvariant()
foreach($taskCase in @('result','city','relaunch','relaunch-loaded')) {
    $taskFile="TestResults/v0190-$taskCase.sav"
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject $taskFile)))
    if(-not $taskP.Valid() -or $taskP.MagicCores -ne 112 -or $taskP.StarsTotal -ne 19 -or $taskP.BestStars[8] -ne 3 -or -not $taskP.FirstCleared[8] -or $taskP.LastReward -ne 26 -or $taskP.LastRewardRun.Length -ne 32 -or $taskP.LastRewardRun -eq $taskSeed.LastRewardRun -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or $taskP.PendingCrisis -or -not $taskP.IsStageOpen(9)){throw "Reward/unlock/pending differs: $taskCase"}
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskResultHash){throw "Return/relaunch changed profile: $taskCase"}
}
$taskRun=Get-Content TestResults/v0190-last-run.txt -Raw
foreach($taskExpected in @('Gatefall 0.19.0','Won elapsed=174.18 core=300 hunter=100 kills=624','fusions=2','Wave clear=True allSpawned=True spawned=624 remainingEnemies=0 bossDefeated=False','Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0 advancedSeconds=0.00')) {
    if(-not $taskRun.Contains($taskExpected)){throw "Run evidence differs: $taskExpected"}
}
# Reconstruct actual spending from the native ledger and unchanged catalogue prices.
$taskActions=@();$taskSpend=0;$taskBuilds=0;$taskFusions=0
foreach($taskLine in $taskRun -split '\r?\n') {
    if($taskLine -match '^([\d.]+) (build|upgrade) (\w+) @(\d+)$') {
        $taskTime=[double]$Matches[1];$taskAction=$Matches[2];$taskKind=[Enum]::Parse([Gatefall.Core.TowerKind],$Matches[3]);$taskSite=[int]$Matches[4]
        $taskDefinition=[Gatefall.Core.TowerCatalog]::Get($taskKind)
        if($taskAction -eq 'build'){$taskCost=$taskDefinition.BuildCost;$taskBuilds++}else{$taskCost=$taskDefinition.UpgradeCost}
        $taskSpend+=$taskCost;$taskActions+=[pscustomobject]@{time=$taskTime;action=$taskAction;kind=$taskKind.ToString();site=$taskSite;cost=$taskCost}
    }elseif($taskLine -match '^([\d.]+) fusion \((\w+)\+(\w+)\) -> (\w+) @(\d+)$') {
        $taskTime=[double]$Matches[1];$taskKind=$Matches[4];$taskSite=[int]$Matches[5]
        $taskRecipe=@([Gatefall.Core.TowerCatalog]::Recipes|Where-Object {$_.Result.ToString() -eq $taskKind})
        if($taskRecipe.Count -ne 1){throw 'Nonunique fusion price'}
        $taskCost=$taskRecipe[0].Credits;$taskSpend+=$taskCost;$taskFusions++
        $taskActions+=[pscustomobject]@{time=$taskTime;action='fusion';kind=$taskKind;site=$taskSite;cost=$taskCost}
    }
}
if($taskActions.Count -ne 26 -or $taskSpend -ne 1070 -or $taskBuilds-$taskFusions -ne 14 -or $taskFusions -ne 2){throw 'Native purchase ledger differs'}
$taskActions|ConvertTo-Json -Depth 3|Set-Content TestResults/v0190-native-purchase-ledger.json
$taskUi=Get-Content TestResults/android-v0190-pulse-observations.json -Raw|ConvertFrom-Json
if(-not $taskUi.nativeFullBattle -or $taskUi.cleanupAt171Seconds.remaining -ne 23 -or -not $taskUi.cleanupAt171Seconds.stillRunning -or $taskUi.towers -ne 14 -or $taskUi.grossSpend -ne $taskSpend -or -not $taskUi.attemptAExcluded){throw 'Native observation scope differs'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing screenshot: $taskShot"}}
foreach($taskCase in @(@('v0190-video.json','v0190-pulse-combat.mp4',23,24),@('v0190-final-video.json','v0190-final-wave.mp4',21,22))) {
    $taskVideo=Get-Content (Join-Path TestResults $taskCase[0]) -Raw|ConvertFrom-Json
    $taskVideoFile=Join-Path 'Docs/QA/v050' $taskCase[1]
    if([double]$taskVideo.format.duration -lt $taskCase[2] -or [double]$taskVideo.format.duration -gt $taskCase[3] -or $taskVideo.streams[0].width -ne 1920 -or $taskVideo.streams[0].height -ne 864 -or (Get-Item -LiteralPath $taskVideoFile).Length -ne [long]$taskVideo.format.size){throw 'Representative video metadata differs'}
}
$taskPid=[int](Get-Content TestResults/v0190-app-pid.txt)
$taskRaw=@(Get-Content TestResults/v0190-logcat.txt)
$taskApp=@($taskRaw|Where-Object {$_ -match "\s$taskPid\s+\d+\s"})
$taskFirstPerf=-1
for($taskI=0;$taskI -lt $taskApp.Count;$taskI++){if($taskApp[$taskI] -match 'GATEFALL PERF'){$taskFirstPerf=$taskI;break}}
if($taskFirstPerf -lt 0){throw 'Missing app observation boundary'}
$taskStartup=@($taskApp[0..$taskFirstPerf]|Where-Object {$_ -match '\s[EF]\s'})
$taskLater=@($taskApp[($taskFirstPerf+1)..($taskApp.Count-1)]|Where-Object {$_ -match '\s[EF]\s'})
$taskAnr=@($taskRaw|Where-Object {$_ -match 'ANR.*com\.gatefall\.prototype'})
$taskGraphics=@($taskApp|Where-Object {$_ -match 'Version:.*OpenGL ES 3\.1'}).Count -gt 0
if($taskStartup.Count -ne 5 -or $taskLater.Count -ne 0 -or $taskAnr.Count -ne 0 -or -not $taskGraphics){throw 'Unexpected diagnostics; inspect raw log'}
[pscustomobject]@{appPid=$taskPid;firstPerfLine=$taskApp[$taskFirstPerf];rawStartupErrorLines=$taskStartup;startupErrorCount=$taskStartup.Count;graphics31Created=$taskGraphics;postFirstPerfErrors=$taskLater.Count;packageAnrLines=$taskAnr.Count;includesAbandonedAttemptA=$true;rootCauseResolved=$false;physicalImpactKnown=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/android-v0190-errors.json
$taskCleanup=Get-Content TestResults/qa-cleanup-v0190.json -Raw|ConvertFrom-Json
if($taskCleanup.name -ne 'GatefallPulseV190' -or $taskCleanup.serial -ne 'emulator-5602' -or $taskCleanup.priorityAfter -ne 'Normal' -or $taskCleanup.processesAfterShutdown -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or -not $taskCleanup.removed -or -not $taskCleanup.protectedStage4Exists -or -not $taskCleanup.protectedBossExists -or (Test-Path -LiteralPath $taskCleanup.absoluteTarget)){throw 'Owned cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');valid=$taskP.Valid();seedBytes=(Get-Item TestResults/v0190-pulse-seed.sav).Length;resultBytes=(Get-Item TestResults/v0190-result.sav).Length;seedCores=86;cores=$taskP.MagicCores;seedStars=16;stars=$taskP.StarsTotal;bestStage9=$taskP.BestStars[8];lastReward=$taskP.LastReward;rewardId=$taskP.LastRewardRun;pending=$taskP.PendingRun;pendingStage=$taskP.PendingStage;stage10Open=$taskP.IsStageOpen(9);seedSha256=$taskSeedHash;updateAndAbandonHashEqual=$true;returnAndRelaunchHashEqual=$true;sha256=$taskResultHash}|ConvertTo-Json|Set-Content TestResults/v0190-save-check.json
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;bytes=$taskApk.bytes;sha256=$taskApk.sha256;installedApkMatch=$true;sourceFilesChecked=$taskSources.Count;changedProductFiles=$taskChanged;coreFilesUnchanged=4;combatEqualsReviewedCandidate=$true;campaignChangedOnlyStage9Text=$true;nativeStage='1-9';nativeFullBattle=$true;elapsed=174.18;spawned=624;kills=624;remaining=0;bossStage=$false;bossDefeated=$false;core=300;hunter=100;towers=14;purchaseActions=$taskActions.Count;grossSpend=$taskSpend;refunds=0;fusions=2;calls=0;supplies=5;supplyCredits=500;cardOffers=3;cardCreditsBonus=35;catalystsGranted=2;stars=3;reward=26;saveAndUnlockVerified=$true;returnAndRelaunchHashEqual=$true;attemptAExcluded=$true;ownedAvdCleaned=$true;startupDiagnosticCount=5;postFirstPerfErrors=0;packageAnrLines=0;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/delivery-consistency-v0190.json
'PASS: stage-9 pulse candidate, native 624-enemy victory/reward/save, APK/source identity and owned cleanup'
