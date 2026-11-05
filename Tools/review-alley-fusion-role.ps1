param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskManifest=Get-Content TestResults/source-hashes-v0200.json -Raw|ConvertFrom-Json
foreach($taskRow in $taskManifest){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product changed before role review'}}
$taskOld=@((Get-Content TestResults/alley-v0192-three-clear-courts.json -Raw|ConvertFrom-Json).models|Where-Object Policy -eq mixed)
if(-not $UseRecorded){
    if(Test-Path TestResults/alley-fusion-role-v0200-raw.json){throw 'Preserve role review records'}
    $taskRoot=Join-Path $taskProject 'TestResults/alley-fusion-role-v0200-review'
    New-Item -ItemType Directory -Path $taskRoot|Out-Null
    foreach($taskFile in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskRoot $taskFile.Name)}
    $taskSource=Get-Content TestResults/alley-v0192-review/AlleyDensityReview.cs -Raw
    $taskSwap=@'
            for(int n=0;n<order.Count;n++)order[n]=Map(order[n]);anchor=Map(anchor);partner=Map(partner);
'@
    $taskHelpers=@'
        public static int SwapSocket=-1;
        const int MaximumSpend=1040;
        static int Map(int slot){return SwapSocket<0?slot:slot==12?SwapSocket:slot==SwapSocket?12:slot;}
        static bool BuildCapped(DefenseSimulation g,int socket,TowerKind kind,string command,out string why){
            if(g.Telemetry.CreditsSpent+TowerCatalog.Get(kind).BuildCost>MaximumSpend){why="Review spending cap";return false;}return g.TryBuild(socket,kind,command,out why);
        }
        static bool UpgradeCapped(DefenseSimulation g,int id,string command,out string why){
            Tower t=g.Towers.Find(x=>x.Id==id);if(t==null){why="Missing tower";return false;}
            if(g.Telemetry.CreditsSpent+TowerCatalog.Get(t.Kind).UpgradeCost>MaximumSpend){why="Review spending cap";return false;}return g.TryUpgrade(id,command,out why);
        }
        static bool FuseCapped(DefenseSimulation g,int recipe,int first,int second,int anchor,string command,out string why){
            if(g.Telemetry.CreditsSpent+TowerCatalog.Recipes[recipe].Credits>MaximumSpend){why="Review spending cap";return false;}return g.TryFuse(recipe,first,second,anchor,command,out why);
        }
'@
    $taskDerived=$taskSource.Replace('class AlleyDensityReview','class AlleyFusionRoleReview')
    $taskDerived=$taskDerived.Replace('public int[] SiteShots;','public int[] SiteShots,KindShots; public float[] KindDamage;')
    $taskDerived=$taskDerived.Replace('            int desired=index<3?',($taskSwap+"`n            int desired=index<3?"))
    $taskDerived=$taskDerived.Replace('var cards=new List<string>();','var cards=new List<string>();int[] kindShots=new int[TowerCatalog.All.Length];')
    $taskShot='foreach(CombatEvent e in g.Events)if(e.Kind==EventKind.TowerShot)for'
    if(([regex]::Matches($taskDerived,[regex]::Escape($taskShot))).Count -ne 1){throw 'Shot observer ambiguous'}
    $taskDerived=$taskDerived.Replace($taskShot,'foreach(CombatEvent e in g.Events)if(e.Kind==EventKind.TowerShot && e.Source>=0)kindShots[e.Source]++;'+$taskShot)
    $taskDerived=$taskDerived.Replace('Cards=cards.ToArray()};','Cards=cards.ToArray(),KindShots=kindShots,KindDamage=(float[])g.Telemetry.TowerDamage.Clone()};')
    $taskDerived=$taskDerived.Replace('g.TryBuild(','BuildCapped(g,').Replace('g.TryUpgrade(','UpgradeCapped(g,').Replace('g.TryFuse(','FuseCapped(g,')
    $taskDerived=$taskDerived.Replace('        public sealed class Result',($taskHelpers+"`n        public sealed class Result"))
    $taskPath=Join-Path $taskRoot 'AlleyFusionRoleReview.cs'
    Set-Content -LiteralPath $taskPath -Value $taskDerived -NoNewline
    Add-Type -Path @((Get-ChildItem -LiteralPath $taskRoot -Filter '*.cs').FullName)
    $taskRun=[Gatefall.Tests.AlleyFusionRoleReview].GetMethod('Run',[Reflection.BindingFlags]'Static,NonPublic')
    $taskPilot=@(foreach($taskSwapSocket in @(-1,9,13,17)){
        [Gatefall.Tests.AlleyFusionRoleReview]::SwapSocket=$taskSwapSocket
        [pscustomobject]@{seed=7;swapSocket=$taskSwapSocket;result=$taskRun.Invoke($null,@([int]7,[int]7,'mixed'))}
    })
    # Prefer actual Arc contribution among surviving pilots; this is a local choice, not a global optimum.
    $taskSelected=(@($taskPilot|Where-Object {$_.swapSocket -ge 0 -and $_.result.State -eq 'Won'}|Sort-Object @{e={$_.result.KindDamage[3]};Descending=$true},@{e={$_.result.Time};Descending=$false})[0]).swapSocket
    $taskExtra=@(foreach($taskSeed in @(19,41)){foreach($taskSwapSocket in @(-1,$taskSelected)){
        [Gatefall.Tests.AlleyFusionRoleReview]::SwapSocket=$taskSwapSocket
        [pscustomobject]@{seed=$taskSeed;swapSocket=$taskSwapSocket;result=$taskRun.Invoke($null,@([int]7,[int]$taskSeed,'mixed'))}
    }})
    [pscustomobject]@{created=(Get-Date).ToString('o');sourceVersion='0.20.0';selected=$taskSelected;spendingCap=1040;pilot=$taskPilot;extra=$taskExtra;harnessSha256=(Get-FileHash $taskPath).Hash.ToLowerInvariant()}|ConvertTo-Json -Depth 8|Set-Content TestResults/alley-fusion-role-v0200-raw.json
}
$taskRaw=Get-Content TestResults/alley-fusion-role-v0200-raw.json -Raw|ConvertFrom-Json
$taskRows=@($taskRaw.pilot)+@($taskRaw.extra)
foreach($taskR in $taskRows){if($taskR.result.Spent -gt 1040 -or $taskR.result.SupplyCredits -gt 500 -or $taskR.result.CardOffers -gt 3 -or $taskR.result.Catalysts -gt 2){throw 'Review cap exceeded'}; if($taskR.result.State -eq 'Won' -and ($taskR.result.Kills -ne 612 -or $taskR.result.Spawned -ne 612 -or $taskR.result.Remaining -ne 0 -or -not $taskR.result.AllSpawned)){throw 'Victory before annihilation'}}
foreach($taskControl in @($taskRows|Where-Object swapSocket -eq -1)){
    $taskReference=@($taskOld|Where-Object Seed -eq $taskControl.seed)[0];$taskC=$taskControl.result
    foreach($taskKey in @('Sites','StartingCredits','Towers','Spent','Kills','Fusions','FiredSites','FrequentFiringSites','PeakActive3Seconds','PeakProjectiles','State','Core','Credits','Spawned','Remaining','PeakEnemies','CoreDamageEvents','SupplyCredits','CardOffers','Catalysts')){if($taskC.$taskKey -ne $taskReference.$taskKey){throw "Baseline/cap observation differs: $taskKey"}}
    if([math]::Abs($taskC.Time-$taskReference.Time) -gt .0001 -or (ConvertTo-Json -Compress -InputObject $taskC.PurchaseLog) -cne (ConvertTo-Json -Compress -InputObject $taskReference.PurchaseLog) -or (ConvertTo-Json -Compress -InputObject $taskC.SiteShots) -cne (ConvertTo-Json -Compress -InputObject $taskReference.SiteShots)){throw 'Baseline time/ledger/site shots differ'}
}
foreach($taskRow in $taskManifest){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product changed during review'}}
$taskRows|ForEach-Object{[pscustomobject]@{seed=$_.seed;swap=$_.swapSocket;core=$_.result.Core;time=$_.result.Time;spent=$_.result.Spent;credits=$_.result.Credits;arcShots=$_.result.KindShots[3];arcDamage=$_.result.KindDamage[3];fired=$_.result.FiredSites}}|Format-Table
[pscustomobject]@{created=(Get-Date).ToString('o');task=183;sourceVersion='0.20.0';productChanged=$false;newPilotConditions=3;selectedAdditionalSeeds=2;baselineObserverReplays=3;baselineAllThreeParity=$true;spendingCap=1040;selected=$taskRaw.selected;rows=$taskRows;limitations=@('Swap maps all orders, material upgrades, fusion anchor and replenishment12↔candidate. Initial/early material positions differ; not a pure post60 anchor test.','Same spending ceiling and purchase rule; actual purchase times or total spending may differ. No balance/credits correction.','Three pilots seed7 then selected pair on19/41, not global optimum. All original controls reused for metric parity; missing per-kind observations replayed.','Inherited automatic skill/first-offer cards are different from native0.20.0 input policy; no human fun or survival superiority claim from full-core cases.');newApk=$false;newAvd=$false;newAndroidBattle=$false}|ConvertTo-Json -Depth 9|Set-Content TestResults/alley-fusion-role-v0200-comparison.json
'PASS role position pilot/selected seeds with matched baseline observer; product unchanged'
