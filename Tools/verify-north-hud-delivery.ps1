$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskApk=Get-Content TestResults/android-apk-v0204.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.20.4' -or $taskApk.versionCode -ne 61 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.20.4.apk','TestResults/v0204-north-installed.apk')){if((Get-FileHash $taskPath).Hash -ne $taskApk.sha256 -or (Get-Item $taskPath).Length -ne $taskApk.bytes){throw 'APK/install hash differs'}}
$taskOld=Get-Content TestResults/north-hud-v0204-baseline/source-hashes.json -Raw|ConvertFrom-Json
$taskChanged=@(foreach($taskR in (Get-Content TestResults/source-hashes-v0204.json -Raw|ConvertFrom-Json)){
    if((Get-FileHash -LiteralPath $taskR.path).Hash -ne $taskR.sha256){throw 'Current source hash differs'}
    $taskB=@($taskOld|Where-Object path -eq $taskR.path);if($taskB.Count -ne 1){throw 'Missing unique baseline source'}
    if($taskB[0].sha256 -ne $taskR.sha256){$taskR.path}
})
if($taskChanged.Count -ne 3 -or 'Assets\Scripts\Core\MobileInput.cs' -notin $taskChanged -or 'Assets\Scripts\Presentation\MobileHud.cs' -notin $taskChanged -or 'ProjectSettings\ProjectSettings.asset' -notin $taskChanged){throw 'Unexpected product source changes'}
$taskInput=[IO.File]::ReadAllText((Join-Path (Get-Location) 'Assets/Scripts/Core/MobileInput.cs'))
$taskStart=$taskInput.IndexOf('        // Keep wave information together');$taskEnd=$taskInput.IndexOf('        public Box2 NoticeBox(int length, bool preparing)',$taskStart)
if($taskStart -lt 0 -or $taskEnd -lt 0 -or $taskInput.Remove($taskStart,$taskEnd-$taskStart) -cne [IO.File]::ReadAllText((Join-Path (Get-Location) 'TestResults/north-hud-v0204-baseline/MobileInput.cs'))){throw 'Input changed beyond added header layout helper'}
$taskHud=[IO.File]::ReadAllText((Join-Path (Get-Location) 'Assets/Scripts/Presentation/MobileHud.cs'));$taskOldHud=[IO.File]::ReadAllText((Join-Path (Get-Location) 'TestResults/north-hud-v0204-baseline/MobileHud.cs'))
function WithoutTopBar([string]$text){$a=$text.IndexOf('        private void TopBar()');$b=$text.IndexOf('        private void Controls()',$a);if($a -lt 0 -or $b -lt 0){throw 'Missing HUD methods'};return $text.Remove($a,$b-$a)}
if((WithoutTopBar $taskHud).Replace("        private readonly List<Box2> headerRifts = new List<Box2>(4);`n",'').Replace("        private readonly List<Box2> headerRifts = new List<Box2>(4);`r`n",'') -cne (WithoutTopBar $taskOldHud)){throw 'HUD changed outside top bar and its reusable list'}
function TopBarTail([string]$text){$a=$text.IndexOf('            float right = W - 92;');$b=$text.IndexOf('        private void Controls()',$a);return $text.Substring($a,$b-$a)}
if((TopBarTail $taskHud) -cne (TopBarTail $taskOldHud)){throw 'Currency/pause/boss HUD source changed'}
$taskEditor=Get-Content TestResults/editor-hash-v0204.json -Raw|ConvertFrom-Json
if((Get-FileHash -LiteralPath $taskEditor.path).Hash -ne $taskEditor.sha256){throw 'Editor font validation source changed'}
$taskFont=Get-Content TestResults/wave-header-font-v0204.json -Raw|ConvertFrom-Json
if($taskFont.compactWidth -ne 150 -or @($taskFont.widths|Where-Object {$_+1.5 -gt 150}).Count -or @($taskFont.heights|Where-Object {$_ -gt 32}).Count){throw 'Actual Unity font does not fit'}
$taskGeometry=Get-Content TestResults/north-hud-v0204-geometry.json -Raw|ConvertFrom-Json
if($taskGeometry.cases -ne 42 -or $taskGeometry.totalLayouts -ne 56 -or $taskGeometry.observedSafe106ExtraCases -ne 14 -or $taskGeometry.crisisEntranceGeometryPairs -ne 14 -or $taskGeometry.unchangedClearCases -ne 30 -or -not $taskGeometry.allClear){throw 'Geometry scope differs'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskNorthHash='8735c3eb30d0209f52685ce1ad81fba26e4563ebb542ce7aadb811137f26f4c3'
foreach($taskPhase in @('seed','before-update','after-update','abandon','relaunch')){
    $taskPath="TestResults/v0204-north-$taskPhase.sav";$taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if((Get-FileHash $taskPath).Hash -ne $taskNorthHash -or (Get-Item $taskPath).Length -ne 324 -or -not $taskP.Valid() -or $taskP.MagicCores -ne 130 -or $taskP.StarsTotal -ne 33 -or $taskP.FirstCleared[11] -or $taskP.PendingRun -ne '' -or $taskP.LastReward -ne 30){throw 'North fixture preservation differs'}
}
$taskInterrupted=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0204-north-interrupted.sav')))
if(-not $taskInterrupted.Valid() -or $taskInterrupted.MagicCores -ne 130 -or $taskInterrupted.StarsTotal -ne 33 -or $taskInterrupted.PendingStage -ne 11 -or $taskInterrupted.PendingRun.Length -ne 32){throw 'Interrupted attempt evidence differs'}
$taskBossSeed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0204-boss-seed.sav')))
$taskBossAfter=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0204-boss-abandon.sav')))
if((Get-FileHash TestResults/v0204-boss-seed.sav).Hash -ne (Get-FileHash TestResults/v0204-boss-before.sav).Hash -or $taskBossSeed.TutorialMask -ne 0 -or $taskBossAfter.TutorialMask -ne 32){throw 'Boss lesson bookkeeping differs'}
$taskExpected=$taskBossSeed.Copy();$taskExpected.TutorialMask=32
foreach($taskField in $taskBossAfter.GetType().GetFields()){if(($taskField.GetValue($taskExpected)|ConvertTo-Json -Compress) -cne ($taskField.GetValue($taskBossAfter)|ConvertTo-Json -Compress)){throw "Unexpected boss profile change: $($taskField.Name)"}}
$taskBossHash=(Get-FileHash TestResults/v0204-boss-abandon.sav).Hash.ToLowerInvariant()
if($taskBossHash -ne 'ecb8257f292961e8917c9b05483af957d36c7cf8ec908a6a9ebf871cd6a1632e' -or (Get-FileHash TestResults/v0204-boss-relaunch.sav).Hash -ne $taskBossHash -or -not $taskBossAfter.Valid() -or $taskBossAfter.MagicCores -ne 40 -or $taskBossAfter.StarsTotal -ne 10 -or $taskBossAfter.FirstCleared[5] -or $taskBossAfter.PendingRun -ne '' -or $taskBossAfter.LastReward -ne 0){throw 'Boss earned profile/relaunch differs'}
[pscustomobject]@{northOriginalAndPostHash=$taskNorthHash;northBytes=324;bossBeforeHash=(Get-FileHash TestResults/v0204-boss-seed.sav).Hash.ToLowerInvariant();bossPostHash=$taskBossHash;bossBytes=292;bossOnlyChangedField='TutorialMask0->32 at existing StartRun';bossPostRelaunchIdentical=$true;newReward=0}|ConvertTo-Json|Set-Content TestResults/v0204-save-check.json
$taskUi=Get-Content TestResults/android-v0204-hud-observations.json -Raw|ConvertFrom-Json
if(-not $taskUi.northHeaderAndRiftSeparated -or $taskUi.northScreens.Count -ne 3 -or -not $taskUi.speed2Then1Verified -or $taskUi.northAttemptA.normalAbandonmentVerified -or -not $taskUi.northRecoveryB.normalAbandonment -or -not $taskUi.bossUi.bossAliveWithHealthBar -or -not $taskUi.bossUi.bossWarningAndRedCircle -or -not $taskUi.bossUi.normalAbandonment -or $taskUi.newVersionFullBattle){throw 'Native observation scope differs'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw 'Missing screenshot evidence'}}
$taskEvents=Get-Content TestResults/v0204-north-events-diagnostic.txt -Raw
if($taskEvents -notmatch '7490.*boot_progress_system_run'){throw 'Framework restart evidence missing'}
$taskCrash=Get-Content TestResults/v0204-north-crash-buffer.txt -Raw
if($taskCrash -notmatch 'crash_dump helper failed'){throw 'System crash helper evidence missing'}
$taskHost=Get-Content TestResults/v0204-emulator-out.log -Raw
if(@([regex]::Matches($taskHost,'Failed to find ColorBuffer:')).Count -ne 4){throw 'Host diagnostic scope differs'}
$taskDiag=Get-Content TestResults/v0204-diagnostic-phase-counts.json -Raw|ConvertFrom-Json
if($taskDiag.Count -ne 3 -or @($taskDiag|Where-Object {$_.other -ne 0 -or $_.capturedAppFatal -ne 0 -or $_.packageAnr -ne 0}).Count){throw 'Captured process diagnostics scope differs'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0204.json -Raw|ConvertFrom-Json
if($taskCleanup.serial -ne 'emulator-5618' -or $taskCleanup.qemuPid -ne 9296 -or $taskCleanup.priorityRestored -ne 'Normal' -or $taskCleanup.processesAfterShutdown -ne 0 -or $taskCleanup.reparsePoints -ne 0 -or -not $taskCleanup.protectedStage4Exists -or -not $taskCleanup.protectedBossExists){throw 'Owned shutdown differs'}
if(-not $taskCleanup.removed){
    $taskRemainder=@(Get-ChildItem -LiteralPath $taskCleanup.absoluteTarget -Force -Recurse)
    if($taskCleanup.phase -ne 'delayed-log-removal' -or $taskRemainder.Count -lt 1 -or $taskRemainder.Count -gt 2 -or @($taskRemainder|Where-Object {$_.PSIsContainer -or $_.Name -notin @('emulator-err.log','emulator-out.log') -or $_.Length -gt 20000 -or ($_.Attributes -band [IO.FileAttributes]::ReparsePoint)}).Count){throw 'Unexpected cleanup remainder'}
    foreach($taskF in $taskRemainder){
        $taskCopy=if($taskF.Name -eq 'emulator-out.log'){'TestResults/v0204-emulator-out.log'}else{'TestResults/v0204-emulator-err.log'}
        # Read a stopped emulator's redirected log with sharing; never close another process's handle.
        $taskStream=[IO.FileStream]::new($taskF.FullName,[IO.FileMode]::Open,[IO.FileAccess]::Read,([IO.FileShare]::ReadWrite -bor [IO.FileShare]::Delete))
        try{$taskSha=[Security.Cryptography.SHA256]::Create();try{$taskLogHash=[BitConverter]::ToString($taskSha.ComputeHash($taskStream)).Replace('-','')}finally{$taskSha.Dispose()}}finally{$taskStream.Dispose()}
        if($taskLogHash -ne (Get-FileHash $taskCopy).Hash){throw 'Deferred log evidence differs'}
    }
}elseif(Test-Path -LiteralPath $taskCleanup.absoluteTarget){throw 'Cleanup marked complete but target remains'}
[pscustomobject]@{created=(Get-Date).ToString('o');task=198;version='0.20.4';versionCode=61;sha256=$taskApk.sha256;bytes=$taskApk.bytes;changedRuntimeSourceAndSettingsFiles=$taskChanged;editorFontGuardAdded=$true;otherFiveCoreUnchanged=$true;inputRoutesUnchanged=$true;bossCurrencyPauseHudSourceUnchanged=$true;layoutCases=56;crisisGeometryPairs=14;mobileChecks=31;originalClearLayoutsUnchanged=30;actualUnityLabelMaxWidth=139.3125;compactWidth=150;nativeNorthScreens=3;speed2Then1=$true;recoveryLiveMenu=$true;bossAliveHudAndWarning=$true;newFullBattle=$false;newReward=0;northProfileIdentical=$true;bossOnlyTutorialSeenBitUpdated=$true;bossPostRelaunchIdentical=$true;unexpectedFrameworkRestartObserved=1;frameworkRestartCauseResolved=$false;hostInvalidColorBufferPairs=4;capturedAppPhaseDiagnostics=$taskDiag;performanceBenchmark=$false;ownedAvdStopped=$true;ownedAvdRemoved=[bool]$taskCleanup.removed;cleanupLogRemovalDeferred=(-not $taskCleanup.removed);humanFunEvaluated=$false;physicalAndroidTested=$false;iosTested=$false;audioReviewed=$false}|ConvertTo-Json -Depth 6|Set-Content TestResults/delivery-consistency-v0204.json
'PASS v0.20.4 functional HUD acceptance:56 geometry/31 input/font/build/install/save and native north/boss UI. Earlier framework restart remains unresolved; cleanup status is recorded separately.'
