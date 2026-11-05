$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskMeta=Get-Content TestResults/android-apk-v0206-candidate.json -Raw|ConvertFrom-Json
if($taskMeta.version -ne '0.20.6' -or $taskMeta.versionCode -ne 63 -or $taskMeta.signature -ne 'verified' -or $taskMeta.alignment16KB -ne 'verified'){throw 'Release metadata mismatch'}
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.20.6.apk','TestResults/v0206-installed.apk')){
    if((Get-FileHash $taskPath).Hash -ne $taskMeta.sha256 -or (Get-Item $taskPath).Length -ne $taskMeta.bytes){throw 'Delivery/install differs'}
}
$taskOld=Get-Content TestResults/breach-v0206-baseline/source-hashes-v0205.json -Raw|ConvertFrom-Json
$taskNew=Get-Content TestResults/source-hashes-v0206-candidate.json -Raw|ConvertFrom-Json
$taskChanged=@(foreach($taskR in $taskNew){
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Candidate source changed'}
    if(@($taskOld|Where-Object path -eq $taskR.path)[0].sha256 -ne $taskR.sha256){$taskR.path}
})
$taskExpected=@('Assets\Scripts\Core\RaonCombat.cs','Assets\Scripts\Core\DefenseSimulation.cs','Assets\Scripts\Presentation\GatefallGame.cs','ProjectSettings\ProjectSettings.asset')
if(@(Compare-Object $taskChanged $taskExpected).Count){throw 'Unexpected source scope'}
& (Join-Path $PSScriptRoot 'review-breach-metadata.ps1') -UseRecorded
$taskGame=[IO.File]::ReadAllText('Assets/Scripts/Presentation/GatefallGame.cs').Replace("`r`n","`n")
$taskBlock=@'
                else if (effect.Kind == EventKind.CoreHurt && IsCampaign && Simulation.State == RunState.Running &&
                    effect.Source >= 0 && effect.Source < Simulation.GatePositions.Length &&
                    !Simulation.AwaitingCard && Time.unscaledTime >= NoticeUntil) {
                    ShowNotice(MobileHud.GateName(effect.Source) + "에서 결계 피해 · 해당 길을 보강하세요", 2.4f);
                    Debug.Log("GATEFALL CORE BREACH lane=" + effect.Source + " elapsed=" + Simulation.Elapsed.ToString("F2") + " core=" + Simulation.CoreHealth.ToString("F1"));
                }
'@
if($taskGame.Replace($taskBlock+"`n",'') -cne [IO.File]::ReadAllText('TestResults/breach-v0206-baseline/GatefallGame.cs').Replace("`r`n","`n")){throw 'Presentation changed beyond guarded notice'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskHash='944dc9755d53a5b3bdf32b8a6430d977e73832fefbdd4697fb88b98f9e78547f'
$taskReference=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0206-seed.sav')))
$taskSaves=@(foreach($taskPhase in @('before-update','after-update','abandon','relaunch')){
    $taskPath="TestResults/v0206-$taskPhase.sav";$taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if(-not $taskP.Valid() -or (Get-Item $taskPath).Length -ne 324 -or (Get-FileHash $taskPath).Hash -ne $taskHash){throw 'Save mismatch'}
    foreach($taskF in $taskP.GetType().GetFields()){if(($taskF.GetValue($taskP)|ConvertTo-Json -Compress) -cne ($taskF.GetValue($taskReference)|ConvertTo-Json -Compress)){throw 'Profile fields differ'}}
    [ordered]@{phase=$taskPhase;bytes=324;sha256=$taskHash;valid=$true;magic=$taskP.MagicCores;stars=$taskP.StarsTotal;reward=$taskP.LastReward;rewardRun=$taskP.LastRewardRun;pending=$taskP.PendingRun}
})
$taskSaves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0206-save-check.json
$taskLog=Get-Content TestResults/v0206-logcat-final.txt
$taskApp=@($taskLog|Where-Object {$_ -match '^\d\d-\d\d \S+\s+4736\s+'})
$taskFirst=($taskApp|Where-Object {$_ -match 'GATEFALL PERF'}|Select-Object -First 1).Substring(0,18)
$taskEF=@($taskApp|Where-Object {$_.Substring(0,18) -ge $taskFirst -and $_ -match '\s[EF]\s'})
$taskSurface=@($taskEF|Where-Object {$_ -match 'E Surface\s*:'})
$taskOther=@($taskEF|Where-Object {$_ -notmatch 'E Surface\s*:'})
$taskAnr=@(Get-Content TestResults/v0206-events-final.txt|Where-Object {$_ -match 'am_anr:.*com.gatefall.prototype'}).Count
$taskFatal=@($taskApp|Where-Object {$_ -match '\sF\s'}).Count
$taskWatchdog=@($taskLog|Where-Object {$_ -match 'W Watchdog: \*\*\* WATCHDOG KILLING SYSTEM PROCESS'}).Count
if($taskOther.Count -ne 0 -or $taskAnr -ne 0 -or $taskFatal -ne 0 -or $taskWatchdog -ne 0){throw 'Captured diagnostics need review'}
foreach($taskPhase in @('before','final')){if((Get-Content "TestResults/v0206-system-$taskPhase.txt").Trim() -ne '581'){throw 'Framework PID changed'}}
if(-not($taskApp|Where-Object {$_ -match 'CORE BREACH lane=0 elapsed=29.33 core=299.0'}) -or -not($taskApp|Where-Object {$_ -match 'CORE BREACH lane=1 elapsed=35.20 core=294.0'})){throw 'Breach observations missing'}
foreach($taskShot in @('city-loaded','stage13-ready','card30','first-breach','abandon-pause','abandon-city','relaunch-city')){if(-not(Test-Path "Docs/QA/v050/v0206-$taskShot.png")){throw 'Screen evidence missing'}}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0206.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.normalEmulatorShutdown -or $taskCleanup.ownedProcessesRemaining -ne 0 -or $taskCleanup.reparsePoints -ne 0){throw 'Cleanup incomplete'}
[ordered]@{created=(Get-Date -Format o);task=212;version='0.20.6';code=63;bytes=$taskMeta.bytes;sha256=$taskMeta.sha256;productChanges=$taskChanged;
    combatDamageMovementAndEconomyUnchanged=$true;metadataOnlyCoreChange=$true;newModels=1;selectedModelMatchesOldOutcomeLedgerCardsDamageAndShots=$true;campaignChecks=49;unitDamageEntrances=@(0,1,2);
    nativeStage=13;nativeScope='Zero-tower short breach feedback check; normal abandon, not a complete battle';nativeTowers=0;nativeSpend=0;firstCard='Range';firstSupply=100;
    loggedWestBreach=@{elapsed=29.33;core=299};displayedNorthBreach=@{elapsed=35.20;core=294};shownMessage='북쪽에서 결계 피해 · 해당 길을 보강하세요';pauseCore=282;newReward=0;
    cardScreenHidesToast=$true;allWarningPrioritiesNativelyExercised=$false;allThreeEntrancesNativelyExercised=$false;simultaneousThreatsFullyEnumerated=$false;
    saveSha256=$taskHash;realBeforeUpdateExtractionSucceeded=$true;appPid=4736;frameworkPid=581;postFirstPerfSurface=$taskSurface.Count;postFirstPerfOtherEF=$taskOther.Count;appFatal=$taskFatal;packageAnr=$taskAnr;watchdogKill=$taskWatchdog;
    startupDiagnosticsResolved=$false;homeMixedPerfIsBenchmark=$false;physicalDeviceTested=$false;humanFunEvaluated=$false;iosTested=$false;audioTested=$false;cleanup=$taskCleanup;passed=$true
}|ConvertTo-Json -Depth 7|Set-Content TestResults/delivery-consistency-v0206.json
Write-Output "PASS breach delivery: metadata/presentation only; three lane checks; native north feedback; four actual save extractions unchanged; Surface=$($taskSurface.Count),otherEF=$($taskOther.Count)."
