$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskMeta=Get-Content TestResults/android-apk-v0207-candidate.json -Raw|ConvertFrom-Json
if($taskMeta.version -ne '0.20.7' -or $taskMeta.versionCode -ne 64 -or $taskMeta.signature -ne 'verified' -or $taskMeta.alignment16KB -ne 'verified'){throw 'Release metadata differs'}
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.20.7.apk','TestResults/v0207-installed.apk')){
    if((Get-FileHash $taskPath).Hash -ne $taskMeta.sha256 -or (Get-Item $taskPath).Length -ne $taskMeta.bytes){throw 'Delivery/install hash differs'}
}
$taskOld=Get-Content TestResults/narrow-menu-v0207-baseline/source-hashes-v0206.json -Raw|ConvertFrom-Json
$taskNew=Get-Content TestResults/source-hashes-v0207-candidate.json -Raw|ConvertFrom-Json
$taskChanged=@(foreach($taskR in $taskNew){
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Candidate product changed'}
    if(@($taskOld|Where-Object path -eq $taskR.path)[0].sha256 -ne $taskR.sha256){$taskR.path}
})
if(@(Compare-Object $taskChanged @('Assets\Scripts\Presentation\MobileHud.cs','ProjectSettings\ProjectSettings.asset')).Count){throw 'Unexpected product changes'}
$taskHud=[IO.File]::ReadAllText('Assets/Scripts/Presentation/MobileHud.cs')
if($taskHud.Replace('Mathf.Max(352, 28 + tiles * 140 + (tiles - 1) * 8)','Mathf.Max(330, 28 + tiles * 140 + (tiles - 1) * 8)') -cne [IO.File]::ReadAllText('TestResults/narrow-menu-v0207-baseline/MobileHud.cs')){throw 'HUD changed beyond minimum width'}
$taskFontCases=@(foreach($taskName in @('tower-menu-reachable-v0206.json','tower-menu-reachable-max-v0206.json')){
    $taskAudit=Get-Content (Join-Path TestResults $taskName) -Raw|ConvertFrom-Json
    $taskAudit.rows|Where-Object context -like '*minimum352'
})
if($taskFontCases.Count -ne 8 -or @($taskFontCases|Where-Object {$_.width -gt 262 -or $_.height -gt 22 -or $_.overflowsMinimum}).Count){throw 'Measured candidate labels do not fit'}
foreach($taskLevel in @(1,2)){foreach($taskWidth in @(2400,1920,1280)){if(-not(Test-Path "Docs/QA/v050/v0207-binding-lv$taskLevel-$taskWidth.png")){throw 'Six level/resolution captures incomplete'}}}
foreach($taskShot in @('baseline-binding-lv1','baseline-binding-live','panel-blank-block','external-pad-switch','binding-live-a','binding-live-b','abandon-city','relaunch-city')){if(-not(Test-Path "Docs/QA/v050/v0207-$taskShot.png")){throw 'Functional evidence missing'}}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskHash='944dc9755d53a5b3bdf32b8a6430d977e73832fefbdd4697fb88b98f9e78547f'
$taskRef=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0204-stage13-relaunch.sav')))
$taskSaves=@(foreach($taskPhase in @('before-update','after-update','abandon','relaunch')){
    $taskPath="TestResults/v0207-$taskPhase.sav";$taskProfile=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if(-not $taskProfile.Valid() -or (Get-FileHash $taskPath).Hash -ne $taskHash -or (Get-Item $taskPath).Length -ne 324){throw 'Save bytes differ'}
    foreach($taskF in $taskProfile.GetType().GetFields()){if(($taskF.GetValue($taskProfile)|ConvertTo-Json -Compress) -cne ($taskF.GetValue($taskRef)|ConvertTo-Json -Compress)){throw 'Saved fields differ'}}
    [ordered]@{phase=$taskPhase;valid=$true;bytes=324;sha256=$taskHash;magic=$taskProfile.MagicCores;stars=$taskProfile.StarsTotal;reward=$taskProfile.LastReward;rewardRun=$taskProfile.LastRewardRun;pending=$taskProfile.PendingRun}
})
$taskSaves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0207-save-check.json
$taskLog=Get-Content TestResults/v0207-logcat-final.txt
$taskApp=@($taskLog|Where-Object {$_ -match '^\d\d-\d\d \S+\s+5260\s+'})
$taskFirst=($taskApp|Where-Object {$_ -match 'GATEFALL PERF'}|Select-Object -First 1).Substring(0,18)
$taskEF=@($taskApp|Where-Object {$_.Substring(0,18) -ge $taskFirst -and $_ -match '\s[EF]\s'})
$taskSurface=@($taskEF|Where-Object {$_ -match 'E Surface\s*:'});$taskOther=@($taskEF|Where-Object {$_ -notmatch 'E Surface\s*:'})
$taskFatal=@($taskApp|Where-Object {$_ -match '\sF\s'}).Count
$taskAnr=@(Get-Content TestResults/v0207-events-final.txt|Where-Object {$_ -match 'am_anr:.*com.gatefall.prototype'}).Count
$taskWatchdog=@($taskLog|Where-Object {$_ -match 'W Watchdog: \*\*\* WATCHDOG KILLING SYSTEM PROCESS'}).Count
if($taskOther.Count -ne 0 -or $taskFatal -ne 0 -or $taskAnr -ne 0 -or $taskWatchdog -ne 0){throw 'Captured diagnostics require review'}
foreach($taskPhase in @('before','final')){if((Get-Content "TestResults/v0207-system-$taskPhase.txt").Trim() -ne '582'){throw 'Framework PID changed'}}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0207.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.normalEmulatorShutdown -or $taskCleanup.ownedProcessesRemaining -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or -not $taskCleanup.diskFilesRemoved){throw 'Owned cleanup needs review'}
[ordered]@{created=(Get-Date -Format o);task=214;version='0.20.7';code=64;bytes=$taskMeta.bytes;sha256=$taskMeta.sha256;productChanges=$taskChanged;
    minimumPanelWidthBefore=330;minimumPanelWidthAfter=352;contentWidthBefore=240;contentWidthAfter=262;allSixCoreFilesUnchanged=$true;allLabelsAndFontsAndInputLogicUnchanged=$true;
    editorReachableTowerKinds=@('Binding','Arc','Shatter','Gravity');editorCandidateCases=8;maxMeasuredWidth=($taskFontCases|Measure-Object width -Maximum).Maximum;
    nativeReproductionVersion='0.20.6';nativeStage=2;nativeSites=18;nativeTower='Binding';nativeResolutions=@('2400x1080/safe136','1920x1080/safe109','1280x720/safe73');nativeLevels=@(1,2);
    finalTowers=1;nativeBuilds=1;nativeUpgrades=1;nativeSpend=85;nativeCredits=155;nativeSales=0;nativeFusions=0;nativeCards=0;nativeSupplies=0;nativeCatalysts=0;
    liveClock='0:04->0:09';liveCredits='155->155';liveLevel=2;liveTargetCount='0->0';panelBlankPreservedSelection=$true;outsidePadSwitchWorks=$true;
    otherThreeKindsNativelyExercised=$false;newVersionWholeBattleCompleted=$false;newReward=0;saveSha256=$taskHash;realBeforeUpdateExtractionSucceeded=$true;
    appPid=5260;frameworkPid=582;postFirstPerfSurface=$taskSurface.Count;postFirstPerfOtherEF=$taskOther.Count;appFatal=$taskFatal;packageAnr=$taskAnr;watchdogKill=$taskWatchdog;
    startupDiagnosticsResolved=$false;homeMixedPerfIsBenchmark=$false;physicalDeviceTested=$false;humanFunEvaluated=$false;iosTested=$false;audioTested=$false;cleanup=$taskCleanup;passed=$true
}|ConvertTo-Json -Depth 7|Set-Content TestResults/delivery-consistency-v0207.json
Write-Output "PASS narrow menu: one HUD constant /6core unchanged /6 native menus /LIVE and input /4 saves identical /install hash; Surface=$($taskSurface.Count),otherEF=$($taskOther.Count)."
