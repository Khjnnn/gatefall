$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
# Read only. Never selects a device, sends input, changes priority or kills a process.
$owners=@(Get-ChildItem -LiteralPath TestResults -Filter '*owned-avd.json' -File|ForEach-Object {
    $o=Get-Content $_.FullName -Raw|ConvertFrom-Json
    if($o.serial -notin @('emulator-5556','emulator-5558') -and $o.name){[pscustomobject]@{file=$_.Name;name=$o.name;serial=$o.serial;recordedPhase=$o.phase}}
})
$processes=@(Get-CimInstance Win32_Process)
$running=@(foreach($p in $processes|Where-Object {$_.Name -match '^(?:qemu-system-x86_64(?:-headless)?|emulator)\.exe$'}){
    foreach($o in $owners){
        if($p.CommandLine -match ('-avd\s+"?'+[regex]::Escape($o.name)+'"?(?:\s|$)')){[pscustomobject]@{pid=$p.ProcessId;parentPid=$p.ParentProcessId;executable=$p.Name;name=$o.name;serial=$o.serial}}
    }
})
$projectArgument='-projectPath\s+"?'+[regex]::Escape((Get-Location).Path)+'"?(?:\s|$)'
$builds=@($processes|Where-Object {$_.Name -eq 'Unity.exe' -and $_.CommandLine -match '-batchmode' -and $_.CommandLine -match $projectArgument})
$legacy=@($owners|Where-Object {$_.recordedPhase -notin @('complete','deleted','delayed-log-removal')})
[ordered]@{created=(Get-Date -Format o);method='Read-only Win32_Process QEMU/launcher executable-name plus exact known non-protected owned AVD name; Unity batchmode+exact projectPath filter';ownerSnapshots=$owners.Count;runningOwnedEmulators=@($running|Select-Object -ExpandProperty name -Unique).Count;runningOwnedProcessCount=$running.Count;runningOwnedLaunchers=@($running|Where-Object executable -eq 'emulator.exe').Count;ownedProcesses=$running;runningGatefallBatchBuilds=$builds.Count;legacySnapshotOnlyOwners=$legacy;legacyPhaseAloneIsNotLiveness=$true;adbInputSent=$false;processesChanged=$false;protectedDevicesChanged=$false}|ConvertTo-Json -Depth 6|Set-Content TestResults/qa-owned-process-census-v0243.json
"Read-only census:owned emulators=$($running.Count),Gatefall batch builds=$($builds.Count). Historical owner phases retained; no input/process changes."
