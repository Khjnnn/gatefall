param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot=Join-Path $taskProject 'TestResults/investment-v0190-review'
$taskManifest=@(Get-Content TestResults/source-hashes-v0190.json -Raw|ConvertFrom-Json)
foreach($taskRow in $taskManifest){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Product changed: $($taskRow.path)"}}
$taskPrior=Get-Content TestResults/pulse-v0190-window12.json -Raw|ConvertFrom-Json
$taskOldSource=[IO.File]::ReadAllText((Join-Path $taskProject 'TestResults/pulse-v0190-review/TowerDensityReview-observed.cs')).Replace("`r`n","`n")
# Every change below is an explicit budget gate or read-only local observation.
# Order, Kind, all purchasing branches, card choice, Tick and skill timing are inherited.
$taskNewSource=$taskOldSource
$taskTransforms=@(
    @('public static class TowerDensityReview','public static class InvestmentNecessityReview'),
    @('public int PeakEnemies, PeakEnemiesLate;', 'public int EndingCredits, Spawned, Remaining, CoreDamageEvents, HunterDownEvents, CoreContacts, SkillUses, SupplyCredits, CardOffers, CatalystsGranted; public float Hunter, CoreDamage; public bool AllSpawned; public double FirstCoreDamage, AllSpawnedTime; public string[] Cards; public int[] ShotKinds; public float[] DamageKinds; public Sample[] Samples; public int PeakEnemies, PeakEnemiesLate;'),
    @('static List<int> Order(DefenseSimulation g)', 'public sealed class Sample { public double Time; public int Enemies, Credits, Towers, Kills, Spawned; public float Core; } static List<int> Order(DefenseSimulation g)'),
    @('static Result Run(int index,int seed,string policy)','public static Result Run(int index,int seed,string policy,int cap)'),
    @('var order=Order(g);string why;', 'if(index!=8 || policy!="mixed" || (cap!=270 && cap!=380 && cap!=int.MaxValue))throw new ArgumentException("Limited stage9 mixed budgets only"); var order=Order(g);string why;'),
    @('g.Start();double nextBuy=1;', 'int coreEvents=0,hunterDown=0,skillUses=0,supplyCredits=0;float coreDamage=0;double firstCore=-1,allSpawnedAt=-1,nextSample=30;var contacts=new HashSet<int>();var cards=new List<string>();var samples=new List<Sample>();int[] shotKinds=new int[TowerCatalog.All.Length]; g.Start();double nextBuy=1;'),
    @('if(g.AwaitingCard)g.ChooseCard(g.CardOffers[0]);', 'if(g.AwaitingCard){cards.Add(g.CardOffers[0].ToString());g.ChooseCard(g.CardOffers[0]);}'),
    @('if(g.Elapsed>=nextBuy) {', 'if(g.Elapsed>=nextBuy && g.Telemetry.CreditsSpent<cap) {'),
    @('g.Tick(.05f,dodge?new V2(1,0):new V2());g.UseSkill();', 'g.Tick(.05f,dodge?new V2(1,0):new V2()); foreach(Enemy enemy in g.Enemies)if(enemy.Health>0 && enemy.BlockOwner==0 && enemy.Waypoint==g.Roads[enemy.Lane].Length-1 && V2.Distance(enemy.Position,g.Roads[enemy.Lane][enemy.Waypoint])<=1.14f)contacts.Add(enemy.Id); if(g.UseSkill())skillUses++;'),
    @('int alive=0;foreach(Enemy enemy', 'foreach(CombatEvent e in g.Events){if(e.Kind==EventKind.CoreHurt){coreEvents++;coreDamage+=e.Value;if(firstCore<0)firstCore=g.Elapsed;}if(e.Kind==EventKind.HunterDown)hunterDown++;if(e.Kind==EventKind.TowerShot && e.Source>=0)shotKinds[e.Source]++;if(e.Kind==EventKind.Supply)supplyCredits+=(int)e.Value;} if(g.AllWavesSpawned && allSpawnedAt<0)allSpawnedAt=g.Elapsed; if(g.Elapsed>=nextSample){samples.Add(new Sample{Time=g.Elapsed,Enemies=g.Enemies.Count,Credits=g.Credits,Towers=g.Towers.Count,Kills=g.Kills,Spawned=g.Telemetry.Spawned,Core=g.CoreHealth});nextSample+=30;} int alive=0;foreach(Enemy enemy'),
    @('return new Result {PeakEnemies=peakEnemies,', 'return new Result {EndingCredits=g.Credits,Spawned=g.Telemetry.Spawned,Remaining=g.Enemies.Count,CoreDamageEvents=coreEvents,HunterDownEvents=hunterDown,CoreContacts=contacts.Count,SkillUses=skillUses,SupplyCredits=supplyCredits,CardOffers=g.Telemetry.CardOffers,CatalystsGranted=g.Telemetry.CoresGranted,Hunter=g.PlayerHealth,CoreDamage=coreDamage,AllSpawned=g.AllWavesSpawned,FirstCoreDamage=firstCore,AllSpawnedTime=allSpawnedAt,Cards=cards.ToArray(),ShotKinds=shotKinds,DamageKinds=(float[])g.Telemetry.TowerDamage.Clone(),Samples=samples.ToArray(),PeakEnemies=peakEnemies,'),
    @('rows.Add(Run(stage,seed,policy))','rows.Add(Run(stage,seed,policy,int.MaxValue))')
)
foreach($taskPair in $taskTransforms){
    if(-not $taskNewSource.Contains($taskPair[0])){throw "Missing transformation: $($taskPair[0])"}
    $taskNewSource=$taskNewSource.Replace($taskPair[0],$taskPair[1])
}
$taskReversed=$taskNewSource
for($taskI=$taskTransforms.Count-1;$taskI -ge 0;$taskI--){$taskPair=$taskTransforms[$taskI];$taskReversed=$taskReversed.Replace($taskPair[1],$taskPair[0])}
if($taskReversed -ne $taskOldSource){throw 'Observation/budget transformation is not reversible'}
foreach($taskCore in Get-ChildItem Assets/Scripts/Core -Filter '*.cs') {
    if($taskCore.Name -eq 'Campaign.cs'){
        $taskText=[IO.File]::ReadAllText($taskCore.FullName).Replace('후반에는 정예와 마수가 양쪽에서 짧게 몰려옵니다.','정예가 섞인 무리가 양쪽에서 접근합니다.').Replace('후반에는 양쪽에서 짧게 몰려옵니다. 붕괴의 범위 공격과 정예를 상대할 화력을 준비하세요.','정예를 상대할 화력을 남겨 두세요.')
    }else{$taskText=[IO.File]::ReadAllText($taskCore.FullName)}
    $taskOld=[IO.File]::ReadAllText((Join-Path $taskProject "TestResults/pulse-v0190-review/window12/$($taskCore.Name)"))
    if($taskText.Replace("`r`n","`n") -ne $taskOld.Replace("`r`n","`n")){throw "Core not equivalent to reused results: $($taskCore.Name)"}
}
if(-not $UseRecorded){
    if(Test-Path -LiteralPath $taskRoot){throw 'Preserve existing investment review; use -UseRecorded to verify'}
    New-Item -ItemType Directory -Path $taskRoot|Out-Null
    [IO.File]::WriteAllText((Join-Path $taskRoot 'InvestmentNecessityReview.cs'),$taskNewSource)
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path $taskRoot 'InvestmentNecessityReview.cs'))
    $taskRows=@()
    foreach($taskSeed in @(7,19,41)){foreach($taskCap in @(270,380)){
        $taskModel=[Gatefall.Tests.InvestmentNecessityReview]::Run(8,$taskSeed,'mixed',$taskCap)
        $taskRows+=[pscustomobject]@{budgetCap=$taskCap;reused=$false;model=$taskModel}
    }}
    $taskFull=[Gatefall.Tests.InvestmentNecessityReview]::Run(8,7,'mixed',[int]::MaxValue)
    [pscustomobject]@{created=(Get-Date).ToString('o');productVersion='0.19.0';newLowBudgetConditions=6;selectedFullObservationReplay=1;newCases=$taskRows;fullSeed7Observation=$taskFull}|ConvertTo-Json -Depth 8|Set-Content TestResults/investment-v0190-raw.json
}
$taskRecorded=Get-Content TestResults/investment-v0190-raw.json -Raw|ConvertFrom-Json
if([IO.File]::ReadAllText((Join-Path $taskRoot 'InvestmentNecessityReview.cs')) -ne $taskNewSource){throw 'Recorded harness differs'}
$taskFull=$taskRecorded.fullSeed7Observation
$taskBaseline=@($taskPrior.models|Where-Object Policy -eq mixed)
$taskOldSeed7=@($taskBaseline|Where-Object Seed -eq 7)[0]
foreach($taskField in @('Stage','Seed','Sites','StartingCredits','Towers','Spent','Kills','Fusions','FiredSites','FrequentFiringSites','PeakActive3Seconds','PeakProjectiles','Policy','State','Core','PeakEnemies','PeakEnemiesLate')){
    if($taskFull.$taskField -ne $taskOldSeed7.$taskField){throw "Full seed7 parity differs: $taskField"}
}
if([math]::Abs($taskFull.Time-$taskOldSeed7.Time) -gt .0001 -or [math]::Abs($taskFull.LateEnemySeconds-$taskOldSeed7.LateEnemySeconds) -gt .0001 -or (ConvertTo-Json -Compress -InputObject @($taskFull.PurchaseLog)) -cne (ConvertTo-Json -Compress -InputObject @($taskOldSeed7.PurchaseLog)) -or (ConvertTo-Json -Compress -InputObject @($taskFull.SiteShots)) -cne (ConvertTo-Json -Compress -InputObject @($taskOldSeed7.SiteShots))){throw 'Full seed7 recorded result/ledger/shot parity differs'}
if($taskRecorded.newCases.Count -ne 6){throw 'Expected six low-budget conditions'}
foreach($taskCase in $taskRecorded.newCases){
    $taskRow=$taskCase.model
    if($taskRow.Stage -ne 9 -or $taskRow.Sites -ne 24 -or $taskRow.StartingCredits -ne 280 -or $taskRow.Spent -ne $taskCase.budgetCap -or $taskRow.Towers -ne $(if($taskCase.budgetCap -eq 270){6}else{7})){throw 'Low-budget fixture or actual spending differs'}
    if($taskRow.SupplyCredits -gt 500 -or $taskRow.CardOffers -gt 3 -or $taskRow.CatalystsGranted -gt 2){throw 'Resource cap differs'}
    if($taskRow.State -ne 'Won' -and $taskRow.State -ne 'Lost'){throw 'Nonterminal result'}
    if($taskRow.State -eq 'Won' -and (-not $taskRow.AllSpawned -or $taskRow.Spawned -ne 624 -or $taskRow.Kills -ne 624 -or $taskRow.Remaining -ne 0)){throw 'Victory is not all-spawned annihilation'}
    $taskExpectedPrefix=if($taskCase.budgetCap -eq 270){6}else{9}
    $taskExpectedLog=@($taskBaseline|Where-Object Seed -eq $taskRow.Seed)[0].PurchaseLog[0..($taskExpectedPrefix-1)]
    if((ConvertTo-Json -Compress -InputObject @($taskRow.PurchaseLog)) -cne (ConvertTo-Json -Compress -InputObject @($taskExpectedLog))){throw 'Low-budget ledger is not the expected same-policy prefix'}
    $taskActualCards=@($taskRow.Cards)
    if($taskRow.Seed -eq 7 -and (ConvertTo-Json -Compress -InputObject $taskActualCards) -cne (ConvertTo-Json -Compress -InputObject @($taskFull.Cards)[0..($taskActualCards.Count-1)])){throw 'Same seed7 card prefix differs'}
}
$taskRows=@($taskRecorded.newCases|ForEach-Object {
    $taskRow=$_.model
    [pscustomobject]@{seed=$taskRow.Seed;budget=$_.budgetCap;reused=$false;state=$taskRow.State;time=$taskRow.Time;core=$taskRow.Core;hunter=$taskRow.Hunter;spawned=$taskRow.Spawned;kills=$taskRow.Kills;remaining=$taskRow.Remaining;allSpawned=$taskRow.AllSpawned;towers=$taskRow.Towers;spent=$taskRow.Spent;credits=$taskRow.EndingCredits;coreDamageEvents=$taskRow.CoreDamageEvents;coreDamage=$taskRow.CoreDamage;firstCoreDamage=$taskRow.FirstCoreDamage;contacts=$taskRow.CoreContacts;fired=$taskRow.FiredSites;frequent=$taskRow.FrequentFiringSites;peakRecent=$taskRow.PeakActive3Seconds;peakEnemies=$taskRow.PeakEnemies;fusions=$taskRow.Fusions;cards=@($taskRow.Cards);skillUses=$taskRow.SkillUses;damageKinds=$taskRow.DamageKinds;siteShots=$taskRow.SiteShots}
})
if($taskNewSource.Contains('TryCallNextWave')){throw 'Unexpected call input in inherited policy'}
$taskPairs=@(foreach($taskSeed in @(7,19,41)){
    $taskSix=@($taskRows|Where-Object {$_.seed -eq $taskSeed -and $_.budget -eq 270})[0]
    $taskSeven=@($taskRows|Where-Object {$_.seed -eq $taskSeed -and $_.budget -eq 380})[0]
    $taskFourteen=@($taskBaseline|Where-Object Seed -eq $taskSeed)[0]
    [pscustomobject]@{seed=$taskSeed;timeSavedSixToSeven=$taskSix.time-$taskSeven.time;timeSavedSixToFourteen=$taskSix.time-$taskFourteen.Time;timeSavedSevenToFourteen=$taskSeven.time-$taskFourteen.Time;extraSpendSixToSeven=$taskSeven.spent-$taskSix.spent;extraSpendSixToFourteen=$taskFourteen.Spent-$taskSix.spent;allCore300=($taskSix.core -eq 300 -and $taskSeven.core -eq 300 -and $taskFourteen.Core -eq 300)}
})
$taskResult=[pscustomobject]@{created=(Get-Date).ToString('o');task=169;productVersion='0.19.0';productChanged=$false;harnessTransformsReversible=$true;coreEquivalentToReusedCandidate=$true;reusedFullCases=3;newLowBudgetCases=6;selectedFullObservationReplays=1;selectedFullResultLedgerShotParity=$true;initialPlacement='0 spear,12 binding,9 spear,10 rupture,11 spear,15 spear; 270 credits';cards='first offer; RNG is used only for card offer selection';skills='UseSkill after every .05s Tick, identical existing policy; also moves Hunter0 by dodge, not a damage-only buff';calls=0;unspentMoneyRemoved=$false;priceAdjustments=$false;rows=$taskRows;paired=$taskPairs;reusedFullRows=$taskBaseline;selectedFullObservation=$taskFull;limitations=@('Three seeds and one fixed mixed placement/policy, not all possible strategies or human fun.','SiteShots aggregates the position, including towers replaced by fusion.','Core damage event count is repeated attacks, not leaked enemy count.','CoreContacts samples surviving unblocked enemies within1.14 on the final road leg after Tick.','Full seed19/41 ending credits, core contacts, damage breakdown and skill counts were not newly observed.');humanPlaytested=$false;newAndroidBattle=$false;newApk=$false;newAvd=$false}
$taskResult|ConvertTo-Json -Depth 9|Set-Content TestResults/investment-v0190-comparison.json
$taskRows|Select-Object seed,budget,state,time,core,kills,remaining,towers,spent,credits,coreDamageEvents,fired,frequent,skillUses|Format-Table -AutoSize
"PASS: six limited-budget models; three full cases reused with one observation parity replay; product unchanged"
