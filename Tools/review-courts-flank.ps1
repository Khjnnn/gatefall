param([ValidateSet('baseline','baseline-remainder','geometry','pilot','remainder')][string]$Mode='baseline',[ValidateSet('south','west-meet')][string]$Variant='south',[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$prefix=if($Variant -eq 'south'){'courts-flank-v0230'}else{'courts-west-meet-v0230'}
$taskId=if($Variant -eq 'south'){247}else{248}
$output="TestResults/$prefix-$Mode.json"
if($UseRecorded){Get-Content $output -Raw;exit}
if(Test-Path $output){throw 'Preserve recorded review; use -UseRecorded'}
foreach($r in(Get-Content TestResults/source-hashes-v0230.json -Raw|ConvertFrom-Json)){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Verified product changed'}}
$root=Join-Path (Get-Location) ("TestResults/${prefix}-review")
if($Mode -eq 'baseline'){
    if($Variant -ne 'south'){throw 'Use preserved current baseline3; no duplicate baseline models'}
    if(Test-Path $root){throw 'Preserve existing source snapshot'}
    New-Item -ItemType Directory -Path (Join-Path $root baseline-core) -Force|Out-Null
    foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $root baseline-core)}
    $observer=Get-Content TestResults/final-north-short-v0220-review/AlleyDensityReview.cs -Raw
    if(-not $observer.Contains('CurrentIdPeakRecent3')){throw 'Current tower ID participation metric missing'}
    Set-Content -LiteralPath (Join-Path $root AlleyDensityReview.cs) -Value $observer -NoNewline
}
$core=Join-Path $root $(if($Mode -in @('baseline','baseline-remainder')){'baseline-core'}else{'candidate-core'})
if($Mode -eq 'baseline-remainder' -and $Variant -ne 'south'){throw 'Reuse original baseline only'}
if($Mode -eq 'geometry'){
    if(Test-Path $core){throw 'Preserve existing candidate snapshot'}
    New-Item -ItemType Directory -Path $core -Force|Out-Null
    foreach($file in Get-ChildItem TestResults/courts-flank-v0230-review/baseline-core -Filter '*.cs'){
        if((Get-FileHash $file.FullName).Hash -ne (Get-FileHash ('Assets/Scripts/Core/'+$file.Name)).Hash){throw 'Reusable baseline core differs from verified current product'}
        Copy-Item -LiteralPath $file.FullName -Destination $core
    }
    $before=@'
            L("tower-outer-courts",3,
              P(-23,-4,-17,-4,-17,5,-9,5,-9,-3,-3,-3,0,0),none,
              P(23,5,17,5,17,-4,9,-4,9,3,3,3,0,0)),
'@
    $after=@'
            L("tower-east-south-shared-flank",3,
              P(-23,-4,-17,-4,-17,5,-9,5,-9,-3,-3,-3,0,0),none,
              P(23,5,18,5,18,-8,6,-8,6,-3,-9,-3,-3,-3,0,0)),
'@
    if($Variant -eq 'west-meet'){
        $after=@'
            L("tower-west-mid-meet",3,
              P(-23,-4,-17,-4,-17,5,-9,5,3,5,3,3,0,0),none,
              P(23,5,17,5,17,-4,9,-4,9,3,3,3,0,0)),
'@
    }
    $layout=[IO.File]::ReadAllText((Join-Path $core BattlefieldLayouts.cs)).Replace("`r`n","`n")
    if(([regex]::Matches($layout,[regex]::Escape($before))).Count -ne 1){throw 'Unique1-5 layout anchor missing'}
    [IO.File]::WriteAllText((Join-Path $core BattlefieldLayouts.cs),$layout.Replace($before,$after))
}
if($Mode -in @('baseline','baseline-remainder','pilot','remainder')){
    $observer=Get-Content TestResults/courts-flank-v0230-review/AlleyDensityReview.cs -Raw
    $seeds=if($Mode -in @('remainder','baseline-remainder')){'19,41'}else{'7'}
    if(-not $observer.Contains('foreach(int seed in new[]{7,19,41})')){throw 'Observer seed selector differs'}
    $observer=$observer.Replace('foreach(int seed in new[]{7,19,41})',('foreach(int seed in new[]{'+$seeds+'})'))
    $observerPath=Join-Path $root "$Mode-observer.cs"
    Set-Content -LiteralPath $observerPath -Value $observer -NoNewline
    Add-Type -Path @((Get-ChildItem $core -Filter '*.cs').FullName+$observerPath)
}else{Add-Type -Path (Get-ChildItem $core -Filter '*.cs').FullName}
function StaticRows {
    @(foreach($index in 0..13){foreach($staticCrisis in @($false,$true)){
        $g=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[$index];Crisis=$staticCrisis})
        [ordered]@{stage=$index+1;crisis=$staticCrisis;sites=@($g.BoardSockets|Select-Object X,Y);lanes=$g.OpenLanes;roads=@(foreach($lane in $g.OpenLanes){[ordered]@{lane=$lane;points=@($g.Roads[$lane]|Select-Object X,Y);length=[Gatefall.Core.BattlefieldLayouts]::RoadLength($g.Roads[$lane])}});startingCredits=$g.Credits;supply=$g.WaveSupplyCredits;scheduled=(@($g.Schedule)|Measure-Object Count -Sum).Sum;schedule=@($g.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm)}
    }})
}
if($Mode -in @('baseline','baseline-remainder')){
    $static=if($Mode -eq 'baseline'){StaticRows}else{$null}
    $models=[Gatefall.Tests.AlleyDensityReview]::Models(4)
    [ordered]@{created=(Get-Date -Format o);task=$taskId;mode=$Mode;newModels=$models.Count;stage=5;static28=$static;models=$models;productChanged=$false;newApkAvdNative=$false;manualSkillEveryTick=$true;geometryRankedPolicyNotFixedWorldPurchases=$true}|ConvertTo-Json -Depth 12|Set-Content $output
}elseif($Mode -eq 'geometry'){
    try {
        $g=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[4]})
        if($g.BoardSockets.Length -ne 24 -or ($g.OpenLanes -join ',') -ne '0,2'){throw 'Stage5 site/entrance count differs'}
        $nearMin=1000.0;$nearMax=0.0;$gapMin=1000.0
        for($i=0;$i -lt 24;$i++){
            $p=$g.BoardSockets[$i];$near=1000.0;foreach($lane in $g.OpenLanes){$near=[math]::Min($near,[Gatefall.Core.BattlefieldLayouts]::RoadDistance($p,$g.Roads[$lane]))}
            $nearMin=[math]::Min($nearMin,$near);$nearMax=[math]::Max($nearMax,$near)
            if($near -lt 2.14 -or $near -gt 3.26){throw 'Road clearance/reach differs'}
            for($j=0;$j -lt $i;$j++){$gap=[Gatefall.Core.V2]::Distance($p,$g.BoardSockets[$j]);$gapMin=[math]::Min($gapMin,$gap);if($gap -lt 2.59){throw 'Tower sites overlap'}}
        }
        $static=StaticRows
        $old=Get-Content TestResults/courts-flank-v0230-baseline.json -Raw|ConvertFrom-Json
        for($i=0;$i -lt 28;$i++){
            $a=$old.static28[$i];$b=$static[$i]
            if($a.stage -ne 5){if(($a|ConvertTo-Json -Depth 12 -Compress) -cne ($b|ConvertTo-Json -Depth 12 -Compress)){throw 'Other operation differs'}}else{
                foreach($field in @('lanes','startingCredits','supply','scheduled','schedule')){if(($a.$field|ConvertTo-Json -Depth 8 -Compress) -cne ($b.$field|ConvertTo-Json -Depth 8 -Compress)){throw "Stage5 fixed field differs: $field"}}
                $fixedRoad=if($Variant -eq 'south'){0}else{1}
                if(($a.roads[$fixedRoad]|ConvertTo-Json -Depth 6 -Compress) -cne ($b.roads[$fixedRoad]|ConvertTo-Json -Depth 6 -Compress)){throw 'Protected road changed'}
                if($Variant -eq 'west-meet' -and [math]::Abs($a.roads[0].length-$b.roads[0].length) -gt .0001){throw 'West equal-length candidate differs'}
            }
        }
        [ordered]@{created=(Get-Date -Format o);task=$taskId;variant=$Variant;geometryValid=$true;newModels=0;static28=$static;other13Exact=$true;unmodifiedRoadAndAllSchedulesEconomyExact=$true;equalWestLength=($Variant -eq 'west-meet');sites=24;scheduled=780;minimumGap=$gapMin;minimumRoad=$nearMin;maximumRoad=$nearMax;productChanged=$false;newApkAvdNative=$false}|ConvertTo-Json -Depth 12|Set-Content $output
        "Valid candidate geometry:24sites /min gap $gapMin /road $nearMin .. $nearMax /780enemies. No Android rendering verified."
    }catch{[ordered]@{created=(Get-Date -Format o);task=$taskId;geometryValid=$false;reason=$_.Exception.Message;newModels=0;productChanged=$false;newApkAvdNative=$false}|ConvertTo-Json|Set-Content $output;throw}
}else{
    $geometry=Get-Content ("TestResults/$prefix-geometry.json") -Raw|ConvertFrom-Json
    if(-not $geometry.geometryValid){throw 'Geometry failed; no combat models allowed'}
    $models=[Gatefall.Tests.AlleyDensityReview]::Models(4)
    [ordered]@{created=(Get-Date -Format o);task=$taskId;variant=$Variant;mode=$Mode;newModels=$models.Count;stage=5;models=$models;productChanged=$false;newApkAvdNative=$false;manualSkillEveryTick=$true;geometryRankedPolicyNotFixedWorldPurchases=$true}|ConvertTo-Json -Depth 9|Set-Content $output
}
if($Mode -ne 'geometry'){$models|Select-Object Stage,Seed,Policy,State,Core,Time,Towers,Spent,FiredSites,FrequentFiringSites,CurrentIdPeakRecent3,IdleSeconds,PeakEnemies|Format-Table -AutoSize}
