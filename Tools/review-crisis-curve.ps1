param([int[]]$Seeds=@(7,41),[switch]$Append,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$output=Join-Path $root 'crisis-curve-v0283.json'
if(-not $UseRecorded){
    if(@($Seeds|Where-Object{$_ -notin @(7,19,41)}).Count -or @($Seeds|Select-Object -Unique).Count -ne $Seeds.Count){throw 'Use unique recorded comparison seeds'}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+@('Tests/CompetentPlanReview.cs','Tests/AccountGrowthChecks.cs','Tests/ProgressionBalanceReview.cs','Tests/CrisisCurveReview.cs')|ForEach-Object{(Resolve-Path -LiteralPath $_).Path})
    $baseline=Get-Content (Join-Path $project 'TestResults/night-crisis-middle-budgets.json') -Raw|ConvertFrom-Json
    $control=[Gatefall.Tests.CrisisCurveReview]::Play(11,7,'below',720,0)
    $old=@($baseline.rows|Where-Object{$_.Stage -eq 12 -and $_.Seed -eq 7 -and $_.Profile -eq 'below' -and $_.Budget -eq 720 -and $_.Policy -eq $control.Outcome.Policy})
    if($old.Count -ne 1){throw 'Missing existing selected control'}
    foreach($property in $old[0].PSObject.Properties.Name){
        if(($old[0].$property|ConvertTo-Json -Compress -Depth 6) -cne ($control.Outcome.$property|ConvertTo-Json -Compress -Depth 6)){throw ('Existing planner control differs: '+$property)}
    }
    $cases=[Collections.Generic.List[object]]::new()
    if($Append){foreach($case in (Get-Content -LiteralPath $output -Raw|ConvertFrom-Json).cases){$cases.Add($case)}}
    foreach($stage in @(12,13)){foreach($seed in $Seeds){foreach($profile in @('below','recommended')){foreach($budget in @(720,900)){foreach($policy in @(0,3)){
        if(@($cases|Where-Object{$_.Outcome.Stage -eq $stage+1 -and $_.Outcome.Seed -eq $seed -and $_.Outcome.Profile -eq $profile -and $_.Outcome.Budget -eq $budget -and $_.PolicyIndex -eq $policy}).Count){throw 'Selected case already exists; do not repeat it'}
        $cases.Add([Gatefall.Tests.CrisisCurveReview]::Play($stage,$seed,$profile,$budget,$policy))
    }}}}}
    [ordered]@{version='0.28.3';source=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}});
        selectedControl=$control;controlEquivalent=$true;cases=$cases;
        scope='Crisis1-13/14, below/recommended accounts,720/900 spending ceilings; idle hunter, first-card preference, spear/area-fusion plans. Same rules and ceilings do not mean equal actual purchases/positions/times/cards. No product tuning or new Android run.'
    }|ConvertTo-Json -Depth 10|Set-Content -LiteralPath $output -Encoding utf8
}
$report=Get-Content -LiteralPath $output -Raw|ConvertFrom-Json
$report.cases.Outcome|Group-Object Stage,Profile,Budget|ForEach-Object{$g=$_.Group;[pscustomobject]@{Case=$_.Name;Runs=$g.Count;Wins=@($g|Where-Object State -eq Won).Count;Three=@($g|Where-Object Stars -eq 3).Count;
    MinCore=($g.Core|Measure-Object -Minimum).Minimum;SpentMin=($g.Spent|Measure-Object -Minimum).Minimum;SpentMax=($g.Spent|Measure-Object -Maximum).Maximum}}|Format-Table -AutoSize
Write-Output ('Recorded curve cases: '+@($report.cases).Count+'; '+$output)
