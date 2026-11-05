param([ValidateSet('geometry','pilot','remainder')][string]$Mode='geometry',[ValidateSet('original','west-meet')][string]$Map='original',[ValidateSet(10,12)][int]$Window=12,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$prefix="courts-pulse-v0230-$Map-w$Window"
$output="TestResults/$prefix-$Mode.json"
if($UseRecorded){Get-Content $output -Raw;exit}
if(Test-Path $output){throw 'Preserve recorded review; use -UseRecorded'}
foreach($r in(Get-Content TestResults/source-hashes-v0230.json -Raw|ConvertFrom-Json)){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Verified product changed'}}
$root=Join-Path (Get-Location) ("TestResults/${prefix}-review");$core=Join-Path $root candidate-core
if($Mode -eq 'geometry'){
    if(Test-Path $root){throw 'Preserve candidate snapshot'}
    New-Item -ItemType Directory -Path $core -Force|Out-Null
    foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $file.FullName -Destination $core}
    if($Map -eq 'west-meet'){Copy-Item -LiteralPath TestResults/courts-west-meet-v0230-review/candidate-core/BattlefieldLayouts.cs -Destination (Join-Path $core BattlefieldLayouts.cs) -Force}
    $raon=Get-Content (Join-Path $core RaonCombat.cs) -Raw
    $before='if(stage.Index==4 && wave>=3) {offset=(j/3)*(15.5/Math.Max(1,(formations-1)/3))+(j%3)*.25;step=.25/multiplier;}'
    $after=@'
if(stage.Index==4 && wave>=3) {
                        double originalGroupStep=15.5/Math.Max(1,(formations-1)/3);
                        double groupOffset=(j/3)*originalGroupStep;
                        if(!Rules.Crisis) {
                            double firstGroupOffset=((j/3)%stage.Lanes.Length)*originalGroupStep;
                            groupOffset=firstGroupOffset+(groupOffset-firstGroupOffset)*(WINDOW.0-firstGroupOffset)/(15.5-firstGroupOffset);
                        }
                        offset=groupOffset+(j%3)*.25;step=.25/multiplier;
                    }
'@
    if(([regex]::Matches($raon,[regex]::Escape($before))).Count -ne 1){throw 'Unique actual Stage5 pack timing anchor missing'}
    Set-Content -LiteralPath (Join-Path $core RaonCombat.cs) -Value ($raon.Replace($before,$after.Replace('WINDOW',[string]$Window))) -NoNewline
}
if($Mode -in @('pilot','remainder')){
    $observer=Get-Content TestResults/courts-flank-v0230-review/AlleyDensityReview.cs -Raw
    $seeds=if($Mode -eq 'remainder'){'19,41'}else{'7'}
    if(-not $observer.Contains('foreach(int seed in new[]{7,19,41})')){throw 'Observer seed selector differs'}
    $observer=$observer.Replace('foreach(int seed in new[]{7,19,41})',('foreach(int seed in new[]{'+$seeds+'})'))
    $observerPath=Join-Path $root "$Mode-observer.cs";Set-Content $observerPath -Value $observer -NoNewline
    Add-Type -Path @((Get-ChildItem $core -Filter '*.cs').FullName+$observerPath)
}else{Add-Type -Path (Get-ChildItem $core -Filter '*.cs').FullName}
if($Mode -eq 'geometry'){
    try {
        $baselinePath=if($Map -eq 'original'){'TestResults/courts-flank-v0230-baseline.json'}else{'TestResults/courts-west-meet-v0230-geometry.json'}
        $baseline=(Get-Content $baselinePath -Raw|ConvertFrom-Json).static28
        $current=@(foreach($index in 0..13){foreach($staticCrisis in @($false,$true)){
            $g=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[$index];Crisis=$staticCrisis})
            [ordered]@{stage=$index+1;crisis=$staticCrisis;sites=@($g.BoardSockets|Select-Object X,Y);lanes=$g.OpenLanes;roads=@(foreach($lane in $g.OpenLanes){[ordered]@{lane=$lane;points=@($g.Roads[$lane]|Select-Object X,Y);length=[Gatefall.Core.BattlefieldLayouts]::RoadLength($g.Roads[$lane])}});startingCredits=$g.Credits;supply=$g.WaveSupplyCredits;scheduled=(@($g.Schedule)|Measure-Object Count -Sum).Sum;schedule=@($g.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm)}
        }})
        $changes=0;$firstSpawns=@()
        for($i=0;$i -lt 28;$i++){
            $a=$baseline[$i];$b=$current[$i]
            foreach($field in @('stage','crisis','sites','lanes','roads','startingCredits','supply','scheduled')){if(($a.$field|ConvertTo-Json -Depth 8 -Compress) -cne ($b.$field|ConvertTo-Json -Depth 8 -Compress)){throw "Geometry/economy field changed: $field"}}
            if($a.schedule.Count -ne $b.schedule.Count){throw 'Generated cell count differs'}
            for($c=0;$c -lt $a.schedule.Count;$c++){
                $old=$a.schedule[$c];$new=$b.schedule[$c]
                foreach($field in @('Wave','Lane','Kind','Count','Interval','Swarm')){if($old.$field -ne $new.$field){throw "Generated cell fixed field changed: $field"}}
                if($a.stage -ne 5 -or $a.crisis -or $old.Wave -lt 3){if($old.Start -ne $new.Start){throw 'Protected arrival time changed'}}elseif($old.Start -ne $new.Start){$changes++}
            }
            if($a.stage -eq 5 -and -not $a.crisis){foreach($wave in 3..5){foreach($lane in @(0,2)){
                $firstA=($a.schedule|Where-Object {$_.Wave -eq $wave -and $_.Lane -eq $lane}|Measure-Object Start -Minimum).Minimum
                $firstB=($b.schedule|Where-Object {$_.Wave -eq $wave -and $_.Lane -eq $lane}|Measure-Object Start -Minimum).Minimum
                if([math]::Abs($firstA-$firstB) -gt .0000001){throw 'First entrance changed'}
                $firstSpawns+=@{wave=$wave;lane=$lane;before=$firstA;after=$firstB}
            }}}
        }
        if($changes -ne 128){throw 'Expected only128 later pack-cell arrivals to change'}
        [ordered]@{created=(Get-Date -Format o);task=249;map=$Map;packWindow=$Window;geometryValid=$true;newModels=0;static28=$current;staticBaseline=$baselinePath;onlyChangedCellStarts=128;initialThreeWavesAllCrisisOther13Exact=$true;geometryAndEconomyUnchanged=$true;speciesCountIntervalSwarmAndFirstEntranceExact=$true;firstSpawns=$firstSpawns;productChanged=$false;newApkAvdNative=$false}|ConvertTo-Json -Depth 12|Set-Content $output
        "PASS28 initializations: $Map pack-span15.5->$Window only128 normal late cell times; first entrances, geometry,780 species/counts/intervals/economy fixed."
    }catch{[ordered]@{created=(Get-Date -Format o);task=249;map=$Map;packWindow=$Window;geometryValid=$false;reason=$_.Exception.Message;newModels=0;productChanged=$false;newApkAvdNative=$false}|ConvertTo-Json|Set-Content $output;throw}
}else{
    $geometry=Get-Content ("TestResults/$prefix-geometry.json") -Raw|ConvertFrom-Json
    if(-not $geometry.geometryValid){throw 'Static scope rejected; combat model not allowed'}
    $models=[Gatefall.Tests.AlleyDensityReview]::Models(4)
    [ordered]@{created=(Get-Date -Format o);task=249;map=$Map;packWindow=$Window;mode=$Mode;newModels=$models.Count;stage=5;models=$models;productChanged=$false;newApkAvdNative=$false;automaticSkillAndBossDodge=$true;geometryRankedPolicyNotFixedWorldPurchases=$true;cardPolicy='first offered'}|ConvertTo-Json -Depth 9|Set-Content $output
    $models|Select-Object Stage,Seed,Policy,State,Core,Time,Towers,Spent,FiredSites,FrequentFiringSites,CurrentIdPeakRecent3,IdleSeconds,PeakEnemies|Format-Table -AutoSize
}
