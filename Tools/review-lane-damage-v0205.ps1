param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOut='TestResults/lane-damage-v0205-observed.json'
if(-not $UseRecorded){
    if(Test-Path $taskOut){throw 'Preserve selected observation; use -UseRecorded'}
    $taskRoot=Join-Path (Get-Location) 'TestResults/lane-damage-v0205-core'
    if(Test-Path $taskRoot){throw 'Preserve source snapshot'}
    New-Item -ItemType Directory -Path $taskRoot|Out-Null
    foreach($taskR in (Get-Content TestResults/source-hashes-v0205.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed'}}
    foreach($taskCore in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item $taskCore.FullName (Join-Path $taskRoot $taskCore.Name)}
    $taskPath=Join-Path $taskRoot 'RaonCombat.cs';$taskBase=[IO.File]::ReadAllText($taskPath)
    $taskNeedle='CoreHealth=Math.Max(0,CoreHealth-hit);Emit(EventKind.CoreHurt,new V2(),new V2(),hit);'
    $taskProbe='Gatefall.Tests.LaneDamageObservation.Record(Elapsed,e.Id,e.Lane,(int)e.Kind,CoreHealth,hit,CoreShield>0);'
    if(([regex]::Matches($taskBase,[regex]::Escape($taskNeedle))).Count -ne 1){throw 'Damage read point ambiguous'}
    $taskText=$taskBase.Replace($taskNeedle,$taskProbe+$taskNeedle)
    if($taskText.Replace($taskProbe,'') -cne $taskBase){throw 'Observer changed other source'}
    [IO.File]::WriteAllText($taskPath,$taskText)
    Add-Type -Path @((Get-ChildItem $taskRoot -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'Tests/LaneDamageObservation.cs')+(Join-Path (Get-Location) 'TestResults/Stage13FrontChoiceReview-v0205.cs'))
    $taskActions=@(foreach($taskR in (Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json|Select-Object -First 11)){
        [Gatefall.Tests.Stage13FrontChoiceReview+Action]@{Time=$taskR.time;Command=$taskR.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskR.kind);Site=$(if($taskR.site -eq 17){13}else{$taskR.site})}
    })
    [Gatefall.Tests.Stage13FrontChoiceReview]::UseObserver=$true
    [Gatefall.Tests.LaneDamageObservation]::Rows.Clear()
    $taskR=[Gatefall.Tests.Stage13FrontChoiceReview]::Run(41,14,$taskActions)
    $taskPrior=(Get-Content TestResults/stage13-front-choice-v0205-pilot.json -Raw|ConvertFrom-Json).rows|Where-Object {$_.seed -eq 41 -and $_.plan -eq 'observer13'}
    $taskBaseR=$taskPrior.battle
    foreach($taskField in @('State','Core','Hunter','Time','Spent','Credits','Towers','Kills','Spawned','Remaining','CompletedActions','MarkedHits','MarkBonus','MaximumPurchaseDelay')){
        if($taskField -in @('Time','MarkBonus','MaximumPurchaseDelay')){if([Math]::Abs([double]$taskR.$taskField-[double]$taskBaseR.$taskField) -gt .0001){throw "Observed result changed: $taskField"}}
        elseif($taskR.$taskField -cne $taskBaseR.$taskField){throw "Observed result changed: $taskField"}
    }
    foreach($taskField in @('Cards','Purchases','KindDamage')){if(($taskR.$taskField|ConvertTo-Json -Compress) -cne ($taskBaseR.$taskField|ConvertTo-Json -Compress)){throw "Observed array changed: $taskField"}}
    if(($taskR.Observation.SiteShots|ConvertTo-Json -Compress) -cne ($taskBaseR.Observation.SiteShots|ConvertTo-Json -Compress)){throw 'Observed shot map changed'}
    $taskHits=@([Gatefall.Tests.LaneDamageObservation]::Rows.ToArray())
    $taskEffective=($taskHits|Measure-Object Effective -Sum).Sum;$taskRequested=($taskHits|Measure-Object Requested -Sum).Sum
    if([Math]::Abs($taskEffective-(300-$taskR.Core)) -gt .01 -or [Math]::Abs($taskRequested-$taskR.Observation.CoreDamage) -gt .02 -or $taskHits.Count -ne $taskR.Observation.CoreDamageEvents){throw 'Damage amounts/counts do not reconcile'}
    [ordered]@{created=(Get-Date -Format o);task=211;version='0.20.5';newModels=1;selectedSeed=41;plan='observer13';priorResultAndLedgerAndDamageAndShotsIdentical=$true;
        requestedDamage=$taskRequested;effectiveDamage=$taskEffective;count=$taskHits.Count;rows=$taskHits;terminal=$taskR;productChanged=$false;newApk=$false;newAvd=$false}|ConvertTo-Json -Depth 10|Set-Content $taskOut
}
$taskData=Get-Content $taskOut -Raw|ConvertFrom-Json
$taskSummary=@($taskData.rows|Group-Object Lane,Kind|ForEach-Object {[pscustomobject]@{lane=$_.Group[0].Lane;kind=$_.Group[0].Kind;events=$_.Count;first=($_.Group|Measure-Object Time -Minimum).Minimum;last=($_.Group|Measure-Object Time -Maximum).Maximum;requested=($_.Group|Measure-Object Requested -Sum).Sum;effective=($_.Group|Measure-Object Effective -Sum).Sum}})
[ordered]@{created=(Get-Date -Format o);task=211;newObservedConditions=1;prior210ConditionsNotRerun=3;requested=$taskData.requestedDamage;effective=$taskData.effectiveDamage;events=$taskData.count;groups=$taskSummary;productChanged=$false;newNativeRun=$false}|ConvertTo-Json -Depth 5|Set-Content TestResults/lane-damage-v0205-summary.json
$taskSummary|Format-Table -AutoSize
