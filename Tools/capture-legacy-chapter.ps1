param([string]$Name="legacy-after.json")
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName+"$root/Tests/LegacyChapterDigest.cs")
[Gatefall.Tests.LegacyChapterDigest]::Run()|ConvertTo-Json -Depth 8|Set-Content "$root/TestResults/chapter2-implementation/$Name" -Encoding utf8
