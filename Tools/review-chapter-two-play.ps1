param([switch]$Pilot,[switch]$Crisis)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName+@("$root/Tests/ChapterTwoChecks.cs","$root/Tests/CompetentPlanReview.cs","$root/Tests/ChapterTwoPlayReview.cs"))
$rows=[Gatefall.Tests.ChapterTwoPlayReview]::Run([bool]$Pilot,[bool]$Crisis)
$tag=if($Pilot){'pilot'}else{'models'}
if($Crisis){$tag+='-crisis'}
$rows|ConvertTo-Json -Depth 9|Set-Content "$root/TestResults/chapter2-implementation/$tag.json" -Encoding utf8
$rows|Select-Object Stage,Policy,Seed,Growth,Outcome,MinimumCore,Spent,Towers,Fusions,End,SealAt|Format-Table -AutoSize
foreach($row in $rows){if($row.Supplies -gt 5 -or $row.CardOffers -gt 3 -or $row.Catalysts -gt 2 -or $row.Spent -gt 1150){throw 'Economic cap failure'}}
Write-Output "Scope: $($rows.Count) scripted conditions, Crisis=$Crisis; not human play or Android; purchase rules shared, actual costs/times/cards recorded individually."
