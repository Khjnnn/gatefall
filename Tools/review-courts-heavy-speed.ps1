param([ValidateSet('baseline','speed115','speed125')][string]$Candidate='baseline',[ValidateSet('scope','audit','pilot','remainder')][string]$Mode='scope',[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$outputMode=if($Mode -eq 'audit'){'scope'}else{$Mode}
$output="TestResults/courts-heavy-v0230-$Candidate-$outputMode.json"
if($UseRecorded){Get-Content $output -Raw;exit}
if($Mode -ne 'audit' -and (Test-Path $output)){throw 'Preserve recorded evidence; use -UseRecorded'}
foreach($r in(Get-Content TestResults/source-hashes-v0230.json -Raw|ConvertFrom-Json)){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Verified product changed'}}
$root=Join-Path (Get-Location) ("TestResults/courts-heavy-v0230-${Candidate}-review")
$core=Join-Path $root candidate-core
function AuditScope {
    $record=Get-Content $output -Raw|ConvertFrom-Json
    $base=Get-Content TestResults/courts-heavy-v0230-baseline-scope.json -Raw|ConvertFrom-Json
    if(($base.contracts|ConvertTo-Json -Depth 12 -Compress) -cne ($record.contracts|ConvertTo-Json -Depth 12 -Compress)){throw 'Roads/sockets/schedules/economy differ; raw preserved'}
    $scale=if($Candidate -eq 'speed115'){1.15}else{1.25};$changedCells=0
    if($record.stats.Count -ne 840 -or $base.stats.Count -ne 840){throw 'Spawn-stat cell count differs'}
    for($i=0;$i -lt 840;$i++){
        $a=$base.stats[$i];$b=$record.stats[$i]
        foreach($name in @('stage','crisis','wave','kind')){if($a.$name -ne $b.$name){throw "Fixed spawn-stat field differs: $name"}}
        # JSON round-trips a float to a short decimal. Compare unchanged stats at their actual Single precision.
        if([single]$a.hp -ne [single]$b.hp){throw 'Fixed Single HP differs'}
        if($a.stage -eq 5 -and -not $a.crisis -and $a.wave -ge 3 -and $a.kind -in @('Brute','Siege')){if([math]::Abs([single]$a.speed*$scale-[single]$b.speed) -gt .00001){throw 'Selected heavy speed differs'};$changedCells++}
        elseif([single]$a.speed -ne [single]$b.speed){throw 'Unrelated Single speed changed'}
    }
    if($changedCells -ne 6){throw 'Expected only six speed-stat cells'}
    $actual=@($record.contracts|Where-Object {$_.stage -eq 5 -and -not $_.crisis})[0]
    $heavyUnits=(@($actual.schedule|Where-Object {$_.Wave -ge 3 -and [int]$_.Kind -in @(2,3)})|Measure-Object Count -Sum).Sum
    if($heavyUnits -ne 19){throw 'Expected existing19 heavy enemies, not19 stat cells'}
    $record.auditPending=$false;$record|Add-Member -NotePropertyMembers @{auditPassed=$true;changedSpeedStatCells=6;other834SpeedStatCellsUnchanged=$true;actualAffectedLateHeavyEnemies=19;unchangedHpAndSpeedAuditedAtSinglePrecision=$true} -Force
    $record|ConvertTo-Json -Depth 12|Set-Content $output
}
if($Mode -eq 'audit'){if($Candidate -eq 'baseline'){throw 'Baseline is the reference, not a speed candidate'};AuditScope;'PASS recorded840-cell Single-precision audit; no regeneration/combat.';exit}
if($Mode -eq 'scope'){
    if(Test-Path $root){throw 'Preserve existing candidate snapshot'}
    New-Item -ItemType Directory -Path $core -Force|Out-Null
    foreach($file in Get-ChildItem TestResults/courts-pulse-v0230-original-w10-review/candidate-core -Filter '*.cs'){Copy-Item -LiteralPath $file.FullName -Destination $core}
    if($Candidate -ne 'baseline'){
        $original=Get-Content (Join-Path $core DefenseSimulation.cs) -Raw
        $needle='                    if(Rules.Stage.Index==8 && !Rules.Crisis && g.Wave>=3 && (g.Kind==EnemyKind.Brute || g.Kind==EnemyKind.Siege))speed*=1.25f;'
        if(([regex]::Matches($original,[regex]::Escape($needle))).Count -ne 1){throw 'Unique speed insertion anchor missing'}
        $scale=if($Candidate -eq 'speed115'){'1.15'}else{'1.25'}
        $line="                    if(Rules.Stage.Index==4 && !Rules.Crisis && g.Wave>=3 && (g.Kind==EnemyKind.Brute || g.Kind==EnemyKind.Siege))speed*=${scale}f;"
        $changed=$original.Replace($needle,$needle+"`n"+$line)
        if($changed.Replace("`n"+$line,'') -cne $original){throw 'Unrelated speed candidate change'}
        Set-Content -LiteralPath (Join-Path $core DefenseSimulation.cs) -Value $changed -NoNewline
    }
    Add-Type -Path (Get-ChildItem $core -Filter '*.cs').FullName
    $spawn=[Gatefall.Core.DefenseSimulation].GetMethod('Spawn',[Reflection.BindingFlags]'NonPublic,Instance')
    $contracts=@();$stats=@()
    foreach($staticCrisis in @($false,$true)){foreach($stage in [Gatefall.Core.CampaignCatalog]::Stages){
        $g=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=$stage;Crisis=$staticCrisis})
        $contracts+=[ordered]@{stage=$stage.Index+1;crisis=$staticCrisis;startingCredits=$g.Credits;supply=$g.WaveSupplyCredits;sockets=@($g.BoardSockets|Select-Object X,Y);roads=@(for($lane=0;$lane -lt $g.Roads.Length;$lane++){[ordered]@{lane=$lane;points=@($g.Roads[$lane]|Select-Object X,Y)}});schedule=@($g.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Swarm,Warned)}
        foreach($wave in 0..5){foreach($kind in [Enum]::GetValues([Gatefall.Core.EnemyKind])){
            $group=[Gatefall.Core.SpawnGroup]@{Wave=$wave;Lane=$g.OpenLanes[0];Kind=$kind;Count=1}
            $spawn.Invoke($g,@($group))|Out-Null;$e=$g.Enemies[$g.Enemies.Count-1]
            $stats+=[ordered]@{stage=$stage.Index+1;crisis=$staticCrisis;wave=$wave;kind=$kind.ToString();hp=$e.Health;speed=$e.Speed}
            $g.Enemies.Clear()
        }}
    }}
    [ordered]@{created=(Get-Date -Format o);task=250;candidate=$Candidate;newCombatModels=0;constructionCases=28;spawnStatCells=$stats.Count;contracts=$contracts;stats=$stats;productChanged=$false;newApkAvdNative=$false;auditPending=($Candidate -ne 'baseline')}|ConvertTo-Json -Depth 12|Set-Content $output
    if($Candidate -ne 'baseline'){AuditScope}
    "PASS scope $Candidate :28 constructions/840 spawn-stat cells; no combat rerun."
}else{
    if($Candidate -eq 'baseline'){throw 'Reuse249 full9/opening3; baseline combat rerun prohibited'}
    $scope=Get-Content ("TestResults/courts-heavy-v0230-$Candidate-scope.json") -Raw|ConvertFrom-Json
    if(-not $scope.auditPassed -or $scope.auditPending){throw 'Candidate scope audit not passed'}
    $full=Get-Content TestResults/courts-flank-v0230-review/AlleyDensityReview.cs -Raw
    $opening=Get-Content TestResults/courts-opening-v0230-baseline-review/CourtOpeningReview.cs -Raw
    foreach($text in @($full,$opening)){if(([regex]::Matches($text,[regex]::Escape('static Result Run(int index,int seed,string policy)'))).Count -ne 1){throw 'Public observation adapter anchor differs'}}
    $full=$full.Replace('static Result Run(int index,int seed,string policy)','public static Result Run(int index,int seed,string policy)')
    $opening=$opening.Replace('static Result Run(int index,int seed,string policy)','public static Result Run(int index,int seed,string policy)')
    Set-Content -LiteralPath (Join-Path $root full-observer.cs) -Value $full -NoNewline
    Set-Content -LiteralPath (Join-Path $root opening-observer.cs) -Value $opening -NoNewline
    Add-Type -Path @((Get-ChildItem $core -Filter '*.cs').FullName+(Join-Path $root full-observer.cs)+(Join-Path $root opening-observer.cs))
    $cases=if($Mode -eq 'pilot'){@(@{seed=7;policy='many'},@{seed=7;policy='mixed'},@{seed=7;policy='opening'},@{seed=41;policy='many'},@{seed=41;policy='mixed'},@{seed=41;policy='opening'})}else{@(@{seed=7;policy='upgrade'},@{seed=41;policy='upgrade'},@{seed=19;policy='many'},@{seed=19;policy='mixed'},@{seed=19;policy='upgrade'},@{seed=19;policy='opening'})}
    $rows=@(foreach($case in $cases){$r=if($case.policy -eq 'opening'){[Gatefall.Tests.CourtOpeningReview]::Run(4,$case.seed,'many')}else{[Gatefall.Tests.AlleyDensityReview]::Run(4,$case.seed,$case.policy)};[ordered]@{policy=$case.policy;result=$r}})
    [ordered]@{created=(Get-Date -Format o);task=250;candidate=$Candidate;mode=$Mode;newModels=$rows.Count;models=$rows;fullBenchmarkUsesAutomaticSkill=$true;openingHasNoManualSkillMovement=$true;baseline12ReusedFrom249=$true;productChanged=$false;newApkAvdNative=$false}|ConvertTo-Json -Depth 10|Set-Content $output
    $rows|ForEach-Object {[pscustomobject]@{candidate=$Candidate;seed=$_.result.Seed;policy=$_.policy;state=$_.result.State;core=$_.result.Core;time=$_.result.Time;towers=$_.result.Towers;spent=$_.result.Spent;fired=$_.result.FiredSites;frequent=$_.result.FrequentFiringSites;recent=$_.result.CurrentIdPeakRecent3;peak=$_.result.PeakEnemies}}|Format-Table -AutoSize
}
