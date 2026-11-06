param([switch]$UseRecorded)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
$out=Join-Path $root TestResults/chapter2-balance-20261106
$baseline=@(Get-Content "$out/investment-raw.json" -Raw|ConvertFrom-Json)
$files=@((Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName+@("$root/Tests/ChapterTwoChecks.cs","$root/Tests/ChapterTwoInvestmentReview.cs","$root/Tests/ChapterTwoInvestmentFollowup.cs"))
$fingerprint=($files|ForEach-Object{[IO.Path]::GetRelativePath($root,$_)+':'+(Get-FileHash $_).Hash}) -join "`n"
$manifest="$out/investment-followup-source.txt"
$sourceMatches=$true
if(Test-Path $manifest){$sourceMatches=(Get-Content $manifest -Raw).TrimEnd() -ceq $fingerprint;if(-not $sourceMatches -and -not $UseRecorded){throw 'Followup source changed; keep candidates separate'}}else{[IO.File]::WriteAllText($manifest,$fingerprint,[Text.UTF8Encoding]::new($false))}
$path="$out/investment-followup-raw.json"
if(-not $UseRecorded){
 Add-Type -Path $files
 $fixtures=[Collections.Generic.List[Gatefall.Tests.ChapterTwoInvestmentFollowup+Fixture]]::new()
 foreach($b in $baseline){
  $variants=@()
  if(($b.Stage -eq '2-5' -and $b.Policy -eq 'barrage-west' -and $b.Mode -eq 'Crisis' -and $b.Growth -eq 5 -and $b.Seed -eq 7) -or ($b.Stage -eq '2-6' -and $b.Policy -eq 'seal' -and $b.Mode -eq 'Crisis' -and $b.Growth -eq 5 -and $b.Seed -eq 7)){$variants+='control'}
  if($b.Stage -eq '2-5' -and $b.Policy -eq 'barrage-west'){$variants+='spare-pair'}
  if($b.Stage -eq '2-6' -and $b.Seed -in @(7,41)){$variants+='cap825';if($b.Policy -eq 'seal'){$variants+='cap825-seal-east'}}
  foreach($variant in $variants){$f=[Gatefall.Tests.ChapterTwoInvestmentFollowup+Fixture]::new();foreach($name in @('Stage','Policy','Mode','Seed','Growth','Ledger','PurchaseDelays')){$f.$name=$b.$name};$f.BaseKey=$b.Key;$f.Variant=$variant;$fixtures.Add($f)}
 }
 $rows=@([Gatefall.Tests.ChapterTwoInvestmentFollowup]::Run($fixtures.ToArray()))
 $rows|ConvertTo-Json -Depth 10|Set-Content $path -Encoding utf8
}else{$rows=@(Get-Content $path -Raw|ConvertFrom-Json)}
$errors=@($rows|Where-Object Error);if($errors.Count){$errors|Select-Object BaseKey,Variant,Error|Format-List;throw 'Followup harness rejected; raw preserved'}
foreach($r in $rows){
 $b=@($baseline|Where-Object Key -eq $r.BaseKey);if($b.Count -ne 1 -or $r.PrefixHash -cne $b[0].PrefixHash){throw 'Replay common state differs'};$b=$b[0]
 if($r.Variant -eq 'control'){
  foreach($field in @('Outcome','Spent','Remaining','MinimumCore')){if($r.$field -ne $b.$field){throw "Control differs: $field"}}
  foreach($field in @('Cards','Shots','Ledger')){if(($r.$field -join ',') -cne ($b.$field -join ',')){throw "Control differs: $field"}}
  if($r.Damage.Length -ne $b.Damage.Length){throw 'Control damage dimensions differ'}
  for($i=0;$i -lt $r.Damage.Length;$i++){
   if([BitConverter]::ToInt32([BitConverter]::GetBytes([single]$r.Damage[$i]),0) -ne [BitConverter]::ToInt32([BitConverter]::GetBytes([single]$b.Damage[$i]),0)){throw 'Control float damage bits differ'}
  }
  if([math]::Abs($r.End-$b.End) -gt .0001){throw 'Control time differs'}
 }
}
$summary=@($rows|Group-Object Stage,Growth,Mode,Variant,Policy|ForEach-Object{$g=$_.Group;[ordered]@{stage=$g[0].Stage;growth=$g[0].Growth;mode=$g[0].Mode;variant=$g[0].Variant;policy=$g[0].Policy;conditions=$g.Count;wins=@($g|Where-Object Outcome -eq Won).Count;minimumCore=($g.MinimumCore|Measure-Object -Minimum).Minimum;maxDelay=($g.MaximumDelay|Measure-Object -Maximum).Maximum;spent=@($g.Spent|Sort-Object -Unique);end=@(($g.End|Measure-Object -Minimum).Minimum,($g.End|Measure-Object -Maximum).Maximum)}})
[ordered]@{status='PASS';conditions=$rows.Count;currentSourceMatchesRecorded=$sourceMatches;recordedSourceManifest=$manifest;exactReplayControls=2;prefixStateEqual=$true;summary=$summary;scope='Recorded source-version purchase timing replay. UseRecorded never runs a new simulation. Spare-pair changes consumed rupture and reciprocal refill; cap825 removes only planned180s reinforcement without deleting unspent cash. Single-tower causal/human-fun claims excluded.'}|ConvertTo-Json -Depth 8|Set-Content "$out/investment-followup-summary.json" -Encoding utf8
$summary|ForEach-Object{[pscustomobject]$_}|Format-Table stage,growth,mode,variant,policy,conditions,wins,minimumCore,spent,maxDelay
Write-Output "PASS $($rows.Count) followup cases with2 exact controls."
