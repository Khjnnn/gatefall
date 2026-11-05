param([ValidateSet(7,19,41)][int[]]$Seeds=@(7,41),[switch]$Append,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
$output="$root/normal-transition-v0286.json"
if(-not $UseRecorded){
    if($Seeds.Count -eq 0 -or @($Seeds|Select-Object -Unique).Count -ne $Seeds.Count){throw 'Unique nonempty seeds required'}
    if((Test-Path $output) -and -not $Append){throw 'Recorded comparison exists; use -UseRecorded or explicit missing-seed append'}
    $sources=@((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)+@('Tests/CompetentPlanReview.cs','Tests/AccountGrowthChecks.cs')
    $hashes=@($sources|ForEach-Object{[ordered]@{file=[IO.Path]::GetFileName($_);sha256=(Get-FileHash $_).Hash.ToLowerInvariant()}})
    Add-Type -Path $sources
    foreach($stage in [Gatefall.Core.CampaignCatalog]::Stages){[Gatefall.Core.BattlefieldLayouts]::Get($stage)|Out-Null}
    $cases=[Collections.Generic.List[object]]::new()
    if($Append){
        $old=Get-Content $output -Raw|ConvertFrom-Json
        if(($old.source|ConvertTo-Json -Compress) -cne ($hashes|ConvertTo-Json -Compress)){throw 'Recorded core/planner differs'}
        foreach($row in $old.cases){$cases.Add($row)}
    }
    foreach($stage in @(6,7)){foreach($seed in $Seeds){foreach($profile in @('below','recommended')){foreach($budget in @(600,720)){foreach($policy in @('spear','area-fusion')){
        if(@($cases|Where-Object{$_.stage -eq $stage+1 -and $_.seed -eq $seed -and $_.profile -ceq $profile -and $_.budget -eq $budget -and $_.policy -ceq $policy}).Count){throw 'Do not repeat existing case'}
        $account=[Gatefall.Tests.AccountGrowthChecks]::Recommended($stage,0,($(if($profile -eq 'below'){1}else{0})))
        $rules=[Gatefall.Core.CampaignService]::new($account,$null).Battle($stage,$false)
        $plan=[Gatefall.Tests.CompetentPlanReview+Plan]::new();$plan.Spread=.5;$plan.Cap=1
        if($policy -ceq 'spear'){$plan.Upgrade=1}else{$plan.Rupture=.4;$plan.Upgrade=2;$plan.Fuse=$true}
        $g=[Gatefall.Tests.CompetentPlanReview]::Play($stage,$seed,$plan,$false,$budget,$rules,$null,$null)
        $row=[ordered]@{stage=$stage+1;seed=$seed;profile=$profile;budget=$budget;policy=$policy;state=$g.State.ToString();core=$g.MinimumCoreHealth;end=$g.Elapsed;spent=$g.Telemetry.CreditsSpent;credits=$g.Credits;
            growth=$rules.EarnedGrowth;research=$rules.Research;cards=$g.Cards;towers=$g.Towers.Count;fusions=$g.Telemetry.Fusions;spawned=$g.Telemetry.Spawned;remaining=$g.Enemies.Count;allSpawned=$g.AllWavesSpawned;
            stars=[Gatefall.Core.CampaignCatalog]::Stars($g.State -eq [Gatefall.Core.RunState]::Won,$g.MinimumCoreHealth,$g.Balance.CoreHealth);laneDamage=$g.Telemetry.CoreDamageByLane;ledger=$g.Telemetry.Purchases.ToArray()}
        $cases.Add([pscustomobject]$row)
        # Preserve each actual outcome before result guards; this file is not a new model request.
        [ordered]@{source=$hashes;cases=$cases;scope='Normal1-7/8 only,600/720 spending ceilings,below/recommended,idle hunter; fixed card preference Ballistic/Range/Blast/Control/Credits/Hunter. Same rules are not fixed purchase times/positions or equal actual spending. No new Android or product tuning.'}|ConvertTo-Json -Depth 9|Set-Content $output -Encoding utf8
        if($row.spent -gt $budget -or $g.State -in @([Gatefall.Core.RunState]::Paused,[Gatefall.Core.RunState]::Ready,[Gatefall.Core.RunState]::Running)){throw 'Spending cap/nonterminal outcome invalid'}
        if($g.State -eq [Gatefall.Core.RunState]::Won -and (-not $g.AllWavesSpawned -or $g.Enemies.Count -ne 0)){throw 'Not an extermination victory'}
        if([Math]::Abs(($row.laneDamage|Measure-Object -Sum).Sum-(300-$g.CoreHealth)) -gt 25){throw 'Core damage ledger differs beyond final overkill'}
    }}}}}
}
$report=Get-Content $output -Raw|ConvertFrom-Json
$report.cases|Group-Object stage,profile,budget|ForEach-Object{$g=$_.Group;[pscustomobject]@{case=$_.Name;runs=$g.Count;wins=@($g|Where-Object state -eq Won).Count;three=@($g|Where-Object stars -eq 3).Count;minCore=($g.core|Measure-Object -Minimum).Minimum;spentMin=($g.spent|Measure-Object -Minimum).Minimum;spentMax=($g.spent|Measure-Object -Maximum).Maximum}}|Format-Table -AutoSize
"Recorded selected normal transition models:$(@($report.cases).Count); no APK/AVD or product change"
