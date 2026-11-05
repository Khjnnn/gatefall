param([ValidateSet('baseline','w10')][string]$Variant='baseline',[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$output="TestResults/courts-opening-v0230-$Variant.json"
if($UseRecorded){Get-Content $output -Raw;exit}
if(Test-Path $output){throw 'Preserve recorded opening models; use -UseRecorded'}
foreach($r in(Get-Content TestResults/source-hashes-v0230.json -Raw|ConvertFrom-Json)){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Verified product changed'}}
$source=if($Variant -eq 'baseline'){'TestResults/courts-flank-v0230-review/baseline-core'}else{'TestResults/courts-pulse-v0230-original-w10-review/candidate-core'}
$root=Join-Path (Get-Location) ("TestResults/courts-opening-v0230-${Variant}-review")
if(Test-Path $root){throw 'Preserve observation snapshot'}
New-Item -ItemType Directory -Path $root|Out-Null
foreach($file in Get-ChildItem $source -Filter '*.cs'){Copy-Item -LiteralPath $file.FullName -Destination $root}
$observer=Get-Content TestResults/courts-flank-v0230-review/AlleyDensityReview.cs -Raw
$pairs=@(
    @('public static class AlleyDensityReview','public static class CourtOpeningReview'),
    @('if(g.Elapsed>=nextBuy) {','if(g.Elapsed>=nextBuy && g.Elapsed<0) {'),
    @('g.Tick(.05f,dodge?new V2(1,0):new V2());g.UseSkill();','g.Tick(.05f,new V2());'),
    @('foreach(string policy in new[]{"many","mixed","upgrade"})','foreach(string policy in new[]{"many"})')
)
foreach($pair in $pairs){if(-not $observer.Contains($pair[0])){throw 'Opening observation anchor differs'};$observer=$observer.Replace($pair[0],$pair[1])}
Set-Content -LiteralPath (Join-Path $root CourtOpeningReview.cs) -Value $observer -NoNewline
Add-Type -Path (Get-ChildItem $root -Filter '*.cs').FullName
$rows=[Gatefall.Tests.CourtOpeningReview]::Models(4)
[ordered]@{created=(Get-Date -Format o);task=249;variant=$Variant;newModels=$rows.Count;initialOnly=7;spentCap=280;automaticHunterAttackRemains=$true;manualSkillMovementSupportCalls=$false;reinforcedBenchmarkIsSeparateAutomaticSkillPolicy=$true;productChanged=$false;newApkAvdNative=$false;models=$rows}|ConvertTo-Json -Depth 9|Set-Content $output
if(@($rows|Where-Object {$_.Towers -ne 7 -or $_.Spent -ne 280 -or $_.StartingCredits -ne 280}).Count){throw 'Opening purchase cap differs; raw preserved'}
$rows|Select-Object Seed,Policy,State,Core,Time,Towers,Spent,Kills,Spawned,Remaining,CoreDamageEvents,IdleSeconds,@{n='Cards';e={$_.Cards -join '/'}}|Format-Table -AutoSize
