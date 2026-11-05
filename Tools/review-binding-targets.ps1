param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$output=Join-Path $root 'binding-target-candidate-v0285.json'
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Use -UseRecorded; do not repeat the completed pilot'}
    $files=Get-ChildItem Assets/Scripts/Core -Filter '*.cs'
    $source=@($files|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}})
    $candidateRoot=Join-Path $root 'binding-target-candidate-core';New-Item -ItemType Directory $candidateRoot -Force|Out-Null
    foreach($file in $files){Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $candidateRoot $file.Name)}
    $filePath=Join-Path $candidateRoot 'DefenseSimulation.cs';$text=Get-Content $filePath -Raw
    $before='if(tower.Kind==TowerKind.Binding)targets.Sort((a,b)=> { int d=a.SlowFraction.CompareTo(b.SlowFraction); return d!=0 ? d : Lead(a,b); });'
    $after='if(tower.Kind==TowerKind.Binding)targets.Sort((a,b)=> { int d=a.SlowFraction.CompareTo(b.SlowFraction); if(d!=0)return d; int ar=!a.Swarm && (a.Kind==EnemyKind.Brute || a.Kind==EnemyKind.Siege || a.Kind==EnemyKind.Boss) ? 0 : a.Kind==EnemyKind.Runner ? 1 : 2; int br=!b.Swarm && (b.Kind==EnemyKind.Brute || b.Kind==EnemyKind.Siege || b.Kind==EnemyKind.Boss) ? 0 : b.Kind==EnemyKind.Runner ? 1 : 2; d=ar.CompareTo(br); return d!=0 ? d : Lead(a,b); });'
    if($text.IndexOf($before) -lt 0 -or $text.IndexOf($before) -ne $text.LastIndexOf($before)){throw 'Binding target anchor differs'}
    $candidateSource=$text.Replace($before,$after);if($candidateSource.Replace($after,$before) -cne $text){throw 'Target delta exceeds intended tie-break'}
    [IO.File]::WriteAllText($filePath,$candidateSource,[Text.UTF8Encoding]::new($false))
    Add-Type -Path @((Get-ChildItem $candidateRoot -Filter '*.cs').FullName+@('Tests/AccountGrowthChecks.cs','Tests/EarlyCombatDigest.cs','Tests/BasicReinforcementReview.cs')|ForEach-Object{(Resolve-Path $_).Path})
    $rows=[Collections.Generic.List[object]]::new();$pairs=[Collections.Generic.List[object]]::new();$wideReused=0
    foreach($variant in @(0,1,2)){
        $mixed=$variant -gt 0;$front=$variant -eq 2
        $name=if($front){'basic-reinforcement-front-v0285.json'}elseif($mixed){'basic-reinforcement-mixed-v0285.json'}else{'basic-reinforcement-v0285.json'}
        $baseline=Get-Content (Join-Path $root $name) -Raw|ConvertFrom-Json
        foreach($hash in $baseline.source){if((Get-FileHash (Join-Path 'Assets/Scripts/Core' $hash.file)).Hash.ToLowerInvariant() -cne $hash.sha256){throw 'Recorded baseline core changed'}}
        foreach($seed in @(7,41)){
            $old=@($baseline.rows|Where-Object{$_.Seed -eq $seed -and $_.Control})[0]
            $new=[Gatefall.Tests.BasicReinforcementReview]::Play($seed,$true,$mixed,$front);$rows.Add($new)
            [ordered]@{source=$source;rows=$rows;scope='Raw candidate rows saved before comparison guards'}|ConvertTo-Json -Depth 10|Set-Content (Join-Path $root 'binding-target-candidate-raw-v0285.json') -Encoding utf8
            if($old.PrefixHash -cne $new.PrefixHash -or $old.ChoiceAt -ne $new.ChoiceAt -or $old.Spent -ne $new.Spent -or ($old.Cards -join ',') -cne ($new.Cards -join ',')){throw 'Target-only comparison changed prefix/card/spend'}
            $pairs.Add([ordered]@{variant=$variant;seed=$seed;beforeState=$old.State;afterState=$new.State;beforeCore=$old.Core;afterCore=$new.Core;beforeEnd=$old.End;afterEnd=$new.End;beforeSlow=$old.StrongSlowSamples;afterSlow=$new.StrongSlowSamples;beforeBindingDamage=$old.Damage[1];afterBindingDamage=$new.Damage[1]})
            $wideReused++
        }
    }
    $control=[Gatefall.Tests.BasicReinforcementReview]::Play(7,$false)
    $oldControl=@((Get-Content (Join-Path $root 'basic-reinforcement-v0285.json') -Raw|ConvertFrom-Json).rows|Where-Object{$_.Seed -eq 7 -and -not $_.Control})[0]
    foreach($property in $oldControl.PSObject.Properties.Name){if(($oldControl.$property|ConvertTo-Json -Compress -Depth 6) -cne ($control.$property|ConvertTo-Json -Compress -Depth 6)){throw ('Unchanged spear control differs '+$property)}}
    [ordered]@{source=$source;rows=$rows;pairs=$pairs;wideBaselineReused=$wideReused;selectedWideControlEqual=$true;selectedWideControl=$control;productChanged=$false;
        scope='Test-only Binding slow-fraction tie-break: un-swarming Brute/Siege/Boss, then Runner, then other; retain least-slowed as primary key and original Lead/ID afterward. Six control-policy pilots, six compatible wide results reused plus one wide equality control. No damage/speed/price/attack timing changes, no new product or native run. Slow coefficient samples are not prevented damage or exact delay.'}|ConvertTo-Json -Depth 12|Set-Content $output -Encoding utf8
}
$r=Get-Content $output -Raw|ConvertFrom-Json
$r.pairs|ForEach-Object{[pscustomobject]$_}|Format-Table variant,seed,beforeState,afterState,beforeCore,afterCore,beforeSlow,afterSlow -AutoSize
'Recorded test-only Binding target pilot; no product tuning is implied.'
