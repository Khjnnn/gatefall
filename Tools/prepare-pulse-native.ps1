$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
if(Test-Path -LiteralPath 'TestResults/v0190-pulse-seed.sav'){throw 'Preserve existing native fixture'}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+(Join-Path $taskProject 'Tests/TowerDensityReview.cs'))
$taskP=[Gatefall.Core.CampaignProfile]::new()
$taskP.MagicCores=86;$taskP.StoryMask=16383;$taskP.TutorialMask=16383;$taskP.LastReward=24;$taskP.LastRewardRun=[Guid]::NewGuid().ToString('N')
for($taskI=0;$taskI -lt 8;$taskI++){$taskP.BestStars[$taskI]=2;$taskP.FirstCleared[$taskI]=$true}
if(-not $taskP.Valid() -or -not $taskP.IsStageOpen(8) -or -not $taskP.ShouldShowLesson(8)){throw 'Fixture not ready for stage9'}
$taskFile=Join-Path $taskProject 'TestResults/v0190-pulse-seed.sav'
[IO.File]::WriteAllBytes($taskFile,[Gatefall.Core.CampaignStore]::Encode($taskP))
$taskG=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[8]})
$taskMethod=[Gatefall.Tests.TowerDensityReview].GetMethod('Order',[Reflection.BindingFlags]'NonPublic,Static')
$taskOrder=[Collections.Generic.List[int]]::new()
foreach($taskSlot in $taskMethod.Invoke($null,@($taskG))){$taskOrder.Add([int]$taskSlot)}
$taskAnchor=$taskOrder[0];$taskPartner=-1;$taskClosest=1000
foreach($taskSlot in $taskOrder){if($taskSlot -ne $taskAnchor){$taskD=[Gatefall.Core.V2]::Distance($taskG.BoardSockets[$taskSlot],$taskG.BoardSockets[$taskAnchor]);if($taskD -lt $taskClosest){$taskClosest=$taskD;$taskPartner=$taskSlot}}}
$taskOrder.Remove($taskPartner)|Out-Null;$taskOrder.Insert(1,$taskPartner)
$taskRows=@(for($taskI=0;$taskI -lt $taskG.BoardSockets.Length;$taskI++){$taskV=$taskG.BoardSockets[$taskI];[pscustomobject]@{slot=$taskI;x=$taskV.X;y=$taskV.Y;sx=[math]::Round(1200+$taskV.X*1080/25.2);sy=[math]::Round(540+(.35-$taskV.Y)*1080/25.2);rank=$taskOrder.IndexOf($taskI)}})
$taskRows|Export-Csv -LiteralPath 'TestResults/v0190-pulse-sites.csv' -NoTypeInformation
[pscustomobject]@{created=(Get-Date).ToString('o');synthetic=$true;magic=$taskP.MagicCores;stars=$taskP.StarsTotal;pastReward=$taskP.LastReward;pastRewardId=$taskP.LastRewardRun;stage=9;sites=24;scheduled=624;starting=280;firstReward=26;anchor=$taskAnchor;partner=$taskPartner;order=@($taskOrder);rows=$taskRows;projection='2400x1080 full camera(0,.35)/ortho12.6; verify actual screenshot before input';nativePlan='Start model first6: Spear0 Binding12 Spear9 Rupture10 Spear11 Spear15 /270. Reinforce/upgrade/fuse with actual available resources, capture late pulse, clear/save; not exact model replay.'}|ConvertTo-Json -Depth 6|Set-Content TestResults/v0190-pulse-plan.json
$taskRows|Where-Object rank -lt 8|Sort-Object rank|ConvertTo-Json -Compress
