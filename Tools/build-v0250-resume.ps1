$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$runtime=(Get-Process -Id $PID).Path
if(-not(Get-Content TestResults/build-v0250-preflight-failed.txt -Raw).Contains('PASS: 106 legacy model')){throw 'Prior passing regression evidence missing'}
foreach($check in @('verify-tower-density.ps1','verify-four-fronts.ps1','verify-evacuation-route.ps1','verify-alley-readability.ps1')){& $runtime -NoProfile -File (Join-Path $PSScriptRoot $check);if($LASTEXITCODE){throw ('Remaining check failed: '+$check)}}
& $runtime -NoProfile -File ./Tools/review-swarm.ps1 -Tag v0250
if($LASTEXITCODE){throw 'Swarm checks failed'}
& $runtime -NoProfile -File ./Tools/review-wave-call.ps1 -ChecksOnly
if($LASTEXITCODE){throw 'Wave-call checks failed'}
$paths=@((Get-Content TestResults/source-hashes-v0243.json -Raw|ConvertFrom-Json).path)+@('Assets/Scripts/Presentation/GatefallGame.cs','Assets/Scripts/Presentation/ArenaView.CombatEffects.cs')
$paths|ForEach-Object {[ordered]@{path=$_;sha256=(Get-FileHash $_).Hash.ToLowerInvariant()}}|ConvertTo-Json -Depth 4|Set-Content TestResults/source-hashes-v0250-candidate.json
& $runtime -NoProfile -File ./Tools/verify-unity.ps1 -Target Android
if($LASTEXITCODE){throw 'Unity Android failed'}
& $runtime -NoProfile -File ./Tools/verify-apk.ps1
if($LASTEXITCODE){throw 'APK verification failed'}
Copy-Item Builds/Android/Gatefall.apk Builds/Android/Gatefall-v0.25.0.apk -Force
Copy-Item TestResults/android-apk-results.json TestResults/android-apk-v0250-candidate.json
'ANDROID DELIVERY COMPLETE:v0.25.0/code75'
