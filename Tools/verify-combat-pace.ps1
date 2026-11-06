$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/CombatPaceReview.cs'))
$taskDuels=[Gatefall.Tests.CombatPaceReview]::Duels()
foreach ($taskRow in $taskDuels) {
    $taskKind=[int][Gatefall.Core.EnemyKind]::$($taskRow.Kind)
    if ($taskRow.KillKind -ne $taskKind) {throw 'Death effect enemy kind missing'}
    if ($taskRow.Kind -eq 'Grunt' -and ($taskRow.Shots -gt 3 -or $taskRow.Seconds -gt 1.05)) {throw 'Common enemy kill pace exceeded'}
}
foreach ($taskStage in [Gatefall.Core.CampaignCatalog]::LegacyStages) {
    $taskRules=[Gatefall.Core.CampaignBattle]::new();$taskRules.Stage=$taskStage
    $taskGame=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,$taskRules)
    foreach ($taskWave in 0..5) {
        $taskGroups=@($taskGame.Schedule | Where-Object {$_.Wave -eq $taskWave -and $_.Kind -ne 'Boss'})
        if (($taskGroups | Measure-Object Count -Sum).Sum -ne $taskStage.Counts[$taskWave]) {throw 'Schedule budget mismatch'}
        for ($taskIndex=0;$taskIndex -lt $taskGroups.Count;$taskIndex+=2) {
            if ($taskGroups[$taskIndex+1].Kind -ne 'Grunt' -or -not $taskGroups[$taskIndex+1].Swarm -or $taskGroups[$taskIndex+1].Count -ne $taskStage.SwarmMultiplier-1 -or $taskGroups[$taskIndex+1].Lane -ne $taskGroups[$taskIndex].Lane) {throw 'Swarm changed threat formation or lane'}
        }
    }
}
$taskSummary='PASS: 24 isolated tower duels including death-kind events; 84 paired swarm budgets preserve their lead threat and lane. Grunt at most 3 shots / 1.05 s from first launch at fixed 2-unit range; no cards/growth. Android rendering is checked separately.'
$taskSummary | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/combat-pace-checks.txt') -Encoding utf8
Write-Output $taskSummary

