param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskSources=@(Get-Content TestResults/source-hashes-v0191.json -Raw|ConvertFrom-Json)
foreach($taskRow in $taskSources){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Current product differs: $($taskRow.path)"}}
$taskPrior=Get-Content TestResults/growth-choice-v0191-raw.json -Raw|ConvertFrom-Json
$taskSource=Get-Content Tests/Stage9GrowthChoiceReview.cs -Raw
if((Get-FileHash Tests/Stage9GrowthChoiceReview.cs).Hash.ToLowerInvariant() -ne $taskPrior.harnessSha256){throw 'Prior harness changed'}
$taskTransforms=@(
    @('class Stage9GrowthChoiceReview','class Stage9FusionLaneReview'),
    @('Run(int seed,string branch)','Run(int seed,string branch,int anchor)'),
    @('var row=new Result{Seed=seed,Branch=branch};','if(branch!="fusion_refill" || (anchor!=0 && anchor!=9 && anchor!=12))throw new ArgumentException("Only reviewed fusion anchors");var row=new Result{Seed=seed,Branch="anchor_"+anchor};'),
    @('if(action==0)done=g.TryUpgrade(g.TowerAt(0).Id,null,out why);','if(action==0)done=g.TryUpgrade(g.TowerAt(anchor==9?9:0).Id,null,out why);'),
    @('done=g.TryFuse(0,g.TowerAt(0).Id,g.TowerAt(12).Id,0,null,out why);','done=g.TryFuse(0,g.TowerAt(anchor==9?9:0).Id,g.TowerAt(12).Id,anchor,null,out why);'),
    @('done=g.TryBuild(12,TowerKind.Autocannon,null,out why);','done=g.TryBuild(anchor==12?0:12,TowerKind.Autocannon,null,out why);')
)
$taskDerived=$taskSource
foreach($taskPair in $taskTransforms){if(([regex]::Matches($taskDerived,[regex]::Escape($taskPair[0]))).Count -ne 1){throw "Transform not unique: $($taskPair[0])"};$taskDerived=$taskDerived.Replace($taskPair[0],$taskPair[1])}
$taskReversed=$taskDerived
for($taskI=$taskTransforms.Count-1;$taskI -ge 0;$taskI--){$taskReversed=$taskReversed.Replace($taskTransforms[$taskI][1],$taskTransforms[$taskI][0])}
if($taskReversed -cne $taskSource){throw 'Unreviewed harness logic changed'}
if(-not $UseRecorded){
    if(Test-Path TestResults/fusion-lane-v0191-raw.json){throw 'Preserve recorded results'}
    $taskFolder=Join-Path $taskProject 'TestResults/fusion-lane-v0191-review'
    New-Item -ItemType Directory -Path $taskFolder -Force|Out-Null
    $taskDerivedPath=Join-Path $taskFolder 'Stage9FusionLaneReview.cs'
    Set-Content -LiteralPath $taskDerivedPath -Value $taskDerived -NoNewline
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+$taskDerivedPath)
    $taskRows=@(foreach($taskSeed in @(7,19,41)){foreach($taskAnchor in @(12,9)){[Gatefall.Tests.Stage9FusionLaneReview]::Run($taskSeed,'fusion_refill',$taskAnchor)}})
    [pscustomobject]@{created=(Get-Date).ToString('o');productVersion='0.19.1';newConditions=6;baselineReused=3;derivationReversedExactly=$true;rows=$taskRows;harnessSha256=(Get-FileHash $taskDerivedPath).Hash.ToLowerInvariant()}|ConvertTo-Json -Depth 7|Set-Content TestResults/fusion-lane-v0191-raw.json
}
$taskRaw=Get-Content TestResults/fusion-lane-v0191-raw.json -Raw|ConvertFrom-Json
if((Get-FileHash TestResults/fusion-lane-v0191-review/Stage9FusionLaneReview.cs).Hash.ToLowerInvariant() -ne $taskRaw.harnessSha256){throw 'Recorded harness differs'}
$taskBaseline=@($taskPrior.rows|Where-Object Branch -eq fusion_refill)
foreach($taskR in $taskRaw.rows){
    $taskB=@($taskBaseline|Where-Object Seed -eq $taskR.Seed)[0]
    if($taskR.Spent -ne 460 -or $taskR.CompletedActions -ne 11 -or $taskR.Towers -ne 7 -or $taskR.Fusions -ne 1 -or $taskR.Supplies -ne 5 -or $taskR.SupplyCredits -ne 500 -or $taskR.CardOffers -ne 3 -or $taskR.Catalysts -ne 2){throw 'Policy incomplete or resource cap differs'}
    if($taskR.State -eq 'Won' -and (-not $taskR.AllSpawned -or $taskR.Spawned -ne 624 -or $taskR.Kills -ne 624 -or $taskR.Remaining -ne 0)){throw 'Victory before annihilation'}
    if((ConvertTo-Json -Compress -InputObject $taskR.Cards) -cne (ConvertTo-Json -Compress -InputObject $taskB.Cards)){throw 'Card policy differs'}
    if($taskR.MaxPurchaseDelay -gt .051){throw 'Purchase timing differs'}
    if($taskR.Branch -eq 'anchor_12' -and (ConvertTo-Json -Compress -InputObject @($taskR.Purchases)[0..8]) -cne (ConvertTo-Json -Compress -InputObject @($taskB.Purchases)[0..8])){throw 'Anchor12 pre-fusion policy differs'}
}
foreach($taskRow in $taskSources){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product changed during analysis'}}
[pscustomobject]@{created=(Get-Date).ToString('o');task=175;productVersion='0.19.1';productChanged=$false;newConditions=6;baselineReused=3;derivationReversedExactly=$true;baseline=$taskBaseline;rows=$taskRaw.rows;limitations=@('Three seeds; one seven-tower opening and inherited per-tick Hunter skill policy, not human fun.','Anchor12 preserves all actions before fusion and exchanges result/refill positions0/12.','Anchor9 exchanges upgraded Spear material0/9 before fusion; this is not a pure post60 position experiment.','Same460 spend and no money correction; kind damage is not causal team additional damage.');newAndroidBattle=$false;newApk=$false;newAvd=$false}|ConvertTo-Json -Depth 8|Set-Content TestResults/fusion-lane-v0191-comparison.json
$taskRaw.rows|Select-Object Seed,Branch,Core,Time,Spent,Credits,CoreContacts,@{n='ArcShots';e={$_.KindShots[3]}},@{n='ArcDamage';e={$_.KindDamage[3]}}|Format-Table -AutoSize
'PASS fusion anchor comparison; six new cases; three prior cases reused; product unchanged'
