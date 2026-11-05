param([ValidateSet('seed19','recommended')][string]$Scope,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105';$output="$root/stage10-fusion-$Scope-v0288.json"
$prior=Get-Content "$root/stage10-fusion-choice-v0288.json" -Raw|ConvertFrom-Json
foreach($file in $prior.source){if((Get-FileHash $file.file).Hash.ToLowerInvariant() -cne $file.sha256){throw 'Fixed fixture/core changed'}}
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Recorded followup exists; use -UseRecorded'}
    Add-Type -Path @($prior.source.file) -CompilerOptions '/langversion:9'
    foreach($stage in [Gatefall.Core.CampaignCatalog]::Stages){[Gatefall.Core.BattlefieldLayouts]::Get($stage)|Out-Null}
    $seeds=if($Scope -ceq 'seed19'){@(19)}else{@(7,41)}
    $supplements=if($Scope -ceq 'seed19'){@($false,$true)}else{@($false)}
    $cases=[Collections.Generic.List[object]]::new()
    foreach($seed in $seeds){foreach($supplement in $supplements){foreach($choice in @('binding3','shatter13','shatter15')){
        $row=[Gatefall.Tests.Stage10FusionChoiceReview]::Play($seed,$Scope -ceq 'seed19',$choice,$supplement);$cases.Add($row)
        [ordered]@{source=$prior.source;scope=$Scope;cases=$cases;limits='Same declared actual665/730investments/cards preference and public prefix;not whole-game difficulty/human-fun or universal tower value. SupplementRupture@8 is shared,not automatic refill of the consumed material site.'}|ConvertTo-Json -Depth 10|Set-Content $output
        if($row.actions -ne $row.expectedActions -or $row.outcome.spent -ne $(if($supplement){730}else{665}) -or $row.maxPurchaseDelay -gt .1 -or $row.unmatchedShots -ne 0 -or $row.outcome.state -notin @('Won','Lost')){throw 'Preserved raw:fixture/actual price/card state differs'}
        $same=@($cases|Where-Object{$_.outcome.seed -eq $seed})
        if(@($same.prefixHash|Sort-Object -Unique).Count -ne 1){throw 'Public prechoice state differs'}
    }}}
}
$r=Get-Content $output -Raw|ConvertFrom-Json
$r.cases|ForEach-Object{[pscustomobject]@{seed=$_.outcome.seed;profile=$_.outcome.profile;choice=$_.choice;supplement=$_.supplement;state=$_.outcome.state;core=$_.outcome.core;end=[Math]::Round($_.outcome.end,2);spent=$_.outcome.spent;towers=$_.outcome.towers}}|Format-Table -AutoSize
