param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOutput='TestResults/stage13-investment-v0204-models.json'
$taskPrior=Get-Content TestResults/stage13-marker-v0204-models.json -Raw|ConvertFrom-Json
if(-not $UseRecorded){
    if(Test-Path $taskOutput){throw 'Preserve recorded conditions; use -UseRecorded'}
    foreach($taskR in (Get-Content TestResults/source-hashes-v0204.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed'}}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'Tests/Stage13MarkerPositionReview.cs')+(Join-Path (Get-Location) 'Tests/Stage13InvestmentReview.cs'))
    $taskActions=@(foreach($taskR in (Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json)){
        [Gatefall.Tests.Stage13MarkerPositionReview+Action]@{Time=$taskR.time;Command=$taskR.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskR.kind);Site=$taskR.site}
    })
    $taskRows=@(foreach($taskSeed in @(7,19,41)){foreach($taskPlan in @('hold425','spear465','arc465')){
        [pscustomobject]@{plan=$taskPlan;battle=[Gatefall.Tests.Stage13InvestmentReview]::Run($taskSeed,$taskPlan,$taskActions)}
    }})
    [ordered]@{created=(Get-Date -Format o);task=203;version='0.20.4';newModels=9;reusedHighInvestment=3;nativeReplay=$false;productChanged=$false;rows=$taskRows}|ConvertTo-Json -Depth 10|Set-Content $taskOutput
}
$taskData=Get-Content $taskOutput -Raw|ConvertFrom-Json
if($taskData.rows.Count -ne 9){throw 'Limited condition count differs'}
$taskRows=@(foreach($taskRow in $taskData.rows){
    $taskR=$taskRow.battle; $taskBase=@($taskPrior.rows|Where-Object {$_.Seed -eq $taskR.Seed -and $_.MarkerSite -eq 14})[0]
    $taskCardsEqual=(($taskR.Cards|ConvertTo-Json -Compress) -ceq ($taskBase.Cards|ConvertTo-Json -Compress))
    if(-not $taskCardsEqual -or $taskR.State -notin @('Won','Lost')){throw 'Cards/state require review'}
    for($taskI=0;$taskI -lt 10;$taskI++){if($taskR.Purchases[$taskI] -cne $taskBase.Purchases[$taskI]){throw 'Common predecision purchase prefix differs'}}
    $taskSpend=if($taskRow.plan -eq 'hold425'){425}else{465};$taskTowers=switch($taskRow.plan){hold425{8}spear465{9}arc465{7}}
    if($taskR.Spent -ne $taskSpend -or $taskR.Towers -ne $taskTowers){throw 'Actual branch budget/tower count differs'}
    [ordered]@{seed=$taskR.Seed;plan=$taskRow.plan;state=$taskR.State;core=$taskR.Core;time=$taskR.Time;spent=$taskR.Spent;credits=$taskR.Credits;towers=$taskR.Towers;
        cards=$taskR.Cards;cardsEqual=$taskCardsEqual;prefixEqual=$true;maxPurchaseDelay=$taskR.MaximumPurchaseDelay;
        fired=$taskR.Observation.FiredSites;frequent=$taskR.Observation.FrequentFiringSites;peakRecent=$taskR.Observation.PeakActive3Seconds;
        coreDamageEvents=$taskR.Observation.CoreDamageEvents;coreApproaches=$taskR.Observation.CoreApproachStrongTargets;
        markerBonus=$taskR.MarkBonus;arcDamage=$taskR.KindDamage[3];spearDamage=$taskR.KindDamage[0];
        prior16Time=$taskBase.Time;prior16Core=$taskBase.Core;prior16Spent=$taskBase.Spent}
})
[ordered]@{created=(Get-Date -Format o);task=203;newModels=9;reusedHighInvestment=3;equal465PricesAreActual=$true;unusedCreditsPreserved=$true;reinvestment=$false;
    policy='Shared native first10 purchases425 credits. At71.5s hold, new Spear23 for40, or Arc17/Binding16 fusion for40. Same card preference as202; no movement/skill.';
    exactNativeReplay=$false;productChanged=$false;newApk=$false;newAvd=$false;rows=$taskRows}|ConvertTo-Json -Depth 7|Set-Content TestResults/stage13-investment-v0204-comparison.json
$taskRows|ForEach-Object {[pscustomobject]$_}|Format-Table seed,plan,state,core,time,spent,credits,towers,fired,coreDamageEvents -AutoSize
