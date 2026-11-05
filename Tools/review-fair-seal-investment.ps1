param([switch]$UseRecorded,[switch]$ControlOnly)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
$folder=Join-Path $project 'TestResults/night-balance-20261105'
$output=Join-Path $folder 'fair-seal-followup.json'
$baselinePath=Join-Path $folder 'fair-seal-pilot.json'
$controlPath=Join-Path $folder 'fair-seal-original-control.json'
if (-not $UseRecorded) {
    $original=Join-Path $project 'Assets/Scripts/Core'
    $core=$original
    if (-not $ControlOnly) {
        $pwsh=(Get-Command pwsh).Source
        & $pwsh -NoProfile -File $PSCommandPath -ControlOnly
        if ($LASTEXITCODE -ne 0) { throw 'Original-core control failed' }
        $core=Join-Path $folder 'fair-seal-read-core'
        New-Item -ItemType Directory -Path $core -Force | Out-Null
        Get-ChildItem -LiteralPath $original -Filter '*.cs' | Copy-Item -Destination $core
        $file=Join-Path $core 'DefenseSimulation.cs'
        $text=[IO.File]::ReadAllText($file)
        $needle='            if(p.FromTower)Telemetry.TowerDamage[(int)p.Kind]+=Math.Min(e.Health,damage);'
        if (($text.Split(@($needle),[StringSplitOptions]::None)).Count -ne 2) { throw 'Expected one damage probe location' }
        $probe='            Gatefall.Tests.FairSealInvestmentReview.ObserveDamage(this,e,p,Math.Min(e.Health,damage));'
        $patched=$text.Replace($needle,$probe+"`n"+$needle)
        if ($patched.Replace($probe+"`n",'') -cne $text) { throw 'Read probe is not exactly reversible' }
        [IO.File]::WriteAllText($file,$patched,[Text.UTF8Encoding]::new($false))
        foreach($source in Get-ChildItem -LiteralPath $original -Filter '*.cs') {
            $copy=[IO.File]::ReadAllText((Join-Path $core $source.Name))
            if ($source.Name -eq 'DefenseSimulation.cs') { $copy=$copy.Replace($probe+"`n",'') }
            if ($copy -cne [IO.File]::ReadAllText($source.FullName)) { throw ('Core copy differs beyond read probe: '+$source.Name) }
        }
    }
    Add-Type -Path @((Get-ChildItem -LiteralPath $core -Filter '*.cs').FullName + @(
        (Join-Path $project 'Tests/EarlyCombatDigest.cs'),(Join-Path $project 'Tests/FairSealInvestmentReview.cs')))
    $control=[Gatefall.Tests.FairSealInvestmentReview]::Play(7,$true,$false,4,$false)
    if ($ControlOnly) {
        $control | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $controlPath -Encoding utf8
        Write-Output 'Original-core selected control recorded'
        return
    }
    $unprobed=Get-Content -LiteralPath $controlPath -Raw | ConvertFrom-Json
    foreach($property in $unprobed.PSObject.Properties.Name | Where-Object { $_ -notin @('PostSwarm','PostStrong','PostOther') }) {
        if (($unprobed.$property | ConvertTo-Json -Compress -Depth 7) -cne ($control.$property | ConvertTo-Json -Compress -Depth 7)) {
            throw ('Read observation changed selected control: '+$property)
        }
    }
    $baseline=Get-Content -LiteralPath $baselinePath -Raw | ConvertFrom-Json
    $old=@($baseline | Where-Object { $_.Seed -eq 7 -and $_.Seal -and -not $_.Crisis })
    if ($old.Count -ne 1) { throw 'Missing original pilot control' }
    foreach($property in $old[0].PSObject.Properties.Name) {
        if (($old[0].$property | ConvertTo-Json -Compress -Depth 7) -cne ($control.$property | ConvertTo-Json -Compress -Depth 7)) {
            throw ('Current selected control differs from reusable pilot: '+$property)
        }
    }
    $rows=[Collections.Generic.List[object]]::new()
    foreach($crisis in @($false,$true)) { foreach($seed in @(7,19,41)) {
        # Position alone at 945; then separately reinvest exactly 120 with the original and alternate anchors.
        $rows.Add([Gatefall.Tests.FairSealInvestmentReview]::Play($seed,$true,$crisis,7,$false))
        $rows.Add([Gatefall.Tests.FairSealInvestmentReview]::Play($seed,$true,$crisis,4,$true))
        $rows.Add([Gatefall.Tests.FairSealInvestmentReview]::Play($seed,$true,$crisis,7,$true))
        $rows.Add([Gatefall.Tests.FairSealInvestmentReview]::Play($seed,$false,$crisis,4,$true))
    } }
    $rows | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath (Join-Path $folder 'fair-seal-followup-raw.json') -Encoding utf8
    $maxDamageReconcile=0.0
    foreach($row in $rows) {
        $pair=@($baseline | Where-Object { $_.Seed -eq $row.Seed -and $_.Crisis -eq $row.Crisis -and $_.Seal -eq $row.Seal })
        if ($pair.Count -ne 1 -or $row.PrefixHash -cne $pair[0].PrefixHash -or
            ($row.Cards -join ',') -cne ($pair[0].Cards -join ',')) { throw 'Choice prefix/cards differ from pilot' }
        for($i=0;$i -lt $row.Damage.Length;$i++) {
            $delta=[math]::Abs(($row.Damage[$i]-$row.PrefixDamage[$i])-($row.PostSwarm[$i]+$row.PostStrong[$i]+$row.PostOther[$i]))
            $maxDamageReconcile=[math]::Max($maxDamageReconcile,$delta)
            if ($delta -gt .25) { throw ('Effective damage reconciliation failed: '+$delta) }
        }
    }
    $hashes=@(Get-ChildItem -LiteralPath $original -Filter '*.cs' | ForEach-Object {
        [ordered]@{file=$_.Name;sha256=(Get-FileHash -LiteralPath $_.FullName).Hash.ToLowerInvariant()}
    })
    [ordered]@{
        source=$hashes; baseline=$baselinePath; reusedPilotConditions=12; newCandidateConditions=$rows.Count;
        selectedObservationControlConditions=2; observationControlEquivalent=$true; pilotControlEquivalent=$true;
        reversibleReadProbe=$true; maximumDamageReconciliationError=$maxDamageReconcile;
        scope='Stage 1-14 normal/crisis, growth5 research3/2/2/2, idle hunter and fixed card preference. Shared prechoice state hash, equal actual spend945 or1065. Anchor comparison and reinvestment comparison are separate policies. Swarm/strong/other postchoice effective damage is included in total damage; never sum it again. Not human play, universal optimality, or physical-device evidence.';
        control=$control;rows=$rows
    } | ConvertTo-Json -Depth 9 | Set-Content -LiteralPath $output -Encoding utf8
}
$report=Get-Content -LiteralPath $output -Raw | ConvertFrom-Json
$report.rows | Group-Object Crisis,Seal,Anchor,Supplement | ForEach-Object {
    $g=$_.Group
    [pscustomobject]@{Case=$_.Name;Count=$g.Count;Wins=@($g|Where-Object State -eq Won).Count;
        MinimumCore=($g.Core|Measure-Object -Minimum).Minimum;EndMin=($g.End|Measure-Object -Minimum).Minimum;
        EndMax=($g.End|Measure-Object -Maximum).Maximum;Spent=($g.Spent|Select-Object -Unique)-join ',';
        SealStrongMean=($g|ForEach-Object{$_.PostStrong[6]}|Measure-Object -Average).Average;
        SealSwarmMean=($g|ForEach-Object{$_.PostSwarm[6]}|Measure-Object -Average).Average}
} | Format-Table -AutoSize
Write-Output ('Fair investment follow-up: '+$output)
