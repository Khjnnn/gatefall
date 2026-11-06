param(
    [Parameter(Mandatory = $true)][string]$EditorPath,
    [switch]$Build,
    [ValidateSet('Validate','Mobile','Preview','Android','iOS')][string]$Target = 'Validate'
)
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
if (-not (Test-Path -LiteralPath $EditorPath -PathType Leaf)) { throw "Unity.exe not found: $EditorPath" }
$taskResults = Join-Path $taskProject 'TestResults'
New-Item -ItemType Directory -Path $taskResults -Force | Out-Null
$taskLog = Join-Path $taskResults ('unity-' + $Target.ToLowerInvariant() + '.log')
$taskMethod = if ($Build) { 'Gatefall.Editor.PrototypeTools.BuildWindows' } else { 'Gatefall.Editor.PrototypeTools.ValidateProject' }
switch ($Target) {
    'Mobile' { $taskMethod = 'Gatefall.Editor.PrototypeTools.PrepareMobileProject' }
    'Preview' { $taskMethod = 'Gatefall.Editor.PrototypeTools.BuildMobilePreview' }
    'Android' { $taskMethod = 'Gatefall.Editor.PrototypeTools.BuildAndroidApk' }
    'iOS' { $taskMethod = 'Gatefall.Editor.PrototypeTools.ExportIos' }
}
# Unity must already be installed and licensed. Close this project in the Editor first.
$taskArguments = '-batchmode -quit -projectPath "{0}" -executeMethod {1} -logFile "{2}"' -f $taskProject, $taskMethod, $taskLog
if ($Target -eq 'Android') { $taskArguments += ' -buildTarget Android' }
if ($Target -eq 'iOS') { $taskArguments += ' -buildTarget iOS' }
if ($Target -eq 'Preview' -or $Build) { $taskArguments += ' -buildTarget Win64' }
$taskProcess = Start-Process -FilePath $EditorPath -ArgumentList $taskArguments -WindowStyle Hidden -PassThru
# A stalled license service must not leave an unattended Editor holding the project lock.
$taskTimeoutMinutes = if ($Target -eq 'Android') { 25 } else { 12 }
if (-not $taskProcess.WaitForExit($taskTimeoutMinutes * 60000)) {
    $taskProcess.Kill()
    throw "Unity timed out after $taskTimeoutMinutes minutes. See $taskLog"
}
if ($taskProcess.ExitCode -ne 0) { throw "Unity verification failed (exit $($taskProcess.ExitCode)). See $taskLog" }
$taskOutput = Get-Content -LiteralPath $taskLog -Raw
if($taskOutput -match 'error CS\d+|Destroy may not be called from edit mode') { throw "Unity compile/editor cleanup errors found. See $taskLog" }
if (-not $taskOutput.Contains('GATEFALL VALIDATION PASSED')) { throw "Unity did not report completed validation. See $taskLog" }
if (($Build -or $Target -in @('Preview','Android','iOS')) -and -not $taskOutput.Contains('GATEFALL BUILD:')) { throw "Unity did not report a completed build. See $taskLog" }
Write-Output "Unity validation completed. Log: $taskLog"
