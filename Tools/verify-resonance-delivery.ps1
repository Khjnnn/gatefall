# Audit preserved release evidence only; no new device input, battle or account write.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
function Require($ok,[string]$why){if(-not $ok){throw $why}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
foreach($file in (Get-Content "$root/product-hashes-v0284.json" -Raw|ConvertFrom-Json)){Require ((Hash $file.file) -ceq $file.sha256) ('Source changed: '+$file.file)}
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -eq '0.28.4' -and $apk.versionCode -eq 121 -and $apk.sha256 -ceq '12d801bffb65c7c65168499e7e112d7b2faf6352152074a567baec6008b32806') 'Wrong delivered package'
foreach($file in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.28.4.apk',"$root/installed-v0284.apk")){Require ((Hash $file) -ceq $apk.sha256) ('APK differs: '+$file)}
Require ((Hash 'Builds/Android/Gatefall-v0.28.3.apk') -ceq '6502c612ff4575650161f11d5e27ed4379af2289d57dbf74da6b2592721c8dbf') 'Previous stable package changed'
$elf=Get-Content "$root/elf-v0284.json" -Raw|ConvertFrom-Json
Require ($apk.signature -eq 'verified' -and $apk.alignment16KB -eq 'verified' -and $elf.elfLoad16KB -and $elf.sha256 -ceq $apk.sha256) 'Signature/ZIP/ELF check incomplete'
$delta=@(Get-Content "$root/core-delta-v0284.json" -Raw|ConvertFrom-Json)
Require ($delta.Count -eq 6 -and @($delta|Where-Object same).Count -eq 5 -and @($delta|Where-Object{-not $_.same})[0].file -eq 'RaonCombat.cs') 'Unexpected combat source changes'
$stats=Get-Content "$root/resonance-stat-comparison.json" -Raw|ConvertFrom-Json
Require ($stats.exactlyEqual -and $stats.cases -eq 2646 -and $stats.hash -ceq '2fa7e7e81b17ed46d5fa9aacb2a6f6539df83cf9e892a806aecb82d111d371da') 'Full stat comparison incomplete'
foreach($kind in @('Autocannon','Binding','Rupture','Barrier')){
    $before=@($stats.before|Where-Object Kind -eq $kind)[0];$after=@($stats.candidate|Where-Object Kind -eq $kind)[0]
    Require ($before.Calls -eq $after.Calls -and $after.Bytes -lt $before.Bytes*.55 -and $before.Checksum -eq $after.Checksum) ('Allocation/result mismatch: '+$kind)
}
$models=Get-Content "$root/resonance-model-equivalence-v0284.json" -Raw|ConvertFrom-Json
Require ($models.earlyEqual -and $models.earlyCases -eq 26 -and $models.selectedCrisisEqual -and $models.selectedCrisisCases -eq 3 -and $models.selectedNormalEqual -and $models.selectedNormalCases -eq 2) 'Gameplay equivalence incomplete'
$native=Get-Content "$root/resonance-native-observations-v0284.json" -Raw|ConvertFrom-Json
Require ($native.version -eq '0.28.4' -and $native.towers -eq 6 -and $native.spent -eq 385 -and $native.card -eq 'Range' -and $native.pauseLabel -eq '0:49' -and -not $native.fullBattle) 'Native observation scope differs'
foreach($screen in $native.screenEvidence){Require ((Hash $screen.file) -ceq $screen.sha256) 'Native screenshot changed/missing'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
function Profile([string]$file){[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/$file")))}
$before=Profile update-before-v0284.sav;$pending=Profile pending-v0284.sav
Require ($before.MagicCores -eq 162 -and $before.StarsTotal -eq 33 -and $before.CrisisStarsTotal -eq 3 -and -not $before.FirstCleared[11]) 'Synthetic reference progress differs'
Require ($pending.PendingStage -eq 11 -and -not $pending.PendingCrisis -and $pending.PendingRun -ne '') 'Native case was not normal1-12'
$expected=$before.Copy();$expected.PendingRun=$pending.PendingRun;$expected.PendingStage=11
Require ([Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($expected)) -ceq [Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($pending))) 'Preparation changed unrelated profile fields'
foreach($file in @('update-after-v0284.sav','abandon-v0284.sav','relaunch-v0284.sav')){Require ((Hash "$root/$file") -ceq (Hash "$root/update-before-v0284.sav")) ('Update/partial run/relaunch changed account: '+$file)}
foreach($name in @('competent-plan-review','degenerate-strategy-review')){$g=(Get-Content ('TestResults/'+$name+'.json') -Raw|ConvertFrom-Json).gate;Require ($g.enforced -and @($g.violations).Count -eq 0) ('Strategy gate failed: '+$name)}
$cleanup=Get-Content "$root/cleanup-v0284.json" -Raw|ConvertFrom-Json
Require ($cleanup.normalRestored -and $cleanup.normalShutdown -and $cleanup.remainingOwnedProcesses -eq 0 -and $cleanup.reparsePoints -eq 0) 'Owned emulator cleanup incomplete'
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;statCases=2646;statHash=$stats.hash;exactStatEquality=$true;
    allocationCallsPerKind=20000;allocationBytesBefore=2560064;allocationBytesAfter=1280064;allocationScope='Host .NET warmed same-family resonant stats query; not Android frame/GC improvement';
    earlyEquivalent=26;lateEquivalent=5;nativeStage='Normal1-12';nativeLastLive='0:47';nativePause='0:49';towers=6;spent=385;card='Range';newReward=0;core=300;
    nativeShots=$native.nativeShots;nativeHits=$native.nativeHits;saveBytes=(Get-Item "$root/relaunch-v0284.sav").Length;saveHash=(Hash "$root/relaunch-v0284.sav");updateSame=$true;relaunchSame=$true;
    scope='Native partial stat/upgrade/resonance/card/LIVE/shooting check, no new whole-battle victory or physical/human/iOS/audio/performance claim.'
}|ConvertTo-Json -Depth 6|Set-Content "$root/delivery-v0284.json"
'PASS0.28.4:2646 exact stat cases, reduced host allocation,26early+5late equivalence,package/install16KB,native partial shooting/card/upgrades and unchanged cold save'
