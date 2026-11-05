# Recorded0.28.2 outcome/observability QA. No battle, device input or save mutation occurs here.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location $project
$root='TestResults/night-balance-20261105'
function Require($ok,[string]$why){if(-not $ok){throw $why}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
foreach($r in (Get-Content "$root/product-hashes-v0282.json" -Raw|ConvertFrom-Json)){Require ((Hash $r.file) -ceq $r.sha256) ('Product changed: '+$r.file)}
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -eq '0.28.2' -and $apk.versionCode -eq 119) 'Wrong version'
foreach($file in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.28.2.apk',"$root/installed-v0282.apk")){Require ((Hash $file) -ceq $apk.sha256) ('APK mismatch: '+$file)}
Require ((Hash 'Builds/Android/Gatefall-v0.28.1.apk') -ceq '3d4b3ae16538593f90cf746b4f4917a585ae2b126cf8ac7789d1368027ea6100') 'Previous delivery changed'
$elf=Get-Content "$root/elf-v0282.json" -Raw|ConvertFrom-Json
Require ($apk.signature -eq 'verified' -and $apk.alignment16KB -eq 'verified' -and $elf.elfLoad16KB -and $elf.sha256 -ceq $apk.sha256) 'Package verification incomplete'
Require ((Hash "$root/update-before-v0282.sav") -ceq (Hash "$root/update-after-v0282.sav")) 'Installer changed legacy save'
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
function Profile([string]$file){[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/$file")))}
$before=Profile update-before-v0282.sav;$pending=Profile pending-v0282.sav;$result=Profile result-v0282.sav
Require ($before.PendingCrisis -and $before.PendingRun -eq '' -and $before.PendingStage -eq -1) 'Legacy residual fixture differs'
Require ($pending.PendingCrisis -and $pending.PendingStage -eq 7 -and $pending.PendingRun -ne '') 'Actual native case was not crisis1-8'
$expected=$before.Copy();$expected.LastReward=0;$expected.LastRewardRun=$pending.PendingRun;$expected.PendingCrisis=$false;$expected.TutorialMask=$before.TutorialMask -bor (1 -shl 7)
Require ($pending.TutorialMask -eq $expected.TutorialMask) 'Displayed lesson mask differs'
Require ([Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($expected)) -ceq [Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($result))) 'Unexpected outcome/account changes'
Require ($result.MagicCores -eq 162 -and $result.StarsTotal -eq 33 -and $result.CrisisStarsTotal -eq 3 -and -not $result.PendingCrisis -and $result.PendingRun -eq '' -and $result.PendingStage -eq -1) 'Outcome not fully cleared/preserved'
foreach($file in @('map-v0282.sav','relaunch-v0282.sav')){Require ((Hash "$root/$file") -ceq (Hash "$root/result-v0282.sav")) 'Map/relaunch changed outcome'}
$run=Get-Content "$root/last-run-v0282.txt"
Require ($run[0] -eq 'Gatefall 0.28.2' -and $run[1] -eq 'Lost elapsed=61.99 core=0 hunter=69 kills=57 firstFusion=-1 fusions=0') 'Native failure fixture differs'
Require ($run[2] -eq 'Wave clear=True allSpawned=False spawned=120 remainingEnemies=63 bossDefeated=False') 'Native partial-wave result differs'
Require (@($run|Where-Object {$_ -match '^\d+\.\d+ (build|upgrade|fusion|sell) '}).Count -eq 0) 'Failure fixture spent resources'
$context=@($run|Where-Object {$_ -like 'Context *'})
Require ($context.Count -eq 1 -and $context[0] -eq 'Context stage=1-8 mode=Crisis seed=389538 hunter=0 growth=4 research=3,2,2,2 loadout=135 cards=0,0,0,1,0,0') 'Mode/seed/account/card context missing or wrong'
$replays=@(Get-Content "$root/recorded-seed-replay-v0282.json" -Raw|ConvertFrom-Json)
Require ($replays.Count -eq 2) 'Replay samples incomplete'
foreach($replay in $replays){Require ($replay.seed -eq 389538 -and $replay.state -eq 'Lost' -and [math]::Abs($replay.end-61.99) -le .1 -and $replay.kills -eq 57 -and $replay.spawned -eq 120 -and $replay.remaining -eq 63) 'Seed replay differs beyond selected outcome tolerance'}
$early=Get-Content TestResults/observation-early-v0282-comparison.json -Raw|ConvertFrom-Json
Require ($early.equal -and $early.cases -eq 26) 'Early combat equivalence incomplete'
$late=Get-Content "$root/default-crisis-equivalence-v0282.json" -Raw|ConvertFrom-Json
Require ($late.equal -and $late.cases -eq 12) 'Default late-crisis fixtures changed'
foreach($name in @('competent-plan-review','degenerate-strategy-review')){$g=(Get-Content ("TestResults/"+$name+'.json') -Raw|ConvertFrom-Json).gate;Require ($g.enforced -and @($g.violations).Count -eq 0) ('Strategy gate failed: '+$name)}
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;legacyUpdateBytes=(Get-Item "$root/update-after-v0282.sav").Length;updateSame=$true;
    nativeCase='Crisis1-8 no-build loss: outcome/logging QA, not difficulty or player fun';seconds=61.99;seed=389538;kills=57;spawned=120;remaining=63;card='Range';spent=0;reward=0;
    cores=162;normalStars=33;crisisStars=3;pendingCleared=$true;oldResidualAccepted=$true;saveBytes=(Get-Item "$root/result-v0282.sav").Length;saveHash=(Hash "$root/result-v0282.sav");relaunchSame=$true;
    earlyEquivalent=26;lateEquivalent=12;recordedSeedReplaySteps=@('1/60','1/30');replayToleranceSeconds=.1;
    scope='No combat/balance tuning in0.28.2. Native has HOME focus stops; matching selected seed outcomes is not whole-state/native frame equivalence or performance proof.';
    humanPlay=$false;physicalAndroid=$false;ios=$false}|ConvertTo-Json -Depth 4|Set-Content "$root/delivery-v0282.json"
'PASS0.28.2: package/install/legacy update, native outcome mode cleanup and seed context, no reward/progress loss, relaunch,26early/12late and2recorded-seed replays'
