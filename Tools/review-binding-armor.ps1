param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$output=Join-Path $root 'binding-armor-candidate-v0285.json'
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Use -UseRecorded instead of repeating the completed pilot'}
    $source=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}})
    $copy=Join-Path $root 'binding-armor-candidate-core';New-Item -ItemType Directory $copy -Force|Out-Null
    Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{Copy-Item $_.FullName (Join-Path $copy $_.Name)}
    $path=Join-Path $copy 'DefenseSimulation.cs';$text=Get-Content $path -Raw
    $before='ApplySlow(e, p.SourceId, p.Stats.Slow, p.Stats.SlowSeconds);'
    $after='ApplySlow(e, p.SourceId, p.Kind==TowerKind.Binding && e.Armor>0 ? p.Stats.Slow/(e.Kind==EnemyKind.Siege ? .4f : e.Kind==EnemyKind.Brute ? .65f : 1) : p.Stats.Slow, p.Stats.SlowSeconds);'
    if($text.IndexOf($before) -lt 0 -or $text.IndexOf($before) -ne $text.LastIndexOf($before)){throw 'Slow impact anchor differs'}
    $candidateText=$text.Replace($before,$after);if($candidateText.Replace($after,$before) -cne $text){throw 'Unexpected candidate source delta'}
    [IO.File]::WriteAllText($path,$candidateText,[Text.UTF8Encoding]::new($false))
    Add-Type -Path @((Get-ChildItem $copy -Filter '*.cs').FullName+@('Tests/AccountGrowthChecks.cs','Tests/EarlyCombatDigest.cs','Tests/BasicReinforcementReview.cs','Tests/BindingArmorReview.cs')|ForEach-Object{(Resolve-Path $_).Path})
    $units=[Gatefall.Tests.BindingArmorReview]::Check($true)
    $rows=[Collections.Generic.List[object]]::new();$pairs=[Collections.Generic.List[object]]::new()
    $variants=@(
        @{file='basic-reinforcement-v0285.json';mixed=$false;front=$false;late=$false;hunter=0;motion=0},
        @{file='basic-reinforcement-mixed-v0285.json';mixed=$true;front=$false;late=$false;hunter=0;motion=0},
        @{file='basic-reinforcement-front-v0285.json';mixed=$true;front=$true;late=$false;hunter=0;motion=0},
        @{file='reinforcement-order-v0285.json';mixed=$false;front=$false;late=$true;hunter=0;motion=0},
        @{file='reinforcement-doha-motion2-v0285.json';mixed=$false;front=$false;late=$false;hunter=2;motion=2}
    )
    foreach($variant in $variants){
        $baseline=Get-Content (Join-Path $root $variant.file) -Raw|ConvertFrom-Json
        foreach($hash in $baseline.source){if((Get-FileHash (Join-Path 'Assets/Scripts/Core' $hash.file)).Hash.ToLowerInvariant() -cne $hash.sha256){throw 'Original core differs'}}
        foreach($seed in @(7,41)){
            $old=@($baseline.rows|Where-Object{$_.Seed -eq $seed -and $_.Control})[0]
            $new=[Gatefall.Tests.BasicReinforcementReview]::Play($seed,$true,$variant.mixed,$variant.front,$variant.late,$variant.hunter,$variant.motion);$rows.Add($new)
            [ordered]@{source=$source;units=$units;rows=$rows;scope='Raw candidate rows saved before comparison checks'}|ConvertTo-Json -Depth 11|Set-Content (Join-Path $root 'binding-armor-raw-v0285.json') -Encoding utf8
            if($old.PrefixHash -cne $new.PrefixHash -or $old.Spent -ne $new.Spent -or ($old.Cards -join ',') -cne ($new.Cards -join ',')){throw 'Armor candidate changed prefix/spend/cards'}
            $pairs.Add([ordered]@{file=$variant.file;seed=$seed;beforeState=$old.State;afterState=$new.State;beforeCore=$old.Core;afterCore=$new.Core;beforeEnd=$old.End;afterEnd=$new.End;beforeStrongSlow=$old.StrongSlowSamples;afterStrongSlow=$new.StrongSlowSamples})
        }
    }
    $wide=[Gatefall.Tests.BasicReinforcementReview]::Play(7,$false);$oldWide=@((Get-Content (Join-Path $root 'basic-reinforcement-v0285.json') -Raw|ConvertFrom-Json).rows|Where-Object{$_.Seed -eq 7 -and -not $_.Control})[0]
    foreach($field in $oldWide.PSObject.Properties.Name){if(($oldWide.$field|ConvertTo-Json -Compress -Depth 7) -cne ($wide.$field|ConvertTo-Json -Compress -Depth 7)){throw ('Non-binding control differs '+$field)}}
    [ordered]@{source=$source;units=$units;rows=$rows;pairs=$pairs;wideControlEqual=$true;wideControl=$wide;candidateOnly=$true;productChanged=$false;
        scope='Prototype: only Binding projectiles on Armor>0 preserve displayed slow after existing Brute/Siege resistance. Other towers/unarmored enemies/damage/timing/prices unchanged. Ten selected control-policy candidates+one unchanged spear control. No new product/APK/native claim; needs wider validation before adoption.'}|ConvertTo-Json -Depth 12|Set-Content $output -Encoding utf8
}
$r=Get-Content $output -Raw|ConvertFrom-Json
$r.pairs|ForEach-Object{[pscustomobject]$_}|Format-Table file,seed,beforeState,afterState,beforeCore,afterCore -AutoSize
Write-Output ('Prototype armor-control samples: '+$r.units.Count+'; candidate battles: '+$r.rows.Count)
