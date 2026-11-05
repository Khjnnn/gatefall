param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$output='TestResults/north-front-v0230-lanes.json'
if($UseRecorded){Get-Content $output -Raw;exit}
if(Test-Path $output){throw 'Recorded observations exist; use -UseRecorded'}
foreach($r in(Get-Content TestResults/source-hashes-v0230.json -Raw|ConvertFrom-Json)){if((Get-FileHash $r.path).Hash -ne $r.sha256){throw 'Verified product changed'}}
$root=Join-Path (Get-Location) 'TestResults/north-front-v0230-review/lane-observation'
New-Item -ItemType Directory -Path $root -Force|Out-Null
foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $file.FullName -Destination $root}
$original=Get-Content TestResults/north-marker-v0230-review/pilot/NorthMarkerPositionReview.cs -Raw
$replacements=@(
    @('public int[] SiteShots;','public float[] LaneDamage; public int CoreHurtEvents; public string[] LeakLog; public int[] SiteShots;'),
    @('var shots=new int[g.BoardSockets.Length];','var laneDamage=new float[3]; int coreEvents=0; var leaks=new List<string>(); var shots=new int[g.BoardSockets.Length];'),
    @('foreach(var e in g.Events)if(e.Kind==EventKind.TowerShot){','foreach(var e in g.Events)if(e.Kind==EventKind.CoreHurt){if(e.Source<0 || e.Source>=3)throw new Exception("Unidentified campaign lane");laneDamage[e.Source]+=e.Value;coreEvents++;leaks.Add(g.Elapsed.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+" lane="+e.Source+" damage="+e.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));} foreach(var e in g.Events)if(e.Kind==EventKind.TowerShot){'),
    @('ActualTimes=actual.ToArray()','ActualTimes=actual.ToArray(),LaneDamage=laneDamage,CoreHurtEvents=coreEvents,LeakLog=leaks.ToArray()')
)
$observed=$original
foreach($pair in $replacements){if(([regex]::Matches($observed,[regex]::Escape($pair[0]))).Count -ne 1){throw 'Observer insertion anchor differs'};$observed=$observed.Replace($pair[0],$pair[1])}
$reverse=$observed;foreach($pair in $replacements){$reverse=$reverse.Replace($pair[1],$pair[0])};if($reverse -cne $original){throw 'Observer reverse differs'}
Set-Content -LiteralPath (Join-Path $root NorthMarkerPositionReview.cs) -Value $observed -NoNewline
Add-Type -Path (Get-ChildItem $root -Filter '*.cs').FullName
function Fixture([string]$path){@(foreach($r in(Get-Content $path -Raw|ConvertFrom-Json)){$c=[Gatefall.Tests.NorthMarkerPositionReview+Command]::new();$c.Time=$r.Time;$c.Action=$r.Action;$c.Kind=[Gatefall.Core.TowerKind]$r.Kind;$c.Site=$r.Site;$c})}
$control=[Gatefall.Tests.NorthMarkerPositionReview]::Models((Fixture 'TestResults/north-marker-v0230-review/pilot/commands.json'),[int[]]@(41),[int[]]@(21))[0]
$candidate=[Gatefall.Tests.NorthMarkerPositionReview]::Models((Fixture 'TestResults/north-front-v0230-review/pilot/commands.json'),[int[]]@(41),[int[]]@(21))[0]
[ordered]@{created=(Get-Date -Format o);task=245;newObservedModels=2;seed=41;source='Existing CoreHurt events read only; no core edits';observerReverseEqualsOriginal=$true;noProductChange=$true;baseline=$control;candidate=$candidate;equivalenceAuditPending=$true}|ConvertTo-Json -Depth 9|Set-Content $output
$oldControl=@((Get-Content TestResults/north-marker-v0230-pilot.json -Raw|ConvertFrom-Json).models|Where-Object {$_.Seed -eq 41 -and $_.MarkerSite -eq 21})[0]
$oldCandidate=@((Get-Content TestResults/north-front-v0230-pilot.json -Raw|ConvertFrom-Json).models|Where-Object Seed -eq 41)[0]
foreach($pair in @(@($oldControl,$control),@($oldCandidate,$candidate))){
    $old=$pair[0];$new=$pair[1]
    foreach($name in @('Seed','MarkerSite','Spent','Credits','Towers','CompletedOrders','Kills','Spawned','Remaining','MarkedHits','MarkerShots','FiredSites','FinalFiredTowers','PeakRecent3','PeakEnemies','AllSpawned','State')){if($old.$name -ne $new.$name){throw "Observer integer/state differs: $name"}}
    foreach($name in @('Core','Time','MarkerDamage','MarkBonus','MaxPurchaseDelay')){if([math]::Abs($old.$name-$new.$name) -gt .0001){throw "Observer float differs: $name"}}
    foreach($name in @('SiteShots','Cards','PurchaseLog','ActualTimes')){if(($old.$name -join '|') -ne ($new.$name -join '|')){throw "Observer array differs: $name"}}
    if([math]::Abs(($new.LaneDamage|Measure-Object -Sum).Sum-(300-$new.Core)) -gt .02){throw 'Lane damage sum differs from lost core health'}
}
$verified=Get-Content $output -Raw|ConvertFrom-Json;$verified.equivalenceAuditPending=$false;$verified|Add-Member -NotePropertyName equivalenceAuditPassed -NotePropertyValue $true;$verified|ConvertTo-Json -Depth 9|Set-Content $output
$control,$candidate|Select-Object Core,Time,CoreHurtEvents,@{n='LaneDamage';e={$_.LaneDamage -join '/'}},@{n='Leaks';e={$_.LeakLog -join ';'}}|Format-List
