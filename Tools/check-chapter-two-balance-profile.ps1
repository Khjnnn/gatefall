param([Parameter(Mandatory)][string]$InputFile,[Parameter(Mandatory)][string]$OutputFile)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Add-Type -Path (Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName
$bytes=[IO.File]::ReadAllBytes((Resolve-Path $InputFile))
$p=[Gatefall.Core.CampaignStore]::Decode($bytes)
if($null -eq $p -or -not $p.Valid()){throw 'Invalid recorded synthetic profile'}
[ordered]@{bytes=$bytes.Length;sha256=(Get-FileHash $InputFile).Hash.ToLowerInvariant();valid=$p.Valid();cores=$p.MagicCores;stars=$p.StarsTotal;crisisStars=$p.CrisisStarsTotal;hunter=$p.SelectedHunter;growth=$p.Growth;research=$p.Research;loadout=$p.Loadout;pending=$p.PendingRun;pendingStage=$p.PendingStage;pendingCrisis=$p.PendingCrisis;lastReward=$p.LastReward;lastRewardRun=$p.LastRewardRun;normal=$p.BestStars;crisis=$p.CrisisStars;first=$p.FirstCleared}|ConvertTo-Json -Depth 6|Set-Content $OutputFile -Encoding utf8
Write-Output "PASS profile $InputFile"
