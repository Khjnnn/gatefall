$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=Join-Path $root TestResults/chapter2-implementation
Add-Type -Path (Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName
$rows=@()
foreach($local in 1..6){
    $index=13+$local;$stage=[Gatefall.Core.CampaignCatalog]::Stages[$index]
    $log=Get-Content "$out/native-s$local-last-run.txt" -Raw
    $p=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes("$out/native-s$local-result.sav"))
    if($log -notmatch 'Gatefall 0\.30\.0\s+Won elapsed=([\d.]+) core=([\d.]+)' -or [double]$Matches[2] -le 0){throw "Native $local did not win current APK"}
    $seconds=[double]$Matches[1];$core=[double]$Matches[2]
    if($log -notmatch 'allSpawned=True spawned=(\d+) remainingEnemies=0 bossDefeated=False' -or [int]$Matches[1] -ne ($stage.Counts|Measure-Object -Sum).Sum){throw "Native $local finite-wave victory mismatch"}
    $spawned=[int]$Matches[1]
    if($log -notmatch ('Context stage=2-'+$local+' mode=Normal seed=(\d+)')){throw "Native $local context missing"};$seed=[int]$Matches[1]
    if($log -notmatch 'Supplies=5 supplyCredits=500 cardOffers=3 catalystsGranted=2'){throw "Native $local economy cap mismatch"}
    if($p.PendingRun -ne '' -or $p.PendingStage -ne -1 -or $p.PendingCrisis -or -not $p.FirstCleared[$index] -or $p.LastReward -ne $stage.FirstReward){throw "Native $local result/profile mismatch"}
    if($local -eq 5 -and $log -notmatch '-> Barrage'){throw 'Native2-5 barrage creation missing'}
    if($local -eq 6 -and ($log -notmatch '-> Barrage' -or $log -notmatch '-> Seal')){throw 'Native2-6 new capstone route missing'}
    $rows+=@{stage="2-$local";seed=$seed;seconds=$seconds;core=$core;spawned=$spawned;stars=$p.BestStars[$index];reward=$p.LastReward;profileValid=$p.Valid();profileSha256=(Get-FileHash "$out/native-s$local-result.sav").Hash.ToLowerInvariant()}
}
$apk=Get-Content "$root/TestResults/android-apk-results.json" -Raw|ConvertFrom-Json
$elf=Get-Content "$out/elf-alignment.json" -Raw|ConvertFrom-Json
$installed=(Get-FileHash "$out/installed-base.apk").Hash.ToLowerInvariant()
if($installed -cne $apk.sha256 -or $elf.apkSha256 -cne $apk.sha256 -or $elf.status -cne 'PASS'){throw 'Installed/APK/ELF receipts differ'}
$final=[IO.File]::ReadAllBytes("$out/native-s6-result.sav")
$cold=[IO.File]::ReadAllBytes("$out/native-final-cold.sav")
if([Convert]::ToBase64String($final) -cne [Convert]::ToBase64String($cold)){throw 'Final cold save differs'}
$legacy=(Get-FileHash "$out/native-legacy-backup.sav").Hash
if($legacy -cne (Get-FileHash "$out/native-before-v2.sav").Hash){throw 'Legacy original backup differs'}
[ordered]@{status='PASS';version=$apk.version;code=$apk.versionCode;sha256=$apk.sha256;bytes=$apk.bytes;native=$rows;installedHashMatches=$true;legacyBackupExact=$true;finalColdSaveExact=$true;scope='Six normal Android emulator battles and listed actions; not physical device, iOS, human fun, audio or an FPS benchmark';remaining='Crisis2-5 pressure/placement balance and human/physical-device review'}|ConvertTo-Json -Depth 8|Set-Content "$out/delivery.json" -Encoding utf8
Write-Output 'PASS six normal Android operations, new barrage/capstone route, finite victories, saved rewards, exact install and cold-save receipts.'
