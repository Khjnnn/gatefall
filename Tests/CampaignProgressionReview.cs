using System;
using System.Collections.Generic;
using System.Linq;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class CampaignProgressionReview
    {
        const float Step = .05f;
        const int CoreHealth = 300;
        static readonly int[] Seeds = { 7, 19, 41 };
        static readonly TowerKind Spear = TowerKind.Autocannon;
        static readonly TowerKind Binding = TowerKind.Binding;
        static readonly TowerKind Rupture = TowerKind.Rupture;

        public sealed class LaneRow
        {
            public int Lane, PadCount, ForwardPadCount, ForwardSealPadCount;
        }

        public sealed class StageRow
        {
            public string Stage;
            public int EarnedCores, SpentCores, UnspentCores, Growth, AllHunterGrowthCost, AllResearchCost;
            public int[] Research;
            public int Sockets, StartCredits, GuaranteedAt60, GuaranteedAt120, GuaranteedAt150, ReachableTowerMask;
            public double CatalystOne, CatalystTwo, FinalFirstSpawn, FinalLastSpawn;
            public LaneRow[] Lanes;
        }

        public sealed class RunRow
        {
            public string Stage, Policy, Profile, Outcome;
            public int Seed, Spent, Credits, Kills, PlannedKills, Towers, Stars, HunterDowns, SealShots;
            public double End, FirstFusion = -1, SealFusion = -1, SealFirstShot = -1, SealLastShot = -1;
            public float Core, SealDamage;
            public float[] LaneDamage;
            public string[] Purchases, Cards;
            public int[] MaterialSites, ReplacementSites;
        }

        public sealed class Result
        {
            public StageRow[] Stages;
            public RunRow[] Runs;
            public int[] RecipeCosts;
        }

        static CampaignService Profile(int stage, bool trained)
        {
            var p = new CampaignProfile();
            for (int i = 0; i < stage; i++) {
                p.FirstCleared[i] = true;
                p.BestStars[i] = 1;
                p.MagicCores += CampaignCatalog.Stages[i].FirstReward;
            }
            if (stage >= 10) p.Loadout = 263;
            var service = new CampaignService(p, null);
            if (!trained) return service;
            service.Research(0);
            while (service.Grow(0)) { }
            for (int line = 0; line < 4; line++) while (service.Research(line)) { }
            return service;
        }

        static DefenseSimulation New(int stage, int seed, bool trained, bool spawn = true, bool cards = true)
        {
            return new DefenseSimulation(new GameBalance { SpawnEnemies = spawn, OfferCards = cards },
                seed, Profile(stage, trained).Battle(stage, false));
        }

        static V2[] SampleRoad(V2[] road)
        {
            var points = new List<V2>();
            for (int i = 1; i < road.Length; i++) {
                var start = road[i - 1];
                var delta = road[i] - start;
                int sections = Math.Max(1, (int)Math.Ceiling(delta.Length / .25f));
                for (int j = 0; j < sections; j++) points.Add(start + delta * ((float)j / sections));
            }
            return points.ToArray();
        }

        static int Coverage(V2 socket, V2[] points, float radius, bool forward)
        {
            int length = forward ? (int)Math.Ceiling(points.Length * .7) : points.Length;
            int count = 0;
            for (int i = 0; i < length; i++)
                if (V2.DistanceSquared(socket, points[i]) <= radius * radius) count++;
            return count;
        }

        static V2[][] Roads(DefenseSimulation game) => game.Roads.Select(SampleRoad).ToArray();

        static int[] Order(DefenseSimulation game, V2[][] roads, IEnumerable<int> blocked = null)
        {
            var available = new HashSet<int>(Enumerable.Range(0, game.BoardSockets.Length));
            if (blocked != null) available.ExceptWith(blocked);
            var order = new List<int>();
            float range = game.Stats(Spear, 1).Range;
            while (available.Count > 0) {
                int previous = available.Count;
                foreach (int lane in game.OpenLanes) {
                    int best = available.OrderByDescending(s => Coverage(game.BoardSockets[s], roads[lane], range, true))
                        .ThenByDescending(s => Coverage(game.BoardSockets[s], roads[lane], range, false))
                        .ThenBy(s => s).FirstOrDefault(-1);
                    if (best >= 0 && Coverage(game.BoardSockets[best], roads[lane], range, false) > 0) {
                        available.Remove(best);
                        order.Add(best);
                    }
                }
                if (available.Count == previous) break;
            }
            return order.ToArray();
        }

        static StageRow Stage(int stage)
        {
            var game = New(stage, 7, false, false, false);
            var p = Profile(stage, true).Profile;
            var budget = Profile(stage, true);
            for (int hunter = 1; hunter < 3; hunter++) while (budget.Grow(hunter)) { }
            int earned = Profile(stage, false).Profile.MagicCores;
            var roads = Roads(game);
            var final = game.GetWaveBriefing(5);
            float baseRange = game.Stats(Spear, 1).Range;
            float sealRange = game.Stats(TowerKind.Seal, 1).Range;
            var row = new StageRow {
                Stage = CampaignCatalog.StageId(stage), EarnedCores = earned, SpentCores = earned - p.MagicCores,
                UnspentCores = p.MagicCores, Growth = p.Growth[0], Research = p.Research,
                AllResearchCost = budget.ResearchInvestment(),
                AllHunterGrowthCost = earned - budget.Profile.MagicCores - budget.ResearchInvestment(),
                Sockets = game.BoardSockets.Length, StartCredits = game.Credits,
                CatalystOne = -1, CatalystTwo = -1, FinalFirstSpawn = final.FirstSpawn, FinalLastSpawn = final.LastSpawn,
                ReachableTowerMask = game.ReachableTowerFamilies(),
                Lanes = game.OpenLanes.Select(lane => new LaneRow {
                    Lane = lane,
                    PadCount = game.BoardSockets.Count(s => Coverage(s, roads[lane], baseRange, false) > 0),
                    ForwardPadCount = game.BoardSockets.Count(s => Coverage(s, roads[lane], baseRange, true) > 0),
                    ForwardSealPadCount = game.BoardSockets.Count(s => Coverage(s, roads[lane], sealRange, true) > 0)
                }).ToArray()
            };
            game.Start();
            foreach (int time in new[] { 60, 120, 150 }) {
                int expectedCores = time == 60 ? 1 : 2;
                while (game.Elapsed < time) {
                    game.Tick(Step, new V2());
                    if (game.Cores == 1 && row.CatalystOne < 0) row.CatalystOne = game.Elapsed;
                    if (game.Cores == expectedCores && expectedCores == 2 && row.CatalystTwo < 0) row.CatalystTwo = game.Elapsed;
                    game.Events.Clear();
                }
                if (time == 60) row.GuaranteedAt60 = game.Credits;
                else if (time == 120) row.GuaranteedAt120 = game.Credits;
                else row.GuaranteedAt150 = game.Credits;
            }
            return row;
        }

        static bool Build(DefenseSimulation g, int site, TowerKind kind = TowerKind.Autocannon)
            => g.TryBuild(site, kind, null, out _);

        static bool Upgrade(DefenseSimulation g, int site)
            => g.TowerAt(site) is Tower tower && g.TryUpgrade(tower.Id, null, out _);

        static bool Fuse(DefenseSimulation g, int recipe, int anchor, int material)
            => g.TowerAt(anchor) is Tower a && g.TowerAt(material) is Tower b &&
                g.TryFuse(recipe, a.Id, b.Id, anchor, null, out _);

        static void Require(bool succeeded, string detail)
        {
            if (!succeeded) throw new Exception(detail);
        }

        static RunRow Run(int stage, int seed, bool trained, string policy)
        {
            var game = New(stage, seed, trained);
            var roads = Roads(game);
            var order = Order(game, roads);
            var row = new RunRow { Stage = CampaignCatalog.StageId(stage), Seed = seed, Profile = trained ? "funded" : "zero",
                Policy = policy, PlannedKills = game.Schedule.Sum(s => s.Count), MaterialSites = new int[0], ReplacementSites = new int[0] };
            var cards = new List<string>();
            int next = 0, step = 0;
            bool growthRun = policy == "ordinary" || policy == "lv1-only";
            int[] materials = new int[0];
            double nextAction = 0;
            int target = stage < 3 ? 5 : stage < 6 ? 7 : stage < 10 ? 8 : 9;
            if (growthRun) {
                int opening = stage < 3 ? 3 : Math.Min(6, 2 * game.OpenLanes.Length);
                for (; next < opening; next++) {
                    var kind = next == game.OpenLanes.Length && stage >= 3 ? Rupture : Spear;
                    Require(Build(game, order[next], kind), "ordinary prep " + row.Stage);
                }
                nextAction = 27;
            } else {
                bool north = policy.EndsWith("-north", StringComparison.Ordinal);
                int lane = north ? 1 : game.OpenLanes
                    .OrderByDescending(l => game.Schedule.Where(s => s.Lane == l && s.Wave >= 4).Sum(s => s.Count))
                    .ThenBy(l => l).First();
                var candidates = order.OrderByDescending(s => Coverage(game.BoardSockets[s], roads[lane], game.Stats(Spear, 1).Range, true)).ThenBy(s => s);
                materials = candidates.Take(3).ToArray();
                row.MaterialSites = materials;
                Require(Build(game, materials[0], Spear) && Build(game, materials[1], Binding) && Build(game, materials[2], Rupture), "recipe prep");
                order = Order(game, roads, materials);
                int support = (game.Credits - 10) / TowerCatalog.Get(Spear).BuildCost;
                for (; next < support; next++) Require(Build(game, order[next]), "support prep");
                nextAction = 31;
            }

            game.Start();
            bool makeSeal = policy.StartsWith("seal", StringComparison.Ordinal);
            bool spread = policy.StartsWith("wide", StringComparison.Ordinal);
            double[] recipeTimes = { 31, 34, 37, 63, 75, 91, 95, 98, 122, 125 };
            for (int frame = 0; frame < 7200 && game.State != RunState.Won && game.State != RunState.Lost; frame++) {
                if (game.AwaitingCard) {
                    var card = game.CardOffers[0];
                    Require(game.ChooseCard(card), "choose card");
                    cards.Add(card.ToString());
                }
                if (game.Elapsed >= nextAction) {
                    if (growthRun) {
                        bool built = next < target && Build(game, order[next]);
                        if (built) next++;
                        bool canLevel = policy != "lv1-only" && (!built || step % 2 == 1);
                        if (canLevel) {
                            var tower = game.Towers.FirstOrDefault(t => t.Level == 1 && game.CanUpgrade(t.Id) == "");
                            if (tower != null) Upgrade(game, tower.Socket);
                        }
                        step++;
                        nextAction = game.Elapsed + 11;
                    } else if (step < recipeTimes.Length) {
                        bool action;
                        switch (step) {
                            case 0: action = Upgrade(game, materials[0]); break;
                            case 1: action = Upgrade(game, materials[1]); break;
                            case 2: action = Upgrade(game, materials[2]); break;
                            case 3: action = Fuse(game, 0, materials[0], materials[1]); break;
                            case 4: action = Upgrade(game, materials[0]); break;
                            case 5: case 6: action = Build(game, order[next]); if (action) next++; break;
                            case 7: action = Build(game, materials[1]); break;
                            case 8:
                                if (makeSeal) {
                                    action = Fuse(game, 3, materials[0], materials[2]);
                                    if (action) row.SealFusion = game.Elapsed;
                                } else if (spread) {
                                    action = Build(game, order[next]);
                                    if (action) { row.ReplacementSites = new[] { order[next] }; next++; }
                                } else action = true;
                                break;
                            default:
                                action = !spread || Build(game, order[next]);
                                if (spread && action) row.ReplacementSites = row.ReplacementSites.Concat(new[] { order[next] }).ToArray();
                                break;
                        }
                        if (action) {
                            step++;
                            nextAction = step < recipeTimes.Length ? recipeTimes[step] : double.PositiveInfinity;
                        }
                    }
                }
                game.Tick(Step, new V2());
                foreach (var e in game.Events) {
                    if (e.Kind == EventKind.HunterDown) row.HunterDowns++;
                    if (e.Kind != EventKind.TowerShot || e.Source != (int)TowerKind.Seal) continue;
                    if (row.SealFirstShot < 0) row.SealFirstShot = game.Elapsed;
                    row.SealLastShot = game.Elapsed;
                    row.SealShots++;
                }
                game.Events.Clear();
            }
            row.Outcome = game.State.ToString();
            row.Spent = game.Telemetry.CreditsSpent; row.Credits = game.Credits;
            row.Kills = game.Kills; row.End = game.Elapsed; row.Core = game.MinimumCoreHealth;
            row.Stars = CampaignCatalog.Stars(game.State == RunState.Won, row.Core, CoreHealth);
            row.Towers = game.Towers.Count; row.FirstFusion = game.Telemetry.FirstFusion;
            row.SealDamage = game.Telemetry.TowerDamage[(int)TowerKind.Seal];
            row.LaneDamage = game.Telemetry.CoreDamageByLane;
            row.Purchases = game.Telemetry.Purchases.ToArray(); row.Cards = cards.ToArray();
            return row;
        }

        static int Cost(FusionRecipe recipe)
            => Material(recipe.First) + Material(recipe.Second) + recipe.Credits;

        static int Material(TowerKind kind)
        {
            var d = TowerCatalog.Get(kind);
            return d.UpgradePrice(1) + (d.Tier == 1 ? d.BuildCost : TowerCatalog.Recipes.Where(r => r.Result == kind).Min(Cost));
        }

        public static Result Review()
        {
            int count = CampaignCatalog.Stages.Length;
            var runs = new List<RunRow>();
            for (int stage = 0; stage < count; stage++)
                foreach (var seed in Seeds) {
                    runs.Add(Run(stage, seed, false, "ordinary"));
                    runs.Add(Run(stage, seed, true, "ordinary"));
                    runs.Add(Run(stage, seed, true, "lv1-only"));
                }
            foreach (int stage in new[] { 4, 5, 8, 9, 10, 11, 12, 13 })
                foreach (var seed in Seeds)
                    foreach (var policy in new[] { "hold", "seal", "wide" })
                        runs.Add(Run(stage, seed, true, policy));
            foreach (int seed in Seeds)
                foreach (var policy in new[] { "hold-north", "seal-north", "wide-north" })
                    runs.Add(Run(13, seed, true, policy));
            return new Result { Stages = Enumerable.Range(0, count).Select(Stage).ToArray(),
                RecipeCosts = TowerCatalog.Recipes.Select(Cost).ToArray(), Runs = runs.ToArray() };
        }
    }
}

