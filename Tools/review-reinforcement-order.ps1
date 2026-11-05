param([ValidateSet(7,19,41)][int[]]$Seeds=@(7,41),[switch]$Append,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$output=Join-Path $root 'reinforcement-order-v0285.json'
function Shape($ledger){
    $state=@{}
    foreach($entry in $ledger){
        if($entry -match 'build (\w+) @(\d+)$'){$state[$Matches[2]]=@{kind=$Matches[1];level=1}}
        elseif($entry -match 'upgrade (\w+) @(\d+)$'){$state[$Matches[2]].level++}
    }
    @($state.Keys|Sort-Object {[int]$_}|ForEach-Object{[ordered]@{site=[int]$_;kind=$state[$_].kind;level=$state[$_].level}})
}
if(-not $UseRecorded){
    if(@($Seeds|Select-Object -Unique).Count -ne $Seeds.Count -or $Seeds.Count -eq 0){throw 'Use nonempty unique seeds'}
    if((Test-Path $output) -and -not $Append){throw 'Use -UseRecorded or append missing seeds'}
    $source=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}})
    $harness=(Get-FileHash Tests/BasicReinforcementReview.cs).Hash.ToLowerInvariant()
    $rows=[Collections.Generic.List[object]]::new()
    if($Append){$prior=Get-Content $output -Raw|ConvertFrom-Json;if(($source|ConvertTo-Json -Compress) -cne ($prior.source|ConvertTo-Json -Compress) -or $prior.harness -cne $harness){throw 'Recorded source/harness differs'};foreach($row in $prior.rows){$rows.Add($row)}}
    foreach($seed in $Seeds){if(@($rows|Where-Object Seed -eq $seed).Count){throw 'Requested late seed already recorded'}}
    $baseline=Get-Content (Join-Path $root 'basic-reinforcement-v0285.json') -Raw|ConvertFrom-Json
    foreach($hash in $baseline.source){if((Get-FileHash (Join-Path 'Assets/Scripts/Core' $hash.file)).Hash.ToLowerInvariant() -cne $hash.sha256){throw 'Baseline core differs'}}
    Add-Type -Path @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName+@('Tests/AccountGrowthChecks.cs','Tests/EarlyCombatDigest.cs','Tests/BasicReinforcementReview.cs')|ForEach-Object{(Resolve-Path $_).Path})
    $defaultControl=if($Append){$prior.defaultControl}else{[Gatefall.Tests.BasicReinforcementReview]::Play(7,$false)}
    $oldControl=@($baseline.rows|Where-Object{$_.Seed -eq 7 -and -not $_.Control})[0]
    foreach($field in $oldControl.PSObject.Properties.Name){if(($oldControl.$field|ConvertTo-Json -Compress -Depth 7) -cne ($defaultControl.$field|ConvertTo-Json -Compress -Depth 7)){throw ('Default fixture changed '+$field)}}
    $beforeRows=[Collections.Generic.List[object]]::new();foreach($row in $baseline.rows){$beforeRows.Add($row)}
    $extraRows=[Collections.Generic.List[object]]::new();$extraPath=Join-Path $root 'reinforcement-order-baseline-added-v0285.json'
    if(Test-Path $extraPath){$extra=Get-Content $extraPath -Raw|ConvertFrom-Json;if(($source|ConvertTo-Json -Compress) -cne ($extra.source|ConvertTo-Json -Compress) -or $extra.harness -cne $harness){throw 'Added baseline source/harness differs'};foreach($row in $extra.rows){$extraRows.Add($row);$beforeRows.Add($row)}}
    foreach($seed in $Seeds){foreach($control in @($false,$true)){
        if(-not @($beforeRows|Where-Object{$_.Seed -eq $seed -and $_.Control -eq $control}).Count){
            $added=[Gatefall.Tests.BasicReinforcementReview]::Play($seed,$control);$extraRows.Add($added);$beforeRows.Add($added)
            [ordered]@{source=$source;harness=$harness;rows=$extraRows;scope='Only missing original-order references, not a rerun of the existing7/41 cases'}|ConvertTo-Json -Depth 10|Set-Content $extraPath -Encoding utf8
        }
    }}
    foreach($seed in $Seeds){foreach($control in @($false,$true)){
        $rows.Add([Gatefall.Tests.BasicReinforcementReview]::Play($seed,$control,$false,$false,$true))
        [ordered]@{source=$source;harness=$harness;rows=$rows;defaultControl=$defaultControl;scope='Raw late-order results saved before final guards'}|ConvertTo-Json -Depth 10|Set-Content (Join-Path $root 'reinforcement-order-raw-v0285.json') -Encoding utf8
    }}
    $pairs=@(foreach($row in $rows){
        $old=@($beforeRows|Where-Object{$_.Seed -eq $row.Seed -and $_.Control -eq $row.Control})
        if($old.Count -ne 1){throw 'Missing unique before-order reference'};$before=$old[0]
        if($before.Spent -ne $row.Spent -or $row.Spent -ne 710 -or $row.Towers -ne 11 -or $row.CompletedOrders -ne $row.TotalOrders -or (Shape $before.Ledger|ConvertTo-Json -Compress) -cne (Shape $row.Ledger|ConvertTo-Json -Compress)){throw 'Final cost/site/kind/level mismatch'}
        if([math]::Abs(($row.LaneDamage|Measure-Object -Sum).Sum-(300-$row.Core)) -gt .01 -or $row.Credits -lt 0){throw 'Late-order economic/damage mismatch'}
        [ordered]@{seed=$row.Seed;control=$row.Control;beforeState=$before.State;afterState=$row.State;beforeCore=$before.Core;afterCore=$row.Core;beforeEnd=$before.End;afterEnd=$row.End;beforeChoiceAt=$before.ChoiceAt;afterChoiceAt=$row.ChoiceAt;actualSpend=$row.Spent;purchaseDelay=$row.MaxPurchaseDelay;sameCards=($before.Cards -join ',') -ceq ($row.Cards -join ',');beforeStrongSlow=$before.StrongSlowSamples;afterStrongSlow=$row.StrongSlowSamples}
    })
    foreach($pair in ($rows|Group-Object Seed)){$a=$pair.Group[0];$b=$pair.Group[1];if($pair.Count -ne 2 -or $a.PrefixHash -cne $b.PrefixHash -or $a.ChoiceAt -ne $b.ChoiceAt -or $a.ChoiceCost -ne 170 -or $b.ChoiceCost -ne 170){throw 'Late choice prefix/cost mismatch'}}
    [ordered]@{source=$source;harness=$harness;rows=$rows;pairs=$pairs;defaultControlEqual=$true;defaultControl=$defaultControl;defaultControlReused=$Append.IsPresent;addedBeforeCases=$extraRows.Count;priorBeforeCasesReused=$baseline.rows.Count;productChanged=$false;
        scope='Order-only pilot, crisis1-12 below growth3/research2,1,1,1, first offered card/idle Kang. Same final11 sites/kinds/levels and710 actual spend; two common spears move to60, another to90, original170 choice to120. Before results reused. Tower IDs, firing iteration order and income may change naturally; not an isolated slow-effect test or human/native play.'}|ConvertTo-Json -Depth 11|Set-Content $output -Encoding utf8
}
$r=Get-Content $output -Raw|ConvertFrom-Json
$r.pairs|ForEach-Object{[pscustomobject]$_}|Format-Table seed,control,beforeState,afterState,beforeCore,afterCore,beforeEnd,afterEnd,purchaseDelay,sameCards -AutoSize
Write-Output ('Recorded late-order cases: '+$r.rows.Count)
