param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskSources=@(Get-Content TestResults/source-hashes-v0191.json -Raw|ConvertFrom-Json)
foreach($taskRow in $taskSources){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Current product differs: $($taskRow.path)"}}
$taskBaseline=@((Get-Content TestResults/heavy-speed-v0190-speed125-supplemental.json -Raw|ConvertFrom-Json).models|Where-Object policy -eq limited7)
if($taskBaseline.Count -ne 3){throw 'Missing three current-pressure seven-tower cases'}
if(-not $UseRecorded){
    if(Test-Path TestResults/growth-choice-v0191-raw.json){throw 'Preserve recorded comparison'}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path $taskProject 'Tests/Stage9GrowthChoiceReview.cs'))
    $taskControl=[Gatefall.Tests.Stage9GrowthChoiceReview]::Run(7,'prepare_only')
    $taskRows=@(foreach($taskSeed in @(7,19,41)){foreach($taskBranch in @('fusion_refill','direct_upgrades')){[Gatefall.Tests.Stage9GrowthChoiceReview]::Run($taskSeed,$taskBranch)}})
    [pscustomobject]@{created=(Get-Date).ToString('o');productVersion='0.19.1';newConditions=6;selectedControlReplay=1;control=$taskControl;rows=$taskRows;harnessSha256=(Get-FileHash Tests/Stage9GrowthChoiceReview.cs).Hash.ToLowerInvariant()}|ConvertTo-Json -Depth 7|Set-Content TestResults/growth-choice-v0191-raw.json
}
$taskRaw=Get-Content TestResults/growth-choice-v0191-raw.json -Raw|ConvertFrom-Json
if((Get-FileHash Tests/Stage9GrowthChoiceReview.cs).Hash.ToLowerInvariant() -ne $taskRaw.harnessSha256){throw 'Recorded harness differs'}
$taskA=$taskRaw.control;$taskB=@($taskBaseline|Where-Object {$_.result.Seed -eq 7})[0].result
$taskPairs=@(@('Spent','Spent'),@('Credits','EndingCredits'),@('Towers','Towers'),@('Spawned','Spawned'),@('Kills','Kills'),@('Remaining','Remaining'),@('Fusions','Fusions'),@('Core','Core'),@('Hunter','Hunter'),@('State','State'),@('CoreDamageEvents','CoreDamageEvents'),@('CoreContacts','CoreContacts'),@('SkillUses','SkillUses'),@('FiredSites','FiredSites'),@('FrequentFiringSites','FrequentFiringSites'),@('PeakActive3Seconds','PeakActive3Seconds'),@('PeakProjectiles','PeakProjectiles'),@('PeakEnemies','PeakEnemies'))
foreach($taskPair in $taskPairs){if($taskA.($taskPair[0]) -ne $taskB.($taskPair[1])){throw "Control parity differs: $($taskPair[0])"}}
if([math]::Abs($taskA.Time-$taskB.Time) -gt .0001 -or (ConvertTo-Json -Compress -InputObject $taskA.Purchases) -cne (ConvertTo-Json -Compress -InputObject $taskB.PurchaseLog) -or (ConvertTo-Json -Compress -InputObject $taskA.SiteShots) -cne (ConvertTo-Json -Compress -InputObject $taskB.SiteShots) -or (ConvertTo-Json -Compress -InputObject $taskA.Cards) -cne (ConvertTo-Json -Compress -InputObject $taskB.Cards)){throw 'Control time/ledger/shots/cards differ'}
foreach($taskR in $taskRaw.rows){
    $taskExpected=if($taskR.Branch -eq 'fusion_refill'){460}else{455}
    if($taskR.Spent -ne $taskExpected -or $taskR.CompletedActions -ne 11 -or $taskR.Towers -ne 7 -or $taskR.Supplies -gt 5 -or $taskR.SupplyCredits -gt 500 -or $taskR.CardOffers -gt 3 -or $taskR.Catalysts -gt 2){throw 'Incomplete purchase policy or resource cap differs'}
    if($taskR.State -ne 'Won' -and $taskR.State -ne 'Lost'){throw 'Nonterminal run'}
    if($taskR.State -eq 'Won' -and (-not $taskR.AllSpawned -or $taskR.Spawned -ne 624 -or $taskR.Kills -ne 624 -or $taskR.Remaining -ne 0)){throw 'Victory without full annihilation'}
    $taskReference=@($taskBaseline|Where-Object {$_.result.Seed -eq $taskR.Seed})[0].result
    if((ConvertTo-Json -Compress -InputObject $taskR.Cards) -cne (ConvertTo-Json -Compress -InputObject $taskReference.Cards)){throw 'Card policy differs'}
}
foreach($taskRow in $taskSources){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product changed during analysis'}}
[pscustomobject]@{created=(Get-Date).ToString('o');task=174;productVersion='0.19.1';productChanged=$false;newConditions=6;baselineCasesReused=3;selectedControlReplay=1;controlParity=$true;priceAdjustment=$false;differenceReinvested=$false;fixedOpening=@(0,12,9,10,11,15);baseline=$taskBaseline;rows=$taskRaw.rows;controlWithSlowObservations=$taskRaw.control;limitations=@('Three seeds and one seven-tower opening; not all strategies or human fun.','455 and460 actual spending differ by5; remaining money is preserved.','Per-kind damage is not causal additional team damage; site shots include pre/post fusion at the same socket.','Binding slow observations sample surviving enemies with resistance and excess over other slow sources, not exact travel delay or prevented leaks.','Skill use is the inherited per-tick policy including Hunter0 movement; not a no-skill test.');newAndroidBattle=$false;newApk=$false;newAvd=$false;humanPlaytested=$false}|ConvertTo-Json -Depth 8|Set-Content TestResults/growth-choice-v0191-comparison.json
$taskRaw.rows|Select-Object Seed,Branch,State,Core,Time,Spent,Credits,FiredSites,Fusions,StrongBindingTargets,MarginalStrongBindingSlowSeconds|Format-Table -AutoSize
'PASS current-pressure growth choices; control matched existing record; product unchanged'
