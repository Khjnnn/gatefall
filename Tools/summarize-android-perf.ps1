param(
    [string]$Log='TestResults/android-v050-final-runtime.log',
    [string]$Output='TestResults/android-v050-performance.json',
    [string]$ApkSha256='',
    [int]$AppPid=0,
    [string]$Environment='Unspecified runtime; no physical-device or GPU benchmark claim'
)
$ErrorActionPreference='Stop'
$taskRows=@(foreach($taskLine in Get-Content -LiteralPath $Log){
    $taskM=[regex]::Match($taskLine,'GATEFALL PERF page=(\w+) state=(\w+|-) enemies=(\d+) avgMs=([\d.]+|na) worstMs=([\d.]+|na) over33ms=(\d+)/(\d+) monoMB=([\d.]+)')
    if(-not $taskM.Success){continue}
    $taskPidMatch=[regex]::Match($taskLine,'^\d\d-\d\d \S+\s+(\d+)\s+')
    $taskPidValue=if($taskPidMatch.Success){[int]$taskPidMatch.Groups[1].Value}else{0}
    if($AppPid -gt 0 -and $taskPidValue -ne $AppPid){continue}
    $taskScoped=$taskLine.Contains('timingScope=ActiveCombat')
    [pscustomobject]@{timestamp=$taskLine.Substring(0,[math]::Min(18,$taskLine.Length));pid=$taskPidValue;page=$taskM.Groups[1].Value;state=$taskM.Groups[2].Value;enemies=[int]$taskM.Groups[3].Value;
        avgMs=$(if($taskM.Groups[4].Value -ne 'na'){[double]::Parse($taskM.Groups[4].Value,[Globalization.CultureInfo]::InvariantCulture)}else{$null});
        worstMs=$(if($taskM.Groups[5].Value -ne 'na'){[double]::Parse($taskM.Groups[5].Value,[Globalization.CultureInfo]::InvariantCulture)}else{$null});
        slow=[int]$taskM.Groups[6].Value;samples=[int]$taskM.Groups[7].Value;monoMB=[double]::Parse($taskM.Groups[8].Value,[Globalization.CultureInfo]::InvariantCulture);activeCombatScope=$taskScoped}
})
$taskBattle=@($taskRows|Where-Object {$_.activeCombatScope -and $_.page -eq 'Battle' -and $_.state -eq 'Running' -and $_.samples -gt 0 -and $null -ne $_.avgMs -and $null -ne $_.worstMs})
$taskSamples=($taskBattle|Measure-Object samples -Sum).Sum
$taskAvailable=$taskBattle.Count -gt 0 -and $taskSamples -gt 0
$taskReport=[ordered]@{
    created=(Get-Date -Format o);source=$Log;apkSha256=$ApkSha256;appPidFilter=$AppPid;environment=$Environment
    status=$(if($taskAvailable){'active-combat-samples'}else{'insufficient-active-samples'})
    scope='Only labeled consecutive focused combat frames; excludes legacy mixed windows, cards, inactive periods and transition intervals. No physical-device/GPU/total-memory inference.'
    allWindows=$taskRows.Count;legacyMixedWindowsExcluded=@($taskRows|Where-Object {-not $_.activeCombatScope}).Count
    scopedNoSampleWindows=@($taskRows|Where-Object {$_.activeCombatScope -and $_.samples -eq 0}).Count
    battleRunningWindows=$taskBattle.Count;frameSamples=$(if($taskAvailable){[int]$taskSamples}else{0})
    weightedAverageMs=$(if($taskAvailable){[math]::Round((($taskBattle|ForEach-Object {$_.avgMs*$_.samples}|Measure-Object -Sum).Sum/$taskSamples),2)}else{$null})
    worstMs=$(if($taskAvailable){($taskBattle|Measure-Object worstMs -Maximum).Maximum}else{$null})
    framesOver33ms=$(if($taskAvailable){($taskBattle|Measure-Object slow -Sum).Sum}else{$null})
    slowThresholdMs=1000/30;maxEnemiesAtWindowEnd=$(if($taskAvailable){($taskBattle|Measure-Object enemies -Maximum).Maximum}else{$null})
    maxManagedMonoMB=$(if($taskAvailable){($taskBattle|Measure-Object monoMB -Maximum).Maximum}else{$null})
    windows=$taskBattle;allParsedWindows=$taskRows
}
$taskReport|ConvertTo-Json -Depth 6|Set-Content -LiteralPath $Output -Encoding utf8
[pscustomobject]$taskReport|Select-Object status,allWindows,legacyMixedWindowsExcluded,battleRunningWindows,frameSamples,weightedAverageMs,worstMs|Format-List
