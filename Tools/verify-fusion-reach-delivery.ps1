$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskMeta=Get-Content TestResults/android-apk-v0209-candidate.json -Raw|ConvertFrom-Json
if($taskMeta.version -ne '0.20.9' -or $taskMeta.versionCode -ne 66 -or $taskMeta.signature -ne 'verified' -or $taskMeta.alignment16KB -ne 'verified'){throw 'Android metadata differs'}
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.20.9.apk','TestResults/v0209-installed.apk')){
    if((Get-FileHash $taskPath).Hash -ne $taskMeta.sha256 -or (Get-Item $taskPath).Length -ne $taskMeta.bytes){throw 'Delivered/installed APK differs'}
}
$taskOld=Get-Content TestResults/fusion-reach-v0209-baseline/source-hashes-v0208.json -Raw|ConvertFrom-Json
$taskNew=Get-Content TestResults/source-hashes-v0209-candidate.json -Raw|ConvertFrom-Json
$taskChanges=@(foreach($taskR in $taskNew){
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Candidate sources changed'}
    if(@($taskOld|Where-Object path -eq $taskR.path)[0].sha256 -ne $taskR.sha256){$taskR.path}
})
if(@(Compare-Object $taskChanges @('Assets\Scripts\Core\RaonCombat.cs','Assets\Scripts\Presentation\GatefallGame.cs','ProjectSettings\ProjectSettings.asset')).Count){throw 'Unexpected product scope'}
$taskCore=[IO.File]::ReadAllText('Assets/Scripts/Core/RaonCombat.cs')
$taskStart=$taskCore.IndexOf('        // Family availability only:');$taskEnd=$taskCore.IndexOf('        GameBalance ConfigureBalance(GameBalance b)')
if($taskStart -lt 0 -or $taskEnd -le $taskStart -or $taskCore.Remove($taskStart,$taskEnd-$taskStart) -cne [IO.File]::ReadAllText('TestResults/fusion-reach-v0209-baseline/RaonCombat.cs')){throw 'Combat change outside read-only query'}
$taskController=[IO.File]::ReadAllText('Assets/Scripts/Presentation/GatefallGame.cs').Replace("`r`n","`n")
$taskController=$taskController.Replace("            int reachable = Simulation.ReachableTowerFamilies();`n",'').Replace("                if ((reachable & (1 << (int)other)) == 0) continue;`n",'')
if($taskController -cne [IO.File]::ReadAllText('TestResults/fusion-reach-v0209-baseline/GatefallGame.cs').Replace("`r`n","`n")){throw 'Controller change outside availability filter'}
if((Get-Content TestResults/fusion-reachability-v0209-check.txt -Raw) -notlike 'PASS:12 family/gate/owned-resource cases*'){throw 'New query checks missing'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskSaves=@(foreach($taskPhase in @('before','after-update','after-single-abandon','relaunch-confirmed')){
    $taskPath="TestResults/v0209-$taskPhase.sav";$taskHash=(Get-FileHash $taskPath).Hash.ToLowerInvariant()
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if($taskHash -ne 'd701872138d84ce1dd26def37b6fca4eebd4fedfff23db96d6abbd0c182646aa' -or (Get-Item $taskPath).Length -ne 324 -or
        -not $taskP.Valid() -or $taskP.Loadout -ne 1 -or $taskP.MagicCores -ne 196 -or $taskP.StarsTotal -ne 39 -or $taskP.LastReward -ne 34 -or
        $taskP.PendingRun -ne '' -or -not $taskP.IsStageOpen(13) -or $taskP.FirstCleared[13]){throw "Single fixture save differs: $taskPhase"}
    [pscustomobject]@{phase=$taskPhase;bytes=324;sha256=$taskHash;loadout=1;magic=196;stars=39;previousReward=34;rewardRun=$taskP.LastRewardRun;pending='';valid=$true}
})
foreach($taskCase in @(@{mask=3;phase='pair-abandon'},@{mask=7;phase='default-abandon'})){
    $taskSeed="TestResults/v0209-loadout$($taskCase.mask)-seed.sav";$taskPath="TestResults/v0209-$($taskCase.phase).sav"
    if((Get-FileHash $taskSeed).Hash -ne (Get-FileHash $taskPath).Hash){throw 'Other fixture save changed'}
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if(-not $taskP.Valid() -or $taskP.Loadout -ne $taskCase.mask -or $taskP.MagicCores -ne 196 -or $taskP.StarsTotal -ne 39 -or $taskP.LastReward -ne 34 -or $taskP.PendingRun -ne ''){throw 'Other fixture fields differ'}
    $taskSaves+=[pscustomobject]@{phase=$taskCase.phase;bytes=324;sha256=(Get-FileHash $taskPath).Hash.ToLowerInvariant();loadout=$taskCase.mask;magic=196;stars=39;previousReward=34;pending='';valid=$true}
}
$taskSaves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0209-save-check.json
foreach($taskName in @('before-spear-lv1','before-spear-lv2','after-single-lv1','after-single-lv2','after-single-blank-block','after-single-sold-pause','pair-spear-lv1','pair-catalyst-needed','default-spear-menu','default-live-before','default-live-after','relaunch-confirmed-city')){
    if(-not(Test-Path "Docs/QA/v050/v0209-$taskName.png")){throw "Actual screen absent: $taskName"}
}
$taskD=Get-Content TestResults/v0209-diagnostics.json -Raw|ConvertFrom-Json
if($taskD.appPid -ne 8069 -or $taskD.frameworkPid -ne '552' -or $taskD.frameworkAfter -ne '552' -or $taskD.other -ne 0 -or $taskD.fatal -ne 0 -or $taskD.packageAnr -ne 0 -or $taskD.watchdog -ne 0){throw 'Diagnostic change requires review'}
[ordered]@{
    created=(Get-Date -Format o);task=225;version=$taskMeta.version;code=$taskMeta.versionCode;bytes=$taskMeta.bytes;sha256=$taskMeta.sha256;changedProductFiles=$taskChanges
    queryReadOnly=$true;priorCoreReverseIdentical=$true;otherFiveCoreFilesUnchanged=$true;balanceEconomyPathsSaveFormatUnchanged=$true
    nativeStage=13;nativeLoadouts=@(1,3,7);singleFamilyBeforeOptions=@('Seal');singleFamilyAfterOptions=@();pairAfterOptions=@('Arc');defaultAfterOptions=@('Arc','Shatter','Seal')
    levelCatalystCreditShortageStillVisible=$true;nativeCatalystShortageVerified=$true;nativeZeroCreditShortageVerified=$false
    singlePreparation=@{builds=1;upgrades=1;spent=70;sales=1;refund=70;finalCredits=320;finalTowers=0;elapsed=0}
    pairPreparation=@{builds=2;upgrades=2;spent=155;sales=0;credits=165;towers=2;elapsed=0;catalysts=0;fusions=0}
    defaultLive=@{builds=1;upgrades=0;spent=40;credits=280;towers=1;hudBefore='0:04';hudAfter='0:09';pauseHud='0:10';core=300;fusions=0;cards=0;supplies=0;catalysts=0}
    nativeNewFullBattle=$false;newReward=0;fourSingleSavesEqual=$true;otherTwoFixtureAbandonsEqual=$true;noPushRelaunchVerified=$true
    restoredFixtureRelaunchExcludedFromPersistenceProof=$true;partialBeforeScreenExcluded=$true;duplicateTapsParameterFailedBeforeInput=$true
    appPid=8069;frameworkPid=552;postFirstPerfSurface=$taskD.surface;postFirstPerfOtherEF=0;capturedAppFatal=0;capturedPackageAnr=0;capturedWatchdogKill=0
    diagnosticScope=$taskD.scope;startupSurfaceGnssRootCauseResolved=$false;perfBenchmark=$false
    nativeObserverUnlockFixtureTested=$false;humanFunTested=$false;physicalDeviceTested=$false;iosTested=$false;audioTested=$false
    ownedCleanup=$(if(Test-Path TestResults/qa-cleanup-v0209.json){Get-Content TestResults/qa-cleanup-v0209.json -Raw|ConvertFrom-Json}else{$null})
    passed=$true
}|ConvertTo-Json -Depth 7|Set-Content TestResults/delivery-consistency-v0209.json
'PASS: v0.20.9 reachable-only fusion choices/3 native loadouts/LIVE/installed hash/4+2 preserved saves; no new full battle'
