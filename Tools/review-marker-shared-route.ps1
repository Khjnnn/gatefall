param([ValidateSet('geometry','scope-audit','control','pilot','remainder')][string]$Mode='geometry',[ValidateSet('shared-south','shallow-east')][string]$Variant='shared-south',[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$prefix=if($Variant -eq 'shallow-east'){'marker-shallow-v0240'}else{'marker-shared-v0240'}
$taskId=if($Variant -eq 'shallow-east'){254}else{253}
$output="TestResults/$prefix-$(if($Mode -eq 'scope-audit'){'geometry'}else{$Mode}).json"
if($Variant -eq 'shallow-east' -and $Mode -in @('scope-audit','control')){throw 'Use recorded253 controls; scope-audit is only for the preserved253 failure'}
if($UseRecorded){$r=Get-Content $output -Raw|ConvertFrom-Json;$r|Select-Object task,mode,geometryValid,newModels,other13Exact,reason|ConvertTo-Json;if($r.models){$r.models|Select-Object Seed,Policy,State,Core,Time,Spent,Towers,FiredSites,FrequentFiringSites,CurrentIdPeakRecent3,IdleSeconds|Format-Table};exit}
if(Test-Path $output){if($Mode -ne 'scope-audit'){throw 'Preserve recorded review'}; $failed=Get-Content $output -Raw|ConvertFrom-Json;if($failed.geometryValid -or $failed.reason -ne 'Other operation changed'){throw 'Only recorded scope audit failure can resume'};Copy-Item -LiteralPath $output -Destination TestResults/marker-shared-v0240-geometry-pre-order-audit.json}
foreach($r in(Get-Content TestResults/source-hashes-v0240-candidate.json -Raw|ConvertFrom-Json)){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Product changed'}}
$root=Join-Path (Get-Location) "TestResults/$prefix-review"
$core=Join-Path $root $(if($Mode -eq 'control'){'baseline-core'}else{'candidate-core'})
if($Mode -in @('geometry','control')){
    if(Test-Path $core){throw 'Preserve source snapshot'}
    New-Item -ItemType Directory -Path $core -Force|Out-Null
    foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $file.FullName -Destination $core}
    if($Mode -eq 'geometry'){
        $before=@'
            L("marker-twin-u-courts",3,
              P(-23,4,-16,4,-16,-4,-7,-4,-7,2,-2,2,0,0),none,
              P(23,4,16,4,16,-4,7,-4,7,2,2,2,0,0)),
'@
        $after=@'
            L("marker-south-shared-approach",3,
              P(-23,4,-16,4,-16,-4,-7,-4,-7,2,-2,2,0,0),none,
              P(23,4,17,4,17,-6,3,-6,3,-4,-7,-4,-7,2,-2,2,0,0)),
'@
        if($Variant -eq 'shallow-east'){
            $after=@'
            L("marker-shallow-east-switchback",3,
              P(-23,4,-16,4,-16,-4,-7,-4,-7,2,-2,2,0,0),none,
              P(23,4,16,4,16,-2,10,-2,10,4,3,4,3,2,0,0)),
'@
        }
        $layout=[IO.File]::ReadAllText((Join-Path $core BattlefieldLayouts.cs)).Replace("`r`n","`n")
        if(([regex]::Matches($layout,[regex]::Escape($before))).Count -ne 1){throw 'Unique stage11 layout missing'}
        [IO.File]::WriteAllText((Join-Path $core BattlefieldLayouts.cs),$layout.Replace($before,$after))
    }
}
if($Mode -in @('geometry','scope-audit')){
    Add-Type -Path (Get-ChildItem $core -Filter '*.cs').FullName
    try {
        $base=(Get-Content TestResults/courts-heavy-v0230-speed115-scope.json -Raw|ConvertFrom-Json).contracts
        $contracts=if($Mode -eq 'scope-audit'){@(Get-Content TestResults/marker-shared-v0240-scope-raw.json -Raw|ConvertFrom-Json)}else{@(foreach($index in 0..13){foreach($crisis in @($false,$true)){
            $g=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[$index];Crisis=$crisis})
            [pscustomobject][ordered]@{stage=$index+1;crisis=$crisis;startingCredits=$g.Credits;supply=$g.WaveSupplyCredits;sockets=@($g.BoardSockets|Select-Object X,Y);roads=@(foreach($lane in 0..3){[pscustomobject][ordered]@{lane=$lane;points=@($g.Roads[$lane]|Select-Object X,Y)}});schedule=@($g.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm,Warned)}
        }})}
        if($Mode -ne 'scope-audit'){$contracts|ConvertTo-Json -Depth 11|Set-Content "TestResults/$prefix-scope-raw.json"}
        if($contracts.Count -ne 28){throw 'Recorded contract coverage differs'}
        foreach($b in $contracts){$match=@($base|Where-Object {$_.stage -eq $b.stage -and $_.crisis -eq $b.crisis});if($match.Count -ne 1){throw 'Stage/crisis key is not unique'};$a=$match[0];if($a.stage -ne 11){if(($a|ConvertTo-Json -Depth 10 -Compress) -cne ($b|ConvertTo-Json -Depth 10 -Compress)){throw 'Other operation changed'}}else{foreach($field in @('stage','crisis','startingCredits','supply','schedule')){if(($a.$field|ConvertTo-Json -Depth 8 -Compress) -cne ($b.$field|ConvertTo-Json -Depth 8 -Compress)){throw "Fixed stage11 field changed: $field"}};if(($a.roads[0]|ConvertTo-Json -Depth 6 -Compress) -cne ($b.roads[0]|ConvertTo-Json -Depth 6 -Compress)){throw 'West road changed'}}}
        $stage=@($contracts|Where-Object {$_.stage -eq 11 -and -not $_.crisis})[0]
        if($stage.sockets.Count -ne 24 -or ($stage.schedule|Measure-Object Count -Sum).Sum -ne 816 -or (($contracts|Where-Object {-not $_.crisis}|ForEach-Object {$_.sockets.Count})|Measure-Object -Sum).Sum -ne 308){throw 'Site/enemy count changed'}
        $nearMin=1000.0;$nearMax=0.0;$gapMin=1000.0
        foreach($p in $stage.sockets){$near=1000.0;foreach($road in $stage.roads|Where-Object {$_.points.Count -gt 1}){$points=[Gatefall.Core.V2[]]@($road.points|ForEach-Object {[Gatefall.Core.V2]::new($_.X,$_.Y)});$near=[math]::Min($near,[Gatefall.Core.BattlefieldLayouts]::RoadDistance([Gatefall.Core.V2]::new($p.X,$p.Y),$points))};$nearMin=[math]::Min($nearMin,$near);$nearMax=[math]::Max($nearMax,$near)}
        for($i=0;$i -lt 24;$i++){for($j=0;$j -lt $i;$j++){$a=$stage.sockets[$i];$b=$stage.sockets[$j];$gapMin=[math]::Min($gapMin,[math]::Sqrt([math]::Pow($a.X-$b.X,2)+[math]::Pow($a.Y-$b.Y,2)))}}
        if($nearMin -lt 2.14 -or $nearMax -gt 3.26 -or $gapMin -lt 2.59){throw 'Socket clearance/reach failed'}
        [ordered]@{created=(Get-Date -Format o);task=$taskId;mode=$Mode;variant=$Variant;geometryValid=$true;newModels=0;staticConstruction28=$contracts;recordedScopeReauditNoConstruction=($Mode -eq 'scope-audit');totalConstructionsIncludingEarlierMissingWarnedAndDiagnostic=$(if($Mode -eq 'scope-audit'){57}else{28});other13Exact=$true;stage11SchedulesEconomyWestExact=$true;sites=24;scheduled=816;totalSites=308;minimumGap=$gapMin;minimumRoad=$nearMin;maximumRoad=$nearMax;productChanged=$false;newApkAvdNative=$false}|ConvertTo-Json -Depth 11|Set-Content $output
        "PASS candidate geometry:24sites/816; clearance $nearMin..$nearMax /gap $gapMin. Rendering not verified."
    }catch{[ordered]@{created=(Get-Date -Format o);task=$taskId;mode=$Mode;geometryValid=$false;reason=$_.Exception.Message;newModels=0;productChanged=$false}|ConvertTo-Json|Set-Content $output;throw}
    exit
}
if(-not (Get-Content "TestResults/$prefix-geometry.json" -Raw|ConvertFrom-Json).geometryValid){throw 'Geometry failed'}
$observer=Get-Content TestResults/courts-flank-v0230-review/AlleyDensityReview.cs -Raw
$seeds=if($Mode -eq 'remainder'){'19,41'}else{'7'}
if(-not $observer.Contains('foreach(int seed in new[]{7,19,41})')){throw 'Seed selector missing'}
$observer=$observer.Replace('foreach(int seed in new[]{7,19,41})',('foreach(int seed in new[]{'+$seeds+'})'))
$observerPath=Join-Path $root "$Mode-observer.cs";[IO.File]::WriteAllText($observerPath,$observer)
Add-Type -Path @((Get-ChildItem $core -Filter '*.cs').FullName+$observerPath)
$models=[Gatefall.Tests.AlleyDensityReview]::Models(10)
[ordered]@{created=(Get-Date -Format o);task=$taskId;mode=$Mode;variant=$Variant;newModels=$models.Count;models=$models;manualSkillEveryTick=$true;geometryRankedPolicyNotFixedWorldPurchases=$true;noMarkerEquippedByGenericPolicy=$true;productChanged=$false;newApkAvdNative=$false}|ConvertTo-Json -Depth 9|Set-Content $output
if($Mode -eq 'control'){
    $prior=(Get-Content TestResults/marker-pulse-v0201-window16.json -Raw|ConvertFrom-Json).models
    foreach($m in $models){$p=@($prior|Where-Object {$_.Seed -eq $m.Seed -and $_.Policy -eq $m.Policy})[0];foreach($field in @('State','Core','Time','Towers','Spent','Credits','Kills','Spawned','Remaining','FiredSites','FrequentFiringSites','IdleSeconds','PeakEnemies','SiteShots','PurchaseLog','Cards')){if(($p.$field|ConvertTo-Json -Compress) -cne ($m.$field|ConvertTo-Json -Compress)){throw "Recorded control mismatch $field / $($m.Policy)"}}}
}
$models|Select-Object Seed,Policy,State,Core,Time,Spent,Towers,FiredSites,FrequentFiringSites,CurrentIdPeakRecent3,IdleSeconds|Format-Table -AutoSize
