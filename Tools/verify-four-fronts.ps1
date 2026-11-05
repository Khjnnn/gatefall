param([string]$Tag='')
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
if(-not $Tag){$taskText=Get-Content (Join-Path $taskProject 'ProjectSettings/ProjectSettings.asset') -Raw;$Tag='v'+[regex]::Match($taskText,'(?m)^  bundleVersion: (\S+)').Groups[1].Value.Replace('.','')}
Add-Type -Path @((Get-ChildItem (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/FourFrontChecks.cs'))
$taskResult=[Gatefall.Tests.FourFrontChecks]::Verify()
$taskResult|Set-Content (Join-Path $taskProject "TestResults/four-fronts-$Tag-checks.txt")
Write-Output $taskResult
$taskStage=[Gatefall.Core.CampaignCatalog]::Stages[9]
$taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=$taskStage})
$taskRoads=@(foreach($taskRoad in $taskG.Roads){[pscustomobject]@{points=@($taskRoad|Select-Object X,Y)}})
[pscustomobject]@{stage=10;layout=[Gatefall.Core.BattlefieldLayouts]::Get($taskStage).Id;roads=$taskRoads;sockets=@($taskG.BoardSockets|Select-Object X,Y);lanes=$taskG.OpenLanes}|ConvertTo-Json -Depth 8|Set-Content (Join-Path $taskProject "TestResults/four-fronts-$Tag-map.json")
