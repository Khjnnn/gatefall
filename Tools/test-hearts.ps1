$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Add-Type -CompilerOptions '/langversion:7.3' -Path @((Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName+"$root/Tests/HeartChecks.cs")
[Gatefall.Tests.HeartChecks]::Run()
