$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot=Join-Path $taskProject 'TestResults/alley-v0192-review'
if(Test-Path -LiteralPath $taskRoot){throw 'Preserve existing review snapshots'}
$taskManifest=Get-Content TestResults/source-hashes-v0192.json -Raw|ConvertFrom-Json
foreach($taskRow in $taskManifest){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product differs before review'}}
New-Item -ItemType Directory -Path $taskRoot|Out-Null
$taskOriginal=[IO.File]::ReadAllText((Join-Path $taskProject 'Assets/Scripts/Core/BattlefieldLayouts.cs')).Replace("`r`n","`n")
$taskNeedle=@'
            L("alley-west-north-elbows",1,
              P(-23,-6,-14,-6,-14,1,-7,1,-7,-3,-3,-3,0,0),
              P(12,11,12,6,5,6,5,1,0,1,0,0),none),
'@
$taskCandidates=[ordered]@{
    'west-hook'=@'
            L("alley-long-west-hook",1,
              P(-23,-6,-14,-6,-14,5,-5,5,-5,-3,-3,-3,0,0),
              P(12,11,12,6,5,6,5,1,0,1,0,0),none),
'@
    'north-court'=@'
            L("alley-north-return-court",1,
              P(-23,-6,-14,-6,-14,1,-7,1,-7,-3,-3,-3,0,0),
              P(12,11,12,7,18,7,18,-3,8,-3,8,2,2,2,0,0),none),
'@
}
if(([regex]::Matches($taskOriginal,[regex]::Escape($taskNeedle))).Count -ne 1){throw 'Stage8 definition ambiguous'}
foreach($taskName in @('baseline')+@($taskCandidates.Keys)){
    $taskDir=Join-Path $taskRoot $taskName
    New-Item -ItemType Directory -Path $taskDir|Out-Null
    foreach($taskFile in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskDir $taskFile.Name)}
    if($taskName -ne 'baseline'){
        $taskCandidate=$taskOriginal.Replace($taskNeedle,$taskCandidates[$taskName])
        if($taskCandidate.Replace($taskCandidates[$taskName],$taskNeedle) -cne $taskOriginal){throw 'Unexpected geometry change'}
        [IO.File]::WriteAllText((Join-Path $taskDir 'BattlefieldLayouts.cs'),$taskCandidate)
    }
}
$taskHarness=[IO.File]::ReadAllText((Join-Path $taskProject 'Tests/TowerDensityReview.cs'))
$taskTransforms=@(
    @('class TowerDensityReview','class AlleyDensityReview'),
    @('public string Policy, State; public float Core, Time;','public string Policy, State; public float Core, Time; public int Credits,Spawned,Remaining,PeakEnemies,CoreDamageEvents,SupplyCredits,CardOffers,Catalysts; public float IdleSeconds,EnemySeconds; public bool AllSpawned; public int[] SiteShots; public string[] PurchaseLog,Cards;'),
    @('g.Start();double nextBuy=1;int peakActive=0,peakProjectiles=0;','g.Start();double nextBuy=1;int peakActive=0,peakProjectiles=0,peakEnemies=0,coreEvents=0;float idle=0,enemySeconds=0;var cards=new List<string>();'),
    @('if(g.AwaitingCard)g.ChooseCard(g.CardOffers[0]);','if(g.AwaitingCard){cards.Add(g.CardOffers[0].ToString());g.ChooseCard(g.CardOffers[0]);}'),
    @('peakActive=Math.Max(peakActive,active);peakProjectiles=Math.Max(peakProjectiles,g.Projectiles.Count);g.Events.Clear();','peakActive=Math.Max(peakActive,active);peakProjectiles=Math.Max(peakProjectiles,g.Projectiles.Count);int alive=0;foreach(Enemy e in g.Enemies)if(e.Health>0)alive++;peakEnemies=Math.Max(peakEnemies,alive);enemySeconds+=alive*.05f;if(alive==0)idle+=.05f;foreach(CombatEvent e in g.Events)if(e.Kind==EventKind.CoreHurt)coreEvents++;g.Events.Clear();'),
    @('Time=(float)g.Elapsed};','Time=(float)g.Elapsed,Credits=g.Credits,Spawned=g.Telemetry.Spawned,Remaining=g.Enemies.Count,PeakEnemies=peakEnemies,CoreDamageEvents=coreEvents,SupplyCredits=g.SuppliesGranted*g.WaveSupplyCredits,CardOffers=g.Telemetry.CardOffers,Catalysts=g.Telemetry.CoresGranted,IdleSeconds=idle,EnemySeconds=enemySeconds,AllSpawned=g.AllWavesSpawned,SiteShots=shots,PurchaseLog=g.Telemetry.Purchases.ToArray(),Cards=cards.ToArray()};')
)
$taskDerived=$taskHarness
foreach($taskPair in $taskTransforms){if(([regex]::Matches($taskDerived,[regex]::Escape($taskPair[0]))).Count -ne 1){throw "Observer transform ambiguous: $($taskPair[0])"};$taskDerived=$taskDerived.Replace($taskPair[0],$taskPair[1])}
$taskReversed=$taskDerived
for($taskI=$taskTransforms.Count-1;$taskI -ge 0;$taskI--){$taskReversed=$taskReversed.Replace($taskTransforms[$taskI][1],$taskTransforms[$taskI][0])}
if($taskReversed -cne $taskHarness){throw 'Observer changed purchase/combat policy'}
[IO.File]::WriteAllText((Join-Path $taskRoot 'AlleyDensityReview.cs'),$taskDerived)
Copy-Item TestResults/source-hashes-v0192.json (Join-Path $taskRoot 'source-hashes-v0192.json')
'Prepared isolated stage8 baseline/two geometry candidates; observer reverse matched original policy'
