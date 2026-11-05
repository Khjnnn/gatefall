$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskRoot=Join-Path (Get-Location) 'TestResults/stage12-packets-v0202-review'
if(Test-Path -LiteralPath $taskRoot){throw 'Preserve packet candidate snapshots'}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0202.json -Raw|ConvertFrom-Json)){if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product differs'}}
New-Item -ItemType Directory -Path $taskRoot|Out-Null
$taskOriginal=[IO.File]::ReadAllText((Join-Path (Get-Location) 'Assets/Scripts/Core/RaonCombat.cs')).Replace("`r`n","`n")
$taskBefore='                    double offset=j*multiplier*step;'
if(([regex]::Matches($taskOriginal,[regex]::Escape($taskBefore))).Count -ne 1){throw 'Packet timing insertion ambiguous'}
foreach($taskSize in @(2,3)){
    $taskDir=Join-Path $taskRoot "pack$taskSize"
    New-Item -ItemType Directory -Path $taskDir|Out-Null
    foreach($taskCore in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $taskCore.FullName -Destination (Join-Path $taskDir $taskCore.Name)}
    $taskAfter=@"
                    int formationRank=j;
                    if(stage.Index==11 && !Rules.Crisis && wave>=3 && j>=2) {
                        int half=(formations-2)/2, full=half/$taskSize*$taskSize, n=(j-2)/2, side=(j-2)%2;
                        formationRank=n<full ? 2+(n/$taskSize)*2*$taskSize+side*$taskSize+n%$taskSize : 2+2*full+side*(half-full)+n-full;
                    }
                    double offset=formationRank*multiplier*step;
"@
    $taskChanged=$taskOriginal.Replace($taskBefore,$taskAfter)
    if($taskChanged.Replace($taskAfter,$taskBefore) -cne $taskOriginal){throw 'Candidate changes more than timing rank'}
    [IO.File]::WriteAllText((Join-Path $taskDir 'RaonCombat.cs'),$taskChanged)
}
foreach($taskName in @('MarkerBattleReview.cs','StageThreatObservation.cs')){Copy-Item -LiteralPath (Join-Path (Get-Location) ('TestResults/stage12-speed-v0202-review/'+$taskName)) -Destination (Join-Path $taskRoot $taskName)}
'Prepared two isolated lane-packet timing candidates; same read-only observer and product unchanged.'
