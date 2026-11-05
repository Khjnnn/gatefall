param([string]$CoreDirectory='', [string]$Tag='v0160', [int]$Stage=0)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
if(-not $CoreDirectory){$CoreDirectory=Join-Path $taskProject 'Assets/Scripts/Core'}
Add-Type -Path @((Get-ChildItem -LiteralPath $CoreDirectory -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/TowerDensityReview.cs'))
if($Stage -lt 0 -or $Stage -gt 14){throw 'Stage must be0(all) or1..14'}
$taskRows=if($Stage -gt 0){[Gatefall.Tests.TowerDensityReview]::Models($Stage-1)}else{[Gatefall.Tests.TowerDensityReview]::Models()}
$taskExpected=if($Stage -gt 0){9}else{126}
if($taskRows.Count -ne $taskExpected -or @($taskRows|Where-Object State -NotIn @('Won','Lost')).Count){throw 'Incomplete density models'}
[pscustomobject]@{tag=$Tag;stageSelection=$Stage;method='Selected stages x3 policies x3 seeds; same purchase policy before/after. First card, automatic skill/boss avoidance. Active means fired in last3 combat seconds, not same-frame rendering; not human fun.';models=$taskRows}|ConvertTo-Json -Depth 5|Set-Content (Join-Path $taskProject "TestResults/tower-density-$Tag.json")
$taskRows|Group-Object Policy|ForEach-Object{[pscustomobject]@{policy=$_.Name;wins=@($_.Group|Where-Object State -eq Won).Count;meanTowers=[math]::Round(($_.Group|Measure-Object Towers -Average).Average,1);maxActive3s=($_.Group|Measure-Object PeakActive3Seconds -Maximum).Maximum}}|Format-Table
