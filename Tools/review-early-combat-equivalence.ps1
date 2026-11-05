param([Parameter(Mandatory=$true)][string]$BeforeCore,[ValidatePattern('^[a-zA-Z0-9_-]+$')][string]$Tag='early-equivalence')
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location $project
New-Item -ItemType Directory -Path (Join-Path $project 'TestResults') -Force|Out-Null
$baseline=(Resolve-Path -LiteralPath $BeforeCore).Path
$powershell=(Get-Process -Id $PID).Path
$runner=Join-Path $project ('TestResults/'+$Tag+'-run.ps1')
$source=@'
param([string]$Core,[string]$Checks,[string]$Output)
$ErrorActionPreference='Stop'
Add-Type -Path @((Get-ChildItem -LiteralPath $Core -Filter '*.cs').FullName+$Checks)
[Gatefall.Tests.EarlyCombatDigest]::Run()|ConvertTo-Json -Depth 5|Set-Content -LiteralPath $Output
'@
[IO.File]::WriteAllText($runner,$source,[Text.UTF8Encoding]::new($false))
$outputs=@()
foreach($pair in @(@('before',$baseline),@('after',(Join-Path $project 'Assets/Scripts/Core')))){
    $output=Join-Path $project ('TestResults/'+$Tag+'-'+$pair[0]+'.json')
    & $powershell -NoProfile -File $runner -Core $pair[1] -Checks (Join-Path $project 'Tests/EarlyCombatDigest.cs') -Output $output
    if($LASTEXITCODE){throw "Digest failed for $($pair[0])"}
    $outputs+=,@(Get-Content -LiteralPath $output -Raw|ConvertFrom-Json)
}
if($outputs[0].Count -ne 26 -or $outputs[1].Count -ne 26){throw 'Early comparison cases incomplete'}
for($i=0;$i -lt 26;$i++){
    if(($outputs[0][$i]|ConvertTo-Json -Compress) -cne ($outputs[1][$i]|ConvertTo-Json -Compress)){throw "Early public combat state changed at case $i"}
}
[ordered]@{beforeCore=$baseline;cases=26;fixedStep='1/60';seeds=@(7,41);scope='Lab plus normal/crisis1-1~1-6; deterministic random actions, movement, sales/fusions/calls/cards. Public actor, projectile, event, resource and telemetry fields. New early armor separately tested0. Not private PRNG/internal-state serialization.';equal=$true}|ConvertTo-Json|Set-Content ('TestResults/'+$Tag+'-comparison.json')
'PASS26 early/lab normal/crisis traces identical at1/60 public-state digests'
