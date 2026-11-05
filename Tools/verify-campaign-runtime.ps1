$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path (Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName
$design = Get-Content -LiteralPath (Join-Path $taskProject 'Docs/Design/campaign-foundation-v2.json') -Raw | ConvertFrom-Json
$checks = [System.Collections.Generic.List[string]]::new()
function Check([bool]$condition,[string]$message) { if (-not $condition) { throw $message }; $checks.Add($message) }
$ids = @('Spear','Binding','Rupture','Arc','Shatter','Gravity','Seal','Barrier')
$lanes = @('west','north','east')
Check ($design.battle_completion.rule -eq 'all_scheduled_enemies_spawned_and_defeated_with_core_alive' -and $null -eq $design.battle_completion.hard_time_limit_seconds) 'Campaign ends on wave clear without a time deadline'
Check (-not $design.battle_completion.cleanup_grants_additional_supplies) 'Cleanup grants no extra supply budget'
for ($i=0;$i -lt 6;$i++) {
    $stage = [Gatefall.Core.CampaignCatalog]::Stages[$i]
    $d = $design.stages[$i]
    Check ($stage.Name -eq $d.name) "$i name"
    Check ($stage.SocketCount -eq $d.sockets) "$i sockets"
    Check (($stage.Counts -join ',') -eq ($d.wave_counts -join ',')) "$i wave budgets"
    Check ((@($stage.Lanes | ForEach-Object { $lanes[$_] }) -join ',') -eq ($d.lanes -join ',')) "$i entry lanes"
    Check ($stage.FirstReward -eq $d.first_clear_magic_cores) "$i first reward"
    Check ($stage.HasAdjacency -eq $d.adjacency_enabled) "$i adjacency gate"
    Check ($stage.HasCatalysts -eq $d.core_supply_enabled) "$i catalyst gate"
    Check ($stage.Boss -eq $d.require_boss_kill) "$i boss objective"
    for ($kind=0;$kind -lt 8;$kind++) {
        Check ($stage.Allows([Gatefall.Core.TowerKind]$kind) -eq ($ids[$kind] -in ($d.base_towers + $d.fusion_results))) "$i $($ids[$kind]) unlock"
    }
}
foreach ($d in $design.base_towers) {
    $kind = [array]::IndexOf($ids,$d.id)
    $tower = [Gatefall.Core.TowerCatalog]::Get([Gatefall.Core.TowerKind]$kind)
    Check ($tower.BuildCost -eq $d.build -and $tower.UpgradeCost -eq $d.upgrade -and $tower.Name -eq $d.name) "$($d.id) name and prices"
}
$report = "PASS: $($checks.Count) Raon runtime/design comparisons: stage names, sockets, lanes, six wave budgets, rewards, unlocks, boss requirement and four base prices."
$report | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/campaign-runtime-results.txt') -Encoding utf8
Write-Output $report
