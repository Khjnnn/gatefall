param([ValidateSet('baseline-control-audit-fixed','west-connector-pilot','west-lower-connector-pilot')][string]$Snapshot='west-connector-pilot',[switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$output="TestResults/final-road-header-v0211-$Snapshot.json"
if($UseRecorded){Get-Content $output -Raw;exit}
if(Test-Path $output){throw 'Preserve projection evidence; use -UseRecorded'}
$root=Join-Path (Get-Location) "TestResults/final-chicane-v0211-review/$Snapshot"
Add-Type -Path @(Get-ChildItem $root -Filter '*.cs'|Where-Object Name -ne AlleyDensityReview.cs|Select-Object -ExpandProperty FullName)
$g=[Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(),7,[Gatefall.Core.CampaignBattle]@{Stage=[Gatefall.Core.CampaignCatalog]::Stages[13]})
$rows=@(foreach($screen in @(@{w=2400;h=1080;sh=944},@{w=1920;h=1080;sh=971},@{w=1280;h=720;sh=647})){
    $layout=[Gatefall.Core.MobileLayout]::new($screen.w,$screen.h,[Gatefall.Core.Box2]::new(0,0,$screen.w,$screen.sh))
    $worldScale=$screen.h/(2*[math]::Max(12.6,24.8/($screen.w/[double]$screen.h)))
    $rifts=[Collections.Generic.List[Gatefall.Core.Box2]]::new()
    foreach($lane in $g.OpenLanes){$p=$g.GatePositions[$lane];$x=($screen.w/2+$p.X*$worldScale-$layout.Origin.X)/$layout.Scale;$y=($screen.h/2-($p.Y-.35)*$worldScale-$layout.Origin.Y)/$layout.Scale;$r=[Gatefall.Core.Box2]::new(($x-2.55*$worldScale/$layout.Scale),($y-1.6*$worldScale/$layout.Scale),(5.1*$worldScale/$layout.Scale),(3.2*$worldScale/$layout.Scale));if($r.Y -lt 62 -and $r.Y+$r.Height -gt 0){$rifts.Add($r)}}
    $header=$layout.WaveHeader($rifts)
    $roadRadius=1.42*$worldScale/$layout.Scale
    $centre=0;$edge=0
    foreach($lane in $g.OpenLanes){$road=$g.Roads[$lane];$length=[Gatefall.Core.BattlefieldLayouts]::RoadLength($road)
        for($distance=0.0;$distance -le $length;$distance+=.1){$next=0;$p=[Gatefall.Core.BattlefieldLayouts]::Along($road,$distance,[ref]$next);$x=($screen.w/2+$p.X*$worldScale-$layout.Origin.X)/$layout.Scale;$y=($screen.h/2-($p.Y-.35)*$worldScale-$layout.Origin.Y)/$layout.Scale
            if($header.Contains([Gatefall.Core.V2]::new($x,$y))){$centre++}
            $dx=[math]::Max(0,[math]::Max($header.X-$x,$x-($header.X+$header.Width)));$dy=[math]::Max(0,[math]::Max($header.Y-$y,$y-($header.Y+$header.Height)))
            if($dx*$dx+$dy*$dy -le $roadRadius*$roadRadius){$edge++}
        }
    }
    [ordered]@{screen="$($screen.w)x$($screen.h)";safeHeight=$screen.sh;originY=$layout.Origin.Y;header=($header|Select-Object X,Y,Width,Height);roadCentreSamplesUnderHeader=$centre;roadStrokeSampleDiscsIntersectHeader=$edge;sampleStepWorld=.1;roadOuterRadiusWorld=1.42}
})
[ordered]@{created=(Get-Date -Format o);snapshot=$Snapshot;stage=14;projectionOnly=$true;actualAndroidCandidateTested=$false;roadStrokeEdgeApproximation=$true;rows=$rows}|ConvertTo-Json -Depth 6|Set-Content $output
$rows|ForEach-Object {[pscustomobject]$_}|Format-Table screen,safeHeight,roadCentreSamplesUnderHeader,roadStrokeSampleDiscsIntersectHeader -AutoSize
