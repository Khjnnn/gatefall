param([switch]$UseRecorded,[switch]$Seed19)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$part=if($Seed19){'seed19'}else{'pilot'}
$output="TestResults/north-front-v0230-$part.json"
if($UseRecorded){Get-Content -LiteralPath $output -Raw;exit}
if(Test-Path -LiteralPath $output){throw 'Recorded models exist; use -UseRecorded'}
foreach($r in(Get-Content TestResults/source-hashes-v0230.json -Raw|ConvertFrom-Json)){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Current product differs from verified v0230'}}
$root=Join-Path (Get-Location) "TestResults/north-front-v0230-review/$part"
New-Item -ItemType Directory -Path $root -Force|Out-Null
foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $file.FullName -Destination $root}
Copy-Item Tests/NorthMarkerPositionReview.cs $root
Add-Type -Path (Get-ChildItem $root -Filter '*.cs').FullName
$fixture=@(foreach($r in(Get-Content TestResults/north-marker-v0230-review/pilot/commands.json -Raw|ConvertFrom-Json)){
    $c=[Gatefall.Tests.NorthMarkerPositionReview+Command]::new();$c.Time=$r.Time;$c.Action=$r.Action;$c.Kind=[Gatefall.Core.TowerKind]$r.Kind;$c.Site=$r.Site
    if($c.Site -eq 12){if($c.Action -ne 'build' -or $c.Kind -ne [Gatefall.Core.TowerKind]::Autocannon -or [math]::Abs($c.Time-98.3) -gt .0001){throw 'Expected only98.3 spear12'};$c.Site=0};$c
})
if(@($fixture|Where-Object Site -eq 0).Count -ne 1 -or @($fixture|Where-Object Site -eq 12).Count){throw 'Sole spare spear replacement differs'}
$fixture|ConvertTo-Json -Depth 5|Set-Content (Join-Path $root commands.json)
$seeds=if($Seed19){[int[]]@(19)}else{[int[]]@(7,41)}
$rows=[Gatefall.Tests.NorthMarkerPositionReview]::Models($fixture,$seeds,[int[]]@(21))
[ordered]@{created=(Get-Date -Format o);task=245;newModels=$rows.Count;baselineSource='north-marker-v0230 controlMarker21 recorded rows';onlyChangedPurchase='98.3s build Autocannon12 ->0';markerSite=21;modelTowers=20;spend=1140;nativeExactReplay=$false;productChanged=$false;newApkAvdNative=$false;models=$rows}|ConvertTo-Json -Depth 8|Set-Content $output
if(@($rows|Where-Object {$_.Spent -ne 1140 -or $_.CompletedOrders -ne 28 -or $_.Towers -ne 20}).Count){throw 'Actual spend/order completion differs; raw preserved'}
$rows|Select-Object Seed,State,Core,Time,Spent,Credits,FinalFiredTowers,PeakRecent3,PeakEnemies,MarkedHits,MarkBonus,@{n='Spear0Shots';e={$_.SiteShots[0]}},@{n='Cards';e={$_.Cards -join '/'}}|Format-Table -AutoSize
