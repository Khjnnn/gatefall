$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
$taskText=Get-Content (Join-Path $taskProject 'ProjectSettings/ProjectSettings.asset') -Raw
$taskTag='v'+[regex]::Match($taskText,'(?m)^  bundleVersion: (\S+)').Groups[1].Value.Replace('.','')
Add-Type -Path @((Get-ChildItem (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/EvacuationRouteChecks.cs'))
$taskResult=[Gatefall.Tests.EvacuationRouteChecks]::Verify()
$taskResult|Set-Content (Join-Path $taskProject "TestResults/evacuation-$taskTag-checks.txt")
Write-Output $taskResult
$taskStage=[Gatefall.Core.CampaignCatalog]::Stages[6]
$taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=$taskStage})
foreach($taskBefore in (Get-Content (Join-Path $taskProject 'Tests/Fixtures/stage7-threat-budget-v0171.json') -Raw|ConvertFrom-Json)) {
    $taskSum=(@($taskG.Schedule|Where-Object {$_.Wave -eq $taskBefore.wave -and $_.Kind.ToString() -eq $taskBefore.kind})|Measure-Object Count -Sum).Sum
    if([int]$taskSum -ne [int]$taskBefore.count){throw "Changed wave/kind budget $($taskBefore.wave)/$($taskBefore.kind)"}
}
$taskRoads=@(foreach($taskRoad in $taskG.Roads){[pscustomobject]@{points=@($taskRoad|Select-Object X,Y)}})
[pscustomobject]@{stage=7;layout=[Gatefall.Core.BattlefieldLayouts]::Get($taskStage).Id;roads=$taskRoads;sockets=@($taskG.BoardSockets|Select-Object X,Y);lanes=$taskG.OpenLanes;originalThreatBudgetPreserved=$true}|ConvertTo-Json -Depth 8|Set-Content (Join-Path $taskProject "TestResults/evacuation-$taskTag-map.json")
Write-Output 'PASS all6 original per-kind wave budgets preserved.'
