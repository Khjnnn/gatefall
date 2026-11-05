param([switch]$UseRecorded,[switch]$Seed19)
$ErrorActionPreference='Stop'
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$taskTag=if($Seed19){'seed19'}else{'pilot'}
$taskOutput="TestResults/stage14-marker-v0210-$taskTag.json"
if($UseRecorded){Get-Content $taskOutput -Raw;exit}
if(Test-Path $taskOutput){throw 'Preserve recorded models; use -UseRecorded'}
foreach($taskR in(Get-Content TestResults/source-hashes-v0210.json -Raw|ConvertFrom-Json)){if((Get-FileHash $taskR.path).Hash -ne $taskR.sha256){throw 'Product changed before analysis'}}
$taskRoot=Join-Path (Get-Location) ('TestResults/stage14-marker-v0210-review/'+$taskTag)
New-Item -ItemType Directory -Path $taskRoot -Force|Out-Null
foreach($taskF in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item -LiteralPath $taskF.FullName -Destination $taskRoot}
Copy-Item Tests/Stage14MarkerPositionReview.cs $taskRoot
Add-Type -Path (Get-ChildItem $taskRoot -Filter '*.cs').FullName
$taskLedger=Get-Content TestResults/v0210-final-native-purchase-ledger.json -Raw|ConvertFrom-Json
$taskFixture=@(foreach($taskLine in $taskLedger.receipts[0..22]){
    $taskC=[Gatefall.Tests.Stage14MarkerPositionReview+Command]::new()
    if($taskLine -match '^(\d+\.\d+) (build|upgrade) (\w+) @(\d+)$'){
        $taskC.Time=[double]::Parse($Matches[1],[Globalization.CultureInfo]::InvariantCulture);$taskC.Action=$Matches[2];$taskC.Kind=[Gatefall.Core.TowerKind][Enum]::Parse([Gatefall.Core.TowerKind],$Matches[3]);$taskC.Site=[int]$Matches[4]
    }elseif($taskLine -match '^(\d+\.\d+) fusion .* -> Arc @22$'){
        $taskC.Time=[double]::Parse($Matches[1],[Globalization.CultureInfo]::InvariantCulture);$taskC.Action='fusion';$taskC.Kind=[Gatefall.Core.TowerKind]::Arc;$taskC.Site=22
    }else{throw 'Unrecognized native receipt'};$taskC
})
$taskFixture|ConvertTo-Json -Depth 5|Set-Content (Join-Path $taskRoot commands.json)
$taskSeeds=if($Seed19){[int[]]@(19)}else{[int[]]@(7,41)}
$taskRows=[Gatefall.Tests.Stage14MarkerPositionReview]::Models($taskFixture,$taskSeeds,[int[]]@(13,21,23))
[ordered]@{created=(Get-Date -Format o);task=235;newModels=$taskRows.Count;nativeSource='v0210-final-native-purchase-ledger.json first23 receipts through151.6s';nativeFullRunSpend=1060;modelPrefixSpend=940;modelTowers=16;targetTimesRoundedToNativeTenths=$true;nativeExactSeedOrReplay=$false;firstCardPreferences=@('Blast','Blast','Ballistic');fallback='first offered';manualSkillMovementSupportCalls=$false;currentTowerIdRecentMetric=$true;lifetimeSiteMetricIncludesConsumedMaterials=$true;markerBonusAlreadyIncludedInAlliedDamage=$true;productChanged=$false;newApkOrAvd=$false;models=$taskRows}|ConvertTo-Json -Depth 8|Set-Content $taskOutput
if(@($taskRows|Where-Object {$_.Spent -ne 940 -or $_.CompletedOrders -ne 23 -or $_.Towers -ne 16}).Count){throw 'Common actual spend/completion differs; raw evidence saved for separate review'}
$taskRows|Select-Object Seed,MarkerSite,State,Core,Time,Spent,Credits,MarkerShots,MarkerDamage,MarkedHits,MarkBonus,FinalFiredTowers,PeakRecent3|Format-Table -AutoSize
