$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskMeta=Get-Content TestResults/android-apk-v0208-candidate.json -Raw|ConvertFrom-Json
if($taskMeta.version -ne '0.20.8' -or $taskMeta.versionCode -ne 65 -or $taskMeta.signature -ne 'verified' -or $taskMeta.alignment16KB -ne 'verified'){throw 'Android metadata differs'}
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.20.8.apk','TestResults/v0208-installed.apk')){
    if((Get-FileHash $taskPath).Hash -ne $taskMeta.sha256 -or (Get-Item $taskPath).Length -ne $taskMeta.bytes){throw 'Delivery/install differs'}
}
$taskOld=Get-Content TestResults/version-label-v0208-baseline/source-hashes-v0207.json -Raw|ConvertFrom-Json
$taskNew=Get-Content TestResults/source-hashes-v0208-candidate.json -Raw|ConvertFrom-Json
$taskChanged=@(foreach($taskR in $taskNew){
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Current candidate source changed'}
    if(@($taskOld|Where-Object path -eq $taskR.path)[0].sha256 -ne $taskR.sha256){$taskR.path}
})
if(@(Compare-Object $taskChanged @('Assets\Scripts\Presentation\CampaignHud.cs','ProjectSettings\ProjectSettings.asset')).Count){throw 'Unexpected product scope'}
$taskHud=[IO.File]::ReadAllText('Assets/Scripts/Presentation/CampaignHud.cs').Replace("`r`n","`n")
$taskPrior=[IO.File]::ReadAllText('TestResults/version-label-v0208-baseline/CampaignHud.cs').Replace("`r`n","`n")
if($taskHud.Replace('"Gatefall v" + Application.version + " · Android"','"Gatefall v0.5.0 · Android"') -cne $taskPrior){throw 'HUD changes beyond version text'}
foreach($taskCore in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){
    if((Get-FileHash $taskCore.FullName).Hash -ne (Get-FileHash (Join-Path TestResults/version-label-v0208-baseline $taskCore.Name)).Hash){throw 'Combat core differs'}
}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskHash='c85b059454993f432d48e8179bc4334ae990a78b7f63c448577a97cd81e418a7'
$taskSaves=@(foreach($taskPhase in @('before','after-update','relaunch','city-confirmed')){
    $taskPath="TestResults/v0208-$taskPhase.sav"
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if((Get-FileHash $taskPath).Hash -ne $taskHash -or (Get-Item $taskPath).Length -ne 324 -or -not $taskP.Valid() -or
        $taskP.MagicCores -ne 196 -or $taskP.StarsTotal -ne 39 -or $taskP.Loadout -ne 263 -or $taskP.PendingRun -ne '' -or
        $taskP.LastReward -ne 34 -or -not $taskP.FirstCleared[12] -or $taskP.FirstCleared[13] -or -not $taskP.IsStageOpen(13)){throw "Preserved profile differs: $taskPhase"}
    [pscustomobject]@{phase=$taskPhase;bytes=324;sha256=$taskHash;magic=196;stars=39;loadout=263;previousReward=34;rewardRun=$taskP.LastRewardRun;pending='';stage14Open=$true;observerUnlocked=$false;valid=$true}
})
$taskSaves|ConvertTo-Json|Set-Content TestResults/v0208-save-check.json
foreach($taskName in @('before-settings','settings-2400','settings-1920','settings-1280','city-confirmed','relaunch-city')){
    if(-not(Test-Path "Docs/QA/v050/v0208-$taskName.png")){throw "Missing actual screen: $taskName"}
}
$taskD=Get-Content TestResults/v0208-diagnostics.json -Raw|ConvertFrom-Json
if($taskD.appPid -ne 6263 -or $taskD.frameworkPid -ne '546' -or $taskD.frameworkAfter -ne '546' -or $taskD.other -ne 0 -or $taskD.fatal -ne 0 -or $taskD.packageAnr -ne 0 -or $taskD.watchdog -ne 0){throw 'Diagnostic change requires review'}
$taskBoot=Get-Content TestResults/qa-boot-readiness-v0208.json -Raw|ConvertFrom-Json
if(-not $taskBoot.passed -or $taskBoot.mockCases -ne 7){throw 'Boot helper mock checks incomplete'}
[ordered]@{
    created=(Get-Date -Format o);task=222;version=$taskMeta.version;code=$taskMeta.versionCode;bytes=$taskMeta.bytes;sha256=$taskMeta.sha256
    changedProductFiles=$taskChanged;allSixCoreFilesUnchanged=$true;versionSource='Application.version';oldActualLabel='Gatefall v0.5.0 · Android';newActualLabel='Gatefall v0.20.8 · Android'
    screenCases=@(@{width=2400;height=1080;safeLeft=0;safeHeight=1080;originY=0},@{width=1920;height=1080;safeLeft=0;safeHeight=1019;originY=61},@{width=1280;height=720;safeLeft=0;safeHeight=679;originY=41})
    existingPanelFontAndInputUnchanged=$true;newBattleRun=$false;newReward=0;saveSha256=$taskHash;fourActualSavesEqual=$true
    priorBattleEvidence='runtime-v0207-mass.md: previous0.20.7/24 towers/183.87s/928 clear; not a new0.20.8 full battle'
    earlyBootInstallFailedBeforeReady=$true;bootCompletedCheckedBeforeSuccessfulInstall=$true;partialApkPullExcluded=$true;retryFullInstalledHashVerified=$true
    firstCityReturnTapMissedDueSafeArea=$true;misnamedCityReturnScreenExcluded=$true;cityConfirmedAfterActualLayoutInput=$true
    capturedAppPid=6263;frameworkPid=546;postFirstPerfSurface=$taskD.surface;postFirstPerfOtherEF=$taskD.other;capturedAppFatal=0;capturedPackageAnr=0;capturedWatchdogKill=0
    diagnosticScope='6263 captured settings buffers through before app restart; Surface/startup/GNSS cause and physical impact unresolved; HOME/resize PERF not a benchmark'
    humanFunTested=$false;physicalDeviceTested=$false;iosTested=$false;audioTested=$false
    ownedCleanup=$(if(Test-Path TestResults/qa-cleanup-v0208.json){Get-Content TestResults/qa-cleanup-v0208.json -Raw|ConvertFrom-Json}else{$null})
    passed=$true
}|ConvertTo-Json -Depth 7|Set-Content TestResults/delivery-consistency-v0208.json
'PASS: v0.20.8 dynamic settings version/three screens/installed hash/four unchanged actual saves; six-core unchanged'
