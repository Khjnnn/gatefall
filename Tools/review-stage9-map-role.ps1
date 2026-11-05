param([switch]$Control,[ValidateSet('south-merge','forward-pads')][string]$Variant='south-merge',[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
$name=if($Control){'control'}else{'candidate'}
$output=if($Control){"$root/stage9-south-merge-control-v0287.json"}else{"$root/stage9-$Variant-$name-v0287.json"}
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Recorded map cases exist; use -UseRecorded'}
    $core=if($Control){'Assets/Scripts/Core'}else{"$root/stage9-$Variant-core"}
    Add-Type -Path @((Get-ChildItem $core -Filter '*.cs').FullName+@('Tests/CompetentPlanReview.cs','Tests/AccountGrowthChecks.cs','Tests/MapRoleReview.cs')) -CompilerOptions '/langversion:9'
    foreach($stage in [Gatefall.Core.CampaignCatalog]::Stages){[Gatefall.Core.BattlefieldLayouts]::Get($stage)|Out-Null}
    $baseline=(Get-Content "$root/normal-transition-9-10-v0287.json" -Raw|ConvertFrom-Json).cases
    $jobs=if($Control){@(@{seed=7;budget=600;policy='spear'},@{seed=7;budget=600;policy='area-fusion'},@{seed=7;budget=720;policy='area-fusion'})}
        else{@(foreach($seed in @(7,41)){foreach($budget in @(600,720)){foreach($policy in @('spear','area-fusion')){@{seed=$seed;budget=$budget;policy=$policy}}}})}
    $cases=[Collections.Generic.List[object]]::new()
    foreach($job in $jobs){
        $row=[Gatefall.Tests.MapRoleReview]::Play($job.seed,'below',$job.budget,$job.policy);$cases.Add($row)
        [ordered]@{control=[bool]$Control;cases=$cases;source=@(Get-ChildItem $core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}});scope='Stage9 only. Three selected baseline observations or eight candidate below-profile models. Same policy/ceilings are not fixed world positions,spending,times. Event position+kind matched per-tower shots,not cooldown-upgrade heuristics. No product/Android change.'}|ConvertTo-Json -Depth 10|Set-Content $output -Encoding utf8
        if($row.unmatchedShots -ne 0 -or $row.outcome.spent -gt $job.budget){throw 'Shot mapping/spend guard rejected; raw preserved'}
        if($Control){
            $before=@($baseline|Where-Object{$_.stage -eq 9 -and $_.seed -eq $job.seed -and $_.profile -ceq 'below' -and $_.budget -eq $job.budget -and $_.policy -ceq $job.policy})
            if($before.Count -ne 1){throw 'Baseline reference missing'}
            foreach($property in $before[0].PSObject.Properties.Name){if(($before[0].$property|ConvertTo-Json -Depth 7 -Compress) -cne ($row.outcome.$property|ConvertTo-Json -Depth 7 -Compress)){throw ('Observer changed baseline:'+ $property)}}
        }
    }
}
$r=Get-Content $output -Raw|ConvertFrom-Json
$r.cases|ForEach-Object{[pscustomobject]@{seed=$_.outcome.seed;budget=$_.outcome.budget;policy=$_.outcome.policy;state=$_.outcome.state;core=$_.outcome.core;end=[Math]::Round($_.outcome.end,2);spent=$_.outcome.spent;towers=$_.outcome.towers;fired=$_.firedCurrent;recent=[Math]::Round($_.meanRecent3,2);peak=$_.peakRecent3;alive=$_.maxAlive;nearCore=$_.maxNearCore}}|Format-Table -AutoSize
