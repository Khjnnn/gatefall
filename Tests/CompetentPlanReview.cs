using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Gatefall.Core;

namespace Gatefall.Tests
{
    // Upper reference next to DegenerateStrategyReview. A scripted planner reads the roads and the published wave
    // schedule, spreads coverage by lane threat, mixes tower roles, upgrades and fuses; the hunter stays idle
    // (a player adds movement, skills and wave calls on top). A grid of plans is searched per operation and the
    // best plan is reported, so "best" is a planner with hindsight, not a human. Review-only.
    public static class CompetentPlanReview
    {
        public sealed class Plan
        {
            public float Rupture, Binding, Spread, Cap;
            public int Upgrade; // 0 never, 1 once the tower cap or the board is full, 2 alternate with construction
            public bool Fuse;
            public bool SpearOnly { get { return Rupture == 0 && Binding == 0; } }
            public override string ToString()
            { return "rupture " + Rupture.ToString("0.##") + " binding " + Binding.ToString("0.##") + " spread " + Spread + " cap " + Cap.ToString("0.##") + " upgrade " + Upgrade + (Fuse ? " fuse" : ""); }
        }
        public sealed class Row
        {
            public string Stage, Best, BestPlan, BestSpearOnly, BestNoUpgrade;
            public int StageIndex, Plans, ThreeStarPlans, WinningPlans, BestStars;
            public float BestMinimumCore;
        }
        sealed class Score { public Plan Plan; public int MinStars; public float MinCore; public string Seeds; }

        public static readonly int[] Seeds = { 7, 19, 41 };

        public static List<Plan> Grid()
        {
            var plans = new List<Plan>();
            foreach (float rupture in new[] { 0f, .25f, .4f })
            foreach (float binding in new[] { 0f, .15f })
            foreach (float spread in new[] { .5f, 3f })
            foreach (float cap in new[] { 1f, .6f })
            foreach (int upgrade in new[] { 0, 1, 2 })
            foreach (bool fuse in new[] { false, true }) {
                if (fuse && upgrade == 0) continue; // fusion needs Lv2 materials
                plans.Add(new Plan { Rupture = rupture, Binding = binding, Spread = spread, Cap = cap, Upgrade = upgrade, Fuse = fuse });
            }
            return plans;
        }

        public static Row[] Run(int fromStage)
        {
            foreach (CampaignStage stage in CampaignCatalog.Stages) BattlefieldLayouts.Get(stage); // the layout cache is not thread-safe
            List<Plan> grid = Grid();
            var rows = new List<Row>();
            for (int stage = Math.Max(0, fromStage - 1); stage < CampaignCatalog.Stages.Length; stage++) {
                var scores = new Score[grid.Count];
                int index = stage;
                Parallel.For(0, grid.Count, i => {
                    var score = new Score { Plan = grid[i], MinStars = 3, MinCore = float.MaxValue, Seeds = "" };
                    foreach (int seed in Seeds) {
                        DefenseSimulation g = Play(index, seed, grid[i]);
                        bool won = g.State == RunState.Won;
                        int stars = CampaignCatalog.Stars(won, g.MinimumCoreHealth, g.Balance.CoreHealth);
                        score.MinStars = Math.Min(score.MinStars, stars);
                        score.MinCore = Math.Min(score.MinCore, won ? g.MinimumCoreHealth : -1);
                        score.Seeds += won ? stars.ToString() : "L";
                    }
                    scores[i] = score;
                });
                Score best = null, spearOnly = null, noUpgrade = null; int three = 0, winning = 0;
                foreach (Score s in scores) {
                    if (s.MinStars == 3) three++;
                    if (s.MinStars >= 1) winning++;
                    if (Better(s, best)) best = s;
                    if (s.Plan.SpearOnly && Better(s, spearOnly)) spearOnly = s;
                    if (s.Plan.Upgrade == 0 && Better(s, noUpgrade)) noUpgrade = s;
                }
                rows.Add(new Row {
                    Stage = CampaignCatalog.StageId(stage), StageIndex = stage, Plans = grid.Count, ThreeStarPlans = three, WinningPlans = winning,
                    Best = Describe(best), BestPlan = best.Plan.ToString(), BestStars = best.MinStars, BestMinimumCore = best.MinCore,
                    BestSpearOnly = Describe(spearOnly), BestNoUpgrade = Describe(noUpgrade)
                });
            }
            return rows.ToArray();
        }
        static bool Better(Score a, Score b) { return b == null || a.MinStars > b.MinStars || a.MinStars == b.MinStars && a.MinCore > b.MinCore; }
        static string Describe(Score s) { return s == null ? "-" : s.Seeds + " (core " + s.MinCore.ToString("F0") + ")"; }

        public static string Summary(Row[] rows)
        {
            var lines = new List<string>();
            foreach (Row r in rows)
                lines.Add(r.Stage.PadRight(5) + " best " + r.Best + " · 3-star plans " + r.ThreeStarPlans + "/" + r.Plans + " · winning plans " + r.WinningPlans + "/" + r.Plans +
                    " · spear only " + r.BestSpearOnly + " · no upgrades " + r.BestNoUpgrade + " · [" + r.BestPlan + "]");
            return string.Join("\n", lines);
        }

        // Operations (1-based, from fromStage) where no plan earns three stars on every seed.
        public static string[] Violations(Row[] rows, int fromStage)
        {
            var found = new List<string>();
            foreach (Row r in rows) if (r.StageIndex + 1 >= fromStage && r.BestStars < 3) found.Add(r.Stage + ": best plan " + r.Best);
            return found.ToArray();
        }

        static float ScheduleWeight(SpawnGroup group)
        {
            if (group.Swarm) return 12;
            switch (group.Kind) {
                case EnemyKind.Grunt: return 10;
                case EnemyKind.Runner: return 30;
                case EnemyKind.Brute: return 300;
                case EnemyKind.Siege: return 550;
                default: return 900;
            }
        }

        public static DefenseSimulation Play(int stage, int seed, Plan plan, bool crisis = false,
            int spendingLimit = int.MaxValue, CampaignBattle battle = null, Action<DefenseSimulation> afterTick = null,
            Func<DefenseSimulation, V2> movement = null)
        {
            if (spendingLimit < 0) throw new ArgumentOutOfRangeException(nameof(spendingLimit));
            var g = new DefenseSimulation(new GameBalance(), seed, battle ?? new CampaignBattle { Stage = CampaignCatalog.Stages[stage], Crisis = crisis });
            int spent = 0;
            int lanes = g.Roads.Length, sites = g.BoardSockets.Length;
            // Lane threat from the published schedule.
            var weight = new float[lanes]; float total = 0;
            foreach (SpawnGroup group in g.Schedule) { float w = group.Count * ScheduleWeight(group); weight[group.Lane] += w; total += w; }
            for (int lane = 0; lane < lanes; lane++) weight[lane] /= Math.Max(1e-6f, total);
            // Road length inside each kind's range, per site and lane.
            TowerKind[] kinds = { TowerKind.Autocannon, TowerKind.Binding, TowerKind.Rupture };
            var coverage = new Dictionary<TowerKind, float[,]>(); var laneLength = new float[lanes];
            foreach (TowerKind kind in kinds) {
                float range = g.Stats(kind, 1).Range, r2 = range * range; var c = new float[sites, lanes];
                foreach (int lane in g.OpenLanes) {
                    V2[] road = g.Roads[lane]; float length = BattlefieldLayouts.RoadLength(road); laneLength[lane] = length; int node;
                    for (float d = 0; d < length - 1.1f; d += .25f) {
                        V2 point = BattlefieldLayouts.Along(road, d, out node);
                        for (int s = 0; s < sites; s++) if (V2.DistanceSquared(point, g.BoardSockets[s]) <= r2) c[s, lane] += .25f;
                    }
                }
                coverage[kind] = c;
            }
            var assigned = new float[lanes];
            var built = new Dictionary<TowerKind, int> { { TowerKind.Autocannon, 0 }, { TowerKind.Binding, 0 }, { TowerKind.Rupture, 0 } };
            int cap = (int)Math.Ceiling(plan.Cap * sites), constructions = 0, upgrades = 0; string why;
            if (g.TowerLimit > 0) cap = Math.Min(cap, g.TowerLimit); // the operation's tower limit also ends construction

            Func<int, TowerKind, float> value = (s, kind) => {
                float v = 0;
                foreach (int lane in g.OpenLanes) if (weight[lane] > 0)
                    v += weight[lane] * coverage[kind][s, lane] / (1 + assigned[lane] / (plan.Spread * weight[lane] * laneLength[lane] + 1e-3f));
                return v;
            };
            Func<int, float> siteValue = s => { float v = 0; foreach (int lane in g.OpenLanes) v += weight[lane] * coverage[TowerKind.Autocannon][s, lane]; return v; };
            Func<TowerKind> nextKind = () => {
                int n = 1; foreach (int count in built.Values) n += count;
                TowerKind best = TowerKind.Autocannon; float deficit = float.MinValue;
                // Ties go to the area and control roles first.
                foreach (TowerKind kind in new[] { TowerKind.Rupture, TowerKind.Binding, TowerKind.Autocannon }) {
                    float share = kind == TowerKind.Rupture ? plan.Rupture : kind == TowerKind.Binding ? plan.Binding : 1 - plan.Rupture - plan.Binding;
                    if (share <= 0 || !g.Allows(kind)) continue;
                    float d = share * n - built[kind]; if (d > deficit) { deficit = d; best = kind; }
                }
                return best;
            };
            Func<bool> construct = () => {
                if (g.Towers.Count >= cap) return false;
                TowerKind kind = nextKind(); int price = TowerCatalog.Get(kind).BuildCost;
                if (g.Credits < price || price > spendingLimit - spent) return false;
                int site = -1; float bestValue = -1;
                for (int s = 0; s < sites; s++) { if (g.TowerAt(s) != null) continue; float v = value(s, kind); if (v > bestValue) { bestValue = v; site = s; } }
                if (site < 0 || !g.TryBuild(site, kind, null, out why)) return false;
                foreach (int lane in g.OpenLanes) assigned[lane] += coverage[kind][site, lane];
                built[kind]++; constructions++; spent += price; return true;
            };
            Func<bool> upgrade = () => {
                Tower best = null; float bestValue = -1;
                foreach (Tower t in g.Towers) {
                    if (t.Kind == TowerKind.Barrier || g.CanUpgrade(t.Id) != "" ||
                        TowerCatalog.Get(t.Kind).UpgradePrice(t.Level) > spendingLimit - spent) continue;
                    float v = siteValue(t.Socket) / TowerCatalog.Get(t.Kind).UpgradePrice(t.Level); if (v > bestValue) { bestValue = v; best = t; }
                }
                if (best == null) return false;
                int price = TowerCatalog.Get(best.Kind).UpgradePrice(best.Level);
                if (!g.TryUpgrade(best.Id, null, out why)) return false;
                upgrades++; spent += price; return true;
            };
            Func<bool> fuse = () => {
                if (g.Cores < 1) return false;
                foreach (int recipe in new[] { 1, 0, 2, 3, 4, 5 }) {
                    FusionRecipe r = TowerCatalog.Recipes[recipe];
                    if (g.Credits < r.Credits || r.Credits > spendingLimit - spent) continue;
                    Tower first = null, second = null; float closest = float.MaxValue;
                    foreach (Tower a in g.Towers) foreach (Tower b in g.Towers) {
                        if (a.Kind != r.First || !r.Matches(a, b)) continue;
                        float d = V2.Distance(a.Position, b.Position); if (d < closest) { closest = d; first = a; second = b; }
                    }
                    if (first == null) continue;
                    int anchor = siteValue(first.Socket) >= siteValue(second.Socket) ? first.Socket : second.Socket;
                    if (g.TryFuse(recipe, first.Id, second.Id, anchor, null, out why)) { spent += r.Credits; return true; }
                }
                return false;
            };
            CardKind[] cardPreference = { CardKind.Ballistic, CardKind.Range, CardKind.Blast, CardKind.Control, CardKind.Credits, CardKind.Hunter };
            Action act = () => {
                if (g.AwaitingCard) { foreach (CardKind card in cardPreference) if (g.CardOffers.Contains(card)) { g.ChooseCard(card); break; } return; }
                for (int guard = 0; guard < 6; guard++) {
                    if (plan.Fuse && fuse()) continue;
                    bool full = g.Towers.Count >= Math.Min(cap, sites);
                    if (plan.Upgrade == 2 && constructions >= 4 && upgrades < constructions - 2 && upgrade()) continue;
                    if (construct()) continue;
                    if (plan.Upgrade >= 1 && (full || plan.Upgrade == 2) && upgrade()) continue;
                    break;
                }
            };
            act(); g.Start();
            while (g.State != RunState.Won && g.State != RunState.Lost && g.Elapsed < 400) { act(); g.Tick(1f / 30, movement == null ? new V2() : movement(g)); if(afterTick!=null)afterTick(g); g.Events.Clear(); }
            return g;
        }
    }
}
