$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
$report=Get-Content "$root/crisis-curve-v0283.json" -Raw|ConvertFrom-Json
$cases=@($report.cases)
if($cases.Count -ne 48 -or @($cases|Group-Object {$_.Outcome.Stage.ToString()+','+$_.Outcome.Seed+','+$_.Outcome.Profile+','+$_.Outcome.Budget+','+$_.PolicyIndex}|Where-Object Count -ne 1).Count){throw 'Expected48 unique curve fixtures'}
$maxDamageError=0.0
foreach($case in $cases){
    $g=$case.Outcome
    if($g.Spent -gt $g.Budget -or $g.Credits -lt 0 -or $g.State -ne 'Won' -or -not $case.AllSpawned -or $case.Remaining -ne 0 -or $case.Spawned -ne $(if($g.Stage -eq 13){928}else{944})){throw 'Economy/completion mismatch'}
    $damage=($case.Hits.Damage|Measure-Object -Sum).Sum
    if($null -eq $damage){$damage=0}
    $delta=[math]::Abs(300-$g.Core-$damage);$maxDamageError=[math]::Max($maxDamageError,$delta)
    if($delta -gt .01){throw 'Core event total mismatch'}
    for($lane=0;$lane -lt 4;$lane++){
        $sum=($case.Hits|Where-Object Lane -eq $lane|Measure-Object Damage -Sum).Sum
        if($null -eq $sum){$sum=0}
        if([math]::Abs($sum-$case.LaneDamage[$lane]) -gt .01){throw 'Lane/event total mismatch'}
    }
}
$earlier=(Get-Content TestResults/night-crisis-middle-budgets.json -Raw|ConvertFrom-Json).rows
$policies=@($cases.Outcome.Policy|Select-Object -Unique)
$reused=@($earlier|Where-Object{$_.Stage -eq 12 -and $_.Policy -in $policies})
if($reused.Count -ne 24){throw 'Expected24 matching-policy previous-stage references'}
$groups=@((@($reused)+@($cases.Outcome))|Group-Object Stage,Profile,Budget|ForEach-Object{
    $g=$_.Group
    [ordered]@{stage=$g[0].Stage;profile=$g[0].Profile;budget=$g[0].Budget;runs=$g.Count;wins=@($g|Where-Object State -eq Won).Count;
        threeStars=@($g|Where-Object Stars -eq 3).Count;minimumCore=($g.Core|Measure-Object -Minimum).Minimum;
        spentRange=@(($g.Spent|Measure-Object -Minimum).Minimum,($g.Spent|Measure-Object -Maximum).Maximum);
        endRange=@(($g.End|Measure-Object -Minimum).Minimum,($g.End|Measure-Object -Maximum).Maximum)}
})
[ordered]@{newCurveConditions=48;reusedStage12Conditions=24;maximumCoreEventError=$maxDamageError;groups=$groups;
    scope='Selected spear and area-fusion policies only. Known-wave scripted planner with idle hunter, not human difficulty/fun. Later stages have different roads, wave mixes and growth recommendations; do not treat raw clear time or three-star counts as an isolated stage-strength effect.'
}|ConvertTo-Json -Depth 7|Set-Content "$root/crisis-curve-comparison-v0283.json"
$groups|ForEach-Object{[pscustomobject]$_}|Format-Table stage,profile,budget,runs,wins,threeStars,minimumCore -AutoSize
'PASS curve completion/economy and per-lane core event reconciliation; no new simulations'
