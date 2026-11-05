param([ValidateSet('9','latest')][string]$Language='9',[switch]$Candidate,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
$suffix=if($Candidate){'candidate'}else{'before'}
$output="$root/target-query-csharp$Language-$suffix-v0286.json"
if(-not $UseRecorded){
    if(Test-Path $output){throw 'Recorded allocation probe exists; use -UseRecorded'}
    $paths=@((Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName)
    if($Candidate){
        $dir="$root/target-query-candidate-core";New-Item -ItemType Directory $dir -Force|Out-Null
        foreach($path in $paths){Copy-Item $path (Join-Path $dir ([IO.Path]::GetFileName($path))) -Force}
        $path=Join-Path $dir DefenseSimulation.cs;$text=[IO.File]::ReadAllText((Join-Path (Get-Location) $path))
        $old='targetScratch.Sort(Lead);return targetScratch;';$new='targetScratch.Sort(LeadComparison);return targetScratch;';$field='        private static readonly Comparison<Enemy> LeadComparison = Lead;'
        if($text.Split($old).Count -ne 2 -or $text.Contains($field)){throw 'Target query candidate differs'}
        $changed=$text.Replace($old,$new).Replace('        private static int Lead(Enemy a,Enemy b)', $field+"`n"+'        private static int Lead(Enemy a,Enemy b)')
        if($changed.Replace($new,$old).Replace($field+"`n",'') -cne $text){throw 'Candidate changed other code'}
        [IO.File]::WriteAllText((Join-Path (Get-Location) $path),$changed)
        $paths=@((Get-ChildItem $dir -Filter '*.cs').FullName)
    }
    Add-Type -Path @($paths+(Join-Path $project 'Tests/TargetQueryAllocationReview.cs')) -CompilerOptions ('/langversion:'+$Language)
    $rows=@(0,64|ForEach-Object{[Gatefall.Tests.TargetQueryAllocationReview]::Run($_)})
    [ordered]@{language=$Language;candidate=[bool]$Candidate;rows=$rows;source=@($paths|ForEach-Object{[ordered]@{file=[IO.Path]::GetFileName($_);sha256=(Get-FileHash $_).Hash.ToLowerInvariant()}});scope='Warm host .NET target-query allocation under explicit compiler language. No Unity/Android GC, frame-time, thermal or physical-device claim.'}|ConvertTo-Json -Depth 7|Set-Content $output -Encoding utf8
}
$r=Get-Content $output -Raw|ConvertFrom-Json
$r.rows|Format-Table -AutoSize
