$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
New-Item -ItemType Directory -Path (Join-Path $project 'TestResults') -Force|Out-Null
Add-Type -Path @((Get-ChildItem (Join-Path $project 'Assets/Scripts/Core') -Filter '*.cs').FullName+(Join-Path $project 'Tests/RunObservationChecks.cs'))
$result=[Gatefall.Tests.RunObservationChecks]::Run()
$result|Set-Content (Join-Path $project 'TestResults/run-observation-results.txt')
$result
