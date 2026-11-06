# Audit the built UI candidate only. Native inspection remains explicitly pending.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/soft-ui-20261106'
function Require($value,[string]$why){if(-not $value){throw $why}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()}
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -ceq '0.28.11' -and $apk.versionCode -eq 128 -and $apk.bytes -eq 100464605 -and $apk.sha256 -ceq '1b76590321a5d910a5e09692df9de10a2133d1e70c3cccbc25cdb5ce6dd2e4ce') 'Candidate metadata differs'
Require ((Hash 'Builds/Android/Gatefall-v0.28.11.apk') -ceq $apk.sha256 -and $apk.signature -ceq 'verified' -and $apk.alignment16KB -ceq 'verified') 'Candidate package differs'
Require ((Hash 'Builds/Android/Gatefall.apk') -ceq '12b49530f1e339c6ca698cb4e95665cd103c2c27dc6e3c7ffe9bb7939d5e1330' -and (Hash 'Builds/Android/Gatefall-v0.28.10.apk') -ceq '12b49530f1e339c6ca698cb4e95665cd103c2c27dc6e3c7ffe9bb7939d5e1330') 'Stable alias not preserved'
$elf=Get-Content "$root/elf-v02811.json" -Raw|ConvertFrom-Json
Require ($elf.elfLoad16KB -and $elf.nativeLibraries.Count -eq 3 -and $elf.sha256 -ceq $apk.sha256) 'Native library alignment incomplete'
$core=@(Get-Content TestResults/hud-investment-20261106/product-hashes-v02810.json -Raw|ConvertFrom-Json|Where-Object{$_.file -match 'Scripts[\\/]Core[\\/]'})
Require ($core.Count -eq 6) 'Expected six core files'
foreach($file in $core){Require ((Hash $file.file) -ceq $file.sha256) ('Combat/input/save core changed '+$file.file)}
foreach($name in @('degenerate-strategy-review','competent-plan-review')){$gate=(Get-Content ('TestResults/'+$name+'.json') -Raw|ConvertFrom-Json).gate;Require ($gate.enforced -and @($gate.violations).Count -eq 0) ('Strategy gate differs '+$name)}
$font=Get-Content "$root/skin-font-audit.json" -Raw|ConvertFrom-Json
Require ($font.surfaces -eq 11 -and $font.partialAlphaPixels -gt 0 -and $font.chapterWidth -le 236 -and $font.pageWidth -le 150) 'Skin/font bounds failed'
$build=Get-Content "$root/build-v02811.log" -Raw
Require ($build.Contains('Unity validation completed.') -and $build.Contains('ANDROID DELIVERY COMPLETE:') -and $build.Contains('PASS: 87 C# files') -and $build.Contains('PASS: 32 mobile input/layout checks.')) 'Build/regression evidence incomplete'
$owner=Get-Content TestResults/soft-ui-v02811-owned-avd.json -Raw|ConvertFrom-Json
Require ($owner.name -ceq 'GatefallSoftUIV2811' -and $owner.serial -ceq 'emulator-5694' -and $owner.phase -in @('boot-failed-no-space','review-ready-not-launched')) 'Native review progressed; replace candidate audit with real delivery audit'
foreach($script in @('Tools/open-soft-ui-review.ps1','Tools/verify-soft-ui-candidate.ps1')){$tokens=$null;$errors=$null;[Management.Automation.Language.Parser]::ParseFile((Join-Path $project $script),[ref]$tokens,[ref]$errors)|Out-Null;Require ($errors.Count -eq 0) ('Script syntax failed '+$script)}
$files=@(Get-ChildItem Assets/Scripts/Core,Assets/Scripts/Presentation -Filter '*.cs')+@(Get-Item Assets/Editor/PrototypeTools.cs,ProjectSettings/ProjectSettings.asset)
$files|ForEach-Object{[ordered]@{file=$_.FullName;sha256=(Hash $_.FullName)}}|ConvertTo-Json|Set-Content "$root/product-candidate-hashes.json" -Encoding utf8
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;candidateOnly=$true;stableAliasVersion='0.28.10';coreFilesIdentical=6;proceduralSkins=11;unityAndroidBuilt=$true;signatureZIP16KB=$true;elf16KBLibraries=3;strategyGateViolations=0;nativeInstalled=$false;nativeScreensVerified=$false;nativeSaveCompatibilityVerified=$false;manualReviewScriptSyntax=$true;manualReviewScriptExecuted=$false;commitPushed=$false;preview='Docs/QA/v050/soft-ui-components-preview.png';previewSha256=(Hash 'Docs/QA/v050/soft-ui-components-preview.png');blocker='Automatic approval review service usage limit prevented independent emulator restart;action not executed. No bypass. Original user5692 preserved.';scope='Unity-built UI candidate/static original skin proof only. No new Android battle, native visual/input/save verification, human fun or physical Android/iOS claim.'}|ConvertTo-Json -Depth 5|Set-Content "$root/candidate-v02811.json" -Encoding utf8
'PASS built UI candidate/core/package/font/regression/stable-preservation;Native review remains blocked and unverified'
