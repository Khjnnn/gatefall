param([ValidateSet(11,12)][int]$Stage=11,[ValidateSet(7,16)][int]$Cap=7,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskPrefix=if($Stage -eq 11 -and $Cap -eq 7){'marker-cap'}else{"marker-stage$Stage-cap$Cap"}
$taskOutput="TestResults/$taskPrefix-v0202.json"
if($UseRecorded){Get-Content -LiteralPath $taskOutput -Raw;exit}
if(Test-Path -LiteralPath $taskOutput){throw 'Preserve recorded limited-investment models'}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0202.json -Raw|ConvertFrom-Json)){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product changed'}}
foreach($taskCore in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){
    $taskPrior=Get-Content ('TestResults/marker-pulse-v0201-review/window16/'+$taskCore.Name) -Raw
    if($taskPrior.Replace("`r`n","`n") -cne ([IO.File]::ReadAllText($taskCore.FullName)).Replace("`r`n","`n")){throw 'Prior16-tower core parity lost'}
}
$taskRoot=Join-Path (Get-Location) "TestResults/$taskPrefix-v0202-review"
if(Test-Path -LiteralPath $taskRoot){throw 'Preserve observer snapshot'}
New-Item -ItemType Directory -Path $taskRoot|Out-Null
$taskOriginal=Get-Content TestResults/marker-support-v0201-review/MarkerBattleReview.cs -Raw
$taskBefore='if(!acted && next<slots.Count && g.Credits>=40)'
$taskAfter="if(!acted && next<slots.Count && g.Credits>=40 && g.Towers.Count<$Cap)"
if(([regex]::Matches($taskOriginal,[regex]::Escape($taskBefore))).Count -ne 1){throw 'Tower-cap insertion ambiguous'}
$taskText=$taskOriginal.Replace($taskBefore,$taskAfter)
if($taskText.Replace($taskAfter,$taskBefore) -cne $taskOriginal){throw 'Cap changed other policies'}
$taskObserver=Join-Path $taskRoot 'MarkerBattleReview.cs'
[IO.File]::WriteAllText($taskObserver,$taskText)
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+$taskObserver)
$taskBaseline=if($Stage -eq 11){Get-Content TestResults/marker-support-v0201-window16.json -Raw|ConvertFrom-Json}elseif($Cap -eq 7){Get-Content TestResults/marker-stage12-cap16-v0202.json -Raw|ConvertFrom-Json}else{$null}
$taskRun=[Gatefall.Tests.MarkerBattleReview].GetMethod('Run',[Reflection.BindingFlags]'Static,NonPublic')
$taskRows=@(foreach($taskSeed in @(7,19,41)){foreach($taskPlan in @('spears','marker','cluster_spears','cluster_marker')){
    $taskSummary=$taskRun.Invoke($null,@([int]($Stage-1),[string]$taskPlan,[int]$taskSeed,[bool]$true))
    $taskG=[Gatefall.Tests.MarkerBattleReview]::Last
    if($taskG.State.ToString() -notin @('Won','Lost') -or $taskG.Towers.Count -gt $Cap){throw 'Incomplete/out-of-policy result'}
    $taskPrevious=@($taskBaseline.rows|Where-Object {$_.seed -eq $taskSeed -and $_.plan -eq $taskPlan})
    if($null -ne $taskBaseline -and $taskPrevious.Count -ne 1){throw 'No unique prior16-tower condition'}
    $taskPurchases=@($taskG.Telemetry.Purchases.ToArray())
    if($null -ne $taskBaseline){for($taskI=0;$taskI -lt $taskPurchases.Count;$taskI++){if($taskPurchases[$taskI] -cne $taskPrevious[0].purchases[$taskI]){throw 'Common purchase prefix differs'}}}
    [pscustomobject]@{seed=$taskSeed;plan=$taskPlan;state=$taskG.State.ToString();time=$taskG.Elapsed;core=$taskG.MinimumCoreHealth;hunter=$taskG.PlayerHealth;spent=$taskG.Telemetry.CreditsSpent;credits=$taskG.Credits;towers=$taskG.Towers.Count;kills=$taskG.Kills;spawned=$taskG.Telemetry.Spawned;remaining=$taskG.Enemies.Count;allSpawned=$taskG.AllWavesSpawned;markedHits=$taskG.Telemetry.MarkedHits;effectiveMarkBonus=$taskG.Telemetry.MarkBonusDamage;kindDamage=$taskG.Telemetry.TowerDamage;cards=(,[int[]]$taskG.Cards.Clone())[0];purchases=$taskPurchases;prior16Time=$(if($taskPrevious.Count){$taskPrevious[0].time});prior16Core=$(if($taskPrevious.Count){if($Stage -eq 11){$taskPrevious[0].minimumCore}else{$taskPrevious[0].core}});prior16Spend=$(if($taskPrevious.Count){$taskPrevious[0].spent});prior16Credits=$(if($taskPrevious.Count){$taskPrevious[0].credits});prior16MarkedHits=$(if($taskPrevious.Count){$taskPrevious[0].markedHits});prior16EffectiveBonus=$(if($taskPrevious.Count){$taskPrevious[0].effectiveMarkBonus});commonPurchasePrefixEqual=$(if($null -ne $taskBaseline){$true}else{$null});rawSummary=$taskSummary}
}})
[pscustomobject]@{created=(Get-Date).ToString('o');sourceVersion='0.20.2';stage=$Stage;cap=$Cap;newModels=12;prior16ModelsUsed=$(if($null -ne $taskBaseline){12}else{0});coreParityWithReviewedSource=$true;onlyPurchaseCapChanged=$true;markerUnlocked=$true;observerUnlocked=$false;growthResearch=0;firstOfferCardPolicy=$true;prior16CardCountsNotRecorded=($Stage -eq 11);noSkillCallsInHarness=$true;remainingCreditsNotDeleted=$true;pricesNotEqualized=$true;productChanged=$false;newApk=$false;newAvd=$false;humanPlaytested=$false;rows=$taskRows}|ConvertTo-Json -Depth 8|Set-Content $taskOutput
$taskRows|Group-Object plan|ForEach-Object{[pscustomobject]@{plan=$_.Name;wins=@($_.Group|Where-Object state -eq Won).Count;minCore=($_.Group|Measure-Object core -Minimum).Minimum;maxTime=($_.Group|Measure-Object time -Maximum).Maximum;spent=($_.Group|Measure-Object spent -Average).Average;credits=($_.Group|Measure-Object credits -Average).Average;bonus=[math]::Round(($_.Group|Measure-Object effectiveMarkBonus -Average).Average,1)}}|Format-Table
