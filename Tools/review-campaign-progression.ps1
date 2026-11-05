$ErrorActionPreference = 'Stop'
$project = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $project 'Assets/Scripts/Core') -Filter '*.cs').FullName +
    (Join-Path $project 'Tests/CampaignProgressionReview.cs'))
$result = [Gatefall.Tests.CampaignProgressionReview]::Review()
$settings = Get-Content -LiteralPath (Join-Path $project 'ProjectSettings/ProjectSettings.asset') -Raw
$version = [regex]::Match($settings, '(?m)^  bundleVersion: ([0-9.]+)').Groups[1].Value
if (-not $version) { throw 'Project version missing' }
$tag = 'v' + $version.Replace('.', '')
$path = Join-Path $project ("TestResults/campaign-progression-" + $tag + ".json")
$result | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $path -Encoding utf8
Write-Output ("Reviewed {0} stages and {1} simulated battles. {2}" -f $result.Stages.Length, $result.Runs.Length, $path)

