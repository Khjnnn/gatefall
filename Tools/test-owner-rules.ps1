$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Add-Type -CompilerOptions '/langversion:7.3' -Path @((Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName+"$root/Assets/Scripts/Presentation/DailyLabels.cs"+"$root/Tests/OwnerRuleChecks.cs")
$env:GATEFALL_FIXTURES=Join-Path $root "Tests/Fixtures"
[Gatefall.Tests.OwnerRuleChecks]::Run()
