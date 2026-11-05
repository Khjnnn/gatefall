param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
$output="$root/transition-anchor-isolation-v0286.json"
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Recorded isolated pairs exist; use -UseRecorded'}
    $prior=Get-Content "$root/normal-transition-v0286.json" -Raw|ConvertFrom-Json
    foreach($hash in $prior.source){$path=if($hash.file -in @('AccountGrowthChecks.cs','CompetentPlanReview.cs')){'Tests/'+$hash.file}else{'Assets/Scripts/Core/'+$hash.file};if((Get-FileHash $path).Hash.ToLowerInvariant() -cne $hash.sha256){throw 'Recorded planner/core changed'}}
    $original=Get-Content Tests/CompetentPlanReview.cs -Raw
    $candidate=Get-Content "$root/AlternateAnchorPlanReview.cs" -Raw
    $old='int anchor = siteValue(first.Socket) >= siteValue(second.Socket) ? first.Socket : second.Socket;'
    $new=$old+' if(stage==6 && recipe==1)anchor=anchor==first.Socket?second.Socket:first.Socket;'
    if($candidate.Replace('class AlternateAnchorPlanReview','class CompetentPlanReview').Replace($new,$old) -cne $original){throw 'Candidate is not the same single anchor change'}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+@('Tests/CompetentPlanReview.cs','Tests/AccountGrowthChecks.cs',"$root/AlternateAnchorPlanReview.cs"))
    foreach($stage in [Gatefall.Core.CampaignCatalog]::Stages){[Gatefall.Core.BattlefieldLayouts]::Get($stage)|Out-Null}
    $cases=[Collections.Generic.List[object]]::new()
    foreach($seed in @(7,41)){foreach($profile in @('below','recommended')){foreach($alternate in @($false,$true)){
        $account=[Gatefall.Tests.AccountGrowthChecks]::Recommended(6,0,($(if($profile -eq 'below'){1}else{0})))
        $rules=[Gatefall.Core.CampaignService]::new($account,$null).Battle(6,$false)
        $plan=if($alternate){[Gatefall.Tests.AlternateAnchorPlanReview+Plan]::new()}else{[Gatefall.Tests.CompetentPlanReview+Plan]::new()}
        $plan.Spread=.5;$plan.Cap=1;$plan.Rupture=.4;$plan.Upgrade=2;$plan.Fuse=$true
        $g=if($alternate){[Gatefall.Tests.AlternateAnchorPlanReview]::Play(6,$seed,$plan,$false,645,$rules,$null,$null)}else{[Gatefall.Tests.CompetentPlanReview]::Play(6,$seed,$plan,$false,645,$rules,$null,$null)}
        $row=[pscustomobject][ordered]@{seed=$seed;profile=$profile;alternate=$alternate;budget=645;spent=$g.Telemetry.CreditsSpent;state=$g.State.ToString();core=$g.MinimumCoreHealth;end=$g.Elapsed;towers=$g.Towers.Count;fusions=$g.Telemetry.Fusions;cards=$g.Cards;laneDamage=$g.Telemetry.CoreDamageByLane;ledger=$g.Telemetry.Purchases.ToArray();allSpawned=$g.AllWavesSpawned;remaining=$g.Enemies.Count}
        $cases.Add($row)
        [ordered]@{coreAndPlannerHashes=$prior.source;cases=$cases;scope='Normal1-7,645 exact spend,no purchases after first fusion. Paired anchor-only change; no product/Android tuning.'}|ConvertTo-Json -Depth 9|Set-Content $output -Encoding utf8
        if($row.spent -ne 645 -or $row.towers -ne 7 -or $row.fusions -ne 1 -or $row.ledger.Count -ne 15){throw 'Isolation includes another purchase; raw preserved'}
        if($alternate){$control=$cases[$cases.Count-2];if(($control.ledger[0..13] -join '|') -cne ($row.ledger[0..13] -join '|') -or ($control.cards -join ',') -cne ($row.cards -join ',')){throw 'Paired prefix/card differs'}}
    }}}
}
$report=Get-Content $output -Raw|ConvertFrom-Json
$report.cases|Select-Object seed,profile,alternate,spent,core,end,state,laneDamage|Format-Table -AutoSize
'Eight new selected models at645, not repeats of the earlier720 pilot. No refill decisions after fusion.'
