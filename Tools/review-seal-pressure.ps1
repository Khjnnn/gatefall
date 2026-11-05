param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$output=Join-Path $root 'fair-seal-pressure-review.json'
if(-not $UseRecorded){
    $core=Join-Path $root 'seal-pressure-read-core'
    New-Item -ItemType Directory -Path $core -Force|Out-Null
    Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|Copy-Item -Destination $core
    $path=Join-Path $core 'DefenseSimulation.cs';$original=[IO.File]::ReadAllText($path)
    $needle='            if(p.FromTower)Telemetry.TowerDamage[(int)p.Kind]+=Math.Min(e.Health,damage);'
    $probe='            Gatefall.Tests.FairSealInvestmentReview.ObserveDamage(this,e,p,Math.Min(e.Health,damage));'
    if($original.Split(@($needle),[StringSplitOptions]::None).Count -ne 2){throw 'Expected one read probe location'}
    $patched=$original.Replace($needle,$probe+"`n"+$needle)
    if($patched.Replace($probe+"`n",'') -cne $original){throw 'Read probe not reversible'}
    [IO.File]::WriteAllText($path,$patched,[Text.UTF8Encoding]::new($false))
    Add-Type -Path @((Get-ChildItem $core -Filter '*.cs').FullName+@((Join-Path $project 'Tests/EarlyCombatDigest.cs'),(Join-Path $project 'Tests/FairSealInvestmentReview.cs')))
    $rows=[Collections.Generic.List[object]]::new()
    foreach($crisis in @($false,$true)){foreach($seed in @(7,19,41)){foreach($supplement in @($false,$true)){foreach($seal in @($false,$true)){
        $rows.Add([Gatefall.Tests.FairSealInvestmentReview]::PlayFixture($seed,$seal,$crisis,7,$supplement,$true,$true))
    }}}}
    $rows|ConvertTo-Json -Depth 9|Set-Content (Join-Path $root 'fair-seal-pressure-raw.json')
    [ordered]@{conditions=24;newConditions=24;source=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}});
        scope='Eight common towers at122, growth4 research2/1/1/1; equal785/905 actual spending. Position7, idle hunter/fixed card preference. Read probe float sum must match product telemetry exactly; double category sums can differ by float rounding. No product or Android change.';rows=$rows}|ConvertTo-Json -Depth 9|Set-Content $output
}
$report=Get-Content -LiteralPath $output -Raw|ConvertFrom-Json
$rows=@($report.rows)
if($rows.Count -ne 24 -or @($rows|Group-Object Seed,Crisis,Seal,Supplement|Where-Object Count -ne 1).Count){throw 'Pressure fixture incomplete/duplicate'}
foreach($group in ($rows|Group-Object Seed,Crisis)){
    $g=@($group.Group)
    if($g.Count -ne 4 -or @($g.PrefixHash|Select-Object -Unique).Count -ne 1 -or @($g|ForEach-Object{$_.Cards-join ','}|Select-Object -Unique).Count -ne 1){throw 'Pressure prefix/card sets differ'}
    foreach($row in $g){if($row.Spent -ne 785+$row.Supplement -or -not $row.Compact -or $row.Profile -ne 'below'){throw 'Pressure fixture scope/spending differs'}}
}
$rows|Group-Object Crisis,Seal,Supplement|ForEach-Object{$g=$_.Group;[pscustomobject]@{Case=$_.Name;Wins=@($g|Where-Object State -eq Won).Count;
    CoreMin=($g.Core|Measure-Object -Minimum).Minimum;CoreMax=($g.Core|Measure-Object -Maximum).Maximum;
    EndMin=($g.End|Measure-Object -Minimum).Minimum;EndMax=($g.End|Measure-Object -Maximum).Maximum;ActualSpend=($g.Spent|Select-Object -Unique)-join ','}}|Format-Table -AutoSize
Write-Output ('PASS:24 pressure conditions; '+$output)
