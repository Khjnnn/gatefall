param([string]$CoreDirectory='', [string]$Tag='v0140', [ValidateRange(1,14)][int]$StageCount=14)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
if (-not $CoreDirectory) { $CoreDirectory=Join-Path $taskProject 'Assets/Scripts/Core' }
Add-Type -Path @((Get-ChildItem -LiteralPath $CoreDirectory -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/CombatPaceReview.cs'))
$taskTimer=[Diagnostics.Stopwatch]::StartNew()
$taskModels=[Gatefall.Tests.CombatPaceReview]::Models($StageCount)
if ($taskModels.Count -ne $StageCount*12 -or @($taskModels | Where-Object State -NotIn @('Won','Lost')).Count) {throw 'Incomplete model matrix'}
$taskReport=[ordered]@{ tag=$Tag; method="$StageCount stages x 4 policies x 3 seeds, first offered card, no wave calls, hunter0/no permanent growth; automated skill/boss avoidance, not human play"; models=$taskModels; duels=[Gatefall.Tests.CombatPaceReview]::Duels(); budgets=[Gatefall.Tests.CombatPaceReview]::Budgets(); geometry=[Gatefall.Tests.CombatPaceReview]::Geometry(); hostModelSeconds=$taskTimer.Elapsed.TotalSeconds }
$taskReport | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $taskProject "TestResults/combat-pace-$Tag.json") -Encoding utf8
$taskModels | Group-Object Policy | ForEach-Object {Write-Output ($_.Name+': wins='+@($_.Group | Where-Object State -eq Won).Count+' losses='+@($_.Group | Where-Object State -eq Lost).Count)}
Write-Output ('Saved combat-pace-'+$Tag+'.json; host model time is not Android performance.')
