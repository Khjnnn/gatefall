$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskReport=Get-Content TestResults/refill-choice-v0184-models.json -Raw|ConvertFrom-Json
$taskObserved=@($taskReport.rows|Where-Object {$_.Metrics.Seed -eq 19 -and $_.Metrics.BindingSocket -eq 16})[0]
$taskUnobserved=Get-Content TestResults/refill-v0184-unobserved-parity.json -Raw|ConvertFrom-Json
if(($taskObserved.Metrics|ConvertTo-Json -Depth 4 -Compress) -cne ($taskUnobserved.Metrics|ConvertTo-Json -Depth 4 -Compress) -or ($taskObserved.Purchases -join '|') -cne ($taskUnobserved.Purchases -join '|') -or $taskObserved.RefillShots -ne $taskUnobserved.RefillShots){throw 'Observer affects simulation'}
$taskSourceAudit=@(foreach($taskFile in (Get-ChildItem Assets/Scripts/Core -Filter '*.cs')) {
    $taskOriginal=[IO.File]::ReadAllText($taskFile.FullName)
    $taskCopy=[IO.File]::ReadAllText((Join-Path TestResults/refill-v0184-observer-core $taskFile.Name))
    $taskEqual=$taskCopy.Replace($taskReport.observerCall,'') -ceq $taskOriginal
    if(-not $taskEqual){throw 'Analysis copy changed algorithm'}
    [pscustomobject]@{file=$taskFile.Name;sourceHash=(Get-FileHash -LiteralPath $taskFile.FullName).Hash.ToLowerInvariant();codeTextEqualAfterObserverRemoval=$taskEqual}
})
$taskPairs=@(foreach($taskRow in $taskReport.rows) {
    $taskM=$taskRow.Metrics;$taskB=$taskRow.ReusedBindingBaseline
    if($null -eq $taskB -or $taskM.BindingShotsBeforeFusion -ne $taskB.BindingShotsBeforeFusion -or $taskRow.LingeringBindingProjectilesAtRefill -ne 0){throw 'Baseline or initial phase mismatch'}
    [pscustomobject]@{seed=$taskM.Seed;socket=$taskM.BindingSocket;bindingSpent=$taskB.Spent;spearSpent=$taskM.Spent;bindingRemainder=$taskB.FinalCredits;spearRemainder=$taskM.FinalCredits;bindingCore=$taskB.Core;spearCore=$taskM.Core;bindingSeconds=$taskB.Time;spearSeconds=$taskM.Time;spearTimeDifference=$taskM.Time-$taskB.Time;bindingRefillShots=$taskB.BindingShotsAfterFusion;spearRefillShots=$taskRow.RefillShots;bindingRefillDamage=$taskRow.ReconstructedBindingRefillDamage;spearRefillDamage=$taskRow.RefillDamage;spearRefillStrongDamage=$taskRow.RefillStrongDamage;bindingStrongSlow=$taskB.MarginalStrongSlowFractionSeconds;spearStrongSlow=$taskM.MarginalStrongSlowFractionSeconds;bindingRunnerSlow=$taskB.MarginalRunnerSlowFractionSeconds;spearRunnerSlow=$taskM.MarginalRunnerSlowFractionSeconds;allSpawnedKills=$taskM.Kills;cardsEqual=(($taskM.Cards -join ',') -eq ($taskB.Cards -join ','));firstFusionEqual=($taskM.FirstFusion -eq $taskB.FirstFusion)}
})
if($taskPairs.Count -ne 9 -or @($taskPairs|Where-Object {$_.spearCore -ne 300 -or $_.bindingCore -ne 300 -or $_.allSpawnedKills -ne 552 -or $_.spearSpent -ne 935 -or $_.bindingSpent -ne 940 -or $_.spearRemainder -ne 125 -or $_.bindingRemainder -ne 120 -or -not $_.cardsEqual -or -not $_.firstFusionEqual}).Count){throw 'Control outcomes differ'}
if([math]::Abs($taskReport.bindingIdSupplement.RefillDamage-$taskObserved.ReconstructedBindingRefillDamage) -gt .02){throw 'Binding damage reconstruction differs'}
foreach($taskRow in (Get-Content TestResults/source-hashes-v0184.json -Raw|ConvertFrom-Json)) {if((Get-FileHash -LiteralPath $taskRow.path).Hash.ToLowerInvariant() -ne $taskRow.sha256){throw 'Product source changed'}}
$taskApk=Get-Content TestResults/android-apk-v0184.json -Raw|ConvertFrom-Json
foreach($taskFile in @('Builds/Android/Gatefall.apk','Builds/Android/Gatefall-v0.18.4.apk','TestResults/v0184-installed.apk')){if((Get-FileHash -LiteralPath $taskFile).Hash.ToLowerInvariant() -ne $taskApk.sha256){throw 'Verified APK changed'}}
[pscustomobject]@{created=(Get-Date).ToString('o');productVersion='0.18.4';apkSha256=$taskApk.sha256;newSpearConditions=9;reusedBindingConditions=9;observedBindingSupplementConditions=1;unobservedParityConditions=1;observerParityPassed=$true;productSourceAndApkUnchanged=$true;newApk=$false;newAvd=$false;codeSourceAudit=$taskSourceAudit;pairs=$taskPairs;scope='14 towers with same21 purchases except refill kind at78.9, cap940 vs actual940/935. No reinvestment. Damage reconstructed for8 reused binding rows and confirmed by one binding ID supplement; slow samples are not travel delay or human fun. Deterministic cases, not independent human trials.'}|ConvertTo-Json -Depth 6|Set-Content TestResults/refill-choice-v0184-comparison.json
Write-Output 'PASS refill isolation/preserved baseline/budget/observer parity and source audit/damage reconstruction/product source and APK unchanged. No new APK or AVD.'
