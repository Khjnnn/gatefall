param([ValidateSet('control','passive')][string]$Mode='control',[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOutput="TestResults/stage4-input-v0210-$Mode.json"
if($UseRecorded){Get-Content $taskOutput -Raw;exit}
if(Test-Path $taskOutput){throw 'Preserve recorded observations; use -UseRecorded'}
foreach($taskR in (Get-Content TestResults/source-hashes-v0210.json -Raw|ConvertFrom-Json)){
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed before analysis'}
}
$taskRoot=Join-Path (Get-Location) ('TestResults/stage4-input-v0210-review/'+$Mode)
New-Item -ItemType Directory -Path $taskRoot -Force|Out-Null
foreach($taskFile in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $taskFile.FullName -Destination $taskRoot}
$taskOriginal=[IO.File]::ReadAllText((Join-Path (Get-Location) 'TestResults/alley-v0192-review/AlleyDensityReview.cs'))
$taskObserver=$taskOriginal
if($Mode -eq 'passive'){
    if(([regex]::Matches($taskOriginal,[regex]::Escape('g.UseSkill();'))).Count -ne 1){throw 'Skill call ambiguous'}
    $taskObserver=$taskOriginal.Replace('g.UseSkill();','/* no manual active skill in this policy */')
    if($taskObserver.Replace('/* no manual active skill in this policy */','g.UseSkill();') -cne $taskOriginal){throw 'Observer change outside one input'}
}
$taskObserverPath=Join-Path $taskRoot AlleyDensityReview.cs
[IO.File]::WriteAllText($taskObserverPath,$taskObserver)
Add-Type -Path (Get-ChildItem $taskRoot -Filter '*.cs').FullName
$taskRun=[Gatefall.Tests.AlleyDensityReview].GetMethod('Run',[Reflection.BindingFlags]'NonPublic,Static')
$taskRows=if($Mode -eq 'control'){@($taskRun.Invoke($null,@(3,7,'mixed')))}else{
    @(foreach($taskSeed in @(7,41)){foreach($taskPolicy in @('many','mixed','upgrade')){$taskRun.Invoke($null,@(3,$taskSeed,$taskPolicy))}})
}
$taskHistorical=@((Get-Content TestResults/tower-density-v0170.json -Raw|ConvertFrom-Json).models|Where-Object Stage -eq 4)
if($Mode -eq 'control'){
    $taskOld=@($taskHistorical|Where-Object {$_.Seed -eq 7 -and $_.Policy -eq 'mixed'})[0]
    foreach($taskField in @('Stage','Seed','Sites','StartingCredits','Towers','Spent','Kills','Fusions','FiredSites','FrequentFiringSites','PeakActive3Seconds','PeakProjectiles','Policy','State','Core')){
        if($taskRows[0].$taskField -ne $taskOld.$taskField){throw "Historical control differs: $taskField"}
    }
    if([math]::Abs($taskRows[0].Time-$taskOld.Time) -gt .0001){throw 'Historical control time differs'}
}
$taskGame=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[3]})
[ordered]@{created=(Get-Date -Format o);task=230;mode=$Mode;newModels=$taskRows.Count;historicalBaselineReused=9;historicalControlMatched=($Mode -eq 'control');stage=4;sites=$taskGame.BoardSockets.Length;totalScheduled=(@($taskGame.Schedule)|Measure-Object Count -Sum).Sum;initialCredits=$taskGame.Credits;boss=$taskGame.Rules.Stage.Boss;manualSkillEnabled=($Mode -eq 'control');samePurchaseRules=$true;fixedPurchaseTimes=$false;humanBeginnerPlay=$false;nativeAndroid=$false;productChanged=$false;models=$taskRows}|ConvertTo-Json -Depth 8|Set-Content $taskOutput
$taskRows|Select-Object Seed,Policy,State,Core,Time,Towers,Spent,Credits,FiredSites,PeakActive3Seconds,CoreDamageEvents|Format-Table -AutoSize
