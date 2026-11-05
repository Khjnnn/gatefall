param([string]$EditorPath = 'C:\Program Files\Unity\Hub\Editor\6000.6.7f1\Editor\Unity.exe')
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskPowerShell = (Get-Process -Id $PID).Path
$taskApk = Join-Path $taskProject 'Builds\Android\Gatefall.apk'
# Preserve the previously delivered APK before Unity writes its output path.
if (Test-Path -LiteralPath $taskApk) {
    $taskPreviousHash = (Get-FileHash -LiteralPath $taskApk -Algorithm SHA256).Hash.ToLowerInvariant()
    $taskBackupDir = Join-Path $taskProject 'Backups\android-deliveries'
    New-Item -ItemType Directory -Path $taskBackupDir -Force | Out-Null
    $taskPreviousApk = Join-Path $taskBackupDir ('Gatefall-' + $taskPreviousHash + '.apk')
    if (-not (Test-Path -LiteralPath $taskPreviousApk)) { Copy-Item -LiteralPath $taskApk -Destination $taskPreviousApk }
    if ((Get-FileHash -LiteralPath $taskPreviousApk -Algorithm SHA256).Hash -ne $taskPreviousHash) { throw 'Previous APK backup verification failed' }
}
foreach ($taskCheck in @('test-simulation.ps1','test-mobile.ps1','test-campaign.ps1','test-fusion-reachability.ps1','test-active-combat-timing.ps1','../Tests/PerfSummaryChecks.ps1','verify-source.ps1','verify-design.ps1','verify-campaign-runtime.ps1','verify-combat-pace.ps1','verify-tower-density.ps1','verify-four-fronts.ps1','verify-evacuation-route.ps1','verify-alley-readability.ps1')) {
    & $taskPowerShell -NoProfile -File (Join-Path $PSScriptRoot $taskCheck)
    if ($LASTEXITCODE -ne 0) { throw "Check failed: $taskCheck" }
}
$taskSettingsText = Get-Content -LiteralPath (Join-Path $taskProject 'ProjectSettings/ProjectSettings.asset') -Raw
$taskCurrentVersion = [regex]::Match($taskSettingsText, '(?m)^  bundleVersion: (\S+)').Groups[1].Value
& $taskPowerShell -NoProfile -File (Join-Path $PSScriptRoot 'review-swarm.ps1') -Tag ('v' + $taskCurrentVersion.Replace('.',''))
if ($LASTEXITCODE -ne 0) { throw 'Swarm checks failed' }
& $taskPowerShell -NoProfile -File (Join-Path $PSScriptRoot 'review-wave-call.ps1') -ChecksOnly
if ($LASTEXITCODE -ne 0) { throw 'Wave-call checks failed' }
& $taskPowerShell -NoProfile -File (Join-Path $PSScriptRoot 'verify-unity.ps1') -EditorPath $EditorPath -Target Android
if ($LASTEXITCODE -ne 0) { throw 'Unity Android build failed' }
& $taskPowerShell -NoProfile -File (Join-Path $PSScriptRoot 'verify-apk.ps1') -EditorPath $EditorPath
if ($LASTEXITCODE -ne 0) { throw 'APK verification failed' }
$taskReport = Get-Content -LiteralPath (Join-Path $taskProject 'TestResults\android-apk-results.json') -Raw | ConvertFrom-Json
$taskVersioned = Join-Path $taskProject ('Builds\Android\Gatefall-v' + $taskReport.version + '.apk')
Copy-Item -LiteralPath $taskApk -Destination $taskVersioned -Force
Write-Output "ANDROID DELIVERY COMPLETE: $taskVersioned"
Write-Output "APK: $taskApk"
Write-Output 'Install on an Android device or emulator; Windows player packaging is retired.'

