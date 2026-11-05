$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskApk=Get-Content TestResults/android-apk-v0181.json -Raw|ConvertFrom-Json
if($taskApk.version -ne '0.18.1' -or $taskApk.versionCode -ne 49 -or $taskApk.signature -ne 'verified' -or $taskApk.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($taskFile in @('Builds/Android/Gatefall-v0.18.1.apk','Builds/Android/Gatefall.apk','TestResults/v0181-installed.apk')) {
    if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256 -or (Get-Item -LiteralPath $taskFile).Length -ne $taskApk.bytes){throw "APK differs: $taskFile"}
}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0181.json -Raw|ConvertFrom-Json)) {
    if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Source changed: $($taskRow.path)"}
}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
$taskBaseHash=(Get-FileHash TestResults/v0181-update-before.sav).Hash
$taskDecoded=@{}
foreach($taskCase in @('update-after','preparation-pending','ready-after','running-pending','failure-profile','retry-after','relaunch-profile')) {
    $taskPath="TestResults/v0181-$taskCase.sav"
    $taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path $taskProject $taskPath)))
    if(-not $taskP.Valid() -or $taskP.MagicCores -ne 62 -or $taskP.StarsTotal -ne 15 -or $taskP.BestStars[6] -ne 3 -or $taskP.LastReward -ne 22 -or $taskP.LastRewardRun -ne '71b5f22036c6462cbe2caf3b29e7948d' -or -not $taskP.IsStageOpen(7)){throw "Progress/reward differs: $taskCase"}
    $taskDecoded[$taskCase]=$taskP
    if($taskCase -in @('preparation-pending','running-pending','failure-profile')) {
        if($taskP.PendingRun.Length -ne 32 -or $taskP.PendingStage -ne 6 -or $taskP.PendingCrisis){throw "Pending fixture invalid: $taskCase"}
    } elseif((Get-FileHash -LiteralPath $taskPath).Hash -ne $taskBaseHash -or $taskP.PendingRun -ne '' -or $taskP.PendingStage -ne -1 -or $taskP.PendingCrisis){throw "Cleared profile differs: $taskCase"}
}
if($taskDecoded['preparation-pending'].PendingRun -eq $taskDecoded['running-pending'].PendingRun){throw 'Separate attempts reused an ID'}
if((Get-FileHash TestResults/v0181-running-pending.sav).Hash -ne (Get-FileHash TestResults/v0181-failure-profile.sav).Hash){throw 'Failed save changed existing profile'}
$taskFixture=Get-Content TestResults/v0181-failure-injection.json -Raw|ConvertFrom-Json
if($taskFixture.preexisting -or -not $taskFixture.syntheticOnly -or -not $taskFixture.fixtureRemoved -or $taskFixture.serial -ne 'emulator-5590'){throw 'Failure fixture ownership/removal incomplete'}
$taskUi=Get-Content TestResults/android-v0181-abandon-observations.json -Raw|ConvertFrom-Json
if($taskUi.failureScreen -ne 'Pause' -or $taskUi.failureClock -ne '0:06' -or -not $taskUi.retryTextVisible -or -not $taskUi.retryReturnedToCity -or -not $taskUi.relaunchReturnedToCity){throw 'Native UI observations incomplete'}
foreach($taskShot in $taskUi.screenshots){if(-not(Test-Path -LiteralPath $taskShot)){throw "Missing screenshot: $taskShot"}}
$taskParity=Get-Content TestResults/combat-parity-v0181.json -Raw|ConvertFrom-Json
foreach($taskFile in @('BattlefieldLayouts.cs','DefenseSimulation.cs','MobileInput.cs','RaonCombat.cs','TowerCatalog.cs')) {
    if((Get-FileHash "Assets/Scripts/Core/$taskFile").Hash -ne (Get-FileHash "TestResults/abandon-v0181-baseline/Core/$taskFile").Hash){throw "Combat baseline differs: $taskFile"}
}
$taskCampaignBefore=[IO.File]::ReadAllText((Join-Path $taskProject 'TestResults/abandon-v0181-baseline/Core/Campaign.cs'))
$taskCampaignAfter=[IO.File]::ReadAllText((Join-Path $taskProject 'Assets/Scripts/Core/Campaign.cs'))
$taskWithoutAbandon=[regex]::Replace($taskCampaignAfter,'(?ms)^        public bool Abandon\(string id\)\r?\n        \{.*?^        \}\r?\n','')
if($taskCampaignBefore.Replace("`r`n","`n") -ne $taskWithoutAbandon.Replace("`r`n","`n")){throw 'Campaign changed beyond added Abandon method'}
if((Get-Content TestResults/campaign-results.txt -Raw) -notmatch 'PASS: 48 campaign/combat/persistence checks'){throw 'Campaign checks missing'}
$taskErrors=Get-Content TestResults/android-v0181-errors.json -Raw|ConvertFrom-Json
if($taskErrors.rawStartupErrorLines -ne 5 -or $taskErrors.postFirstPerfErrors -ne 0 -or -not $taskErrors.graphics31Created){throw 'Native diagnostics differ'}
$taskCleanup=Get-Content TestResults/qa-cleanup-v0181.json -Raw|ConvertFrom-Json
if(-not $taskCleanup.directoryAbsent -or $taskCleanup.priorityRestored -ne 'Normal' -or -not $taskCleanup.protectedDevicesReceivedNoInput -or -not $taskCleanup.protectedBossCall -or -not $taskCleanup.protectedStage4Followup -or $taskCleanup.otherProcessesChanged){throw 'Owned cleanup incomplete'}
[pscustomobject]@{created=(Get-Date).ToString('o');version=$taskApk.version;versionCode=$taskApk.versionCode;sha256=$taskApk.sha256;apkSourceInstalledMatch=$true;updateSaveEqual=$true;nativeProfileCases=7;preparationAbandon=$true;runningAbandonSaveFailureAtomic=$true;retryAndRelaunchEqualOriginal=$true;magicCoresPreserved=62;starsPreserved=15;oldRewardPreserved=22;newRewardGranted=0;campaignChecks=48;combatFilesUnchanged=5;saveFormatUnchanged=$true;startupDiagnostics=5;postFirstPerfErrors=0;ownedAvdCleaned=$true;nativeFullBattleThisVersion=$false;humanPlaytested=$false;physicalDeviceTested=$false;iosTested=$false}|ConvertTo-Json|Set-Content TestResults/delivery-consistency-v0181.json
Write-Output 'PASS0.18.1 APK/source/install/update-save; seven native save cases; abandon/failure/retry/relaunch; existing rewards preserved; combat unchanged; diagnostics retained; owned cleanup.'
