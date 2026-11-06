param([Parameter(Mandatory)][string]$After,[string]$Before='',[string]$Tag='profile')
$ErrorActionPreference='Stop'
$root=Split-Path -Parent $PSScriptRoot
Add-Type -Path (Get-ChildItem "$root/Assets/Scripts/Core/*.cs").FullName
$bytes=[IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $After))
$p=[Gatefall.Core.CampaignStore]::Decode($bytes)
$same=$null
if($Before){$old=[Gatefall.Core.CampaignStore]::Decode([IO.File]::ReadAllBytes((Resolve-Path -LiteralPath $Before)));$same=[Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($old)) -ceq [Convert]::ToBase64String([Gatefall.Core.CampaignStore]::Encode($p))}
$stages=@(foreach($s in [Gatefall.Core.CampaignCatalog]::Stages){$i=$s.Index;[ordered]@{id=$s.Id;label=[Gatefall.Core.CampaignCatalog]::StageId($i);normal=$p.BestStars[$i];crisis=$p.CrisisStars[$i];first=$p.FirstCleared[$i];erosion=$p.BestErosion[$i];open=$p.IsStageOpen($i)}})
$r=[ordered]@{bytes=$bytes.Length;sha256=(Get-FileHash -LiteralPath $After).Hash.ToLowerInvariant();valid=$p.Valid();cores=$p.MagicCores;hunter=$p.SelectedHunter;growth=$p.Growth;research=$p.Research;loadout=$p.Loadout;muted=$p.Muted;stars=$p.StarsTotal;crisisStars=$p.CrisisStarsTotal;pending=$p.PendingRun;pendingStage=$p.PendingStage;pendingCrisis=$p.PendingCrisis;lastReward=$p.LastReward;lastRewardRun=$p.LastRewardRun;canonicalSameAsBefore=$same;stages=$stages}
$r|ConvertTo-Json -Depth 8|Set-Content "$root/TestResults/chapter2-implementation/$Tag.json" -Encoding utf8
([pscustomobject]$r)|Select-Object bytes,sha256,valid,cores,hunter,growth,research,loadout,stars,lastReward,pending,canonicalSameAsBefore|ConvertTo-Json -Depth 4
