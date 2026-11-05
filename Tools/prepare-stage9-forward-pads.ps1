# Preserve roads and the ten original sockets; evaluate only the added stage9 pad distribution.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$root='TestResults/night-balance-20261105'
$dir="$root/stage9-forward-pads-core"
if(Test-Path $dir){throw 'Preserve existing candidate;do not prepare twice'}
$original=[IO.File]::ReadAllText((Join-Path $project 'Assets/Scripts/Core/BattlefieldLayouts.cs'))
$old='(stage==10 || stage==11)';$new='(stage==8 || stage==10 || stage==11)'
if([regex]::Matches($original,[regex]::Escape($old)).Count -ne 1){throw 'Forward-site selector ambiguous'}
$candidate=$original.Replace($old,$new)
if($candidate.Replace($new,$old) -cne $original){throw 'Other geometry rules changed'}
New-Item -ItemType Directory $dir|Out-Null
foreach($file in Get-ChildItem Assets/Scripts/Core -Filter '*.cs'){Copy-Item $file.FullName (Join-Path $dir $file.Name)}
[IO.File]::WriteAllText((Join-Path $project "$dir/BattlefieldLayouts.cs"),$candidate)
[ordered]@{created=(Get-Date -Format o);productChanged=$false;candidateDirectory=$dir;change='Stage1-9 added14socket desired fractions only;roads/first10original sites and other13maps unchanged';source=@(Get-ChildItem Assets/Scripts/Core -Filter '*.cs'|ForEach-Object{[ordered]@{file=$_.Name;sha256=(Get-FileHash $_.FullName).Hash.ToLowerInvariant()}})}|ConvertTo-Json -Depth 6|Set-Content "$root/stage9-forward-pads-preflight.json" -Encoding utf8
'Prepared one added-pad-only candidate;no product/APK change'
