param([ValidateSet(7,19,41)][int[]]$Seeds=@(7,41),[switch]$Mixed,[switch]$Front,[switch]$Append,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
if($Front -and -not $Mixed){throw 'Front is a separate mixed-policy comparison'}
$output=Join-Path $project $(if($Front){'TestResults/night-balance-20261105/basic-reinforcement-front-v0285.json'}elseif($Mixed){'TestResults/night-balance-20261105/basic-reinforcement-mixed-v0285.json'}else{'TestResults/night-balance-20261105/basic-reinforcement-v0285.json'})
if(-not $UseRecorded){
    if(@($Seeds|Select-Object -Unique).Count -ne $Seeds.Count){throw 'Duplicate seed selection'}
    if((Test-Path $output) -and -not $Append){throw 'Use -UseRecorded or append only missing seeds'}
    $source=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}})
    $harness=(Get-FileHash Tests/BasicReinforcementReview.cs).Hash.ToLowerInvariant()
    $rows=[Collections.Generic.List[object]]::new()
    if($Append){$old=Get-Content $output -Raw|ConvertFrom-Json;if(($old.source|ConvertTo-Json -Compress) -cne ($source|ConvertTo-Json -Compress) -or $old.harness -cne $harness){throw 'Previous fixture source/harness differs'};foreach($row in $old.rows){$rows.Add($row)}}
    foreach($seed in $Seeds){if(@($rows|Where-Object Seed -eq $seed).Count){throw 'Requested seed already recorded'}}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+@('Tests/AccountGrowthChecks.cs','Tests/EarlyCombatDigest.cs','Tests/BasicReinforcementReview.cs')|ForEach-Object{(Resolve-Path $_).Path})
    foreach($seed in $Seeds){foreach($control in @($false,$true)){
        $rows.Add([Gatefall.Tests.BasicReinforcementReview]::Play($seed,$control,$Mixed.IsPresent,$Front.IsPresent))
        [ordered]@{source=$source;harness=$harness;rows=$rows;scope='Raw results saved before comparison guards; complete selected/pair validation is in the non-raw report.'}|ConvertTo-Json -Depth 10|Set-Content ($output -replace '\.json$','-raw.json') -Encoding utf8
    }}
    foreach($pair in ($rows|Group-Object Seed)){
        $a=@($pair.Group|Where-Object{-not $_.Control})[0];$b=@($pair.Group|Where-Object Control)[0]
        $cost=if($Mixed){110}else{170};$budget=if($Mixed){650}else{710}
        if($a.PrefixHash -cne $b.PrefixHash -or $a.ChoiceAt -ne $b.ChoiceAt -or $a.ChoiceCost -ne $cost -or $b.ChoiceCost -ne $cost){throw 'Common prefix or actual choice cost differs'}
        foreach($row in $pair.Group){if($row.Spent -gt $budget -or $row.State -eq 'Won' -and ($row.Spent -ne $budget -or $row.Towers -ne 11 -or $row.CompletedOrders -ne $row.TotalOrders) -or [math]::Abs(($row.LaneDamage|Measure-Object -Sum).Sum-(300-$row.Core)) -gt .01){throw 'Fixture purchase/count/damage validation failed'}}
    }
    [ordered]@{source=$source;harness=$harness;rows=$rows;mixed=$Mixed.IsPresent;front=$Front.IsPresent;scope='Crisis1-12, below growth3/research2,1,1,1. Idle Kang, first offered card, no manual skill/call/support. Common6spear240 + three upgrades90, choice at60, common three Lv2 spears210. Standard choice170:BindingLv2x2 vs SpearLv2x2+existing spear upgrade, total710. Mixed choice110:BindingLv1+RuptureLv1 vs SpearLv2+SpearLv1, total650. Both11towers and same two new pads within each variant. Front variant also moves initial common spear6to8 and chooses7/6 instead of8/7; not a single-tower-coordinate causal comparison. Actual purchase times/income may change. Slow samples are post-tick coefficient*enemy-seconds, not exact delay or prevented leaks. No product or native change.'}|ConvertTo-Json -Depth 10|Set-Content $output -Encoding utf8
}
$r=Get-Content $output -Raw|ConvertFrom-Json
$r.rows|Select-Object Seed,Control,State,Core,End,Spent,Towers,ChoiceAt,MaxPurchaseDelay,StrongSlowSamples,RunnerSlowSamples|Format-Table -AutoSize
Write-Output ('Recorded equal-cost reinforcement cases: '+$r.rows.Count)
