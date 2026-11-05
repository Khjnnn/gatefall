$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/road-direction-v0241-avd'
$ownerPath=Join-Path $project 'TestResults/v0241-owned-avd.json'
if((Test-Path $root) -or (Test-Path $ownerPath)){throw 'Owned QA already prepared; inspect owner/progress instead of creating a duplicate'}
$sdk='C:\dev\rea_project\realm_defense_work\tools\android-sdk'
$jdk='C:\dev\rea_project\realm_defense_work\tools\jdk21\jdk-21.0.12.1+1'
if(-not(Test-Path (Join-Path $sdk 'system-images/android-35/google_apis_playstore/x86_64/system.img')) -or -not(Test-Path (Join-Path $jdk bin/java.exe))){throw 'Existing local Android image/JDK not found'}
$active=@(Get-CimInstance Win32_Process|Where-Object {$_.Name -match '^(emulator|qemu-system-x86_64(?:-headless)?)\.exe$' -and $_.CommandLine -match 'GatefallRoadV241|(?:-port\s+5646)(?:\s|$)'})
if($active.Count -or @(Get-NetTCPConnection -LocalPort 5646 -ErrorAction SilentlyContinue).Count){throw 'Owned QA name/port already active; do not duplicate'}
New-Item -ItemType Directory -Path $root|Out-Null
foreach($dir in @('avd-home','user','emulator-home')){New-Item -ItemType Directory -Path (Join-Path $root $dir)|Out-Null}
$env:JAVA_HOME=$jdk;$env:ANDROID_SDK_ROOT=$sdk;$env:ANDROID_AVD_HOME=Join-Path $root avd-home;$env:ANDROID_USER_HOME=Join-Path $root user;$env:ANDROID_EMULATOR_HOME=Join-Path $root emulator-home
$owner=[ordered]@{created=(Get-Date -Format o);task=255;name='GatefallRoadV241';serial='emulator-5646';root=$root;phase='creating';launcherPid=0;qemuPid=0;appPid=0}
$owner|ConvertTo-Json|Set-Content $ownerPath
'no'|& (Join-Path $sdk cmdline-tools/16.0/bin/avdmanager.bat) create avd -n GatefallRoadV241 -k 'system-images;android-35;google_apis_playstore;x86_64' -p (Join-Path $root device.avd) --device pixel_8_pro *> (Join-Path $root avd-create.log)
if($LASTEXITCODE -ne 0){$owner.phase='create-failed';$owner|ConvertTo-Json|Set-Content $ownerPath;throw 'AVD creation failed; inspect preserved owned root'}
$process=Start-Process -FilePath (Join-Path $sdk emulator/emulator.exe) -ArgumentList @('-avd','GatefallRoadV241','-port','5646','-no-window','-gpu','host','-memory','2560','-cores','4','-no-audio','-no-snapshot','-no-boot-anim','-skin','2400x1080') -WindowStyle Hidden -PassThru -RedirectStandardOutput (Join-Path $root stdout.log) -RedirectStandardError (Join-Path $root stderr.log)
$owner.launcherPid=$process.Id;$owner.phase='starting'
Start-Sleep -Seconds 2
$qemu=@(Get-CimInstance Win32_Process|Where-Object {$_.Name -match '^qemu-system-x86_64(?:-headless)?\.exe$' -and $_.ParentProcessId -eq $process.Id -and $_.CommandLine -match '-avd GatefallRoadV241 -port 5646'})
if($qemu.Count -eq 1){$owner.qemuPid=$qemu[0].ProcessId}
$owner|ConvertTo-Json|Set-Content $ownerPath
$owner|ConvertTo-Json

