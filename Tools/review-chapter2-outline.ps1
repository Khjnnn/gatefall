# Static planning audit, not a gameplay simulation or release check.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/chapter2-plan-20261106'
New-Item -ItemType Directory -Path $root -Force|Out-Null
function Require($condition,[string]$why){if(-not $condition){throw $why}}
$p=Get-Content Docs/Design/gatefall_chapter2_stages.json -Raw|ConvertFrom-Json
$overall=Get-Content Docs/Design/gatefall_10chapter_outline.json -Raw|ConvertFrom-Json
Require ($p.status -ceq 'design_proposal_not_runtime' -and $p.chapter -eq 2 -and $p.stages.Count -eq 22 -and $p.stageCount -eq $overall.chapters[1].stageCount) 'Design/runtime or stage budget differs'
Require ($p.entryAfter -ceq 'C01-S014' -and $p.bossStage -ceq 'C02-S022') 'Chapter continuity differs'
Require ($p.mapFamilies.Count -eq 8 -and @($p.mapFamilies.id|Sort-Object -Unique).Count -eq 8) 'Map family brief budget differs'
Require ($p.towerLimit -eq 14 -and $p.startingCredits -eq 320 -and $p.supplyGrants -eq 5 -and $p.supplyCredits -eq 500 -and $p.cardOffers -eq 3 -and $p.catalystsGranted -eq 2) 'Prototype economic baseline differs'
$firstNames=@('다솜 남문','곡물 창고','야시장 삼거리','상가 순환로','냉장 물류로','시장 시계광장')
$late=0;$entries=[Collections.Generic.HashSet[int]]::new();$seen=[Collections.Generic.HashSet[string]]::new()
for($i=0;$i -lt 22;$i++){
    $stage=$p.stages[$i];$expected='C02-S{0:D3}' -f ($i+1)
    Require ($stage.id -ceq $expected -and $seen.Add($stage.name)) 'Stage order/name duplicate'
    if($i -lt 6){Require ($stage.name -ceq $firstNames[$i]) 'First six-stage slice was renamed'}
    $family=@($p.mapFamilies|Where-Object id -CEQ $stage.map)
    Require ($family.Count -eq 1 -and $stage.lanes.Count -eq $family[0].ingressCount -and @($stage.lanes|Sort-Object -Unique).Count -eq $stage.lanes.Count -and @($stage.lanes|Where-Object{$_ -lt 0 -or $_ -gt 3}).Count -eq 0) 'Ingress/family mismatch'
    [void]$entries.Add($stage.lanes.Count)
    Require ($stage.sites -ge 24 -and $stage.sites -le 28 -and $stage.sites -ge $p.towerLimit -and $stage.waves -ge 6 -and $stage.waves -le 8) 'Planned site/wave range differs'
    $act=@($p.acts|Where-Object{$i+1 -ge $_.from -and $i+1 -le $_.to})
    Require ($act.Count -eq 1 -and $act[0].id -eq $stage.act) 'Act ranges overlap or omit a stage'
    if($stage.boss){Require ($stage.id -ceq $p.bossStage -and $stage.bossLane -in $stage.lanes) 'Boss has no declared road'}
    if($null -ne $stage.lateLane){Require ($stage.lateLane -in $stage.lanes -and $stage.warningWave -lt $stage.activeWave -and $stage.activeWave -le $stage.waves) 'Backdoor warning/route invalid';$late++}
}
Require ($p.acts.Count -eq 4 -and @($p.stages|Where-Object boss).Count -eq 1 -and $late -eq 3 -and ($entries|Sort-Object)-join ',' -ceq '1,2,3,4') 'Chapter rhythm/boss/entry variety differs'
foreach($group in ($p.stages|Group-Object map)){
    for($a=0;$a -lt $group.Count;$a++){for($b=$a+1;$b -lt $group.Count;$b++){
        $differences=0;foreach($field in @('primaryFront','wavePattern','investment')){if($group.Group[$a].$field -cne $group.Group[$b].$field){$differences++}}
        Require ($differences -ge 2) 'Repeated family has fewer than two different strategy-brief dimensions'
    }}
}
foreach($item in @(@{id='Scatter';after=2},@{id='Barrage';after=4},@{id='BarrageSealRoute';after=5})){
    Require (@($p.unlocks|Where-Object{$_.id -ceq $item.id -and $_.after -eq $item.after}).Count -eq 1) 'New tower/recipe unlock timing differs'
}
Require (@($p.unlocks|Where-Object{$_.id -ceq 'C03-S001' -and $_.after -eq 22}).Count -eq 1) 'Chapter three link differs'
$baseline=Get-Content TestResults/soft-ui-20261106/product-candidate-hashes.json -Raw|ConvertFrom-Json
$concurrent=@();$core=@($baseline|Where-Object{$_.file -match 'Scripts[\\/]Core[\\/]'})
Require ($core.Count -eq 6) 'Missing core reference'
foreach($f in $core){Require ((Get-FileHash $f.file).Hash.ToLowerInvariant() -ceq $f.sha256) ('Planning assumptions need review after combat core change '+$f.file)}
foreach($f in $baseline){$actual=(Get-FileHash $f.file).Hash.ToLowerInvariant();if($actual -cne $f.sha256){$concurrent+=[ordered]@{file=$f.file;reference=$f.sha256;observed=$actual}}}
# UI changes from the shared workspace are recorded, never restored or called this task's validation.
[ordered]@{status='chapter2_strategy_structure_pass_not_gameplay';stageBriefs=22;actRanges=@('1-6','7-12','13-18','19-22');mapFamilyBriefs=8;ingressCounts=@($entries|Sort-Object);bossStages=@($p.stages|Where-Object boss|Select-Object -ExpandProperty id);advertisedLateEntries=$late;existingFirstSixPreserved=$true;plannedModelConditions=22*4*3*3*2;modelConditionsActuallyRun=0;nativeSessions=0;newAPKsByThisTask=0;newTowersImplemented=0;combatCoreFilesUnchanged=$core.Count;observedConcurrentProductDeltas=$concurrent;unresolved=@('actual road coordinates and tower sites','per-wave counts/spawn times','new tower/recipe and research/growth prices','boss patterns implementation','v3/data foundations','human and Android gameplay');scope=$p.scope}|ConvertTo-Json -Depth 5|Set-Content "$root/structure-review.json" -Encoding utf8
'PASS22strategy briefs/8path families/4acts/declared boss lane/2-4-5unlocks/productunchanged;not22playable levels'
