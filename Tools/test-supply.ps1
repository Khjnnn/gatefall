$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath "$root/Assets/Scripts/Core" -Filter '*.cs').FullName+"$root/Tests/SupplyChecks.cs") -CompilerOptions '/langversion:7.3'
[Gatefall.Tests.SupplyChecks]::Run()
