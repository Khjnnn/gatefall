param([string]$CoreDirectory='',[string]$Tag='v0140',[switch]$StressOnly)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
if(-not $CoreDirectory){$CoreDirectory=Join-Path $taskProject 'Assets/Scripts/Core'}
$taskFiles=@((Get-ChildItem -LiteralPath $CoreDirectory -Filter '*.cs').FullName)+(Join-Path $taskProject 'Tests/SwarmStressReview.cs')
if(-not $StressOnly){$taskFiles+=Join-Path $taskProject 'Tests/SwarmChecks.cs'}
Add-Type -Path $taskFiles
if(-not $StressOnly){$taskChecks=[Gatefall.Tests.SwarmChecks]::Verify();$taskChecks|Set-Content (Join-Path $taskProject "TestResults/swarm-checks-$Tag.txt") -Encoding utf8;Write-Output $taskChecks}
$taskRows=[Gatefall.Tests.SwarmStressReview]::Run()
[ordered]@{tag=$Tag;method='Host .NET fixed-position high-health workload / all board sites (actual Towers in rows) / 128,384,768 enemies / warm12 then measure90 steps. Different board capacities are not an equal-workload speed comparison; not Android FPS or player difficulty.';rows=$taskRows}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $taskProject "TestResults/swarm-stress-$Tag.json") -Encoding utf8
$taskRows|Select-Object Enemies,Towers,Milliseconds,AllocatedBytes,Damage,HealthSum,Projectiles|ConvertTo-Json -Compress|Write-Output
