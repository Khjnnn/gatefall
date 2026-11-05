# Run in a fresh PowerShell process: pwsh -NoProfile -File .\Tools\review-degenerate-strategies.ps1 [-Enforce] [-FromStage 8] [-MaxStars 2]
# Report-only by default. -Enforce (used by build-delivery.ps1) fails when a gated bot earns more than MaxStars
# from operation FromStage onwards. 1-7 introduces the post-seal tower limit and is not gated.
param([switch]$Enforce,[int]$FromStage=8,[int]$MaxStars=2)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName+(Join-Path $taskProject 'Tests/DegenerateStrategyReview.cs'))
$taskReview=[Gatefall.Tests.DegenerateStrategyReview]
$taskRows=$taskReview::Run()
$taskViolations=@($taskReview::Violations($taskRows,$FromStage,$MaxStars))
$taskGate="GATE gated bots at most $MaxStars stars from 1-$($FromStage): $($taskViolations.Count) violations"+$(if($Enforce){' (enforced)'}else{' (report-only)'})
$taskText=@($taskReview::Summary($taskRows),$taskGate)+@($taskViolations|Select-Object -First 12)
if($taskViolations.Count -gt 12){$taskText+="... $($taskViolations.Count-12) more in degenerate-strategy-review.json"}
$taskText+='Scope: scripted bots on the actual combat core, seeds '+($taskReview::Seeds -join '/')+'; idle hunter, no growth, no wave calls. A lower bound, not a human difficulty rating.'
$taskResultsDir=Join-Path $taskProject 'TestResults'
New-Item -ItemType Directory -Path $taskResultsDir -Force|Out-Null
[ordered]@{method='Degenerate strategy review: bots skip lane choice, upgrades, fusions, sales, wave calls, hunter movement and account growth.';gate=[ordered]@{enforced=[bool]$Enforce;fromStage=$FromStage;maxStars=$MaxStars;violations=$taskViolations};policies=$taskReview::Policies;rows=$taskRows}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $taskResultsDir 'degenerate-strategy-review.json') -Encoding utf8
$taskText|Set-Content -LiteralPath (Join-Path $taskResultsDir 'degenerate-strategy-review.txt') -Encoding utf8
$taskText|Write-Output
if($Enforce -and $taskViolations.Count -gt 0){throw "Degenerate strategy gate failed: $($taskViolations.Count) runs exceed $MaxStars stars from 1-$FromStage"}
