$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=Join-Path $root TestResults/chapter2-implementation
New-Item $out -ItemType Directory -Force|Out-Null
Add-Type -Path @((Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName+"$root/Tests/ChapterTwoChecks.cs"+"$root/Tests/RailCampaignChecks.cs")
$report=@([Gatefall.Tests.ChapterTwoChecks]::Run($out);[Gatefall.Tests.RailCampaignChecks]::Run())
$report|Set-Content "$out/chapter-two-checks.txt" -Encoding utf8
$report
