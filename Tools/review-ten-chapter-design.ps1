# Structural design audit only. Does not add stages, simulate battles, build or operate Android.
$ErrorActionPreference='Stop'
$project=Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $project
$file='Docs/Design/gatefall_10chapter_outline.json'
$root='TestResults/ten-chapter-plan-20261106'
New-Item -ItemType Directory -Path $root -Force|Out-Null
function Require($condition,[string]$why){if(-not $condition){throw $why}}
$p=Get-Content $file -Raw|ConvertFrom-Json
Require ($p.status -ceq 'design_proposal_not_runtime' -and $p.implementedChapterCount -eq 1 -and $p.implementedStageCount -eq 14) 'Planning/implementation conflated'
Require ($p.chapters.Count -eq 10 -and ($p.chapters.stageCount|Measure-Object -Sum).Sum -eq 240) 'Chapter/stage budget differs'
Require ($p.chapters[0].stageCount -eq 14 -and $p.chapters[0].sealStage -eq 6 -and $p.chapters[1].unlockAfter -ceq 'C01-S014') 'Existing first chapter/gate was changed'
$seals=@($p.chapters|Where-Object sealsTower)
Require ($seals.Count -eq 7 -and @($seals.towerId|Sort-Object -Unique).Count -eq 7 -and @($p.chapters.towerId|Sort-Object -Unique).Count -eq 7) 'Seven black towers not preserved'
Require (((@($seals.towerId|Sort-Object) -join ',')) -ceq '1,2,3,4,5,6,7') 'Black tower IDs were replaced'
for($i=0;$i -lt 10;$i++){
    $chapter=$p.chapters[$i]
    Require ($chapter.id -eq $i+1 -and $chapter.stageCount -gt 0 -and $chapter.sites[1] -ge $chapter.towerCap[1] -and $chapter.waves[0] -le $chapter.waves[1]) 'Invalid chapter/range'
    if($i -gt 0){$prev=$p.chapters[$i-1];$expected='C{0:D2}-S{1:D3}' -f $prev.id,$prev.stageCount;Require ($chapter.unlockAfter -ceq $expected) 'Prerequisite does not reference previous chapter end';Require ($chapter.status -ceq 'proposal') 'Unimplemented chapter labelled playable'}
    if($chapter.sealsTower){Require ($chapter.sealStage -gt 0 -and $chapter.sealStage -le $chapter.stageCount) 'Seal outside chapter'}
}
foreach($field in @('topology','decision','visual')){Require (@($p.chapters.$field|Sort-Object -Unique).Count -eq 10) ('Repeated chapter brief '+$field)}
Require ($p.newTowers.Count -eq 4 -and $p.newBaseTowerBudget -eq 4 -and $p.defaultCatalystsGranted -eq 2 -and $p.loadoutLimit -eq 4) 'Scope/catalyst/loadout budget changed'
Require (@($p.newTowers.id|Sort-Object -Unique).Count -eq 4) 'Duplicate new basic ID'
Add-Type -Path (Get-ChildItem Assets/Scripts/Core -Filter '*.cs').FullName
$definitions=@([Gatefall.Core.TowerCatalog]::All)
$basic=@($definitions|Where-Object Tier -eq 1|ForEach-Object{$_.Kind.ToString()})
Require ($basic.Count -eq 5 -and $basic.Count+$p.newTowers.Count -eq $p.baseTowerBudget) 'Basic collection budget differs'
$recipes=@([Gatefall.Core.TowerCatalog]::Recipes|ForEach-Object{[pscustomobject]@{id='old'+$_.Id;first=$_.First.ToString();second=$_.Second.ToString();result=$_.Result.ToString()}})
foreach($tower in $p.newTowers){
    Require ($tower.id -notin $basic -and $tower.recipe.partner -in $basic -and $tower.recipe.sealPartner -in $basic -and $tower.recipe.partner -cne $tower.recipe.sealPartner) 'New material IDs conflict'
    $chapter=$p.chapters[$tower.chapter-1]
    Require ($chapter.newBase -ceq $tower.id -and $tower.unlockStage -lt $p.newRecipeUnlockStages.tier2After -and $p.newRecipeUnlockStages.tier2After -lt $p.newRecipeUnlockStages.tier3RouteAfter -and $p.newRecipeUnlockStages.tier3RouteAfter -lt 6) 'Unlock order cannot teach upper recipe by the sixth stage'
    Require ($null -eq $tower.recipe.fee -and $null -eq $tower.recipe.upgrade -and $null -eq $tower.recipe.sealFee) 'Future prices should remain explicitly unpriced'
    $recipes += [pscustomobject]@{id='new-'+$tower.id;first=$tower.id;second=$tower.recipe.partner;result=$tower.recipe.result}
    $recipes += [pscustomobject]@{id='new-seal-'+$tower.id;first=$tower.recipe.result;second=$tower.recipe.sealPartner;result='Seal'}
}
Require ($recipes.Count -eq 17 -and $recipes.Count-[Gatefall.Core.TowerCatalog]::Recipes.Count -eq $p.newRecipeBudget) 'Recipe budget differs'
$resultTypes=@($recipes.result|Sort-Object -Unique)
Require ($resultTypes.Count+$basic.Count+$p.newTowers.Count -eq 19) 'Unique tower/result type budget differs'
function Closure([string[]]$loadout){
    $available=[Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach($id in $loadout){[void]$available.Add($id)}
    do{$changed=$false;foreach($r in $recipes){if($available.Contains($r.first) -and $available.Contains($r.second) -and $available.Add($r.result)){$changed=$true}}}while($changed)
    return ,$available
}
$allBasic=@($basic)+@($p.newTowers.id);$loadouts=0;$maxRecipes=0;$maxAnchorOptions=0;$newPaths=@()
for($a=0;$a -lt $allBasic.Count-3;$a++){for($b=$a+1;$b -lt $allBasic.Count-2;$b++){for($c=$b+1;$c -lt $allBasic.Count-1;$c++){for($d=$c+1;$d -lt $allBasic.Count;$d++){
    $available=Closure @($allBasic[$a],$allBasic[$b],$allBasic[$c],$allBasic[$d]);$loadouts++
    $visible=@($recipes|Where-Object{$available.Contains($_.first) -and $available.Contains($_.second)})
    $maxRecipes=[Math]::Max($maxRecipes,$visible.Count)
    foreach($anchor in $available){$options=@($visible|Where-Object{$_.first -ceq $anchor -or $_.second -ceq $anchor});$maxAnchorOptions=[Math]::Max($maxAnchorOptions,$options.Count)}
}}}}
foreach($tower in $p.newTowers){
    $materials=@($tower.id,$tower.recipe.partner,$tower.recipe.sealPartner)|Sort-Object -Unique
    Require ($materials.Count -le 4 -and (Closure $materials).Contains('Seal')) 'New basic prevents an upper route within four slots'
    $newPaths+=[ordered]@{newBase=$tower.id;requiredBaseTypes=$materials;typeCount=$materials.Count;canReachSeal=$true;catalysts=2;pricesValidated=$false}
}
Require ($loadouts -eq 126) 'Four-of-nine loadout review incomplete'
$oldMaxRecipes=0;$oldMaxAnchor=0;$oldLoadouts=0
for($a=0;$a -lt $basic.Count-3;$a++){for($b=$a+1;$b -lt $basic.Count-2;$b++){for($c=$b+1;$c -lt $basic.Count-1;$c++){for($d=$c+1;$d -lt $basic.Count;$d++){
    $available=Closure @($basic[$a],$basic[$b],$basic[$c],$basic[$d]);$oldLoadouts++
    $visible=@($recipes|Where-Object{$available.Contains($_.first) -and $available.Contains($_.second)})
    $oldMaxRecipes=[Math]::Max($oldMaxRecipes,$visible.Count)
    foreach($anchor in $available){$options=@($visible|Where-Object{$_.first -ceq $anchor -or $_.second -ceq $anchor});$oldMaxAnchor=[Math]::Max($oldMaxAnchor,$options.Count)}
}}}}
Require ($oldLoadouts -eq 5 -and $maxAnchorOptions -le 4) 'Existing comparison/menu option scope differs'
# Recursive cost uses current catalog prices only. New tower prices are deliberately not invented.
function Level2([Gatefall.Core.TowerKind]$kind){
    $def=[Gatefall.Core.TowerCatalog]::Get($kind)
    if($def.Tier -eq 1){return [pscustomobject]@{cost=$def.BuildCost+$def.UpgradePrice(1);catalysts=0;leaves=1;peak=1;materials=@($kind.ToString())}}
    $sources=@([Gatefall.Core.TowerCatalog]::Recipes|Where-Object Result -eq $kind)
    Require ($sources.Count -eq 1) 'Cost recursion requires a single existing tier2 source'
    $source=$sources[0];$first=Level2 $source.First;$second=Level2 $source.Second
    $peak=[Math]::Min([Math]::Max($first.peak,1+$second.peak),[Math]::Max($second.peak,1+$first.peak))
    return [pscustomobject]@{cost=$first.cost+$second.cost+$source.Credits+$def.UpgradePrice(1);catalysts=$first.catalysts+$second.catalysts+1;leaves=$first.leaves+$second.leaves;peak=$peak;materials=@($first.materials)+@($second.materials)}
}
$sealCosts=@()
foreach($route in [Gatefall.Core.TowerCatalog]::Recipes|Where-Object Result -eq ([Gatefall.Core.TowerKind]::Seal)){
    $first=Level2 $route.First;$second=Level2 $route.Second
    $price=$first.cost+$second.cost+$route.Credits;$leaves=$first.leaves+$second.leaves
    $peak=[Math]::Min([Math]::Max($first.peak,1+$second.peak),[Math]::Max($second.peak,1+$first.peak))
    Require ($first.catalysts+$second.catalysts+1 -eq 2 -and $leaves -eq 3 -and $peak -eq 2) 'Existing upper-route resource contract differs'
    $sealCosts+=[ordered]@{recipeId=$route.Id;path=$route.First.ToString()+'+'+$route.Second;materialTypes=@($first.materials)+@($second.materials);sealCost=$price;catalysts=2;minimumStaggeredSites=$peak;parallelMaterialSites=$leaves;refillSites=$leaves-1;withTwoSpearRefills=$price+2*[Gatefall.Core.TowerCatalog]::Get([Gatefall.Core.TowerKind]::Autocannon).BuildCost}
}
$arcRoute=@($sealCosts|Where-Object recipeId -eq 3)
Require ($arcRoute.Count -eq 1 -and $arcRoute[0].sealCost -eq 455 -and $arcRoute[0].withTwoSpearRefills -eq 535) 'Documented current Arc/Seal cost differs'
$income=([Gatefall.Core.CampaignCatalog]::Stages|Measure-Object FirstReward -Sum).Sum
$hero5=([Gatefall.Core.CampaignCatalog]::GrowthCosts[0..4]|Measure-Object -Sum).Sum
$research=0;foreach($rank in @(3,2,2,2)){for($i=0;$i -lt $rank;$i++){$research += [Gatefall.Core.CampaignCatalog]::ResearchCosts[$i]}}
Require ($income -eq 322 -and $hero5+$research -eq 78 -and $income-$hero5-$research -eq 244) 'Current reward/growth anchor differs'
$runtimeFiles=@(Get-ChildItem Assets/Scripts/Core,Assets/Scripts/Presentation -Filter '*.cs')+@(Get-Item Assets/Editor/PrototypeTools.cs,ProjectSettings/ProjectSettings.asset)
$baseline=Get-Content TestResults/soft-ui-20261106/product-candidate-hashes.json -Raw|ConvertFrom-Json
foreach($f in $baseline){Require ((Get-FileHash $f.file).Hash.ToLowerInvariant() -ceq $f.sha256) ('Planning changed product '+$f.file)}
$report=[ordered]@{status='design_structure_pass_not_gameplay';chapters=10;plannedRegularStages=240;implementedStages=14;preservedFirstChapterEnd='C01-S014';blackTowers=7;pairedTowerIds=@(3,6,7);baseTowerBudget=9;uniqueResultTypes=$resultTypes.Count+$basic.Count+$p.newTowers.Count;totalRecipeBudget=17;reviewedFourTypeLoadouts=$loadouts;maxReachableRecipes=$maxRecipes;maxOptionsPerAnchor=$maxAnchorOptions;currentFourTypeLoadouts=$oldLoadouts;currentMaxRecipes=$oldMaxRecipes;currentMaxAnchorOptions=$oldMaxAnchor;newPaths=$newPaths;currentSealCosts=$sealCosts;currentFirstClearIncome=$income;currentRecommendedSpend=$hero5+$research;currentReserve=$income-$hero5-$research;productFilesUnchanged=$baseline.Count;newBattlesRun=0;newAndroidSessions=0;newAPKs=0;unresolved=@('new tower/recipe prices and combat numbers','v2-to-v3 migration implementation','chapter2 six actual maps/waves','growth economy and hero cap expansion','native UI candidate verification','air/support performance and alternatives','human fun and physical Android/iOS');scope=$p.validationScope}
$report|ConvertTo-Json -Depth 8|Set-Content "$root/structure-review.json" -Encoding utf8
$report|ConvertTo-Json -Depth 5
'PASS10chapters/240budget/7towers/126loadouts/currentcosts/productunchanged;not battle/balance validation'
