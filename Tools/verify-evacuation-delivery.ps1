$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0180.json -Raw|ConvertFrom-Json
foreach($taskFile in @('Builds/Android/Gatefall-v0.18.0.apk','Builds/Android/Gatefall.apk','TestResults/v0180-installed.apk')) {
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256){throw "APK differs: $taskFile"}
}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0180.json -Raw|ConvertFrom-Json)) {
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Source changed: $($taskRow.path)"}
}
if((Get-FileHash TestResults/v0180-update-before.sav).Hash -ne (Get-FileHash TestResults/v0180-update-after.sav).Hash){throw 'Completed synthetic save changed on update'}
$taskProfile=Get-Content TestResults/v0180-stage7-profile-summary.json -Raw|ConvertFrom-Json
if(-not $taskProfile.valid -or $taskProfile.magic -ne 62 -or $taskProfile.stars -ne 15 -or $taskProfile.stage7Stars -ne 3 -or $taskProfile.reward -ne 22 -or -not $taskProfile.stage8Open -or $taskProfile.pending){throw 'Native clear/reward profile differs'}
$taskRun=Get-Content TestResults/v0180-stage7-last-run.txt -Raw
if($taskRun -notmatch 'Won elapsed=188.60 core=300 hunter=100 kills=552' -or $taskRun -notmatch 'allSpawned=True spawned=552 remainingEnemies=0' -or $taskRun -notmatch 'Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2 waveCalls=0'){throw 'Native run differs'}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
$taskTowers=@{};$taskSpent=0;$taskRefunds=0;$taskFusions=0;$taskReturnedCores=0
foreach($taskLine in ($taskRun -split '\r?\n')) {
    if($taskLine -match '^[\d.]+ build (\w+) @(\d+)$') {
        $taskKind=[Gatefall.Core.TowerKind]$Matches[1];$taskSocket=[int]$Matches[2]
        if($taskTowers.ContainsKey($taskSocket)){throw 'Build on occupied site'}
        $taskCost=[Gatefall.Core.TowerCatalog]::Get($taskKind).BuildCost;$taskSpent+=$taskCost
        $taskTowers[$taskSocket]=[pscustomobject]@{socket=$taskSocket;kind=$taskKind;level=1;paid=$taskCost;cores=0}
    } elseif($taskLine -match '^[\d.]+ upgrade (\w+) @(\d+)$') {
        $taskKind=[Gatefall.Core.TowerKind]$Matches[1];$taskT=$taskTowers[[int]$Matches[2]]
        if(-not $taskT -or $taskT.kind -ne $taskKind -or $taskT.level -ne 1){throw 'Invalid upgrade in native log'}
        $taskCost=[Gatefall.Core.TowerCatalog]::Get($taskKind).UpgradeCost;$taskSpent+=$taskCost;$taskT.paid+=$taskCost;$taskT.level=2
    } elseif($taskLine -match '^[\d.]+ sell (\w+) @(\d+)$') {
        $taskSocket=[int]$Matches[2];$taskT=$taskTowers[$taskSocket]
        if(-not $taskT -or $taskT.kind.ToString() -ne $Matches[1]){throw 'Invalid sale in native log'}
        $taskRefunds+=[int][math]::Floor($taskT.paid*3/4);$taskReturnedCores+=$taskT.cores;$taskTowers.Remove($taskSocket)
    } elseif($taskLine -match '^[\d.]+ fusion \((\w+)\+(\w+)\) -> (\w+) @(\d+)$') {
        $taskA=[Gatefall.Core.TowerKind]$Matches[1];$taskB=[Gatefall.Core.TowerKind]$Matches[2];$taskResult=[Gatefall.Core.TowerKind]$Matches[3];$taskSocket=[int]$Matches[4]
        $taskAnchor=$taskTowers[$taskSocket];$taskOther=if($taskAnchor.kind -eq $taskA){$taskB}else{$taskA}
        $taskPartners=@($taskTowers.Values|Where-Object {$_.socket -ne $taskSocket -and $_.kind -eq $taskOther -and $_.level -eq 2})
        if($taskAnchor.level -ne 2 -or $taskPartners.Count -ne 1){throw 'Ambiguous/invalid fusion in native log'}
        $taskPartner=$taskPartners[0];$taskRecipe=@([Gatefall.Core.TowerCatalog]::Recipes|Where-Object {$_.Result -eq $taskResult -and (($_.First -eq $taskA -and $_.Second -eq $taskB) -or ($_.First -eq $taskB -and $_.Second -eq $taskA))})[0]
        $taskSpent+=$taskRecipe.Credits;$taskFusions++
        $taskTowers.Remove($taskPartner.socket)
        $taskTowers[$taskSocket]=[pscustomobject]@{socket=$taskSocket;kind=$taskResult;level=1;paid=$taskAnchor.paid+$taskPartner.paid+$taskRecipe.Credits;cores=$taskAnchor.cores+$taskPartner.cores+1}
    }
}
if($taskTowers.Count -ne 14 -or $taskSpent -ne 1195 -or $taskRefunds -ne 191 -or $taskFusions -ne 2 -or $taskReturnedCores -ne 1){throw 'Native purchase ledger differs'}
[pscustomobject]@{towers=@($taskTowers.Values|Sort-Object socket|Select-Object socket,@{n='kind';e={$_.kind.ToString()}},level,paid,cores);count=$taskTowers.Count;spent=$taskSpent;refunds=$taskRefunds;netSpent=$taskSpent-$taskRefunds;fusions=$taskFusions;catalystsGranted=2;catalystsRefunded=$taskReturnedCores;scope='Reconstructed from successful native purchase log, independent of planned coordinates; no per-site shot count'}|ConvertTo-Json -Depth 5|Set-Content TestResults/v0180-native-purchase-ledger.json
$taskErrors=Get-Content TestResults/android-v0180-errors.json -Raw|ConvertFrom-Json
if($taskErrors.postFirstPerfErrors -ne 0 -or -not $taskErrors.graphics31Created){throw 'Unexpected runtime diagnostics'}
$taskEffects=Get-Content TestResults/android-v0180-effects.json -Raw|ConvertFrom-Json
if($taskEffects.countMismatches -ne 0 -or $taskEffects.fxPeak -gt 220 -or $taskEffects.recycled -ne 0){throw 'Native view/effect mismatch'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0180.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.directoryAbsent -or $taskCleanup.priorityRestored -ne 'Normal' -or -not $taskCleanup.protectedDevicesReceivedNoInput){throw 'Cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;sha256=$taskApk.sha256;apkSourceInstalledMatch=$true;updateSaveEqual=$true;stage7NativeClearSeconds=188.6;kills=552;core=300;hunter=100;stars=3;rewardSaved=22;nextStageOpen=$true;towers=14;fusions=2;spent=$taskSpent;refunds=$taskRefunds;netSpent=$taskSpent-$taskRefunds;bindingShots=0;arcShotsAfterForwardRefusion=6;startupDiagnostics=$taskErrors.rawErrorLines;postFirstPerfErrors=0;fxPeak=$taskEffects.fxPeak;ownedAvdCleaned=$true;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json|Set-Content TestResults/delivery-consistency-v0180.json
Write-Output 'PASS0.18 APK/source/install/update-save/14 native towers/552 clear/reward and purchase ledger/diagnostics/effects/owned cleanup; Binding0 and rear-idle limitation retained.'
