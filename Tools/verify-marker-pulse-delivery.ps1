$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskApk=Get-Content TestResults/android-apk-v0202.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.20.2' -or $taskApk.versionCode -ne 59 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'Final APK metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.20.2.apk','Builds/Android/Gatefall.apk','TestResults/v0202-installed.apk')){
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw 'Final delivery/installed APK differs'}
}
$taskRejected=Get-Content TestResults/android-apk-v0202-candidate0.json -Raw|ConvertFrom-Json
if((Get-FileHash TestResults/v0202-candidate0.apk).Hash.ToLowerInvariant() -ne $taskRejected.sha256 -or $taskRejected.sha256 -eq $taskApk.sha256){throw 'Rejected UI candidate not separated'}
$taskOld=Get-Content TestResults/source-hashes-v0201.json -Raw|ConvertFrom-Json
$taskNew=Get-Content TestResults/source-hashes-v0202.json -Raw|ConvertFrom-Json
$taskChanged=@(foreach($taskRow in $taskNew){
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Source changed after build'}
    $taskPrevious=@($taskOld|Where-Object path -eq $taskRow.path)
    if($taskPrevious.Count -ne 1){throw 'Source baseline not unique'}
    if($taskPrevious[0].sha256 -ne $taskRow.sha256){$taskRow.path}
})
if($taskChanged.Count -ne 3 -or @($taskChanged|Where-Object {$_ -notin @('Assets\Scripts\Core\RaonCombat.cs','Assets\Scripts\Presentation\MobileHud.cs','ProjectSettings\ProjectSettings.asset')}).Count){throw 'Unexpected product difference'}
$taskReviewed=Get-Content TestResults/marker-pulse-v0201-review/window16/RaonCombat.cs -Raw
if($taskReviewed.Replace("`r`n","`n") -cne (Get-Content Assets/Scripts/Core/RaonCombat.cs -Raw).Replace("`r`n","`n")){throw 'Product differs from reviewed16-second candidate'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskSeed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0202-marker-seed.sav')))
$taskSeedHash=(Get-FileHash TestResults/v0202-marker-seed.sav).Hash
foreach($taskFile in @('v0202-before-update.sav','v0202-after-update.sav','v0202-candidate0-after.sav','v0202-final-after-update.sav')){if((Get-FileHash (Join-Path TestResults $taskFile)).Hash -ne $taskSeedHash){throw 'Update/rejected-attempt abandonment changed fixture'}}
if($taskSeed.MagicCores -ne 100 -or $taskSeed.StarsTotal -ne 30 -or $taskSeed.Loadout -ne 263 -or $taskSeed.FirstCleared[10] -or @($taskSeed.Growth|Where-Object {$_ -ne 0}).Count -or @($taskSeed.Research|Where-Object {$_ -ne 0}).Count){throw 'First-unlock fixture differs'}
$taskResultHash=(Get-FileHash TestResults/v0202-result.sav).Hash
foreach($taskFile in @('v0202-result.sav','v0202-city.sav','v0202-relaunch.sav')){
    $taskPath=Join-Path (Get-Location) ('TestResults/'+$taskFile)
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes($taskPath))
    if((Get-FileHash $taskPath).Hash -ne $taskResultHash -or (Get-Item $taskPath).Length -ne 324 -or -not $taskP.Valid() -or $taskP.MagicCores -ne 130 -or $taskP.StarsTotal -ne 33 -or $taskP.BestStars[10] -ne 3 -or -not $taskP.FirstCleared[10] -or $taskP.LastReward -ne 30 -or $taskP.LastRewardRun -eq $taskSeed.LastRewardRun -or $taskP.LastRewardRun.Length -ne 32 -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or -not $taskP.IsStageOpen(11) -or $taskP.FirstCleared[13] -or $taskP.Loadout -ne 263){throw 'Earned reward/unlock/profile differs'}
}
$taskRun=Get-Content TestResults/v0202-last-run.txt -Raw
if($taskRun -notmatch 'Gatefall 0.20.2' -or $taskRun -notmatch 'Won elapsed=186.07 core=300 hunter=100 kills=816 firstFusion=-1 fusions=0' -or $taskRun -notmatch 'allSpawned=True spawned=816 remainingEnemies=0 bossDefeated=False' -or $taskRun -notmatch 'Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0' -or $taskRun -notmatch 'Mark hits=99 effective bonus damage=295.5'){throw 'Native terminal/resource/contribution scope differs'}
$taskLedger=@();$taskSpent=0;$taskBuilt=0;$taskUpgraded=0
foreach($taskLine in $taskRun -split '\r?\n'){
    if($taskLine -match '^([\d.]+) (build|upgrade) (\w+) @(\d+)$'){
        $taskTime=[double]$Matches[1];$taskAction=$Matches[2];$taskKind=[Enum]::Parse([Gatefall.Core.TowerKind],$Matches[3]);$taskSite=[int]$Matches[4];$taskDef=[Gatefall.Core.TowerCatalog]::Get($taskKind)
        if($taskAction -eq 'build'){$taskCost=$taskDef.BuildCost;$taskBuilt++}else{$taskCost=$taskDef.UpgradeCost;$taskUpgraded++}
        $taskSpent+=$taskCost;$taskLedger+=[pscustomobject]@{time=$taskTime;action=$taskAction;kind=$taskKind.ToString();site=$taskSite;cost=$taskCost}
    }elseif($taskLine -match '^\d.* (fusion|sell) '){throw 'Unexpected native fusion/sale'}
}
if($taskBuilt -ne 16 -or $taskUpgraded -ne 12 -or $taskSpent -ne 1065 -or $taskLedger.Count -ne 28){throw 'Native purchase ledger differs'}
$taskLedger|ConvertTo-Json -Depth 3|Set-Content TestResults/v0202-native-purchase-ledger.json
$taskUi=Get-Content TestResults/android-v0202-pulse-observations.json -Raw|ConvertFrom-Json
if(-not $taskUi.fullBattle -or -not $taskUi.candidate0MarkerOverlapReproduced -or -not $taskUi.finalMarkerLv1Lv2LinesComplete -or $taskUi.builtTowers -ne 16 -or $taskUi.nativeAll16FiringVerified -or $taskUi.cleanupHudBefore180.remaining -ne 30 -or $taskUi.cleanupHudAfter180.remaining -ne 10 -or -not $taskUi.cleanupHudAfter180.stillRunning){throw 'Native observation scope differs'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing visual evidence: $taskShot"}}
$taskRaw=@(Get-Content TestResults/v0202-logcat-pre-relaunch.txt)
$taskPid=6963
$taskApp=@($taskRaw|Where-Object {$_ -match "\s$taskPid\s+\d+\s"})
$taskFirstPerf=-1
for($taskI=0;$taskI -lt $taskApp.Count;$taskI++){if($taskApp[$taskI] -match 'GATEFALL PERF'){$taskFirstPerf=$taskI;break}}
if($taskFirstPerf -lt 0){throw 'Missing process observation boundary'}
$taskStartup=@($taskApp[0..$taskFirstPerf]|Where-Object {$_ -match '\s[EF]\s'})
$taskLater=@($taskApp[($taskFirstPerf+1)..($taskApp.Count-1)]|Where-Object {$_ -match '\s[EF]\s'})
$taskAnr=@($taskRaw|Where-Object {$_ -match 'ANR.*com\.gatefall\.prototype'})
$taskGraphics=@($taskApp|Where-Object {$_ -match 'Version:.*OpenGL ES 3\.1'}).Count -gt 0
if($taskStartup.Count -ne 5 -or $taskAnr.Count -ne 0 -or -not $taskGraphics -or @($taskLater|Where-Object {$_ -notmatch ' E Surface\s*:'}).Count){throw 'Additional diagnostic requires raw-log review'}
if(-not @($taskApp|Where-Object {$_ -match 'state=Won.*shots=1199,0,4,0,0,0,0,0,60,0 hits=1198,0,8,0,0,0,0,0,57,0'}).Count){throw 'Native effect counter differs'}
[pscustomobject]@{appPid=$taskPid;firstPerfLine=$taskApp[$taskFirstPerf];rawStartupErrorLines=$taskStartup;startupErrorCount=$taskStartup.Count;graphics31Created=$taskGraphics;postFirstPerfErrors=$taskLater.Count;postFirstPerfErrorLines=$taskLater;packageAnrLines=$taskAnr.Count;rootCauseResolved=$false;physicalImpactKnown=$false;scope='Final APK before relaunch, including HOME/focus QA and representative screen recording. Background intervals make frame statistics unsuitable for a performance benchmark.'}|ConvertTo-Json -Depth 4|Set-Content TestResults/android-v0202-errors.json
$taskVideo=Get-Content TestResults/v0202-video.json -Raw|ConvertFrom-Json
$taskStream=@($taskVideo.streams|Where-Object codec_type -eq video)[0]
if([double]$taskVideo.format.duration -lt 12 -or [double]$taskVideo.format.duration -gt 13 -or $taskStream.width -ne 1920 -or $taskStream.height -ne 864 -or [long]$taskVideo.format.size -ne (Get-Item Docs/QA/v050/v0202-marker-pulse.mp4).Length -or -not(Test-Path Docs/QA/v050/v0202-video-contact.png)){throw 'Representative video metadata differs'}
$taskModels=Get-Content TestResults/marker-pulse-v0201-comparison.json -Raw|ConvertFrom-Json
if($taskModels.selectedCandidateWindow -ne 16 -or $taskModels.newStandardCandidateModels -ne 18 -or $taskModels.newSupportModels -ne 24 -or -not $taskModels.baselineStandard9Reused -or $taskModels.newAndroidVerified){throw 'Model-time scope differs'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0202.json -Raw|ConvertFrom-Json
if($taskCleanup.processesAfterShutdown -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or -not $taskCleanup.protectedStage4Exists -or -not $taskCleanup.protectedBossExists){throw 'Owned shutdown/protected paths differ'}
if($taskCleanup.removed){if(Test-Path -LiteralPath $taskCleanup.absoluteTarget){throw 'Removed path still exists'}}else{
    $taskRemainder=@(Get-ChildItem -LiteralPath $taskCleanup.absoluteTarget -Force)
    if($taskCleanup.phase -ne 'delayed-log-removal' -or $taskRemainder.Count -ne 2 -or @($taskRemainder|Where-Object {$_.PSIsContainer -or $_.Name -notin @('emulator-err.log','emulator-out.log') -or $_.Length -gt 1024 -or ($_.Attributes -band [IO.FileAttributes]::ReparsePoint)}).Count){throw 'Unexpected cleanup remainder'}
}
[pscustomobject]@{created=(Get-Date).ToString('o');valid=$taskP.Valid();syntheticSeedMagic=100;magic=130;syntheticSeedStars=30;stars=33;stage11BestStars=3;lastReward=30;rewardId=$taskP.LastRewardRun;pending=$taskP.PendingRun;stage12Open=$taskP.IsStageOpen(11);observerStillLocked=$true;loadout=263;bytes=324;sha256=$taskResultHash.ToLowerInvariant();updateHashEqual=$true;mapRelaunchHashEqual=$true}|ConvertTo-Json|Set-Content TestResults/v0202-save-check.json
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;sha256=$taskApk.sha256;bytes=$taskApk.bytes;productChangedFiles=$taskChanged;unchangedOtherCoreFiles=5;reviewedPulseMatchesProduct=$true;normalCandidateModels=18;currentStandard9Reused=$true;supportModels=24;staticPairs=28;changedNormalStage=11;nativeFullBattleStage11=$true;nativeTowers=16;nativeSpent=$taskSpent;nativeUpgrades=12;nativeFusions=0;nativeElapsed=186.07;kills=816;core=300;hunter=100;stars=3;reward=30;markerLinkedHits=99;effectiveMarkBonus=295.5;markerOwnDamage=309.2;bonusAlreadyInAllyDamage=$true;investmentSuperiorityResolved=$false;sourceInstalledHashEqual=$true;updateSavePreserved=$true;mapRelaunchSaveIdentical=$true;saveSha256=$taskResultHash.ToLowerInvariant();stage12Open=$true;stage12Entered=$false;nativeAll16FiringVerified=$false;startupDiagnosticLines=$taskStartup.Count;postFirstPerfSurfaceLines=$taskLater.Count;packageAnr=0;framePerformanceBenchmark=$false;representativeVideoSeconds=[double]$taskVideo.format.duration;wholeBattleVideo=$false;ownedAvdStopped=$true;ownedAvdCleaned=[bool]$taskCleanup.removed;cleanupLogRemovalDeferred=(-not $taskCleanup.removed);humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false;audioReviewed=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/delivery-consistency-v0202.json
'PASS0.20.2 final APK/source/install;16 towers1065spend/816annihilation186.07/core300;Marker99/295.5;30reward/unlock/save; diagnostics and cleanup scope recorded.'
