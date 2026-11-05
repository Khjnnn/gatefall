$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskApk=(Get-FileHash Builds/Android/Gatefall-v0.20.4.apk).Hash
if($taskApk -ne '140fe4b1cedcb804776f7c267c74d587758dbadd3cf58c0d29c1acf60e8d1bfd' -or
    (Get-FileHash TestResults/v0204-observer-installed.apk).Hash -ne $taskApk){throw 'Delivered/installed APK differs'}
foreach($taskR in (Get-Content TestResults/source-hashes-v0204.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Version204 source changed; inspect its recorded report instead'}}
$taskSeed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0204-observer-seed.sav')))
if(-not $taskSeed.Valid() -or -not $taskSeed.FirstCleared[13] -or $taskSeed.Loadout -ne 135 -or $taskSeed.MagicCores -ne 76 -or $taskSeed.StarsTotal -ne 29 -or $taskSeed.LastReward -ne 36){throw 'Prior completed synthetic fixture differs'}
if((Get-FileHash TestResults/v0204-observer-before.sav).Hash -ne (Get-FileHash TestResults/v0204-observer-seed.sav).Hash){throw 'Initial transfer changed profile'}
$taskExpected=$taskSeed.Copy();$taskExpected.Loadout=263
$taskSaves=@(foreach($taskPhase in @('loadout','abandon','relaunch')){
    $taskPath="TestResults/v0204-observer-$taskPhase.sav"
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if(-not $taskP.Valid() -or (Get-FileHash $taskPath).Hash -ne 'c75400f9f8b77db15b3b0c07ab63298e116bc966ae052bcabdc444b7925d4981'){throw 'Post loadout/abandon/relaunch profile differs'}
    foreach($taskField in $taskP.GetType().GetFields()){if(($taskField.GetValue($taskP)|ConvertTo-Json -Compress) -cne ($taskField.GetValue($taskExpected)|ConvertTo-Json -Compress)){throw "Unexpected saved field: $($taskField.Name)"}}
    [ordered]@{phase=$taskPhase;valid=$true;bytes=(Get-Item $taskPath).Length;sha256=(Get-FileHash $taskPath).Hash.ToLowerInvariant();magic=$taskP.MagicCores;stars=$taskP.StarsTotal;loadout=$taskP.Loadout;lastReward=$taskP.LastReward;rewardRun=$taskP.LastRewardRun;pending=$taskP.PendingRun}
})
$taskSaves|ConvertTo-Json -Depth 4|Set-Content TestResults/v0204-observer-save-check.json
$taskLog=Get-Content TestResults/v0204-observer-logcat-final.txt
$taskPidLines=@($taskLog|Where-Object {$_ -match '^\d\d-\d\d \S+\s+4727\s+'})
$taskFirstPerf=($taskPidLines|Where-Object {$_ -match 'GATEFALL PERF'}|Select-Object -First 1).Substring(0,18)
$taskAfter=@($taskPidLines|Where-Object {$_.Substring(0,18) -ge $taskFirstPerf})
$taskEF=@($taskAfter|Where-Object {$_ -match '\s[EF]\s'})
$taskSurface=@($taskEF|Where-Object {$_ -match 'E Surface\s*:'})
$taskOther=@($taskEF|Where-Object {$_ -notmatch 'E Surface\s*:'})
$taskCounters=@(foreach($taskLine in $taskPidLines){if($taskLine -match 'shots=([\d,]+) hits=([\d,]+)'){
    $taskShots=$Matches[1].Split(',');$taskHits=$Matches[2].Split(',');[pscustomobject]@{shots=[int]$taskShots[9];hits=[int]$taskHits[9]}
}})
$taskMaxShots=($taskCounters|Measure-Object shots -Maximum).Maximum;$taskMaxHits=($taskCounters|Measure-Object hits -Maximum).Maximum
if($taskMaxShots -lt 32 -or $taskMaxHits -lt 32){throw 'Observed actual Observer fire/hits not preserved'}
foreach($taskShot in @('loadout-selected','spear-six-menu','ready-catalyst-needed','fusion-available','level1-menu','level1-live','level2-actual-menu','abandon-city','relaunch-city')){
    if(-not(Test-Path "Docs/QA/v050/v0204-observer-$taskShot.png")){throw "Missing evidence: $taskShot"}
}
[ordered]@{created=(Get-Date -Format o);task=205;version='0.20.4';apkSha256=$taskApk.ToLowerInvariant();nativeShortTest=$true;
    sourceOfUnlock='Existing0.16 completed synthetic profile; no new stage14 victory';actualLoadout135To263=$true;initialTowers=5;initialSpend=275;
    observerAnchor=20;consumedSpearSite=6;observerLevel=2;finalTowers=5;reconstructedSpend=415;terminalPurchaseReceiptAvailable=$false;
    firstCard='Ballistic';secondCardIdentityRecorded=$false;secondCardResumeCoordinateInputUncertain=$true;newReward=0;
    maxObservedObserverShots=$taskMaxShots;maxObservedObserverHits=$taskMaxHits;shotRateIncreaseFromUpgradeProven=$false;
    saveSha256='c75400f9f8b77db15b3b0c07ab63298e116bc966ae052bcabdc444b7925d4981';onlyProfileChange='Loadout135->263 via actual city UI';originalReward36AndIdPreserved=$true;
    appPid=4727;observedSystemServerPid=580;postFirstPerfSurface=$taskSurface.Count;postFirstPerfOtherEF=$taskOther.Count;
    actualUiIssue='Observer description wraps range into the enemy-count line and omits damage/upgrade difference at both levels';uiFixComplete=$false;
    wholeBattleCompleted=$false;humanFunEvaluated=$false;physicalDeviceTested=$false;iosTested=$false;audioTested=$false;
    deviceContinuesForTask206=$true;ownedSerial='emulator-5622';qemuPid=27740;launcherPid=32156;passedFunctionalScope=$true}|ConvertTo-Json -Depth 5|Set-Content TestResults/observer-functional-v0204.json
Write-Output "PASS functional scope: Observer native shots/hits $taskMaxShots/$taskMaxHits, Lv2 and normal-abandon save. UI overlap remains; Surface=$($taskSurface.Count) otherEF=$($taskOther.Count)"
