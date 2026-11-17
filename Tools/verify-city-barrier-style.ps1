param([switch]$CheckIntegrationBaseline)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
& python -X utf8 (Join-Path $PSScriptRoot 'sync-city-barrier-style.py')
if($LASTEXITCODE -ne 0){throw 'Project style tokens and shared Unity constants differ'}
$styleResults=Join-Path $project 'TestResults/chapter2-design-20261107'
New-Item -ItemType Directory -Path $styleResults -Force | Out-Null
& python -X utf8 (Join-Path $PSScriptRoot 'validate-city-barrier-style.py') --json (Join-Path $styleResults 'token-contrast-validation.json')
if($LASTEXITCODE -ne 0){throw 'Project style token/contrast validation failed'}
$baseline=Join-Path $project 'Docs/Design/chapter2-design-20261107/core-baseline.json'
if($CheckIntegrationBaseline){
    if(-not(Test-Path -LiteralPath $baseline)){throw 'Original design integration baseline is missing'}
    $core=@(Get-Content -LiteralPath $baseline -Raw|ConvertFrom-Json)
    foreach($file in $core){$taskCorePath=Join-Path $project $file.relative;if((Get-FileHash -LiteralPath $taskCorePath -Algorithm SHA256).Hash.ToLowerInvariant() -cne $file.sha256){throw ('Design integration changed chapter2 core: '+$taskCorePath)}}
    Write-Output ('PASS: '+$core.Count+' combat/input/save core source hashes preserved.')
}
