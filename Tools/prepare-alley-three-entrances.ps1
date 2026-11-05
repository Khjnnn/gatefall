$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskRoot=Join-Path $taskProject 'TestResults/alley-v0192-review'
$taskNew=Join-Path $taskRoot 'three-entrances'
if(Test-Path -LiteralPath $taskNew){throw 'Preserve candidate snapshot'}
New-Item -ItemType Directory -Path $taskNew|Out-Null
foreach($taskFile in Get-ChildItem (Join-Path $taskRoot 'baseline') -Filter '*.cs'){Copy-Item -LiteralPath $taskFile.FullName -Destination (Join-Path $taskNew $taskFile.Name)}
$taskOriginal=Get-Content (Join-Path $taskNew 'BattlefieldLayouts.cs') -Raw
$taskNeedle=@'
            L("alley-west-north-elbows",1,
              P(-23,-6,-14,-6,-14,1,-7,1,-7,-3,-3,-3,0,0),
              P(12,11,12,6,5,6,5,1,0,1,0,0),none),
'@
$taskReplacement=@'
            L("alley-three-staggered-courts",1,
              P(-23,-6,-14,-6,-14,1,-7,1,-7,-3,-3,-3,0,0),
              P(12,11,12,6,5,6,5,1,0,1,0,0),
              P(23,-3,17,-3,17,4,10,4,10,-3,3,-3,0,0)),
'@
$taskText=$taskOriginal.Replace("`r`n","`n")
if(([regex]::Matches($taskText,[regex]::Escape($taskNeedle))).Count -ne 1){throw 'Ambiguous stage8 map'}
[IO.File]::WriteAllText((Join-Path $taskNew 'BattlefieldLayouts.cs'),$taskText.Replace($taskNeedle,$taskReplacement))
$taskCampaign=Get-Content (Join-Path $taskNew 'Campaign.cs') -Raw
$taskStageNeedle='S(7,"남쪽 골목",22,new[]{0,1},'
if(([regex]::Matches($taskCampaign,[regex]::Escape($taskStageNeedle))).Count -ne 1){throw 'Ambiguous stage8 lane array'}
[IO.File]::WriteAllText((Join-Path $taskNew 'Campaign.cs'),$taskCampaign.Replace($taskStageNeedle,'S(7,"남쪽 골목",22,new[]{0,1,2},'))
'Prepared third entrance as a bundled geometry/lane-allocation candidate; west/north paths and all formation timing/kinds stay unchanged. Normal and crisis share this stage layout.'
