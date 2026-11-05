param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOutput='TestResults/stage13-marker-v0204-models.json'
if(-not $UseRecorded) {
    if(Test-Path -LiteralPath $taskOutput){throw 'Preserve prior models; use -UseRecorded'}
    foreach($taskR in (Get-Content TestResults/source-hashes-v0204.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed'}}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'Tests/Stage13MarkerPositionReview.cs'))
    $taskActions=@(foreach($taskR in (Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json)){
        [Gatefall.Tests.Stage13MarkerPositionReview+Action]@{Time=$taskR.time;Command=$taskR.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskR.kind);Site=$taskR.site}
    })
    if($taskActions.Count -ne 24){throw 'Native action policy differs'}
    $taskRows=@(foreach($taskSeed in @(7,19,41)){foreach($taskSite in @(14,7,22)){
        $taskR=[Gatefall.Tests.Stage13MarkerPositionReview]::Run($taskSeed,$taskSite,$taskActions)
        if($taskR.State -notin @('Won','Lost')){throw 'Incomplete model'}
        $taskR
    }})
    [ordered]@{created=(Get-Date -Format o);task=202;version='0.20.4';newModels=9;baselineModels=3;candidateModels=6;
        policy='24 native receipt target times; retry until affordable; first Blast preferred, then Range preferred, else first available. Swap all14<->7/22 actions; Arc17 and partner16 fixed. No movement/skill calls.';
        nativeSeedUnknown=$true;nativeReplay=$false;productChanged=$false;rows=$taskRows}|ConvertTo-Json -Depth 9|Set-Content $taskOutput
}
$taskData=Get-Content $taskOutput -Raw|ConvertFrom-Json
if($taskData.rows.Count -ne 9){throw 'Limited model count differs'}
$taskRows=@(foreach($taskR in $taskData.rows){
    $taskBase=@($taskData.rows|Where-Object {$_.Seed -eq $taskR.Seed -and $_.MarkerSite -eq 14})[0]
    $taskCardsEqual=(($taskR.Cards|ConvertTo-Json -Compress) -ceq ($taskBase.Cards|ConvertTo-Json -Compress))
    if(-not $taskCardsEqual -or $taskR.CompletedActions -ne 24 -or $taskR.Spent -ne 1050 -or $taskR.Towers -ne 16){throw 'Matched policy/card/budget validation failed; retain raw for review'}
    [ordered]@{seed=$taskR.Seed;markerSite=$taskR.MarkerSite;state=$taskR.State;core=$taskR.Core;time=$taskR.Time;
        spent=$taskR.Spent;credits=$taskR.Credits;cards=$taskR.Cards;cardsEqual=$taskCardsEqual;maxPurchaseDelay=$taskR.MaximumPurchaseDelay;
        markerDamage=$taskR.KindDamage[8];markedHits=$taskR.MarkedHits;markBonus=$taskR.MarkBonus;spearDamage=$taskR.KindDamage[0];arcDamage=$taskR.KindDamage[3];ruptureDamage=$taskR.KindDamage[2];
        markerShots=$taskR.Observation.KindShots[8];fired=$taskR.Observation.FiredSites;frequent=$taskR.Observation.FrequentFiringSites;peakRecent=$taskR.Observation.PeakActive3Seconds;peakAlive=$taskR.Observation.PeakAlive}
})
[ordered]@{created=(Get-Date -Format o);task=202;rows=$taskRows;newModels=9;onlyPositionActionMappingChanged=$true;
    markerAndSpearLocationsBothChange=$true;candidate22MovesLateSpearBuild=$true;nativeReplay=$false;
    bonusAlreadyIncludedInAlliedDamage=$true;productChanged=$false;newApk=$false;newAvd=$false}|ConvertTo-Json -Depth 7|Set-Content TestResults/stage13-marker-v0204-comparison.json
$taskRows|ForEach-Object {[pscustomobject]$_}|Format-Table seed,markerSite,state,core,time,spent,markerShots,markBonus,fired,frequent,peakRecent -AutoSize
