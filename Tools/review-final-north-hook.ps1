param([ValidateSet('geometry','pilot','remainder','opening')][string]$Mode='geometry',[ValidateSet('hook','short')][string]$Variant='hook',[switch]$Crisis,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$prefix=if($Variant -eq 'hook'){'final-north-v0220'}else{'final-north-short-v0220'}
$output="TestResults/$prefix-$Mode"+$(if($Crisis){'-crisis'}else{''})+'.json'
if($UseRecorded){Get-Content $output -Raw;exit}
if(Test-Path $output){throw 'Preserve recorded evidence; use -UseRecorded'}
foreach($row in(Get-Content TestResults/source-hashes-v0220.json -Raw|ConvertFrom-Json)){if((Get-FileHash $row.path).Hash -ne $row.sha256){throw 'Current product changed'}}
$root=Join-Path (Get-Location) "TestResults/${prefix}-review"
$candidate=Join-Path $root 'candidate-core'
if($Mode -eq 'geometry'){
    if(Test-Path $root){throw 'Preserve source snapshot'}
    New-Item -ItemType Directory -Path $candidate -Force|Out-Null
    foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){
        $prior=[IO.File]::ReadAllText((Join-Path (Get-Location) ('TestResults/final-chicane-v0211-review/west-lower-connector-pilot-window12/'+$file.Name)))
        if($file.Name -eq 'RaonCombat.cs'){$prior=$prior.Replace('arrivalWindow=12.0;if(stage.Index==13','arrivalWindow=12.0;'+[Environment]::NewLine+'                    if(stage.Index==13')}
        if($prior -cne [IO.File]::ReadAllText($file.FullName)){throw 'Current core differs from reusable nine-model baseline'}
        Copy-Item -LiteralPath $file.FullName -Destination $candidate
    }
    $before=@'
            L("observer-offset-shared-spine",1,
              P(-23,-4,-17,-4,-17,4,-8,4,-8,6,6,6,6,2,2,2,0,0),none,
              P(23,6,14,6,14,-5,6,-5,6,2,2,2,0,0))
'@
    $after=@'
            L("observer-three-north-hook",1,
              P(-23,-4,-17,-4,-17,4,-8,4,-8,6,6,6,6,2,2,2,0,0),
              P(7.5f,11,7.5f,7,10.5f,7,10.5f,4,6,4,6,2,2,2,0,0),
              P(23,6,14,6,14,-5,6,-5,6,2,2,2,0,0))
'@
    if($Variant -eq 'short'){
        $after=@'
            L("observer-three-short-north",1,
              P(-23,-4,-17,-4,-17,4,-8,4,-8,6,6,6,6,2,2,2,0,0),
              P(7.5f,11,7.5f,6,6,6,6,2,2,2,0,0),
              P(23,6,14,6,14,-5,6,-5,6,2,2,2,0,0))
'@
    }
    $layout=[IO.File]::ReadAllText('Assets/Scripts/Core/BattlefieldLayouts.cs').Replace("`r`n","`n")
    if(([regex]::Matches($layout,[regex]::Escape($before))).Count -ne 1){throw 'Layout anchor differs'}
    [IO.File]::WriteAllText((Join-Path $candidate BattlefieldLayouts.cs),$layout.Replace($before,$after))
    $campaign=[IO.File]::ReadAllText('Assets/Scripts/Core/Campaign.cs')
    $anchor='S(13,"관측창 설계 시험",24,new[]{0,2},'
    if(([regex]::Matches($campaign,[regex]::Escape($anchor))).Count -ne 1){throw 'Lane anchor differs'}
    [IO.File]::WriteAllText((Join-Path $candidate Campaign.cs),$campaign.Replace($anchor,'S(13,"관측창 설계 시험",24,new[]{0,1,2},'))
    # Keep each existing formation's timing while assigning those cells to three roads.
    $raon=[IO.File]::ReadAllText('Assets/Scripts/Core/RaonCombat.cs')
    $timing='double firstLaneOffset=(j%stage.Lanes.Length)*multiplier*originalStep;'
    if(-not $raon.Contains($timing)){throw 'Pulse timing anchor differs'}
    [IO.File]::WriteAllText((Join-Path $candidate RaonCombat.cs),$raon.Replace($timing,'double firstLaneOffset=(j%(stage.Index==13 ? 2 : stage.Lanes.Length))*multiplier*originalStep;'))
    Copy-Item TestResults/final-chicane-v0211-review/west-lower-connector-pilot-window12/AlleyDensityReview.cs $root
}
if(-not(Test-Path $candidate)){throw 'Run geometry first'}
$observerToCompile=Join-Path $root 'AlleyDensityReview.cs'
if($Crisis){
    if($Mode -eq 'opening' -or $Mode -eq 'geometry'){throw 'Crisis is a separate full-purchase pilot only'}
    $crisisPath=Join-Path $root 'NorthCrisisReview.cs'
    $observer=[IO.File]::ReadAllText($observerToCompile).Replace('public static class AlleyDensityReview','public static class NorthCrisisReview').Replace('Stage=CampaignCatalog.Stages[index]','Stage=CampaignCatalog.Stages[index],Crisis=true')
    if(-not(Test-Path $crisisPath)){[IO.File]::WriteAllText($crisisPath,$observer)}
    $observerToCompile=$crisisPath
}
if($Mode -eq 'opening'){
    $openingPath=Join-Path $root 'NorthOpeningReview.cs'
    $observer=[IO.File]::ReadAllText($observerToCompile).Replace('public static class AlleyDensityReview','public static class NorthOpeningReview').Replace('if(g.Elapsed>=nextBuy)','if(g.Elapsed>=nextBuy && g.Elapsed<0)')
    if(-not(Test-Path $openingPath)){[IO.File]::WriteAllText($openingPath,$observer)}
    $observerToCompile=$openingPath
}
Add-Type -Path @((Get-ChildItem $candidate -Filter '*.cs').FullName+$observerToCompile)
if($Mode -eq 'geometry'){
    try{
        $g=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[13]})
        if($g.BoardSockets.Length -ne 24 -or ($g.OpenLanes -join ',') -ne '0,1,2'){throw 'Expected three entrances/24sites'}
        $minRoad=1000.0;$maxRoad=0.0;$minGap=1000.0
        for($i=0;$i -lt 24;$i++){$p=$g.BoardSockets[$i];$near=1000.0;foreach($lane in $g.OpenLanes){$near=[math]::Min($near,[Gatefall.Core.BattlefieldLayouts]::RoadDistance($p,$g.Roads[$lane]))};if($near -lt 2.14 -or $near -gt 3.26){throw 'Invalid clearance/reach'};$minRoad=[math]::Min($minRoad,$near);$maxRoad=[math]::Max($maxRoad,$near);for($j=0;$j -lt $i;$j++){$gap=[Gatefall.Core.V2]::Distance($p,$g.BoardSockets[$j]);if($gap -lt 2.59){throw 'Overlapping sites'};$minGap=[math]::Min($minGap,$gap)}}
        $static=@(foreach($index in 0..13){foreach($staticCrisis in @($false,$true)){
            $s=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[$index];Crisis=$staticCrisis})
            [ordered]@{stage=$index+1;crisis=$staticCrisis;sites=@($s.BoardSockets|Select-Object X,Y);lanes=$s.OpenLanes;roads=@(foreach($lane in $s.OpenLanes){[ordered]@{lane=$lane;points=@($s.Roads[$lane]|Select-Object X,Y);length=[Gatefall.Core.BattlefieldLayouts]::RoadLength($s.Roads[$lane])}});startingCredits=$s.Credits;supply=$s.WaveSupplyCredits;scheduled=(@($s.Schedule)|Measure-Object Count -Sum).Sum;schedule=@($s.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm)}
        }})
        $alpha=Get-Content TestResults/north-hud-v0203-rift-alpha.json -Raw|ConvertFrom-Json
        $bounds=$alpha.thresholdBounds.'64'
        $screens=@(@{w=2400;h=1080;sh=944;left=0},@{w=1920;h=1080;sh=971;left=0},@{w=1280;h=720;sh=647;left=0},@{w=2400;h=1080;sh=1080;left=136},@{w=1920;h=1080;sh=1080;left=109},@{w=1280;h=720;sh=720;left=73})
        $projection=@(foreach($screen in $screens){foreach($phase in @('Ready','Running')){
            $l=[Gatefall.Core.MobileLayout]::new($screen.w,$screen.h,[Gatefall.Core.Box2]::new($screen.left,0,($screen.w-$screen.left),$screen.sh));$scale=$screen.h/(2*[math]::Max(12.6,24.8/($screen.w/[double]$screen.h)))
            $guards=[Collections.Generic.List[Gatefall.Core.Box2]]::new()
            foreach($lane in $g.OpenLanes){$p=$g.GatePositions[$lane];$cx=($screen.w/2+$p.X*$scale-$l.Origin.X)/$l.Scale;$cy=($screen.h/2-($p.Y-.35)*$scale-$l.Origin.Y)/$l.Scale;$guard=[Gatefall.Core.Box2]::new(($cx-2.55*$scale/$l.Scale),($cy-1.6*$scale/$l.Scale),(5.1*$scale/$l.Scale),(3.2*$scale/$l.Scale));if($guard.Y -lt 62 -and $guard.Y+$guard.Height -gt 0){$guards.Add($guard)}}
            $header=if($phase -eq 'Running'){$l.WaveHeader($guards)}else{[Gatefall.Core.Box2]::new(($l.Width/2-90),4,180,32)}
            $boxes=@([pscustomobject]@{name='wave';box=$header},[pscustomobject]@{name='barrier';box=[Gatefall.Core.Box2]::new(14,10,252,50)},[pscustomobject]@{name='credits';box=[Gatefall.Core.Box2]::new(($l.Width-314),10,128,50)},[pscustomobject]@{name='catalyst';box=[Gatefall.Core.Box2]::new(($l.Width-178),10,86,50)},[pscustomobject]@{name='pause';box=$l.Button([Gatefall.Core.MobileAction]::Pause)})
            if($phase -eq 'Running'){$boxes+=[pscustomobject]@{name='speed';box=$l.Button([Gatefall.Core.MobileAction]::Speed)}}
            $p=$g.GatePositions[1];$px=$screen.w/2+$p.X*$scale;$py=$screen.h/2-($p.Y-.35)*$scale;$pulse=if($phase -eq 'Ready'){1.06}else{1.55};$factor=$pulse*$scale/58
            $ink=[Gatefall.Core.Box2]::new((($px+($bounds[0]-110)*$factor-$l.Origin.X)/$l.Scale),(($py+($bounds[1]-74.2)*$factor-$l.Origin.Y)/$l.Scale),(($bounds[2]-$bounds[0])*$factor/$l.Scale),(($bounds[3]-$bounds[1])*$factor/$l.Scale))
            $clashes=@(foreach($entry in $boxes){$b=$entry.box;if($ink.X -lt $b.X+$b.Width -and $b.X -lt $ink.X+$ink.Width -and $ink.Y -lt $b.Y+$b.Height -and $b.Y -lt $ink.Y+$ink.Height){$entry.name}})
            [ordered]@{screen="$($screen.w)x$($screen.h)";safeLeft=$screen.left;safeHeight=$screen.sh;phase=$phase;northWarningScale=$pulse;ink=($ink|Select-Object X,Y,Width,Height);waveHeader=($header|Select-Object X,Y,Width,Height);clashes=$clashes}
        }})
        [ordered]@{created=(Get-Date -Format o);task=242;geometryValid=$true;baselineSixCoreExactReusable=$true;minimumRoad=$minRoad;maximumRoad=$maxRoad;minimumGap=$minGap;static28=$static;projection=$projection;opaqueBoundsNotPixelExact=$true;newModels=0;productChanged=$false;newApkAvd=$false}|ConvertTo-Json -Depth 12|Set-Content $output
        $projection|ForEach-Object {[pscustomobject]$_}|Select-Object screen,safeLeft,safeHeight,phase,@{n='clashes';e={$_.clashes -join ','}}|Format-Table -AutoSize
    }catch{[ordered]@{task=242;geometryValid=$false;newModels=0;failure=$_.Exception.ToString();productChanged=$false}|ConvertTo-Json -Depth 5|Set-Content $output;throw}
    exit
}
$geo=Get-Content "TestResults/$prefix-geometry.json" -Raw|ConvertFrom-Json
if(-not $geo.geometryValid -or @($geo.projection|Where-Object {$_.clashes.Count -gt 0}).Count){throw 'Review geometry/HUD clashes before battle models'}
if($Mode -eq 'opening'){
    $run=[Gatefall.Tests.NorthOpeningReview].GetMethod('Run',[Reflection.BindingFlags]'NonPublic,Static');$seeds=@(7,41);$policies=@('mixed')
}else{$runType=if($Crisis){[Gatefall.Tests.NorthCrisisReview]}else{[Gatefall.Tests.AlleyDensityReview]};$run=$runType.GetMethod('Run',[Reflection.BindingFlags]'NonPublic,Static');$seeds=if($Mode -eq 'pilot'){@(7)}else{@(19,41)};$policies=@('many','mixed','upgrade')}
$models=@(foreach($seed in $seeds){foreach($policy in $policies){$run.Invoke($null,@(13,$seed,$policy))}})
[ordered]@{created=(Get-Date -Format o);task=242;mode=$Mode;crisis=[bool]$Crisis;newModels=$models.Count;models=$models;productChanged=$false;newApkAvd=$false;scope='Three-lane geometry-ranked purchase policy; cell timings/count/kind/HP unchanged, lane allocation and existing entrance first times change. First cards/automatic skill-dodge; not Native exact replay/human fun.'}|ConvertTo-Json -Depth 9|Set-Content $output
$models|Select-Object Seed,Policy,State,Core,Towers,Time,Spent,FiredSites,FrequentFiringSites,CurrentIdPeakRecent3,PeakEnemies|Format-Table -AutoSize
