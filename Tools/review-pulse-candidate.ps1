param([ValidateSet('baseline','window16','window12')][string]$Candidate,[switch]$BaselineSamples,[switch]$TimingSamples)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot='TestResults/pulse-v0190-review'
$taskHarness=if($TimingSamples){'TowerDensityReview-timing.cs'}else{'TowerDensityReview-observed.cs'}
Add-Type -Path @((Get-ChildItem -LiteralPath "$taskRoot/$Candidate" -Filter '*.cs').FullName+(Join-Path $taskProject "$taskRoot/$taskHarness"))
if($BaselineSamples -or $TimingSamples) {
    if($BaselineSamples -and $Candidate -ne 'baseline'){throw 'Only baseline has selected pressure supplementation'}
    $taskMethod=[Gatefall.Tests.TowerDensityReview].GetMethod('Run',[Reflection.BindingFlags]'NonPublic,Static')
    $taskRows=@(foreach($taskPolicy in @('many','mixed','upgrade')){$taskMethod.Invoke($null,@([int]8,[int]7,[string]$taskPolicy))})
} else {$taskRows=@([Gatefall.Tests.TowerDensityReview]::Models(8))}
$taskCases=@()
foreach($taskCrisis in @($false,$true)) {foreach($taskStage in [Gatefall.Core.CampaignCatalog]::Stages) {
    $taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=$taskStage;Crisis=$taskCrisis})
    $taskCases+=[pscustomobject]@{stage=$taskStage.Index+1;crisis=$taskCrisis;sockets=@($taskG.BoardSockets|Select-Object X,Y);schedule=@($taskG.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm,Warned)}
}}
$taskSuffix=if($TimingSamples){'-timing-samples'}else{''}
[pscustomobject]@{created=(Get-Date).ToString('o');candidate=$Candidate;baselineSelectedSamples=$BaselineSamples.IsPresent;timingSelectedSamples=$TimingSamples.IsPresent;method='Same purchase policy and seeds; read-only live-enemy/shot/purchase counters added outside product. LateEnemySeconds is alive enemies x .05s after90 combat seconds, not rendering load/human fun. Timing supplementation uses actual combat-clock deltas in seed7 only.';models=$taskRows;contractCases=$taskCases}|ConvertTo-Json -Depth 9|Set-Content -LiteralPath "TestResults/pulse-v0190-$Candidate$taskSuffix.json"
$taskRows|Group-Object Policy|ForEach-Object{[pscustomobject]@{candidate=$Candidate;policy=$_.Name;cases=$_.Count;wins=@($_.Group|Where-Object State -eq Won).Count;fired=($_.Group|Measure-Object FiredSites -Average).Average;frequent=($_.Group|Measure-Object FrequentFiringSites -Average).Average;peakAlive=($_.Group|Measure-Object PeakEnemies -Maximum).Maximum;core=($_.Group|Measure-Object Core -Minimum).Minimum;lastClear=($_.Group|Measure-Object Time -Maximum).Maximum}}|ConvertTo-Json -Compress
