# Read-only verification of the recorded 0.28.0 QA delivery; does not run a battle or modify a save.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location $project
$root='TestResults/growth-role-cycle'
function Require($condition,[string]$message){if(-not $condition){throw $message}}
function Hash([string]$path){(Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash.ToLowerInvariant()}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
foreach($row in (Get-Content "$root/final-product-hashes.json" -Raw|ConvertFrom-Json)){
    Require ((Hash $row.file) -ceq $row.sha256) ('Product changed: '+$row.file)
}
$apk=Get-Content TestResults/android-apk-results.json -Raw|ConvertFrom-Json
Require ($apk.version -eq '0.28.0' -and $apk.versionCode -eq 117) 'Wrong delivery version'
foreach($path in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.28.0.apk',"$root/final-installed.apk")){
    Require ((Hash $path) -ceq $apk.sha256) ('APK mismatch: '+$path)
}
Require ($apk.signature -eq 'verified' -and $apk.alignment16KB -eq 'verified') 'Signature/ZIP alignment not verified'
$elf=Get-Content "$root/final-elf.json" -Raw|ConvertFrom-Json
Require ($elf.elfLoad16KB -and $elf.sha256 -ceq $apk.sha256) 'ELF verification differs'
Require ((Hash "$root/final-update-before.sav") -ceq (Hash "$root/final-update-after.sav")) 'Update changed synthetic save'
$before=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/final-update-before.sav")))
$bought=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/final-growth-purchased.sav")))
$result=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $project "$root/final-result.sav")))
Require ($before.MagicCores -eq 342 -and $bought.MagicCores -eq 264 -and $result.MagicCores -eq 266) 'Account expenditure/reward differs'
Require (($bought.Growth -join ',') -eq '5,0,0' -and ($bought.Research -join ',') -eq '3,2,2,2') 'Native growth purchases differ'
Require ($result.PendingRun -eq '' -and $result.LastReward -eq 2 -and $result.StarsTotal -eq 42) 'Reward/pending state differs'
$normal=$result.Copy();$normal.MagicCores=$bought.MagicCores;$normal.LastRewardRun=$bought.LastRewardRun
Require ([Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($normal)) -ceq [Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($bought))) 'Reward changed other account fields'
foreach($path in @("$root/final-map.sav","$root/final-relaunch.sav")){
    Require ((Hash $path) -ceq (Hash "$root/final-result.sav")) ('Save differs: '+$path)
}
$run=Get-Content "$root/final-last-run.txt"
Require ($run[0] -eq 'Gatefall 0.28.0' -and $run[1] -match '^Won elapsed=185\.11 core=300 hunter=140 kills=944 .* fusions=2$') 'Native result differs'
Require ($run[2] -eq 'Wave clear=True allSpawned=True spawned=944 remainingEnemies=0 bossDefeated=False') 'Wave-clear condition differs'
Require ($run[3] -match '^Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0 ') 'Supply/card/catalyst caps differ'
# Costs come from the product catalog and the observed ordered ledger. No guessed price table.
$levels=@{};$spent=0;$builds=0;$upgrades=0;$fusions=0
foreach($line in $run){
    if($line -match '^\d+\.\d+ build (\w+) @(\d+)$'){
        $kind=[Gatefall.Core.TowerKind]$Matches[1];$slot=[int]$Matches[2]
        $spent+=[Gatefall.Core.TowerCatalog]::Get($kind).BuildCost;$levels[$slot]=1;$builds++
    }elseif($line -match '^\d+\.\d+ upgrade (\w+) @(\d+)$'){
        $kind=[Gatefall.Core.TowerKind]$Matches[1];$slot=[int]$Matches[2]
        Require ($levels.ContainsKey($slot)) 'Upgrade lacks prior material'
        $spent+=[Gatefall.Core.TowerCatalog]::Get($kind).UpgradePrice($levels[$slot]);$levels[$slot]++;$upgrades++
    }elseif($line -match '^\d+\.\d+ fusion .* -> (\w+) @(\d+)$'){
        $kind=[Gatefall.Core.TowerKind]$Matches[1];$slot=[int]$Matches[2]
        $spent+=([Gatefall.Core.TowerCatalog]::Recipes|Where-Object Result -eq $kind|Select-Object -First 1).Credits
        $levels[$slot]=1;$fusions++
    }
}
Require ($builds -eq 16 -and $upgrades -eq 7 -and $fusions -eq 2 -and $spent -eq 1065) 'Native purchase accounting differs'
Require ($builds-$fusions -eq 14 -and -not @($run|Where-Object {$_ -match '^\d+\.\d+ sell '}).Count) 'Final tower count/sales differ'
$early=Get-Content TestResults/final-growth-early-comparison.json -Raw|ConvertFrom-Json
Require ($early.equal -and $early.cases -eq 26) 'Early combat equivalence not verified'
foreach($name in @('competent-plan-review','degenerate-strategy-review')){
    $gate=(Get-Content ("TestResults/"+$name+'.json') -Raw|ConvertFrom-Json).gate
    Require ($gate.enforced -and @($gate.violations).Count -eq 0) ('Gate failed: '+$name)
}
$matrix=Get-Content TestResults/growth-role-final.json -Raw|ConvertFrom-Json
Require (@($matrix.rows).Count -eq 288) 'Final progression cases incomplete'
[ordered]@{version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;
    updateSaveSame=$true;accountSpent=78;repeatReward=2;resultSaveHash=(Hash "$root/final-result.sav");relaunchSame=$true;
    nativeStage='1-14 normal replay';seconds=185.11;kills=944;core=300;hunter=140;stars=3;
    builds=$builds;upgrades=$upgrades;fusions=$fusions;towers=14;spentFromCatalogLedger=$spent;
    sealDamage=7082.659;cards=@('Blast','Range','Ballistic');models=288;earlyEquivalent=26;
    humanPlay=$false;physicalAndroid=$false;ios=$false;
    scope='Independent emulator and synthetic save; card/menu inspection included HOME focus pauses. Not an exact fixed-seed model reproduction, all-stage native rerun, simultaneous fourteen-tower firing proof, or device performance benchmark.'
}|ConvertTo-Json -Depth 4|Set-Content "$root/final-delivery-consistency.json"
'PASS0.28.0: product/APK/install/ELF, update and growth/reward/relaunch saves, native14 towers/2fusions/944clear, early26 and both gates'
