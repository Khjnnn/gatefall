param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOutput='TestResults/stage13-fusion-choice-v0205-models.json'
$taskPrior=Get-Content TestResults/stage13-marker-v0204-models.json -Raw|ConvertFrom-Json
if(-not $UseRecorded){
    if(Test-Path $taskOutput){throw 'Preserve recorded evidence; use -UseRecorded'}
    $taskOld=Get-Content TestResults/source-hashes-v0204.json -Raw|ConvertFrom-Json
    foreach($taskR in (Get-Content TestResults/source-hashes-v0205.json -Raw|ConvertFrom-Json)){
        if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed'}
        if($taskR.path -like 'Assets\Scripts\Core\*' -and @($taskOld|Where-Object path -eq $taskR.path)[0].sha256 -ne $taskR.sha256){throw 'Prior model core parity lost'}
    }
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'TestResults/stage13-fusion-choice-v0205-review/Stage13FusionChoiceReview.cs'))
    $taskActions=@(foreach($taskR in (Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json)){
        [Gatefall.Tests.Stage13FusionChoiceReview+Action]@{Time=$taskR.time;Command=$taskR.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskR.kind);Site=$taskR.site}
    })
    if(-not(Test-Path TestResults/stage13-fusion-choice-v0205-control.json)) {
    [Gatefall.Tests.Stage13FusionChoiceReview]::UseObserver=$false
    $taskControl=[Gatefall.Tests.Stage13FusionChoiceReview]::Run(19,14,$taskActions)
    $taskBase=@($taskPrior.rows|Where-Object {$_.Seed -eq 19 -and $_.MarkerSite -eq 14})[0]
    $taskParity=$taskControl.State -ceq $taskBase.State -and [Math]::Abs($taskControl.Time-$taskBase.Time) -lt .0001 -and
        $taskControl.Core -eq $taskBase.Core -and $taskControl.Spent -eq $taskBase.Spent -and $taskControl.Credits -eq $taskBase.Credits -and
        (($taskControl.Cards|ConvertTo-Json -Compress) -ceq ($taskBase.Cards|ConvertTo-Json -Compress)) -and
        (($taskControl.Purchases|ConvertTo-Json -Compress) -ceq ($taskBase.Purchases|ConvertTo-Json -Compress)) -and
        (($taskControl.KindDamage|ConvertTo-Json -Compress) -ceq ($taskBase.KindDamage|ConvertTo-Json -Compress)) -and
        (($taskControl.Observation.SiteShots|ConvertTo-Json -Compress) -ceq ($taskBase.Observation.SiteShots|ConvertTo-Json -Compress))
    [ordered]@{created=(Get-Date -Format o);seed=19;observerUnlockFlagOnlyChanged=$true;parity=$taskParity;control=$taskControl}|ConvertTo-Json -Depth 8|Set-Content TestResults/stage13-fusion-choice-v0205-control.json
    } else {
        $taskRecorded=Get-Content TestResults/stage13-fusion-choice-v0205-control.json -Raw|ConvertFrom-Json
        $taskParity=$taskRecorded.parity
    }
    if(-not $taskParity){throw 'Unlock flag control differs; retain raw and do not reuse prior baseline'}
    [Gatefall.Tests.Stage13FusionChoiceReview]::UseObserver=$true
    $taskRows=@(foreach($taskSeed in @(7,19,41)){[Gatefall.Tests.Stage13FusionChoiceReview]::Run($taskSeed,14,$taskActions)})
    [ordered]@{created=(Get-Date -Format o);task=209;newCandidateModels=3;newSelectedControl=1;reusedBaseline=3;sourceVersion='0.20.5';replayAfterStage14Only=$true;productChanged=$false;rows=$taskRows}|ConvertTo-Json -Depth 9|Set-Content $taskOutput
}
$taskData=Get-Content $taskOutput -Raw|ConvertFrom-Json
$taskRows=@(foreach($taskR in $taskData.rows){
    $taskBase=@($taskPrior.rows|Where-Object {$_.Seed -eq $taskR.Seed -and $_.MarkerSite -eq 14})[0]
    $taskCardsEqual=(($taskR.Cards|ConvertTo-Json -Compress) -ceq ($taskBase.Cards|ConvertTo-Json -Compress))
    if($taskR.CompletedActions -ne 24 -or $taskR.Spent -ne 1050 -or $taskR.Towers -ne 16 -or $taskR.State -notin @('Won','Lost')){throw 'Branch budget/completion differs'}
    [ordered]@{seed=$taskR.Seed;state=$taskR.State;core=$taskR.Core;time=$taskR.Time;spent=$taskR.Spent;credits=$taskR.Credits;cards=$taskR.Cards;cardsEqual=$taskCardsEqual;
        observerDamage=$taskR.KindDamage[9];spearDamage=$taskR.KindDamage[0];markerDamage=$taskR.KindDamage[8];markBonus=$taskR.MarkBonus;markedHits=$taskR.MarkedHits;
        observerShots=$taskR.Observation.KindShots[9];fired=$taskR.Observation.FiredSites;frequent=$taskR.Observation.FrequentFiringSites;peakRecent=$taskR.Observation.PeakActive3Seconds;
        baselineArcCore=$taskBase.Core;baselineArcTime=$taskBase.Time;baselineArcDamage=$taskBase.KindDamage[3];baselineMarkBonus=$taskBase.MarkBonus;
        baselineFired=$taskBase.Observation.FiredSites;baselineFrequent=$taskBase.Observation.FrequentFiringSites;maxPurchaseDelay=$taskR.MaximumPurchaseDelay}
})
[ordered]@{created=(Get-Date -Format o);task=209;newCandidates=3;newControl=1;reusedBaseline=3;replayAfterUnlockOnly=$true;nativeReplay=$false;
    initialMaterialKindAlsoChanges=$true;actualPairAndFusionCost195=$true;productChanged=$false;newApk=$false;newAvd=$false;rows=$taskRows}|ConvertTo-Json -Depth 7|Set-Content TestResults/stage13-fusion-choice-v0205-comparison.json
$taskRows|ForEach-Object {[pscustomobject]$_}|Format-Table seed,state,core,time,spent,cardsEqual,observerDamage,markBonus,fired,frequent,baselineArcTime -AutoSize
