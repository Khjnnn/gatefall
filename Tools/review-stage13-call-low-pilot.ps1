param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOut='TestResults/stage13-call-low-v0207-pilot.json'
if(-not $UseRecorded){
    if(Test-Path $taskOut){throw 'Preserve pilot; use -UseRecorded'}
    foreach($taskR in (Get-Content TestResults/source-hashes-v0207.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed'}}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'TestResults/Stage13CallPressureReview-v0207.cs'))
    $taskActions=@(foreach($taskR in (Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json|Select-Object -First 10)){
        [Gatefall.Tests.Stage13CallPressureReview+Action]@{Time=$taskR.time;Command=$taskR.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskR.kind);Site=$taskR.site}
    })
    $taskActions+= [Gatefall.Tests.Stage13CallPressureReview+Action]@{Time=71.5;Command='build';Kind=[Gatefall.Core.TowerKind]::Autocannon;Site=13}
    [Gatefall.Tests.Stage13CallPressureReview]::Calling='none'
    $taskControl=[Gatefall.Tests.Stage13CallPressureReview]::Run(41,14,$taskActions)
    $taskPrior=((Get-Content TestResults/stage13-reinforcement-v0204-models.json -Raw|ConvertFrom-Json).rows|Where-Object {$_.plan -eq 'north465' -and $_.battle.Seed -eq 41}).battle
    foreach($taskF in @('State','Core','Hunter','Time','Spent','Credits','Towers','Kills','Spawned','Remaining','CompletedActions','MarkedHits','MarkBonus','MaximumPurchaseDelay')){
        if($taskF -in @('Time','MarkBonus','MaximumPurchaseDelay')){if([Math]::Abs([double]$taskControl.$taskF-[double]$taskPrior.$taskF) -gt .0001){throw "Pilot control differs: $taskF"}}
        elseif($taskControl.$taskF -cne $taskPrior.$taskF){throw "Pilot control differs: $taskF"}
    }
    foreach($taskF in @('Cards','Purchases','KindDamage')){if(($taskControl.$taskF|ConvertTo-Json -Compress) -cne ($taskPrior.$taskF|ConvertTo-Json -Compress)){throw "Pilot control array differs: $taskF"}}
    if(($taskControl.Observation.SiteShots|ConvertTo-Json -Compress) -cne ($taskPrior.Observation.SiteShots|ConvertTo-Json -Compress)){throw 'Pilot control shot map differs'}
    $taskRows=@(foreach($taskPolicy in @('earliest','clear')){
        [Gatefall.Tests.Stage13CallPressureReview]::Calling=$taskPolicy
        $taskBattle=[Gatefall.Tests.Stage13CallPressureReview]::Run(41,14,$taskActions)
        if($taskBattle.Spent -ne 465 -or $taskBattle.Towers -ne 9 -or $taskBattle.CompletedActions -ne 11 -or $taskBattle.Supplies -gt 5 -or $taskBattle.Offers -gt 3 -or $taskBattle.CatalystsGranted -gt 2 -or $taskBattle.Advance -gt 30.001){throw 'Pilot policy/caps differ'}
        [ordered]@{seed=41;policy=$taskPolicy;battle=$taskBattle}
    })
    [ordered]@{created=(Get-Date -Format o);task=216;version='0.20.7';newModels=3;baselineReused=1;controlMatchesOutcomeLedgerCardsDamageShots=$true;control=$taskControl;rows=$taskRows;unrunSeeds=@(7,19);productChanged=$false;newAndroid=$false}|ConvertTo-Json -Depth 12|Set-Content $taskOut
}
$taskData=Get-Content $taskOut -Raw|ConvertFrom-Json
$taskData.rows|ForEach-Object {[pscustomobject]@{policy=$_.policy;state=$_.battle.State;core=$_.battle.Core;time=$_.battle.Time;spent=$_.battle.Spent;credits=$_.battle.Credits;calls=$_.battle.WaveCalls;advanced=$_.battle.Advance;peak=$_.battle.Observation.PeakAlive;fired=$_.battle.Observation.FiredSites}}|Format-Table -AutoSize
