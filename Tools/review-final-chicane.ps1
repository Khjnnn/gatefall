param([ValidateSet('baseline','west-chicane','west-connector','west-lower-connector')][string]$Variant='baseline',[ValidateSet('control','pilot','remainder','mixed-all','mixed-rest')][string]$Mode='control',[ValidateSet(22,16,12)][int]$ArrivalWindow=22,[switch]$Crisis,[switch]$OpeningOnly,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$tag="$Variant-$Mode"+$(if($Crisis){'-crisis'}else{''})+$(if($OpeningOnly){'-opening-only'}else{''})+$(if($ArrivalWindow -ne 22){"-window$ArrivalWindow"}else{''});$output="TestResults/final-chicane-v0211-$tag.json"
if($UseRecorded){Get-Content $output -Raw;exit}
if(Test-Path $output){throw 'Preserve recorded models; use -UseRecorded'}
foreach($row in(Get-Content TestResults/source-hashes-v0211.json -Raw|ConvertFrom-Json)){if((Get-FileHash $row.path).Hash -ne $row.sha256){throw 'Product changed before review'}}
$snapshotTag=if($Variant -eq 'baseline' -and $Mode -eq 'control' -and -not $Crisis){'baseline-control-audit-fixed'}else{$tag}
$root=Join-Path (Get-Location) "TestResults/final-chicane-v0211-review/$snapshotTag"
if(Test-Path $root){throw 'Preserve source snapshot'}
New-Item -ItemType Directory -Path $root -Force|Out-Null
foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $file.FullName -Destination $root}
$original=[IO.File]::ReadAllText('Assets/Scripts/Core/BattlefieldLayouts.cs').Replace("`r`n","`n")
$before=@'
            L("observer-long-opposed-courts",1,
              P(-23,-4,-16,-4,-16,5,-6,5,-6,-2,-2,-2,0,0),none,
              P(23,6,14,6,14,-5,6,-5,6,2,2,2,0,0))
'@
$after=@'
            L("observer-west-staggered-chicane",1,
              P(-23,-4,-20,-4,-20,2,-12,2,-12,-5,-4,-5,-4,3,0,0),none,
              P(23,6,14,6,14,-5,6,-5,6,2,2,2,0,0))
'@
if($Variant -eq 'west-connector'){
    $after=@'
            L("observer-west-shared-east-spine",1,
              P(-23,-4,-17,-4,-17,4,-8,4,-8,8,6,8,6,2,2,2,0,0),none,
              P(23,6,14,6,14,-5,6,-5,6,2,2,2,0,0))
'@
}
if($Variant -eq 'west-lower-connector'){
    $after=@'
            L("observer-offset-shared-spine",1,
              P(-23,-4,-17,-4,-17,4,-8,4,-8,6,6,6,6,2,2,2,0,0),none,
              P(23,6,14,6,14,-5,6,-5,6,2,2,2,0,0))
'@
}
$reviewTask=if($ArrivalWindow -ne 22){240}elseif($Variant -eq 'west-lower-connector'){239}elseif($Variant -eq 'west-connector'){238}else{237}
if(([regex]::Matches($original,[regex]::Escape($before))).Count -ne 1){throw 'Selected stage14 layout ambiguous'}
if($Variant -ne 'baseline'){
    $candidate=$original.Replace($before,$after)
    if($candidate.Replace($after,$before) -cne $original){throw 'Unexpected layout scope'}
    [IO.File]::WriteAllText((Join-Path $root 'BattlefieldLayouts.cs'),$candidate)
}
if($ArrivalWindow -ne 22){
    $raon=[IO.File]::ReadAllText('Assets/Scripts/Core/RaonCombat.cs')
    $anchor='if(stage.Index==8 && !Rules.Crisis && wave>=3)arrivalWindow=12.0;'
    $windowLine="if(stage.Index==13 && !Rules.Crisis && wave>=3)arrivalWindow=$ArrivalWindow.0;"
    $oldScope='if((stage.Index==8 || stage.Index==10) && !Rules.Crisis && wave>=3)'
    $newScope='if((stage.Index==8 || stage.Index==10 || stage.Index==13) && !Rules.Crisis && wave>=3)'
    if(-not $raon.Contains($anchor) -or -not $raon.Contains($oldScope)){throw 'Arrival scope anchors differ'}
    $raon=$raon.Replace($anchor,$anchor+$windowLine).Replace($oldScope,$newScope)
    if($raon.Replace($windowLine,'').Replace($newScope,$oldScope) -cne [IO.File]::ReadAllText('Assets/Scripts/Core/RaonCombat.cs')){throw 'Unexpected arrival change'}
    [IO.File]::WriteAllText((Join-Path $root 'RaonCombat.cs'),$raon)
}
$observer=[IO.File]::ReadAllText('TestResults/alley-v0192-review/AlleyDensityReview.cs')
# Preserve the original socket-window metric for historical comparison, and add a
# separate current-ID window so a consumed material cannot lend its shot to a refill.
$observer=$observer.Replace('public int Stage, Seed, Sites,','public int CurrentIdPeakRecent3; public int Stage, Seed, Sites,')
$observer=$observer.Replace('g.Start();double nextBuy=1;','var lastById=new Dictionary<int,double>();int currentPeak=0;g.Start();double nextBuy=1;')
$observer=$observer.Replace('foreach(CombatEvent e in g.Events)if(e.Kind==EventKind.TowerShot)for', 'foreach(CombatEvent ce in g.Events)if(ce.Kind==EventKind.TowerShot){foreach(Tower ct in g.Towers)if(V2.DistanceSquared(g.BoardSockets[ct.Socket],ce.Position)<.001f){lastById[ct.Id]=g.Elapsed;break;}} int current=0;foreach(Tower ct in g.Towers){double lt;if(lastById.TryGetValue(ct.Id,out lt)&&g.Elapsed-lt<=3)current++;}currentPeak=Math.Max(currentPeak,current);foreach(CombatEvent e in g.Events)if(e.Kind==EventKind.TowerShot)for')
$observer=$observer.Replace('return new Result {Stage=', 'return new Result {CurrentIdPeakRecent3=currentPeak,Stage=')
if($Crisis){$observer=$observer.Replace('Stage=CampaignCatalog.Stages[index]','Stage=CampaignCatalog.Stages[index],Crisis=true')}
if($OpeningOnly){$observer=$observer.Replace('if(g.Elapsed>=nextBuy)','if(g.Elapsed>=nextBuy && g.Elapsed<0)')}
[IO.File]::WriteAllText((Join-Path $root 'AlleyDensityReview.cs'),$observer)
Add-Type -Path (Get-ChildItem $root -Filter '*.cs').FullName
try {
    $game=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[13]})
    if($game.BoardSockets.Length -ne 24 -or $game.Credits -ne 320){throw 'Selected sites/start differ'}
    $minRoad=1000.0;$maxRoad=0.0;$minGap=1000.0
    for($i=0;$i -lt 24;$i++){
        $p=$game.BoardSockets[$i];$near=1000.0
        foreach($lane in $game.OpenLanes){$near=[math]::Min($near,[Gatefall.Core.BattlefieldLayouts]::RoadDistance($p,$game.Roads[$lane]))}
        if($near -lt 2.14 -or $near -gt 3.26){throw 'Invalid road clearance/reach'}
        $minRoad=[math]::Min($minRoad,$near);$maxRoad=[math]::Max($maxRoad,$near)
        for($j=0;$j -lt $i;$j++){$gap=[Gatefall.Core.V2]::Distance($p,$game.BoardSockets[$j]);if($gap -lt 2.59){throw 'Overlapping sites'};$minGap=[math]::Min($minGap,$gap)}
    }
    $static=@(foreach($index in 0..13){foreach($staticCrisis in @($false,$true)){
        $g=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[$index];Crisis=$staticCrisis})
        [ordered]@{stage=$index+1;crisis=[bool]$staticCrisis;sites=@($g.BoardSockets|Select-Object X,Y);lanes=$g.OpenLanes;roads=@(foreach($lane in $g.OpenLanes){[ordered]@{lane=$lane;points=@($g.Roads[$lane]|Select-Object X,Y);length=[Gatefall.Core.BattlefieldLayouts]::RoadLength($g.Roads[$lane])}});startingCredits=$g.Credits;supply=$g.WaveSupplyCredits;scheduled=(@($g.Schedule)|Measure-Object Count -Sum).Sum;schedule=@($g.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm)}
    }})
} catch {
    [ordered]@{created=(Get-Date -Format o);task=$reviewTask;variant=$Variant;mode=$Mode;geometryValid=$false;newModels=0;failure=$_.Exception.ToString();productChanged=$false}|ConvertTo-Json -Depth 5|Set-Content $output
    throw
}
$run=[Gatefall.Tests.AlleyDensityReview].GetMethod('Run',[Reflection.BindingFlags]'NonPublic,Static')
$seeds=if($Mode -eq 'mixed-all'){@(7,19,41)}elseif($Mode -eq 'remainder' -or $Mode -eq 'mixed-rest'){@(19,41)}else{@(7)}
$policies=if($Mode -eq 'control' -or $Mode -eq 'mixed-all' -or $Mode -eq 'mixed-rest'){@('mixed')}else{@('many','mixed','upgrade')}
$models=@(foreach($seed in $seeds){foreach($policy in $policies){$run.Invoke($null,@(13,$seed,$policy))}})
$models|ConvertTo-Json -Depth 8|Set-Content ($output.Replace('.json','-models-raw.json'))
$historical=Get-Content TestResults/tower-density-v0170.json -Raw|ConvertFrom-Json
if($Variant -eq 'baseline' -and -not $OpeningOnly -and $ArrivalWindow -eq 22){
    foreach($m in $models){$prior=@($historical.models|Where-Object {$_.Stage -eq 14 -and $_.Seed -eq $m.Seed -and $_.Policy -eq $m.Policy})[0]
        foreach($field in @('State','Core','Sites','StartingCredits','Towers','Spent','Kills','Fusions','FiredSites','FrequentFiringSites','PeakActive3Seconds','PeakProjectiles')){if(-not $Crisis -and $m.$field -ne $prior.$field){throw "Historical control differs: $field"}}
        if(-not $Crisis -and [math]::Abs($m.Time-$prior.Time) -gt .0001){throw 'Historical control time differs'}
    }
}
[ordered]@{created=(Get-Date -Format o);task=$reviewTask;variant=$Variant;mode=$Mode;crisis=[bool]$Crisis;openingOnly=[bool]$OpeningOnly;arrivalWindow=$ArrivalWindow;geometryValid=$true;minimumRoad=$minRoad;maximumRoad=$maxRoad;minimumGap=$minGap;static28=$static;models=$models;newModels=$models.Count;historicalControlMatched=($Variant -eq 'baseline' -and -not $Crisis -and -not $OpeningOnly -and $ArrivalWindow -eq 22);scope='Geometry-ranked same purchase rules, first offered cards, automatic skill/dodge; actual world sites and purchase times may differ. OpeningOnly stops all post-start purchases without deleting earnings. Lifetime socket firing and legacy socket recent metric include consumed materials; CurrentIdPeakRecent3 is separate. No human fun/Native replay.'}|ConvertTo-Json -Depth 12|Set-Content $output
$models|Select-Object Seed,Policy,State,Core,Time,Spent,Credits,FiredSites,FrequentFiringSites,PeakActive3Seconds,CurrentIdPeakRecent3,PeakEnemies,IdleSeconds|Format-Table -AutoSize
