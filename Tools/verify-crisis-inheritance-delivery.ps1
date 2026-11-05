# Recorded 0.28.1 delivery only. This reads evidence; it does not run a battle or change a profile.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location $project
$root='TestResults/night-balance-20261105'
function Require($ok,[string]$why){if(-not $ok){throw $why}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
foreach($row in (Get-Content "$root/product-hashes-v0281.json" -Raw|ConvertFrom-Json)){
    Require ((Hash $row.file) -ceq $row.sha256) ('Product changed: '+$row.file)
}
$original=[IO.File]::ReadAllText((Join-Path $project "$root/baseline-core/DefenseSimulation.cs"))
$needle='if(Rules!=null && Rules.Stage!=null && !Rules.Crisis && (Rules.Stage.Index==10 || Rules.Stage.Index==11) && g.Wave>=3'
$replacement='if(Rules!=null && Rules.Stage!=null && (Rules.Stage.Index==10 || Rules.Stage.Index==11) && g.Wave>=3'
Require ($original.Replace($needle,$replacement) -ceq [IO.File]::ReadAllText((Join-Path $project 'Assets/Scripts/Core/DefenseSimulation.cs'))) 'Combat delta exceeds one reviewed condition'
foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){
    if($file.Name -ne 'DefenseSimulation.cs'){Require ((Hash $file.FullName) -ceq (Hash (Join-Path "$root/baseline-core" $file.Name))) 'Another combat core changed'}
}
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -eq '0.28.1' -and $apk.versionCode -eq 118) 'Wrong candidate version'
foreach($file in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.28.1.apk',"$root/installed-v0281.apk")){
    Require ((Hash $file) -ceq $apk.sha256) ('APK differs: '+$file)
}
Require ((Hash 'Builds/Android/Gatefall-v0.28.0.apk') -ceq 'd485ea552ddadf2048515ee923dec733f3a935a9969032c29cceea3f929f2432') 'Previous stable APK changed'
Require ($apk.signature -eq 'verified' -and $apk.alignment16KB -eq 'verified') 'APK checks incomplete'
$elf=Get-Content "$root/elf-v0281.json" -Raw|ConvertFrom-Json
Require ($elf.elfLoad16KB -and $elf.sha256 -ceq $apk.sha256) 'ELF check differs'
Require ((Hash "$root/update-before.sav") -ceq (Hash "$root/update-after.sav")) 'Update changed synthetic profile'
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
function Profile([string]$file){[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/$file")))}
$before=Profile update-before.sav;$pending=Profile native-pending.sav;$result=Profile result-v0281.sav
Require ($before.MagicCores -eq 160 -and $before.StarsTotal -eq 33 -and ($before.Growth -join ',') -eq '4,0,0' -and ($before.Research -join ',') -eq '3,2,2,2') 'Recommended profile differs'
Require ($pending.PendingCrisis -and $pending.PendingStage -eq 11 -and $pending.PendingRun -ne '') 'Actual native run was not crisis1-12'
$expected=$before.Copy();$expected.MagicCores+=2;$expected.CrisisStars[11]=3;$expected.LastReward=2;$expected.LastRewardRun=$pending.PendingRun
$expected.PendingCrisis=$true # Existing Complete leaves the mode flag set after clearing ID/stage; recorded follow-up.
$expected.TutorialMask=$before.TutorialMask -bor (1 -shl 11) # StartRun records the actually displayed lesson.
Require ($pending.TutorialMask -eq $expected.TutorialMask) 'Tutorial transition differs from captured run'
Require ([Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($expected)) -ceq [Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($result))) 'Unexpected account changes'
Require ($result.PendingRun -eq '' -and $result.PendingStage -eq -1 -and -not $result.FirstCleared[11] -and -not $result.IsStageOpen(12)) 'Crisis incorrectly advanced the normal campaign'
foreach($file in @('map-v0281.sav','relaunch-v0281.sav')){Require ((Hash "$root/$file") -ceq (Hash "$root/result-v0281.sav")) 'Result/map/relaunch differ'}
$run=Get-Content "$root/last-run-v0281.txt"
Require ($run[0] -eq 'Gatefall 0.28.1' -and $run[1] -eq 'Won elapsed=191.33 core=300 hunter=132 kills=880 firstFusion=-1 fusions=0') 'Native result differs'
Require ($run[2] -eq 'Wave clear=True allSpawned=True spawned=880 remainingEnemies=0 bossDefeated=False') 'Wave clear differs'
Require ($run[3] -match '^Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0 ') 'Caps/calls differ'
$levels=@{};$spent=0;$builds=0;$upgrades=0
foreach($line in $run){
    if($line -match '^\d+\.\d+ build Autocannon @(\d+)$'){$slot=[int]$Matches[1];$levels[$slot]=1;$spent+=[Gatefall.Core.TowerCatalog]::Get([Gatefall.Core.TowerKind]::Autocannon).BuildCost;$builds++}
    elseif($line -match '^\d+\.\d+ upgrade Autocannon @(\d+)$'){$slot=[int]$Matches[1];Require ($levels.ContainsKey($slot)) 'Upgrade without material';$spent+=[Gatefall.Core.TowerCatalog]::Get([Gatefall.Core.TowerKind]::Autocannon).UpgradePrice($levels[$slot]);$levels[$slot]++;$upgrades++}
}
Require ($builds -eq 14 -and $upgrades -eq 15 -and $spent -eq 1080) 'Native catalog-ledger price/count differs'
Require (@($levels.Values|Where-Object {$_ -eq 3}).Count -eq 2 -and @($levels.Values|Where-Object {$_ -eq 1}).Count -eq 1) 'Final level mix differs'
$cells=Get-Content "$root/spawn-preservation.json" -Raw|ConvertFrom-Json
Require ($cells.groups -eq 6022 -and $cells.changedThreatGroups -eq 39 -and $cells.preservedGroups -eq 5983 -and $cells.candidateCoreFilesIdentical -eq 6) 'Spawn preservation incomplete'
foreach($name in @('competent-plan-review','degenerate-strategy-review')){
    $gate=(Get-Content ("TestResults/"+$name+'.json') -Raw|ConvertFrom-Json).gate
    Require ($gate.enforced -and @($gate.violations).Count -eq 0) ('Strategy gate failed: '+$name)
}
$models=Get-Content TestResults/night-crisis-heavy-candidate.json -Raw|ConvertFrom-Json
Require (@($models.rows).Count -eq 48 -and @($models.rows|Where-Object State -ne Won).Count -eq 0) 'Reviewed candidate cases differ'
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;stage='1-12 crisis';seconds=191.33;enemies=880;core=300;hunter=132;stars=3;
    builds=$builds;upgrades=$upgrades;towers=14;fusions=0;spentFromCatalogLedger=$spent;cards=@('Ballistic','Credits','Ballistic');cardCredits=35;supplyCredits=500;
    accountCoresBefore=160;accountCoresAfter=162;normalStars=33;crisisStars=3;tutorialBit=2048;pendingCrisisResidual=$true;
    resultSaveBytes=(Get-Item "$root/result-v0281.sav").Length;resultSaveHash=(Hash "$root/result-v0281.sav");updateSaveSame=$true;mapRelaunchSame=$true;
    spawnGroups=6022;changedThreatGroups=39;preservedGroups=5983;candidateModels=48;humanPlay=$false;physicalAndroid=$false;ios=$false;
    scope='One independent Android emulator replay with legitimate growth4/research3,2,2,2. Native timing and cards differ from planner fixtures. Fourteen towers built is not proof of all firing simultaneously. Normal campaign progress preserved; no whole-campaign native rerun. HOME focus pauses included.'
}|ConvertTo-Json -Depth 4|Set-Content "$root/delivery-v0281.json"
'PASS0.28.1: one-condition combat delta,6022spawn groups, both strategy gates, APK/signature/ZIP+ELF/install, crisis1-12 clear1080spend and full account/update/relaunch checks'
