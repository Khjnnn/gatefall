$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskApk='140fe4b1cedcb804776f7c267c74d587758dbadd3cf58c0d29c1acf60e8d1bfd'
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.20.4.apk','TestResults/v0204-stage13-installed.apk')) {
    if((Get-FileHash $taskPath).Hash -ne $taskApk -or (Get-Item $taskPath).Length -ne 97354499){throw 'Delivered/installed APK differs'}
}
foreach($taskR in (Get-Content TestResults/source-hashes-v0204.json -Raw|ConvertFrom-Json)) { if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed during stage13 review'} }
$taskSeedHash='0ce7cb233287126d38f3a3a3f896778671cc28b78d0afd4915c19a88730542d8'
foreach($taskFile in @('seed','before')){if((Get-FileHash "TestResults/v0204-stage13-$taskFile.sav").Hash -ne $taskSeedHash){throw 'Actual persistent fixture differs'}}
$taskSeed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0204-stage13-seed.sav')))
if(-not $taskSeed.Valid() -or $taskSeed.MagicCores -ne 162 -or $taskSeed.StarsTotal -ne 36 -or -not $taskSeed.IsStageOpen(12) -or $taskSeed.FirstCleared[12] -or $taskSeed.FirstCleared[13] -or $taskSeed.Loadout -ne 263){throw 'First entry fixture differs'}
$taskSaveHash='944dc9755d53a5b3bdf32b8a6430d977e73832fefbdd4697fb88b98f9e78547f'
$taskSaves=@(foreach($taskFile in @('result','city','relaunch')) {
    $taskPath="TestResults/v0204-stage13-$taskFile.sav"
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if((Get-FileHash $taskPath).Hash -ne $taskSaveHash -or (Get-Item $taskPath).Length -ne 324 -or -not $taskP.Valid() -or
        $taskP.MagicCores -ne 196 -or $taskP.StarsTotal -ne 39 -or $taskP.BestStars[12] -ne 3 -or -not $taskP.FirstCleared[12] -or
        -not $taskP.IsStageOpen(13) -or $taskP.FirstCleared[13] -or $taskP.LastReward -ne 34 -or
        $taskP.LastRewardRun -eq $taskSeed.LastRewardRun -or $taskP.LastRewardRun.Length -ne 32 -or $taskP.PendingRun -ne '' -or
        $taskP.PendingStage -ne -1 -or $taskP.Loadout -ne 263 -or @($taskP.Growth|Where-Object {$_ -ne 0}).Count -or
        @($taskP.Research|Where-Object {$_ -ne 0}).Count) {throw "Earned profile differs: $taskFile"}
    [pscustomobject]@{phase=$taskFile;bytes=324;sha256=$taskSaveHash;valid=$true;magic=196;stars=39;reward=34;rewardRun=$taskP.LastRewardRun;pending='';stage14Open=$true;stage14Entered=$false;observerUnlocked=$false}
})
$taskSaves | ConvertTo-Json -Depth 4 | Set-Content TestResults/v0204-stage13-save-check.json
$taskRun=Get-Content -LiteralPath TestResults/v0204-stage13-last-run.txt -Raw
if($taskRun -notmatch 'Won elapsed=188.57 core=300 hunter=100 kills=928' -or $taskRun -notmatch 'fusions=1' -or
    $taskRun -notmatch 'allSpawned=True spawned=928 remainingEnemies=0 bossDefeated=False' -or
    $taskRun -notmatch 'Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0' -or
    $taskRun -notmatch 'Mark hits=24 effective bonus damage=63.0') {throw 'Native terminal receipt differs'}
$taskLedger=@(); $taskSpent=0; $taskBuilds=0; $taskUpgrades=0; $taskFusions=0
foreach($taskLine in $taskRun -split '\r?\n') {
    if($taskLine -match '^([\d.]+) (build|upgrade) (\w+) @(\d+)$') {
        $taskTime=[double]$Matches[1]; $taskAction=$Matches[2]; $taskKind=[Enum]::Parse([Gatefall.Core.TowerKind],$Matches[3]); $taskSite=[int]$Matches[4]
        $taskDef=[Gatefall.Core.TowerCatalog]::Get($taskKind)
        if($taskAction -eq 'build'){$taskCost=$taskDef.BuildCost;$taskBuilds++}else{$taskCost=$taskDef.UpgradeCost;$taskUpgrades++}
        $taskLedger += [pscustomobject]@{time=$taskTime;action=$taskAction;kind=$taskKind.ToString();site=$taskSite;cost=$taskCost}; $taskSpent+=$taskCost
    } elseif($taskLine -match '^([\d.]+) fusion \(Autocannon\+Binding\) -> Arc @(\d+)$') {
        $taskLedger += [pscustomobject]@{time=[double]$Matches[1];action='fusion';kind='Arc';site=[int]$Matches[2];cost=40};$taskSpent+=40;$taskFusions++
    } elseif($taskLine -match '^\d.* (fusion|sell) ') {throw 'Unexpected sale/combination'}
}
if($taskBuilds -ne 17 -or $taskUpgrades -ne 6 -or $taskFusions -ne 1 -or $taskLedger.Count -ne 24 -or $taskSpent -ne 1050){throw 'Receipt-derived purchases differ'}
$taskLedger | ConvertTo-Json | Set-Content TestResults/v0204-stage13-native-purchase-ledger.json
$taskLog=Get-Content -LiteralPath TestResults/v0204-stage13-logcat.txt
$taskEvents=Get-Content -LiteralPath TestResults/v0204-stage13-events.txt
$taskPidLines=@($taskLog|Where-Object {$_ -match '^\d\d-\d\d \S+\s+6086\s+'})
$taskFirstPerf=@($taskPidLines|Where-Object {$_ -match 'GATEFALL PERF'}|Select-Object -First 1)
if($taskFirstPerf.Count -ne 1){throw 'No actual app PERF boundary'}
$taskFirstPerfTime=$taskFirstPerf[0].Substring(0,18)
$taskAfterPerf=@($taskPidLines|Where-Object {$_.Substring(0,18) -ge $taskFirstPerfTime})
$taskErrors=@($taskAfterPerf|Where-Object {$_ -match '\s[EF]\s'})
$taskSurface=@($taskErrors|Where-Object {$_ -match 'E Surface\s*:'})
$taskSurfaceBuffers=@($taskSurface|Where-Object {$_ -match 'freeAllBuffers|dequeueBuffer'})
$taskSurfaceSlots=@($taskSurface|Where-Object {$_ -match 'getSlotFromBufferLocked: unknown buffer'})
$taskOtherErrors=@($taskErrors|Where-Object {$_ -notmatch 'E Surface\s*:'})
$taskPackageAnr=@($taskEvents|Where-Object {$_ -match 'am_anr:.*com.gatefall.prototype'}).Count
$taskWatchdogKills=@($taskLog|Where-Object {$_ -match 'W Watchdog: \*\*\* WATCHDOG KILLING SYSTEM PROCESS'}).Count
if($taskPackageAnr -ne 0 -or $taskWatchdogKills -ne 0 -or @($taskPidLines|Where-Object {$_ -match '\sF\s'}).Count -ne 0){throw 'New critical diagnostic requires review'}
$taskScreens=@('city-loaded','ready7','three-lanes','card30','fusion-menu','fused-east','card90','reinforce12','reinforce15','card150','arc-upgrade-menu','final-progress','cleanup180','result','city-after-clear','relaunch-city')
foreach($taskShot in $taskScreens){if(-not(Test-Path -LiteralPath "Docs/QA/v050/v0204-stage13-$taskShot.png")){throw "Missing screenshot: $taskShot"}}
[ordered]@{
    created=(Get-Date -Format o);task=201;version='0.20.4';code=61;apkSha256=$taskApk;bytes=97354499
    stage=13;scheduledEnemies=928;sites=24;finalTowers=16;builds=17;upgrades=6;fusions=1;purchases=24;spent=1050
    elapsed=188.57;core=300;hunter=100;kills=928;allSpawned=$true;remaining=0;bossStage=$false
    stars=3;reward=34;stage14Open=$true;stage14Entered=$false;observerUnlocked=$false
    cards=@('Blast','Range','Range');calls=0;supplyCredits=500;cardCredits=0;catalystsGranted=2;catalystsConsumed=1
    kindDamage=@(11401.46,0,1383.5,4271.005,0,0,0,0,54.8,0);markedHits=24;markBonusAlreadyIncluded=63.0
    saveSha256=$taskSaveHash;resultCityRelaunchEqual=$true;safeLeft=136
    capturedAppPid=6086;observedFrameworkPid=579;postFirstPerfSurfaceErrors=$taskSurface.Count;surfaceFreeDequeue=$taskSurfaceBuffers.Count;surfaceUnknownSlot=$taskSurfaceSlots.Count;postFirstPerfOtherEF=$taskOtherErrors.Count
    capturedAppFatal=0;capturedPackageAnr=0;capturedSystemWatchdogKills=0
    diagnosticScope='Captured log buffers and before/after PIDs; no proof that previous GNSS watchdog/Surface issue is resolved'
    humanFunTested=$false;physicalDeviceTested=$false;iosTested=$false;audioTested=$false
    all16TowersFiredVerified=$false;individualTowerInvestmentSuperiorityVerified=$false;exactNativeModelReplay=$false
    initialInternalSeedAttemptExcluded=$true;invalidPauseFusionTapExcluded=$true;noNewApk=$true;productChanged=$false
    ownedCleanup=$(if(Test-Path TestResults/qa-cleanup-v0204-stage13.json){Get-Content TestResults/qa-cleanup-v0204-stage13.json -Raw|ConvertFrom-Json}else{$null})
    passed=$true
} | ConvertTo-Json -Depth 6 | Set-Content TestResults/delivery-consistency-v0204-stage13.json
Write-Output "PASS: 1-13 928-clear/188.57s/16 towers/1050 credits/34 reward and consistent saves; post-PERF Surface=$($taskSurface.Count), otherEF=$($taskOtherErrors.Count)"
