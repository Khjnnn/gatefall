$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskMeta=Get-Content TestResults/android-apk-v0210-candidate.json -Raw|ConvertFrom-Json
if($taskMeta.version -ne '0.21.0' -or $taskMeta.versionCode -ne 67 -or $taskMeta.signature -ne 'verified' -or $taskMeta.alignment16KB -ne 'verified'){throw 'Android metadata differs'}
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.21.0.apk','TestResults/v0210-installed.apk')){
    if((Get-FileHash $taskPath).Hash -ne $taskMeta.sha256 -or (Get-Item $taskPath).Length -ne $taskMeta.bytes){throw 'Delivered/installed APK differs'}
}
$taskOld=Get-Content TestResults/active-timing-v0210-baseline/source-hashes-v0209.json -Raw|ConvertFrom-Json
$taskNew=Get-Content TestResults/source-hashes-v0210-candidate.json -Raw|ConvertFrom-Json
$taskChanges=@(foreach($taskR in $taskNew){
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Candidate sources changed'}
    $taskPrior=@($taskOld|Where-Object path -eq $taskR.path)
    if($taskPrior.Count -eq 0 -or $taskPrior[0].sha256 -ne $taskR.sha256){$taskR.path}
})
$taskExpected=@('Assets\Scripts\Presentation\ActiveCombatTiming.cs','Assets\Scripts\Presentation\ActiveCombatTiming.cs.meta','Assets\Scripts\Presentation\GatefallGame.cs','ProjectSettings\ProjectSettings.asset')
if(@(Compare-Object $taskChanges $taskExpected).Count){throw 'Unexpected product scope'}
$taskController=[IO.File]::ReadAllText('Assets/Scripts/Presentation/GatefallGame.cs').Replace("`r`n","`n")
$taskPriorController=[IO.File]::ReadAllText('TestResults/active-timing-v0210-baseline/GatefallGame.cs').Replace("`r`n","`n")
$taskOldStart=$taskPriorController.IndexOf('        // Development builds log frame timing')
$taskNewStart=$taskController.IndexOf('        // Keep diagnostic cadence,')
$taskOldEnd=$taskPriorController.IndexOf('        private void Suspend()')
$taskNewEnd=$taskController.IndexOf('        private void Suspend()')
if($taskOldStart -lt 0 -or $taskNewStart -lt 0 -or $taskOldEnd -le $taskOldStart -or $taskNewEnd -le $taskNewStart -or
    $taskPriorController.Substring(0,$taskOldStart) -cne $taskController.Substring(0,$taskNewStart) -or
    $taskPriorController.Substring($taskOldEnd) -cne $taskController.Substring($taskNewEnd)){throw 'Controller changed outside diagnostic block'}
if((Get-Content TestResults/active-combat-timing-results.txt -Raw) -notlike 'PASS: inactive/entry/resume*'){throw 'Timing timeline checks absent'}
$taskParser=Get-Content TestResults/perf-summary-228-fixtures/summary.json -Raw|ConvertFrom-Json
$taskLegacy=Get-Content TestResults/perf-summary-228-fixtures/legacy-summary.json -Raw|ConvertFrom-Json
if($taskParser.frameSamples -ne 150 -or $taskParser.worstMs -ne 200 -or $taskParser.weightedAverageMs -ne 17.33 -or $taskLegacy.status -ne 'insufficient-active-samples'){throw 'Parser boundary checks absent'}
$taskPerf=Get-Content TestResults/v0210-active-performance.json -Raw|ConvertFrom-Json
if($taskPerf.appPidFilter -ne 5702 -or $taskPerf.status -ne 'active-combat-samples' -or $taskPerf.battleRunningWindows -ne 2 -or $taskPerf.frameSamples -ne 944 -or $taskPerf.legacyMixedWindowsExcluded -ne 0){throw 'Native active windows differ'}
foreach($taskState in @('Ready','Paused')){
    $taskInactive=@($taskPerf.allParsedWindows|Where-Object state -eq $taskState)
    if(-not $taskInactive.Count -or @($taskInactive|Where-Object {$_.samples -ne 0 -or $null -ne $_.avgMs -or $null -ne $_.worstMs}).Count){throw 'Inactive windows were measured'}
}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskSaves=@(foreach($taskPhase in @('before','after-update','abandon','relaunch')){
    $taskPath="TestResults/v0210-$taskPhase.sav";$taskHash=(Get-FileHash $taskPath).Hash.ToLowerInvariant()
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if($taskHash -ne 'c85b059454993f432d48e8179bc4334ae990a78b7f63c448577a97cd81e418a7' -or (Get-Item $taskPath).Length -ne 324 -or
        -not $taskP.Valid() -or $taskP.Loadout -ne 263 -or $taskP.MagicCores -ne 196 -or $taskP.StarsTotal -ne 39 -or $taskP.LastReward -ne 34 -or
        $taskP.PendingRun -ne '' -or -not $taskP.IsStageOpen(13) -or $taskP.FirstCleared[13]){throw "Save differs: $taskPhase"}
    [pscustomobject]@{phase=$taskPhase;bytes=324;sha256=$taskHash;loadout=263;magic=196;stars=39;previousReward=34;rewardRun=$taskP.LastRewardRun;pending='';valid=$true}
})
$taskSaves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0210-save-check.json
foreach($taskName in @('ready-timing','running-first','focus-pause','running-resumed','final-pause','abandon-map','relaunch-city')){
    if(-not(Test-Path "Docs/QA/v050/v0210-$taskName.png")){throw "Actual screen absent: $taskName"}
}
$taskD=Get-Content TestResults/v0210-diagnostics.json -Raw|ConvertFrom-Json
if($taskD.appPid -ne 5702 -or $taskD.frameworkAfter -ne '554' -or $taskD.surface -ne 16 -or $taskD.other -ne 0 -or $taskD.fatal -ne 0 -or $taskD.packageAnr -ne 0 -or $taskD.watchdog -ne 0){throw 'Diagnostics require review'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0210.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.normalExit -or $taskCleanup.remainingProcesses -ne 0 -or -not $taskCleanup.diskFilesRemoved -or $taskCleanup.protectedDevicesTouched){throw 'Owned cleanup incomplete'}
[ordered]@{
    created=(Get-Date -Format o);task=228;version=$taskMeta.version;code=$taskMeta.versionCode;bytes=$taskMeta.bytes;sha256=$taskMeta.sha256;changedProductFiles=$taskChanges
    sixCoreFilesUnchanged=$true;controllerOutsideDiagnosticsUnchanged=$true;balanceEconomyPathsInputSaveFormatUnchanged=$true
    measurementScope='Consecutive focused Running battle frames only; cards/inactive/transition frames excluded; last600 samples maximum; actual long active frames preserved'
    nativeStage=13;nativeFullBattle=$false;nativeTimingOnly=$true;nativeCardBoundaryTested=$false;cardBoundaryUnitTested=$true
    hudFirst='0:13';hudPaused='0:14';hudResumed='0:24';hudFinalPause='0:25';spearSite=0;builds=1;spent=40;finalCredits=284;core=300
    activeWindows=2;activeSamples=944;activeSampleSeconds=15.734;reportedRoundedMeanMs=16.7;reportedWorstMs=16.7;inactiveNaVerified=$true
    normalAbandon=$true;newReward=0;fourActualSavesEqual=$true;noPushRelaunchVerified=$true;noResultFileOnAbandon=$true
    appPid=5702;frameworkPid=554;postFirstPerfSurface=16;postFirstPerfOtherEF=0;capturedFatal=0;capturedPackageAnr=0;capturedWatchdogKill=0
    diagnosticScope=$taskD.scope;startupSurfaceGnssRootCauseResolved=$false;physicalDeviceOrFullLoadBenchmark=$false
    screenshotPullFailedOnce=$true;failedPartialPreserved=$true;inputNotRepeatedOnScreenshotRetry=$true
    humanFunTested=$false;physicalDeviceTested=$false;iosTested=$false;audioTested=$false;ownedCleanup=$taskCleanup;passed=$true
}|ConvertTo-Json -Depth 7|Set-Content TestResults/delivery-consistency-v0210.json
'PASS: v0.21.0 scoped combat timing/Ready-HOME-Pause-resume/944 active samples/4 actual saves/installed hash/owned cleanup; short low-load diagnostic only'
