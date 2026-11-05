param([ValidateSet(0,1,2)][int[]]$Hunters=@(1,2),[ValidateSet(7,19,41)][int[]]$Seeds=@(7,41),[ValidateSet(12,14)][int[]]$Stages=@(12,14),[ValidateSet('below','recommended')][string[]]$Profiles=@('below','recommended'),[ValidateSet(0,3)][int[]]$Policies=@(0,3),[ValidateRange(0,2147483647)][int]$Budget=720,[ValidateSet(0,1,2)][int]$Motion=0,
    [ValidatePattern('^[a-zA-Z0-9_-]+$')][string]$Tag='hero-idle-v0284',[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$output=Join-Path $root ($Tag+'.json')
if(-not $UseRecorded){
    if(Test-Path -LiteralPath $output){throw 'Recorded tag exists; use -UseRecorded or a new tag instead of repeating cases'}
    foreach($selection in @($Hunters,$Seeds,$Stages,$Profiles,$Policies)){
        if($selection.Count -eq 0 -or @($selection|Select-Object -Unique).Count -ne $selection.Count){throw 'Use nonempty unique comparison selections'}
    }
    if(@($Hunters|Where-Object{$_ -lt 0 -or $_ -gt 2}).Count -or @($Stages|Where-Object{$_ -notin @(12,14)}).Count -or $Motion -notin @(0,1,2)){throw 'Unknown selected hero/stage/motion'}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+@('Tests/CompetentPlanReview.cs','Tests/AccountGrowthChecks.cs','Tests/ProgressionBalanceReview.cs','Tests/CrisisCurveReview.cs')|ForEach-Object{(Resolve-Path -LiteralPath $_).Path})
    $harness=@('Tests/CompetentPlanReview.cs','Tests/CrisisCurveReview.cs'|ForEach-Object{[ordered]@{file=$_;sha256=(Get-FileHash $_).Hash.ToLowerInvariant()}})
    $cachedPath=Join-Path $root 'hero-idle-v0284.json';$cached=$null
    if(Test-Path -LiteralPath $cachedPath){
        $cached=Get-Content -LiteralPath $cachedPath -Raw|ConvertFrom-Json
        foreach($hash in $cached.source){if((Get-FileHash (Join-Path 'Assets/Scripts/Core' $hash.file)).Hash.ToLowerInvariant() -cne $hash.sha256){throw 'Cached control core changed'}}
        if(($cached.harness|ConvertTo-Json -Compress) -cne ($harness|ConvertTo-Json -Compress)){throw 'Cached control harness changed or is missing source hashes'}
    }
    $control=if($null -ne $cached){$cached.selectedControl}else{[Gatefall.Tests.CrisisCurveReview]::Play(11,7,'below',720,0)}
    $prior=@((Get-Content TestResults/night-crisis-middle-budgets.json -Raw|ConvertFrom-Json).rows|Where-Object{$_.Stage -eq 12 -and $_.Seed -eq 7 -and $_.Profile -eq 'below' -and $_.Budget -eq 720 -and $_.Policy -eq $control.Outcome.Policy})[0]
    foreach($property in $prior.PSObject.Properties.Name){if(($prior.$property|ConvertTo-Json -Compress -Depth 7) -cne ($control.Outcome.$property|ConvertTo-Json -Compress -Depth 7)){throw ('Default Kang fixture changed: '+$property)}}
    $cases=[Collections.Generic.List[object]]::new()
    foreach($stage in $Stages){foreach($hunter in $Hunters){foreach($seed in $Seeds){foreach($profile in $Profiles){foreach($policy in $Policies){
        $cases.Add([Gatefall.Tests.CrisisCurveReview]::Play($stage-1,$seed,$profile,$Budget,$policy,$hunter,$Motion))
    }}}}}
    [ordered]@{source=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}});
        defaultControlEquivalent=$true;selectedControl=$control;controlReused=($null -ne $cached);harness=$harness;
        hunters=$Hunters;seeds=$Seeds;stages=$Stages;profiles=$Profiles;policies=$Policies;budget=$Budget;motion=$Motion;cases=$cases;
        scope='Synthetic affordable recommended/below accounts, same tower buying rules and spending ceiling; first-card preference. Motion0 idle;1 follows closest-to-core enemy;2 same rule with skill when enemy within3. No support ability. Skills can change movement/target selection and actual purchases. Down time sampled at1/30, travel excludes revive jumps. No human fun, physical-device or universal hero-power verdict.'
    }|ConvertTo-Json -Depth 10|Set-Content -LiteralPath $output -Encoding utf8
}
$report=Get-Content -LiteralPath $output -Raw|ConvertFrom-Json
$report.cases|Group-Object {$_.Outcome.Stage.ToString()+','+$_.Hunter+','+$_.Outcome.Profile}|ForEach-Object{$g=$_.Group;[pscustomobject]@{Case=$_.Name;Runs=$g.Count;Wins=@($g|Where-Object{$_.Outcome.State -eq 'Won'}).Count;
    Three=@($g|Where-Object{$_.Outcome.Stars -eq 3}).Count;MinCore=($g.Outcome.Core|Measure-Object -Minimum).Minimum;Downs=($g.HunterDowns|Measure-Object -Sum).Sum;Skills=($g.SkillUses|Measure-Object -Sum).Sum}}|Format-Table -AutoSize
Write-Output ('Recorded hero cases: '+@($report.cases).Count+'; '+$output)
