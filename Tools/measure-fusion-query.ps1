param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskPath='TestResults/fusion-query-v0209-measurement.json'
if($UseRecorded){Get-Content $taskPath -Raw;exit}
if(Test-Path $taskPath){throw 'Preserve measured sample; use -UseRecorded'}
foreach($taskR in (Get-Content TestResults/source-hashes-v0209.json -Raw|ConvertFrom-Json)){
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product source changed'}
}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/FusionQueryMeasurement.cs'))
$taskRows=@(foreach($taskCount in @(0,8,16,24)){[Gatefall.Tests.FusionQueryMeasurement]::Measure($taskCount,10000)})
[ordered]@{created=(Get-Date -Format o);task=227;version='0.20.9';hostRuntime=[Environment]::Version.ToString();rows=$taskRows;queryOnly=$true;androidBenchmark=$false;wholeFrameBenchmark=$false;newBattleModels=0;productChanged=$false;newApk=$false;scope='One warmed host .NET measurement; diagnostic, no timing threshold or phone/Unity IL2CPP FPS inference'}|ConvertTo-Json -Depth 6|Set-Content $taskPath
$taskRows|Select-Object Towers,Iterations,AllocatedBytes,Milliseconds|Format-Table -AutoSize
