$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
& python -X utf8 (Join-Path $PSScriptRoot 'sync-city-barrier-style.py')
if($LASTEXITCODE -ne 0){throw 'Project style tokens and shared Unity constants differ'}
$styleResults=Join-Path $project 'TestResults/city-barrier-ui-20261106'
New-Item -ItemType Directory -Path $styleResults -Force | Out-Null
& python -X utf8 (Join-Path $PSScriptRoot 'validate-city-barrier-style.py') --json (Join-Path $styleResults 'token-contrast-validation.json')
if($LASTEXITCODE -ne 0){throw 'Project style token/contrast validation failed'}
$baseline=Join-Path $project 'TestResults/city-barrier-ui-20261106/baseline-hashes.json'
if(Test-Path -LiteralPath $baseline){
    $core=@(Get-Content -LiteralPath $baseline -Raw|ConvertFrom-Json|Where-Object{$_.path -match 'Scripts[\\/]Core[\\/]'})
    foreach($file in $core){if((Get-FileHash -LiteralPath $file.path -Algorithm SHA256).Hash.ToLowerInvariant() -cne $file.sha256){throw ('UI-only scope changed core: '+$file.path)}}
    Write-Output ('PASS: '+$core.Count+' combat/input/save core source hashes preserved.')
}
