param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOutput='TestResults/stage12-rupture-v0202-models.json'
if(-not $UseRecorded){
    foreach($taskFile in (Get-Content TestResults/source-hashes-v0202.json -Raw|ConvertFrom-Json)){
        if((Get-FileHash -LiteralPath $taskFile.path).Hash -ne $taskFile.sha256){throw 'Product changed during selected review'}
    }
    if(Test-Path -LiteralPath $taskOutput){throw 'Preserve prior model evidence; use -UseRecorded for summary'}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'Tests/Stage12RupturePositionReview.cs'))
    $taskActions=@(foreach($taskRow in (Get-Content TestResults/v0202-stage12-native-purchase-ledger.json -Raw|ConvertFrom-Json)){
        [Gatefall.Tests.Stage12RupturePositionReview+Action]@{Time=$taskRow.time;Command=$taskRow.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskRow.kind);Site=$taskRow.site}
    })
    if($taskActions.Count -ne 28){throw 'Native-derived action fixture differs'}
    $taskRows=@(foreach($taskSeed in @(7,19,41)){foreach($taskSite in @(22,6,10)){
        $taskR=[Gatefall.Tests.Stage12RupturePositionReview]::Run($taskSeed,$taskSite,$taskActions)
        if($taskR.State -notin @('Won','Lost')){throw 'Incomplete model'}
        $taskR
    }})
    [pscustomobject]@{created=(Get-Date).ToString('o');task=195;sourceVersion='0.20.2';newBaselineModels=3;newPositionModels=6;nativeReplay=$false;policy='28 native-receipt action times; retry scheduled purchase until affordable, no reinvestment. First card Ballistic preferred, then Range preferred, else first offered; no Hunter movement/skill. Swap every6/10 and22 build/upgrade action; anchor21/partner16 fixed.';rows=$taskRows;productChanged=$false;newApk=$false;newAvd=$false}|ConvertTo-Json -Depth 10|Set-Content $taskOutput
}
$taskData=Get-Content $taskOutput -Raw|ConvertFrom-Json
if($taskData.rows.Count -ne 9){throw 'Wrong limited condition count'}
$taskComparison=@(foreach($taskSeed in @(7,19,41)){
    $taskBase=@($taskData.rows|Where-Object {$_.Seed -eq $taskSeed -and $_.RuptureSite -eq 22})[0]
    foreach($taskR in @($taskData.rows|Where-Object {$_.Seed -eq $taskSeed})){
        [pscustomobject]@{seed=$taskSeed;ruptureSite=$taskR.RuptureSite;state=$taskR.State;core=$taskR.Core;time=$taskR.Time;spent=$taskR.Spent;credits=$taskR.Credits;actions=$taskR.CompletedActions;cards=$taskR.Cards;cardsEqual=(($taskR.Cards|ConvertTo-Json -Compress) -ceq ($taskBase.Cards|ConvertTo-Json -Compress));ruptureDamage=$taskR.KindDamage[2];arcDamage=$taskR.KindDamage[3];spearDamage=$taskR.KindDamage[0];fired=$taskR.Observation.FiredSites;frequent=$taskR.Observation.FrequentFiringSites;peakRecent3s=$taskR.Observation.PeakActive3Seconds;ruptureShots=$taskR.Observation.KindShots[2];peakAlive=$taskR.Observation.PeakAlive;coreDamageEvents=$taskR.Observation.CoreDamageEvents;maxPurchaseDelay=$taskR.MaximumPurchaseDelay}
    }
})
[pscustomobject]@{created=(Get-Date).ToString('o');task=195;sourceVersion=$taskData.sourceVersion;models=9;baseline=3;candidates=6;rows=$taskComparison;nativeSeedAndFirstCardUnknown=$true;equalWorldPositions=$false;initialSpearPositionAlsoChanges=$true;individualDamageNotTeamAddedDamage=$true;productChanged=$false;newApk=$false;newAvd=$false}|ConvertTo-Json -Depth 7|Set-Content TestResults/stage12-rupture-v0202-comparison.json
$taskComparison|Format-Table seed,ruptureSite,state,core,time,spent,cardsEqual,ruptureShots,ruptureDamage,fired,frequent,peakRecent3s -AutoSize
