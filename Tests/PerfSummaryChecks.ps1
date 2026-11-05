$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskDir='TestResults/perf-summary-228-fixtures'
New-Item -ItemType Directory -Path $taskDir -Force|Out-Null
$taskLines=@(
    '10-31 10:00:00.000   999  1000 I Unity : GATEFALL PERF page=Battle state=Running enemies=9 avgMs=10000.0 worstMs=50000.0 over33ms=2/10 monoMB=1.0',
    '10-31 10:00:10.000   999  1000 I Unity : GATEFALL PERF page=City state=Ready enemies=0 avgMs=na worstMs=na over33ms=0/0 monoMB=1.0 timingScope=ActiveCombat',
    '10-31 10:00:20.000   999  1000 I Unity : GATEFALL PERF page=Battle state=Running enemies=20 avgMs=16.0 worstMs=20.0 over33ms=0/100 monoMB=1.2 timingScope=ActiveCombat',
    '10-31 10:00:30.000   999  1000 I Unity : GATEFALL PERF page=Battle state=Paused enemies=20 avgMs=500.0 worstMs=600.0 over33ms=20/20 monoMB=1.2 timingScope=ActiveCombat',
    '10-31 10:00:40.000   999  1000 I Unity : GATEFALL PERF page=Battle state=Running enemies=30 avgMs=20.0 worstMs=200.0 over33ms=2/50 monoMB=1.3 timingScope=ActiveCombat',
    '10-31 10:00:50.000   999  1000 I Unity : GATEFALL PERF page=Battle state=Running enemies=30 avgMs=na worstMs=na over33ms=0/0 monoMB=1.3 timingScope=ActiveCombat',
    '10-31 10:01:00.000   998  1000 I Unity : GATEFALL PERF page=Battle state=Running enemies=99 avgMs=999.0 worstMs=999.0 over33ms=5/5 monoMB=2.0 timingScope=ActiveCombat'
)
$taskLines|Set-Content "$taskDir/mixed.log"
& .\Tools\summarize-android-perf.ps1 -Log "$taskDir/mixed.log" -Output "$taskDir/summary.json" -AppPid 999 -Environment 'Synthetic parser fixture'|Out-Null
$taskR=Get-Content "$taskDir/summary.json" -Raw|ConvertFrom-Json
if($taskR.allWindows -ne 6 -or $taskR.legacyMixedWindowsExcluded -ne 1 -or $taskR.battleRunningWindows -ne 2 -or
    $taskR.frameSamples -ne 150 -or $taskR.weightedAverageMs -ne 17.33 -or $taskR.worstMs -ne 200 -or $taskR.framesOver33ms -ne 2){throw 'Mixed/legacy/PID filtering or long-frame summary differs'}
$taskLines[0]|Set-Content "$taskDir/legacy-only.log"
& .\Tools\summarize-android-perf.ps1 -Log "$taskDir/legacy-only.log" -Output "$taskDir/legacy-summary.json" -Environment 'Synthetic legacy-only fixture'|Out-Null
$taskL=Get-Content "$taskDir/legacy-summary.json" -Raw|ConvertFrom-Json
if($taskL.status -ne 'insufficient-active-samples' -or $taskL.frameSamples -ne 0 -or $null -ne $taskL.weightedAverageMs){throw 'Legacy-only log falsely claimed usable performance'}
'PASS: scoped/legacy/na/paused/PID parser and weighted samples; genuine200ms retained; legacy-only insufficient'
