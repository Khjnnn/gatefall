$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskApk='f44822fe49a7a5634420a6256e1b2c19a8838f82472525436dcc3ced03631009'
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.20.7.apk','TestResults/v0207-mass-installed.apk')) {
    if((Get-FileHash $taskPath).Hash -ne $taskApk -or (Get-Item $taskPath).Length -ne 97355203){throw 'Delivered/installed APK differs'}
}
foreach($taskR in (Get-Content TestResults/source-hashes-v0207.json -Raw|ConvertFrom-Json)) {
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed during mass-fusion verification'}
}
$taskSeedHash='0ce7cb233287126d38f3a3a3f896778671cc28b78d0afd4915c19a88730542d8'
foreach($taskPath in @('TestResults/v0204-stage13-seed.sav','TestResults/v0207-mass-before.sav')) {
    if((Get-FileHash $taskPath).Hash -ne $taskSeedHash){throw 'Actual first-entry fixture differs'}
}
$taskSeed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0207-mass-before.sav')))
if(-not $taskSeed.Valid() -or $taskSeed.MagicCores -ne 162 -or $taskSeed.StarsTotal -ne 36 -or
    -not $taskSeed.IsStageOpen(12) -or $taskSeed.FirstCleared[12] -or $taskSeed.FirstCleared[13] -or $taskSeed.Loadout -ne 263){throw 'First-entry profile differs'}
$taskSaveHash='c85b059454993f432d48e8179bc4334ae990a78b7f63c448577a97cd81e418a7'
$taskSaves=@(foreach($taskPhase in @('result','city','relaunch')) {
    $taskPath="TestResults/v0207-mass-$taskPhase.sav"
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if((Get-FileHash $taskPath).Hash -ne $taskSaveHash -or (Get-Item $taskPath).Length -ne 324 -or -not $taskP.Valid() -or
        $taskP.MagicCores -ne 196 -or $taskP.StarsTotal -ne 39 -or $taskP.BestStars[12] -ne 3 -or -not $taskP.FirstCleared[12] -or
        -not $taskP.IsStageOpen(13) -or $taskP.FirstCleared[13] -or $taskP.LastReward -ne 34 -or
        $taskP.LastRewardRun -eq $taskSeed.LastRewardRun -or $taskP.LastRewardRun.Length -ne 32 -or $taskP.PendingRun -ne '' -or
        $taskP.PendingStage -ne -1 -or $taskP.Loadout -ne 263 -or @($taskP.Growth|Where-Object {$_ -ne 0}).Count -or
        @($taskP.Research|Where-Object {$_ -ne 0}).Count){throw "Earned profile differs: $taskPhase"}
    [pscustomobject]@{phase=$taskPhase;bytes=324;sha256=$taskSaveHash;valid=$true;magic=196;stars=39;reward=34;rewardRun=$taskP.LastRewardRun;pending='';stage14Open=$true;stage14Entered=$false;observerUnlocked=$false}
})
$taskSaves | ConvertTo-Json -Depth 4 | Set-Content TestResults/v0207-mass-save-check.json
$taskRun=Get-Content TestResults/v0207-mass-last-run.txt -Raw
foreach($taskPattern in @('Gatefall 0.20.7','Won elapsed=183.87 core=300 hunter=100 kills=928','fusions=1',
    'allSpawned=True spawned=928 remainingEnemies=0 bossDefeated=False',
    'Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0 advancedSeconds=0.00',
    'Mark hits=13 effective bonus damage=36.0')) {
    if(-not $taskRun.Contains($taskPattern)){throw "Terminal receipt differs: $taskPattern"}
}
$taskLedger=@(); $taskSpent=0; $taskBuilds=0; $taskUpgrades=0; $taskFusions=0
foreach($taskLine in $taskRun -split '\r?\n') {
    if($taskLine -match '^([\d.]+) (build|upgrade) (\w+) @(\d+)$') {
        $taskTime=[double]$Matches[1];$taskAction=$Matches[2];$taskKind=[Enum]::Parse([Gatefall.Core.TowerKind],$Matches[3]);$taskSite=[int]$Matches[4]
        $taskDef=[Gatefall.Core.TowerCatalog]::Get($taskKind)
        if($taskAction -eq 'build'){$taskCost=$taskDef.BuildCost;$taskBuilds++}else{$taskCost=$taskDef.UpgradeCost;$taskUpgrades++}
        $taskLedger += [pscustomobject]@{time=$taskTime;action=$taskAction;kind=$taskKind.ToString();site=$taskSite;cost=$taskCost};$taskSpent+=$taskCost
    } elseif($taskLine -match '^([\d.]+) fusion \(Autocannon\+Binding\) -> Arc @(\d+)$') {
        $taskLedger += [pscustomobject]@{time=[double]$Matches[1];action='fusion';kind='Arc';site=[int]$Matches[2];cost=40};$taskSpent+=40;$taskFusions++
    } elseif($taskLine -match '^\d.* (fusion|sell) '){throw 'Unexpected sale/combination'}
}
if($taskBuilds -ne 25 -or $taskUpgrades -ne 2 -or $taskFusions -ne 1 -or $taskLedger.Count -ne 28 -or $taskSpent -ne 1145 -or
    @($taskLedger|Where-Object {$_.action -eq 'build'}|Select-Object -ExpandProperty site -Unique).Count -ne 24 -or
    $taskLedger[-1].site -ne 23 -or $taskLedger[-1].time -ne 172.6){throw 'Receipt-derived purchases differ'}
$taskLedger | ConvertTo-Json | Set-Content TestResults/v0207-mass-native-purchase-ledger.json
$taskLog=Get-Content TestResults/v0207-mass-logcat.txt
$taskEvents=Get-Content TestResults/v0207-mass-events.txt
$taskPidLines=@($taskLog|Where-Object {$_ -match '^\d\d-\d\d \S+\s+6046\s+'})
$taskFirstPerf=@($taskPidLines|Where-Object {$_ -match 'GATEFALL PERF'}|Select-Object -First 1)
if($taskFirstPerf.Count -ne 1){throw 'No captured app PERF boundary'}
$taskBoundary=$taskFirstPerf[0].Substring(0,18)
$taskAfter=@($taskPidLines|Where-Object {$_.Substring(0,18) -ge $taskBoundary})
$taskErrors=@($taskAfter|Where-Object {$_ -match '\s[EF]\s'})
$taskSurface=@($taskErrors|Where-Object {$_ -match 'E Surface\s*:'})
$taskOther=@($taskErrors|Where-Object {$_ -notmatch 'E Surface\s*:'})
$taskFatal=@($taskPidLines|Where-Object {$_ -match '\sF\s'}).Count
$taskAnr=@($taskEvents|Where-Object {$_ -match 'am_anr:.*com.gatefall.prototype'}).Count
$taskWatchdog=@($taskLog|Where-Object {$_ -match 'W Watchdog: \*\*\* WATCHDOG KILLING SYSTEM PROCESS'}).Count
if($taskFatal -ne 0 -or $taskAnr -ne 0 -or $taskWatchdog -ne 0){throw 'Critical captured diagnostic requires review'}
$taskBefore=(Get-Content TestResults/v0207-mass-system-before.txt -Raw).Trim()
$taskAfterPid=(Get-Content TestResults/v0207-mass-system-after.txt -Raw).Trim()
if($taskBefore -ne '581' -or $taskAfterPid -ne '581'){throw 'Framework PID changed; inspect before reporting'}
foreach($taskShot in @('ready7','card30','arc-created','card90','card150','build22','build24','after-video24','city-after-clear','relaunch-first')) {
    if(-not(Test-Path "Docs/QA/v050/v0207-mass-$taskShot.png")){throw "Missing screen: $taskShot"}
}
$taskVideos=@(foreach($taskCount in @(18,24)) {
    $taskProbe=Get-Content "TestResults/v0207-mass-video$taskCount-probe.json" -Raw|ConvertFrom-Json
    if([double]$taskProbe.format.duration -lt 11 -or [double]$taskProbe.format.duration -gt 13){throw 'Representative video duration differs'}
    [pscustomobject]@{towers=$taskCount;seconds=[double]$taskProbe.format.duration;bytes=[int]$taskProbe.format.size;width=1920;height=864;wholeBattle=$false;allFramesReviewed=$false;audioReviewed=$false}
})
[ordered]@{
    created=(Get-Date -Format o);task=219;version='0.20.7';code=64;bytes=97355203;sha256=$taskApk
    productChanged=$false;newApk=$false;stage=13;scheduledEnemies=928;sites=24;finalTowers=24;builds=25;upgrades=2;fusions=1;purchases=28;spent=1145;sales=0
    elapsed=183.87;core=300;hunter=100;kills=928;allSpawned=$true;remainingEnemies=0;bossStage=$false;stars=3;reward=34
    firstFusionTime=70.2833340484649;lastBuildReceiptTime=172.6;cards=@('Range','Ballistic','Ballistic');cardCredits=0
    calls=0;supplyCredits=500;catalystsGranted=2;catalystsConsumed=1;catalystsRemaining=1;finalCreditsDirectlyCaptured=$false
    kindDamage=@(12731.24,0,42.8,4301.708,0,0,0,0,35,0);markedHits=13;markBonusAlreadyIncluded=36.0
    saveSha256=$taskSaveHash;resultCityRelaunchEqual=$true;stage14Open=$true;stage14Entered=$false;observerUnlocked=$false
    capturedAppPid=6046;observedFrameworkPid=581;postFirstPerfSurfaceErrors=$taskSurface.Count;postFirstPerfOtherEF=$taskOther.Count
    capturedAppFatal=$taskFatal;capturedPackageAnr=$taskAnr;capturedSystemWatchdogKills=$taskWatchdog
    diagnosticScope='Captured buffers through result, app6046 only; HOME/recording make PERF unsuitable for benchmarks; startup/Surface/GNSS root causes and physical impact unresolved'
    videos=$taskVideos;humanFunTested=$false;physicalDeviceTested=$false;iosTested=$false;audioTested=$false
    all24TowersFiredVerified=$false;individualTowerInvestmentSuperiorityVerified=$false;exactNativeModelReplay=$false
    directMovementOrSkillCommandsPlanned=$false;skillSuccessCountInstrumented=$false;safeBlank=@(2250,400);unsafeDodgeOverlapNotUsed=$true
    ownedCleanup=$(if(Test-Path TestResults/qa-cleanup-v0207-mass.json){Get-Content TestResults/qa-cleanup-v0207-mass.json -Raw|ConvertFrom-Json}else{$null})
    passed=$true
} | ConvertTo-Json -Depth 7 | Set-Content TestResults/delivery-consistency-v0207-mass.json
Write-Output "PASS: v0.20.7 24 towers/1145 spend/183.87s/928 clear/34 reward; save equal; Surface=$($taskSurface.Count), otherEF=$($taskOther.Count)"
