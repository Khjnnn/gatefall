$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=Join-Path $root TestResults/chapter2-implementation
Add-Type -Path (Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName
$original=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes("$out/native-before-v2.sav"))
$grown=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes("$out/native-grown.sav"))
$battleApk=Get-Content "$out/v0300-validation/android-apk-results.json" -Raw|ConvertFrom-Json
$wonHeader='Gatefall '+[regex]::Escape($battleApk.version)+'\s+Won elapsed=([\d.]+) core=([\d.]+)'
$rewardTotal=0
$rows=@()
foreach($local in 1..6){
    $index=13+$local;$stage=[Gatefall.Core.CampaignCatalog]::Stages[$index]
    $log=Get-Content "$out/native-s$local-last-run.txt" -Raw
    $p=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes("$out/native-s$local-result.sav"))
    if($null -eq $p -or -not $p.Valid()){throw "Native $local invalid profile"}
    if($log -notmatch $wonHeader -or [double]$Matches[2] -le 0){throw "Native $local did not win recorded battle APK"}
    $seconds=[double]$Matches[1];$core=[double]$Matches[2]
    if($log -notmatch 'allSpawned=True spawned=(\d+) remainingEnemies=0 bossDefeated=False' -or [int]$Matches[1] -ne ($stage.Counts|Measure-Object -Sum).Sum){throw "Native $local finite-wave victory mismatch"}
    $spawned=[int]$Matches[1]
    if($log -notmatch ('Context stage=2-'+$local+' mode=Normal seed=(\d+)')){throw "Native $local context missing"};$seed=[int]$Matches[1]
    if($log -notmatch 'Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2'){throw "Native $local economy cap mismatch"}
    if($p.PendingRun -ne '' -or $p.PendingStage -ne -1 -or $p.PendingCrisis -or -not $p.FirstCleared[$index] -or $p.LastReward -ne $stage.FirstReward){throw "Native $local result/profile mismatch"}
    $rewardTotal+=$stage.FirstReward
    if($p.MagicCores -ne $grown.MagicCores+$rewardTotal){throw "Native $local currency differs from actual growth purchases plus first rewards"}
    foreach($field in @('Growth','Research')){if(($p.$field -join ',') -cne ($grown.$field -join ',')){throw "Native $local changed $field during combat"}}
    foreach($field in @('BestStars','CrisisStars','BestErosion','FirstCleared')){
        if(($p.$field[0..13] -join ',') -cne ($original.$field[0..13] -join ',')){throw "Native $local changed original chapter $field"}
    }
    if($local -eq 5 -and $log -notmatch '-> Barrage'){throw 'Native2-5 barrage creation missing'}
    if($local -eq 6 -and ($log -notmatch '-> Barrage' -or $log -notmatch '-> Seal')){throw 'Native2-6 new capstone route missing'}
    $rows+=@{stage="2-$local";seed=$seed;seconds=$seconds;core=$core;spawned=$spawned;stars=$p.BestStars[$index];reward=$p.LastReward;profileValid=$p.Valid();profileSha256=(Get-FileHash "$out/native-s$local-result.sav").Hash.ToLowerInvariant()}
}
$apk=Get-Content "$root/TestResults/android-apk-results.json" -Raw|ConvertFrom-Json
$baseline=Get-Content "$out/v0300-product-source.json" -Raw|ConvertFrom-Json
$allowed=@('Assets/Scripts/Presentation/CampaignHud.cs','Assets/Scripts/Presentation/GatefallGame.cs')
$changed=@($baseline|Where-Object{(Get-FileHash (Join-Path $root $_.path)).Hash.ToLowerInvariant() -cne $_.sha256})
if((($changed.path|Sort-Object) -join ',') -cne (($allowed|Sort-Object) -join ',')){throw 'Unexpected gameplay source changes after recorded battles'}
if(@(Get-ChildItem "$root/Assets/Scripts/Core","$root/Assets/Scripts/Presentation" -Filter '*.cs' -Recurse).Count -ne $baseline.Count){throw 'Product source set changed'}
$ui=Get-Content "$out/native-ui-v0301.json" -Raw|ConvertFrom-Json
if($ui.version -cne $apk.version -or ($ui.cityWaveCountsVerified -join ',') -cne '6,7,8' -or -not $ui.updateAndColdSaveExact){throw 'Final UI/update observations incomplete'}
if((Get-Content "$out/unity-audit.txt" -Raw) -notmatch 'actual controller action dispatch through all6/7/8 waves'){throw 'Late-wave controller audit missing'}
$elf=Get-Content "$out/elf-alignment.json" -Raw|ConvertFrom-Json
$installed=(Get-FileHash "$out/installed-base.apk").Hash.ToLowerInvariant()
if($installed -cne $apk.sha256 -or $elf.apkSha256 -cne $apk.sha256 -or $elf.status -cne 'PASS'){throw 'Installed/APK/ELF receipts differ'}
$final=[IO.File]::ReadAllBytes("$out/native-s6-result.sav")
$cold=[IO.File]::ReadAllBytes("$out/native-final-cold.sav")
if([Convert]::ToBase64String($final) -cne [Convert]::ToBase64String($cold)){throw 'Final cold save differs'}
$legacy=(Get-FileHash "$out/native-legacy-backup.sav").Hash
if($legacy -cne (Get-FileHash "$out/native-before-v2.sav").Hash){throw 'Legacy original backup differs'}
if($legacy -cne (Get-FileHash "$out/native-final-legacy-backup.sav").Hash){throw 'Updated APK changed original legacy backup'}
[ordered]@{status='PASS';version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;battleVersion=$battleApk.version;battleApkSha256=$battleApk.sha256;native=$rows;finalVersionFullBattles=$false;finalVersionCityAndUpdateVerified=$true;combatCoreHashesMatchBattleVersion=$true;changedPresentationSources=$changed.path;lateWaveControllerAudit=$true;installedHashMatches=$true;legacyBackupExact=$true;legacyChapterProgressPreserved=$true;growthAndResearchPreserved=$true;firstRewardsExactlyAccounted=$true;finalColdSaveExact=$true;scope='Six normal Android emulator battles on0.30.0;0.30.1 has identical combat core, verified6/7/8-wave controller dispatch in Unity and actual Android city labels/update/cold saves. Not physical device, iOS, human fun, audio or an FPS benchmark';remaining='Crisis2-5 pressure/placement balance, Android late-wave calling during a full battle and human/physical-device review'}|ConvertTo-Json -Depth 8|Set-Content "$out/delivery.json" -Encoding utf8
Write-Output 'PASS recorded six Android battles and new fusions; final UI/controller fix, exact install/update/cold saves and original chapter preservation verified separately.'
