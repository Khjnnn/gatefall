param([string]$CoreDirectory='', [ValidatePattern('^[a-zA-Z0-9_-]+$')][string]$Tag='resonance-stats')
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
if(-not $CoreDirectory){$CoreDirectory=Join-Path $project 'Assets/Scripts/Core'}
$CoreDirectory=(Resolve-Path -LiteralPath $CoreDirectory).Path
Add-Type -Path @((Get-ChildItem -LiteralPath $CoreDirectory -Filter '*.cs').FullName+(Join-Path $project 'Tests/ResonanceStatsReview.cs'))
$result=[Gatefall.Tests.ResonanceStatsReview]::Run()
[ordered]@{source=@(Get-ChildItem -LiteralPath $CoreDirectory -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}});
    report=$result;scope='Exact full-field stat snapshots over modes/cards/levels/neighborhoods and mutable-return isolation; host .NET warmed allocation count, not Android/Unity frame time or GC pause measurement.'
}|ConvertTo-Json -Depth 7|Set-Content (Join-Path $project ('TestResults/'+$Tag+'.json'))
$result|Select-Object Cases,Hash|Format-List
$result.Allocations|Format-Table -AutoSize
