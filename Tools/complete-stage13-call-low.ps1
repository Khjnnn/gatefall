param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskPilot=Get-Content TestResults/stage13-call-low-v0207-pilot.json -Raw|ConvertFrom-Json
$taskOut='TestResults/stage13-call-low-v0207-supplement.json'
if(-not $UseRecorded){
    if(Test-Path $taskOut){throw 'Preserve supplement; use -UseRecorded'}
    if(-not $taskPilot.controlMatchesOutcomeLedgerCardsDamageShots){throw 'Pilot control not verified'}
    foreach($taskR in (Get-Content TestResults/source-hashes-v0207.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed'}}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'TestResults/Stage13CallPressureReview-v0207.cs'))
    $taskActions=@(foreach($taskR in (Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json|Select-Object -First 10)){
        [Gatefall.Tests.Stage13CallPressureReview+Action]@{Time=$taskR.time;Command=$taskR.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskR.kind);Site=$taskR.site}
    })
    $taskActions+=[Gatefall.Tests.Stage13CallPressureReview+Action]@{Time=71.5;Command='build';Kind=[Gatefall.Core.TowerKind]::Autocannon;Site=13}
    $taskRows=@(foreach($taskSeed in @(7,19)){foreach($taskPolicy in @('earliest','clear')){
        [Gatefall.Tests.Stage13CallPressureReview]::Calling=$taskPolicy
        $taskBattle=[Gatefall.Tests.Stage13CallPressureReview]::Run($taskSeed,14,$taskActions)
        if($taskBattle.Spent -ne 465 -or $taskBattle.Towers -ne 9 -or $taskBattle.CompletedActions -ne 11 -or $taskBattle.Supplies -gt 5 -or $taskBattle.Offers -gt 3 -or $taskBattle.CatalystsGranted -gt 2 -or $taskBattle.Advance -gt 30.001){throw 'Supplement policy/caps differ'}
        [ordered]@{seed=$taskSeed;policy=$taskPolicy;battle=$taskBattle}
    }})
    [ordered]@{created=(Get-Date -Format o);task=216;version='0.20.7';newModels=4;pilotModelsNotRerun=3;rows=$taskRows;productChanged=$false;newAndroid=$false}|ConvertTo-Json -Depth 12|Set-Content $taskOut
}
$taskSupplement=Get-Content $taskOut -Raw|ConvertFrom-Json
$taskBase=@((Get-Content TestResults/stage13-reinforcement-v0204-models.json -Raw|ConvertFrom-Json).rows|Where-Object plan -eq 'north465')
$taskRows=@(foreach($taskR in @($taskPilot.rows)+@($taskSupplement.rows)){
    $taskA=$taskR.battle;$taskB=($taskBase|Where-Object {$_.battle.Seed -eq $taskR.seed}).battle
    [ordered]@{seed=$taskR.seed;policy=$taskR.policy;state=$taskA.State;core=$taskA.Core;baselineCore=$taskB.Core;time=$taskA.Time;baselineTime=$taskB.Time;spent=$taskA.Spent;credits=$taskA.Credits;towers=$taskA.Towers;completedActions=$taskA.CompletedActions;calls=$taskA.WaveCalls;advanced=$taskA.Advance;supplies=$taskA.Supplies;cards=$taskA.Offers;catalysts=$taskA.CatalystsGranted;peak=$taskA.Observation.PeakAlive;baselinePeak=$taskB.Observation.PeakAlive;fired=$taskA.Observation.FiredSites;baselineFired=$taskB.Observation.FiredSites;firstCoreDamage=$taskA.Observation.FirstCoreDamage;damageEvents=$taskA.Observation.CoreDamageEvents;requestedCoreDamage=$taskA.Observation.CoreDamage;purchaseDelay=$taskA.MaximumPurchaseDelay;sameCards=(($taskA.Cards|ConvertTo-Json -Compress) -ceq ($taskB.Cards|ConvertTo-Json -Compress));callMoments=$taskA.CallMoments}
})
if($taskRows.Count -ne 6){throw 'Six candidate conditions required'}
[ordered]@{created=(Get-Date -Format o);task=216;newModels=7;baselineReused=3;currentControlSeed=41;currentControlParity=$taskPilot.controlMatchesOutcomeLedgerCardsDamageShots;summaryNewModels=0;rows=$taskRows;scope='One north465 nine-tower fixed purchase/first-card policy; not all placements or a human/native playtest';productChanged=$false;newApk=$false;newAvd=$false}|ConvertTo-Json -Depth 9|Set-Content TestResults/stage13-call-low-v0207-comparison.json
$taskRows|ForEach-Object {[pscustomobject]$_}|Select-Object seed,policy,state,core,time,calls,advanced,peak,baselinePeak,damageEvents,sameCards|Format-Table -AutoSize
