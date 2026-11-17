$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath "$root/Assets/Scripts/Core" -Filter '*.cs').FullName+"$root/Tests/CampaignSupplyChecks.cs") -CompilerOptions '/langversion:7.3'
[Gatefall.Tests.CampaignSupplyChecks]::Run()
