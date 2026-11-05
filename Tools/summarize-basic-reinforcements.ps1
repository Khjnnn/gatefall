$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$summaries=[Collections.Generic.List[object]]::new()
foreach($name in @('basic-reinforcement-v0285.json','basic-reinforcement-mixed-v0285.json','basic-reinforcement-front-v0285.json')){
    $r=Get-Content (Join-Path $root $name) -Raw|ConvertFrom-Json
    foreach($hash in $r.source){if((Get-FileHash (Join-Path 'Assets/Scripts/Core' $hash.file)).Hash.ToLowerInvariant() -cne $hash.sha256){throw 'Recorded reinforcement core changed'}}
    foreach($pair in ($r.rows|Group-Object Seed)){
        if($pair.Count -ne 2){throw 'Incomplete pair'}
        $wide=@($pair.Group|Where-Object{-not $_.Control})[0];$control=@($pair.Group|Where-Object Control)[0]
        if($wide.PrefixHash -cne $control.PrefixHash -or $wide.Spent -ne $control.Spent -or $wide.ChoiceCost -ne $control.ChoiceCost -or $wide.ChoiceAt -ne $control.ChoiceAt -or ($wide.Cards -join ',') -cne ($control.Cards -join ',')){throw 'Unequal prefix/cost/cards'}
        foreach($row in $pair.Group){if($row.MaxPurchaseDelay -gt .001 -or $row.Credits -lt 0 -or $row.Towers -ne 11 -or $row.CompletedOrders -ne $row.TotalOrders -or [math]::Abs(($row.LaneDamage|Measure-Object -Sum).Sum-(300-$row.Core)) -gt .01){throw 'Timing/ledger/core mismatch'}}
        $summaries.Add([ordered]@{file=$name;seed=$wide.Seed;choiceCost=$wide.ChoiceCost;actualSpend=$wide.Spent;wideState=$wide.State;controlState=$control.State;wideCore=$wide.Core;controlCore=$control.Core;wideEnd=$wide.End;controlEnd=$control.End;bindingShots=$control.Shots[1];bindingDamage=$control.Damage[1];ruptureShots=$control.Shots[2];ruptureDamage=$control.Damage[2];strongSlow=$control.StrongSlowSamples;runnerSlow=$control.RunnerSlowSamples;controlSpawned=$control.Spawned;controlRemaining=$control.Remaining;controlAllSpawned=$control.AllSpawned;prefixEqual=$true})
    }
}
$targets=Get-Content (Join-Path $root 'binding-target-candidate-v0285.json') -Raw|ConvertFrom-Json
if($summaries.Count -ne 6 -or $targets.rows.Count -ne 6 -or -not $targets.selectedWideControlEqual -or $targets.wideBaselineReused -ne 6){throw 'Selected counts differ'}
[ordered]@{baselineNew=12;targetCandidateNew=6;wideControlRerun=1;wideReferencesReused=6;pairs=$summaries;targetPairs=$targets.pairs;adopted=$false;
    conclusion='At these fixed positions, replacing two damage towers with two upgraded binders loses both seeds at710. Mixed binder/area at650 wins both with less core than the spear alternative; moving the area tower to west front also moves an initial spear and loses both, so it is a full placement-policy comparison. Targeting tie-break raises strong-enemy slow samples in all6, rescues one front seed but slightly harms one original mixed seed and does not rescue either double-binding seed. Do not apply a global damage/slow/target buff from this pilot.';
    limitations='Crisis1-12 only, below growth3/research2,1,1,1, seeds7/41, fixed6initial/11final positions and first offered card. Same two new sites and full public prefix within each pair, no purchase delay or price correction. Different packages spend650/710 and are not directly equal-cost across packages. Damage is already included in team telemetry; slow coefficient*enemy-seconds is not prevented damage or exact delay. No human, native or universal investment verdict.';
    interrupted='The first pilot used literal0 as CardKind rather than first offered card and remained paused. Its one owned pwsh9472/parent22984 was identified and stopped; no valid result was counted. The fixed harness explicitly chooses the offered CardKind, fails when paused and saves raw rows before comparison guards.'
}|ConvertTo-Json -Depth 10|Set-Content (Join-Path $root 'basic-reinforcement-comparison-v0285.json') -Encoding utf8
$summaries|ForEach-Object{[pscustomobject]$_}|Format-Table file,seed,actualSpend,wideState,controlState,wideCore,controlCore -AutoSize
'PASS recorded reinforcement review:12 baseline+6 candidate+1 equality control, no product tuning; no model executed by this summary'
