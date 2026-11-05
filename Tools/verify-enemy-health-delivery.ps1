# Reconcile immutable release/native evidence. No new build, simulation or device input.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
function Require($ok,[string]$why){if(-not $ok){throw $why}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()}
foreach($file in (Get-Content "$root/product-hashes-v0288.json" -Raw|ConvertFrom-Json)){Require ((Hash $file.file) -ceq $file.sha256) ('Changed product '+$file.file)}
$delta=@(Get-Content "$root/product-delta-v0288.json" -Raw|ConvertFrom-Json)
Require (@($delta|Where-Object{-not $_.same}).Count -eq 3 -and @($delta|Where-Object{$_.file -match '[\\/]Core[\\/]' -and -not $_.same}).Count -eq 0) 'Unexpected core/product changes'
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -ceq '0.28.8' -and $apk.versionCode -eq 125 -and $apk.sha256 -ceq '4603a243df9e2432a1666ea6b5a5f244702e3fefa83f23e9b2ef108d700a6685') 'Wrong release'
foreach($name in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.28.8.apk',"$root/installed-v0288.apk")){Require ((Hash $name) -ceq $apk.sha256) ('APK hash differs '+$name)}
Require ((Hash 'Builds/Android/Gatefall-v0.28.7.apk') -ceq '96bd38f3010e92c4b9a9d76995d88b2f4f96c3b4fc8cb6139f0c62d93ae6d32a') 'Prior stable changed'
$elf=Get-Content "$root/elf-v0288.json" -Raw|ConvertFrom-Json
Require ($apk.signature -ceq 'verified' -and $apk.alignment16KB -ceq 'verified' -and $elf.elfLoad16KB -and $elf.sha256 -ceq $apk.sha256) 'Package checks incomplete'
$font=Get-Content TestResults/enemy-health-font-v0288.json -Raw|ConvertFrom-Json
Require ($font.labels -eq 10 -and $font.maxArmoredWidth -le $font.armoredWidth -and $font.maxPlainWidth -le $font.plainWidth -and $font.maxHeight -le 16) 'Actual font clips/wraps'
foreach($name in @('competent-plan-review','degenerate-strategy-review')){$gate=(Get-Content ('TestResults/'+$name+'.json') -Raw|ConvertFrom-Json).gate;Require ($gate.enforced -and @($gate.violations).Count -eq 0) ('Strategy gate failed '+$name)}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$before=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/update-before-v0288.sav")))
Require ($before.Valid() -and $before.MagicCores -eq 214 -and $before.StarsTotal -eq 39 -and $before.Loadout -eq 263 -and ($before.Research -join ',') -ceq '3,2,2,0' -and $before.Growth[0] -eq 5 -and -not $before.FirstCleared[13] -and $before.LastReward -eq 34 -and $before.PendingRun -eq '') 'Synthetic baseline differs'
foreach($name in @('update-after','abandon','cold','abandon-plain','cold-plain')){Require ((Hash "$root/$name-v0288.sav") -ceq (Hash "$root/update-before-v0288.sav")) ('Save changed '+$name)}
foreach($case in @(@{name='pending';stage=13},@{name='pending-plain';stage=5})){
    $path="$root/$($case.name)-v0288.sav";$pending=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project $path)))
    Require ($pending.Valid() -and $pending.PendingStage -eq $case.stage -and $pending.PendingRun -ne '' -and -not $pending.PendingCrisis) 'Native stage/mode differs'
    $expected=$before.Copy();$expected.PendingRun=$pending.PendingRun;$expected.PendingStage=$case.stage
    Require ([Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($expected)) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $project $path)))) 'Native preparation changed unrelated fields'
}
$native=Get-Content "$root/health-native-v0288.json" -Raw|ConvertFrom-Json
Require (-not $native.fullBattle -and $native.newReward -eq 0 -and $native.stage14.towers -eq 6 -and $native.stage14.spent -eq 250 -and $native.stage14.core -eq 282 -and $native.stage6.towers -eq 4 -and $native.stage6.spent -eq 160 -and $native.stage6.observedHP -ceq '253' -and -not $native.stage6.armorSuffix) 'Native scope differs'
foreach($screen in $native.screenEvidence){Require ((Hash $screen.file) -ceq $screen.sha256) 'Missing/changed screenshot'}
$cleanup=Get-Content "$root/cleanup-v0288.json" -Raw|ConvertFrom-Json
Require ($cleanup.normalRestored -and $cleanup.normalShutdown -and $cleanup.remainingOwnedProcesses -eq 0 -and $cleanup.reparsePoints -eq 0) 'Owned QA cleanup incomplete'
$diagnostics=Get-Content "$root/diagnostics-v0288.json" -Raw|ConvertFrom-Json
Require ($diagnostics.finalPids.Count -eq 3 -and @($diagnostics.finalPids|Where-Object{$_.fatalLines -ne 0}).Count -eq 0 -and $diagnostics.retainedPackageAnrLines -eq 0) 'Retained process diagnostics differ'
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;unchangedCoreFiles=6;nativeFullBattle=$false;stage14Hud='1:44';stage14Towers=6;stage14Spend=250;stage14Core=282;stage6Hud='1:11';stage6Towers=4;stage6Spend=160;stage6Core=300;armoredScreens=3;plainHpObserved='253';actualFontCases=10;saveHash=(Hash "$root/cold-plain-v0288.sav");saveBytes=328;updateBothAbandonsColdSame=$true;newReward=0;scope='HP presentation only. Two partial UI tests,not whole battles,human-fun,physical Android/iOS,audio or performance proof. Startup/EGL/Surface root causes remain unresolved.'}|ConvertTo-Json -Depth 5|Set-Content "$root/delivery-v0288.json" -Encoding utf8
'PASS0.28.8 explicitHP/armor qualifier,actual font/3screens/plainHP/menu,6cores unchanged,package/install/save preservation'
