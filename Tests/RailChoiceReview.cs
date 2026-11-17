using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gatefall.Core;

namespace Gatefall.Tests
{
    // Chapter 3 difficulty and investment choices on the actual combat core.
    // Every policy plays the same operation with the same seed, account and spending allowance: at any moment a
    // policy may have spent at most Budget x (everything earned so far). Only the way the allowance is used differs:
    //   expand   more Lv1 towers on empty pads, never an upgrade (보충)
    //   upgrade  six towers, then Lv2/Lv3 before any further site (강화)
    //   mixed    all fourteen towers, then Lv2 (the earlier baseline plan)
    //   fusion   the same opening, materials to Lv2, the best chain the operation has unlocked, refilled pads (조합)
    //   spear    fourteen Lv1 spears nearest the core: the decision-free bot of the chapter 1 gate
    // The hunter stands at the barrier and auto-fires. A scripted lower/upper reference, not a human rating.
    public static class RailChoiceReview
    {
        public static readonly int[] Seeds = { 7, 19, 41 };
        public static readonly float[] Budgets = { .5f, .6f, .7f, .8f, .9f, 1f };
        public static readonly string[] Policies = { "spear", "expand", "upgrade", "mixed", "fusion" };

        public sealed class Row
        {
            public string Stage, Policy, Outcome, Chain;
            public int Seed, Stars, Spent, Earned, Towers, Fusions, Kills, Total, Grade;
            public float Budget, MinimumCore;
            public double End;
            public float[] CoreDamageByLane;
            public string[] Purchases;
        }

        // Recommended account for the chapter. Set Unimproved to replay with the chapter 1 gate's bare account.
        public static bool Unimproved;
        // Crisis operations multiply every enemy's health by 1.25 and have no wave calls; report-only here.
        public static bool Crisis;
        // Report-only: the fusion policy stops after its first fusion and spends the rest like the mixed plan.
        public static bool SingleFusion;
        public static int Chapter = 3;
        public static int SupplyGrade = 1;
        public static CampaignBattle Battle(int stage)
        {
            var p = Unimproved ? new CampaignProfile() : new CampaignProfile { Growth = new[]{6,4,3}, Research = new[]{4,2,2,2} };
            for (int i = 0; i < stage; i++) { p.BestStars[i] = 3; p.FirstCleared[i] = true; p.BestErosion[i] = 0; }
            for (int i = 0; i < p.Supply.Grades.Length; i++) { p.Supply.Grades[i] = SupplyGrade; p.Supply.Cards[i] = SupplyCatalog.RequiredCards(SupplyGrade); }
            // Spear, seal and collapse; the fourth slot takes the Piercer as soon as 3-2 has unlocked it.
            p.Loadout = 7 | (p.IsTowerUnlocked(TowerKind.Piercer) ? 1 << (int)TowerKind.Piercer : 1 << (int)TowerKind.Barrier);
            return new CampaignService(p, null).Battle(stage, Crisis);
        }

        static int Site(DefenseSimulation g, int lane, float fraction, HashSet<int> used)
        {
            V2[] road = g.Roads[lane];
            V2 point = BattlefieldLayouts.Along(road, BattlefieldLayouts.RoadLength(road)*fraction, out _);
            return Enumerable.Range(0, g.BoardSockets.Length).Where(s => !used.Contains(s))
                .OrderBy(s => V2.DistanceSquared(point, g.BoardSockets[s])).First();
        }

        sealed class Order { public int Site; public TowerKind Kind; }

        public static Row Play(int stage, int seed, string policy, float budget)
        {
            CampaignBattle battle = Battle(stage);
            var g = new DefenseSimulation(new GameBalance(), seed, battle);
            var row = new Row { Stage = CampaignCatalog.StageId(stage), Policy = policy, Seed = seed, Budget = budget, Grade = SupplyGrade,
                Total = g.Schedule.Sum(x => x.Count), Chain = "" };
            int[] lanes = g.Schedule.GroupBy(x => x.Lane).OrderBy(x => x.Min(y => y.Start)).Select(x => x.Key).ToArray();
            var used = new HashSet<int>();
            var orders = new List<Order>();
            Action<int,float,TowerKind> add = (lane, fraction, kind) => {
                if (orders.Count >= 14) return;
                int site = Site(g, lane, fraction, used); used.Add(site); orders.Add(new Order { Site = site, Kind = kind });
            };
            bool piercer = g.Allows(TowerKind.Piercer);
            bool surge = piercer && (g.ReachableTowerFamilies() & (1 << (int)TowerKind.Surge)) != 0;
            bool surgeSeal = surge && battle.SurgeSealUnlocked;
            if (policy == "spear") {
                foreach (int site in Enumerable.Range(0, g.BoardSockets.Length).OrderBy(s => g.BoardSockets[s].Length).ThenBy(s => s).Take(14))
                    orders.Add(new Order { Site = site, Kind = TowerKind.Autocannon });
            } else {
                // The same lane-aware order for every policy: one collapse and one spear per road, then alternating depth.
                foreach (int lane in lanes) { add(lane, .55f, TowerKind.Rupture); add(lane, .76f, TowerKind.Autocannon); }
                foreach (float portion in new[]{.36f,.87f,.48f,.66f,.26f,.92f,.16f,.60f})
                    foreach (int lane in lanes) add(lane, portion, portion <= .48f ? TowerKind.Rupture : TowerKind.Autocannon);
            }
            // Fusion materials: the first road's collapse and spear are already in the shared order.
            int collapse = policy == "spear" ? -1 : orders[0].Site, spear = policy == "spear" ? -1 : orders[1].Site, binding = -1, line = -1;
            if (policy == "fusion") {
                var taken = new HashSet<int>(orders.Select(o => o.Site));
                binding = Site(g, lanes[0], .50f, taken); taken.Add(binding);
                if (surge) line = Site(g, lanes[0], .42f, taken);
            }

            int next = 0, step = 0;
            Func<int,bool> afford = cost => g.Credits >= cost && g.Telemetry.CreditsSpent+cost <= budget*(g.Telemetry.CreditsSpent+g.Credits)+.001f;
            Func<int,bool> build = count => {
                bool any = false;
                while (next < Math.Min(count, orders.Count) && g.Towers.Count < g.TowerLimit) {
                    Order o = orders[next];
                    if (g.TowerAt(o.Site) != null) { next++; continue; }
                    if (!afford(TowerCatalog.Get(o.Kind).BuildCost)) break;
                    string why; if (!g.TryBuild(o.Site, o.Kind, null, out why)) throw new InvalidOperationException(why);
                    next++; any = true;
                }
                return any;
            };
            Func<int,int,bool> upgrade = (site, level) => {
                Tower t = g.TowerAt(site);
                if (t == null || t.Level >= level || t.Level >= g.TowerLevelCap(t.Kind)) return false;
                if (!afford(TowerCatalog.Get(t.Kind).UpgradePrice(t.Level))) return false;
                string why; if (!g.TryUpgrade(t.Id, null, out why)) throw new InvalidOperationException(why);
                return true;
            };
            Func<int,bool> upgradeAll = level => {
                bool any = false;
                foreach (Tower t in g.Towers.OrderBy(t => t.Id).ToArray()) while (upgrade(t.Socket, level)) any = true;
                return any;
            };
            Func<int,TowerKind,bool> place = (site, kind) => {
                if (g.TowerAt(site) != null) return true;
                if (g.Towers.Count >= g.TowerLimit || !afford(TowerCatalog.Get(kind).BuildCost)) return false;
                string why; if (!g.TryBuild(site, kind, null, out why)) throw new InvalidOperationException(why);
                return true;
            };
            Func<int,int,int,bool> fuse = (recipe, keep, consumed) => {
                Tower a = g.TowerAt(keep), b = g.TowerAt(consumed);
                if (a == null || b == null || a.Level < 2 || b.Level < 2 || g.Cores < 1 || !afford(TowerCatalog.Recipes[recipe].Credits)) return false;
                string why; if (!g.TryFuse(recipe, a.Id, b.Id, keep, null, out why)) throw new InvalidOperationException(why);
                row.Chain += (row.Chain == "" ? "" : " > ")+TowerCatalog.Recipes[recipe].Result+"@"+g.Elapsed.ToString("F0");
                return true;
            };
            // Credits still owed to bring a material to Lv2 (building it first when the pad is empty).
            Func<int,TowerKind,int> owed = (site, kind) => {
                Tower t = g.TowerAt(site);
                if (t == null) return TowerCatalog.Get(kind).BuildCost+TowerCatalog.Get(kind).UpgradeCost;
                return t.Level >= 2 ? 0 : TowerCatalog.Get(t.Kind).UpgradeCost;
            };
            Action invest = () => {
                switch (policy) {
                case "spear": case "expand": build(14); break;
                case "upgrade":
                    build(6);
                    if (next >= Math.Min(6, orders.Count)) { if (!upgradeAll(2)) if (!upgradeAll(3)) build(14); }
                    break;
                case "mixed":
                    build(14);
                    if (next >= orders.Count || g.Towers.Count >= g.TowerLimit) upgradeAll(2);
                    break;
                case "fusion":
                    build(6);
                    if (next < Math.Min(6, orders.Count)) break;
                    int reserve = 0;
                    if (step == 0) {
                        if (surge) {
                            // 관통 + 봉인 -> 쇄도 on the Piercer pad; the seal pad is refilled with a spear.
                            if (place(line, TowerKind.Piercer)) upgrade(line, 2);
                            if (place(binding, TowerKind.Binding)) upgrade(binding, 2);
                            reserve = owed(line, TowerKind.Piercer)+owed(binding, TowerKind.Binding)+50;
                            if (fuse(11, line, binding)) step = 1;
                        } else {
                            // 붕괴 + 창 -> 파쇄 on the collapse pad; the spear pad is refilled.
                            upgrade(collapse, 2); upgrade(spear, 2);
                            reserve = owed(collapse, TowerKind.Rupture)+owed(spear, TowerKind.Autocannon)+45;
                            if (fuse(1, collapse, spear)) step = 1;
                        }
                    }
                    if (step == 1) {
                        if (surge) {
                            place(binding, TowerKind.Autocannon);
                            if (!surgeSeal || SingleFusion) step = 3;
                            else {
                                // 쇄도 Lv2 + 붕괴 Lv2 -> 종결 on the collapse pad; the Piercer pad is refilled.
                                upgrade(line, 2); upgrade(collapse, 2);
                                reserve = owed(line, TowerKind.Surge)+owed(collapse, TowerKind.Rupture)+80;
                                if (fuse(12, collapse, line)) step = 2;
                            }
                        } else {
                            // 파쇄 Lv2 + 봉인 Lv2 -> 종결 on the 파쇄 pad; both consumed pads are refilled with spears.
                            place(spear, TowerKind.Autocannon);
                            if (SingleFusion) { if (g.TowerAt(spear) != null) step = 3; break; }
                            if (place(binding, TowerKind.Binding)) upgrade(binding, 2);
                            upgrade(collapse, 2);
                            reserve = owed(collapse, TowerKind.Shatter)+owed(binding, TowerKind.Binding)+75;
                            if (fuse(4, collapse, binding)) step = 2;
                        }
                    }
                    if (step == 2 && place(surge ? line : binding, surge ? TowerKind.Piercer : TowerKind.Autocannon)) step = 3;
                    if (step >= 3) { build(14); if (next >= orders.Count || g.Towers.Count >= g.TowerLimit) upgradeAll(2); }
                    else while (next < orders.Count && g.Towers.Count < g.TowerLimit-1 && g.Credits >= reserve+TowerCatalog.Get(orders[next].Kind).BuildCost) {
                        int before = next; build(next+1); if (next == before) break;
                    }
                    break;
                }
            };

            invest(); g.Start();
            while (g.State != RunState.Won && g.State != RunState.Lost && g.Elapsed < 500) {
                if (g.AwaitingCard) {
                    // Neutral picks first so a card does not favour one policy's tower mix.
                    CardKind pick = g.CardOffers.Contains(CardKind.Range) ? CardKind.Range : g.CardOffers.Contains(CardKind.Hunter) ? CardKind.Hunter : g.CardOffers[0];
                    g.ChooseCard(pick);
                }
                invest();
                g.Tick(1f/30, new V2()); g.Events.Clear();
            }
            bool won = g.State == RunState.Won;
            row.Outcome = g.State.ToString(); row.Stars = CampaignCatalog.Stars(won, g.MinimumCoreHealth, g.Balance.CoreHealth);
            row.MinimumCore = g.MinimumCoreHealth; row.End = Math.Round(g.Elapsed, 1); row.Spent = g.Telemetry.CreditsSpent;
            row.Earned = g.Telemetry.CreditsSpent+g.Credits; row.Towers = g.Towers.Count; row.Fusions = g.Telemetry.Fusions; row.Kills = g.Kills;
            row.CoreDamageByLane = (float[])g.Telemetry.CoreDamageByLane.Clone(); row.Purchases = g.Telemetry.Purchases.ToArray();
            return row;
        }

        public static Row[] Run(string[] stageIds = null, float[] budgets = null, string[] policies = null)
        {
            foreach (CampaignStage stage in CampaignCatalog.Stages) BattlefieldLayouts.Get(stage); // the layout cache is not thread-safe
            var cases = new List<Func<Row>>();
            for (int i = CampaignCatalog.ChapterStart(Chapter); i < CampaignCatalog.ChapterStart(Chapter)+CampaignCatalog.ChapterCount(Chapter); i++) {
                if (stageIds != null && !stageIds.Contains(CampaignCatalog.Stages[i].Id)) continue;
                int stage = i;
                foreach (string policy in policies ?? Policies)
                    foreach (float budget in policy == "spear" ? new[]{1f} : budgets ?? Budgets)
                        foreach (int seed in Seeds) { string p = policy; float b = budget; int s = seed; cases.Add(() => Play(stage, s, p, b)); }
            }
            var rows = new Row[cases.Count];
            Parallel.For(0, cases.Count, i => rows[i] = cases[i]());
            return rows;
        }

        // One line per operation and policy: stars for seeds 7/19/41 at each allowance (L = loss).
        public static string Summary(Row[] rows)
        {
            var lines = new List<string>();
            foreach (var stage in rows.GroupBy(r => r.Stage)) {
                lines.Add(stage.Key+"  (enemies "+stage.First().Total+")");
                foreach (var policy in stage.GroupBy(r => r.Policy)) {
                    string cells = string.Join("  ", policy.GroupBy(r => r.Budget).OrderBy(b => b.Key).Select(b =>
                        (b.Key*100).ToString("0")+"%:"+string.Concat(b.OrderBy(r => r.Seed).Select(r => r.Outcome == "Won" ? r.Stars.ToString() : "L"))+
                        "/"+b.Min(r => r.Outcome == "Won" ? r.MinimumCore : 0).ToString("0")));
                    lines.Add("  "+policy.Key.PadRight(8)+cells);
                }
            }
            return string.Join("\n", lines);
        }
    }
}
