$ErrorActionPreference = 'Stop'
$taskProject = Split-Path -Parent $PSScriptRoot
$dataPath = Join-Path $taskProject 'Docs/Design/campaign-foundation-v2.json'
$campaign = Get-Content -LiteralPath $dataPath -Raw | ConvertFrom-Json
$checks = [System.Collections.Generic.List[string]]::new()
function Assert-Campaign([bool]$condition, [string]$message) {
    if (-not $condition) { throw $message }
    $checks.Add($message)
}
Assert-Campaign ($campaign.status -eq 'design_not_runtime' -and $campaign.schema_version -eq 2) 'v2 is design data, separate from shipped runtime'
Assert-Campaign ($campaign.world.city -eq '라온시' -and $campaign.world.protagonist -eq '강윤') 'Active city and protagonist match the supplied setting'
Assert-Campaign ($campaign.world.years_since_fall -eq 7 -and $campaign.chapters.Count -eq 7) 'Seven towers and seven years since the fall'
Assert-Campaign ($campaign.scope.long_term_districts -eq $campaign.chapters.Count) 'Scope matches chapter count'
Assert-Campaign (@($campaign.chapters.tower_id | Sort-Object -Unique).Count -eq 7) 'Each chapter has a distinct tower'
Assert-Campaign (($campaign.chapters.id -join ',') -eq '1,2,3,4,5,6,7') 'Chapters are ordered without missing IDs'
Assert-Campaign (($campaign.chapters.act -join ',') -eq '1,1,2,2,2,3,3') 'Three acts match chapter boundaries'
Assert-Campaign ($campaign.stages.Count -eq 6 -and @($campaign.stages.id | Sort-Object -Unique).Count -eq 6) 'First chapter has six unique stages'
Assert-Campaign ($campaign.base_towers.Count -eq 4) 'Four base ward families are defined'
$baseIds = @($campaign.base_towers.id)
$resultIds = @($campaign.fusion.results.id)
$knownIds = $baseIds + $resultIds
Assert-Campaign (@($knownIds | Sort-Object -Unique).Count -eq $knownIds.Count) 'Base and fusion IDs are distinct'
$investments = @{}
$catalystInvestments = @{}
foreach ($tower in $campaign.base_towers) {
    Assert-Campaign ($tower.build -gt 0 -and $tower.upgrade -gt 0) "$($tower.id): positive build and upgrade costs"
    $investments[$tower.id] = $tower.build + $tower.upgrade
    $catalystInvestments[$tower.id] = 0
}
$recipePairs = [System.Collections.Generic.HashSet[string]]::new()
$sealCosts = [System.Collections.Generic.List[int]]::new()
Assert-Campaign ($campaign.fusion.recipes.Count -eq 6 -and $campaign.fusion.consumes_materials -and $campaign.fusion.material_level -eq 2) 'Six consuming recipes retain the Lv2 material rule'
foreach ($recipe in $campaign.fusion.recipes) {
    $key = (@($recipe.a,$recipe.b) | Sort-Object) -join ':'
    Assert-Campaign ($recipePairs.Add($key)) "$($recipe.id): unordered material pair is unique"
    Assert-Campaign ($investments.ContainsKey($recipe.a) -and $investments.ContainsKey($recipe.b) -and $resultIds -contains $recipe.result) "$($recipe.id): ingredients and result are defined in dependency order"
    Assert-Campaign ($recipe.cost -gt 0 -and $recipe.catalysts -eq 1) "$($recipe.id): explicit positive price and one catalyst"
    $cost = $investments[$recipe.a] + $investments[$recipe.b] + $recipe.cost
    $cores = $catalystInvestments[$recipe.a] + $catalystInvestments[$recipe.b] + $recipe.catalysts
    if ($recipe.result -eq 'Seal') {
        $sealCosts.Add($cost)
        Assert-Campaign ($cost -eq $campaign.economy.capstone_total -and $cores -eq $campaign.economy.capstone_cores) "$($recipe.id): cumulative capstone cost is455 and two catalysts"
    } else {
        $investments[$recipe.result] = $cost + $campaign.fusion.result_upgrade_cost
        $catalystInvestments[$recipe.result] = $cores
        if ($recipe.result -eq 'Arc') {
            Assert-Campaign ($cost -eq $campaign.economy.first_arc_total) 'First Arc cumulative investment is195'
        }
    }
}
Assert-Campaign ($sealCosts.Count -eq 3) 'Three capstone routes remain available'
Assert-Campaign (-not $campaign.fusion.requires_material_adjacency -and $campaign.fusion.requires_anchor_interaction_range) 'Fusion range differs from adjacency resonance'
foreach ($result in $campaign.fusion.results) {
    Assert-Campaign (@($result.research_tags | Where-Object { $baseIds -notcontains $_ }).Count -eq 0) "$($result.id): research tags refer to defined families"
    Assert-Campaign (@($result.research_tags | Sort-Object -Unique).Count -eq $result.research_tags.Count) "$($result.id): research tags do not duplicate effects"
}
$unlocked = [System.Collections.Generic.HashSet[string]]::new()
foreach ($item in $campaign.initial_unlocks) { [void]$unlocked.Add($item) }
$previous = $null
foreach ($stage in $campaign.stages) {
    Assert-Campaign ($stage.previous -eq $previous -and $stage.chapter -eq 1) "$($stage.id): prerequisite chain is valid"
    Assert-Campaign ($stage.min_previous_stars -eq $(if($previous){1}else{0})) "$($stage.id): basic clear opens the main path"
    Assert-Campaign ($stage.duration_seconds -eq 180 -and $stage.wave_counts.Count -eq 6) "$($stage.id): nominal spawn budget and six wave budgets"
    Assert-Campaign (($stage.wave_counts | Measure-Object -Minimum).Minimum -ge 0) "$($stage.id): no negative enemy counts"
    Assert-Campaign ($stage.sockets -ge 5 -and $stage.sockets -le 12 -and $stage.lanes.Count -ge 1 -and $stage.lanes.Count -le 3) "$($stage.id): socket and lane bounds"
    foreach ($tower in @($stage.base_towers)+@($stage.fusion_results)) {
        Assert-Campaign ($knownIds -contains $tower -and $unlocked.Contains($tower)) "$($stage.id): $tower is defined and unlocked before use"
    }
    Assert-Campaign ($stage.core_supply_enabled -eq ($stage.fusion_results.Count -gt 0) -and $stage.core_supply_enabled -eq $unlocked.Contains('FusionCatalyst')) "$($stage.id): catalysts unlock with fusion"
    Assert-Campaign ($stage.adjacency_enabled -eq $unlocked.Contains('Resonance')) "$($stage.id): adjacency starts after its introduction"
    $available = @($stage.base_towers)+@($stage.fusion_results)
    foreach ($result in $stage.fusion_results) {
        $routes = @($campaign.fusion.recipes | Where-Object result -eq $result)
        Assert-Campaign ($routes.Count -gt 0) "$($stage.id): $result has a recipe"
        foreach ($route in $routes) {
            Assert-Campaign (($available -contains $route.a) -and ($available -contains $route.b)) "$($stage.id): $($route.id) materials are available"
        }
    }
    Assert-Campaign ($stage.first_clear_magic_cores -gt $stage.repeat_clear_magic_cores -and $stage.repeat_clear_magic_cores -eq 2) "$($stage.id): first and repeat rewards are explicit"
    Assert-Campaign (-not $stage.mandatory_fusion -and $stage.rescue_points -eq 0) "$($stage.id): no mandatory fusion or early rescue gate"
    Assert-Campaign ($stage.permanent_tower_seal -eq $stage.require_boss_kill) "$($stage.id): permanent sealing belongs to the boss encounter"
    if ($stage.require_boss_kill) {
        Assert-Campaign ($stage.boss_count -eq 1 -and $stage.boss_spawn_seconds -eq 150 -and $stage.boss_spawn_seconds -lt $stage.duration_seconds) "$($stage.id): boss joins the final scheduled wave, cleanup has no deadline"
    }
    foreach ($item in $stage.clear_unlocks) { [void]$unlocked.Add($item) }
    $previous = $stage.id
}
Assert-Campaign (@($campaign.stages | Where-Object permanent_tower_seal).Count -eq 1) 'Only the first chapter finale permanently seals its tower'
Assert-Campaign ($campaign.adjacency.base_only -and $campaign.adjacency.same_family -and $campaign.adjacency.max_effect_stacks -eq 1 -and -not $campaign.adjacency.consumes_materials) 'Adjacency is bounded and does not consume towers'
Assert-Campaign ($campaign.adjacency.requires_active -and -not $campaign.adjacency.self_link -and $campaign.adjacency.radius -gt 0) 'Adjacency requires another active tower in finite range'
Assert-Campaign ($campaign.barrier.max_targets -eq 2 -and $campaign.barrier.shared_block_immunity_seconds -gt $campaign.barrier.base_block_seconds -and $campaign.barrier.no_physical_path_closure) 'Barrier blocking is finite with shared immunity and no physical route closure'
Assert-Campaign ($campaign.barrier.excluded -contains 'Boss' -and $campaign.barrier.excluded -contains 'Siege' -and $campaign.barrier.excluded -contains 'Flying') 'Unblockable roles remain explicit'
Assert-Campaign (-not $campaign.stars.is_currency -and -not $campaign.stars.optional_objectives_affect_stars -and -not $campaign.stars.healing_reduces_recorded_erosion) 'Stars use peak erosion, separate from objectives and currency'
Assert-Campaign ($campaign.stars.three_star_max_erosion -ge 0 -and $campaign.stars.three_star_max_erosion -lt $campaign.stars.two_star_max_erosion -and $campaign.stars.two_star_max_erosion -lt 1) 'Erosion thresholds are ordered'
# This checks the design's arithmetic, not the unimplemented Unity rating code.
function Get-DesignStars([decimal]$minimumHealth, [decimal]$startMax, [bool]$won) {
    if (-not $won) { return 0 }
    if ($minimumHealth -ge $startMax*(1-[decimal]$campaign.stars.three_star_max_erosion)) { return 3 }
    if ($minimumHealth -ge $startMax*(1-[decimal]$campaign.stars.two_star_max_erosion)) { return 2 }
    return 1
}
foreach ($sample in @(@(300,3),@(270,3),@(269,2),@(210,2),@(209,1),@(1,1))) {
    Assert-Campaign ((Get-DesignStars $sample[0] 300 $true) -eq $sample[1]) "Erosion design boundary: minimumHP $($sample[0])/300 gives$($sample[1]) stars"
}
Assert-Campaign ((Get-DesignStars 300 300 $false) -eq 0) 'Defeat overrides a healthy core'
$growth = $campaign.hunter_progression
Assert-Campaign ($growth.transition_costs.Count -eq $growth.grade_order.Count-1 -and $growth.legacy_grade_promotion_min_chapters.Count -eq $growth.grade_order.Count) 'Grade transitions and legacy references cover all grades'
Assert-Campaign (($growth.current_earned_step_caps -join ',') -eq '3,1,1' -and $growth.kang_yoon_third_step_requires -eq 'D1-S6') 'Current hunter growth follows implemented unlocks and the first seal'
Assert-Campaign ($growth.preserve_existing_earned_steps -and $growth.recruited_first_step_requires -eq 'hunter_unlocked') 'Recruited hunters can grow once without resetting old growth'
Assert-Campaign (($growth.grade_order -join ',') -eq 'F,E,D,C,B,A,S') 'Measured grades follow supplied order'
Assert-Campaign ($growth.controlled_hunters -eq 1 -and -not $growth.random_permanent_corruption) 'One controlled hunter and no random permanent corruption loss'
foreach ($hunter in $growth.hunters) {
    Assert-Campaign ($hunter.start_index -ge 0 -and $hunter.start_index -lt $growth.grade_order.Count -and $unlocked.Contains($hunter.id)) "$($hunter.name): valid starting grade and first-chapter unlock"
    if ($hunter.grade_mode -eq 'measured') {
        Assert-Campaign ($growth.grade_order[$hunter.start_index] -eq $hunter.display_grade) "$($hunter.name): recruitment preserves narrative grade"
    } else {
        Assert-Campaign ($hunter.display_grade -eq '미분류' -and $hunter.start_index -eq 0) 'Kang Yoon keeps unclassified status through stabilization'
    }
}
Assert-Campaign ($campaign.research.lines.Count -eq 4 -and $campaign.research.step_costs.Count -eq 3) 'Four families and three research ranks'
foreach ($line in $campaign.research.lines) {
    Assert-Campaign ($baseIds -contains $line.id -and $line.total_effect_by_rank.Count -eq 3) "$($line.id): research family and effects match ranks"
}
Assert-Campaign ($campaign.research.currency -eq '마핵' -and $growth.currency -eq '마핵' -and $campaign.economy.permanent_currency -eq '마핵') 'Magic cores are the shared permanent growth currency'
Assert-Campaign ($campaign.research.free_respec -and $campaign.research.refund -eq 'actual_research_spend_only') 'Free respec refunds only actual research spending'
$firstClearTotal = ($campaign.stages.first_clear_magic_cores | Measure-Object -Sum).Sum
$heroTotal = ($growth.transition_costs | Measure-Object -Sum).Sum
$researchTotal = ($campaign.research.step_costs | Measure-Object -Sum).Sum*$campaign.research.lines.Count
$chapterResearchRanks = @($campaign.research.rank_unlocks | Where-Object { $_ -match '^D1-' }).Count
$chapterResearch = ($campaign.research.step_costs[0..($chapterResearchRanks-1)] | Measure-Object -Sum).Sum*$campaign.research.lines.Count
$chapterHero = 0
for ($i=0; $i -lt $growth.current_earned_step_caps[0]; $i++) { $chapterHero += $growth.transition_costs[$i] }
$chapterRecruited = 0
for ($h=1; $h -lt $growth.hunters.Count; $h++) {
    for ($i=0; $i -lt $growth.current_earned_step_caps[$h]; $i++) { $chapterRecruited += $growth.transition_costs[$growth.hunters[$h].start_index+$i] }
}
$budget = $campaign.fullChapterBudget
Assert-Campaign ($firstClearTotal -eq 90 -and $firstClearTotal -eq $budget.first_clear_magic_cores) 'First-clear magic-core income totals90'
Assert-Campaign ($heroTotal -eq $budget.kang_yoon_all_stabilization_cost -and $researchTotal -eq $budget.all_research_cost) 'Full growth costs match derived74 and48'
Assert-Campaign ($chapterResearch -eq $budget.chapter1_available_research_cost -and $chapterHero -eq $budget.chapter1_available_kang_yoon_cost -and $chapterRecruited -eq $budget.chapter1_available_recruited_hunter_cost) 'First-seal research costs24, Kang Yoon18, recruited hunters30'
Assert-Campaign ($firstClearTotal-$chapterResearch-$chapterHero-$chapterRecruited -eq $budget.chapter1_remainder -and $budget.chapter1_remainder -ge 0) 'First six clears cover current growth and research with18 remaining'
Assert-Campaign ($heroTotal+$researchTotal -eq $budget.full_long_term_combined_cost) 'Long-term combined cost122 is distinct from first-chapter spending'
Assert-Campaign ($campaign.stages.Count*$campaign.stars.maximum_per_stage -eq $budget.max_stars) 'First chapter has18 non-spendable stars'
Assert-Campaign ($campaign.economy.starting_credits+2*$campaign.economy.supply_credits -ge $campaign.economy.first_arc_total) 'Guaranteed income by60s covers the first fusion'
Assert-Campaign ($campaign.economy.core_supply_times.Count*$campaign.economy.core_grant -eq $campaign.economy.capstone_cores) 'Scheduled catalyst supply covers the capstone'
Assert-Campaign ($campaign.economy.entry_cost -eq 0 -and $campaign.stars.main_path_star_requirement -eq 1) 'No entrance fee or high-star main-path gate'
Assert-Campaign (-not $campaign.economy.combat_credits_convert_to_permanent -and -not $campaign.economy.first_plus_repeat_same_run -and $campaign.economy.normal_enemy_magic_core_drop -eq 0) 'Combat credits, rare cores and reward modes are separated'
Assert-Campaign ($campaign.rescue.introduced_chapter -eq 2 -and -not $campaign.rescue.affects_stars -and $campaign.rescue.magic_core_reward -eq 0) 'Rescue is a chapter-two combat-credit objective'
Assert-Campaign ($campaign.cards.filter_locked_tower_tags -and $campaign.cards.filter_max_stacks -and -not $campaign.cards.pad_with_duplicates) 'Card pool excludes unavailable and exhausted options'
Assert-Campaign ($campaign.save.idempotent_run_reward -and $campaign.save.atomic_reward_unlock_story -and $campaign.save.backup -and -not $campaign.save.mid_battle_resume -and $campaign.save.retain_best_on_failure) 'Save requirements cover atomic rewards, backup and retained best records'
Assert-Campaign (($campaign.stages[-1].wave_counts | Measure-Object -Sum).Sum -eq 147 -and $campaign.stages[-1].boss_count -eq 1) 'Final proposed budget is147 normal enemies plus one boss'
$result = [ordered]@{
    status='passed'; schema_version=2; input='campaign-foundation-v2.json'
    input_sha256=(Get-FileHash -LiteralPath $dataPath -Algorithm SHA256).Hash.ToLowerInvariant()
    scope='Design structure and arithmetic only; no campaign runtime, visuals or combat balance tested'
    checked=(Get-Date).ToString('o'); check_count=$checks.Count
    first_clear_magic_cores=$firstClearTotal; max_unique_stars=$budget.max_stars
    total_research_cost=$researchTotal; full_kang_yoon_cost=$heroTotal
    chapter1_available_growth_cost=$chapterResearch+$chapterHero
    checks=$checks
}
$result | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $taskProject 'Docs/Design/campaign-foundation-validation.json') -Encoding utf8
Write-Output "PASS: $($checks.Count) Raon campaign dependency, fusion, rating and reward checks. Design only."

