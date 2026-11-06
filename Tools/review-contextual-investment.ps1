param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/hud-investment-20261106';$old='TestResults/night-balance-20261105';$output="$root/contextual-investment.json"
function Same($a,$b,[string[]]$fields){foreach($f in $fields){if(($a.$f|ConvertTo-Json -Depth 8 -Compress) -cne ($b.$f|ConvertTo-Json -Depth 8 -Compress)){throw ('Equality control differs:'+ $f)}}}
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Recorded review exists; use -UseRecorded'}
    foreach($file in (Get-Content "$root/before-balance-hashes.json" -Raw|ConvertFrom-Json)){
        if((Get-FileHash (Join-Path 'Assets/Scripts/Core' $file.file)).Hash.ToLowerInvariant() -cne $file.sha256){throw 'Historical 0.28.9 comparison requires its exact baseline; use -UseRecorded on the adopted product'}
    }
    $files=@((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+@('Tests/AccountGrowthChecks.cs','Tests/EarlyCombatDigest.cs','Tests/BasicReinforcementReview.cs','Tests/FairSealInvestmentReview.cs'))
    Add-Type -Path $files -CompilerOptions '/langversion:9'
    foreach($s in [Gatefall.Core.CampaignCatalog]::Stages){[Gatefall.Core.BattlefieldLayouts]::Get($s)|Out-Null}
    $basicReference=(Get-Content "$old/basic-reinforcement-v0285.json" -Raw|ConvertFrom-Json).rows|Where-Object{$_.Seed -eq 7 -and $_.Control -and -not $_.Mixed -and -not $_.Front}
    if(@($basicReference).Count -ne 1){throw 'Expected one retained basic reference'}
    $basicControl=[Gatefall.Tests.BasicReinforcementReview]::Play(7,$true)
    $basicControl|ConvertTo-Json -Depth 9|Set-Content "$root/basic-control.json"
    Same $basicReference $basicControl @('State','End','Core','Spent','Credits','Towers','PrefixHash','Cards','Shots','Damage','LaneDamage','Ledger','ChoiceAt','MaxPurchaseDelay')
    $sealReference=(Get-Content "$old/fair-seal-pressure-review.json" -Raw|ConvertFrom-Json).rows|Where-Object{$_.Seed -eq 7 -and $_.Seal -and -not $_.Crisis -and $_.Supplement -eq 0}
    if(@($sealReference).Count -ne 1){throw 'Expected one retained seal reference'}
    $sealControl=[Gatefall.Tests.FairSealInvestmentReview]::PlayFixture(7,$true,$false,7,$false,$true,$true)
    $sealControl|ConvertTo-Json -Depth 9|Set-Content "$root/seal-control.json"
    Same $sealReference $sealControl @('State','End','Core','Spent','Credits','Towers','PrefixHash','Cards','Shots','Damage','Ledger')
    $basic=[Collections.Generic.List[object]]::new();$seal=[Collections.Generic.List[object]]::new()
    function SaveRaw {
        [ordered]@{created=(Get-Date -Format o);basicControl=$basicControl;sealControl=$sealControl;basic=$basic;seal=$seal;source=@($files|ForEach-Object{@{file=$_;sha256=(Get-FileHash $_).Hash.ToLowerInvariant()}});scope='Eight selected1-8contexts (normal/crisis,seed7/41,Binding170/Spear170) plus four1-14lower-growth compact Seal785 anchor4 contexts. Two existing controls matched;other old references reused. Same final price/purchase rules,not fixed purchase timing or worldwide optimal placement. No product stat/price change or Android result.'}|ConvertTo-Json -Depth 10|Set-Content $output
    }
    foreach($crisis in @($false,$true)){foreach($seed in @(7,41)){foreach($control in @($false,$true)){
        $row=[Gatefall.Tests.BasicReinforcementReview]::Play($seed,$control,$false,$false,$false,0,0,7,$crisis);$basic.Add($row);SaveRaw
        if($row.CompletedOrders -ne $row.TotalOrders -or $row.Spent -ne 710 -or $row.Towers -ne 11){throw 'Incomplete stage8 fixture;raw preserved'}
    }}}
    foreach($crisis in @($false,$true)){foreach($seed in @(7,41)){
        $row=[Gatefall.Tests.FairSealInvestmentReview]::PlayFixture($seed,$true,$crisis,4,$false,$true,$true);$seal.Add($row);SaveRaw
        if($row.Spent -ne 785 -or $row.Towers -ne 7){throw 'Incomplete compact seal fixture;raw preserved'}
    }}
}
$r=Get-Content $output -Raw|ConvertFrom-Json
if(@($r.basic).Count -ne 8 -or @($r.seal).Count -ne 4){throw 'Selected review incomplete'}
foreach($pair in ($r.basic|Group-Object Seed,Crisis)){
    if(@($pair.Group.PrefixHash|Select-Object -Unique).Count -ne 1 -or @($pair.Group|Group-Object Control).Count -ne 2){throw 'Basic pair prefix differs'}
}
$r.basic|Select-Object Stage,Crisis,Seed,Control,State,Core,End,Spent,Credits,MaxPurchaseDelay|Format-Table -AutoSize
$r.seal|Select-Object Crisis,Seed,Anchor,State,Core,End,Spent|Format-Table -AutoSize
'PASS selected contextual investment;no stats changed'
