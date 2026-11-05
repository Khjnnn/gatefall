param([ValidateSet('baseline','product')][string]$Source='product')
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$core=if($Source -eq 'baseline'){'TestResults/v0250-baseline/Assets/Scripts/Core'}else{'Assets/Scripts/Core'}
Add-Type -Path @((Get-ChildItem $core -Filter '*.cs').FullName+(Join-Path (Get-Location) 'Tests/LateScopeReview.cs'))
$rows=@([Gatefall.Tests.LateScopeReview]::Inspect()|ForEach-Object {
    $r=$_
    [ordered]@{stage=$r.Stage;crisis=$r.Crisis;start=$r.Start;supply=$r.Supply;total=$r.Total;
        roads=@(for($i=0;$i -lt $r.Roads.Length;$i++){[ordered]@{lane=$i;points=@($r.Roads[$i]|ForEach-Object {[ordered]@{x=$_.X;y=$_.Y}})}});
        sites=@($r.Sites|ForEach-Object {[ordered]@{x=$_.X;y=$_.Y}});
        schedule=@($r.Schedule|Select-Object Wave,Lane,Kind,Count,Start,Interval,Warned,Swarm);
        spawnStats=@($r.SpawnStats|Select-Object Wave,Kind,Health,Speed)}
})
[ordered]@{created=(Get-Date -Format o);source=$Source;staticContracts=$rows.Count;spawnStatSamples=840;combatRuns=0;rows=$rows}|ConvertTo-Json -Depth 12|Set-Content ("TestResults/v0250-scope-$Source-dto.json")
"Recorded ${Source}:28 static contracts/840spawn stat samples; no battle."
