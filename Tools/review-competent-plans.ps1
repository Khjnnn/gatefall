# Run in a fresh PowerShell process: pwsh -NoProfile -File .\Tools\review-competent-plans.ps1 [-FromStage 7] [-Enforce]
# Searches the planner grid of Tests/CompetentPlanReview.cs on every operation from FromStage (multi-core, a few minutes).
# Report-only by default. -Enforce fails when an operation has no plan that earns three stars on every seed.
param([int]$FromStage=7,[switch]$Enforce)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName+(Join-Path $taskProject 'Tests/CompetentPlanReview.cs'))
$taskReview=[Gatefall.Tests.CompetentPlanReview]
$taskRows=$taskReview::Run($FromStage)
$taskViolations=@($taskReview::Violations($taskRows,$FromStage))
$taskGate="GATE every operation from 1-$FromStage has a three-star plan on every seed: $($taskViolations.Count) violations"+$(if($Enforce){' (enforced)'}else{' (report-only)'})
$taskText=@($taskReview::Summary($taskRows),$taskGate)+$taskViolations
$taskText+='Scope: scripted planner with hindsight over '+$taskReview::Grid().Count+' plans x seeds '+($taskReview::Seeds -join '/')+'; idle hunter, no growth, no wave calls. Not a human difficulty rating.'
$taskResultsDir=Join-Path $taskProject 'TestResults'
New-Item -ItemType Directory -Path $taskResultsDir -Force|Out-Null
[ordered]@{method='Competent plan review: lane-threat placement, role mix, upgrades and fusions searched over a plan grid.';gate=[ordered]@{enforced=[bool]$Enforce;fromStage=$FromStage;violations=$taskViolations};rows=$taskRows}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $taskResultsDir 'competent-plan-review.json') -Encoding utf8
$taskText|Set-Content -LiteralPath (Join-Path $taskResultsDir 'competent-plan-review.txt') -Encoding utf8
$taskText|Write-Output
if($Enforce -and $taskViolations.Count -gt 0){throw "Competent plan gate failed: $($taskViolations.Count) operations without a three-star plan"}
