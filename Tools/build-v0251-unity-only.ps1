$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$runtime=(Get-Process -Id $PID).Path
$editor='C:/Program Files/Unity/Hub/Editor/6000.6.7f1/Editor/Unity.exe'
& $runtime -NoProfile -File ./Tools/verify-unity.ps1 -EditorPath $editor -Target Android
if($LASTEXITCODE){throw 'Unity Android failed'}
& $runtime -NoProfile -File ./Tools/verify-apk.ps1
if($LASTEXITCODE){throw 'APK verification failed'}
Copy-Item Builds/Android/Gatefall.apk Builds/Android/Gatefall-v0.25.1.apk -Force
Copy-Item TestResults/android-apk-results.json TestResults/android-apk-v0251-candidate.json
'ANDROID DELIVERY COMPLETE:v0.25.1/code76'
