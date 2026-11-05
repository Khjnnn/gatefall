$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$manifest=@(Get-Content TestResults/source-hashes-v0230.json -Raw|ConvertFrom-Json)
foreach($r in $manifest){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Verified current product changed; do not overwrite'}}
foreach($path in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.23.0.apk')){if((Get-FileHash $path).Hash -ne 'b4d4873174a3d16226a3909e05aaac3429246f51e3ab700706a3f8db293943e2'){throw 'Verified APK fallback differs'}}
$selected='TestResults/courts-heavy-v0230-speed115-review/candidate-core'
$snapshot='TestResults/courts-growth-v0240-baseline'
if(Test-Path $snapshot){throw 'Preserve existing preparation; inspect current progress before retry'}
$settings=Get-Content ProjectSettings/ProjectSettings.asset -Raw
foreach($needle in @('  bundleVersion: 0.23.0','  AndroidBundleVersionCode: 70')){if(([regex]::Matches($settings,[regex]::Escape($needle))).Count -ne 1){throw 'Version anchor differs'}}
foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){
    $candidate=Join-Path $selected $file.Name
    if($file.Name -notin @('DefenseSimulation.cs','RaonCombat.cs') -and (Get-FileHash $file.FullName).Hash -ne (Get-FileHash $candidate).Hash){throw 'Selected snapshot has unrelated core edits'}
}
$model=Get-Content TestResults/courts-heavy-v0230-comparison.json -Raw|ConvertFrom-Json
if($model.selectedSpeedMultiplier -ne 1.15 -or $model.selectedPackSpan -ne 10 -or $model.newAndroidVerified){throw 'Selected pre-product model record differs'}
New-Item -ItemType Directory -Path $snapshot|Out-Null
foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $file.FullName -Destination $snapshot}
Copy-Item ProjectSettings/ProjectSettings.asset (Join-Path $snapshot ProjectSettings.asset)
Copy-Item TestResults/source-hashes-v0230.json (Join-Path $snapshot source-hashes-v0230.json)
Copy-Item -LiteralPath (Join-Path $selected DefenseSimulation.cs) -Destination Assets/Scripts/Core/DefenseSimulation.cs
Copy-Item -LiteralPath (Join-Path $selected RaonCombat.cs) -Destination Assets/Scripts/Core/RaonCombat.cs
$settings=$settings.Replace('  bundleVersion: 0.23.0','  bundleVersion: 0.24.0').Replace('  AndroidBundleVersionCode: 70','  AndroidBundleVersionCode: 71')
Set-Content -LiteralPath ProjectSettings/ProjectSettings.asset -Value $settings -NoNewline
$now=@(foreach($r in $manifest){[ordered]@{path=$r.path;sha256=(Get-FileHash $r.path).Hash.ToLowerInvariant()}})
$changed=@(foreach($r in $now){$old=@($manifest|Where-Object path -eq $r.path)[0];if($old.sha256 -ne $r.sha256){$r.path}})
if(@(Compare-Object $changed @('Assets\Scripts\Core\DefenseSimulation.cs','Assets\Scripts\Core\RaonCombat.cs','ProjectSettings\ProjectSettings.asset')).Count){throw 'Applied source scope differs; do not build'}
$now|ConvertTo-Json|Set-Content TestResults/source-hashes-v0240-candidate.json
[ordered]@{created=(Get-Date -Format o);task=251;candidateVersion='0.24.0';candidateCode=71;changedProductFiles=$changed;other17FilesUnchanged=$true;fallbackVersion='0.23.0';fallbackSha256='b4d4873174a3d16226a3909e05aaac3429246f51e3ab700706a3f8db293943e2';selectedSnapshot=$selected;newAndroidVerified=$false;newApkBuilt=$false;selectedScope='Original1-5 map;normal late triple-pack span10 and existing19 heavy enemies speed1.15;initial3/other13/crisis/HP/count/economy/save unchanged'}|ConvertTo-Json -Depth 5|Set-Content TestResults/v0240-selected-product-audit.json
'Applied candidate0.24.0/code71: selected Raon/Defense plus version only. Verified0.23.0 APK preserved; build/Android validation pending.'
