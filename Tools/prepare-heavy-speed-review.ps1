$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot=Join-Path $taskProject 'TestResults/heavy-speed-v0190-review'
if(Test-Path -LiteralPath $taskRoot){throw 'Preserve existing heavy-speed review'}
foreach($taskRow in @(Get-Content TestResults/source-hashes-v0190.json -Raw|ConvertFrom-Json)){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw "Product source differs: $($taskRow.path)"}}
New-Item -ItemType Directory -Path $taskRoot|Out-Null
$taskOriginal=[IO.File]::ReadAllText((Join-Path $taskProject 'Assets/Scripts/Core/DefenseSimulation.cs'))
$taskNeedle='                else if(g.Kind==EnemyKind.Siege)speed*=1.1f;'
if(([regex]::Matches($taskOriginal,[regex]::Escape($taskNeedle))).Count -ne 1){throw 'Ambiguous speed insertion'}
foreach($taskName in @('baseline','speed125','speed140')){
    $taskDir=Join-Path $taskRoot $taskName
    New-Item -ItemType Directory -Path $taskDir|Out-Null
    foreach($taskCore in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $taskCore.FullName -Destination (Join-Path $taskDir $taskCore.Name)}
    if($taskName -ne 'baseline'){
        $taskScale=if($taskName -eq 'speed125'){'1.25'}else{'1.40'}
        $taskLine="                    if(Rules.Stage.Index==8 && !Rules.Crisis && g.Wave>=3 && (g.Kind==EnemyKind.Brute || g.Kind==EnemyKind.Siege))speed*=$($taskScale)f;"
        $taskChanged=$taskOriginal.Replace($taskNeedle,$taskNeedle+"`n"+$taskLine)
        if($taskChanged.Replace("`n"+$taskLine,'') -ne $taskOriginal){throw 'Candidate has unrelated changes'}
        [IO.File]::WriteAllText((Join-Path $taskDir 'DefenseSimulation.cs'),$taskChanged)
    }
}
$taskHarness=[IO.File]::ReadAllText((Join-Path $taskProject 'TestResults/investment-v0190-review/InvestmentNecessityReview.cs'))
$taskTransforms=@(
    @('public int EndingCredits,','public double LateHeavyWithLightSeconds, LastHeavyKillTime, LastLightKillTime; public int PeakHeavyWithLight; public int EndingCredits,'),
    @('double lateEnemySeconds=0;','double lateEnemySeconds=0,heavyWithLightSeconds=0,lastHeavyKill=-1,lastLightKill=-1;int peakHeavyWithLight=0;'),
    @('foreach(CombatEvent e in g.Events){if(e.Kind==EventKind.CoreHurt)', 'foreach(CombatEvent e in g.Events)if(e.Kind==EventKind.Kill){if(e.Source==(int)EnemyKind.Brute || e.Source==(int)EnemyKind.Siege)lastHeavyKill=g.Elapsed;else if(e.Source==(int)EnemyKind.Grunt || e.Source==(int)EnemyKind.Runner)lastLightKill=g.Elapsed;} int overlapping=0;if(g.Elapsed>=90){foreach(Enemy heavy in g.Enemies)if(heavy.Health>0 && (heavy.Kind==EnemyKind.Brute || heavy.Kind==EnemyKind.Siege)){foreach(Enemy light in g.Enemies)if(light.Health>0 && (light.Kind==EnemyKind.Grunt || light.Kind==EnemyKind.Runner) && V2.DistanceSquared(heavy.Position,light.Position)<=27.04f){overlapping++;break;}}heavyWithLightSeconds+=overlapping*.05;peakHeavyWithLight=Math.Max(peakHeavyWithLight,overlapping);} foreach(CombatEvent e in g.Events){if(e.Kind==EventKind.CoreHurt)'),
    @('return new Result {EndingCredits=', 'return new Result {LateHeavyWithLightSeconds=heavyWithLightSeconds,LastHeavyKillTime=lastHeavyKill,LastLightKillTime=lastLightKill,PeakHeavyWithLight=peakHeavyWithLight,EndingCredits=')
)
$taskObserved=$taskHarness
foreach($taskPair in $taskTransforms){if(([regex]::Matches($taskObserved,[regex]::Escape($taskPair[0]))).Count -ne 1){throw 'Observer insertion ambiguous'};$taskObserved=$taskObserved.Replace($taskPair[0],$taskPair[1])}
$taskReversed=$taskObserved
for($taskI=$taskTransforms.Count-1;$taskI -ge 0;$taskI--){$taskPair=$taskTransforms[$taskI];$taskReversed=$taskReversed.Replace($taskPair[1],$taskPair[0])}
if($taskReversed -ne $taskHarness){throw 'Observer is not reversible'}
[IO.File]::WriteAllText((Join-Path $taskRoot 'InvestmentNecessityReview.cs'),$taskObserved)
[pscustomobject]@{created=(Get-Date).ToString('o');task=170;productVersion='0.19.0';source='TestResults/investment-v0190-review/InvestmentNecessityReview.cs';observerChangesReversible=$true;speedCandidates=@(1.25,1.40);productChanged=$false}|ConvertTo-Json|Set-Content (Join-Path $taskRoot 'preparation.json')
'Prepared isolated baseline and two speed candidates; product unchanged'
