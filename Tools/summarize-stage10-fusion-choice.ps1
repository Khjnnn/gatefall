# Reconcile recorded experiment files,including historical source hashes. Does not replay current product.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
$pilot=Get-Content "$root/stage10-fusion-choice-v0288.json" -Raw|ConvertFrom-Json
$seed19=Get-Content "$root/stage10-fusion-seed19-v0288.json" -Raw|ConvertFrom-Json
$recommended=Get-Content "$root/stage10-fusion-recommended-v0288.json" -Raw|ConvertFrom-Json
$rows=@($pilot.cases)+@($seed19.cases)+@($recommended.cases)
if(-not $pilot.originalEquivalent -or $rows.Count -ne 24){throw 'Missing control/followup rows'}
foreach($r in @($seed19,$recommended)){if(($r.source|ConvertTo-Json -Depth 5 -Compress) -cne ($pilot.source|ConvertTo-Json -Depth 5 -Compress)){throw 'Recorded fixture/core hashes differ'}}
foreach($group in ($rows|Group-Object {$_.outcome.profile+':'+$_.outcome.seed})){
    if(@($group.Group.prefixHash|Sort-Object -Unique).Count -ne 1){throw 'Prechoice public state differs'}
    foreach($r in $group.Group){if($r.prefixSpent -ne 620 -or $r.prefixTowers -ne 8 -or $r.outcome.spent -ne $(if($r.supplement){730}else{665}) -or $r.actions -ne $r.expectedActions -or $r.unmatchedShots -ne 0){throw 'Investment/event mapping mismatch'}}
}
$summary=@($rows|Group-Object {$_.outcome.profile+':'+$_.choice+':'+$_.supplement}|ForEach-Object{
    $g=$_.Group;[ordered]@{group=$_.Name;cases=$g.Count;wins=@($g|Where-Object{$_.outcome.state -ceq 'Won'}).Count;minCore=($g.outcome.core|Measure-Object -Minimum).Minimum;maxCore=($g.outcome.core|Measure-Object -Maximum).Maximum;minEnd=($g.outcome.end|Measure-Object -Minimum).Minimum;maxEnd=($g.outcome.end|Measure-Object -Maximum).Maximum;spent=$g[0].outcome.spent;towers=$g[0].outcome.towers}
})
[ordered]@{created=(Get-Date -Format o);source=$pilot.source;newChoiceModels=24;originalReplayControl=1;originalEquivalent=$true;commonPrefix=@{spent=620;towers=8;choiceTime=100;choiceCredits=45};summary=$summary;productTuning=$false;decision='Preserve tower/enemy values. Complete identical materials at the defended southern approach instead of vacating that front. A general briefing reminder about coverage after fusion is justified;no automatic target/anchor recommendation.';limits='Binding@3 vs fusion differs spatial policy and tower count/catalyst use.13vs15 alone is an anchor comparison. Optional65Rupture@8 is common additional investment,not consumed-site refill. Early losers miss150card;only prechoicecards/stateareidentical. Longer surviving shatter15 accumulates more totaldamage/shots;not isolatedDPS proof. Public prefix does not expose private RNG. No human or Android battle validation.'}|ConvertTo-Json -Depth 8|Set-Content "$root/stage10-fusion-choice-review-v0288.json"
$summary|ForEach-Object{[pscustomobject]$_}|Format-Table -AutoSize
