$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
# Reuse static contracts only after all six selected core files match the current product.
foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){if((Get-FileHash $file.FullName).Hash -ne (Get-FileHash ('TestResults/courts-heavy-v0230-speed115-review/candidate-core/'+$file.Name)).Hash){throw 'Static contract source differs'}}
$contracts=(Get-Content TestResults/courts-heavy-v0230-speed115-scope.json -Raw|ConvertFrom-Json).contracts
function RoadLength($points){$n=0.0;for($i=1;$i -lt $points.Count;$i++){$n += [math]::Sqrt([math]::Pow($points[$i].X-$points[$i-1].X,2)+[math]::Pow($points[$i].Y-$points[$i-1].Y,2))};return $n}
function At($points,$distance){for($i=1;$i -lt $points.Count;$i++){$a=$points[$i-1];$b=$points[$i];$len=[math]::Sqrt([math]::Pow($b.X-$a.X,2)+[math]::Pow($b.Y-$a.Y,2));if($distance -le $len){$t=if($len -gt .00001){$distance/$len}else{0};return @{X=$a.X+($b.X-$a.X)*$t;Y=$a.Y+($b.Y-$a.Y)*$t}};$distance-=$len};return $points[-1]}
$rows=@(foreach($c in $contracts|Where-Object {-not $_.crisis}){
    $roads=@($c.roads|Where-Object {$_.points.Count -gt 1});$lengths=@(foreach($r in $roads){[ordered]@{lane=$r.lane;length=(RoadLength $r.points);vertices=$r.points.Count}})
    $west=@($roads|Where-Object lane -eq 0);$east=@($roads|Where-Object lane -eq 2);$mirror=$null;$rotation=$null
    if($west.Count -eq 1 -and $east.Count -eq 1){$wl=RoadLength $west[0].points;$el=RoadLength $east[0].points;$mirror=0.0;$rotation=0.0;foreach($i in 0..40){$a=At $west[0].points ($wl*$i/40);$b=At $east[0].points ($el*$i/40);$mirror += [math]::Sqrt([math]::Pow(-$a.X-$b.X,2)+[math]::Pow($a.Y-$b.Y,2))/41;$rotation += [math]::Sqrt([math]::Pow(-$a.X-$b.X,2)+[math]::Pow(-$a.Y-$b.Y,2))/41}}
    [pscustomobject][ordered]@{stage=$c.stage;sites=$c.sockets.Count;entrances=$roads.Count;roads=$lengths;scheduled=($c.schedule|Measure-Object Count -Sum).Sum;mirrorXMeanDistance=$mirror;rotation180MeanDistance=$rotation}
})
if(($rows|Measure-Object sites -Sum).Sum -ne 308 -or $rows.Count -ne 14){throw 'Static profile coverage differs'}
[ordered]@{created=(Get-Date -Format o);task=253;source='courts-heavy-v0230-speed115-scope.json';sixCurrentCoreFilesMatchSource=$true;newCombatModels=0;newStaticConstructions=0;reusedContracts=28;rows=$rows;selectedStage=11;reason='Remaining exact left/right X-reflection; preserve west and inspect a different southern shared approach. This metric is geometric similarity, not difficulty or human enjoyment.';productChanged=$false}|ConvertTo-Json -Depth 8|Set-Content TestResults/map-symmetry-v0240.json
$rows|ForEach-Object {[pscustomobject]@{stage=$_.stage;sites=$_.sites;entrances=$_.entrances;lengths=($_.roads|ForEach-Object {$_.lane.ToString()+':'+[math]::Round($_.length,2)}) -join '/';mirrorX=$_.mirrorXMeanDistance;rotation180=$_.rotation180MeanDistance}}|Format-Table -AutoSize
