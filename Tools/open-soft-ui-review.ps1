# Run locally only when ready to open the new UI candidate in its own visible emulator.
# Does not install on, stop, reset or delete the user's existing5692 or protected5556/5558.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$ownerFile=Join-Path $project 'TestResults/soft-ui-v02811-owned-avd.json'
$record=Join-Path $project 'TestResults/soft-ui-20261106'
$o=Get-Content -LiteralPath $ownerFile -Raw|ConvertFrom-Json
$expectedRoot=[IO.Path]::GetFullPath((Join-Path $project 'TestResults/soft-ui-v02811-avd'))
if($o.name -cne 'GatefallSoftUIV2811' -or $o.serial -cne 'emulator-5694' -or [IO.Path]::GetFullPath($o.root) -cne $expectedRoot){throw 'Review device identity differs'}
$sdk='C:/dev/rea_project/realm_defense_work/tools/android-sdk'
$adb=Join-Path $sdk platform-tools/adb.exe
$apk=Join-Path $project 'Builds/Android/Gatefall-v0.28.11.apk'
$apkHash='1b76590321a5d910a5e09692df9de10a2133d1e70c3cccbc25cdb5ce6dd2e4ce'
if((Get-FileHash -LiteralPath $apk).Hash.ToLowerInvariant() -cne $apkHash){throw 'Candidate APK changed; review the newer delivery first'}
$active=@(Get-CimInstance Win32_Process|Where-Object{$_.Name -match '^(emulator|qemu-system-x86_64(?:-headless)?)\.exe$' -and $_.CommandLine -match 'GatefallSoftUIV2811|(?:-port\s+5694)(?:\s|$)'})
if($active.Count){
    if($o.phase -ceq 'user-review-preserve'){Write-Output 'The review device is already running. Its current play/save is preserved.';exit 0}
    throw 'Review device already active; inspect ownership before repeating setup'
}
if($o.phase -notin @('boot-failed-no-space','review-ready-not-launched')){throw 'Unexpected review phase; do not overwrite a later user save'}
if((Get-PSDrive C).Free -lt 8200000000){throw 'Review device needs8.2GB free. Do not remove other emulators or user saves.'}
$items=@(Get-Item -LiteralPath $expectedRoot -Force)+@(Get-ChildItem -LiteralPath $expectedRoot -Recurse -Force)
if(@($items|Where-Object{$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Reparse point in dedicated device root'}
$env:JAVA_HOME='C:/dev/rea_project/realm_defense_work/tools/jdk21/jdk-21.0.12.1+1'
$env:ANDROID_SDK_ROOT=$sdk;$env:ANDROID_AVD_HOME=Join-Path $expectedRoot avd-home
$env:ANDROID_USER_HOME=Join-Path $expectedRoot user;$env:ANDROID_EMULATOR_HOME=Join-Path $expectedRoot emulator-home
$stamp=Get-Date -Format yyyyMMdd-HHmmss
# Visible window is intentional: the user invoked this script to compare and play the UI.
$p=Start-Process -FilePath (Join-Path $sdk emulator/emulator.exe) -ArgumentList @('-avd','GatefallSoftUIV2811','-port','5694','-gpu','host','-memory','2560','-cores','4','-no-snapshot','-no-boot-anim','-skin','2400x1080') -WindowStyle Normal -PassThru -RedirectStandardOutput (Join-Path $expectedRoot "review-$stamp-stdout.log") -RedirectStandardError (Join-Path $expectedRoot "review-$stamp-stderr.log")
$o.launcherPid=$p.Id;$o.phase='review-starting';$o|ConvertTo-Json|Set-Content -LiteralPath $ownerFile -Encoding utf8
$ready=$false
for($attempt=0;$attempt -lt 60;$attempt++){
    Start-Sleep -Seconds 2
    $state=(& $adb -s $o.serial get-state 2>$null|Out-String).Trim()
    if($LASTEXITCODE -eq 0 -and $state -ceq 'device'){
        $boot=(& $adb -s $o.serial shell getprop sys.boot_completed 2>$null|Out-String).Trim()
        if($LASTEXITCODE -eq 0 -and $boot -ceq '1'){$ready=$true;break}
    }
    Write-Output 'Waiting for the separate UI review emulator...'
}
if(-not $ready){throw 'Review boot not confirmed. Do not create another device; inspect this owner/log.'}
$qemu=@(Get-CimInstance Win32_Process|Where-Object{$_.Name -match '^qemu-system-x86_64(?:-headless)?\.exe$' -and $_.ParentProcessId -eq $p.Id -and $_.CommandLine -match '-avd GatefallSoftUIV2811 -port 5694'})
if($qemu.Count -ne 1){throw 'Review QEMU ownership ambiguous'}
$o.qemuPid=$qemu[0].ProcessId
& $adb -s $o.serial install -r $apk
if($LASTEXITCODE -ne 0){throw 'Separate candidate install failed'}
& $adb -s $o.serial shell am start -n com.gatefall.prototype/com.unity3d.player.UnityPlayerActivity|Out-Null
if($LASTEXITCODE -ne 0){throw 'Initial candidate launch failed'}
Start-Sleep -Seconds 8
& $adb -s $o.serial shell am force-stop com.gatefall.prototype
if($LASTEXITCODE -ne 0){throw 'Initial review app stop failed'}
$profile=Join-Path $project 'TestResults/hud-investment-20261106/profileC-cold.sav'
if((Get-FileHash $profile).Hash.ToLowerInvariant() -cne '082a90aa86a2e676aa6f904574a2522a309d638e04a4e0c8a99a388819eb8b5f'){throw 'Verified synthetic profile changed'}
& $adb -s $o.serial push $profile /sdcard/Android/data/com.gatefall.prototype/files/raon-profile.sav
if($LASTEXITCODE -ne 0){throw 'Separate synthetic profile copy failed'}
& $adb -s $o.serial shell am start -n com.gatefall.prototype/com.unity3d.player.UnityPlayerActivity|Out-Null
if($LASTEXITCODE -ne 0){throw 'Review game launch failed'}
Start-Sleep -Seconds 8
$o.appPid=[int]((& $adb -s $o.serial shell pidof com.gatefall.prototype|Out-String).Trim())
$o.phase='user-review-preserve'
$o|Add-Member version '0.28.11' -Force
$o|Add-Member note 'Visible user review of unverified UI candidate;preserve subsequent user save/input.No automatic QA/cleanup.' -Force
$o|ConvertTo-Json|Set-Content -LiteralPath $ownerFile -Encoding utf8
[ordered]@{version='0.28.11';sha256=$apkHash;serial=$o.serial;launcherPid=$o.launcherPid;qemuPid=$o.qemuPid;appPid=$o.appPid;started=(Get-Date -Format o);scope='Candidate installed and launched;this script does not assert visual/input/save validation'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $record manual-review-launch.json) -Encoding utf8
Write-Output 'Gatefall0.28.11 is open in GatefallSoftUIV2811:5694. Existing5692 is unchanged.'
