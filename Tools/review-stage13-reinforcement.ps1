param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOutput='TestResults/stage13-reinforcement-v0204-models.json'
$taskPrior=Get-Content TestResults/stage13-investment-v0204-models.json -Raw|ConvertFrom-Json
if(-not $UseRecorded){
    if(Test-Path $taskOutput){throw 'Preserve recorded evidence; use -UseRecorded'}
    foreach($taskR in (Get-Content TestResults/source-hashes-v0204.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed'}}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'Tests/Stage13MarkerPositionReview.cs')+(Join-Path (Get-Location) 'Tests/Stage13InvestmentReview.cs'))
    $taskActions=@(foreach($taskR in (Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json)){
        [Gatefall.Tests.Stage13MarkerPositionReview+Action]@{Time=$taskR.time;Command=$taskR.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskR.kind);Site=$taskR.site}
    })
    $taskRows=@(foreach($taskSeed in @(7,19,41)){foreach($taskPlan in @('north465','west465')){
        [pscustomobject]@{plan=$taskPlan;battle=[Gatefall.Tests.Stage13InvestmentReview]::Run($taskSeed,$taskPlan,$taskActions)}
    }})
    [ordered]@{created=(Get-Date -Format o);task=204;newModels=6;reusedConditions=9;productChanged=$false;rows=$taskRows}|ConvertTo-Json -Depth 10|Set-Content $taskOutput
}
$taskData=Get-Content $taskOutput -Raw|ConvertFrom-Json
if($taskData.rows.Count -ne 6){throw 'Selected model count differs'}
$taskRows=@(foreach($taskRow in $taskData.rows){
    $taskR=$taskRow.battle;$taskBase=@($taskPrior.rows|Where-Object {$_.battle.Seed -eq $taskR.Seed -and $_.plan -eq 'spear465'})[0].battle
    $taskCardsEqual=(($taskR.Cards|ConvertTo-Json -Compress) -ceq ($taskBase.Cards|ConvertTo-Json -Compress))
    if(-not $taskCardsEqual -or $taskR.Spent -ne 465 -or $taskR.Towers -ne 9 -or $taskR.CompletedActions -ne 11 -or $taskR.State -notin @('Won','Lost')){throw 'Matching policy validation failed'}
    for($taskI=0;$taskI -lt 10;$taskI++){if($taskR.Purchases[$taskI] -cne $taskBase.Purchases[$taskI]){throw 'Common predecision receipt differs'}}
    [ordered]@{seed=$taskR.Seed;plan=$taskRow.plan;state=$taskR.State;core=$taskR.Core;time=$taskR.Time;spent=$taskR.Spent;credits=$taskR.Credits;towers=$taskR.Towers;
        cards=$taskR.Cards;cardsEqual=$true;prefixEqual=$true;maxPurchaseDelay=$taskR.MaximumPurchaseDelay;
        fired=$taskR.Observation.FiredSites;frequent=$taskR.Observation.FrequentFiringSites;peakRecent=$taskR.Observation.PeakActive3Seconds;
        coreDamageEvents=$taskR.Observation.CoreDamageEvents;strongCoreApproaches=$taskR.Observation.CoreApproachStrongTargets;
        spearDamage=$taskR.KindDamage[0];markerBonus=$taskR.MarkBonus;
        eastCore=$taskBase.Core;eastTime=$taskBase.Time;eastSpent=$taskBase.Spent;eastFired=$taskBase.Observation.FiredSites}
})
[ordered]@{created=(Get-Date -Format o);task=204;newModels=6;reusedConditions=9;actualEqualPrice40=$true;onlyNextSpearPositionChanged=$true;
    nativeReplay=$false;unusedCreditsPreserved=$true;productChanged=$false;newApk=$false;newAvd=$false;rows=$taskRows}|ConvertTo-Json -Depth 7|Set-Content TestResults/stage13-reinforcement-v0204-comparison.json
$taskRows|ForEach-Object {[pscustomobject]$_}|Format-Table seed,plan,state,core,time,spent,credits,fired,eastCore,coreDamageEvents -AutoSize
