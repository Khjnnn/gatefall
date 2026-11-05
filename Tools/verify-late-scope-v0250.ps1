$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$before=Get-Content TestResults/v0250-scope-baseline-dto.json -Raw|ConvertFrom-Json
$after=Get-Content TestResults/v0250-scope-product-dto.json -Raw|ConvertFrom-Json
if($before.rows.Count -ne 28 -or $after.rows.Count -ne 28){throw 'Expected normal/crisis28 contracts'}
function Canon($x){return ConvertTo-Json -InputObject $x -Depth 12 -Compress}
$changedStats=@();$changedGeometry=@();$total=0
for($i=0;$i -lt 28;$i++){
    $a=$before.rows[$i];$b=$after.rows[$i]
    if($a.stage -ne $b.stage -or $a.crisis -ne $b.crisis -or $a.start -ne $b.start -or $a.supply -ne $b.supply -or $a.total -ne $b.total -or (Canon $a.schedule) -cne (Canon $b.schedule)){throw 'Budget, species or schedule changed'}
    if(-not $b.crisis){$total+=$b.sites.Count}
    $roadChanged=(Canon $a.roads) -cne (Canon $b.roads);$sitesChanged=(Canon $a.sites) -cne (Canon $b.sites)
    if($roadChanged -and $b.stage -ne 11){throw 'Road changed outside1-11'}
    if($sitesChanged -and $b.stage -notin @(11,12)){throw 'Sites changed outside1-11/12'}
    if($b.stage -eq 12 -and (Canon @($a.sites|Select-Object -First 10)) -cne (Canon @($b.sites|Select-Object -First 10))){throw 'Original1-12 ten sites moved'}
    if($roadChanged -or $sitesChanged){$changedGeometry += [ordered]@{stage=$b.stage;crisis=$b.crisis;roads=$roadChanged;sites=$sitesChanged}}
    for($j=0;$j -lt 30;$j++){
        $x=$a.spawnStats[$j];$y=$b.spawnStats[$j]
        $expected=-not $b.crisis -and $b.stage -in @(11,12) -and $x.Wave -ge 3 -and $x.Kind -in @(2,3)
        $changed=[math]::Abs($x.Health-$y.Health) -gt .001 -or [math]::Abs($x.Speed-$y.Speed) -gt .001
        if($changed -ne $expected){throw 'Unexpected spawn stat change'}
        if($changed){$hp=if($b.stage -eq 11){1.35}else{1.6};if([math]::Abs($y.Health-$x.Health*$hp) -gt .001 -or [math]::Abs($y.Speed-$x.Speed*1.6) -gt .001){throw 'Heavy escort factor differs'};$changedStats += [ordered]@{stage=$b.stage;wave=$x.Wave;kind=$x.Kind;before=$x;after=$y}}
    }
}
if($total -ne 308 -or $changedStats.Count -ne 12 -or $changedGeometry.Count -ne 4){throw 'Expected geometry/spawn scope differs'}
$baseline=@();foreach($name in @('baseline-pilot','baseline-remainder','baseline-roles','baseline-roles19')){$baseline+=@(Get-Content ("TestResults/v0250-$name.json") -Raw|ConvertFrom-Json).rows}
$selected=@();foreach($name in @('selected-pilot4','selected-remainder','selected-roles')){$selected+=@(Get-Content ("TestResults/v0250-$name.json") -Raw|ConvertFrom-Json).rows}
if($baseline.Count -ne 36 -or $selected.Count -ne 36){throw 'Expected36 paired policies/seeds'}
$paired=@(foreach($b in $selected){$a=@($baseline|Where-Object {$_.Stage -eq $b.Stage -and $_.Seed -eq $b.Seed -and $_.Cap -eq $b.Cap -and $_.Policy -eq $b.Policy});if($a.Count -ne 1){throw 'Missing unique control'};if($b.Cap -eq 16 -and ($b.State -ne 'Won' -or $b.Core -ne 300)){throw 'Reinforced strategy failed'};[ordered]@{stage=$b.Stage;seed=$b.Seed;cap=$b.Cap;policy=$b.Policy;cardChoicesEqual=((Canon $a[0].ChosenCards) -ceq (Canon $b.ChosenCards));before=$a[0];after=$b}})
[ordered]@{created=(Get-Date -Format o);staticContracts=28;spawnSamplesEach=840;changedSpawnCells=$changedStats;sharedGeometryChanges=$changedGeometry;allSchedulesCountsEconomyUnchanged=$true;all308Sites=$true;earlyThreeOperationsUnchanged=$true;crisisStatsUnchanged=$true;crisisSharesRedesignedGeometry=$true;baselineModels=36;selectedModels=36;reinforcedAll18WonWithoutDamage=$true;matchingCardPolicies=@($paired|Where-Object cardChoicesEqual).Count;paired=$paired;limits='Road/site ranking may change purchase positions and kill income/times. Actual cards recorded; no pure stat-only causal or human-fun claim.';passed=$true}|ConvertTo-Json -Depth 14|Set-Content TestResults/v0250-late-comparison.json
'PASS28 schedule/economy contracts,308 sites,12/840 targeted stat cells only,early3 preserved;36paired models/reinforced18 all300.'
