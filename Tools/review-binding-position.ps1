$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
foreach($taskFile in (Get-ChildItem Assets/Scripts/Core -Filter '*.cs')) {
    if((Get-FileHash -LiteralPath $taskFile.FullName).Hash -ne (Get-FileHash -LiteralPath (Join-Path TestResults/fusion-v0182-baseline/Core $taskFile.Name)).Hash){throw 'Core differs from the reused baseline'}
}
Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/FusionPositionReview.cs') + (Join-Path $taskProject 'Tests/BindingPositionReview.cs'))
$taskRows=[Gatefall.Tests.BindingPositionReview]::Models()
if($taskRows.Count -ne 9 -or @($taskRows|Where-Object {$_.CompletedPurchases -ne 21 -or $_.Spent -ne 940 -or $_.Towers -ne 14 -or $_.State -notin @('Won','Lost')}).Count){throw 'Incomplete or different-budget comparison'}
$taskBaseline=@((Get-Content TestResults/fusion-position-v0182-models.json -Raw|ConvertFrom-Json).rows|Where-Object Anchor -eq 11)
if($taskBaseline.Count -ne 3 -or @($taskBaseline|Where-Object BindingShots -ne 0).Count){throw 'Reused baseline differs'}
[pscustomobject]@{created=(Get-Date).ToString('o');productVersion='0.18.3';baselineFile='fusion-position-v0182-models.json';baselineRerun=$false;baseline=$taskBaseline;purchaseFixtureSha256=(Get-FileHash Tests/FusionPositionReview.cs).Hash.ToLowerInvariant();method='Only Binding6 placement changes to unused9/23/16, including upgrade/fusion partner/rebuild; Arc11 and same21 purchases/card preference/552 schedule fixed. Slow metrics sample surviving enemies before each0.05s tick; marginal slow means excess over other sources after resistance, not exact travel delay or causal prevention. Not Android or human fun.';rows=$taskRows}|ConvertTo-Json -Depth 5|Set-Content TestResults/binding-position-v0183-models.json
$taskRows|Format-Table Seed,BindingSocket,BindingShots,BindingDamage,StrongSlowedDistinct,MarginalStrongSlowFractionSeconds,RunnersSlowedDistinct,ArcShots,Core,Time,MaxPurchaseDelay
