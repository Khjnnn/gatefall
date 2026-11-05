param([switch]$UnobservedParity,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$taskProject=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $taskProject
$taskCopy=Join-Path $taskProject 'TestResults/refill-v0184-observer-core'
$taskObserver='global::Gatefall.Tests.RefillImpactObserver.Record(p.SourceId,Math.Min(e.Health,damage),e.Kind);'
if(-not $UnobservedParity) {
    New-Item -ItemType Directory -Path $taskCopy -Force|Out-Null
    foreach($taskFile in (Get-ChildItem Assets/Scripts/Core -Filter '*.cs')) {
        $taskOriginal=[IO.File]::ReadAllText($taskFile.FullName);$taskText=$taskOriginal
        if($taskFile.Name -eq 'DefenseSimulation.cs') {
            $taskNeedle='if(p.FromTower)Telemetry.TowerDamage[(int)p.Kind]+=Math.Min(e.Health,damage);'
            if(([regex]::Matches($taskOriginal,[regex]::Escape($taskNeedle))).Count -ne 1){throw 'Damage observation site ambiguous'}
            $taskText=$taskOriginal.Replace($taskNeedle,$taskNeedle+$taskObserver)
        }
        if($taskText.Replace($taskObserver,'') -cne $taskOriginal){throw 'Observer changes algorithm'}
        [IO.File]::WriteAllText((Join-Path $taskCopy $taskFile.Name),$taskText)
    }
}
$taskCore=if($UnobservedParity){Join-Path $taskProject 'Assets/Scripts/Core'}else{$taskCopy}
Add-Type -Path @((Get-ChildItem -LiteralPath $taskCore -Filter '*.cs').FullName + (Join-Path $taskProject 'Tests/FusionPositionReview.cs') + (Join-Path $taskProject 'Tests/BindingPositionReview.cs') + (Join-Path $taskProject 'Tests/RefillChoiceReview.cs'))
if($UnobservedParity) {
    [Gatefall.Tests.RefillChoiceReview]::Run(19,16,$true)|ConvertTo-Json -Depth 6|Set-Content TestResults/refill-v0184-unobserved-parity.json
    Write-Output 'Unobserved parity condition saved.'
    exit
}
if($UseRecorded) {
    $taskRaw=Get-Content TestResults/refill-v0184-observed-raw.json -Raw|ConvertFrom-Json
    $taskRows=$taskRaw.rows;$taskSupplement=$taskRaw.bindingSupplement
} else {
    $taskRows=[Gatefall.Tests.RefillChoiceReview]::SpearModels()
    $taskSupplement=[Gatefall.Tests.RefillChoiceReview]::Run(19,16,$false)
    [pscustomobject]@{rows=$taskRows;bindingSupplement=$taskSupplement}|ConvertTo-Json -Depth 7|Set-Content TestResults/refill-v0184-observed-raw.json
}
$taskRows=@($taskRows|ForEach-Object {$_|ConvertTo-Json -Depth 6|ConvertFrom-Json})
$taskBase=Get-Content TestResults/binding-position-v0183-models.json -Raw|ConvertFrom-Json
foreach($taskRow in $taskRows) {
    $taskM=$taskRow.Metrics;$taskOld=@($taskBase.rows|Where-Object {$_.Seed -eq $taskM.Seed -and $_.BindingSocket -eq $taskM.BindingSocket})[0]
    if($taskM.Spent -ne 935 -or $taskM.FinalCredits -ne 125 -or $taskM.Towers -ne 14 -or $taskM.CompletedPurchases -ne 21 -or $taskM.BindingShotsBeforeFusion -ne $taskOld.BindingShotsBeforeFusion -or $taskM.FirstFusion -ne $taskOld.FirstFusion -or ($taskM.Cards -join ',') -ne ($taskOld.Cards -join ',') -or $taskRow.LingeringBindingProjectilesAtRefill -ne 0 -or [math]::Abs($taskM.BindingDamage-$taskRow.InitialBindingDamageAtRefill) -gt .01){throw 'Refill isolation or budget differs'}
    $taskRow|Add-Member ReusedBindingBaseline $taskOld
    $taskRow|Add-Member ReconstructedBindingRefillDamage ($taskOld.BindingDamage-$taskM.BindingDamage)
    $taskExpected=@('build Autocannon','build Autocannon','build Autocannon','build Autocannon','build Autocannon','build Binding','build Rupture','build Rupture','upgrade Autocannon','upgrade Binding','fusion (Autocannon+Binding) -> Arc','build Autocannon','build Autocannon','build Autocannon','build Autocannon','build Autocannon','build Autocannon','build Autocannon','upgrade Arc','upgrade Rupture','upgrade Rupture')
    if($taskRow.Purchases.Count -ne $taskExpected.Count){throw 'Purchase count differs'}
    for($taskIndex=0;$taskIndex -lt $taskExpected.Count;$taskIndex++){if($taskRow.Purchases[$taskIndex] -notmatch ('^\d+\.\d '+[regex]::Escape($taskExpected[$taskIndex])+' @')){throw 'Purchase kind/order differs'}}
}
$taskSelected=@($taskRows|Where-Object {$_.Metrics.Seed -eq 19 -and $_.Metrics.BindingSocket -eq 16})[0]
if([math]::Abs($taskSelected.ReconstructedBindingRefillDamage-$taskSupplement.RefillDamage) -gt .02){throw 'Reconstructed damage not confirmed by observed binding ID'}
if([math]::Abs($taskSupplement.Metrics.BindingDamage-@($taskBase.rows|Where-Object {$_.Seed -eq 19 -and $_.BindingSocket -eq 16})[0].BindingDamage) -gt .02){throw 'Supplement does not match preserved baseline'}
[pscustomobject]@{created=(Get-Date).ToString('o');version='0.18.4';reusedBaselineFile='binding-position-v0183-models.json';baselineBatchRerun=$false;newSpearConditions=9;bindingIdMetricSupplementConditions=1;budgetCap=940;bindingSpend=940;spearSpend=935;bindingRemainder=120;spearRemainder=125;observerCall=$taskObserver;method='Only purchase11(78.9s) changes from Binding45 to Autocannon40; initial Binding and Arc fusion unchanged. ID-specific health reduction observed in exact core copy; no product change. Other8 binding refill damage reconstructed from identical initial binding phase; one measured binding ID confirms reconstruction. Slow metrics remain samples, not travel delay or human fun.';rows=$taskRows;bindingIdSupplement=$taskSupplement}|ConvertTo-Json -Depth 7|Set-Content TestResults/refill-choice-v0184-models.json
$taskRows|ForEach-Object {[pscustomobject]@{seed=$_.Metrics.Seed;slot=$_.Metrics.BindingSocket;state=$_.Metrics.State;core=$_.Metrics.Core;time=$_.Metrics.Time;bindingTime=$_.ReusedBindingBaseline.Time;newSpearShots=$_.RefillShots;newSpearDamage=$_.RefillDamage;bindingRefillDamage=$_.ReconstructedBindingRefillDamage;strongSlowDelta=$_.Metrics.MarginalStrongSlowFractionSeconds-$_.ReusedBindingBaseline.MarginalStrongSlowFractionSeconds}}|Format-Table
