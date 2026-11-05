$ErrorActionPreference='Stop'
& (Join-Path $PSScriptRoot 'review-resonance-stats.ps1') -Tag resonance-stats-current
$project=Split-Path -Parent $PSScriptRoot
$result=(Get-Content (Join-Path $project 'TestResults/resonance-stats-current.json') -Raw|ConvertFrom-Json).report
if($result.Cases -ne 2646){throw 'Resonance coverage incomplete'}
$chain=@($result.Allocations|Where-Object Kind -eq Arc)[0]
foreach($kind in @('Autocannon','Binding','Rupture','Barrier')){
    $row=@($result.Allocations|Where-Object Kind -eq $kind)[0]
    if($row.Calls -ne $chain.Calls -or $row.Bytes -gt $chain.Bytes*.8){throw ('Redundant resonance allocation returned: '+$kind)}
}
'PASS2646 resonance stat/alias cases and no second snapshot allocation; host .NET only'
