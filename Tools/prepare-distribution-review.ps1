$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot=Join-Path $taskProject 'TestResults/distribution-v0190-review'
if(Test-Path -LiteralPath $taskRoot){throw 'Preserve existing review snapshots'}
New-Item -ItemType Directory -Path $taskRoot -Force|Out-Null
$taskOriginal=[IO.File]::ReadAllText((Join-Path $taskProject 'Assets/Scripts/Core/BattlefieldLayouts.cs')).Replace("`r`n","`n")
$taskNeedle=@'
            L("distribution-rooftop-loop",2,
              P(-23,0,-17,0,-17,6,-7,6,-7,0,0,0),none,
              P(23,-3,13,-3,13,4,5,4,5,0,0,0)),
'@
$taskCandidates=[ordered]@{
    'south-spine'=@'
            L("distribution-south-spine",2,
              P(-23,0,-18,0,-18,-5,-9,-5,3,-5,3,-2,0,0),none,
              P(23,5,15,5,15,-5,3,-5,3,-2,0,0)),
'@
    'north-merge'=@'
            L("distribution-offset-north-merge",2,
              P(-23,4,-17,4,-17,-4,-8,-4,-8,5,4,5,4,2,0,0),none,
              P(23,-4,16,-4,16,5,4,5,4,2,0,0)),
'@
}
if(([regex]::Matches($taskOriginal,[regex]::Escape($taskNeedle))).Count -ne 1){throw 'Stage9 definition ambiguous'}
foreach($taskName in @('baseline')+@($taskCandidates.Keys)) {
    $taskDir=Join-Path $taskRoot $taskName
    New-Item -ItemType Directory -Path $taskDir -Force|Out-Null
    foreach($taskFile in (Get-ChildItem Assets/Scripts/Core -Filter '*.cs')){Copy-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskDir $taskFile.Name)}
    if($taskName -ne 'baseline'){[IO.File]::WriteAllText((Join-Path $taskDir 'BattlefieldLayouts.cs'),$taskOriginal.Replace($taskNeedle,$taskCandidates[$taskName]))}
}
Copy-Item -LiteralPath 'ProjectSettings/ProjectSettings.asset' -Destination (Join-Path $taskRoot 'ProjectSettings.asset')
Copy-Item -LiteralPath 'TestResults/source-hashes-v0185.json' -Destination (Join-Path $taskRoot 'source-hashes-v0185.json')
Copy-Item -LiteralPath 'Tests/TowerDensityReview.cs' -Destination (Join-Path $taskRoot 'TowerDensityReview.cs')
'Prepared isolated baseline/south-spine/north-merge cores; product unchanged'
