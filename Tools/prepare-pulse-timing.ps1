$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot='TestResults/pulse-v0190-review'
$taskText=[IO.File]::ReadAllText((Join-Path $taskProject "$taskRoot/TowerDensityReview-observed.cs"))
$taskText=$taskText.Replace('public double LateEnemySeconds;','public double LateEnemySeconds, QuietSecondsLate, MaxQuietGapLate;')
$taskText=$taskText.Replace('double lateEnemySeconds=0;','double lateEnemySeconds=0,quietSecondsLate=0,quietGap=0,maxQuietGap=0;')
$taskText=$taskText.Replace('g.Tick(.05f,dodge?','double clockBefore=g.Elapsed;g.Tick(.05f,dodge?')
$taskText=$taskText.Replace('int active=0;foreach(Tower t in g.Towers)','if(g.Elapsed>=90 && g.State!=RunState.Won && g.State!=RunState.Lost){double delta=Math.Max(0,g.Elapsed-clockBefore);if(alive==0){quietSecondsLate+=delta;quietGap+=delta;maxQuietGap=Math.Max(maxQuietGap,quietGap);}else quietGap=0;}int active=0;foreach(Tower t in g.Towers)')
$taskText=$taskText.Replace('PeakEnemies=peakEnemies,','QuietSecondsLate=quietSecondsLate,MaxQuietGapLate=maxQuietGap,PeakEnemies=peakEnemies,')
foreach($taskNeedle in @('QuietSecondsLate, MaxQuietGapLate;','double clockBefore=g.Elapsed;','if(alive==0){quietSecondsLate+=delta;','QuietSecondsLate=quietSecondsLate,')){if(-not $taskText.Contains($taskNeedle)){throw 'Timing observation insertion incomplete'}}
[IO.File]::WriteAllText((Join-Path $taskProject "$taskRoot/TowerDensityReview-timing.cs"),$taskText)
