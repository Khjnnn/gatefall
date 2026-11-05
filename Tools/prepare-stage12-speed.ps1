$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskRoot=Join-Path (Get-Location) 'TestResults/stage12-speed-v0202-review'
if(Test-Path -LiteralPath $taskRoot){throw 'Preserve existing speed candidate snapshots'}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0202.json -Raw|ConvertFrom-Json)){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product differs'}}
New-Item -ItemType Directory -Path $taskRoot|Out-Null
$taskOriginal=[IO.File]::ReadAllText((Join-Path (Get-Location) 'Assets/Scripts/Core/DefenseSimulation.cs')).Replace("`r`n","`n")
$taskNeedle='if(Rules.Stage.Index==8 && !Rules.Crisis && g.Wave>=3 && (g.Kind==EnemyKind.Brute || g.Kind==EnemyKind.Siege))speed*=1.25f;'
if(([regex]::Matches($taskOriginal,[regex]::Escape($taskNeedle))).Count -ne 1){throw 'Speed insertion ambiguous'}
foreach($taskName in @('baseline','speed115','speed125')){
    $taskDir=Join-Path $taskRoot $taskName
    New-Item -ItemType Directory -Path $taskDir|Out-Null
    foreach($taskCore in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $taskCore.FullName -Destination (Join-Path $taskDir $taskCore.Name)}
    if($taskName -ne 'baseline'){
        $taskScale=if($taskName -eq 'speed115'){'1.15'}else{'1.25'}
        $taskNew="                    if(Rules.Stage.Index==11 && !Rules.Crisis && g.Wave>=3 && (g.Kind==EnemyKind.Brute || g.Kind==EnemyKind.Siege))speed*=$($taskScale)f;"
        $taskChanged=$taskOriginal.Replace($taskNeedle,$taskNeedle+"`n"+$taskNew)
        if($taskChanged.Replace("`n"+$taskNew,'') -cne $taskOriginal){throw 'Unrelated candidate changes'}
        [IO.File]::WriteAllText((Join-Path $taskDir 'DefenseSimulation.cs'),$taskChanged)
    }
}
$taskHarness=Get-Content TestResults/marker-support-v0201-review/MarkerBattleReview.cs -Raw
$taskTransforms=@(
    @('public static DefenseSimulation Last;','public static DefenseSimulation Last; public static StageThreatObservation Audit; public static int TowerLimit=16;'),
    @('int next=2;g.Start();double actionAt=3;','Audit=new StageThreatObservation(g.BoardSockets.Length);int next=2;g.Start();double actionAt=3;'),
    @('if(!acted && next<slots.Count && g.Credits>=40)','if(!acted && next<slots.Count && g.Credits>=40 && g.Towers.Count<TowerLimit)'),
    @('g.Tick(.05f,new V2());g.Events.Clear();','g.Tick(.05f,new V2());Audit.Sample(g,.05f);g.Events.Clear();')
)
$taskObserved=$taskHarness
foreach($taskPair in $taskTransforms){if(([regex]::Matches($taskObserved,[regex]::Escape($taskPair[0]))).Count -ne 1){throw 'Observer insertion ambiguous'};$taskObserved=$taskObserved.Replace($taskPair[0],$taskPair[1])}
$taskReverse=$taskObserved
for($taskI=$taskTransforms.Count-1;$taskI -ge 0;$taskI--){$taskReverse=$taskReverse.Replace($taskTransforms[$taskI][1],$taskTransforms[$taskI][0])}
if($taskReverse -cne $taskHarness){throw 'Observer changed purchase/skill policy'}
[IO.File]::WriteAllText((Join-Path $taskRoot 'MarkerBattleReview.cs'),$taskObserved)
Copy-Item Tests/StageThreatObservation.cs (Join-Path $taskRoot 'StageThreatObservation.cs')
Copy-Item TestResults/source-hashes-v0202.json (Join-Path $taskRoot 'source-hashes-v0202.json')
'Prepared stage12 isolated baseline/speed115/speed125; read-only observation reverse matched.'
