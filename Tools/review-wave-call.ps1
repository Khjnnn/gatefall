param([switch]$ChecksOnly)
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/WaveCallReview.cs'))
$taskChecks = [Gatefall.Tests.WaveCallReview]::Verify()
$taskChecks | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/wave-call-candidate-checks.txt') -Encoding utf8
Write-Output $taskChecks
if ($ChecksOnly) { exit 0 }
$taskReview = [Gatefall.Tests.WaveCallReview]::RunAll()
$taskReview | Set-Content -LiteralPath (Join-Path $taskProject 'TestResults/wave-call-candidate.tsv') -Encoding utf8
Write-Output 'Model report written. Use separately versioned Android evidence for delivery and interaction validation.'
