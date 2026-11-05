# Read-only initial campaign schedules and the briefings derived from them.
param([ValidatePattern('^v[0-9]+$')][string]$Tag='v0112')
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter *.cs).FullName
$taskSchedules=@();$taskBriefings=@()
foreach($taskStage in 0..13){foreach($taskCrisis in @($false,$true)){
    $taskBattle=[Gatefall.Core.CampaignBattle]::new();$taskBattle.Stage=[Gatefall.Core.CampaignCatalog]::Stages[$taskStage];$taskBattle.Crisis=$taskCrisis
    $taskGame=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,$taskBattle)
    $taskSchedules += [pscustomobject]@{stage=$taskStage;crisis=$taskCrisis;groups=@($taskGame.Schedule | ForEach-Object {[pscustomobject]@{wave=$_.Wave;lane=$_.Lane;kind=[int]$_.Kind;count=$_.Count;start=$_.Start;interval=$_.Interval;warned=$_.Warned}})}
    for($taskWave=0;$taskWave -lt $taskGame.WaveCount;$taskWave++){
        $taskInfo=$taskGame.GetWaveBriefing($taskWave)
        $taskBriefings += [pscustomobject]@{stage=[Gatefall.Core.CampaignCatalog]::StageId($taskStage);crisis=$taskCrisis;wave=$taskWave+1;total=$taskInfo.Total;lanes=@(0..2 | ForEach-Object {$taskInfo.LaneCount($_)});kinds=@(0..4 | ForEach-Object {$taskInfo.KindCount([Gatefall.Core.EnemyKind]$_)});firstSpawn=$taskInfo.FirstSpawn;lastSpawn=$taskInfo.LastSpawn;threat=$taskInfo.Threat.ToString();summary=$taskInfo.Summary}
    }
}}
$taskSchedules | ConvertTo-Json -Depth 7 -Compress | Set-Content ("TestResults/wave-schedules-after-"+$Tag+".json") -Encoding UTF8
$taskBriefings | ConvertTo-Json -Depth 6 | Set-Content ("TestResults/wave-briefings-"+$Tag+".json") -Encoding UTF8
Write-Output ("Exported "+$taskBriefings.Count+" wave briefings across "+$taskSchedules.Count+" stage/mode combinations. No combat was run.")
