# QA input helper: only sends ordinary Android touch/key events and captures the screen.
# Coordinates must be chosen from the current observed screen. This does not change game/save state directly.
# Invoke this script directly with & from a caller using $ErrorActionPreference='Stop'.
# If launching child pwsh instead, check its exit code before every dependent step.
# Do not reuse Resume/field coordinates across the30/90/150 card thresholds: inspect the
# foreground screen or stop well before the threshold and advance without field taps.
# A screenshot alone does not pause LIVE combat. For a long review always request
# -BackgroundAfterShot, then foreground/wait2s and inspect actual Pause/card state.
# There is NO universal empty point:2250,400 is inside wide right-hand tower panels.
# Since0.25 Pause is visible inside Socket/Fusion menus and opens Pause directly.
# Older0.24 evidence used hidden Pause; do not rewrite that historical observation.
param(
    [string]$Taps = '',
    [ValidateRange(0,50)][int]$WaitSeconds = 0,
    [string]$Shot = '',
    [switch]$BackgroundAfterShot,
    [string]$Serial = '',
    [string]$Adb = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe'
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($Serial)) { throw 'Explicit QA serial required; this helper never selects a default device.' }
if ($Serial -in @('emulator-5556','emulator-5558')) { throw 'This device and its saves are protected; no QA input was sent.' }
if ($BackgroundAfterShot -and -not $Shot) { throw 'BackgroundAfterShot requires a screenshot so the pre-background state is recorded.' }
. (Join-Path $PSScriptRoot 'qa-device-ready.ps1')
. (Join-Path $PSScriptRoot 'qa-screenshot-transfer.ps1')
Assert-QaAndroidReady -Serial $Serial -Adb $Adb
$taskProject = Split-Path -Parent $PSScriptRoot
$taskEvidence = Join-Path $taskProject 'Docs\QA\v050'
New-Item -ItemType Directory -Path $taskEvidence -Force | Out-Null
foreach ($taskTap in ($Taps -split ';' | Where-Object { $_ })) {
    if ($taskTap -notmatch '^(\d+),(\d+)$') { throw "Invalid tap: $taskTap" }
    & $Adb -s $Serial shell input tap $Matches[1] $Matches[2]
    if ($LASTEXITCODE -ne 0) { throw 'ADB tap failed' }
    Add-Content -LiteralPath (Join-Path $taskEvidence 'actions.log') -Value ((Get-Date).ToString('o') + ' tap ' + $taskTap)
    Start-Sleep -Milliseconds 350
}
if ($WaitSeconds) { Start-Sleep -Seconds $WaitSeconds }
if ($Shot) {
    if ($Shot -notmatch '^[a-zA-Z0-9_-]+$') { throw 'Use a plain screenshot name without a path or extension' }
    & $Adb -s $Serial shell screencap -p /sdcard/gatefall-qa.png
    if ($LASTEXITCODE -ne 0) { throw 'ADB screenshot failed' }
    $taskFile = Join-Path $taskEvidence ($Shot + '.png')
    Receive-QaScreenshot -Serial $Serial -Adb $Adb -Destination $taskFile
    Add-Content -LiteralPath (Join-Path $taskEvidence 'actions.log') -Value ((Get-Date).ToString('o') + ' screenshot ' + $Shot)
}
if ($BackgroundAfterShot) {
    # Long screenshot reviews must not leave a LIVE battle running. Foreground the app explicitly later;
    # Gatefall's existing focus-loss pause preserves the battle and still requires the normal Resume button.
    & $Adb -s $Serial shell input keyevent 3
    if ($LASTEXITCODE -ne 0) { throw 'ADB background request failed' }
    Add-Content -LiteralPath (Join-Path $taskEvidence 'actions.log') -Value ((Get-Date).ToString('o') + ' HOME after screenshot ' + $Shot)
}
