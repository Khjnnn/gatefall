$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskApkHash='6a05295a1f807c00bbf1c5df4bcf36a6c4eef5e97facda7c01419f66b8f8d564'
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.20.2.apk','TestResults/v0202-stage12-installed.apk')) {
    if((Get-FileHash $taskPath).Hash -ne $taskApkHash -or (Get-Item $taskPath).Length -ne 98187224){throw "APK mismatch: $taskPath"}
}
foreach($taskFile in (Get-Content TestResults/source-hashes-v0202.json -Raw|ConvertFrom-Json)){
    if((Get-FileHash -LiteralPath $taskFile.path).Hash -ne $taskFile.sha256){throw "Product source mismatch: $($taskFile.path)"}
}
$taskSeedHash='8735c3eb30d0209f52685ce1ad81fba26e4563ebb542ce7aadb811137f26f4c3'
$taskSeed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0202-stage12-seed.sav')))
foreach($taskPath in @('TestResults/v0202-stage12-seed.sav','TestResults/v0202-stage12-before.sav')){
    if((Get-FileHash $taskPath).Hash -ne $taskSeedHash){throw 'Synthetic fixture changed before battle'}
}
if(-not $taskSeed.Valid() -or $taskSeed.MagicCores -ne 130 -or $taskSeed.StarsTotal -ne 33 -or $taskSeed.FirstCleared[11] -or -not $taskSeed.IsStageOpen(11) -or $taskSeed.Loadout -ne 263){throw 'Stage12 initial profile differs'}
$taskSaveHash='0ce7cb233287126d38f3a3a3f896778671cc28b78d0afd4915c19a88730542d8'
$taskSaves=@()
foreach($taskFile in @('result','city','relaunch')){
    $taskPath="TestResults/v0202-stage12-$taskFile.sav"
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if((Get-FileHash $taskPath).Hash -ne $taskSaveHash -or (Get-Item $taskPath).Length -ne 324 -or -not $taskP.Valid() -or $taskP.MagicCores -ne 162 -or $taskP.StarsTotal -ne 36 -or $taskP.BestStars[11] -ne 3 -or -not $taskP.FirstCleared[11] -or -not $taskP.IsStageOpen(12) -or $taskP.FirstCleared[12] -or $taskP.FirstCleared[13] -or $taskP.LastReward -ne 32 -or $taskP.LastRewardRun -eq $taskSeed.LastRewardRun -or $taskP.LastRewardRun.Length -ne 32 -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or $taskP.Loadout -ne 263 -or @($taskP.Growth|Where-Object {$_ -ne 0}).Count -or @($taskP.Research|Where-Object {$_ -ne 0}).Count){throw "Earned profile differs: $taskFile"}
    $taskSaves += [pscustomobject]@{phase=$taskFile;bytes=324;sha256=$taskSaveHash;valid=$true;magic=$taskP.MagicCores;stars=$taskP.StarsTotal;reward=$taskP.LastReward;rewardRun=$taskP.LastRewardRun;pending=$taskP.PendingRun;stage13Open=$true;stage13Entered=$false}
}
$taskSaves|ConvertTo-Json -Depth 4|Set-Content TestResults/v0202-stage12-save-check.json
$taskRun=Get-Content TestResults/v0202-stage12-last-run.txt -Raw
if($taskRun -notmatch 'Won elapsed=180.28 core=300 hunter=100 kills=880' -or $taskRun -notmatch 'fusions=1' -or $taskRun -notmatch 'allSpawned=True spawned=880 remainingEnemies=0 bossDefeated=False' -or $taskRun -notmatch 'Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0' -or $taskRun -notmatch 'Mark hits=118 effective bonus damage=315.4'){throw 'Native terminal receipt differs'}
$taskLedger=@();$taskSpent=0;$taskBuilds=0;$taskUpgrades=0;$taskFusions=0
foreach($taskLine in $taskRun -split '\r?\n'){
    if($taskLine -match '^([\d.]+) (build|upgrade) (\w+) @(\d+)$'){
        $taskTime=[double]$Matches[1];$taskAction=$Matches[2];$taskKind=[Enum]::Parse([Gatefall.Core.TowerKind],$Matches[3]);$taskSite=[int]$Matches[4];$taskDef=[Gatefall.Core.TowerCatalog]::Get($taskKind)
        if($taskAction -eq 'build'){$taskCost=$taskDef.BuildCost;$taskBuilds++}else{$taskCost=$taskDef.UpgradeCost;$taskUpgrades++}
        $taskLedger += [pscustomobject]@{time=$taskTime;action=$taskAction;kind=$taskKind.ToString();site=$taskSite;cost=$taskCost};$taskSpent+=$taskCost
    }elseif($taskLine -match '^([\d.]+) fusion \(Autocannon\+Binding\) -> Arc @(\d+)$'){
        $taskLedger += [pscustomobject]@{time=[double]$Matches[1];action='fusion';kind='Arc';site=[int]$Matches[2];cost=40};$taskSpent+=40;$taskFusions++
    }elseif($taskLine -match '^\d.* (fusion|sell) '){throw 'Unexpected fusion or sale'}
}
if($taskBuilds -ne 17 -or $taskUpgrades -ne 10 -or $taskFusions -ne 1 -or $taskLedger.Count -ne 28 -or $taskSpent -ne 1130 -or ($taskBuilds-$taskFusions) -ne 16){throw 'Purchase ledger differs'}
$taskLedger|ConvertTo-Json -Depth 4|Set-Content TestResults/v0202-stage12-native-purchase-ledger.json
$taskRaw=@(Get-Content TestResults/v0202-stage12-logcat-pre-relaunch.txt)
$taskApp=@($taskRaw|Where-Object {$_ -match '\s6742\s+\d+\s'})
$taskFirst=-1
for($taskI=0;$taskI -lt $taskApp.Count;$taskI++){if($taskApp[$taskI] -match 'GATEFALL PERF'){$taskFirst=$taskI;break}}
if($taskFirst -lt 0){throw 'Missing app process observation boundary'}
$taskStartup=@($taskApp[0..$taskFirst]|Where-Object {$_ -match '\s[EF]\s'})
$taskLater=@($taskApp[($taskFirst+1)..($taskApp.Count-1)]|Where-Object {$_ -match '\s[EF]\s'})
$taskAnr=@($taskRaw|Where-Object {$_ -match 'ANR.*com\.gatefall\.prototype'})
if($taskStartup.Count -ne 5 -or $taskLater.Count -ne 52 -or @($taskLater|Where-Object {$_ -notmatch 'Surface'}).Count -or $taskAnr.Count -ne 0 -or -not($taskApp -match 'OpenGL ES 3.1')){throw 'Recorded diagnostics scope differs'}
[pscustomobject]@{pid=6742;startupErrorLines=$taskStartup;postFirstPerfErrorLines=$taskLater;packageAnr=$taskAnr.Count;postFirstPerfSurfaceLines=$taskLater.Count;diagnosisResolved=$false;performanceBenchmark=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/v0202-stage12-diagnostics.json
$taskUi=Get-Content TestResults/android-v0202-stage12-observations.json -Raw|ConvertFrom-Json
if(-not $taskUi.fullBattle -or $taskUi.towers -ne 16 -or $taskUi.all16FiringVerified -or $taskUi.card30IdentityVerified -or $taskUi.cleanupHud.hudClock -ne '3:00' -or $taskUi.cleanupHud.remaining -ne 12 -or $taskUi.cleanupHud.preciseElapsedVerified -or $taskUi.ruptureDamage -ne 0){throw 'Observation limits differ'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing screenshot: $taskShot"}}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0202-stage12.json -Raw|ConvertFrom-Json
if($taskCleanup.serial -ne 'emulator-5614' -or $taskCleanup.qemuPid -ne 34972 -or $taskCleanup.priorityRestored -ne 'Normal' -or $taskCleanup.processesAfterShutdown -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or -not $taskCleanup.protectedStage4Exists -or -not $taskCleanup.protectedBossExists){throw 'Owned shutdown evidence differs'}
if(-not $taskCleanup.removed){throw 'Owned cleanup still pending'}
if(Test-Path -LiteralPath $taskCleanup.absoluteTarget){throw 'Owned AVD directory remains'}
[pscustomobject]@{created=(Get-Date).ToString('o');task=194;version='0.20.2';versionCode=59;sha256=$taskApkHash;bytes=98187224;newBuild=$false;productSourceChanged=$false;installedHashEqual=$true;syntheticFixtureLoadEqual=$true;stage=12;nativeFullBattle=$true;towers=16;builds=17;upgrades=10;fusions=1;spent=1130;elapsed=180.28;kills=880;core=300;hunter=100;stars=3;reward=32;arcDamage=5377.307;markerOwnDamage=305.8001;markHits=118;markBonusAlreadyInAlliedDamage=315.4;ruptureDamage=0;investmentSuperiorityResolved=$false;all16FiringVerified=$false;card30IdentityVerified=$false;card90='Range';card150='Range';supplies=5;supplyCredits=500;cardOffers=3;catalystsGranted=2;catalystsConsumed=1;waveCalls=0;profileResultMapRelaunchEqual=$true;saveSha256=$taskSaveHash;stage13Open=$true;stage13Entered=$false;startupErrorLines=5;postFirstPerfSurfaceLines=52;packageAnr=0;diagnosisResolved=$false;performanceBenchmark=$false;wholeBattleVideo=$false;ownedAvdStopped=$true;ownedAvdRemoved=$true;humanFunEvaluated=$false;physicalAndroidTested=$false;iosTested=$false;audioReviewed=$false}|ConvertTo-Json -Depth 5|Set-Content TestResults/delivery-consistency-v0202-stage12.json
'PASS: unchanged v0.20.2 installation/source; native1-12 annihilation, legal purchase costs, reward/profile and scoped evidence/cleanup verified. No human fun/physical-device claim.'
