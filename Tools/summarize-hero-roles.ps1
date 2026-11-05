$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$rows=[Collections.Generic.List[object]]::new()
$sources=[Collections.Generic.List[object]]::new()
$excluded=[Collections.Generic.List[object]]::new()
$keys=[Collections.Generic.HashSet[string]]::new()
foreach($file in (Get-ChildItem -LiteralPath $root -Filter 'hero-*-v0284.json'|Where-Object Name -NotMatch 'profile-preflight|hero-role-comparison')){
    $report=Get-Content -LiteralPath $file.FullName -Raw|ConvertFrom-Json
    foreach($hash in $report.source){if((Get-FileHash (Join-Path 'Assets/Scripts/Core' $hash.file)).Hash.ToLowerInvariant() -cne $hash.sha256){throw 'Recorded hero core changed'}}
    if($report.excludedFromPlannedComparison){$excluded.Add([ordered]@{file=$file.Name;reason=$report.excludedFromPlannedComparison;cases=@($report.cases).Count});continue}
    $sources.Add([ordered]@{file=$file.Name;sha256=(Get-FileHash $file.FullName).Hash.ToLowerInvariant();cases=@($report.cases).Count})
    foreach($c in $report.cases){
        $o=$c.Outcome;$key=@($o.Stage,$c.Hunter,$c.Motion,$o.Seed,$o.Profile,$c.PolicyIndex,$o.Budget)-join ':'
        if(-not $keys.Add($key)){throw ('Duplicate hero condition '+$key)}
        if($o.Seed -notin @(7,19,41) -or -not $o.Crisis -or $o.State -ne 'Won' -or -not $c.AllSpawned -or $c.Remaining -ne 0 -or $c.Spawned -ne $(if($o.Stage -eq 12){880}else{944}) -or $o.Spent -gt $o.Budget -or $o.Credits -lt 0 -or $o.Towers -gt $o.Limit){throw ('Invalid recorded outcome '+$key)}
        $laneSum=($c.LaneDamage|Measure-Object -Sum).Sum
        $hitSum=($c.Hits.Damage|Measure-Object -Sum).Sum
        if([math]::Abs($laneSum-(300-$o.Core)) -gt .01 -or [math]::Abs($hitSum-$laneSum) -gt .01){throw ('Core damage does not reconcile '+$key)}
        if(($o.Cards|Measure-Object -Sum).Sum -ne 3){throw ('Unexpected card count '+$key)}
        $rows.Add([pscustomobject][ordered]@{Stage=$o.Stage;Hunter=$c.Hunter;Motion=$c.Motion;Seed=$o.Seed;Profile=$o.Profile;Policy=$c.PolicyIndex;Budget=$o.Budget;Spent=$o.Spent;Credits=$o.Credits;AccountSpent=$o.AccountSpent;Growth=$o.Growth;Research=$o.Research;Core=$o.Core;End=$o.End;Stars=$o.Stars;Towers=$o.Towers;Shots=$c.HunterShots;Downs=$c.HunterDowns;DownSeconds=$c.HunterDownSeconds;Skills=$c.SkillUses;Travel=$c.MovementDistance;Cards=$o.Cards;Ledger=$o.Ledger;file=$file.Name})
    }
}
if($rows.Count -ne 51){throw ('Expected 51 selected cases; found '+$rows.Count)}
$groups=@($rows|Group-Object Stage,Hunter,Motion,Profile|ForEach-Object{$g=$_.Group;[ordered]@{key=$_.Name;cases=$g.Count;wins=$g.Count;threeStars=@($g|Where-Object Stars -eq 3).Count;minCore=($g.Core|Measure-Object -Minimum).Minimum;maxCore=($g.Core|Measure-Object -Maximum).Maximum;minSpent=($g.Spent|Measure-Object -Minimum).Minimum;maxSpent=($g.Spent|Measure-Object -Maximum).Maximum;downs=($g.Downs|Measure-Object -Sum).Sum;skillUses=($g.Skills|Measure-Object -Sum).Sum}})
$pairs=@($rows|Where-Object Motion -gt 0|ForEach-Object{
    $moving=$_;$idle=@($rows|Where-Object{$_.Motion -eq 0 -and $_.Stage -eq $moving.Stage -and $_.Hunter -eq $moving.Hunter -and $_.Seed -eq $moving.Seed -and $_.Profile -eq $moving.Profile -and $_.Policy -eq $moving.Policy})
    if($idle.Count -eq 1){
        $base=$idle[0];$sameCards=($moving.Cards|ConvertTo-Json -Compress) -ceq ($base.Cards|ConvertTo-Json -Compress)
        $sameLedger=($moving.Ledger|ConvertTo-Json -Compress) -ceq ($base.Ledger|ConvertTo-Json -Compress)
        $sameOrders=(($moving.Ledger -replace '^\d+(\.\d+)? ','')|ConvertTo-Json -Compress) -ceq (($base.Ledger -replace '^\d+(\.\d+)? ','')|ConvertTo-Json -Compress)
        [ordered]@{stage=$moving.Stage;hunter=$moving.Hunter;seed=$moving.Seed;policy=$moving.Policy;motion=$moving.Motion;idleCore=$base.Core;movingCore=$moving.Core;idleEnd=$base.End;movingEnd=$moving.End;idleShots=$base.Shots;movingShots=$moving.Shots;downs=$moving.Downs;skills=$moving.Skills;sameActualSpend=$moving.Spent -eq $base.Spent;sameCards=$sameCards;sameLedgerIncludingTimes=$sameLedger;sameOrdersIgnoringTimes=$sameOrders}
    }
})
$result=[ordered]@{newSelectedCases=$rows.Count;idleNew=32;additionalDohaIdleSeed19=1;movingOrSkillNew=18;excluded=$excluded;sourceReports=$sources;groups=$groups;pairs=$pairs;cases=$rows;
    reusedKangScope='Compatible idle Kang references are in night-crisis-middle-budgets and crisis-curve-v0283. The unchanged default stage12/below/720/spear/seed7 fixture was executed three times before control caching; these are repeats of one condition, not three new conditions. Current resonance full-field equivalence supports reuse; no prior native battle is claimed as a new APK run.';
    limitations='All selected cases are crisis operations and all win. Motion follows the living enemy nearest the core by straight-line distance and approaches an offset0,-0.8; movement2 also uses the mobility/shield skill within3. No support skill. This changes engagement, deaths, targets and sometimes purchase times/order; it is not isolated skill damage or human handling. Account costs differ by hero. Total shots are hunter shots, not damage or every tower firing. No product/APK/AVD/Android change.'}
$result|ConvertTo-Json -Depth 12|Set-Content (Join-Path $root 'hero-role-comparison-v0284.json') -Encoding utf8
$groups|ForEach-Object{[pscustomobject]$_}|Format-Table -AutoSize
Write-Output ('PASS recorded hero review: '+$rows.Count+' selected cases; excluded '+($excluded.cases|Measure-Object -Sum).Sum+' unintended-seed case. No simulation executed.')
