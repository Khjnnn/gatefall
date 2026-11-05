param([ValidateSet('baseline','pack10','pack12','pack16','burstFront','burstMiddle')][string]$Pattern='baseline',[switch]$Support)
$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem -LiteralPath (Join-Path $taskProject 'Assets/Scripts/Core') -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/Stage8RoleReview.cs'))
$taskReview = if($Support){[Gatefall.Tests.Stage8RoleReview]::RunSupportAll($Pattern)}else{[Gatefall.Tests.Stage8RoleReview]::RunAll($Pattern)}
$taskPath = Join-Path $taskProject ('TestResults/stage8-role-v0113-' + $Pattern + $(if($Support){'-support'}else{''}) + '.tsv')
$taskReview | Set-Content -LiteralPath $taskPath -Encoding utf8
Write-Output ('Stage 1-8 model report: ' + $taskPath + '. Not human or Android play.')
