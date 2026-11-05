param([Parameter(Mandatory=$true)][ValidatePattern('^v[0-9]+(?:-[a-z0-9]+)?$')][string]$Tag,
    [Parameter(Mandatory=$true)][int]$AppPid,
    [Parameter(Mandatory=$true)][int]$FrameworkBefore,
    [Parameter(Mandatory=$true)][int]$FrameworkAfter,[switch]$NoCompletedRun,[switch]$NoRecording,[switch]$StartupNotCaptured)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
# Read captured evidence only. No ADB input, rerun, source change, or historical result replacement.
$raw=@(Get-Content -LiteralPath "TestResults/$Tag-battle-logcat.txt")
$events=@(Get-Content -LiteralPath "TestResults/$Tag-events-logcat.txt")
$lines=@(for($i=0;$i -lt $raw.Count;$i++) {
    if($raw[$i] -match '^(?<stamp>\d\d-\d\d \d\d:\d\d:\d\d\.\d+)\s+(?<pid>\d+)\s+\d+\s+(?<priority>[VDIWEF])\s+(?<tag>[^:]+):\s?(?<message>.*)$' -and [int]$Matches.pid -eq $AppPid){
        [pscustomobject]@{index=$i;timestamp=$Matches.stamp;priority=$Matches.priority;tag=$Matches.tag.Trim();message=$Matches.message;line=$raw[$i]}
    }
})
$first=@($lines|Where-Object message -like 'GATEFALL PERF*'|Select-Object -First 1)
if($first.Count -ne 1){throw 'Captured app PERF boundary missing'}
$errors=@($lines|Where-Object priority -in @('E','F'))
$startup=@($errors|Where-Object index -lt $first[0].index)
$post=@($errors|Where-Object index -gt $first[0].index)
$startup.line|Set-Content -LiteralPath "TestResults/$Tag-startup-errors.txt"
$post.line|Set-Content -LiteralPath "TestResults/$Tag-post-perf-errors.txt"
$fatal=@($lines|Where-Object {$_.priority -eq 'F' -or $_.message -match 'FATAL EXCEPTION|Fatal signal|Abort message:'})
$anr=@($events|Where-Object {$_ -match '\b(am_anr|am_crash)\s*:' -and $_ -match 'com\.gatefall\.prototype'})
$watchdog=@($events|Where-Object {$_ -match 'com\.gatefall\.prototype' -and $_ -match '\b(am_kill|am_proc_died)\s*:' -and $_ -match 'watchdog'})
$audit=@($events|Where-Object {$_ -match "\s$AppPid\s" -and $_ -match 'avc:.*denied.*raon-profile\.sav'})
$audit|Set-Content -LiteralPath "TestResults/$Tag-save-link-audits.txt"
[ordered]@{appPid=$AppPid;frameworkBefore=$FrameworkBefore;frameworkAfter=$FrameworkAfter;firstPerf=$first[0].timestamp;startupErrorCount=$startup.Count;startupAssetPackManager=@($startup|Where-Object message -match 'AssetPackManager').Count;startupEgl=@($startup|Where-Object tag -eq 'EGL_emulation').Count;startupSurface=@($startup|Where-Object tag -eq 'Surface').Count;postFirstPerfSurface=@($post|Where-Object tag -eq 'Surface').Count;postFirstPerfOther=@($post|Where-Object tag -ne 'Surface').Count;fatalPriorityLines=@($lines|Where-Object priority -eq 'F').Count;javaOrNativeFatalSignatures=$fatal.Count;packageAnrOrCrash=$anr.Count;watchdogKills=$watchdog.Count;profileLinkAuditLines=$audit.Count;rootCausesResolved=$false;scope=if($NoCompletedRun){"Captured app PID $AppPid during Ready/short LIVE/explicit abandonment and map, before cold relaunch. Resizes included; no completed run or recording claimed. No physical-device or power-loss atomicity conclusion."}else{"Captured buffers for app PID $AppPid through result, before cold relaunch. Startup and focus transitions included; recording status is recorded separately. No physical-device or power-loss atomicity conclusion."}}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath "TestResults/$Tag-diagnostics.json"
if($StartupNotCaptured){
    $diagnosticPath="TestResults/$Tag-diagnostics.json"
    $capturedDiagnostic=Get-Content $diagnosticPath -Raw|ConvertFrom-Json
    $capturedDiagnostic|Add-Member -NotePropertyName startupCaptured -NotePropertyValue $false
    $capturedDiagnostic.scope="App PID $AppPid from pre-battle buffer reset through result, before cold relaunch. Cold-start logs were cleared before this segment; startup0 is not proof of resolution. Representative video is recorded separately. No physical-device or power-loss atomicity conclusion."
    $capturedDiagnostic|ConvertTo-Json -Depth 5|Set-Content $diagnosticPath
}
$windows=@(foreach($line in $lines){
    if($line.message -match '^GATEFALL PERF page=(\w+) state=(\w+) enemies=(\d+) avgMs=([\d.]+|na) worstMs=([\d.]+|na) over33ms=(\d+)/(\d+) monoMB=([\d.]+)' -and $line.message -match 'timingScope=ActiveCombat'){
        # Save captures before another regex can replace the named match state.
        $line.message -match '^GATEFALL PERF page=(\w+) state=(\w+) enemies=(\d+) avgMs=([\d.]+|na) worstMs=([\d.]+|na) over33ms=(\d+)/(\d+) monoMB=([\d.]+)'|Out-Null
        if([int]$Matches[7] -gt 0){[pscustomobject]@{timestamp=$line.timestamp;pid=$AppPid;page=$Matches[1];state=$Matches[2];enemies=[int]$Matches[3];averageMs=[double]$Matches[4];worstMs=[double]$Matches[5];over33ms=[int]$Matches[6];samples=[int]$Matches[7];monoMB=[double]$Matches[8]}}
    }
})
if(@($windows|Where-Object {$_.page -ne 'Battle' -or $_.state -ne 'Running'}).Count){throw 'Non-combat sample windows need separate review'}
$allPerf=@($lines|Where-Object message -like 'GATEFALL PERF*')
$samples=($windows|Measure-Object samples -Sum).Sum
$weighted=0.0;foreach($window in $windows){$weighted+=$window.averageMs*$window.samples}
[ordered]@{created=(Get-Date -Format o);source="TestResults/$Tag-battle-logcat.txt";appPidFilter=$AppPid;status='active-combat-samples';environment=if($NoCompletedRun){'Owned API35 emulator; short LIVE/menu check followed by explicit abandonment; no completed battle or recording'}elseif($NoRecording){'Owned API35 emulator; LIVE menus/focus transitions, no video; not whole-battle/phone benchmark'}else{'Owned API35 emulator; LIVE menus and representative12s recording included; not whole-battle/phone benchmark'};allWindows=$allPerf.Count;scopedNoSampleWindows=$allPerf.Count-$windows.Count;battleRunningWindows=$windows.Count;frameSamples=$samples;weightedAverageMs=if($samples){$weighted/$samples}else{$null};worstMs=($windows|Measure-Object worstMs -Maximum).Maximum;framesOver33ms=($windows|Measure-Object over33ms -Sum).Sum;slowThresholdMs=1000.0/30;maxEnemiesAtWindowEnd=($windows|Measure-Object enemies -Maximum).Maximum;maxManagedMonoMB=($windows|Measure-Object monoMB -Maximum).Maximum;scope='Consecutive focused ActiveCombat windows only; displayed averages already rounded. No whole-run, physical FPS, GPU, total memory, or human-fun inference.';windows=$windows}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath "TestResults/$Tag-active-performance.json"
if($NoCompletedRun){'Captured diagnostics/ActiveCombat samples only. No completed battle or priced native receipt claim.';exit}
$receipts=@(foreach($line in Get-Content -LiteralPath "TestResults/$Tag-last-run.txt"){
    if($line -match '^([\d.]+) (build|upgrade) (\w+) @(\d+)$'){
        $kind=$Matches[3];$action=$Matches[2];$cost=switch("$action $kind"){'build Autocannon'{40};'build Binding'{45};'build Rupture'{65};'build Marker'{45};'upgrade Autocannon'{30};'upgrade Binding'{40};'upgrade Rupture'{55};'upgrade Marker'{40};'upgrade Arc'{60};'upgrade Shatter'{60};'upgrade Observer'{60};default{throw "Unreviewed receipt price: $line"}}
        [pscustomobject]@{receipt=$line;action=$action;cost=$cost}
    } elseif($line -match '^[\d.]+ fusion \(Autocannon\+Binding\) -> Arc @\d+$') {[pscustomobject]@{receipt=$line;action='fusion';cost=40}}
    elseif($line -match '^[\d.]+ fusion \(Autocannon\+Rupture\) -> Shatter @\d+$') {[pscustomobject]@{receipt=$line;action='fusion';cost=45}}
    elseif($line -match '^[\d.]+ fusion \(Autocannon\+Marker\) -> Observer @\d+$') {[pscustomobject]@{receipt=$line;action='fusion';cost=40}}
    elseif($line -match '^[\d.]+ (build|upgrade|fusion|sell) '){throw "Unreviewed receipt: $line"}
})
$cumulative=0;foreach($r in $receipts){$cumulative+=$r.cost;$r|Add-Member -NotePropertyName cumulative -NotePropertyValue $cumulative}
$builds=@($receipts|Where-Object action -eq 'build').Count;$fusions=@($receipts|Where-Object action -eq 'fusion').Count
[ordered]@{created=(Get-Date -Format o);source="$Tag-last-run.txt";priceScope='Observed current mobile costs, no sale or rebate; fusion costs exclude already purchased/consumed material costs';receipts=$receipts;builds=$builds;upgrades=@($receipts|Where-Object action -eq 'upgrade').Count;fusions=$fusions;sales=0;pricedSpend=$cumulative;finalTowersDerivedFromBuildsMinusFusions=$builds-$fusions}|ConvertTo-Json -Depth 6|Set-Content -LiteralPath "TestResults/$Tag-native-purchase-ledger.json"
'Captured native diagnostics, ActiveCombat windows and priced receipts summarized; this is not a new Android run.'
