param(
    [int[]]$Stages = @(7,8,9,10,11,12,13,14),
    [int[]]$Limits = @(0),
    [string[]]$Profiles = @('zero','below','recommended'),
    [int[]]$Budgets = $null,
    [switch]$Crisis,
    [string]$CoreDirectory = '',
    [ValidatePattern('^[a-zA-Z0-9_-]+$')][string]$Tag = 'progression-balance',
    [switch]$UseRecorded
)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Path (Join-Path $project 'TestResults') -Force | Out-Null
$output = Join-Path $project ('TestResults/' + $Tag + '.json')
if (-not $UseRecorded) {
    if (-not $CoreDirectory) { $CoreDirectory=Join-Path $project 'Assets/Scripts/Core' }
    $CoreDirectory=(Resolve-Path -LiteralPath $CoreDirectory).Path
    if (@($Stages | Where-Object { $_ -lt 7 -or $_ -gt 14 }).Count) { throw 'Stages must be 7 through 14' }
    if (@($Profiles | Where-Object { $_ -notin @('zero','below','recommended') }).Count) { throw 'Unknown account profile' }
    if ($null -ne $Budgets -and @($Budgets | Where-Object { $_ -lt 0 }).Count) { throw 'Spending budgets must not be negative' }
    Add-Type -Path @((Get-ChildItem $CoreDirectory -Filter '*.cs').FullName + @(
        (Join-Path $project 'Tests/CompetentPlanReview.cs'),
        (Join-Path $project 'Tests/AccountGrowthChecks.cs'),
        (Join-Path $project 'Tests/ProgressionBalanceReview.cs')))
    $rows = [Gatefall.Tests.ProgressionBalanceReview]::Run([int[]]@($Stages | ForEach-Object { $_ - 1 }), $Limits, $Profiles, $Budgets, 0, [bool]$Crisis)
    $hashes = @(Get-ChildItem $CoreDirectory -Filter '*.cs' | ForEach-Object {
        [ordered]@{ file=$_.Name; sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant() }
    })
    [ordered]@{ coreDirectory=$CoreDirectory; source=$hashes; stages=$Stages; limits=$Limits; profiles=$Profiles; budgets=$Budgets; crisis=[bool]$Crisis;
        scope='Scripted idle-hunter plans, no active skills or wave calls; first-card policy. Common spending ceilings, not fixed purchase times or equal actual expenditure. FiredLifetime includes consumed materials; FiredCurrent includes surviving towers only. Recent participation is sampled at 60..170s (losses can be shorter). Not human play or a fun rating.';
        rows=$rows } | ConvertTo-Json -Depth 7 | Set-Content -LiteralPath $output -Encoding utf8
}
$report = Get-Content -LiteralPath $output -Raw | ConvertFrom-Json
$report.rows | Group-Object Stage,Limit,Profile,Crisis,Budget | ForEach-Object {
    $g=$_.Group
    [pscustomobject]@{ Case=$_.Name; Runs=$g.Count; Wins=@($g|Where-Object State -eq Won).Count;
        ThreeStars=@($g|Where-Object Stars -eq 3).Count; MinimumCore=($g.Core|Measure-Object -Minimum).Minimum;
        AverageTowers=[math]::Round(($g.Towers|Measure-Object -Average).Average,2);
        AverageFired=[math]::Round(($g.FiredCurrent|Measure-Object -Average).Average,2);
        RecentPeak=($g.PeakRecent3|Measure-Object -Maximum).Maximum }
} | Format-Table -AutoSize
Write-Output ('Recorded models: '+@($report.rows).Count+'; '+$output)
