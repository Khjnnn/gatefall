# Isolated geometry candidate. Never edits the delivered game's core or APK.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
$dir="$root/stage9-south-merge-core"
if(Test-Path $dir){throw 'Preserve existing candidate; inspect its records instead of repeating preparation'}
$path=Join-Path $project 'Assets/Scripts/Core/BattlefieldLayouts.cs'
$original=[IO.File]::ReadAllText($path).Replace("`r`n","`n")
$old=@'
            L("distribution-rooftop-loop",2,
              P(-23,0,-17,0,-17,6,-7,6,-7,0,0,0),none,
              P(23,-3,13,-3,13,4,5,4,5,0,0,0)),
'@
$new=@'
            L("distribution-offset-south-merge",2,
              P(-23,5,-16,5,-16,-3,6,-3,6,0,0,0),none,
              P(23,6,16,6,16,-3,6,-3,6,0,0,0)),
'@
if([regex]::Matches($original,[regex]::Escape($old)).Count -ne 1){throw 'Current stage9 definition ambiguous'}
$candidate=$original.Replace($old,$new)
if($candidate.Replace($new,$old) -cne $original){throw 'Other maps changed'}
New-Item -ItemType Directory $dir|Out-Null
foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item $file.FullName (Join-Path $dir $file.Name)}
[IO.File]::WriteAllText((Join-Path $project "$dir/BattlefieldLayouts.cs"),$candidate)
[ordered]@{created=(Get-Date -Format o);version='0.28.7';productChanged=$false;candidateDirectory=$dir;change='Stage1-9 only,two entrances with offset long-west and south merge,24sites required';priorRejectedSouth='The old0.19south-spine had all paths belowcore and failed24sockets. This candidate adds two tall side approaches;not that rejected path.';source=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}})}|ConvertTo-Json -Depth 6|Set-Content "$root/stage9-south-merge-preflight.json" -Encoding utf8
'Prepared one isolated stage9 geometry candidate;no product/APK change'
