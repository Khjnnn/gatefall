$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/MapLayoutReview.cs'))
$taskGeometry = [Gatefall.Tests.MapLayoutReview]::Geometry()
$taskGeometry | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/map-geometry-v0120.txt') -Encoding utf8
$taskModels = [Gatefall.Tests.MapLayoutReview]::Models()
if ($taskModels.Count -ne 252) { throw 'Expected 14 stages x 6 policies x 3 seeds' }
if (@($taskModels | Where-Object { $_.State -notin @('Won','Lost') }).Count) { throw 'Unfinished campaign model' }
foreach ($taskStage in 1..14) {
    if (-not @($taskModels | Where-Object { $_.Stage -eq $taskStage -and $_.State -eq 'Won' }).Count) { throw "No winning model for stage $taskStage" }
}
foreach ($taskRow in ($taskModels | Where-Object { $_.Stage -le 3 -and $_.Policy -in @('spread','upgrade','fusion-cover') })) {
    if ($taskRow.State -ne 'Won' -or $taskRow.Core -lt 299.9) { throw 'Early active reinforcement model failed' }
}
$taskModels | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/map-models-v0120.json') -Encoding utf8
$taskModels | Group-Object Policy | ForEach-Object {
    [ordered]@{ policy=$_.Name; wins=@($_.Group | Where-Object State -eq Won).Count; losses=@($_.Group | Where-Object State -eq Lost).Count; unfinished=@($_.Group | Where-Object State -eq Running).Count }
} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/map-model-summary-v0120.json') -Encoding utf8
Write-Output $taskGeometry
Write-Output 'PASS: 252 normal campaign model conditions; at least one winning policy per stage; first 3 active-reinforcement policies preserve the barrier. Cards on / first offered choice / no early wave calls / hunter0 / no research-growth; automatic skill and boss avoidance. Opening-fixed begins with three Spears and spends only the starting budget. Fusion-rush concentrates its two initial materials; fusion-cover defends initial lanes then fuses and rebuilds. Neither is an optimized human strategy. Failures are retained in the report. Not Android interaction or human fun testing.'
