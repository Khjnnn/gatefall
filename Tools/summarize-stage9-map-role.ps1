# Reuse recorded results. No simulation, player-save writes, build or device launch.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
$baseline=(Get-Content "$root/normal-transition-9-10-v0287.json" -Raw|ConvertFrom-Json).cases
$control=(Get-Content "$root/stage9-south-merge-control-v0287.json" -Raw|ConvertFrom-Json).cases
$rows=@(foreach($variant in @('south-merge','forward-pads')){
    $candidate=Get-Content "$root/stage9-$variant-candidate-v0287.json" -Raw|ConvertFrom-Json
    if($candidate.cases.Count -ne 8){throw 'Expected eight selected cases per geometry candidate'}
    foreach($row in $candidate.cases){
        $o=$row.outcome
        $before=@($baseline|Where-Object{$_.stage -eq 9 -and $_.profile -ceq $o.profile -and $_.seed -eq $o.seed -and $_.policy -ceq $o.policy -and $_.budget -eq $o.budget})
        if($before.Count -ne 1 -or $o.spawned -ne 624 -or $o.remaining -ne 0 -or -not $o.allSpawned -or $row.unmatchedShots -ne 0){throw 'Recorded result/source mapping invalid'}
        $observation=@($control|Where-Object{$_.outcome.seed -eq $o.seed -and $_.outcome.budget -eq $o.budget -and $_.outcome.policy -ceq $o.policy})
        [ordered]@{variant=$variant;seed=$o.seed;budget=$o.budget;policy=$o.policy;baselineEnd=$before[0].end;candidateEnd=$o.end;clearDelta=$o.end-$before[0].end;baselineSpent=$before[0].spent;candidateSpent=$o.spent;baselineCore=$before[0].core;candidateCore=$o.core;firedCurrent=$row.firedCurrent;meanRecent3=$row.meanRecent3;observedBaselineRecent3=if($observation.Count){$observation[0].meanRecent3}else{$null};observedBaselineFired=if($observation.Count){$observation[0].firedCurrent}else{$null}}
    }
})
$report=[ordered]@{created=(Get-Date -Format o);adopted=$false;productVersion='0.28.7';newCandidateModels=16;selectedBaselineObservations=3;reusedBaselineOutcomes=8;rows=$rows;decision='Reject both limited candidates: longer cleanup/no consistent frequent-shot participation improvement. Preserve stage9 roads/sites. Not a rejection of all asymmetric maps.';limits='Same purchase policy and ceiling,not fixed world positions/purchase timestamps/spending. Shot events mapped by position+kind;three baseline observations match recorded common outcomes exactly. Recent3 mean measured60..170seconds,not longer candidate cleanup;no human-fun or native claim.'}
$report|ConvertTo-Json -Depth 7|Set-Content "$root/stage9-map-role-review-v0287.json" -Encoding utf8
$rows|ForEach-Object{[pscustomobject]@{variant=$_.variant;seed=$_.seed;budget=$_.budget;policy=$_.policy;clearDelta=[Math]::Round($_.clearDelta,2);spent=$_.candidateSpent;fired=$_.firedCurrent;recent=[Math]::Round($_.meanRecent3,2)}}|Format-Table -AutoSize
