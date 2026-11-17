# Report-only: projects every fixed-camera board onto twelve screens and counts controls/menus covering an entrance or pad.
# pwsh -NoProfile -File .\Tools\review-hud-occlusion.ps1 [-Detail]. 'any' = boxes touch (>=2% of the target); 'major' = at least 20% of the target hidden.
param([switch]$Detail)
$ErrorActionPreference='Stop'
$PSStyle.OutputRendering='PlainText'
$root=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName+"$root/Tests/HudOcclusionChecks.cs")
$rows=[Gatefall.Tests.HudOcclusionChecks]::Review()
function S($g,$p){($g|Measure-Object $p -Sum).Sum}
foreach($ch in 1,2,3,0){ $g=if($ch){@($rows|? Chapter -eq $ch)}else{$rows}
 "ch$ch rows=$($g.Count) | start any {0}->{1} major {2}->{3} | call any {4}->{5} major {6}->{7} | speed any {8}->{9} major {10}->{11} | panel any {12}->{13} major {14}->{15} of {16} | skill {17}/{18} stick {19}/{20} pause {21}/{22} currency {23}/{24} | roads start {25} call {26}" -f (S $g StartBefore),(S $g Start),(S $g StartBeforeMajor),(S $g StartMajor),(S $g CallBefore),(S $g Call),(S $g CallBeforeMajor),(S $g CallMajor),(S $g SpeedBefore),(S $g Speed),(S $g SpeedBeforeMajor),(S $g SpeedMajor),(S $g PanelBefore),(S $g Panel),(S $g PanelBeforeMajor),(S $g PanelMajor),(S $g PanelCases),(S $g Skill),(S $g SkillMajor),(S $g Stick),(S $g StickMajor),(S $g Pause),(S $g PauseMajor),(S $g Currency),(S $g CurrencyMajor),(S $g StartRoad),(S $g CallRoad)}
"--- remaining start/call/speed"
$rows|? {$_.Start+$_.Call+$_.Speed -gt 0}|ft Stage,Screen,Start,StartMajor,Call,CallMajor,Speed,StartX,StartY,CallX,CallY,CallH -AutoSize|Out-String -Width 220
"--- moved (count by stage)"
$rows|? {$_.StartY -ne 496 -or [math]::Abs($_.StartX-($_.CallX+30)) -gt 1 -or $_.CallY -ne 488 -or $_.SpeedX -ne 274}|Group-Object Stage|%{ "{0}: {1} screens; e.g. start=({2},{3},h{4}) call=({5},{6},h{7}) speed={8}" -f $_.Name,$_.Count,$_.Group[0].StartX,$_.Group[0].StartY,$_.Group[0].StartH,$_.Group[0].CallX,$_.Group[0].CallY,$_.Group[0].CallH,$_.Group[0].SpeedX}
"--- panel by screen"
$rows|Group-Object Screen|%{"{0}: any {1}->{2} major {3}->{4} of {5}" -f $_.Name,(S $_.Group PanelBefore),(S $_.Group Panel),(S $_.Group PanelBeforeMajor),(S $_.Group PanelMajor),(S $_.Group PanelCases)}
if($Detail){$rows|? Chapter -eq 3|? {$_.SkillMajor+$_.StickMajor+$_.PauseMajor+$_.CurrencyMajor -gt 0}|ft Stage,Screen,SkillMajor,StickMajor,PauseMajor,CurrencyMajor -AutoSize}
