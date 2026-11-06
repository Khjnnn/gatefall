param([Parameter(Mandatory)][ValidateSet('before','after')][string]$Tag)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Add-Type -Path @((Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName+@("$root/Tests/ChapterTwoChecks.cs","$root/Tests/ChapterTwoContentContract.cs"))
$rows=[Gatefall.Tests.ChapterTwoContentContract]::Capture()
$out="$root/TestResults/chapter2-balance-20261106"
New-Item -ItemType Directory -Path $out -Force | Out-Null
$rows|ConvertTo-Json -Depth 5|Set-Content "$out/contracts-$Tag.json" -Encoding utf8
if($Tag -eq 'after'){
 $before=Get-Content "$out/contracts-before.json" -Raw|ConvertFrom-Json
 if($rows.Count -ne 24 -or $before.Count -ne 24){throw 'Contract count differs'}
 for($i=0;$i -lt $rows.Count;$i++){if($rows[$i].Hash -cne $before[$i].Hash){throw 'Non-lesson stage/rules/schedule/layout/stats contract changed'}}
 [ordered]@{status='PASS';conditions=24;excludedOnly='CampaignStage.Lesson';included='All other public stage/rules fields, actual schedules/roads/sites, configured balance, all tower definitions/recipes and profile-adjusted stats';scope='Static gameplay-data preservation for text-only edits; not new combat runs'}|ConvertTo-Json|Set-Content "$out/content-contract.json" -Encoding utf8
}
Write-Output "PASS $($rows.Count) $Tag gameplay contracts, lesson text excluded explicitly."
