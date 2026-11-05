param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
$output="$root/transition-anchor-v0286.json"
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Recorded anchor pilot exists; use -UseRecorded'}
    $baseline=Get-Content "$root/normal-transition-v0286.json" -Raw|ConvertFrom-Json
    foreach($hash in $baseline.source){$path=if($hash.file -in @('AccountGrowthChecks.cs','CompetentPlanReview.cs')){'Tests/'+$hash.file}else{'Assets/Scripts/Core/'+$hash.file};if((Get-FileHash $path).Hash.ToLowerInvariant() -cne $hash.sha256){throw 'Recorded planner/core changed'}}
    $original=Get-Content Tests/CompetentPlanReview.cs -Raw
    $old='int anchor = siteValue(first.Socket) >= siteValue(second.Socket) ? first.Socket : second.Socket;'
    $new='int anchor = siteValue(first.Socket) >= siteValue(second.Socket) ? first.Socket : second.Socket; if(stage==6 && recipe==1)anchor=anchor==first.Socket?second.Socket:first.Socket;'
    if($original.Split($old).Count -ne 2){throw 'Anchor replacement ambiguous'}
    $candidate=$original.Replace('class CompetentPlanReview','class AlternateAnchorPlanReview').Replace($old,$new)
    if($candidate.Replace('class AlternateAnchorPlanReview','class CompetentPlanReview').Replace($new,$old) -cne $original){throw 'Candidate changed other planner rules'}
    [IO.File]::WriteAllText((Join-Path (Get-Location) "$root/AlternateAnchorPlanReview.cs"),$candidate)
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+@('Tests/CompetentPlanReview.cs','Tests/AccountGrowthChecks.cs',"$root/AlternateAnchorPlanReview.cs"))
    foreach($stage in [Gatefall.Core.CampaignCatalog]::Stages){[Gatefall.Core.BattlefieldLayouts]::Get($stage)|Out-Null}
    function Play([int]$seed,[string]$profile,[bool]$alternate){
        $p=[Gatefall.Tests.AccountGrowthChecks]::Recommended(6,0,($(if($profile -eq 'below'){1}else{0})))
        $rules=[Gatefall.Core.CampaignService]::new($p,$null).Battle(6,$false)
        $plan=if($alternate){[Gatefall.Tests.AlternateAnchorPlanReview+Plan]::new()}else{[Gatefall.Tests.CompetentPlanReview+Plan]::new()}
        $plan.Spread=.5;$plan.Cap=1;$plan.Rupture=.4;$plan.Upgrade=2;$plan.Fuse=$true
        $g=if($alternate){[Gatefall.Tests.AlternateAnchorPlanReview]::Play(6,$seed,$plan,$false,720,$rules,$null,$null)}else{[Gatefall.Tests.CompetentPlanReview]::Play(6,$seed,$plan,$false,720,$rules,$null,$null)}
        return [pscustomobject][ordered]@{stage=7;seed=$seed;profile=$profile;budget=720;policy='area-fusion';state=$g.State.ToString();core=$g.MinimumCoreHealth;end=$g.Elapsed;spent=$g.Telemetry.CreditsSpent;credits=$g.Credits;growth=$rules.EarnedGrowth;research=$rules.Research;cards=$g.Cards;towers=$g.Towers.Count;fusions=$g.Telemetry.Fusions;spawned=$g.Telemetry.Spawned;remaining=$g.Enemies.Count;allSpawned=$g.AllWavesSpawned;stars=[Gatefall.Core.CampaignCatalog]::Stars($g.State -eq [Gatefall.Core.RunState]::Won,$g.MinimumCoreHealth,$g.Balance.CoreHealth);laneDamage=$g.Telemetry.CoreDamageByLane;ledger=$g.Telemetry.Purchases.ToArray()}
    }
    $control=Play 7 below $false
    $oldControl=@($baseline.cases|Where-Object{$_.stage -eq 7 -and $_.seed -eq 7 -and $_.profile -ceq 'below' -and $_.budget -eq 720 -and $_.policy -ceq 'area-fusion'})[0]
    foreach($property in $oldControl.PSObject.Properties.Name){if(($oldControl.$property|ConvertTo-Json -Depth 6 -Compress) -cne ($control.$property|ConvertTo-Json -Depth 6 -Compress)){throw ('Selected original control differs:'+ $property)}}
    $cases=[Collections.Generic.List[object]]::new()
    foreach($seed in @(7,41)){foreach($profile in @('below','recommended')){
        $row=Play $seed $profile $true;$cases.Add($row)
        [ordered]@{originalControl=$control;controlEquivalent=$true;plannerInverseExact=$true;cases=$cases}|ConvertTo-Json -Depth 9|Set-Content $output -Encoding utf8
        $prior=@($baseline.cases|Where-Object{$_.stage -eq 7 -and $_.seed -eq $seed -and $_.profile -ceq $profile -and $_.budget -eq 720 -and $_.policy -ceq 'area-fusion'})[0]
        if(($row.ledger[0..13] -join '|') -cne ($prior.ledger[0..13] -join '|') -or ($row.cards -join ',') -cne ($prior.cards -join ',')){throw 'Prefusion purchases/cards diverged'}
        if($row.spent -gt 720 -or $row.state -ne 'Won' -or -not $row.allSpawned -or $row.remaining -ne 0){throw 'Unexpected candidate outcome; preserved raw before guard'}
    }}
}
$report=Get-Content $output -Raw|ConvertFrom-Json
$report.cases|Select-Object seed,profile,core,end,spent,towers,fusions,laneDamage|Format-Table -AutoSize
'Candidate planner only: one alternate anchor rule. Subsequent vacancy refill/targeting can change. No product or Android change.'
