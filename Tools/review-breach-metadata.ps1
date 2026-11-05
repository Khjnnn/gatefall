param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOut='TestResults/breach-v0206-model.json'
if(-not $UseRecorded){
    if(Test-Path $taskOut){throw 'Preserve selected model; use -UseRecorded'}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+(Join-Path (Get-Location) 'TestResults/Stage13FrontChoiceReview-v0205.cs'))
    $taskActions=@(foreach($taskR in (Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json|Select-Object -First 11)){
        [Gatefall.Tests.Stage13FrontChoiceReview+Action]@{Time=$taskR.time;Command=$taskR.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskR.kind);Site=$(if($taskR.site -eq 17){13}else{$taskR.site})}
    })
    [Gatefall.Tests.Stage13FrontChoiceReview]::UseObserver=$true
    $taskR=[Gatefall.Tests.Stage13FrontChoiceReview]::Run(41,14,$taskActions)
    $taskPrior=((Get-Content TestResults/stage13-front-choice-v0205-pilot.json -Raw|ConvertFrom-Json).rows|Where-Object {$_.seed -eq 41 -and $_.plan -eq 'observer13'}).battle
    foreach($taskField in @('State','Core','Hunter','Time','Spent','Credits','Towers','Kills','Spawned','Remaining','CompletedActions','MarkedHits','MarkBonus','MaximumPurchaseDelay')){
        if($taskField -in @('Time','MarkBonus','MaximumPurchaseDelay')){if([Math]::Abs([double]$taskR.$taskField-[double]$taskPrior.$taskField) -gt .0001){throw "Selected outcome changed: $taskField"}}
        elseif($taskR.$taskField -cne $taskPrior.$taskField){throw "Selected outcome changed: $taskField"}
    }
    foreach($taskField in @('Cards','Purchases','KindDamage')){if(($taskR.$taskField|ConvertTo-Json -Compress) -cne ($taskPrior.$taskField|ConvertTo-Json -Compress)){throw "Selected array changed: $taskField"}}
    if(($taskR.Observation.SiteShots|ConvertTo-Json -Compress) -cne ($taskPrior.Observation.SiteShots|ConvertTo-Json -Compress)){throw 'Selected shot map changed'}
    [ordered]@{created=(Get-Date -Format o);task=212;version='0.20.6';newModels=1;seed=41;plan='observer13';priorResultAndLedgerAndDamageAndShotsIdentical=$true;battle=$taskR;newAndroidVerified=$false}|ConvertTo-Json -Depth 12|Set-Content $taskOut
}
$taskBase='TestResults/breach-v0206-baseline'
$taskRaon=[IO.File]::ReadAllText('Assets/Scripts/Core/RaonCombat.cs')
if($taskRaon.Replace('Emit(EventKind.CoreHurt,new V2(),new V2(),hit,e.Lane);','Emit(EventKind.CoreHurt,new V2(),new V2(),hit);') -cne [IO.File]::ReadAllText((Join-Path $taskBase 'RaonCombat.cs'))){throw 'Combat changed beyond lane metadata'}
$taskSim=[IO.File]::ReadAllText('Assets/Scripts/Core/DefenseSimulation.cs')
if($taskSim.Replace(' Kill: EnemyKind. Campaign CoreHurt: entrance lane.',' Kill: EnemyKind.') -cne [IO.File]::ReadAllText((Join-Path $taskBase 'DefenseSimulation.cs'))){throw 'Simulation changed beyond event documentation'}
$taskChanged=@('Assets\Scripts\Core\RaonCombat.cs','Assets\Scripts\Core\DefenseSimulation.cs','Assets\Scripts\Presentation\GatefallGame.cs','ProjectSettings\ProjectSettings.asset')
foreach($taskRow in (Get-Content "$taskBase/source-hashes-v0205.json" -Raw|ConvertFrom-Json)){
    if($taskRow.path -notin $taskChanged -and (Get-FileHash $taskRow.path).Hash -ne $taskRow.sha256){throw "Unrelated product changed: $($taskRow.path)"}
}
Write-Output 'PASS breach metadata: selected outcome/ledger/cards/shots identical; combat metadata only; unrelated product unchanged.'
