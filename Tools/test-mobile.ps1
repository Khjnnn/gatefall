$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -CompilerOptions '/langversion:7.3' -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Assets/Scripts/Presentation/HomeMenu.cs') + (Join-Path $taskProject 'Assets/Scripts/Presentation/DailyLabels.cs') + (Join-Path $taskProject 'Assets/Scripts/Presentation/FusionPreview.cs') + (Join-Path $taskProject 'Assets/Editor/FusionPreviewChecks.cs') + (Join-Path $taskProject 'Tests/MobileChecks.cs') + (Join-Path $taskProject 'Tests/HudStatusChecks.cs') + (Join-Path $taskProject 'Tests/HudOcclusionChecks.cs'))
$taskResult = [Gatefall.Tests.MobileChecks]::RunAll()
$taskResult += [Environment]::NewLine + [Gatefall.Tests.FusionPreviewChecks]::Run()
$taskResult | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults\mobile-results.txt') -Encoding utf8
Write-Output $taskResult
