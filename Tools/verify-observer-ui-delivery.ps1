$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskMeta=Get-Content TestResults/android-apk-v0205-candidate.json -Raw|ConvertFrom-Json
if($taskMeta.version -ne '0.20.5' -or $taskMeta.versionCode -ne 62 -or $taskMeta.signature -ne 'verified' -or $taskMeta.alignment16KB -ne 'verified'){throw 'Release metadata differs'}
foreach($taskPath in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.20.5.apk','TestResults/v0205-observer-installed.apk')){
    if((Get-FileHash $taskPath).Hash -ne $taskMeta.sha256 -or (Get-Item $taskPath).Length -ne $taskMeta.bytes){throw 'APK or installed hash differs'}
}
$taskOld=Get-Content TestResults/observer-ui-v0205-baseline/source-hashes.json -Raw|ConvertFrom-Json
$taskRows=Get-Content TestResults/source-hashes-v0205-candidate.json -Raw|ConvertFrom-Json
$taskChanged=@(foreach($taskR in $taskRows){
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Candidate source changed'}
    if(@($taskOld|Where-Object path -eq $taskR.path)[0].sha256 -ne $taskR.sha256){$taskR.path}
})
if($taskChanged.Count -ne 2 -or 'Assets\Scripts\Presentation\MobileHud.cs' -notin $taskChanged -or 'ProjectSettings\ProjectSettings.asset' -notin $taskChanged){throw 'Unexpected product changes'}
$taskHud=[IO.File]::ReadAllText((Join-Path (Get-Location) 'Assets/Scripts/Presentation/MobileHud.cs')).Replace("`r`n","`n")
$taskBaseline=[IO.File]::ReadAllText((Join-Path (Get-Location) 'TestResults/observer-ui-v0205-baseline/MobileHud.cs')).Replace("`r`n","`n")
$taskNewField='            if (tower.Kind == TowerKind.Observer) field = "표식 우선 3초" + (game.IsPreparing ? "" : " · 범위 적 " + EnemiesInRange(tower.Position, now.Range));'
$taskNewResonance='            if (resonance) field += (field.Length > 0 ? " · " : "") + (tower.Kind == TowerKind.Observer ? "공명 +5%" : "인접 공명 +5%");'
$taskOldResonance='            if (resonance) field += (field.Length > 0 ? " · " : "") + "인접 공명 +5%";'
$taskNewStat='            if(kind==TowerKind.Observer)return "피해 " + s.Damage.ToString("0") + (next==null ? "" : "→" + next.Damage.ToString("0"));'
$taskOldStat='            if(kind==TowerKind.Observer)return "표식 적 우선 · 타격 시 표식 3초";'
if($taskHud.Replace($taskNewField+"`n",'').Replace($taskNewResonance,$taskOldResonance).Replace($taskNewStat,$taskOldStat) -cne $taskBaseline){throw 'HUD changed beyond observer information'}
$taskFont=Get-Content TestResults/observer-menu-font-v0205.json -Raw|ConvertFrom-Json
if($taskFont.labels.Count -ne 4){throw 'Font report differs'}
for($taskI=0;$taskI -lt 4;$taskI++){if($taskFont.widths[$taskI] -gt 240 -or $taskFont.heights[$taskI] -gt $(if($taskI -lt 2){22}else{18})){throw 'Menu font wraps'}}
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$taskReference=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) 'TestResults/v0204-observer-relaunch.sav')))
$taskHash='c75400f9f8b77db15b3b0c07ab63298e116bc966ae052bcabdc444b7925d4981'
$taskSaves=@(foreach($taskPhase in @('after-update','abandon','relaunch')){
    $taskPath="TestResults/v0205-observer-$taskPhase.sav";$taskP=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Join-Path (Get-Location) $taskPath)))
    if(-not $taskP.Valid() -or (Get-FileHash $taskPath).Hash -ne $taskHash -or (Get-Item $taskPath).Length -ne 324){throw 'Save bytes changed'}
    foreach($taskField in $taskP.GetType().GetFields()){if(($taskField.GetValue($taskP)|ConvertTo-Json -Compress) -cne ($taskField.GetValue($taskReference)|ConvertTo-Json -Compress)){throw 'Saved public fields differ'}}
    [ordered]@{phase=$taskPhase;valid=$true;bytes=324;sha256=$taskHash;magic=$taskP.MagicCores;stars=$taskP.StarsTotal;loadout=$taskP.Loadout;reward=$taskP.LastReward;rewardRun=$taskP.LastRewardRun;pending=$taskP.PendingRun}
})
$taskSaves|ConvertTo-Json -Depth 4|Set-Content TestResults/v0205-observer-save-check.json
$taskLog=Get-Content TestResults/v0205-observer-logcat-final.txt
$taskPid=@($taskLog|Where-Object {$_ -match '^\d\d-\d\d \S+\s+7814\s+'})
$taskFirst=($taskPid|Where-Object {$_ -match 'GATEFALL PERF'}|Select-Object -First 1).Substring(0,18)
$taskEF=@($taskPid|Where-Object {$_.Substring(0,18) -ge $taskFirst -and $_ -match '\s[EF]\s'})
$taskSurface=@($taskEF|Where-Object {$_ -match 'E Surface\s*:'});$taskOther=@($taskEF|Where-Object {$_ -notmatch 'E Surface\s*:'})
$taskCounters=@(foreach($taskLine in $taskPid){if($taskLine -match 'shots=([\d,]+) hits=([\d,]+)'){[pscustomobject]@{shots=[int]($Matches[1].Split(',')[9]);hits=[int]($Matches[2].Split(',')[9])}}})
$taskAnr=@(Get-Content TestResults/v0205-observer-events-final.txt|Where-Object {$_ -match 'am_anr:.*com.gatefall.prototype'}).Count
$taskFatal=@($taskPid|Where-Object {$_ -match '\sF\s'}).Count
$taskWatchdog=@($taskLog|Where-Object {$_ -match 'W Watchdog: \*\*\* WATCHDOG KILLING SYSTEM PROCESS'}).Count
if($taskAnr -ne 0 -or $taskFatal -ne 0 -or $taskWatchdog -ne 0 -or (Get-Content TestResults/v0205-system-final.txt).Trim() -ne '580'){throw 'Critical diagnostic needs review'}
$taskShots=($taskCounters|Measure-Object shots -Maximum).Maximum;$taskHits=($taskCounters|Measure-Object hits -Maximum).Maximum
if($taskShots -lt 15 -or $taskHits -lt 15){throw 'Observer actual fire missing'}
foreach($taskLevel in @(1,2)){foreach($taskWidth in @(2400,1920,1280)){if(-not(Test-Path "Docs/QA/v050/v0205-observer-lv$taskLevel-$taskWidth.png")){throw 'Missing one of six level/resolution captures'}}}
foreach($taskShot in @('card30','live-a','live-b','abandon-city','relaunch-city')){if(-not(Test-Path "Docs/QA/v050/v0205-observer-$taskShot.png")){throw 'Missing functional evidence'}}
[ordered]@{created=(Get-Date -Format o);task=206;version='0.20.5';code=62;bytes=$taskMeta.bytes;sha256=$taskMeta.sha256;
    productChanges=$taskChanged;allSixCoreFilesUnchanged=$true;inputPanelSizeAndOtherTowerUiUnchanged=$true;newBattleBalance=$false;
    originalIssueReproducedInVersion='0.20.4';damageBeforeAfter='36->48 at Lv1;48 at Lv2';firstCard='Range';observerRange=5.7;
    actualResolutions=@('2400x1080/safe136','1920x1080/safe109','1280x720/safe73');levelsVerified=@(1,2);fontMaximumWidth=($taskFont.widths|Measure-Object -Maximum).Maximum;
    liveClock='1:17->1:21';liveCredits='225->233';liveRangeTargets='27->26';finalTowers=4;reconstructedSpend=375;
    observerShots=$taskShots;observerHits=$taskHits;wholeBattleCompleted=$false;newReward=0;pastReward36AndIdPreserved=$true;saveSha256=$taskHash;
    preUpdateReference='Prior verified 0.20.4 relaunch profile; immediate pre-update pull failed during ADB reconnect, no before file was fabricated';
    currentAppPid=7814;observedFrameworkPid=580;postFirstPerfSurface=$taskSurface.Count;postFirstPerfOtherEF=$taskOther.Count;capturedAppFatal=0;capturedPackageAnr=0;capturedWatchdogKills=0;
    diagnosticIssueResolved=$false;physicalDeviceTested=$false;humanFunEvaluated=$false;iosTested=$false;audioTested=$false;
    cleanup=$(if(Test-Path TestResults/qa-cleanup-v0205-observer.json){Get-Content TestResults/qa-cleanup-v0205-observer.json -Raw|ConvertFrom-Json}else{$null});passed=$true
}|ConvertTo-Json -Depth 6|Set-Content TestResults/delivery-consistency-v0205.json
Write-Output "PASS: observer information only /six Android menus/LIVE/save/install; Surface=$($taskSurface.Count),otherEF=$($taskOther.Count)"
