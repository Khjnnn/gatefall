param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105';$output="$root/stage10-fusion-choice-v0288.json"
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Recorded review exists; use -UseRecorded'}
    $files=@((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+@('Tests/AccountGrowthChecks.cs','Tests/EarlyCombatDigest.cs','Tests/Stage10FusionChoiceReview.cs'))
    Add-Type -Path $files -CompilerOptions '/langversion:9'
    foreach($stage in [Gatefall.Core.CampaignCatalog]::Stages){[Gatefall.Core.BattlefieldLayouts]::Get($stage)|Out-Null}
    $hashes=@($files|ForEach-Object{@{file=$_;sha256=(Get-FileHash $_).Hash.ToLowerInvariant()}})
    $control=[Gatefall.Tests.Stage10FusionChoiceReview]::Play(7,$true,'original',$false)
    $reference=@((Get-Content "$root/normal-transition-9-10-v0287.json" -Raw|ConvertFrom-Json).cases|Where-Object{$_.stage -eq 10 -and $_.seed -eq 7 -and $_.profile -ceq 'below' -and $_.policy -ceq 'mixed' -and $_.budget -eq 600})
    [ordered]@{source=$hashes;originalControl=$control;cases=@()}|ConvertTo-Json -Depth 9|Set-Content $output
    if($reference.Count -ne 1){throw 'Original outcome reference missing'}
    foreach($property in $reference[0].PSObject.Properties.Name){if(($reference[0].$property|ConvertTo-Json -Depth 6 -Compress) -cne ($control.outcome.$property|ConvertTo-Json -Depth 6 -Compress)){throw ('Fixed replay differs from original: '+$property)}}
    $cases=[Collections.Generic.List[object]]::new()
    foreach($seed in @(7,41)){foreach($supplement in @($false,$true)){foreach($choice in @('binding3','shatter13','shatter15')){
        $row=[Gatefall.Tests.Stage10FusionChoiceReview]::Play($seed,$true,$choice,$supplement);$cases.Add($row)
        [ordered]@{source=$hashes;originalControl=$control;originalEquivalent=$true;cases=$cases;scope='One-stage lower-growth pilot. Actual common620 prefix,100s45 choice;optional same120s65Rupture@8. Binding@3 vs fusion is a spatial policy choice;13vs15 fusion alone changes anchor. No after-choice automatic purchases,skills,calls or product changes. Public-state prefix includes armor,not private RNG snapshot.'}|ConvertTo-Json -Depth 10|Set-Content $output
        if($row.actions -ne $row.expectedActions -or $row.outcome.spent -ne $(if($supplement){730}else{665}) -or $row.unmatchedShots -ne 0 -or $row.maxPurchaseDelay -gt .1 -or $row.outcome.state -notin @('Won','Lost')){throw 'Raw preserved:incomplete/unequal fixture or delayed purchases'}
        if($row.outcome.state -eq 'Won' -and (-not $row.outcome.allSpawned -or $row.outcome.remaining -ne 0)){throw 'Not all scheduled enemies cleared'}
        $same=@($cases|Where-Object{$_.outcome.seed -eq $seed})
        if(@($same.prefixHash|Sort-Object -Unique).Count -ne 1){throw 'Choice state prefix differs'}
    }}}
}
$r=Get-Content $output -Raw|ConvertFrom-Json
$r.cases|ForEach-Object{[pscustomobject]@{seed=$_.outcome.seed;choice=$_.choice;supplement=$_.supplement;state=$_.outcome.state;core=$_.outcome.core;end=[Math]::Round($_.outcome.end,2);spent=$_.outcome.spent;towers=$_.outcome.towers;shatterDamage=[Math]::Round($_.damage[4],1);shatterShots=$_.shots[4];fired=$_.currentFired;delay=$_.maxPurchaseDelay}}|Format-Table -AutoSize
