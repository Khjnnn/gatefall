param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOut='TestResults/stage13-front-choice-v0205-pilot.json'
if($UseRecorded){Get-Content $taskOut -Raw;exit}
if(Test-Path $taskOut){throw 'Preserve pilot; use -UseRecorded'}
foreach($taskR in (Get-Content TestResults/source-hashes-v0205.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed'}}
$taskSource=[IO.File]::ReadAllText((Join-Path (Get-Location) 'TestResults/stage13-fusion-choice-v0205-review/Stage13FusionChoiceReview.cs'))
$taskChanges=@(@('public static class Stage13FusionChoiceReview','public static class Stage13FrontChoiceReview'),@('site!=17','site!=13'),@('site==17','site==13'),@('partner.Id,17,null','partner.Id,13,null'))
$taskText=$taskSource
foreach($taskPair in $taskChanges){if(([regex]::Matches($taskText,[regex]::Escape($taskPair[0]))).Count -ne 1){throw 'Anchor transformation ambiguous'};$taskText=$taskText.Replace($taskPair[0],$taskPair[1])}
$taskInverse=$taskText;for($taskI=$taskChanges.Count-1;$taskI -ge 0;$taskI--){$taskInverse=$taskInverse.Replace($taskChanges[$taskI][1],$taskChanges[$taskI][0])}
if($taskInverse -cne $taskSource){throw 'Policy changed beyond anchor'}
$taskHarness=Join-Path (Get-Location) 'TestResults/Stage13FrontChoiceReview-v0205.cs';[IO.File]::WriteAllText($taskHarness,$taskText)
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/StageThreatObservation.cs')+$taskHarness)
$taskActions=@(foreach($taskR in (Get-Content TestResults/v0204-stage13-native-purchase-ledger.json -Raw|ConvertFrom-Json|Select-Object -First 11)){
    [Gatefall.Tests.Stage13FrontChoiceReview+Action]@{Time=$taskR.time;Command=$taskR.action;Kind=[Enum]::Parse([Gatefall.Core.TowerKind],$taskR.kind);Site=$(if($taskR.site -eq 17){13}else{$taskR.site})}
})
$taskRows=@(foreach($taskSeed in @(7,41)){foreach($taskObserver in @($false,$true)){
    [Gatefall.Tests.Stage13FrontChoiceReview]::UseObserver=$taskObserver
    $taskR=[Gatefall.Tests.Stage13FrontChoiceReview]::Run($taskSeed,14,$taskActions)
    if($taskR.Spent -ne 465 -or $taskR.Towers -ne 7 -or $taskR.CompletedActions -ne 11 -or $taskR.State -notin @('Won','Lost')){throw 'Pilot policy/budget validation failed'}
    [pscustomobject]@{seed=$taskSeed;plan=$(if($taskObserver){'observer13'}else{'arc13'});battle=$taskR}
}})
foreach($taskSeed in @(7,41)){$taskPair=@($taskRows|Where-Object seed -eq $taskSeed);if(($taskPair[0].battle.Cards|ConvertTo-Json -Compress) -cne ($taskPair[1].battle.Cards|ConvertTo-Json -Compress)){throw 'Paired card policies differ'}}
[ordered]@{created=(Get-Date -Format o);task=210;pilotModels=4;seeds=@(7,41);sourceVersion='0.20.5';replayAfterUnlockOnly=$true;actualSpent=465;finalTowers=7;fusionAnchor=13;partner=16;
    initialSpearPositionAndPartnerKindAlsoChange=$true;unusedCreditsPreserved=$true;nativeReplay=$false;productChanged=$false;newApk=$false;newAvd=$false;rows=$taskRows}|ConvertTo-Json -Depth 10|Set-Content $taskOut
$taskRows|ForEach-Object {[pscustomobject]@{seed=$_.seed;plan=$_.plan;state=$_.battle.State;core=$_.battle.Core;time=$_.battle.Time;spent=$_.battle.Spent;credits=$_.battle.Credits;fired=$_.battle.Observation.FiredSites;bonus=$_.battle.MarkBonus}}|Format-Table -AutoSize
