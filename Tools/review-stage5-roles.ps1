param([switch]$Locations)
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/Stage5RoleReview.cs'))
$taskResult = if ($Locations) { [Gatefall.Tests.Stage5RoleReview]::RunLocations() } else { [Gatefall.Tests.Stage5RoleReview]::RunAll() }
$taskFile = if ($Locations) { 'TestResults/stage5-role-locations-v01017.txt' } else { 'TestResults/stage5-roles-v01017.txt' }
$taskResult | Set-Content -LiteralPath (Join-Path $taskProject $taskFile) -Encoding utf8
Write-Output $taskResult
