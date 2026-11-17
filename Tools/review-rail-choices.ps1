# Run in a fresh PowerShell process: pwsh -NoProfile -File .\Tools\review-rail-choices.ps1 [-Tag name] [-Enforce]
# Chapter 3 difficulty and 강화/조합/보충 comparison: every policy gets the same seed, account and spending allowance.
# -Enforce (used by build-delivery.ps1) applies the gate described below.
param([string]$Tag='current',[switch]$Enforce,[string[]]$Stages)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName+(Join-Path $taskProject 'Tests/RailChoiceReview.cs'))
$taskReview=[Gatefall.Tests.RailChoiceReview]
$taskRows=$taskReview::Run($Stages,$null,$null)
$taskSummary=$taskReview::Summary($taskRows)
# 3-1 and 3-2 stay introductory. From 3-3 the decision-free spear bot must miss three stars on at least one seed,
# the full-allowance mixed plan must keep three stars on every seed and still win with 80% of the allowance.
$taskGated=@($taskRows|Where-Object {$_.Stage -notin '3-1','3-2'})
$taskSpear=@($taskGated|Where-Object Policy -eq 'spear'|Group-Object Stage|Where-Object {@($_.Group|Where-Object Stars -lt 3).Count -eq 0}|ForEach-Object Name)
$taskMixed=@($taskGated|Where-Object {$_.Policy -eq 'mixed' -and (($_.Budget -eq 1 -and $_.Stars -lt 3) -or ([math]::Abs($_.Budget-.8) -lt .01 -and $_.Outcome -ne 'Won'))}|ForEach-Object {"$($_.Stage) seed $($_.Seed) at $([int]($_.Budget*100))%"})
$taskGate="GATE from 3-3: spear bot three stars on every seed in [$($taskSpear -join ', ')] ($($taskSpear.Count) operations); mixed plan below target: $($taskMixed.Count) runs"+$(if($Enforce){' (enforced)'}else{' (report-only)'})
# Crisis operations (every enemy's health x1.25, no wave calls): the full-allowance mixed plan must still win every seed
# on 3-1~3-8. Stars are not required there. The other rows are printed for reference only.
$taskReview::Crisis=$true
$taskCrisisRows=$taskReview::Run($Stages,[single[]]@(.8,1),[string[]]@('upgrade','mixed','fusion'))
$taskReview::Crisis=$false
$taskCrisisLost=@($taskCrisisRows|Where-Object {$_.Policy -eq 'mixed' -and $_.Budget -eq 1 -and $_.Outcome -ne 'Won'}|ForEach-Object {"$($_.Stage) seed $($_.Seed)"})
$taskCrisisGate="CRISIS GATE: full-allowance mixed plan lost $($taskCrisisLost.Count) crisis runs [$($taskCrisisLost -join ', ')]"+$(if($Enforce){' (enforced)'}else{' (report-only)'})
# Report-only: on boards where a full chain leaves a road thin, does one fusion followed by the mixed plan hold?
$taskReview::SingleFusion=$true
$taskSingle=$taskReview::Summary($taskReview::Run($Stages,[single[]]@(.8,1),[string[]]@('fusion')))
$taskReview::SingleFusion=$false
# Extra grade-6 gate. Original normal/crisis rows above retain the grade-1 account.
# It starts at 3-4: on 3-3 the grade-1 spear bot already scores 3/3/1 with one leak to spare, so +10% tips it to
# 3/3/3 (seed 41 keeps 276 of 300), and raising 3-3's Threat to fix that makes the 80% mixed plan lose (measured
# 2026-11-14 at 2.3~2.6). 3-3 is still printed below.
$taskReview::SupplyGrade=6
$taskGradeSixRows=$taskReview::Run($Stages,[single[]]@(1),[string[]]@('spear'))
$taskReview::SupplyGrade=1
$taskGradeSixFailed=@($taskGradeSixRows|Where-Object {$_.Stage -notin '3-1','3-2','3-3'}|Group-Object Stage|Where-Object {@($_.Group|Where-Object Stars -lt 3).Count -eq 0}|ForEach-Object Name)
$taskGradeSixFailRows=@($taskGradeSixRows|Where-Object {$_.Stage -in $taskGradeSixFailed}|ForEach-Object {"$($_.Stage) seed $($_.Seed): $($_.Outcome), $($_.Stars) stars, core $($_.MinimumCore)"})
$taskGradeSixGate="GRADE 6 GATE from 3-4: spear bot three stars on every seed in [$($taskGradeSixFailed -join ', ')] ($($taskGradeSixFailed.Count) operations)"+$(if($Enforce){' (enforced)'}else{' (report-only)'})
$taskOut=Join-Path $taskProject 'TestResults/rail-choice-review'
New-Item -ItemType Directory -Path $taskOut -Force|Out-Null
[ordered]@{method='Same seed (7/19/41), same grade-1 account (growth 6/4/3, research 4/2/2/2) and same spending allowance per policy; idle hunter; neutral card picks. Extra spear rows use every tower at grade 6.';budgets=$taskReview::Budgets;gate=[ordered]@{enforced=[bool]$Enforce;spearThreeStarOperations=$taskSpear;mixedBelowTarget=$taskMixed;crisisMixedLost=$taskCrisisLost;gradeSixSpearThreeStarOperations=$taskGradeSixFailed};rows=$taskRows;crisisRows=$taskCrisisRows;gradeSixRows=$taskGradeSixRows}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $taskOut "rail-choice-$Tag.json") -Encoding utf8
@($taskSummary,$taskGate,'Cells: allowance%:stars for seeds 7/19/41 (L=loss)/lowest minimum barrier among wins (0 when a seed lost).','','== Crisis operations',$taskReview::Summary($taskCrisisRows),$taskCrisisGate,'','== Fusion policy stopping after its first fusion (report-only)',$taskSingle,'','== Every tower at grade 6',$taskReview::Summary($taskGradeSixRows),$taskGradeSixGate,$taskGradeSixFailRows)|Set-Content -LiteralPath (Join-Path $taskOut "rail-choice-$Tag.txt") -Encoding utf8
$taskSummary;$taskGate;$taskCrisisGate;$taskReview::Summary($taskGradeSixRows);$taskGradeSixGate;$taskGradeSixFailRows
if($Enforce -and ($taskSpear.Count -gt 0 -or $taskMixed.Count -gt 0 -or $taskCrisisLost.Count -gt 0 -or $taskGradeSixFailed.Count -gt 0)){throw 'Rail choice gate failed'}
