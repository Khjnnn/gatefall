param([switch]$Pilot,[switch]$UseRecorded)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=Join-Path $root TestResults/chapter2-balance-20261106
New-Item -ItemType Directory -Path $out -Force|Out-Null
$files=@((Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName+@("$root/Tests/ChapterTwoChecks.cs","$root/Tests/ChapterTwoInvestmentReview.cs"))
$fingerprint=($files|ForEach-Object{[IO.Path]::GetRelativePath($root,$_)+':'+(Get-FileHash $_).Hash}) -join "`n"
$manifest=Join-Path $out investment-source.txt
$sourceMatches=$true
if(Test-Path $manifest){$sourceMatches=(Get-Content $manifest -Raw).TrimEnd() -ceq $fingerprint;if(-not $sourceMatches -and -not $UseRecorded){throw 'Recorded model/source mismatch; separate new candidates instead of relabeling results'}}else{[IO.File]::WriteAllText($manifest,$fingerprint,[Text.UTF8Encoding]::new($false))}
$path=Join-Path $out investment-raw.json
$old=if(Test-Path $path){@(Get-Content $path -Raw|ConvertFrom-Json)}else{@()}
if(-not $UseRecorded){
 Add-Type -Path $files
 $new=@([Gatefall.Tests.ChapterTwoInvestmentReview]::Run([bool]$Pilot,[string[]]@($old.Key)))
 $all=@($old)+@($new)
 $all|ConvertTo-Json -Depth 10|Set-Content $path -Encoding utf8
}else{$all=$old}
if(-not $all.Count){throw 'No recorded observations'}
if(@($all.Key|Sort-Object -Unique).Count -ne $all.Count){throw 'Duplicate observations'}
$errors=@($all|Where-Object Error)
if($errors.Count){$errors|Select-Object Key,Error|Format-List;throw 'Harness errors preserved in raw results; do not use as balance evidence'}
foreach($group in $all|Group-Object Stage,Seed,Growth,Mode){
 if(@($group.Group.PrefixHash|Sort-Object -Unique).Count -ne 1 -or @($group.Group.PrefixLedgerHash|Sort-Object -Unique).Count -ne 1){throw 'Choice prefix state/ledger mismatch'}
 if(@($group.Group|ForEach-Object{$_.ChoiceCards -join ','}|Sort-Object -Unique).Count -ne 1){throw 'Choice cards differ'}
}
$summary=@($all|Group-Object Stage,Growth,Mode,Policy|ForEach-Object{
 $g=$_.Group
 [ordered]@{stage=$g[0].Stage;growth=$g[0].Growth;mode=$g[0].Mode;policy=$g[0].Policy;conditions=$g.Count;wins=@($g|Where-Object Outcome -eq Won).Count;
  minimumCore=($g.MinimumCore|Measure-Object -Minimum).Minimum;spentRange=@(($g.Spent|Measure-Object -Minimum).Minimum,($g.Spent|Measure-Object -Maximum).Maximum);
  completePlans=@($g|Where-Object{$_.CompletedCommands -eq $_.TotalCommands}).Count;endRange=@(($g.End|Measure-Object -Minimum).Minimum,($g.End|Measure-Object -Maximum).Maximum);
  maxDelay=($g.MaximumPurchaseDelay|Measure-Object -Maximum).Maximum}
})
[ordered]@{status='PASS';conditions=$all.Count;currentSourceMatchesRecorded=$sourceMatches;recordedSourceManifest=$manifest;prefixAndChoiceCardsEqual=$true;budget=1085;summary=$summary;scope='Recorded fixture results. UseRecorded only re-summarizes the saved source version, never claims new simulations. Fixed shared450 prefix/first-offered cards/idle hunter; equal planned1085 packages. Incomplete/lost runs may spend less. Choice includes consumed ingredients, replacements, tower-count and position opportunity costs; not a single-tower causal estimate or human play.'}|ConvertTo-Json -Depth 8|Set-Content (Join-Path $out investment-summary.json) -Encoding utf8
$summary|ForEach-Object{[pscustomobject]$_}|Format-Table stage,growth,mode,policy,conditions,wins,minimumCore,spentRange,completePlans,endRange,maxDelay
Write-Output "PASS $($all.Count) recorded cases; new runs excluded all existing keys."
