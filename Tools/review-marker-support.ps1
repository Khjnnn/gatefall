param([ValidateSet('baseline','window16')][string]$Candidate='baseline')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOutput="TestResults/marker-support-v0201-$Candidate.json"
if(Test-Path -LiteralPath $taskOutput){throw 'Preserve recorded results'}
$taskRoot=Join-Path (Get-Location) 'TestResults/marker-support-v0201-review'
New-Item -ItemType Directory -Path $taskRoot -Force|Out-Null
$taskOriginal=[IO.File]::ReadAllText((Join-Path (Get-Location) 'Tests/MarkerBattleReview.cs'))
$taskTransforms=@(
    @('public static class MarkerBattleReview {','public static class MarkerBattleReview { public static DefenseSimulation Last;'),
    @('ObserverUnlocked=true','ObserverUnlocked=false'),
    @('var t=g.Telemetry;','Last=g;var t=g.Telemetry;')
)
$taskText=$taskOriginal
foreach($taskT in $taskTransforms){if(([regex]::Matches($taskText,[regex]::Escape($taskT[0]))).Count -ne 1){throw 'Observer transform ambiguous'};$taskText=$taskText.Replace($taskT[0],$taskT[1])}
$taskReverse=$taskText
for($taskI=$taskTransforms.Count-1;$taskI -ge 0;$taskI--){$taskReverse=$taskReverse.Replace($taskTransforms[$taskI][1],$taskTransforms[$taskI][0])}
if($taskReverse -cne $taskOriginal){throw 'Observer changed purchase policy'}
$taskObserver=Join-Path $taskRoot 'MarkerBattleReview.cs'
if(Test-Path $taskObserver){if([IO.File]::ReadAllText($taskObserver) -cne $taskText){throw 'Stored observer differs'}}else{[IO.File]::WriteAllText($taskObserver,$taskText)}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0201.json -Raw|ConvertFrom-Json)){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product changed during review'}}
$taskCore=if($Candidate -eq 'baseline'){'TestResults/marker-courts-v0201-review/baseline'}else{'TestResults/marker-pulse-v0201-review/window16'}
Add-Type -Path @((Get-ChildItem -LiteralPath $taskCore -Filter '*.cs').FullName+$taskObserver)
$taskRun=[Gatefall.Tests.MarkerBattleReview].GetMethod('Run',[Reflection.BindingFlags]'Static,NonPublic')
$taskRows=@(foreach($taskSeed in @(7,19,41)){foreach($taskPlan in @('spears','marker','cluster_spears','cluster_marker')){
    $taskRaw=$taskRun.Invoke($null,@([int]10,[string]$taskPlan,[int]$taskSeed,[bool]$true))
    $taskG=[Gatefall.Tests.MarkerBattleReview]::Last
    if($taskG.State.ToString() -notin @('Won','Lost')){throw 'Incomplete battle; no survival-timer result accepted'}
    [pscustomobject]@{seed=$taskSeed;plan=$taskPlan;state=$taskG.State.ToString();time=$taskG.Elapsed;minimumCore=$taskG.MinimumCoreHealth;hunter=$taskG.PlayerHealth;credits=$taskG.Credits;spent=$taskG.Telemetry.CreditsSpent;towers=$taskG.Towers.Count;kills=$taskG.Kills;spawned=$taskG.Telemetry.Spawned;allSpawned=$taskG.AllWavesSpawned;remaining=$taskG.Enemies.Count;fusions=$taskG.Telemetry.Fusions;markedHits=$taskG.Telemetry.MarkedHits;effectiveMarkBonus=$taskG.Telemetry.MarkBonusDamage;kindDamage=$taskG.Telemetry.TowerDamage;purchases=$taskG.Telemetry.Purchases.ToArray();finalTowers=@($taskG.Towers|Select-Object Socket,Kind,Level);supplies=$taskG.SuppliesGranted;cards=$taskG.Telemetry.CardOffers;catalysts=$taskG.Telemetry.CoresGranted;rawSummary=$taskRaw}
}})
[pscustomobject]@{created=(Get-Date).ToString('o');candidate=$Candidate;newModels=12;stage=11;markerUnlocked=$true;observerUnlocked=$false;researchAndGrowth=0;noUseSkillInThisHarness=$true;purchasePolicy='Existing MarkerBattleReview: same positions/action opportunity rules per paired plan; marker costs45/40 versus spear40/30 so actual spend and affordable purchase times differ. No equal-spend causal claim.';sourceObserverReverseMatches=$true;rows=$taskRows;humanPlaytested=$false}|ConvertTo-Json -Depth 8|Set-Content $taskOutput
$taskRows|Group-Object plan|ForEach-Object{[pscustomobject]@{candidate=$Candidate;plan=$_.Name;wins=@($_.Group|Where-Object state -eq Won).Count;minCore=($_.Group|Measure-Object minimumCore -Minimum).Minimum;maxTime=($_.Group|Measure-Object time -Maximum).Maximum;minTowers=($_.Group|Measure-Object towers -Minimum).Minimum;maxTowers=($_.Group|Measure-Object towers -Maximum).Maximum;meanMarkedHits=[math]::Round(($_.Group|Measure-Object markedHits -Average).Average,1);meanEffectiveBonus=[math]::Round(($_.Group|Measure-Object effectiveMarkBonus -Average).Average,1)}}|Format-Table
