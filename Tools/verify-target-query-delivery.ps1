# Read-only artifact audit. Does not build, replay combat, start a device or write player saves.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
function Require($ok,[string]$why){if(-not $ok){throw $why}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path).Hash.ToLowerInvariant()}
foreach($file in (Get-Content "$root/product-hashes-v0287.json" -Raw|ConvertFrom-Json)){Require ((Hash $file.file) -ceq $file.sha256) ('Changed source '+$file.file)}
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -eq '0.28.7' -and $apk.versionCode -eq 124 -and $apk.sha256 -ceq '96bd38f3010e92c4b9a9d76995d88b2f4f96c3b4fc8cb6139f0c62d93ae6d32a') 'Wrong release'
foreach($file in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.28.7.apk',"$root/installed-v0287.apk")){Require ((Hash $file) -ceq $apk.sha256) ('APK differs '+$file)}
Require ((Hash 'Builds/Android/Gatefall-v0.28.6.apk') -ceq 'acf8bcb4368d02d3677ef3c27ec10a690bdf38f211c9c7799354a297c7dd29cc') 'Previous stable APK changed'
$elf=Get-Content "$root/elf-v0287.json" -Raw|ConvertFrom-Json
Require ($apk.signature -eq 'verified' -and $apk.alignment16KB -eq 'verified' -and $elf.elfLoad16KB -and $elf.sha256 -ceq $apk.sha256) 'Package validation incomplete'
$proof=Get-Content "$root/target-query-allocation-proof-v0287.json" -Raw|ConvertFrom-Json
Require ($proof.beforeBytes[0] -eq 1280000 -and $proof.afterBytes[0] -eq 0 -and $proof.sameTargetOrder -and $proof.oldUnityQueryComparisonNewobj -eq 1 -and $proof.newUnityQueryComparisonNewobj -eq 0 -and -not $proof.unityThreadByteCounterUsable -and $proof.unityGcAllocUnit -ceq 'TimeNanoseconds') 'Allocation evidence or measurement limits differ'
$negative=Get-Content "$root/target-query-negative-control-v0287.json" -Raw|ConvertFrom-Json
Require ($negative.oldCoreRejected -and $negative.language -ceq 'C#9') 'Old comparator negative guard missing'
Require ((Get-Content TestResults/allocation-results.txt -Raw).Contains('4 allocation checks') -and (Get-Content Tools/test-allocation.ps1 -Raw).Contains('/langversion:9')) 'C#9 allocation guard missing'
$unit=Get-Content "$root/hud-audit-unit-v0287.log" -Raw
Require ($unit -match 'GATEFALL HUD COMMAND AUDIT.*sampleUnit=TimeNanoseconds.*sampleValueSum=' -and $unit -notmatch 'GATEFALL HUD COMMAND AUDIT.*allocatedBytes=') 'HUD audit still labels time as bytes'
$eq=Get-Content "$root/target-query-equivalence-v0287.json" -Raw|ConvertFrom-Json
Require ($eq.early26 -and $eq.normalEqual -and $eq.normal14Cases -eq 12 -and $eq.crisisEqual -and $eq.crisisCases -eq 2 -and $eq.candidateEqualsProduct) 'Behavior equivalence incomplete'
$delta=@(Get-Content "$root/core-delta-v0287.json" -Raw|ConvertFrom-Json)
Require ($delta.Count -eq 6 -and @($delta|Where-Object same).Count -eq 5 -and @($delta|Where-Object{-not $_.same})[0].file -ceq 'DefenseSimulation.cs') 'Unexpected core changes'
foreach($name in @('competent-plan-review','degenerate-strategy-review')){$gate=(Get-Content ('TestResults/'+$name+'.json') -Raw|ConvertFrom-Json).gate;Require ($gate.enforced -and @($gate.violations).Count -eq 0) ('Strategy gate failed '+$name)}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$before=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/update-before-v0287.sav")))
Require ($before.Valid() -and $before.MagicCores -eq 214 -and $before.StarsTotal -eq 39 -and $before.Loadout -eq 263 -and ($before.Research -join ',') -ceq '3,2,2,0' -and $before.Growth[0] -eq 5 -and -not $before.FirstCleared[13] -and $before.LastReward -eq 34 -and $before.PendingRun -eq '') 'Initial synthetic account differs'
foreach($name in @('update-after','abandon','cold')){Require ((Hash "$root/$name-v0287.sav") -ceq (Hash "$root/update-before-v0287.sav")) ('Save changed '+$name)}
$pending=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/pending-v0287.sav")))
Require ($pending.Valid() -and $pending.PendingStage -eq 13 -and $pending.PendingRun -ne '' -and -not $pending.PendingCrisis) 'Native mode/stage differs'
$expected=$before.Copy();$expected.PendingRun=$pending.PendingRun;$expected.PendingStage=13
Require ([Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($expected)) -ceq [Convert]::ToBase64String([IO.File]::ReadAllBytes((Join-Path $project "$root/pending-v0287.sav")))) 'Battle changed unrelated account fields'
$native=Get-Content "$root/target-query-native-v0287.json" -Raw|ConvertFrom-Json
Require ($native.version -eq '0.28.7' -and -not $native.fullBattle -and $native.finalTowers -eq 6 -and $native.combatSpentReconstructed -eq 485 -and $native.fusions -eq 1 -and $native.arcLevel -eq 2 -and $native.arcFinalLoggedShots -eq 22 -and $native.arcFinalLoggedHits -eq 49 -and $native.newReward -eq 0 -and ($native.cards -join ',') -ceq 'Range30,Ballistic90') 'Native observations differ'
foreach($screen in $native.screenEvidence){Require ((Hash $screen.file) -ceq $screen.sha256) 'Screenshot missing/changed'}
$cleanup=Get-Content "$root/cleanup-v0287.json" -Raw|ConvertFrom-Json
Require ($cleanup.normalRestored -and $cleanup.normalShutdown -and $cleanup.remainingOwnedProcesses -eq 0 -and $cleanup.reparsePoints -eq 0) 'Owned cleanup incomplete'
$dedup=Get-Content "$root/apk-dedup-result.json" -Raw|ConvertFrom-Json
Require ($dedup.pathsPreserved -eq 13 -and $dedup.allContentsSame) 'Owned APK duplicate preservation incomplete'
foreach($file in $dedup.files){Require ((Hash $file.preservedPath) -ceq $file.sha256 -and (Hash $file.canonical) -ceq $file.sha256) 'Prior stable path changed'}
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;hostCompiler='C#9';hostQueries=20000;hostBeforeBytes=1280000;hostAfterBytes=0;unityQueryNewobjRemoved=$true;unityThreadByteCounterUsable=$false;hudProfilerUnit='TimeNanoseconds';earlyEquivalent=26;normalEquivalent=12;crisisEquivalent=2;nativeFullBattle=$false;nativeApproxSeconds=110;towers=6;builds=7;upgrades=3;fusions=1;spentReconstructed=485;arcShots=22;arcHits=49;nativeCore=300;cards=@('Range30','Ballistic90');newReward=0;accountCores=214;normalStars=39;historicalReward=34;saveBytes=(Get-Item "$root/cold-v0287.sav").Length;saveHash=(Hash "$root/cold-v0287.sav");updateAbandonColdSame=$true;scope='Partial Android attack/fusion/reinforcement validation,not whole-battle victory. Host allocation/Unity compiled IL evidence,not Android frame/GC/thermal improvement;no human fun,physical Android/iOS or audio claim.'}|ConvertTo-Json -Depth 5|Set-Content "$root/delivery-v0287.json" -Encoding utf8
'PASS0.28.7:C#9 comparator cache/negative control/compiledIL,correctHUDunits,26early+14late equality,Android6T485/fusion/chain,package16KB/install/save preservation'
