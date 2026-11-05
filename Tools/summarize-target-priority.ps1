$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$candidateReport=Get-Content (Join-Path $root 'target-priority-candidate-v0284.json') -Raw|ConvertFrom-Json
$static=Get-Content (Join-Path $root 'target-priority-static-v0284.json') -Raw|ConvertFrom-Json
foreach($report in @($static,$candidateReport)){foreach($hash in $report.source){if((Get-FileHash (Join-Path 'Assets/Scripts/Core' $hash.file)).Hash.ToLowerInvariant() -cne $hash.sha256){throw 'Recorded target-priority source changed'}}}
$before='private static int Lead(Enemy a,Enemy b) { int d=a.Position.Length.CompareTo(b.Position.Length); return d!=0 ? d : a.Id.CompareTo(b.Id); }'
$after='private int Lead(Enemy a,Enemy b) { float ar=Rules?.Stage==null ? a.Position.Length : Gatefall.Tests.TargetPriorityReview.Remaining(this,a); float br=Rules?.Stage==null ? b.Position.Length : Gatefall.Tests.TargetPriorityReview.Remaining(this,b); int d=ar.CompareTo(br); return d!=0 ? d : a.Id.CompareTo(b.Id); }'
foreach($hash in $candidateReport.source){
    $original=Get-Content (Join-Path 'Assets/Scripts/Core' $hash.file) -Raw
    $copied=Get-Content (Join-Path $root ('target-priority-candidate-core/'+$hash.file)) -Raw
    if($hash.file -eq 'DefenseSimulation.cs'){$copied=$copied.Replace($after,$before)}
    if($copied -cne $original){throw 'Candidate changes exceed Lead replacement'}
}
$middle=(Get-Content TestResults/night-crisis-middle-budgets.json -Raw|ConvertFrom-Json).rows
$late=(Get-Content (Join-Path $root 'crisis-curve-v0283.json') -Raw|ConvertFrom-Json).cases.Outcome
$pairs=@(foreach($c in $candidateReport.cases){
    $o=$c.Outcome;$pool=if($o.Stage -eq 12){$middle}else{$late}
    $old=@($pool|Where-Object{$_.Stage -eq $o.Stage -and $_.Seed -eq $o.Seed -and $_.Policy -ceq $o.Policy -and $_.Profile -eq 'below' -and $_.Budget -eq 720})
    if($old.Count -ne 1){throw 'Missing unique compatible previous condition'}
    $b=$old[0]
    foreach($field in @('Crisis','Stage','Seed','Profile','Budget','Spent','Towers','Fusions','Growth','Research','AccountSpent','Cards')){if(($o.$field|ConvertTo-Json -Compress) -cne ($b.$field|ConvertTo-Json -Compress)){throw ('Unmatched comparison '+$field)}}
    if(-not $c.AllSpawned -or $c.Remaining -ne 0 -or $c.Spawned -ne $(if($o.Stage -eq 12){880}else{944}) -or $o.State -ne 'Won' -or $o.Core -le 0 -or $o.Credits -lt 0){throw 'Candidate completion invalid'}
    if([math]::Abs(($c.LaneDamage|Measure-Object -Sum).Sum-(300-$o.Core)) -gt .01){throw 'Candidate damage does not reconcile'}
    [ordered]@{stage=$o.Stage;seed=$o.Seed;policy=$c.PolicyIndex;spent=$o.Spent;beforeCore=$b.Core;afterCore=$o.Core;beforeEnd=$b.End;afterEnd=$o.End;beforeStars=$b.Stars;afterStars=$o.Stars;beforeFired=$b.FiredCurrent;afterFired=$o.FiredCurrent;beforeRecent3=$b.PeakRecent3;afterRecent3=$o.PeakRecent3;
        sameOrdersIgnoringTimes=(($b.Ledger -replace '^\d+(\.\d+)? ','')|ConvertTo-Json -Compress) -ceq (($o.Ledger -replace '^\d+(\.\d+)? ','')|ConvertTo-Json -Compress);sameLedgerIncludingTimes=($b.Ledger|ConvertTo-Json -Compress) -ceq ($o.Ledger|ConvertTo-Json -Compress)}
})
if($pairs.Count -ne 8 -or ($static.cases.Sockets|Measure-Object -Sum).Sum -ne 308){throw 'Unexpected selected/static count'}
[ordered]@{newCandidateCases=8;reusedBaselineCases=8;staticBoards=14;staticSockets=308;staticPossibleDifferenceSockets=($static.cases.DifferingSockets|Measure-Object -Sum).Sum;candidateOnlyLeadChanged=$true;pairs=$pairs;adopted=$false;
    coreImproved=@($pairs|Where-Object{$_.afterCore -gt $_.beforeCore}).Count;coreWorse=@($pairs|Where-Object{$_.afterCore -lt $_.beforeCore}).Count;starChanges=@($pairs|Where-Object{$_.afterStars -ne $_.beforeStars}).Count;
    decision='Do not adopt a global rule on this pilot. Two stage12 fusion cases retain6.8/10.4 more core, but stage14 spear seed41 loses13 core. Five core outcomes and all eight star ratings are unchanged; all8win and both cards/actual spend match. Purchase times differ. Static pairs are possible geometry conflicts, not actual query frequency. No new product/APK/AVD or native run. Speed, health, roads, money and wave data are unchanged.';
    baselineCompatibility='Prior middle/late Kang outcomes are reused through0.28.4 exact resonance-stat and selected simulation equivalence. This is eight new candidate conditions, not a new run of eight baselines. Candidate is a test-only direct polyline function with extra comparison work; no Android performance claim.'
}|ConvertTo-Json -Depth 10|Set-Content (Join-Path $root 'target-priority-comparison-v0284.json') -Encoding utf8
$pairs|ForEach-Object{[pscustomobject]$_}|Format-Table stage,seed,policy,beforeCore,afterCore,beforeStars,afterStars,sameOrdersIgnoringTimes -AutoSize
Write-Output 'PASS target-priority recorded comparison:8 candidate/8 reused baseline,308 static sockets; candidate not adopted. No simulation executed.'
