$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
# Recorded-evidence consistency only. No simulator, ADB, build, or profile mutation.
$meta=Get-Content TestResults/android-apk-v0251-candidate.json -Raw|ConvertFrom-Json
$sha='c6a80eacd2f87391ccf970455280f33467999862be644a1106017722bc97a963'
if($meta.version -ne '0.25.1' -or $meta.versionCode -ne 76 -or $meta.sha256 -ne $sha -or $meta.signature -ne 'verified' -or $meta.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($path in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.25.1.apk','TestResults/v0251-installed.apk')){
    if((Get-FileHash $path).Hash -ne $sha -or (Get-Item $path).Length -ne 97419469){throw "APK differs: $path"}
}
$sources=@(Get-Content TestResults/source-hashes-v0251-candidate.json -Raw|ConvertFrom-Json)
if($sources.Count -ne 25){throw 'Product manifest scope differs'}
foreach($row in $sources){if((Get-FileHash $row.path).Hash -ne $row.sha256){throw "Product source changed: $($row.path)"}}
$previous=@(Get-Content TestResults/source-hashes-v0250-candidate.json -Raw|ConvertFrom-Json)
foreach($row in $previous|Where-Object path -like '*Scripts*Core*'){if((Get-FileHash $row.path).Hash -ne $row.sha256){throw 'Model/build core has changed'}}
$model=Get-Content TestResults/v0250-late-comparison.json -Raw|ConvertFrom-Json
if(-not $model.passed -or $model.staticContracts -ne 28 -or $model.spawnSamplesEach -ne 840 -or @($model.changedSpawnCells).Count -ne 12 -or $model.baselineModels -ne 36 -or $model.selectedModels -ne 36 -or $model.matchingCardPolicies -ne 36 -or -not $model.reinforcedAll18WonWithoutDamage -or -not $model.all308Sites -or -not $model.allSchedulesCountsEconomyUnchanged -or -not $model.earlyThreeOperationsUnchanged -or -not $model.crisisStatsUnchanged){throw 'Model/static scope differs'}
foreach($tag in @('baseline','selected')){
    $crisis=Get-Content "TestResults/v0250-$tag-crisis.json" -Raw|ConvertFrom-Json
    if($crisis.newModels -ne 12 -or @($crisis.rows).Count -ne 12 -or @($crisis.rows|Where-Object {$_.State -ne 'Won' -or $_.Core -ne 300}).Count){throw 'Shared geometry crisis regression differs'}
}
$elf=Get-Content TestResults/android-elf-v0251.json -Raw|ConvertFrom-Json
if($elf.sha256 -ne $sha -or -not $elf.elfLoad16KB -or @($elf.nativeLibraries).Count -ne 3){throw 'ELF inspection differs'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$initial=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0250-synthetic-stage11.sav')))
if(-not $initial.Valid() -or $initial.MagicCores -ne 100 -or $initial.StarsTotal -ne 30 -or @($initial.FirstCleared|Where-Object{$_}).Count -ne 10){throw 'Initial synthetic campaign differs'}
$initialSha=(Get-FileHash TestResults/v0250-synthetic-stage11.sav).Hash
foreach($file in @('v0250-before.sav','v0250-after-update.sav','v0251-before.sav','v0251-after-update.sav')){if((Get-FileHash ('TestResults/'+$file)).Hash -ne $initialSha){throw 'Update changed synthetic profile'}}
$runs=@();$saveRows=@();$priorId=$initial.LastRewardRun
foreach($stage in @(11,12)){
    $tag="v0251-stage$stage";$total=if($stage -eq 11){816}else{880};$elapsed=if($stage -eq 11){174.78}else{180.28}
    $run=Get-Content "TestResults/$tag-last-run.txt" -Raw
    foreach($fragment in @('Gatefall 0.25.1',"Won elapsed=$elapsed core=300 hunter=100 kills=$total",'fusions=2',"Wave clear=True allSpawned=True spawned=$total remainingEnemies=0 bossDefeated=False",'Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0')){if(-not $run.Contains($fragment)){throw "Run evidence differs: $tag/$fragment"}}
    $ledger=Get-Content "TestResults/$tag-native-purchase-ledger.json" -Raw|ConvertFrom-Json
    if($ledger.builds -ne 18 -or $ledger.upgrades -ne 5 -or $ledger.fusions -ne 2 -or $ledger.sales -ne 0 -or $ledger.pricedSpend -ne 1050 -or $ledger.finalTowersDerivedFromBuildsMinusFusions -ne 16){throw 'Native purchases differ'}
    $stageSha='';$stageId=''
    foreach($part in @('result','map','relaunch')){
        $path="TestResults/$tag-$part.sav";$p=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $path)))
        $hash=(Get-FileHash $path).Hash.ToLowerInvariant();if(-not $stageSha){$stageSha=$hash;$stageId=$p.LastRewardRun}
        if(-not $p.Valid() -or (Get-Item $path).Length -ne 324 -or $hash -ne $stageSha -or $p.MagicCores -ne (100+30+32*($stage-11)) -or $p.StarsTotal -ne (33+3*($stage-11)) -or $p.LastReward -ne (30+2*($stage-11)) -or $p.LastRewardRun -ne $stageId -or $p.LastRewardRun -eq $priorId -or $p.PendingRun -ne '' -or $p.PendingStage -ne -1 -or $p.Loadout -ne 263 -or $p.SelectedHunter -ne 0 -or @($p.Research|Where-Object{$_ -ne 0}).Count -or @($p.Growth|Where-Object{$_ -ne 0}).Count -or @($p.FirstCleared|Where-Object{$_}).Count -ne $stage -or $p.BestStars[$stage-1] -ne 3 -or -not $p.IsStageOpen($stage) -or $p.BestStars[13] -ne 0){throw "Campaign/save differs: $path"}
        $saveRows+=[ordered]@{file=$path;sha256=$hash;bytes=324;magicCores=$p.MagicCores;stars=$p.StarsTotal;reward=$p.LastReward;rewardRun=$p.LastRewardRun;pending=$p.PendingRun;firstCleared=$stage;nextStageOpen=$p.IsStageOpen($stage)}
    }
    $priorId=$stageId
    $diag=Get-Content "TestResults/$tag-diagnostics.json" -Raw|ConvertFrom-Json
    if($diag.postFirstPerfOther -ne 0 -or $diag.postFirstPerfSurface -ne 0 -or $diag.javaOrNativeFatalSignatures -ne 0 -or $diag.packageAnrOrCrash -ne 0 -or $diag.watchdogKills -ne 0){throw 'New captured runtime diagnostic requires review'}
    $video=Get-Content "TestResults/$tag-video-metadata.json" -Raw|ConvertFrom-Json
    if([double]$video.format.duration -lt 10 -or [double]$video.format.duration -gt 13 -or $video.streams[0].width -ne 1920 -or $video.streams[0].height -ne 864 -or -not (Test-Path "Docs/QA/v050/$tag-late.mp4")){throw 'Recorded segment differs'}
    $runs+=[ordered]@{stage=$stage;elapsed=$elapsed;kills=$total;allSpawned=$true;remaining=0;core=300;hunter=100;stars=3;ledger=$ledger;diagnostics=$diag;video=@{file="Docs/QA/v050/$tag-late.mp4";duration=$video.format.duration;sampledFramesInspected=@(2,4);wholeBattle=$false;audioReviewed=$false}}
}
if((Get-FileHash TestResults/v0251-restored-campaign.sav).Hash -ne (Get-FileHash TestResults/v0251-stage12-relaunch.sav).Hash -or (Get-FileHash TestResults/v0251-recipes-after.sav).Hash -ne (Get-FileHash TestResults/v0243-double-before.sav).Hash){throw 'Separate maximum-recipe UI fixture or restoration differs'}
$screens=@('v0251-stage11-live-menu','v0251-stage11-live-direct-pause','v0251-stage11-arc-partner','v0251-stage12-arc-shot-count-live','v0251-stage12-card30','v0251-stage12-card90','v0251-stage12-card150','v0251-recipes-disabled-1280')
foreach($size in @(2400,1920,1280)){$screens+="v0251-base-menu-$size";$screens+="v0251-recipes-four-$size"}
foreach($screen in $screens){if(-not (Test-Path "Docs/QA/v050/$screen.png")){throw "Reviewed image missing: $screen"}}
$cleanup=Get-Content TestResults/qa-cleanup-v0251.json -Raw|ConvertFrom-Json
if($cleanup.serial -ne 'emulator-5652' -or -not $cleanup.normalExit -or -not $cleanup.normalPrioritiesRestored -or $cleanup.remainingProcesses -ne 0 -or -not $cleanup.diskFilesRemoved -or $cleanup.reparsePoints -ne 0 -or $cleanup.protectedDevicesTouched -or @($cleanup.remaining|Where-Object {$_.Name -notin @('stdout.log','stderr.log') -or $_.Length -gt 65536}).Count){throw 'Owned cleanup differs'}
[ordered]@{created=(Get-Date -Format o);tasks=@(264,265,266,267,268);version='0.25.1';code=76;sha256=$sha;bytes=97419469;sourceFiles=25;model=@{normalBefore=36;normalAfter=36;matchingCards=36;reinforced18AllCore300=$true;crisisBefore=12;crisisAfter=12;changedSpawnStatCells=12;staticContracts=28;spawnSamplesEach=840};nativeRuns=$runs;saves=$saveRows;ui=@{baseActions=3;recipeColumns=2;maximumRecipesObserved=4;sizes=@('2400x1080','1920x1080','1280x720');pauseDirectFromLiveMenu=$true;shotCounterObserved=22;disabledRecipePanelBlocksField=$true;reviewedScreens=$screens};cards=@{stage11=@{first='Range';second='not recorded; QA card/partner tap overlap';third='Ballistic';fixedCardComparison=$false};stage12=@{first='Range';second='Ballistic';third='Blast';allThreeScreensRecorded=$true}};diagnosticLimit='Stage11 retained startup AssetPackManager1/EGL4. Stage12 buffers were cleared after cold launch, so its startup0 is not a resolution. Captured post-PERF E/F and package ANR/crash0 in these runs; root causes/physical impact unresolved.';ownedCleanup=$cleanup;humanFunTested=$false;physicalDeviceTested=$false;iosTested=$false;audioReviewed=$false;storeReady=$false;passed=$true}|ConvertTo-Json -Depth 16|Set-Content TestResults/delivery-consistency-v0251.json
$saveRows|ConvertTo-Json -Depth 5|Set-Content TestResults/v0251-save-check.json
'PASS v0.25.1: Android/installed SHA/source25, normal36 pairs+crisis12 pairs, ELF/ZIP16KB, 1-11 174.78s816 and1-12 180.28s880/16T1050/2fusions/300, 6 persistent result-map-relaunch saves, max4 recipe UI/3sizes, owned5652 cleanup. Human/device/iOS/audio excluded.'
