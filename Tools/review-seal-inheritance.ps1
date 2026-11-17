param([switch]$Expanded,[string]$Tag='current-v2',[switch]$Enforce)
$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
$core = (Get-ChildItem -LiteralPath (Join-Path $project 'Assets/Scripts/Core') -Filter '*.cs').FullName
$checks = @('Tests/RailChoiceReview.cs','Tests/SealInheritanceReview.cs','Tests/SealInheritanceChecks.cs') | ForEach-Object { Join-Path $project $_ }
Add-Type -CompilerOptions '/langversion:7.3' -Path @($core + $checks)
if($Enforce -and !$Expanded){throw 'Acceptance requires -Expanded'}

$results = Join-Path $project $(if ($Expanded) { 'TestResults/seal-inheritance-expanded' } else { 'TestResults/seal-inheritance' })
New-Item -ItemType Directory -Force -Path $results | Out-Null
$volleys = [Gatefall.Tests.SealInheritanceReview]::RunVolleys([bool]$Expanded)
$volleys | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $results 'volleys.json') -Encoding utf8
$volleys | Where-Object Seed -eq 7 | Format-Table -AutoSize | Out-String | Write-Output

$rows = [Gatefall.Tests.SealInheritanceReview]::Run([bool]$Expanded)
$rows | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $results 'campaign.json') -Encoding utf8
$summary=@($rows | Group-Object Variant,Route,Policy,RefillFailed | ForEach-Object {
    $set=@($_.Group)
    '{0}: games {1}, wins {2}, three stars {3}, no damage {4}' -f $_.Name,$set.Count,@($set | Where-Object Outcome -eq Won).Count,@($set | Where-Object Stars -eq 3).Count,@($set | Where-Object MinimumCore -eq 300).Count
})
$details=@($rows | Group-Object Variant,Stage,PrimaryLane,Budget,Route,Policy | ForEach-Object {
    $set = @($_.Group | Sort-Object Seed)
    $stars = -join @($set | ForEach-Object { if ($_.Outcome -eq 'Won') { $_.Stars } else { 'L' } })
    $barriers = ($set.MinimumCore | ForEach-Object { [math]::Round($_) }) -join '/'
    $spent = $set.Spent -join '/'
    $shots = $set.SealShots -join '/'
    "{0}, {1}, lane {2}, {3}, {4}, {5}: stars {6}, core {7}, spent {8}, seal volleys {9}, refill failed {10}" -f $set[0].Variant,$set[0].Stage,$set[0].PrimaryLane,$set[0].Budget,$set[0].Route,$set[0].Policy,$stars,$barriers,$spent,$shots,($set.RefillFailed -join '/')
})
$stats=@(8,10,12 | ForEach-Object { $recipe=[Gatefall.Core.TowerCatalog]::Recipes[$_]; [ordered]@{recipe=$recipe.Id;inheritance=$recipe.Inheritance.ToString();stats=([Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,$null)).Stats($recipe)} })
[ordered]@{method='v2: seeds 7/19/41/101/202; budgets 650/750/925; 3-7 removed (pre-branch leaks); ceiling and reserved-40 equal-tower-count variants; identical opening asserted';stats=$stats;campaign=$rows;volleys=$volleys;summary=$summary} | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath (Join-Path $results "$Tag.json") -Encoding utf8
@($summary,'',$details) | Set-Content -LiteralPath (Join-Path $results "$Tag.txt") -Encoding utf8
$summary
if($Enforce){
    $acceptance=[Gatefall.Tests.SealInheritanceChecks]::Run($rows,$volleys)
    $acceptance | Add-Content -LiteralPath (Join-Path $results "$Tag.txt") -Encoding utf8
    $acceptance
}
