$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0200.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.20.0' -or $taskApk.versionCode -ne 57 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'Final APK metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.20.0.apk','Builds/Android/Gatefall.apk','TestResults/v0200-final-installed.apk')){if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw 'Final source/install APK differs'}}
if((Get-FileHash TestResults/v0200-candidate0.apk).Hash -eq (Get-FileHash Builds/Android/Gatefall.apk).Hash){throw 'Rejected candidate0 was delivered'}
$taskBefore=@(Get-Content TestResults/source-hashes-v0192.json -Raw|ConvertFrom-Json)
$taskAfter=@(Get-Content TestResults/source-hashes-v0200.json -Raw|ConvertFrom-Json)
$taskChanged=@(foreach($taskRow in $taskAfter){
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Product source changed: $($taskRow.path)"}
    $taskOld=@($taskBefore|Where-Object path -eq $taskRow.path)
    if($taskOld.Count -ne 1){throw 'Missing unique baseline source'}
    if($taskOld[0].sha256 -ne $taskRow.sha256){$taskRow.path}
})
if($taskChanged.Count -ne 3 -or @($taskChanged|Where-Object {$_ -notin @('Assets\Scripts\Core\BattlefieldLayouts.cs','Assets\Scripts\Core\Campaign.cs','ProjectSettings\ProjectSettings.asset')}).Count){throw 'Unexpected product difference'}
$taskCandidate=Get-Content TestResults/alley-v0192-review/three-clear-courts/BattlefieldLayouts.cs -Raw
if($taskCandidate.Replace("`r`n","`n").TrimEnd() -cne (Get-Content Assets/Scripts/Core/BattlefieldLayouts.cs -Raw).Replace("`r`n","`n").TrimEnd()){throw 'Product map differs from reviewed final candidate'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskSeed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject 'TestResults/v0200-alley-seed.sav')))
$taskSeedHash=(Get-FileHash TestResults/v0200-alley-seed.sav).Hash
foreach($taskFile in @('v0200-before-update.sav','v0200-after-update.sav','v0200-candidate0-after.sav','v0200-final-before-update.sav','v0200-final-after-update.sav')){if((Get-FileHash (Join-Path TestResults $taskFile)).Hash -ne $taskSeedHash){throw 'Update/Ready abandon changed original fixture'}}
$taskResultHash=(Get-FileHash TestResults/v0200-result.sav).Hash
foreach($taskFile in @('v0200-result.sav','v0200-city.sav','v0200-relaunch.sav')){
    $taskPath=Join-Path $taskProject ('TestResults/'+$taskFile)
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes($taskPath))
    if((Get-FileHash $taskPath).Hash -ne $taskResultHash -or -not $taskP.Valid() -or $taskP.MagicCores -ne 86 -or $taskP.StarsTotal -ne 17 -or $taskP.BestStars[7] -ne 3 -or -not $taskP.FirstCleared[7] -or $taskP.LastReward -ne 24 -or $taskP.LastRewardRun -eq $taskSeed.LastRewardRun -or $taskP.LastRewardRun.Length -ne 32 -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or -not $taskP.IsStageOpen(8)){throw 'Earned reward/unlock/profile differs'}
}
$taskRun=Get-Content TestResults/v0200-last-run.txt -Raw
if($taskRun -notmatch 'Won elapsed=182.03 core=300 hunter=100 kills=612' -or $taskRun -notmatch 'allSpawned=True spawned=612 remainingEnemies=0 bossDefeated=False' -or $taskRun -notmatch 'Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0'){throw 'Native annihilation/resource scope differs'}
$taskLedger=@();$taskSpend=0;$taskBuild=0;$taskUpgrade=0;$taskFusion=0
foreach($taskLine in $taskRun -split '\r?\n'){
    if($taskLine -match '^([\d.]+) (build|upgrade) (\w+) @(\d+)$'){
        $taskTime=[double]$Matches[1];$taskAction=$Matches[2];$taskKind=[Enum]::Parse([Gatefall.Core.TowerKind],$Matches[3]);$taskSite=[int]$Matches[4];$taskDef=[Gatefall.Core.TowerCatalog]::Get($taskKind)
        if($taskAction -eq 'build'){$taskCost=$taskDef.BuildCost;$taskBuild++}else{$taskCost=$taskDef.UpgradeCost;$taskUpgrade++}
        $taskSpend+=$taskCost;$taskLedger+=[pscustomobject]@{time=$taskTime;action=$taskAction;kind=$taskKind.ToString();site=$taskSite;cost=$taskCost}
    }elseif($taskLine -match '^([\d.]+) fusion \((\w+)\+(\w+)\) -> (\w+) @(\d+)$'){
        $taskTime=[double]$Matches[1];$taskKind=$Matches[4];$taskSite=[int]$Matches[5];$taskRecipe=@([Gatefall.Core.TowerCatalog]::Recipes|Where-Object {$_.Result.ToString() -eq $taskKind})
        if($taskRecipe.Count -ne 1){throw 'Nonunique native fusion recipe'}
        $taskCost=$taskRecipe[0].Credits;$taskSpend+=$taskCost;$taskFusion++;$taskLedger+=[pscustomobject]@{time=$taskTime;action='fusion';kind=$taskKind;site=$taskSite;cost=$taskCost}
    }
}
if($taskBuild -ne 15 -or $taskUpgrade -ne 11 -or $taskFusion -ne 1 -or $taskSpend -ne 1065 -or $taskLedger.Count -ne 27){throw 'Native purchase ledger differs'}
$taskLedger|ConvertTo-Json -Depth 3|Set-Content TestResults/v0200-native-purchase-ledger.json
$taskUi=Get-Content TestResults/android-v0200-alley-observations.json -Raw|ConvertFrom-Json
if(-not $taskUi.nativeFullBattle -or $taskUi.grossSpend -ne $taskSpend -or $taskUi.towersBuiltAtEnd -ne 14 -or $taskUi.fusionValueResolved -or $taskUi.finalNativeEffectCounters.ArcShots -ne 0){throw 'Native UI/role scope differs'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing native evidence: $taskShot"}}
$taskErrors=Get-Content TestResults/android-v0200-errors.json -Raw|ConvertFrom-Json
if($taskErrors.startupErrorCount -ne 5 -or $taskErrors.postFirstPerfErrors -ne 12 -or $taskErrors.packageAnrLines -ne 0 -or -not $taskErrors.graphics31Created){throw 'Diagnostic scope differs'}
if(@($taskErrors.postFirstPerfErrorLines|Where-Object {$_ -notmatch ' E Surface\s*:'}).Count){throw 'Additional diagnostic requires review'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0200.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.removed -or $taskCleanup.processesAfterShutdown -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or (Test-Path -LiteralPath $taskCleanup.absoluteTarget) -or -not $taskCleanup.protectedStage4Exists -or -not $taskCleanup.protectedBossExists){throw 'Owned cleanup incomplete'}
$taskModels=Get-Content TestResults/alley-v0200-comparison.json -Raw|ConvertFrom-Json
if($taskModels.selected -ne 'three-clear-courts' -or $taskModels.newNormalCandidates -ne 36 -or $taskModels.normalCurrentBaseline -ne 9 -or $taskModels.changedStaticPairs.Count -ne 2){throw 'Model aggregate differs'}
[pscustomobject]@{created=(Get-Date).ToString('o');valid=$taskP.Valid();seedMagic=$taskSeed.MagicCores;magic=$taskP.MagicCores;seedStars=$taskSeed.StarsTotal;stars=$taskP.StarsTotal;stage8BestStars=$taskP.BestStars[7];lastReward=$taskP.LastReward;rewardId=$taskP.LastRewardRun;pending=$taskP.PendingRun;stage9Open=$taskP.IsStageOpen(8);bytes=(Get-Item TestResults/v0200-result.sav).Length;sha256=$taskResultHash.ToLowerInvariant();updateHashEqual=$true;mapRelaunchHashEqual=$true}|ConvertTo-Json|Set-Content TestResults/v0200-save-check.json
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;sha256=$taskApk.sha256;bytes=$taskApk.bytes;productChangedFiles=$taskChanged;unchangedOtherCoreFiles=4;selectedFinalMapMatchesProduct=$true;normalModels=45;selectedCrisisModels=9;finalStaticPairs=28;nativeFullBattleStage8=$true;nativeTowers=14;nativeSpent=$taskSpend;nativeFusion=1;nativeElapsed=182.03;kills=612;core=300;hunter=100;stars=3;reward=24;normalStageBossRequirement=$false;newStage9Open=$true;sourceInstalledHashEqual=$true;updateSavePreserved=$true;mapRelaunchSaveIdentical=$true;saveSha256=$taskResultHash.ToLowerInvariant();fusionValueResolved=$false;nativeArcShots=0;startupDiagnosticLines=5;postFirstPerfSurfaceLines=12;packageAnr=0;framePerformanceBenchmark=$false;ownedAvdCleaned=$true;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json -Depth 4|Set-Content TestResults/delivery-consistency-v0200.json
'PASS0.20.0 final APK/install/map/scope;14towers1065spend/612annihilation182.03/core300;24reward/unlock/save; role0damage and Surface12 limits preserved; owned cleanup'
