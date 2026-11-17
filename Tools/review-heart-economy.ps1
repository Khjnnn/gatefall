param([ValidateRange(1,56)][int]$Days=28,[int[]]$Seeds=@(7,19,41),[switch]$Enforce,[switch]$SummaryOnly)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Add-Type -CompilerOptions '/langversion:7.3' -Path @((Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName+"$root/Tests/HeartEconomyReview.cs")
$rows=[Gatefall.Tests.HeartEconomyReview]::Review($Days,$Seeds)
# Outputs are disposable evidence, never input fixtures.
$out=Join-Path $root 'TestResults/heart-economy'
New-Item -ItemType Directory -Path $out -Force|Out-Null
$rows|Export-Csv -NoTypeInformation -Encoding utf8 (Join-Path $out 'daily.csv')
$summary=[Gatefall.Tests.HeartEconomyReview]::Summary($rows)
$summary|Set-Content -Encoding utf8 (Join-Path $out 'summary.txt')
if(-not $SummaryOnly){$rows|ConvertTo-Csv -NoTypeInformation}
$summary
if($Enforce){[Gatefall.Tests.HeartEconomyReview]::Run()}
