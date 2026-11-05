$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/FusionPositionReview.cs'))
$taskRows=[Gatefall.Tests.FusionPositionReview]::Models()
if($taskRows.Count -ne 6 -or @($taskRows|Where-Object CompletedPurchases -ne 21).Count){throw 'Incomplete position comparison'}
[pscustomobject]@{created=(Get-Date).ToString('o');scope='Same stage7 552 enemies, purchase times/order/kinds and card preference; only slots0/11 swap. Scripted counterfactual, not Android replay or human fun.';rows=$taskRows}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $taskProject 'TestResults/fusion-position-v0182-models.json')
$taskRows|Format-Table Seed,Anchor,Spent,Towers,ArcShots,BindingShots,State,Core,Time,CoveredWest,CoveredEast,CoveredSouth
