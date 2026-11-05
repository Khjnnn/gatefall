$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot=Join-Path $taskProject 'TestResults/heavy-speed-v0190-review'
$taskOutput='TestResults/heavy-speed-v0190-speed125-supplemental.json'
if(Test-Path -LiteralPath $taskOutput){throw 'Preserve existing selected-candidate supplement'}
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskRoot 'speed125') -Filter '*.cs').FullName+(Join-Path $taskRoot 'InvestmentNecessityReview.cs')+(Join-Path $taskProject 'TestResults/pulse-v0190-review/TowerDensityReview-observed.cs'))
$taskMethod=[Gatefall.Tests.TowerDensityReview].GetMethod('Run',[Reflection.BindingFlags]'NonPublic,Static')
$taskRows=@(foreach($taskSeed in @(7,19,41)){
    [pscustomobject]@{policy='limited7';source='investmentObserver';result=[Gatefall.Tests.InvestmentNecessityReview]::Run(8,$taskSeed,'mixed',380)}
    foreach($taskPolicy in @('many','upgrade')){
        [pscustomobject]@{policy=$taskPolicy;source='originalPolicyObserver';result=$taskMethod.Invoke($null,@([int]8,[int]$taskSeed,[string]$taskPolicy))}
    }
})
[pscustomobject]@{created=(Get-Date).ToString('o');candidate='speed125';newAdditionalConditions=9;models=$taskRows;productChanged=$false}|ConvertTo-Json -Depth 9|Set-Content -LiteralPath $taskOutput
$taskRows|ForEach-Object{[pscustomobject]@{policy=$_.policy;seed=$_.result.Seed;state=$_.result.State;core=$_.result.Core;time=$_.result.Time;spent=$_.result.Spent;towers=$_.result.Towers;fired=$_.result.FiredSites;frequent=$_.result.FrequentFiringSites}}|Format-Table -AutoSize
