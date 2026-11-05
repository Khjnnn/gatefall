$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName)
$taskDesign = Get-Content -LiteralPath (Join-Path $taskProject 'Docs/Design/combination-balance-v2.json') -Raw | ConvertFrom-Json
$taskFieldMap = @{ damage='Damage'; primary_damage='Damage'; secondary_damage='Secondary'; interval='Interval'; range='Range'; radius='Radius'; max_targets='MaxTargets'; slow_fraction='Slow'; slow_seconds='SlowSeconds'; chain_range='ChainRange' }
$taskCount = 0
for ($i = 0; $i -lt $taskDesign.towers.Count; $i++) {
    $taskDefinition = [Gatefall.Core.TowerCatalog]::All[$i]
    $taskInput = $taskDesign.towers[$i]
    if ($taskDefinition.Kind.ToString().ToLowerInvariant() -ne $taskInput.id) { throw "Tower identity mismatch $i" }
    for ($j=0; $j -lt $taskInput.levels.Count; $j++) {
        $taskLevel = $taskInput.levels[$j]
        $taskStats = $taskDefinition.Levels[$j]
        foreach ($taskProperty in $taskLevel.PSObject.Properties) {
            if ($taskFieldMap.ContainsKey($taskProperty.Name)) {
                $taskField = $taskFieldMap[$taskProperty.Name]
                if ([Math]::Abs([double]$taskProperty.Value - $taskStats.$taskField) -gt 0.0001) { throw "$($taskInput.id) level $j $taskField mismatch" }
                $taskCount++
            }
        }
        if ($taskLevel.chain_damage) {
            for ($k=0; $k -lt $taskLevel.chain_damage.Count; $k++) {
                if ([Math]::Abs($taskLevel.chain_damage[$k] - $taskStats.Chain[$k]) -gt 0.0001) { throw 'Chain mismatch' }
                $taskCount++
            }
        }
    }
    if ($taskInput.build_cost -and $taskInput.build_cost -ne $taskDefinition.BuildCost) { throw 'Build cost mismatch' }
    if ($taskInput.upgrade_costs.Count -gt 0 -and $taskInput.upgrade_costs[0] -ne $taskDefinition.UpgradeCost) { throw 'Upgrade cost mismatch' }
}
$taskSimulation = [Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7)
for ($i=0; $i -lt 6; $i++) {
    $taskRecipe = [Gatefall.Core.TowerCatalog]::Recipes[$i]
    $taskExpected = $taskDesign.recipes[$i]
    if ($taskRecipe.Credits -ne $taskExpected.credits -or $taskRecipe.First.ToString().ToLower() -ne $taskExpected.ingredients[0].tower -or $taskRecipe.Second.ToString().ToLower() -ne $taskExpected.ingredients[1].tower -or $taskRecipe.Result.ToString().ToLower() -ne $taskExpected.result.tower) { throw "Recipe $i differs" }
    foreach ($taskKind in @('grunt','runner','brute','siege')) {
        $taskSum = ($taskSimulation.Schedule | Where-Object { $_.Wave -eq $i -and $_.Kind.ToString().ToLower() -eq $taskKind } | Measure-Object -Property Count -Sum).Sum
        if ([int]$taskSum -ne $taskDesign.waves[$i].counts.$taskKind) { throw "Wave $i/$taskKind differs" }
        $taskCount++
    }
}
$taskResult = "PASS: $taskCount legacy model tower-stat / wave comparisons (Raon rules tested separately), all purchase costs and all six recipes match approved v2 design."
$taskResult | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/design-runtime-results.txt') -Encoding utf8
Write-Output $taskResult
