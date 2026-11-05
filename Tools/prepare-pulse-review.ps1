$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot=Join-Path $taskProject 'TestResults/pulse-v0190-review'
if(Test-Path -LiteralPath $taskRoot){throw 'Preserve existing pulse review snapshots'}
New-Item -ItemType Directory -Path $taskRoot -Force|Out-Null
$taskOriginal=[IO.File]::ReadAllText((Join-Path $taskProject 'Assets/Scripts/Core/RaonCombat.cs')).Replace("`r`n","`n")
$taskNeedle=@'
                    if(stage.Index==1 && wave>=2)arrivalWindow=12.0;
                    double step=arrivalWindow/Math.Max(1,stage.Counts[wave]-1);
                    double offset=j*multiplier*step;
'@
if(([regex]::Matches($taskOriginal,[regex]::Escape($taskNeedle))).Count -ne 1){throw 'Pulse insertion ambiguous'}
foreach($taskName in @('baseline','window16','window12')) {
    $taskDir=Join-Path $taskRoot $taskName
    New-Item -ItemType Directory -Path $taskDir -Force|Out-Null
    foreach($taskFile in (Get-ChildItem Assets/Scripts/Core -Filter '*.cs')){Copy-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskDir $taskFile.Name)}
    if($taskName -ne 'baseline') {
        $taskWindow=if($taskName -eq 'window16'){16}else{12}
        $taskReplace=@"
                    if(stage.Index==1 && wave>=2)arrivalWindow=12.0;
                    if(stage.Index==8 && !Rules.Crisis && wave>=3)arrivalWindow=$taskWindow.0;
                    double step=arrivalWindow/Math.Max(1,stage.Counts[wave]-1);
                    double offset=j*multiplier*step;
                    if(stage.Index==8 && !Rules.Crisis && wave>=3) {
                        double originalStep=22.0/Math.Max(1,stage.Counts[wave]-1);
                        double firstLaneOffset=(j%stage.Lanes.Length)*multiplier*originalStep;
                        double compression=(arrivalWindow-firstLaneOffset)/(22.0-firstLaneOffset);
                        offset=firstLaneOffset+(j*multiplier*originalStep-firstLaneOffset)*compression;
                        step=originalStep*compression;
                    }
"@
        [IO.File]::WriteAllText((Join-Path $taskDir 'RaonCombat.cs'),$taskOriginal.Replace($taskNeedle,$taskReplace))
    }
}
Copy-Item -LiteralPath 'TestResults/source-hashes-v0185.json' -Destination (Join-Path $taskRoot 'source-hashes-v0185.json')
Copy-Item -LiteralPath 'Tests/TowerDensityReview.cs' -Destination (Join-Path $taskRoot 'TowerDensityReview-original.cs')
$taskHarness=[IO.File]::ReadAllText((Join-Path $taskProject 'Tests/TowerDensityReview.cs'))
$taskHarness=$taskHarness.Replace('public string Policy, State; public float Core, Time;','public string Policy, State; public float Core, Time; public int PeakEnemies, PeakEnemiesLate; public double LateEnemySeconds; public int[] SiteShots; public string[] PurchaseLog;')
$taskHarness=$taskHarness.Replace('int peakActive=0,peakProjectiles=0;','int peakActive=0,peakProjectiles=0,peakEnemies=0,peakEnemiesLate=0;double lateEnemySeconds=0;')
$taskHarness=$taskHarness.Replace('int active=0;foreach(Tower t in g.Towers)','int alive=0;foreach(Enemy enemy in g.Enemies)if(enemy.Health>0)alive++;peakEnemies=Math.Max(peakEnemies,alive);if(g.Elapsed>=90){peakEnemiesLate=Math.Max(peakEnemiesLate,alive);lateEnemySeconds+=alive*.05;}int active=0;foreach(Tower t in g.Towers)')
$taskHarness=$taskHarness.Replace('return new Result {Stage=index+1,','return new Result {PeakEnemies=peakEnemies,PeakEnemiesLate=peakEnemiesLate,LateEnemySeconds=lateEnemySeconds,SiteShots=shots,PurchaseLog=g.Telemetry.Purchases.ToArray(),Stage=index+1,')
foreach($taskExpected in @('public int PeakEnemies, PeakEnemiesLate;','peakEnemiesLate=0;double lateEnemySeconds=0;','int alive=0;foreach(Enemy enemy','PeakEnemies=peakEnemies,')){if(-not $taskHarness.Contains($taskExpected)){throw 'Observation insertion incomplete'}}
[IO.File]::WriteAllText((Join-Path $taskRoot 'TowerDensityReview-observed.cs'),$taskHarness)
'Prepared baseline/window16/window12 and read-only observation harness; product unchanged'
