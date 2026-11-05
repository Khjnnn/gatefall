param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOutput='TestResults/stage4-growth-v0210-raw.json'
if($UseRecorded){Get-Content $taskOutput -Raw;exit}
if(Test-Path $taskOutput){throw 'Preserve recorded models; use -UseRecorded'}
foreach($taskR in (Get-Content TestResults/source-hashes-v0210.json -Raw|ConvertFrom-Json)){
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed before analysis'}
}
$taskRoot=Join-Path (Get-Location) 'TestResults/stage4-growth-v0210-review'
New-Item -ItemType Directory -Path $taskRoot -Force|Out-Null
foreach($taskFile in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $taskFile.FullName -Destination $taskRoot}
Copy-Item TestResults/alley-v0192-review/AlleyDensityReview.cs $taskRoot
Copy-Item Tests/Stage4GrowthChoiceReview.cs $taskRoot
Add-Type -Path (Get-ChildItem $taskRoot -Filter '*.cs').FullName
$taskRows=[Gatefall.Tests.Stage4GrowthChoiceReview]::Models()
if($taskRows.Count -ne 9){throw 'Expected9 models'}
foreach($taskGroup in ($taskRows|Group-Object Seed)){
    if(@($taskGroup.Group|Where-Object Spent -ne 395).Count){throw 'Unequal actual spend requires a separate analysis'}
    $taskInitials=@($taskGroup.Group|ForEach-Object {$_.InitialSites -join ','}|Select-Object -Unique)
    if($taskInitials.Count -ne 1){throw 'Initial sites changed'}
    $taskPrefixes=@($taskGroup.Group|ForEach-Object {$_.PurchaseLog[0..5] -join '|'}|Select-Object -Unique)
    if($taskPrefixes.Count -ne 1){throw 'Initial purchase prefix changed'}
}
[ordered]@{created=(Get-Date -Format o);task=231;newModels=9;stage=4;scheduled=768;initialSpend=245;spendCeiling=395;initialTowers=6;firstOfferCards=$true;manualSkills=$false;movement=$false;support=$false;calls=$false;productChanged=$false;nativeAndroid=$false;method='Same initial5Spear+Binding6 sites/245; expansion3Spear+1upgrade,5Spear upgrades,or2material upgrades+Arc+Spear refill; actual150 additional each; target times differ by policy and resource delays retained; no refunds/free credits';models=$taskRows}|ConvertTo-Json -Depth 9|Set-Content $taskOutput
$taskRows|Select-Object Seed,Policy,State,Core,Time,Towers,Spent,Credits,FiredSites,PeakRecent3,CoreDamageEvents,HunterDowns,MaxPurchaseDelay|Format-Table -AutoSize
