$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$inputPath=Join-Path $project 'TestResults/night-crisis-middle-budgets.json'
$report=Get-Content -LiteralPath $inputPath -Raw | ConvertFrom-Json
$rows=@($report.rows)
if ($rows.Count -ne 96 -or @($rows | Where-Object { -not $_.Crisis -or $_.Stage -notin @(11,12) -or $_.Budget -notin @(720,900) -or $_.Profile -notin @('below','recommended') }).Count) { throw 'Unexpected middle-investment fixture' }
if (@($rows | Group-Object Stage,Seed,Profile,Budget,Policy | Where-Object Count -ne 1).Count) { throw 'Duplicate investment fixture' }
if (@($rows | Where-Object { $_.Spent -gt $_.Budget -or $_.Spent -lt 0 -or $_.Credits -lt 0 }).Count) { throw 'Spending/remaining-credit invariant failed' }
$groups=@($rows | Group-Object Stage,Profile,Budget,Policy | ForEach-Object {
    $g=@($_.Group)
    if ($g.Count -ne 3 -or (($g.Seed|Sort-Object)-join ',') -ne '7,19,41') { throw 'Missing policy seeds' }
    [ordered]@{
        stage=$g[0].Stage;profile=$g[0].Profile;budget=$g[0].Budget;policy=$g[0].Policy;
        wins=@($g|Where-Object State -eq Won).Count;threeStars=@($g|Where-Object Stars -eq 3).Count;
        minimumCore=($g.Core|Measure-Object -Minimum).Minimum;
        actualSpend=@($g.Spent|Sort-Object -Unique);remainingCredits=@($g.Credits|Sort-Object -Unique);
        towers=@($g.Towers|Sort-Object -Unique);fusions=@($g.Fusions|Sort-Object -Unique);
        cardSets=@($g|ForEach-Object{$_.Cards-join ','}|Sort-Object -Unique);
        lastPurchases=@($g|ForEach-Object{[ordered]@{seed=$_.Seed;lastPurchase=$_.Ledger[-1];end=$_.End;state=$_.State}})
    }
})
$output=Join-Path $project 'TestResults/night-balance-20261105/middle-investment-policies.json'
[ordered]@{
    source=$inputPath;sourceSha256=(Get-FileHash -LiteralPath $inputPath).Hash.ToLowerInvariant();
    reusedConditions=$rows.Count;newSimulationConditions=0;groups=$groups;
    scope='Recorded crisis 1-11/12 only. Same spending ceiling and scripted purchasing rules do not mean equal actual spending, positions, card choices or purchase times across policies/profiles. Credits are retained, not removed to force difficulty. No product change or new Android run.'
} | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath $output -Encoding utf8
$groups|ForEach-Object{[pscustomobject]$_}|Select-Object stage,profile,budget,policy,wins,threeStars,minimumCore|Format-Table -AutoSize
Write-Output ('Recorded policy summary: '+$output)
