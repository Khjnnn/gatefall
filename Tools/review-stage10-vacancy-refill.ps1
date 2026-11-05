param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105';$output="$root/stage10-vacancy-refill-v0289.json"
$prior=@((Get-Content "$root/stage10-fusion-choice-v0288.json" -Raw|ConvertFrom-Json).cases)+@((Get-Content "$root/stage10-fusion-seed19-v0288.json" -Raw|ConvertFrom-Json).cases)
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Recorded refill review exists; use -UseRecorded'}
    $files=@((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+@('Tests/AccountGrowthChecks.cs','Tests/EarlyCombatDigest.cs','Tests/Stage10FusionChoiceReview.cs'))
    Add-Type -Path $files -CompilerOptions '/langversion:9'
    foreach($stage in [Gatefall.Core.CampaignCatalog]::Stages){[Gatefall.Core.BattlefieldLayouts]::Get($stage)|Out-Null}
    $control=[Gatefall.Tests.Stage10FusionChoiceReview]::Play(7,$true,'shatter13',$true)
    $before=@($prior|Where-Object{$_.outcome.seed -eq 7 -and $_.choice -ceq 'shatter13' -and $_.supplement})[0]
    foreach($property in $before.PSObject.Properties.Name){if(($before.$property|ConvertTo-Json -Depth 7 -Compress) -cne ($control.$property|ConvertTo-Json -Depth 7 -Compress)){throw ('Refill extension changed prior control:'+ $property)}}
    $cases=[Collections.Generic.List[object]]::new()
    foreach($seed in @(7,19,41)){foreach($choice in @('shatter13','shatter15')){
        $site=if($choice -ceq 'shatter13'){15}else{13}
        $row=[Gatefall.Tests.Stage10FusionChoiceReview]::PlayAt($seed,$true,$choice,$true,$site);$cases.Add($row)
        [ordered]@{source=@($files|ForEach-Object{@{file=$_;sha256=(Get-FileHash $_).Hash.ToLowerInvariant()}});controlEquivalent=$true;control=$control;cases=$cases;scope='Six new lower-growth cases+one equality control. Only120sRupture65 refill location changes8→consumed vacant13/15;choice620+45 and final730/8towers identical. Existing six common-site outcomes reused. No product stats or new native combat.'}|ConvertTo-Json -Depth 10|Set-Content $output
        $old=@($prior|Where-Object{$_.outcome.seed -eq $seed -and $_.choice -ceq $choice -and $_.supplement})[0]
        if($row.prefixHash -cne $old.prefixHash -or $row.outcome.spent -ne 730 -or $row.outcome.towers -ne 8 -or $row.actions -ne $row.expectedActions -or $row.maxPurchaseDelay -gt .1 -or $row.unmatchedShots -ne 0){throw 'Preserved raw:shared prefix/price/count/timing differs'}
    }}
}
$r=Get-Content $output -Raw|ConvertFrom-Json
$r.cases|ForEach-Object{[pscustomobject]@{seed=$_.outcome.seed;choice=$_.choice;refill=$_.supplementSite;state=$_.outcome.state;core=$_.outcome.core;end=[Math]::Round($_.outcome.end,2);spent=$_.outcome.spent;towers=$_.outcome.towers;lane=($_.outcome.laneDamage -join ',')}}|Format-Table -AutoSize
