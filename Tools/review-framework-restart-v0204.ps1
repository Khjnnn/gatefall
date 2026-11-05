$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskLogPath = 'TestResults/v0204-north-resume-diagnostic-logcat.txt'
$taskLines = [IO.File]::ReadAllLines((Join-Path (Get-Location) $taskLogPath))
$taskStart = -1
$taskEnd = -1
for ($taskI=0; $taskI -lt $taskLines.Length; $taskI++) {
    if ($taskLines[$taskI] -match 'W Watchdog: \*\*\* WATCHDOG KILLING SYSTEM PROCESS:') { $taskStart=$taskI }
    if ($taskStart -ge 0 -and $taskLines[$taskI] -match 'W Watchdog: \*\*\* GOODBYE!') { $taskEnd=$taskI; break }
}
if ($taskStart -lt 0 -or $taskEnd -lt $taskStart) { throw 'Missing complete watchdog stack' }
$taskStack = $taskLines[$taskStart..$taskEnd]
if (($taskStack -join "`n") -notmatch 'foreground thread \(android.fg\) for 68s' -or
    ($taskStack -join "`n") -notmatch 'ListenerMultiplexer.deliverToListeners' -or
    ($taskStack -join "`n") -notmatch 'waiting to lock <0x0115c7ca>' -or
    ($taskStack -join "`n") -notmatch 'GnssNative.native_stop') { throw 'Restart mechanism differs from recorded stack' }
$taskStack | Set-Content -LiteralPath 'TestResults/v0204-framework-watchdog-stack.txt'
$taskTimeline = @($taskLines | Where-Object { $_ -match 'WAITED_UNTIL_PRE_WATCHDOG|WATCHDOG KILLING SYSTEM PROCESS|Sending signal. PID: 577 SIG: 9|Process 577 exited due to signal 9|boot_progress_system_run' })
$taskTimeline | Set-Content -LiteralPath 'TestResults/v0204-framework-restart-timeline.txt'
$taskSdk = 'C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK'
$taskAapt = Join-Path $taskSdk 'build-tools/36.0.0/aapt.exe'
$taskPermissions = @(& $taskAapt dump permissions 'Builds/Android/Gatefall-v0.20.4.apk')
if ($LASTEXITCODE -ne 0) { throw 'APK permissions inspection failed' }
$taskPermissions | Set-Content -LiteralPath 'TestResults/v0204-apk-permissions.txt'
if (@($taskPermissions | Where-Object { $_ -match 'uses-permission:' }).Count -ne 1 -or
    ($taskPermissions -join "`n") -notmatch 'android.permission.INTERNET' -or
    ($taskPermissions -join "`n") -match 'ACCESS_.*LOCATION') { throw 'APK permissions differ' }
$taskSourceHits = @(Get-ChildItem -LiteralPath 'Assets/Scripts' -Recurse -File -Filter '*.cs' |
    Select-String -Pattern 'Input\.location|LocationService|ACCESS_FINE_LOCATION|ACCESS_COARSE_LOCATION')
if ($taskSourceHits.Count -ne 0) { throw 'Location use requires further review' }
$taskHost = [IO.File]::ReadAllText((Join-Path (Get-Location) 'TestResults/v0204-emulator-out.log'))
$taskColorErrors = ([regex]::Matches($taskHost,'Failed to find ColorBuffer:')).Count
$taskContextErrors = ([regex]::Matches($taskHost,'Failed to restore previous context:')).Count
$taskSourceRecords = Get-Content -LiteralPath 'TestResults/source-hashes-v0204.json' -Raw | ConvertFrom-Json
foreach ($taskR in $taskSourceRecords) { if ((Get-FileHash -LiteralPath $taskR.path).Hash -ne $taskR.sha256) { throw 'Product changed during diagnosis' } }
$taskApkHash = (Get-FileHash -LiteralPath 'Builds/Android/Gatefall-v0.20.4.apk').Hash.ToLowerInvariant()
if ($taskApkHash -ne '140fe4b1cedcb804776f7c267c74d587758dbadd3cf58c0d29c1acf60e8d1bfd' -or $taskColorErrors -ne 4) { throw 'Artifact or host diagnostic changed' }
[ordered]@{
    created=(Get-Date -Format o); task=199; version='0.20.4'; apkSha256=$taskApkHash
    rawLog=$taskLogPath; rawSha256=(Get-FileHash -LiteralPath $taskLogPath).Hash.ToLowerInvariant()
    watchdogStackStartLine=$taskStart+1; watchdogStackEndLine=$taskEnd+1
    restartMechanism='Android watchdog killed system_server PID577 after android.fg was blocked for 68 seconds'
    blockedStack='GNSS native_stop -> reportStatus -> GnssStatusProvider -> ListenerMultiplexer.deliverToListeners; waiting for Object 0x0115c7ca'
    upstreamLockOwnerKnown=$false; triggeringClientKnown=$false; gameCausalityEstablished=$false
    sourceLocationApiMatches=$taskSourceHits.Count; packagedPermissions=@('android.permission.INTERNET')
    absenceOfLocationPermissionDoesNotProveNoIndirectInteraction=$true
    emulator='37.2.12.0 build16428233'; systemImage='android-35/google_apis_playstore/x86_64'
    graphicsBackend='gfxstream'; selectedVulkanGpu='AMD Radeon RX 9060 XT'; hypervisor='WHPX'
    untimestampedHostColorBufferErrors=$taskColorErrors; untimestampedHostContextRestoreErrors=$taskContextErrors
    hostGraphicsCausalityEstablished=$false
    noNewControlAvd='Existing explicit watchdog stack identifies the immediate restart mechanism; no matched control was run and the upstream trigger remains unresolved'
    productSourcesVerifiedUnchanged=$taskSourceRecords.Count; newBuild=$false; newBattle=$false; newAvd=$false
    physicalDeviceImpactKnown=$false; stabilityResolved=$false
    cleanup=(Get-Content -LiteralPath 'TestResults/qa-cleanup-v0204.json' -Raw | ConvertFrom-Json)
    passed=$true
} | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath 'TestResults/framework-restart-v0204-review.json'
Write-Output 'PASS: recorded watchdog mechanism/stack, APK permission and source scope, unchanged product, unresolved upstream trigger'
