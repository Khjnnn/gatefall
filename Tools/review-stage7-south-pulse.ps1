param([ValidateSet(0, 12, 16)][int]$Window = 0, [switch]$Applied)
$ErrorActionPreference = 'Stop'
if ($Applied -and $Window -ne 12) { throw 'Applied comparison uses the selected 12-second candidate' }
Set-Location -LiteralPath (Split-Path -Parent $PSScriptRoot)
$out = "TestResults/stage7-south-pulse-v02716-$Window"
if (!$Applied -and (Test-Path "$out-schedule.json")) { throw 'Preserve the recorded comparison. Use -Window 12 -Applied for the current build.' }
$core = @((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
$raon = ($core | Where-Object { (Split-Path -Leaf $_) -eq 'RaonCombat.cs' })
$copy = $null
try {
    if ($Window -and !$Applied) {
        $source = Get-Content -LiteralPath $raon -Raw
        $anchor = 'double offset=j*multiplier*step;'
        if ([regex]::Matches($source, [regex]::Escape($anchor)).Count -ne 1) { throw 'Schedule source changed' }
        $patch = @'
                    if (stage.Index == 6 && !Rules.Crisis && lane == 3 && wave >= 3) {
                        double firstSouth = (j % stage.Lanes.Length) * multiplier * step;
                        double compression = (__WINDOW__.0 - firstSouth) / (arrivalWindow - firstSouth);
                        offset = firstSouth + (offset - firstSouth) * compression;
                        step *= compression;
                    }
'@.Replace('__WINDOW__', [string]$Window)
        $copy = Join-Path ([IO.Path]::GetTempPath()) ("gatefall-south-pulse-" + [guid]::NewGuid().ToString('N') + ".cs")
        $candidate = $source.Replace($anchor, $anchor + [Environment]::NewLine + $patch)
        [IO.File]::WriteAllText($copy, $candidate)
        $files = @($core | Where-Object { $_ -ne $raon }) + @(
            $copy, (Resolve-Path Tests/StageThreatObservation.cs).Path, (Resolve-Path Tests/Stage7ChoiceReview.cs).Path
        )
        Add-Type -Path $files
    } else {
        Add-Type -Path $core
    }

    $schedule = @(for ($stage = 0; $stage -lt 14; $stage++) {
        foreach ($crisis in @($false, $true)) {
            $rules = [Gatefall.Core.CampaignBattle]@{ Stage = [Gatefall.Core.CampaignCatalog]::Stages[$stage]; Crisis = $crisis }
            $game = [Gatefall.Core.DefenseSimulation]::new([Gatefall.Core.GameBalance]::new(), 7, $rules)
            [ordered]@{
                stage = $stage + 1
                crisis = $crisis
                groups = @(foreach ($g in $game.Schedule) {
                    ,@($g.Wave, $g.Lane, [int]$g.Kind, $g.Count, $g.Warned, $g.Swarm, $g.Start, $g.Interval)
                })
            }
        }
    })
    if ($Applied) {
        $expected = Get-Content "$out-schedule.json" -Raw
        $actual = $schedule | ConvertTo-Json -Depth 6 -Compress
        if ($expected.TrimEnd() -cne $actual) { throw 'Applied schedule differs from isolated candidate' }
        Write-Output 'PASS: applied schedule exactly matches isolated candidate across all 28 stage/mode cases'
        return
    }
    $schedule | ConvertTo-Json -Depth 6 -Compress | Set-Content "$out-schedule.json"
    if (!$Window) { return }

    $reference = Get-Content TestResults/stage7-south-pulse-v02716-0-schedule.json -Raw | ConvertFrom-Json
    $changedGroups = 0
    for ($case = 0; $case -lt $schedule.Count; $case++) {
        $base = $reference[$case]
        $next = $schedule[$case]
        if ($base.stage -ne $next.stage -or $base.crisis -ne $next.crisis -or $base.groups.Count -ne $next.groups.Count) { throw 'Operation schedule shape changed' }
        for ($i = 0; $i -lt $base.groups.Count; $i++) {
            $old = $base.groups[$i]; $new = $next.groups[$i]
            if (($old[0..5] -join '|') -cne ($new[0..5] -join '|')) { throw 'Group composition changed' }
            if ($old[6] -eq $new[6] -and $old[7] -eq $new[7]) { continue }
            if ($next.stage -ne 7 -or $next.crisis -or $old[0] -lt 3 -or $old[1] -ne 3 -or !$old[4] -or $new[6] -gt $old[6]) {
                throw 'A group outside the selected late south window changed'
            }
            $changedGroups++
        }
    }
    if (!$changedGroups) { throw 'Candidate did not change any late group' }

    $rows = [Gatefall.Tests.Stage7ChoiceReview]::Compare((Get-Content TestResults/v02715-stage7-last-run.txt -Raw))
    [ordered]@{
        window = $Window; changedGroups = $changedGroups; schedules = $schedule.Count
        sourceSha256 = (Get-FileHash -LiteralPath $raon).Hash.ToLowerInvariant()
        candidateSha256 = (Get-FileHash -LiteralPath $copy).Hash.ToLowerInvariant()
        rows = $rows
    } | ConvertTo-Json -Depth 9 | Set-Content "$out-models.json"
    Write-Output "28 stage/mode schedules compared, $changedGroups selected groups changed"
    $rows | Format-Table Seed, Plan, State, Core, Elapsed, Spent, Towers, @{n='Peak';e={$_.Observation.PeakAlive}}, @{n='Breaches';e={$_.BreachesByLane -join '/'}}
} finally {
    if ($copy -and (Test-Path -LiteralPath $copy)) { Remove-Item -LiteralPath $copy }
}
