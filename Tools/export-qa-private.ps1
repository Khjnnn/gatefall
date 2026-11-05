# Binary-safe extraction from an explicitly owned test app; never writes device state.
# Invoke directly with & and ErrorActionPreference=Stop, or explicitly check child pwsh's
# exit code before updating an APK. A failed "before" extraction must never be recreated afterward.
param(
    [Parameter(Mandatory=$true)][string]$Serial,
    [ValidateSet('raon-profile.sav','last-run.txt')][string]$File='raon-profile.sav',
    [Parameter(Mandatory=$true)][string]$Destination
)
$ErrorActionPreference='Stop'
if ($Serial -in @('emulator-5556','emulator-5558') -or $Serial -notmatch '^emulator-[0-9]+$') { throw 'Explicit non-protected QA emulator required' }
$taskProject=Split-Path -Parent $PSScriptRoot
$taskOwner=@(Get-ChildItem -LiteralPath (Join-Path $taskProject 'TestResults') -Filter '*owned-avd.json' -File | ForEach-Object {
    $taskRecord=Get-Content -LiteralPath $_.FullName -Raw | ConvertFrom-Json
    if ($taskRecord.serial -eq $Serial -and $taskRecord.phase -notin @('complete','deleted','delayed-log-removal')) { $taskRecord }
})
if ($taskOwner.Count -ne 1) { throw 'No unique active owned QA device record' }
$taskDestination=[IO.Path]::GetFullPath((Join-Path $taskProject $Destination))
$taskEvidence=[IO.Path]::GetFullPath((Join-Path $taskProject 'TestResults'))+[IO.Path]::DirectorySeparatorChar
if (-not $taskDestination.StartsWith($taskEvidence,[StringComparison]::OrdinalIgnoreCase) -or
    (Test-Path -LiteralPath $taskDestination) -or -not (Test-Path -LiteralPath (Split-Path -Parent $taskDestination))) { throw 'New workspace evidence path required' }
# Unity Application.persistentDataPath on this Android build is app-owned external files,
# not the distinct internal run-as working directory's files folder.
$taskAdb='C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Data/PlaybackEngines/AndroidPlayer/SDK/platform-tools/adb.exe'
. (Join-Path $PSScriptRoot 'qa-device-ready.ps1')
Assert-QaAndroidReady -Serial $Serial -Adb $taskAdb
& $taskAdb -s $Serial pull ('/sdcard/Android/data/com.gatefall.prototype/files/'+$File) $taskDestination
if ($LASTEXITCODE -ne 0) { throw 'QA extraction failed; retained any partial output for diagnosis' }
[pscustomobject]@{serial=$Serial;file=$File;destination=$taskDestination;bytes=(Get-Item -LiteralPath $taskDestination).Length;sha256=(Get-FileHash -LiteralPath $taskDestination).Hash.ToLowerInvariant()} | ConvertTo-Json
