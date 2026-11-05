# Reuse recorded late references; creates12normal+2crisis selected current models unless -UseRecorded.
param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
function Require($value,[string]$message){if(-not $value){throw $message}}
if($UseRecorded){
    $record=Get-Content "$root/target-query-equivalence-v0287.json" -Raw|ConvertFrom-Json
    foreach($hash in (Get-Content "$root/core-delta-v0287.json" -Raw|ConvertFrom-Json)){Require ((Get-FileHash ('Assets/Scripts/Core/'+$hash.file)).Hash.ToLowerInvariant() -ceq $hash.after) 'Recorded current core changed'}
    Require ($record.early26 -and $record.normalEqual -and $record.crisisEqual) 'Recorded equivalence incomplete'
    'PASS recorded26early/12normal/2crisis equality;no new models';return
}
$early=Get-Content TestResults/target-query-v0287-early-comparison.json -Raw|ConvertFrom-Json
Require ($early.equal -and $early.cases -eq 26) 'Early frame digests incomplete'
$delta=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;before=(Get-FileHash (Join-Path "$root/target-query-before-core" $_.Name)).Hash.ToLowerInvariant();after=(Get-FileHash $_.FullName).Hash.ToLowerInvariant();same=(Get-FileHash (Join-Path "$root/target-query-before-core" $_.Name)).Hash -ceq (Get-FileHash $_.FullName).Hash}})
Require (@($delta|Where-Object{-not $_.same}).Count -eq 1 -and @($delta|Where-Object{-not $_.same})[0].file -ceq 'DefenseSimulation.cs') 'Unexpected core delta'
$delta|ConvertTo-Json|Set-Content "$root/core-delta-v0287.json"
$candidate=[IO.File]::ReadAllText((Join-Path (Get-Location) "$root/target-query-candidate-core/DefenseSimulation.cs")).Replace("`r`n","`n")
$product=[IO.File]::ReadAllText((Join-Path (Get-Location) 'Assets/Scripts/Core/DefenseSimulation.cs')).Replace("`r`n","`n")
Require ($product -ceq $candidate) 'Product differs from measured candidate'
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+@('Tests/CompetentPlanReview.cs','Tests/AccountGrowthChecks.cs','Tests/ProgressionBalanceReview.cs','Tests/CrisisCurveReview.cs')) -CompilerOptions '/langversion:9'
$normal=[Gatefall.Tests.ProgressionBalanceReview]::Run([int[]]@(13),[int[]]@(0),[string[]]@('recommended'),[int[]]@(1080),0,$false)
$crisis=@([Gatefall.Tests.CrisisCurveReview]::Play(11,7,'below',720,0),[Gatefall.Tests.CrisisCurveReview]::Play(13,41,'below',720,3))
[ordered]@{normal=$normal;crisis=$crisis}|ConvertTo-Json -Depth 10|Set-Content "$root/target-query-late-raw-v0287.json" -Encoding utf8
$normalBefore=(Get-Content TestResults/growth-role-final.json -Raw|ConvertFrom-Json).rows
foreach($row in $normal){
    $old=@($normalBefore|Where-Object{$_.Stage -eq 14 -and $_.Seed -eq $row.Seed -and $_.Profile -ceq 'recommended' -and $_.Budget -eq 1080 -and $_.Policy -ceq $row.Policy})
    Require ($old.Count -eq 1) 'Recorded normal reference missing'
    foreach($property in $old[0].PSObject.Properties.Name){Require (($old[0].$property|ConvertTo-Json -Depth 8 -Compress) -ceq ($row.$property|ConvertTo-Json -Depth 8 -Compress)) ('Normal reference differs:'+ $property)}
}
$c12=Get-Content "$root/research-hint-equivalence-v0285.json" -Raw|ConvertFrom-Json
$c14=Get-Content "$root/crisis-curve-v0283.json" -Raw|ConvertFrom-Json
$before=@($c12.late,@($c14.cases|Where-Object{$_.Outcome.Stage -eq 14 -and $_.Outcome.Seed -eq 41 -and $_.Outcome.Profile -ceq 'below' -and $_.Outcome.Budget -eq 720 -and $_.PolicyIndex -eq 3})[0])
for($i=0;$i -lt 2;$i++){
    foreach($property in $before[$i].PSObject.Properties.Name){Require (($before[$i].$property|ConvertTo-Json -Depth 9 -Compress) -ceq ($crisis[$i].$property|ConvertTo-Json -Depth 9 -Compress)) ('Crisis reference differs:'+ $i+':'+$property)}
}
[ordered]@{early26=$true;normal14Cases=12;normalEqual=$true;crisisCases=2;crisisEqual=$true;candidateEqualsProduct=$true;changedCore='DefenseSimulation.cs';scope='26early/lab1/60 public-frame digests plus12normal capstone and2selected crisis full recorded outcome/card/ledger/participation/damage fields. No human or Android frame-performance claim.'}|ConvertTo-Json|Set-Content "$root/target-query-equivalence-v0287.json" -Encoding utf8
'PASS26early traces/12normal+2crisis selected reference equality;one comparator cache change only'
