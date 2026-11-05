param([switch]$Candidate,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root=Join-Path $project 'TestResults/night-balance-20261105'
$output=Join-Path $root $(if($Candidate){'target-priority-candidate-v0284.json'}else{'target-priority-static-v0284.json'})
if(-not $UseRecorded){
    if(Test-Path -LiteralPath $output){throw 'Existing result: use -UseRecorded; do not repeat completed models'}
    $cores=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs')
    $source=@($cores|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}})
    $files=$cores.FullName
    if($Candidate){
        $candidateRoot=Join-Path $root 'target-priority-candidate-core'
        New-Item -ItemType Directory -Path $candidateRoot -Force|Out-Null
        foreach($file in $cores){Copy-Item -LiteralPath $file.FullName -Destination (Join-Path $candidateRoot $file.Name)}
        $path=Join-Path $candidateRoot 'DefenseSimulation.cs';$text=Get-Content $path -Raw
        $before='private static int Lead(Enemy a,Enemy b) { int d=a.Position.Length.CompareTo(b.Position.Length); return d!=0 ? d : a.Id.CompareTo(b.Id); }'
        $after='private int Lead(Enemy a,Enemy b) { float ar=Rules?.Stage==null ? a.Position.Length : Gatefall.Tests.TargetPriorityReview.Remaining(this,a); float br=Rules?.Stage==null ? b.Position.Length : Gatefall.Tests.TargetPriorityReview.Remaining(this,b); int d=ar.CompareTo(br); return d!=0 ? d : a.Id.CompareTo(b.Id); }'
        if($text.IndexOf($before) -lt 0 -or $text.IndexOf($before) -ne $text.LastIndexOf($before)){throw 'Expected exactly one current Lead definition'}
        $candidateSource=$text.Replace($before,$after)
        if($candidateSource.Replace($after,$before) -cne $text){throw 'Candidate reverse-text preservation failed'}
        [IO.File]::WriteAllText($path,$candidateSource,[Text.UTF8Encoding]::new($false))
        $files=@((Get-ChildItem $candidateRoot -Filter '*.cs').FullName)
    }
    Add-Type -Path @($files+@('Tests/TargetPriorityReview.cs','Tests/CompetentPlanReview.cs','Tests/AccountGrowthChecks.cs','Tests/ProgressionBalanceReview.cs','Tests/CrisisCurveReview.cs')|ForEach-Object{(Resolve-Path $_).Path})
    $rows=if($Candidate){
        @(foreach($stage in @(11,13)){foreach($seed in @(7,41)){foreach($policy in @(0,3)){[Gatefall.Tests.CrisisCurveReview]::Play($stage,$seed,'below',720,$policy)}}})
    }else{[Gatefall.Tests.TargetPriorityReview]::StaticBoards()}
    [ordered]@{source=$source;candidate=$Candidate.IsPresent;cases=$rows;scope='Candidate only outside product. Remaining polyline length to core center, no speed/slow/blocking/attack reach adjustment. Static1-unit pairs show possible rank disagreement, not simultaneous spawns, query frequency or human fun. Candidate preserves lab radial ordering and all non-Lead source text; no native battle or product change.'}|ConvertTo-Json -Depth 12|Set-Content -LiteralPath $output -Encoding utf8
}
$r=Get-Content $output -Raw|ConvertFrom-Json
if($Candidate){$r.cases.Outcome|Select-Object Stage,Seed,Policy,Spent,Core,End,Stars|Format-Table -AutoSize}else{$r.cases|Select-Object Stage,Sockets,DifferingSockets,SameLaneSockets,LargestRouteGap,ExampleSocket|Format-Table -AutoSize}
Write-Output ('Recorded target-priority review: '+$output)
