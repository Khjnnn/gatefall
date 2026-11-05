param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskOutput='TestResults/stage4-fusion-front-v0210-raw.json'
if($UseRecorded){Get-Content $taskOutput -Raw;exit}
if(Test-Path $taskOutput){throw 'Preserve recorded models; use -UseRecorded'}
foreach($taskR in (Get-Content TestResults/source-hashes-v0210.json -Raw|ConvertFrom-Json)){
    if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed before analysis'}
}
$taskRoot=Join-Path (Get-Location) 'TestResults/stage4-fusion-front-v0210-review'
New-Item -ItemType Directory -Path $taskRoot -Force|Out-Null
foreach($taskFile in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $taskFile.FullName -Destination $taskRoot}
Copy-Item TestResults/alley-v0192-review/AlleyDensityReview.cs $taskRoot
Copy-Item Tests/Stage4GrowthChoiceReview.cs $taskRoot
Add-Type -Path (Get-ChildItem $taskRoot -Filter '*.cs').FullName
$taskBaseline=@((Get-Content TestResults/stage4-growth-v0210-raw.json -Raw|ConvertFrom-Json).models|Where-Object Policy -eq fusion)
$taskControl=[Gatefall.Tests.Stage4GrowthChoiceReview]::Control()
$taskControl|ConvertTo-Json -Depth 8|Set-Content TestResults/stage4-fusion-front-v0210-control.json
$taskOld=@($taskBaseline|Where-Object Seed -eq 7)[0]
foreach($taskField in @('Seed','Spent','Credits','Towers','Kills','Spawned','Remaining','CompletedOrders','Fusions','PeakEnemies','FiredSites','FrequentSites','PeakRecent3','CoreDamageEvents','HunterDowns','State','AllSpawned')){
    if([string]$taskControl.$taskField -cne [string]$taskOld.$taskField){throw "Control differs: $taskField"}
}
foreach($taskField in @('Core','PlayerHealth','Time','MaxPurchaseDelay')){
    if([single]$taskControl.$taskField -ne [single]$taskOld.$taskField){throw "Control float differs: $taskField"}
}
foreach($taskField in @('KindDamage','RequestedCoreDamageByLane')){
    for($taskI=0;$taskI -lt $taskControl.$taskField.Length;$taskI++){
        if([single]$taskControl.$taskField[$taskI] -ne [single]$taskOld.$taskField[$taskI]){throw "Control float array differs: $taskField"}
    }
}
foreach($taskField in @('InitialSites','SiteShots','KindShots','Cards','PurchaseLog')){
    if(($taskControl.$taskField -join '|') -cne ($taskOld.$taskField -join '|')){throw "Control array differs: $taskField"}
}
$taskRows=[Gatefall.Tests.Stage4GrowthChoiceReview]::EasternFusionModels()
foreach($taskRow in $taskRows){
    $taskOld=@($taskBaseline|Where-Object Seed -eq $taskRow.Seed)[0]
    if($taskRow.Spent -ne 395 -or $taskRow.Towers -ne 6 -or $taskRow.CompletedOrders -ne 4 -or $taskRow.Fusions -ne 1 -or
        ($taskRow.InitialSites -join ',') -ne ($taskOld.InitialSites -join ',') -or ($taskRow.PurchaseLog[0..5] -join '|') -cne ($taskOld.PurchaseLog[0..5] -join '|') -or
        ($taskRow.Cards -join ',') -cne ($taskOld.Cards -join ',')){throw 'Candidate fixture/card/budget differs'}
}
[ordered]@{created=(Get-Date -Format o);task=232;newControl=1;controlMatched=$true;baselineModelsReused=3;newCandidate=3;actualSpend=395;method='Initial6 kinds/sites unchanged; upgrade/use easternSpear14 instead of westernSpear12 with sameBinding19, fuse at65 and refillBinding19 at80 unchanged. Pre-fusion15s upgrade location also changes; not pure fusion-placement causality.';nativeAndroid=$false;productChanged=$false;control=$taskControl;models=$taskRows}|ConvertTo-Json -Depth 9|Set-Content $taskOutput
$taskRows|Select-Object Seed,State,Core,Time,Spent,Credits,PeakEnemies,CoreDamageEvents,HunterDowns,@{n='ArcShots';e={$_.KindShots[3]}},@{n='ArcDamage';e={$_.KindDamage[3]}}|Format-Table -AutoSize
