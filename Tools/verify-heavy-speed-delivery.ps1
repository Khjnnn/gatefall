$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0191.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.19.1' -or $taskApk.versionCode -ne 55 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.19.1.apk','Builds/Android/Gatefall.apk','TestResults/v0191-installed.apk')){
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw "APK differs: $taskFile"}
}
$taskSources=@(Get-Content TestResults/source-hashes-v0191.json -Raw|ConvertFrom-Json)
$taskBefore=@(Get-Content TestResults/source-hashes-v0190.json -Raw|ConvertFrom-Json)
$taskChanged=@()
foreach($taskRow in $taskSources){
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Source differs: $($taskRow.path)"}
    $taskOld=@($taskBefore|Where-Object path -eq $taskRow.path)
    if($taskOld.Count -ne 1){throw 'Missing unique baseline source'}
    if($taskOld[0].sha256 -ne $taskRow.sha256){$taskChanged+=$taskRow.path}
}
if($taskChanged.Count -ne 2 -or 'Assets\Scripts\Core\DefenseSimulation.cs' -notin $taskChanged -or 'ProjectSettings\ProjectSettings.asset' -notin $taskChanged){throw 'Unexpected product changes'}
$taskCurrent=[IO.File]::ReadAllText((Join-Path $taskProject 'Assets/Scripts/Core/DefenseSimulation.cs')).Replace("`r`n","`n")
$taskCandidate=[IO.File]::ReadAllText((Join-Path $taskProject 'TestResults/heavy-speed-v0190-review/speed125/DefenseSimulation.cs')).Replace("`r`n","`n")
if($taskCurrent -ne $taskCandidate){throw 'Product does not equal reviewed candidate'}
$taskAnalysis=Get-Content TestResults/heavy-speed-v0190-comparison.json -Raw|ConvertFrom-Json
if($taskAnalysis.newUniqueCandidateConditions -ne 21 -or $taskAnalysis.selectedCandidate -ne 'speed125' -or -not $taskAnalysis.selectedBaselineParity -or $taskAnalysis.fullNinePairs.Count -ne 9 -or @($taskAnalysis.fullNinePairs|Where-Object core -ne 300).Count -ne 0){throw 'Model scope differs'}
$taskBaseline=Get-Content TestResults/heavy-speed-v0190-baseline.json -Raw|ConvertFrom-Json
$taskSelected=Get-Content TestResults/heavy-speed-v0190-speed125.json -Raw|ConvertFrom-Json
if($taskBaseline.spawnStats.Count -ne 840 -or $taskSelected.spawnStats.Count -ne 840 -or (ConvertTo-Json -Compress -Depth 9 -InputObject $taskBaseline.contracts) -cne (ConvertTo-Json -Compress -Depth 9 -InputObject $taskSelected.contracts)){throw 'Spawn/geometry/schedule contract differs'}
$taskAffected=0
for($taskI=0;$taskI -lt 840;$taskI++){
    $taskA=$taskBaseline.spawnStats[$taskI];$taskB=$taskSelected.spawnStats[$taskI]
    foreach($taskKey in @('stage','crisis','wave','kind','hp')){if($taskA.$taskKey -ne $taskB.$taskKey){throw 'HP/type scope differs'}}
    $taskIsAffected=$taskA.stage -eq 9 -and -not $taskA.crisis -and $taskA.wave -ge 3 -and $taskA.kind -in @('Brute','Siege')
    $taskExpected=if($taskIsAffected){$taskAffected++;$taskA.speed*1.25}else{$taskA.speed}
    if([math]::Abs($taskExpected-$taskB.speed) -gt .000001){throw 'Unexpected speed change'}
}
if($taskAffected -ne 6){throw 'Unexpected affected spawn-stat cells'}
if((Get-Content TestResults/unity-android-v0191.log -Raw) -notmatch 'GATEFALL VALIDATION PASSED' -or (Get-Content TestResults/build-delivery-v0191.log -Raw) -notmatch 'ANDROID DELIVERY COMPLETE'){throw 'Build validation incomplete'}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
$taskSeed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject 'TestResults/v0191-heavy-seed.sav')))
if(-not $taskSeed.Valid() -or $taskSeed.MagicCores -ne 86 -or $taskSeed.StarsTotal -ne 16 -or $taskSeed.FirstCleared[8] -or $taskSeed.IsStageOpen(9)){throw 'Synthetic seed differs'}
$taskSeedHash=(Get-FileHash TestResults/v0191-heavy-seed.sav).Hash.ToLowerInvariant()
foreach($taskCase in @('before-update','after-update')){if((Get-FileHash "TestResults/v0191-$taskCase.sav").Hash.ToLowerInvariant() -ne $taskSeedHash){throw 'Update changed the seed'}}
$taskResultHash=(Get-FileHash TestResults/v0191-result.sav).Hash.ToLowerInvariant()
foreach($taskCase in @('result','city','relaunch')){
    $taskFile="TestResults/v0191-$taskCase.sav"
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject $taskFile)))
    if(-not $taskP.Valid() -or $taskP.MagicCores -ne 112 -or $taskP.StarsTotal -ne 19 -or $taskP.BestStars[8] -ne 3 -or -not $taskP.FirstCleared[8] -or $taskP.LastReward -ne 26 -or $taskP.LastRewardRun.Length -ne 32 -or $taskP.LastRewardRun -eq $taskSeed.LastRewardRun -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or -not $taskP.IsStageOpen(9)){throw "Native reward/unlock differs: $taskCase"}
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskResultHash){throw 'Return/relaunch save differs'}
}
$taskRun=Get-Content TestResults/v0191-last-run.txt -Raw
foreach($taskExpected in @('Gatefall 0.19.1','Won elapsed=173.42 core=300 hunter=100 kills=624','fusions=1','Wave clear=True allSpawned=True spawned=624 remainingEnemies=0 bossDefeated=False','Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0 advancedSeconds=0.00')){if(-not $taskRun.Contains($taskExpected)){throw "Native run differs: $taskExpected"}}
$taskActions=@();$taskSpent=0;$taskBuilds=0;$taskFusions=0
foreach($taskLine in $taskRun -split '\r?\n'){
    if($taskLine -match '^([\d.]+) (build|upgrade) (\w+) @(\d+)$'){
        $taskTime=[double]$Matches[1];$taskAction=$Matches[2];$taskKind=[Enum]::Parse([Gatefall.Core.TowerKind],$Matches[3]);$taskSlot=[int]$Matches[4];$taskD=[Gatefall.Core.TowerCatalog]::Get($taskKind)
        if($taskAction -eq 'build'){$taskCost=$taskD.BuildCost;$taskBuilds++}else{$taskCost=$taskD.UpgradeCost}
        $taskSpent+=$taskCost;$taskActions+=[pscustomobject]@{time=$taskTime;action=$taskAction;kind=$taskKind.ToString();site=$taskSlot;cost=$taskCost}
    }elseif($taskLine -match '^([\d.]+) fusion \((\w+)\+(\w+)\) -> (\w+) @(\d+)$'){
        $taskTime=[double]$Matches[1];$taskKind=$Matches[4];$taskSlot=[int]$Matches[5]
        $taskRecipes=@([Gatefall.Core.TowerCatalog]::Recipes|Where-Object {$_.Result.ToString() -eq $taskKind})
        if($taskRecipes.Count -ne 1){throw 'Nonunique fusion price'}
        $taskCost=$taskRecipes[0].Credits;$taskSpent+=$taskCost;$taskFusions++
        $taskActions+=[pscustomobject]@{time=$taskTime;action='fusion';kind=$taskKind;site=$taskSlot;cost=$taskCost}
    }
}
if($taskActions.Count -ne 22 -or $taskSpent -ne 915 -or $taskFusions -ne 1 -or $taskBuilds-$taskFusions -ne 14){throw 'Native purchase ledger differs'}
$taskActions|ConvertTo-Json -Depth 3|Set-Content TestResults/v0191-native-purchase-ledger.json
$taskUi=Get-Content TestResults/android-v0191-heavy-observations.json -Raw|ConvertFrom-Json
if(-not $taskUi.nativeFullBattle -or $taskUi.grossSpend -ne $taskSpent -or $taskUi.towers -ne 14 -or -not $taskUi.incidentalSkillInputIncluded -or $taskUi.cleanupAt170Seconds.remaining -ne 23 -or -not $taskUi.cleanupAt170Seconds.stillRunning){throw 'Native observation scope differs'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing screenshot: $taskShot"}}
$taskLayout=[Gatefall.Core.MobileLayout]::new(2400,1080,[Gatefall.Core.Box2]::new(136,0,2264,1080))
if($taskLayout.HitButton($taskLayout.FromScreen([Gatefall.Core.V2]::new(2200,260)),[Gatefall.Core.RunState]::Running) -ne [Gatefall.Core.MobileAction]::Skill -or $taskLayout.HitButton($taskLayout.FromScreen([Gatefall.Core.V2]::new(2250,680)),[Gatefall.Core.RunState]::Running) -ne [Gatefall.Core.MobileAction]::None){throw 'QA input correction differs'}
foreach($taskCase in @(@('v0191-video.json','v0191-heavy-combat.mp4',23,24),@('v0191-final-video.json','v0191-final-combat.mp4',21,22))){
    $taskVideo=Get-Content (Join-Path TestResults $taskCase[0]) -Raw|ConvertFrom-Json
    if([double]$taskVideo.format.duration -lt $taskCase[2] -or [double]$taskVideo.format.duration -gt $taskCase[3] -or $taskVideo.streams[0].width -ne 1920 -or $taskVideo.streams[0].height -ne 864 -or (Get-Item -LiteralPath (Join-Path 'Docs/QA/v050' $taskCase[1])).Length -ne [long]$taskVideo.format.size){throw 'Video metadata differs'}
}
$taskPid=[int](Get-Content TestResults/v0191-app-pid.txt)
$taskRaw=@(Get-Content TestResults/v0191-logcat.txt);$taskApp=@($taskRaw|Where-Object {$_ -match "\s$taskPid\s+\d+\s"})
$taskFirstPerf=-1;for($taskI=0;$taskI -lt $taskApp.Count;$taskI++){if($taskApp[$taskI] -match 'GATEFALL PERF'){$taskFirstPerf=$taskI;break}}
if($taskFirstPerf -lt 0){throw 'Missing native observation boundary'}
$taskStartup=@($taskApp[0..$taskFirstPerf]|Where-Object {$_ -match '\s[EF]\s'})
$taskLater=@($taskApp[($taskFirstPerf+1)..($taskApp.Count-1)]|Where-Object {$_ -match '\s[EF]\s'})
$taskAnr=@($taskRaw|Where-Object {$_ -match 'ANR.*com\.gatefall\.prototype'})
$taskGraphics=@($taskApp|Where-Object {$_ -match 'Version:.*OpenGL ES 3\.1'}).Count -gt 0
if($taskStartup.Count -ne 5 -or $taskLater.Count -ne 0 -or $taskAnr.Count -ne 0 -or -not $taskGraphics){throw 'Unexpected startup/runtime diagnostics; inspect raw log'}
[pscustomobject]@{appPid=$taskPid;firstPerfLine=$taskApp[$taskFirstPerf];rawStartupErrorLines=$taskStartup;startupErrorCount=$taskStartup.Count;graphics31Created=$taskGraphics;postFirstPerfErrors=$taskLater.Count;packageAnrLines=$taskAnr.Count;rootCauseResolved=$false;physicalImpactKnown=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/android-v0191-errors.json
$taskCleanup=Get-Content TestResults/qa-cleanup-v0191.json -Raw|ConvertFrom-Json
if($taskCleanup.phase -ne 'complete' -or $taskCleanup.name -ne 'GatefallHeavyV191' -or $taskCleanup.serial -ne 'emulator-5604' -or $taskCleanup.qemuPid -ne 25724 -or $taskCleanup.priorityAfter -ne 'Normal' -or $taskCleanup.processesAfterShutdown -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or -not $taskCleanup.removed -or -not $taskCleanup.protectedStage4Exists -or -not $taskCleanup.protectedBossExists -or (Test-Path -LiteralPath $taskCleanup.absoluteTarget)){throw 'Owned cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');valid=$taskP.Valid();seedBytes=(Get-Item TestResults/v0191-heavy-seed.sav).Length;resultBytes=(Get-Item TestResults/v0191-result.sav).Length;seedCores=86;cores=$taskP.MagicCores;seedStars=16;stars=$taskP.StarsTotal;bestStage9=$taskP.BestStars[8];lastReward=$taskP.LastReward;rewardId=$taskP.LastRewardRun;pending=$taskP.PendingRun;pendingStage=$taskP.PendingStage;stage10Open=$taskP.IsStageOpen(9);seedSha256=$taskSeedHash;updateHashEqual=$true;returnAndRelaunchHashEqual=$true;sha256=$taskResultHash}|ConvertTo-Json|Set-Content TestResults/v0191-save-check.json
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;bytes=$taskApk.bytes;sha256=$taskApk.sha256;installedApkMatch=$true;sourceFilesChecked=$taskSources.Count;changedProductFiles=$taskChanged;coreFilesUnchanged=5;combatEqualsReviewedCandidate=$true;normalStage9LateHeavySpeedScale=1.25;onlyAffectedActualEnemies=12;spawnStatCellsChecked=840;affectedStatCells=6;candidateConditions=21;reinforcedModelWins=9;nativeStage='1-9';nativeFullBattle=$true;elapsed=173.42;spawned=624;kills=624;remaining=0;bossStage=$false;core=300;hunter=100;towers=14;purchaseActions=22;grossSpend=$taskSpent;refunds=0;fusions=1;calls=0;supplies=5;supplyCredits=500;cardOffers=3;cardCreditsBonus=0;catalystsGranted=2;incidentalSkillInputIncluded=$true;exactSuccessfulSkillCount=$null;stars=3;reward=26;saveAndUnlockVerified=$true;returnAndRelaunchHashEqual=$true;ownedAvdCleaned=$true;startupDiagnosticCount=5;postFirstPerfErrors=0;packageAnrLines=0;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/delivery-consistency-v0191.json
'PASS: selected heavy speed scope, native624-enemy victory/reward/relaunch, QA input correction, APK/source identity and owned cleanup'
