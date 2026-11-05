param([ValidateSet(7,19,41)][int[]]$Seeds=@(7,41),[ValidateSet(0,1,2)][int]$Motion=2,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$output=Join-Path $root ('reinforcement-doha-motion'+$Motion+'-v0285.json')
if(-not $UseRecorded){
    if((Test-Path $output) -or @($Seeds|Select-Object -Unique).Count -ne $Seeds.Count -or $Seeds.Count -eq 0){throw 'Existing result or invalid seed selection'}
    $source=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}})
    $baseline=Get-Content (Join-Path $root 'basic-reinforcement-v0285.json') -Raw|ConvertFrom-Json
    foreach($hash in $baseline.source){if((Get-FileHash (Join-Path 'Assets/Scripts/Core' $hash.file)).Hash.ToLowerInvariant() -cne $hash.sha256){throw 'Prior core differs'}}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+@('Tests/AccountGrowthChecks.cs','Tests/EarlyCombatDigest.cs','Tests/BasicReinforcementReview.cs')|ForEach-Object{(Resolve-Path $_).Path})
    # One equality proof covers the movement-free default; later motion subsets reuse it.
    $controlPath=Join-Path $root 'reinforcement-hero-default-control-v0285.json'
    $harness=(Get-FileHash Tests/BasicReinforcementReview.cs).Hash.ToLowerInvariant()
    if(Test-Path $controlPath){$proof=Get-Content $controlPath -Raw|ConvertFrom-Json;if($proof.harness -cne $harness -or ($proof.source|ConvertTo-Json -Compress) -cne ($source|ConvertTo-Json -Compress)){throw 'Cached default proof differs'}}
    else{$control=[Gatefall.Tests.BasicReinforcementReview]::Play(7,$false);$old=@($baseline.rows|Where-Object{$_.Seed -eq 7 -and -not $_.Control})[0];foreach($field in $old.PSObject.Properties.Name){if(($old.$field|ConvertTo-Json -Compress -Depth 7) -cne ($control.$field|ConvertTo-Json -Compress -Depth 7)){throw ('Default actor changed '+$field)}};[ordered]@{source=$source;harness=$harness;control=$control;equivalent=$true}|ConvertTo-Json -Depth 10|Set-Content $controlPath -Encoding utf8}
    $rows=[Collections.Generic.List[object]]::new()
    foreach($seed in $Seeds){foreach($control in @($false,$true)){
        $rows.Add([Gatefall.Tests.BasicReinforcementReview]::Play($seed,$control,$false,$false,$false,2,$Motion))
        [ordered]@{source=$source;harness=$harness;rows=$rows;scope='Raw selected actor-policy cases saved before pair checks'}|ConvertTo-Json -Depth 10|Set-Content ($output -replace '\.json$','-raw.json') -Encoding utf8
    }}
    foreach($pair in ($rows|Group-Object Seed)){
        $a=$pair.Group[0];$b=$pair.Group[1]
        if($pair.Count -ne 2 -or $a.PrefixHash -cne $b.PrefixHash -or $a.ChoiceAt -ne $b.ChoiceAt -or $a.ChoiceCost -ne 170 -or $b.ChoiceCost -ne 170 -or ($a.Cards -join ',') -cne ($b.Cards -join ',')){throw 'Doha purchase/card/prefix differs'}
        foreach($row in $pair.Group){if($row.Spent -ne 710 -or $row.Towers -ne 11 -or $row.CompletedOrders -ne $row.TotalOrders -or $row.AccountSpent -ne 30 -or [math]::Abs(($row.LaneDamage|Measure-Object -Sum).Sum-(300-$row.Core)) -gt .01){throw 'Doha final/account/core mismatch'}}
    }
    [ordered]@{source=$source;harness=$harness;rows=$rows;defaultControlEqual=$true;scope='Doha growth1/research2,1,1,1, affordable account cost30 as Kang3 control, same tower fixture170/710/11sites. Motion0 idle,1 follows closest-to-core by straight distance with offset0,-0.8;2 same movement plus shield within3. No manual support or calls. Compare tower policies within one hunter/motion, not isolated slow causality or universal hunter rank; no human/native/product change.'}|ConvertTo-Json -Depth 11|Set-Content $output -Encoding utf8
}
$r=Get-Content $output -Raw|ConvertFrom-Json
$r.rows|Select-Object Seed,Control,Hunter,Motion,Growth,AccountSpent,State,Core,End,HunterShots,HunterDowns,SkillUses|Format-Table -AutoSize
'Recorded selected Doha reinforcement policy; no product tuning implied.'
