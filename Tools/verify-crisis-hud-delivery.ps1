# Recorded HUD/package/save audit only; never sends device input or repeats a battle.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
function Require($value,[string]$why){if(-not $value){throw $why}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
foreach($row in (Get-Content "$root/product-hashes-v0283.json" -Raw|ConvertFrom-Json)) {
    Require ((Hash $row.file) -ceq $row.sha256) ('Product differs: '+$row.file)
}
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -eq '0.28.3' -and $apk.versionCode -eq 120 -and $apk.sha256 -ceq '6502c612ff4575650161f11d5e27ed4379af2289d57dbf74da6b2592721c8dbf') 'Wrong final candidate'
foreach($file in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.28.3.apk',"$root/installed-v0283.apk")) {
    Require ((Hash $file) -ceq $apk.sha256) ('APK mismatch: '+$file)
}
Require ((Hash 'Builds/Android/Gatefall-v0.28.2.apk') -ceq 'bad2563868dd9a05b4c145a4021ca2bfac84695c012a5132514f3aa2e5e53e12') 'Stable previous delivery changed'
$elf=Get-Content "$root/elf-v0283.json" -Raw|ConvertFrom-Json
Require ($apk.signature -eq 'verified' -and $apk.alignment16KB -eq 'verified' -and $elf.elfLoad16KB -and $elf.sha256 -ceq $apk.sha256) 'Package verification incomplete'
foreach($row in (Get-Content "$root/core-before-v0283.json" -Raw|ConvertFrom-Json)) {
    Require ((Hash $row.file) -ceq $row.sha256) ('Combat core changed: '+$row.file)
}
$font=Get-Content TestResults/battle-mode-font.json -Raw|ConvertFrom-Json
Require ($font.version -eq '0.28.3' -and $font.labels[1] -ceq '위기 · 도시 결계' -and
    ($font.widths|Measure-Object -Maximum).Maximum -le 118.5 -and ($font.heights|Measure-Object -Maximum).Maximum -le 18) 'Mode font overflow/missing measurement'
$observed=Get-Content "$root/hud-observations-v0283.json" -Raw|ConvertFrom-Json
Require ($observed.version -eq '0.28.3' -and $observed.finalCandidate -and $observed.readyScreens.Count -eq 6 -and $observed.liveCases.Count -eq 2) 'Final native observation scope missing'
foreach($screen in $observed.readyScreens) {
    Require ($screen.visuallyReviewed -and (Hash $screen.file) -ceq $screen.sha256) 'Ready screenshot not reviewed/preserved'
}
foreach($case in $observed.liveCases) {
    Require ($case.elapsedA -eq '0:05' -and $case.elapsedB -eq '0:10' -and $case.pauseReviewed -and $case.abandoned) 'Short LIVE/pause check incomplete'
}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
function Profile([string]$file){[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/$file")))}
$before=Profile update-before-v0283.sav
Require ($before.MagicCores -eq 162 -and $before.StarsTotal -eq 33 -and $before.CrisisStarsTotal -eq 3 -and -not $before.FirstCleared[11]) 'Synthetic reference progress differs'
foreach($file in @('update-after-v0283.sav','candidate0-abandon-v0283.sav','final-update-before-v0283.sav','final-update-after-v0283.sav',
    'final-normal-abandon-v0283.sav','final-crisis-abandon-v0283.sav','relaunch-v0283.sav')) {
    Require ((Hash "$root/$file") -ceq (Hash "$root/update-before-v0283.sav")) ('Update/partial run/relaunch changed account: '+$file)
}
$normal=Profile final-normal-pending-v0283.sav; $crisis=Profile final-crisis-pending-v0283.sav
Require ($normal.PendingStage -eq 11 -and -not $normal.PendingCrisis -and $normal.PendingRun -ne '' -and
    $crisis.PendingStage -eq 11 -and $crisis.PendingCrisis -and $crisis.PendingRun -ne '') 'Actual native modes were not normal/crisis1-12'
foreach($pending in @($normal,$crisis)) {
    $expected=$before.Copy();$expected.PendingStage=11;$expected.PendingRun=$pending.PendingRun;$expected.PendingCrisis=$pending.PendingCrisis
    Require ([Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($expected)) -ceq
        [Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($pending))) 'Unexpected preparation save mutation'
}
foreach($name in @('competent-plan-review','degenerate-strategy-review')) {
    $gate=(Get-Content ('TestResults/'+$name+'.json') -Raw|ConvertFrom-Json).gate
    Require ($gate.enforced -and @($gate.violations).Count -eq 0) ('Strategy gate failed: '+$name)
}
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;readyScreens=6;liveModes=@('Normal','Crisis');
    elapsedLabels=@('0:05','0:10');stage='1-12';core=300;towers=0;spent=0;newReward=0;
    unchangedCores=6;updateSequence='0.28.2 -> unreleased0.28.3candidate0 -> final0.28.3';
    saveBytes=(Get-Item "$root/relaunch-v0283.sav").Length;saveHash=(Hash "$root/relaunch-v0283.sav");cores=162;normalStars=33;crisisStars=3;
    growth=$before.Growth;research=$before.Research;relaunchSame=$true;
    scope='Final native checks cover Ready at3 resolutions and roughly10s LIVE/pause in2 modes, normal abandonment and cold reload. No new full battle, human fun, physical Android, iOS, audio or performance benchmark. Candidate0 dark Threat text is separate from final Amber text.'
} | ConvertTo-Json -Depth 6 | Set-Content "$root/delivery-v0283.json"
'PASS0.28.3: final installed package/16KB,6 unchanged cores,2 modes/3 Ready sizes/short LIVE, normal abandon and unchanged cold save'
