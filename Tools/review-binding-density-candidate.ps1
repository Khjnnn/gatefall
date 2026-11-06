param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/hud-investment-20261106';$output="$root/binding-density-candidate.json"
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Recorded candidate exists; use -UseRecorded'}
    foreach($file in (Get-Content "$root/before-balance-hashes.json" -Raw|ConvertFrom-Json)){
        if((Get-FileHash (Join-Path 'Assets/Scripts/Core' $file.file)).Hash.ToLowerInvariant() -cne $file.sha256){throw 'Historical 0.28.9 candidate review requires its exact baseline; use -UseRecorded on the adopted product'}
    }
    $core="$root/binding-density-core";New-Item -ItemType Directory -Path $core -Force|Out-Null
    Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|Copy-Item -Destination $core
    $path="$core/TowerCatalog.cs";$original=Get-Content $path -Raw
    $before='S(3,.9f,4.8f,4,0,.45f,1.2f),S(5,.85f,5f,5,0,.50f,1.4f)'
    $after='S(3,.9f,4.8f,6,0,.45f,1.2f),S(5,.85f,5f,8,0,.50f,1.4f)'
    if($original.Split(@($before),[StringSplitOptions]::None).Count -ne 2){throw 'Expected exactly one BindingLv2/Lv3 target-count site'}
    $candidate=$original.Replace($before,$after);if($candidate.Replace($after,$before) -cne $original){throw 'Candidate exceeds two counts'}
    [IO.File]::WriteAllText((Join-Path $project $path),$candidate)
    Add-Type -Path @((Get-ChildItem $core -Filter '*.cs').FullName+@('Tests/AccountGrowthChecks.cs','Tests/EarlyCombatDigest.cs','Tests/BasicReinforcementReview.cs')) -CompilerOptions '/langversion:9'
    $old='TestResults/night-balance-20261105'
    $front=(Get-Content "$root/contextual-front.json" -Raw|ConvertFrom-Json).rows
    $basic=(Get-Content "$old/basic-reinforcement-v0285.json" -Raw|ConvertFrom-Json).rows
    $doha=(Get-Content "$old/reinforcement-hero-comparison-v0285.json" -Raw|ConvertFrom-Json).rows
    $control=[Gatefall.Tests.BasicReinforcementReview]::Play(7,$false,$false,$false,$false,0,0,7,$false,$true)
    $control|ConvertTo-Json -Depth 9|Set-Content "$root/binding-density-spear-control.json"
    $ref=$front|Where-Object{$_.Seed -eq 7 -and -not $_.Control -and -not $_.Crisis}
    foreach($f in @('State','End','Core','Spent','Credits','PrefixHash','Cards','Shots','Damage','Ledger')){if(($control.$f|ConvertTo-Json -Depth 7 -Compress) -cne ($ref.$f|ConvertTo-Json -Depth 7 -Compress)){throw "No-Binding control differs $f"}}
    $rows=[Collections.Generic.List[object]]::new();$references=[Collections.Generic.List[object]]::new()
    $specs=@(@{stage=7;crisis=$false;hero=0;motion=0;front=$true},@{stage=7;crisis=$true;hero=0;motion=0;front=$true},@{stage=11;crisis=$true;hero=0;motion=0;front=$false},@{stage=11;crisis=$true;hero=2;motion=2;front=$false})
    foreach($spec in $specs){foreach($seed in @(7,41)){
        $row=[Gatefall.Tests.BasicReinforcementReview]::Play($seed,$true,$false,$false,$false,$spec.hero,$spec.motion,$spec.stage,$spec.crisis,$spec.front)
        $ref=if($spec.stage -eq 7){$front|Where-Object{$_.Seed -eq $seed -and $_.Control -and $_.Crisis -eq $spec.crisis}}elseif($spec.hero -eq 0){$basic|Where-Object{$_.Seed -eq $seed -and $_.Control}}else{$doha|Where-Object{$_.Seed -eq $seed -and $_.Control -and $_.Motion -eq 2}}
        if(@($ref).Count -ne 1){throw 'Ambiguous baseline'}
        $rows.Add($row);$references.Add($ref)
        [ordered]@{rows=$rows;references=$references;control=$control;counts='BindingLv1 unchanged3;Lv2 4→6/Lv3 5→8 only';productUnchanged=$true;scope='Eight selected policies plus no-Binding equality control. Existing eight baselines reused. Same710 price/purchase rules;not an adopted patch or native result.'}|ConvertTo-Json -Depth 10|Set-Content $output
        if($row.PrefixHash -cne $ref.PrefixHash -or $row.Spent -ne 710 -or $row.CompletedOrders -ne $row.TotalOrders){throw 'Prefix/price/completion mismatch;raw preserved'}
    }}
}
$r=Get-Content $output -Raw|ConvertFrom-Json
@(for($i=0;$i -lt $r.rows.Count;$i++){$a=$r.references[$i];$b=$r.rows[$i];[pscustomobject]@{Stage=$b.Stage;Crisis=$b.Crisis;Hero=$b.Hunter;Seed=$b.Seed;Before=$a.State;After=$b.State;CoreBefore=$a.Core;CoreAfter=$b.Core;EndDelta=[Math]::Round($b.End-$a.End,2);ShotsBefore=$a.Shots[1];ShotsAfter=$b.Shots[1]}})|Format-Table -AutoSize
