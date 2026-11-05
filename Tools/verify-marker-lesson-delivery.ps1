$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$meta=Get-Content TestResults/android-apk-v0211-candidate.json -Raw|ConvertFrom-Json
if($meta.version -ne '0.21.1' -or $meta.versionCode -ne 68 -or $meta.signature -ne 'verified' -or $meta.alignment16KB -ne 'verified'){throw 'APK metadata differs'}
foreach($path in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.21.1.apk','TestResults/v0211-installed.apk')){
    if((Get-FileHash $path).Hash -ne $meta.sha256 -or (Get-Item $path).Length -ne $meta.bytes){throw "APK differs: $path"}
}
$old=@(Get-Content TestResults/marker-lesson-v0211-baseline/source-hashes-v0210.json -Raw|ConvertFrom-Json)
$current=@(Get-Content TestResults/source-hashes-v0211.json -Raw|ConvertFrom-Json)
if($current.Count -ne 20){throw 'Source manifest scope differs'}
$changed=@(foreach($record in $current){
    if((Get-FileHash $record.path).Hash -ne $record.sha256){throw 'Current sources changed'}
    $prior=@($old|Where-Object path -eq $record.path)
    if($prior.Count -ne 1){throw 'Missing unique baseline path'}
    if($prior[0].sha256 -ne $record.sha256){$record.path}
})
if(@(Compare-Object $changed @('Assets\Scripts\Core\Campaign.cs','ProjectSettings\ProjectSettings.asset')).Count){throw 'Unexpected source changes'}
$oldLesson='클리어 후 편성한 표식과 창을 Lv2로 강화해 조합하세요.'
$newLesson='표식을 편성했다면 입구 쪽 창과 함께 두세요. 클리어하면 관측창 조합이 해금됩니다.'
if([IO.File]::ReadAllText('TestResults/marker-lesson-v0211-baseline/Campaign.cs').Replace($oldLesson,$newLesson) -cne [IO.File]::ReadAllText('Assets/Scripts/Core/Campaign.cs')){throw 'Campaign differs beyond one lesson'}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$seed=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0207-mass-result.sav')))
$expectedHash='c85b059454993f432d48e8179bc4334ae990a78b7f63c448577a97cd81e418a7'
$saves=@(foreach($phase in @('before','after-update','abandon','relaunch')){
    $path="TestResults/v0211-$phase.sav";$hash=(Get-FileHash $path).Hash.ToLowerInvariant()
    $profile=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $path)))
    if($hash -ne $expectedHash -or (Get-Item $path).Length -ne 324 -or -not $profile.Valid() -or $profile.MagicCores -ne 196 -or $profile.StarsTotal -ne 39 -or $profile.Loadout -ne 263 -or $profile.SelectedHunter -ne 0 -or $profile.LastReward -ne 34 -or $profile.LastRewardRun -ne $seed.LastRewardRun -or $profile.FirstCleared[13] -or -not $profile.IsStageOpen(13) -or $profile.PendingRun -ne '' -or $profile.PendingStage -ne -1 -or @($profile.Research|Where-Object {$_ -ne 0}).Count -or @($profile.Growth|Where-Object {$_ -ne 0}).Count){throw "Preservation differs: $phase"}
    [pscustomobject]@{phase=$phase;bytes=324;sha256=$hash;valid=$true;magic=196;stars=39;loadout=263;lastReward=34;rewardId=$profile.LastRewardRun;stage14Cleared=$false;pending=''}
})
$saves|ConvertTo-Json -Depth 5|Set-Content TestResults/v0211-save-check.json
$ui=Get-Content TestResults/v0211-lesson-observations.json -Raw|ConvertFrom-Json
if($ui.stage -ne 14 -or -not $ui.preparationOnly -or $ui.elapsed -ne 0 -or $ui.towers -ne 0 -or $ui.spent -ne 0 -or $ui.credits -ne 320 -or $ui.lesson -cne $newLesson -or $ui.screens.Count -ne 3 -or @($ui.screens|Where-Object {-not $_.completeTwoLines}).Count){throw 'Native observation scope differs'}
foreach($width in @(2400,1920,1280)){if(-not(Test-Path "Docs/QA/v050/v0211-lesson-$width.png")){throw 'Missing visual evidence'}}
$diagnostics=Get-Content TestResults/v0211-diagnostics.json -Raw|ConvertFrom-Json
if($diagnostics.appPid -ne 5080 -or $diagnostics.frameworkPid -ne '583' -or $diagnostics.frameworkAfter -ne '583' -or $diagnostics.surface -ne 12 -or $diagnostics.other -ne 0 -or $diagnostics.fatalPriorityLines -ne 0 -or $diagnostics.packageAnr -ne 0 -or $diagnostics.watchdog -ne 0){throw 'Diagnostics require review'}
$cleanup=Get-Content TestResults/qa-cleanup-v0211.json -Raw|ConvertFrom-Json
if($cleanup.serial -ne 'emulator-5638' -or $cleanup.qemuPid -ne 14864 -or -not $cleanup.normalPrioritiesRestored -or -not $cleanup.normalExit -or $cleanup.remainingProcesses -ne 0 -or -not $cleanup.diskFilesRemoved -or $cleanup.reparsePoints -ne 0 -or $cleanup.protectedDevicesTouched){throw 'Owned cleanup differs'}
[ordered]@{created=(Get-Date -Format o);task=236;version=$meta.version;code=$meta.versionCode;bytes=$meta.bytes;sha256=$meta.sha256;changedProductFiles=$changed;singleStage14LessonOnly=$true;otherFiveCoreAndPresentationUnchanged=$true;combatEconomyRoutesInputSaveUnchanged=$true;native=$ui;previousFullStage14Version='0.21.0';newFullBattle=$false;fourActualSavesIdentical=$true;profileHash=$expectedHash;newReward=0;diagnostics=$diagnostics;diagnosticRootCausesResolved=$false;ownedCleanup=$cleanup;humanFunTested=$false;physicalDeviceTested=$false;iosTested=$false;audioTested=$false;passed=$true}|ConvertTo-Json -Depth 8|Set-Content TestResults/delivery-consistency-v0211.json
'PASS: v0.21.1 single lesson/20-source scope/3 native Ready screens/4 actual preserved saves/installed hash/owned cleanup; no new full battle'
