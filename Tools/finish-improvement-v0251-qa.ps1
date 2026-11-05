$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$ownerPath='TestResults/v0250-owned-avd.json'
$owner=Get-Content $ownerPath -Raw|ConvertFrom-Json
$expected=[IO.Path]::GetFullPath((Join-Path (Get-Location) 'TestResults/improvement-v0250-avd'))
if($owner.serial -ne 'emulator-5652' -or $owner.name -ne 'GatefallImproveV250' -or $owner.qemuPid -ne 37028 -or $owner.launcherPid -ne 36036 -or $owner.root -cne $expected){throw 'Unexpected owner; no cleanup'}
$resolved=(Resolve-Path -LiteralPath $expected).Path
if($resolved -cne $expected){throw 'Unexpected resolved cleanup path'}
$reparse=@(Get-Item -LiteralPath $expected;Get-ChildItem -LiteralPath $expected -Recurse -Force)|Where-Object {($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0}
if(@($reparse).Count){throw 'Reparse point in owned root'}
$qemu=@(Get-CimInstance Win32_Process|Where-Object {$_.ProcessId -eq 37028 -and $_.Name -match '^qemu-system-x86_64(?:-headless)?\.exe$' -and $_.ParentProcessId -eq 36036 -and $_.CommandLine -match '-avd GatefallImproveV250 -port 5652'})
$launcher=@(Get-CimInstance Win32_Process|Where-Object {$_.ProcessId -eq 36036 -and $_.Name -eq 'emulator.exe' -and $_.CommandLine -match '-avd GatefallImproveV250 -port 5652'})
if($qemu.Count -ne 1 -or $launcher.Count -ne 1){throw 'Owned process identity mismatch'}
foreach($id in @(37028,36036)){(Get-Process -Id $id).PriorityClass='Normal'}
foreach($name in @('stdout.log','stderr.log')){Copy-Item -LiteralPath (Join-Path $expected $name) -Destination ('TestResults/v0251-owned-'+$name)}
$adb='C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe'
& $adb -s emulator-5652 emu kill
if($LASTEXITCODE){throw 'Normal emulator shutdown failed'}
Start-Sleep -Seconds 4
$remaining=@(Get-CimInstance Win32_Process|Where-Object {($_.ProcessId -in @(37028,36036) -and $_.Name -match '^(qemu-system-x86_64(?:-headless)?|emulator)\.exe$') -or ($_.Name -match '^qemu-system-x86_64(?:-headless)?\.exe$' -and $_.CommandLine -match '-avd GatefallImproveV250 -port 5652')})
if($remaining.Count){throw 'Owned emulator still alive; no deletion'}
if((Resolve-Path -LiteralPath $expected).Path -cne $expected){throw 'Cleanup target changed'}
$reparse=@(Get-Item -LiteralPath $expected;Get-ChildItem -LiteralPath $expected -Recurse -Force)|Where-Object {($_.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0}
if(@($reparse).Count){throw 'Reparse point appeared; no deletion'}
$messages=@()
foreach($child in Get-ChildItem -LiteralPath $expected -Force){try{Remove-Item -LiteralPath $child.FullName -Recurse -Force}catch{$messages+=$_.Exception.Message}}
$left=@(Get-ChildItem -LiteralPath $expected -Recurse -Force)
if(@($left|Where-Object {$_.PSIsContainer -or $_.Name -notin @('stdout.log','stderr.log')}).Count){throw 'Unexpected leftover; preserve root for inspection'}
if($left.Count -eq 0){Remove-Item -LiteralPath $expected -Force}
$owner.phase=if($left.Count){'delayed-log-removal'}else{'deleted'}
$owner|ConvertTo-Json|Set-Content $ownerPath
[ordered]@{created=(Get-Date -Format o);task=267;serial=$owner.serial;name=$owner.name;qemuPid=37028;launcherPid=36036;normalPrioritiesRestored=$true;normalExit=$true;remainingProcesses=0;resolvedRoot=$expected;reparsePoints=0;rootRemoved=(-not(Test-Path -LiteralPath $expected));diskFilesRemoved=$true;remaining=@($left|Select-Object Name,Length,FullName);logCopiesPreserved=$true;removalMessages=$messages;protectedDevicesTouched=$false}|ConvertTo-Json -Depth 6|Set-Content TestResults/qa-cleanup-v0251.json
'Owned5652 normal shutdown/process0/disk cleanup recorded; protected devices untouched.'

