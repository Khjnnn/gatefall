using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gatefall.Core;

namespace Gatefall.Core
{
    public sealed partial class DefenseSimulation
    {
        internal Action<Enemy, SpawnGroup> PrototypeSpawned;
        internal Action<Enemy, Projectile, float> PrototypeDamage;
        internal Action<int> PrototypeLineVolley;

        // Advance normal recharge progress faster while supported. A target change preserves its
        // remaining fraction; existing projectiles and the target's unmodified interval are untouched.
        internal float PrototypeRecharge(Tower tower)
        {
            if (!PrototypeEligible(tower)) return 1;
            float reduction = 0;
            foreach (Tower lamp in Towers) {
                if (lamp.Kind != TowerKind.Accelerator || lamp.Commission > 0) continue;
                float range = TowerCatalog.Get(lamp.Kind).Levels[lamp.Level - 1].Range;
                float distance = V2.DistanceSquared(tower.Position, lamp.Position);
                if (distance > range * range) continue;
                int nearer = 0;
                foreach (Tower other in Towers) {
                    if (other.Id == tower.Id || !PrototypeEligible(other)) continue;
                    float d = V2.DistanceSquared(other.Position, lamp.Position);
                    if (d < distance || d == distance && other.Socket < tower.Socket) nearer++;
                }
                if (nearer < 2) reduction = Math.Max(reduction, lamp.Level == 1 ? .10f : lamp.Level == 2 ? .15f : .18f);
            }
            return 1 / (1 - reduction);
        }

        static bool PrototypeEligible(Tower tower) => tower.Commission <= 0 &&
            tower.Kind != TowerKind.Accelerator && tower.Kind != TowerKind.Barrier &&
            tower.Kind != TowerKind.Binding && tower.Kind != TowerKind.Marker &&
            tower.Kind != TowerKind.Beacon && tower.Kind != TowerKind.Seal;
    }
}

namespace Gatefall.Tests
{
    public static class ChapterPrototypeReview
    {
        static readonly V2[] Empty = Array.Empty<V2>();
        static readonly TowerKind Spear = TowerKind.Autocannon, Bind = TowerKind.Binding, Blast = TowerKind.Rupture;
        static readonly int[] Research = { 3, 2, 2, 2 };
        static readonly int[] Seeds = { 7, 41 };
        const int MergeWave = 3;
        const float Step = 1f / 60;

        public static object Run(string output, string scope)
        {
            var representatives = new List<object>();
            var bosses = new List<object>();
            if (scope == "merge-followup") {
                foreach (int option in new[] { 0, 1 }) representatives.Add(Representative(Transfer(101), 19, option));
            } else if (scope != "bosses") {
                foreach (int seed in Seeds) {
                    foreach (int option in new[] { 0, 1 }) representatives.Add(Representative(Rail(100), seed, option));
                    foreach (int option in new[] { 0, 1 }) representatives.Add(Representative(Transfer(101), seed, option));
                    foreach (bool split in new[] { false, true })
                        foreach (int option in new[] { 0, 1 }) representatives.Add(Representative(Flood(split ? 103 : 102, split), seed, option));
                }
            }
            object saves = null;
            if (scope == "all" || scope == "bosses") {
                foreach (CampaignStage stage in new[] { RailBoss(), TransferBoss(), FloodBoss() }) {
                    DefenseSimulation game = NewGame(stage, 7);
                    bosses.Add(BossRun(game));
                }
                saves = Progression(output, bosses);
            }
            return new { Scope = scope, RepresentativeCount = representatives.Count, BossRunCount = bosses.Count, Representatives = representatives, Bosses = bosses, Progression = saves };
        }

        static void Check(bool ok, string message)
        {
            if (!ok) throw new InvalidOperationException(message);
        }

        static V2[] Path(params float[] xy)
        {
            var points = new V2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = new V2(xy[2 * i], xy[2 * i + 1]);
            return points;
        }

        static SpawnGroup G(int lane, int count, double delay = 0, double span = 14, EnemyKind kind = EnemyKind.Grunt) =>
            new SpawnGroup { Lane = lane, Count = count, Start = delay, Interval = count == 1 ? 1 : span / (count - 1), Kind = kind, Swarm = kind == EnemyKind.Grunt };

        static CampaignWave W(double start, params SpawnGroup[] groups) => new CampaignWave { Start = start, Groups = groups };
        static SpawnGroup R(int lane, int count, double delay, double span) => G(lane, count, delay, span, EnemyKind.Runner);
        static SpawnGroup B(int lane, int count, double delay) => G(lane, count, delay, 6, EnemyKind.Brute);
        static SpawnGroup S(int lane, int count, double delay) => G(lane, count, delay, 6, EnemyKind.Siege);
        static SpawnGroup Boss(int lane) => G(lane, 1, 0, 0, EnemyKind.Boss);

        static CampaignStage Stage(int index, int chapter, int number, string name, int count, V2[][] roads, V2[] fixedSockets, params CampaignWave[] waves)
        {
            string id = $"C{chapter:D2}-S{number:D3}";
            int[] lanes = Enumerable.Range(0, roads.Length).Where(lane => roads[lane].Length > 0).ToArray();
            foreach (int lane in lanes) {
                Check(roads[lane].Length >= 2 && roads[lane].Last().Length == 0, id + " invalid road");
                Check(roads[lane].All(p => Math.Abs(p.X) < 24 && Math.Abs(p.Y) < 12), id + " road outside board");
            }
            Check(waves.SelectMany(w => w.Groups).All(g => lanes.Contains(g.Lane)), id + " group without road");
            var sockets = new List<V2>();
            bool Legal(V2 site) => lanes.All(lane => BattlefieldLayouts.RoadDistance(site, roads[lane]) >= 2.15f) &&
                sockets.All(socket => V2.Distance(socket, site) >= 2.55f);
            foreach (V2 site in fixedSockets) {
                Check(Legal(site), id + " invalid fixed socket " + site.X + "," + site.Y);
                sockets.Add(site);
            }
            // Allocate remaining idle sites deterministically, close enough to at least one road.
            for (float range = 3.3f; range <= 4.2f && sockets.Count < count; range += .3f)
                for (float y = -10.6f; y <= 10.6f && sockets.Count < count; y += 2.6f)
                    for (float x = -21.5f; x <= 21.5f && sockets.Count < count; x += 2.6f) {
                        V2 site = new V2(x, y);
                        if (Legal(site) && lanes.Any(lane => BattlefieldLayouts.RoadDistance(site, roads[lane]) <= range))
                            sockets.Add(site);
                    }
            for (float y = -10.6f; y <= 10.6f && sockets.Count < count; y += 1.1f)
                for (float x = -21.5f; x <= 21.5f && sockets.Count < count; x += 1.1f) {
                    V2 site = new V2(x, y);
                    if (Legal(site) && lanes.Any(lane => BattlefieldLayouts.RoadDistance(site, roads[lane]) <= 5f))
                        sockets.Add(site);
                }
            Check(sockets.Count == count, id + " could not place planned sockets");
            bool hasBoss = waves.SelectMany(w => w.Groups).Any(g => g.Kind == EnemyKind.Boss);
            return new CampaignStage {
                Index = index, Chapter = chapter, Number = number, StableId = id, Name = name, ClearStory = name + " 확보",
                Lanes = lanes, SocketCount = count, TowerLimit = 14, Waves = waves, Counts = waves.Select(w => w.Groups.Sum(g => g.Count)).ToArray(),
                Layout = new BattlefieldLayout { Id = id, Theme = chapter - 2, Roads = roads, Sockets = sockets.ToArray() },
                BossRequired = hasBoss, BossEntryDistance = hasBoss ? 0f : (float?)null,
                HealthScale = 1.45f, SwarmHealth = 2.7f, Threat = 1.225f, EliteArmor = 6, HybridDamage = 1.25f,
                BindingLevel2Targets = 6, BindingLevel3Targets = 8
            };
        }

        static V2[] RailWest() => Path(-23,7,-18,7,-18,2,-2,2,-2,-5,3,-5,3,0,0,0);
        static V2[] RailNorth() => Path(6,11,6,8,14,8,14,3,5,3,5,-1,0,-1,0,0);
        static CampaignStage Rail(int index) => Stage(index, 3, 6, "환승 앞 분기장", 28,
            new[] { RailWest(), RailNorth(), Path(21,-1,0,-1,0,0), Empty },
            Path(-20.3f,2,-4.4f,-3,-10,4.4f,9,-3.5f,9,5.6f,6.2f,-3.6f,16,1.6f),
            W(5,G(0,72)), W(35,G(0,60,0,12),R(2,14,5,8)), W(65,G(1,60,0,12),B(0,3,6),G(2,26,4,9)),
            W(95,G(0,72),R(2,18,7,8),S(1,2,4)), W(125,G(1,68),S(2,3,3),R(0,16,6,8)),
            W(155,G(2,48,0,12),G(0,60,3,14),B(1,3,4)), W(185,G(1,62,0,12),G(0,48,5,12),R(2,16,8,8)),
            W(215,G(0,72),G(1,56,5,12),S(2,3,6)));

        static V2[] TransferWest() => Path(-23,-9,-10,-9,-10,5.5f,-7,5.5f,-7,0,-7,-4,0,-4,0,0);
        static V2[] TransferNorth() => Path(-3,8,-3,0,-7,0,-7,-4,0,-4,0,0);
        static CampaignStage Transfer(int index) => Stage(index, 4, 6, "교차 환승실", 28,
            new[] { TransferWest(), TransferNorth(), Path(23,-1,14,-1,14,-6,9,-6,9,-9,0,-9,0,0), Empty },
            Path(-.6f,5,-2.4f,-6.3f,3,-6,2.7f,-2,-5,-6.4f,11.6f,-3.6f),
            W(5,G(0,72)), W(35,G(0,68,0,13),G(0,2,10,4,EnemyKind.Brute)), W(65,G(0,88,0,16)),
            W(95,G(0,42,0,5),G(0,30,10,7),R(1,18,18,5)), W(125,G(1,64,0,13),B(0,3,6)),
            W(155,G(0,64,0,12),S(2,3,3),R(1,18,8,8)), W(185,G(2,72,0,12),G(0,54,5,10)),
            W(215,G(0,72),R(1,20,8,8),S(2,3,2)));

        static V2[] FloodWest() => Path(-23,3,-14,3,-14,2,4,2,4,6,0,6,0,0);
        static V2[] FloodSouth() => Path(4,-8,4,2,4,6,0,6,0,0);
        static V2[] FloodEast(bool split) => split ? Path(23,-5,15,-5,15,-9,-5,-9,-5,-2,0,-2,0,0) :
            Path(23,-5,1,-5,1,2,4,2,4,6,0,6,0,0);
        static CampaignStage Flood(int index, bool split)
        {
            // Use the same full board in both conditions; candidates must fit beside both east roads.
            var roads = new[] { FloodWest(), Empty, FloodEast(false), FloodSouth() };
            var sites = Path(6.4f,2.5f,6.4f,5.5f,6.6f,-11.4f,6.7f,-2.5f,1.6f,-11.4f,8.9f,4,-2.6f,-4.2f,-8,4.5f);
            var splitRoad = FloodEast(true);
            var shared = Stage(index, 5, 6, "가변 수문", 28, roads, sites,
                W(5,G(0,68),R(3,12,8,6)), W(35,G(0,56,0,12),G(3,34,4,8)), W(65,G(0,68),B(3,3,4),R(3,14,10,6)),
                W(95,G(0,78),S(3,2,3),R(3,14,8,6)), W(125,G(2,52,0,12),G(0,62,4,13),S(2,2,8)),
                W(155,G(0,72,0,13),R(2,18,5,8),B(2,3,8)), W(185,G(2,72,0,12),G(0,44,6,9),S(0,3,8)),
                W(215,G(0,76),G(2,48,5,12),S(2,3,6)));
            var valid = shared.Layout.Sockets.Where(p => BattlefieldLayouts.RoadDistance(p, splitRoad) >= 2.15f).ToList();
            Check(sites.All(p => valid.Contains(p)), "5-6 fixed socket conflicts with split east road");
            for (float y = -10.6f; y < 11 && valid.Count < 28; y += 2.6f)
                for (float x = -21.5f; x < 22 && valid.Count < 28; x += 2.6f) {
                    var site = new V2(x, y);
                    var distances = roads.Where(r => r.Length > 0).Select(r => BattlefieldLayouts.RoadDistance(site, r))
                        .Concat(new[] { BattlefieldLayouts.RoadDistance(site, splitRoad) }).ToArray();
                    if (distances.Min() >= 2.15f && distances.Min() <= 4.2f && valid.All(p => V2.Distance(p, site) >= 2.55f))
                        valid.Add(site);
                }
            Check(valid.Count == 28, "5-6 could not fit shared sockets");
            shared.Layout.Sockets = valid.ToArray();
            if (split) shared.Layout.Roads[2] = splitRoad;
            shared.Layout.Id += split ? "-split" : "-shared";
            return shared;
        }

        static int Mask(params TowerKind[] kinds) => kinds.Aggregate(0, (mask, kind) => mask | (1 << (int)kind));

        static DefenseSimulation NewGame(CampaignStage stage, int seed) => new DefenseSimulation(new GameBalance(), seed,
            new CampaignBattle { Stage = stage, EarnedGrowth = 5, PiercerUnlocked = true, Research = (int[])Research.Clone(),
                Loadout = Mask(Spear, Bind, Blast, stage.Chapter == 5 ? TowerKind.Accelerator : TowerKind.Piercer) });

        static void Buy(DefenseSimulation game, int slot, TowerKind kind, int level = 1)
        {
            Check(game.TryBuild(slot, kind, null, out string reason), "build " + slot + " " + kind + ": " + reason);
            for (int i = 1; i < level; i++) Upgrade(game, slot);
        }

        static void Upgrade(DefenseSimulation game, int slot)
        {
            Tower tower = game.TowerAt(slot);
            Check(tower != null, "missing tower at " + slot);
            Check(game.TryUpgrade(tower.Id, null, out string reason), "upgrade " + slot + ": " + reason);
        }

        static void Card(DefenseSimulation game, List<string> cards)
        {
            if (!game.AwaitingCard) return;
            CardKind kind = game.CardOffers[0];
            Check(game.ChooseCard(kind), "first offered card rejected");
            cards.Add(kind.ToString());
        }

        static bool Finished(DefenseSimulation g) => g.State == RunState.Won || g.State == RunState.Lost;
        static int Total(DefenseSimulation g) => g.Schedule.Sum(group => group.Count);

        sealed class Arrival
        {
            public Enemy Enemy;
            public int MergeWaypoint;
            public double? Crossed, Killed;
        }

        static object Representative(CampaignStage stage, int seed, int option)
        {
            DefenseSimulation g = NewGame(stage, seed);
            int chapter = stage.Chapter;
            Check(Total(g) == (chapter == 3 ? 782 : chapter == 4 ? 693 : 804), stage.Id + " spawn total");
            var cards = new List<string>();
            var arrivals = new Dictionary<int, Arrival>();
            var shots = new List<int>();
            var siteShots = new int[2, stage.SocketCount];
            var siteHits = new int[2, stage.SocketCount];
            var siteDamage = new double[2, stage.SocketCount];
            var siteEastDamage = new double[2, stage.SocketCount];
            var damage = new double[stage.SocketCount];
            var lastShot = new double?[stage.SocketCount];
            var shotGap = new double[stage.SocketCount];
            var supported = new HashSet<int>();
            double simultaneousBridge = 0, simultaneousEastWindow = 0, railExposure = 0, pierceDamage = 0;
            var eastWindowDamage = new double[stage.SocketCount];
            var eastWindowShots = new int[stage.SocketCount];
            double? divergenceTime = null;
            object choicePrefix = null;
            int earlySpent;
            g.PrototypeLineVolley = hits => { if (g.Elapsed < 155) shots.Add(hits); };
            g.PrototypeSpawned = (enemy, group) => {
                if (chapter == 4 && group.Wave == MergeWave && group.Lane < 2)
                    arrivals.Add(enemy.Id, new Arrival { Enemy = enemy, MergeWaypoint = group.Lane == 0 ? 4 : 2 });
            };
            g.PrototypeDamage = (enemy, projectile, amount) => {
                if (!projectile.FromTower) return;
                Tower tower = g.TowerById(projectile.SourceId);
                if (tower == null) return;
                int phase = g.Elapsed < 125 ? 0 : 1;
                int site = tower.Socket;
                damage[site] += amount;
                if (chapter == 3 && tower.Kind == TowerKind.Piercer && g.Elapsed < 155) pierceDamage += amount;
                if (chapter == 5 && g.Elapsed >= 90) {
                    siteHits[phase, site]++;
                    siteDamage[phase, site] += amount;
                    if (enemy.Lane == 2) siteEastDamage[phase, site] += amount;
                    if (g.Elapsed >= 125 && g.Elapsed < 150 && enemy.Lane == 2) eastWindowDamage[site] += amount;
                }
            };
            if (chapter == 3) {
                Buy(g, option, TowerKind.Piercer, 2); Buy(g, 2, Bind, 2); Buy(g, 3, Blast); Buy(g, 4, Spear);
            } else if (chapter == 4) {
                Buy(g, 2, Spear, 2); Buy(g, 3, Spear, 2); Buy(g, option, Bind, 2); Buy(g, 4, Blast);
                foreach (int lane in new[] { 0, 1 }) {
                    int through = lane == 0 ? 4 : 2;
                    V2[] upstream = g.Roads[lane].Take(through + 1).ToArray();
                    Check(BattlefieldLayouts.RoadDistance(g.BoardSockets[1], upstream) > 6.24f,
                        "4-6 post-merge site can reach upstream at maximum Range cards");
                }
            } else {
                Buy(g, 0, Spear, 2); Buy(g, 1, Blast); Buy(g, 2, Spear); Buy(g, 3, Blast); Buy(g, 4, Bind);
            }
            earlySpent = g.Telemetry.CreditsSpent;
            Check(earlySpent == (chapter == 5 ? 285 : 290), "unexpected initial purchase cost");
            g.Start();
            bool thirty = false, sixty = false, ninety = false, later = false, final = false;
            double[] damageAtMeasureEnd = null;
            while (!Finished(g) && g.Elapsed < 400) {
                Card(g, cards);
                if (!thirty && g.Elapsed >= 30) {
                    thirty = true;
                    if (chapter == 5) Upgrade(g, 1);
                }
                if (!sixty && g.Elapsed >= 60) {
                    sixty = true;
                    if (chapter == 3) { Upgrade(g, 3); Upgrade(g, 4); }
                    if (chapter == 4) Upgrade(g, 4);
                }
                if (!ninety && g.Elapsed >= 90) {
                    ninety = true;
                    if (chapter == 5) {
                        divergenceTime = g.Elapsed;
                        Check(g.Telemetry.CreditsSpent == 340, "5-6 prefix cost");
                        choicePrefix = new { g.Credits, g.CoreHealth, g.Kills, g.Telemetry.Spawned, Cards = cards.ToArray(),
                            Purchases = g.Telemetry.Purchases.ToArray() };
                        if (option == 0) Buy(g, 5, TowerKind.Accelerator, 2);
                        else { Upgrade(g, 2); Upgrade(g, 3); }
                        Check(g.Telemetry.CreditsSpent == 425, "5-6 matched spend");
                    }
                    if (chapter == 3) Buy(g, 5, Spear, 2);
                    if (chapter == 4) Buy(g, 5, Blast);
                }
                if (!later && g.Elapsed >= 150) {
                    later = true;
                    if (chapter == 3) Upgrade(g, 3);
                    if (chapter == 4) Upgrade(g, 5);
                    if (chapter == 5) { Buy(g, 6, Blast); Buy(g, 7, Blast); }
                }
                if (chapter == 3 && g.Elapsed >= 155 && damageAtMeasureEnd == null) {
                    damageAtMeasureEnd = (double[])damage.Clone();
                    Buy(g, 6, Spear, 2);
                }
                if (chapter == 5 && !final && g.Elapsed >= 180) {
                    final = true;
                    Upgrade(g, 6); Upgrade(g, 7);
                }
                if (chapter == 3 && g.Elapsed >= 5 && g.Elapsed < 155) {
                    Tower tower = g.TowerAt(option);
                    float range = g.Stats(tower).Range;
                    if (g.Enemies.Any(e => e.Health > 0 && e.IncomingDamage < e.Health && V2.DistanceSquared(e.Position, tower.Position) <= range * range))
                        railExposure += Step;
                }
                if (chapter == 5 && ninety) {
                    int active = 0;
                    foreach (Tower t in g.Towers) if (g.PrototypeRecharge(t) > 1) { supported.Add(t.Socket); active++; }
                    Check(active == (option == 0 ? 2 : 0), "5-6 unexpected support count");
                    bool bothInRange = g.Towers.Take(2).All(t => {
                        float range = g.Stats(t).Range;
                        return g.Enemies.Any(e => e.Health > 0 && V2.DistanceSquared(e.Position,t.Position) <= range * range);
                    });
                    if (bothInRange) {
                        simultaneousBridge += Step;
                        if (g.Elapsed >= 125 && g.Elapsed < 150) simultaneousEastWindow += Step;
                    }
                }
                g.Events.Clear();
                g.Tick(Step, new V2());
                if (chapter == 4) {
                    foreach (Arrival arrival in arrivals.Values)
                        if (!arrival.Crossed.HasValue && arrival.Enemy.Waypoint > arrival.MergeWaypoint)
                            arrival.Crossed = g.Elapsed;
                    foreach (CombatEvent e in g.Events)
                        if (e.Kind == EventKind.Kill && arrivals.TryGetValue(e.EnemyId, out Arrival arrival))
                            arrival.Killed = g.Elapsed;
                }
                foreach (CombatEvent e in g.Events) {
                    if (e.Kind != EventKind.TowerShot) continue;
                    Tower t = g.Towers.Find(tower => tower.Position.X == e.Position.X && tower.Position.Y == e.Position.Y);
                    if (t == null) continue;
                    if (chapter == 3 && g.Elapsed < 155) {
                        int site = t.Socket;
                        shotGap[site] = Math.Max(shotGap[site], g.Elapsed - (lastShot[site] ?? 5));
                        lastShot[site] = g.Elapsed;
                    }
                    if (chapter == 5 && g.Elapsed >= 90) {
                        siteShots[g.Elapsed < 125 ? 0 : 1, t.Socket]++;
                        if (g.Elapsed >= 125 && g.Elapsed < 150) eastWindowShots[t.Socket]++;
                    }
                }
            }
            Check(Finished(g), stage.Id + " representative timed out");
            if (chapter == 3) Check(g.Elapsed >= 155 && shots.Count >= 20, "3-6 incomplete straight/curved observation");
            if (chapter == 4) Check(arrivals.Count == 90 && arrivals.Values.Any(a => a.Enemy.Lane == 0 && a.Crossed.HasValue)
                && arrivals.Values.Any(a => a.Enemy.Lane == 1 && a.Crossed.HasValue), "4-6 no valid merge cohort");
            if (chapter == 5) Check(g.Elapsed >= 150 && supported.SetEquals(option == 0 ? new[]{0,1} : Array.Empty<int>()), "5-6 incomplete shared-bridge comparison");
            Check(g.SuppliesGranted == 5 && cards.Count == 3 && g.Telemetry.CoresGranted == 2, stage.Id + " economy caps");
            double railEnd = Math.Min(g.Elapsed, 155);
            object rail = chapter == 3 ? new { WindowEnd = railEnd, Shots = shots.Count, Hits = shots.Sum(),
                PerShot = shots.Count == 0 ? 0 : shots.Average(), Damage = pierceDamage,
                EligibleTargetSeconds = railExposure, NoEligibleTargetSeconds = Math.Max(0, railEnd - 5 - railExposure),
                LongestShotGap = Math.Max(shotGap[option], railEnd - (lastShot[option] ?? 5)),
                SiteDamageInWindow = damageAtMeasureEnd ?? damage } : null;
            object merge = chapter == 4 ? Merge(arrivals.Values) : null;
            object flood = chapter == 5 ? new {
                Supported = supported.OrderBy(i => i).ToArray(), BridgeOverlapSeconds = simultaneousBridge,
                EastWindow = new { Start = 125, End = 150, BridgeOverlapSeconds = simultaneousEastWindow, SiteShots = eastWindowShots, SiteEastDamage = eastWindowDamage },
                Sites = Enumerable.Range(0, 5).Select(site => new {
                    Site = site, BeforeEast = new { Shots = siteShots[0,site], Hits = siteHits[0,site], Damage = siteDamage[0,site] },
                    AfterEast = new { Shots = siteShots[1,site], Hits = siteHits[1,site], Damage = siteDamage[1,site], EastDamage = siteEastDamage[1,site] }
                }).ToArray()
            } : null;
            return new {
                stage.Id, stage.Name, Map = stage.Layout.Id, Seed = seed, Option = option,
                Paths = g.Roads.Select(BattlefieldLayouts.RoadLength).ToArray(), Sockets = stage.SocketCount,
                Sites = g.BoardSockets.Select(site => new[]{site.X,site.Y}).ToArray(),
                First = g.Schedule.Min(s => s.Start), Last = g.Schedule.Max(s => s.Start + (s.Count-1)*s.Interval),
                Budget = earlySpent, DivergenceTime = divergenceTime, ChoicePrefix = choicePrefix, State = g.State.ToString(), g.Elapsed,
                g.MinimumCoreHealth, g.CoreHealth, g.Telemetry.CoreDamageByLane,
                g.Telemetry.CreditsSpent, g.Credits, g.Telemetry.Spawned, Scheduled = Total(g), g.Kills, Remaining = g.Enemies.Count,
                Cards = cards.ToArray(), g.SuppliesGranted, g.Telemetry.CoresGranted, g.Telemetry.WaveCalls, Purchases = g.Telemetry.Purchases.ToArray(),
                Rail = rail, Merge = merge, Flood = flood
            };
        }

        static object Merge(IEnumerable<Arrival> cohort)
        {
            var west = cohort.Where(a => a.Enemy.Lane == 0).ToArray();
            var north = cohort.Where(a => a.Enemy.Lane == 1).ToArray();
            double[] Times(Arrival[] lane) => lane.Where(a => a.Crossed.HasValue).Select(a => a.Crossed.Value).OrderBy(t => t).ToArray();
            double[] w = Times(west), n = Times(north);
            int peak = 0;
            foreach (double time in w.Concat(n))
                peak = Math.Max(peak, Math.Min(w.Count(t => t >= time && t < time + 3), n.Count(t => t >= time && t < time + 3)));
            int Before(Arrival[] lane) => lane.Count(a => a.Killed.HasValue && !a.Crossed.HasValue);
            return new { WestSpawned = west.Length, NorthSpawned = north.Length, West = w, North = n,
                WestKilledBeforeLine = Before(west), NorthKilledBeforeLine = Before(north), PeakThreeSecondOverlap = peak };
        }

        static CampaignStage RailBoss()
        {
            var waves = Rail(100).Waves.Take(7).Concat(new[] {
                W(215,Boss(0),G(1,56,5,12)), W(245,S(2,4,0),G(2,56,4,12)),
                W(275,G(0,88),B(0,3,8)), W(305,R(3,36,0,9),G(3,36,5,10))
            });
            return NameBoss(Stage(104,3,24,"지상 분기장",28,
                new[]{RailWest(),RailNorth(),Path(23,-1,15,-1,15,-7,8,-7,8,0,0,0),Path(-10,-11,-10,-7,0,-7,0,0)},Empty,waves.ToArray()),
                "전환기 파수자","지하 진입로 확보");
        }

        static CampaignStage TransferBoss()
        {
            var waves = Transfer(101).Waves.Take(7).Select(w => W(w.Start,
                w.Groups.Select(g => g.Lane == 2 ? G(0,g.Count,g.Start,(g.Count-1)*g.Interval,g.Kind) : g).ToArray())).Concat(new[] {
                W(215,B(0,4,0),G(0,64,4,12)), W(245,R(2,28,2,8)), W(275,Boss(1),G(0,56,6,12)),
                W(305,G(2,54,0,12),R(2,16,6,8))
            });
            return NameBoss(Stage(105,4,24,"심연의 탑",28,
                new[]{TransferWest(),TransferNorth(),Path(5,0,-3,0,-7,0,-7,-4,0,-4,0,0),Empty},Empty,waves.ToArray()),
                "맥동 감시자","심연의 탑 봉인");
        }

        static CampaignStage FloodBoss()
        {
            var waves = Flood(102,false).Waves.Take(7).Concat(new[] {
                W(215,G(3,48,0,12),S(3,3,3)), W(245,G(0,80),B(0,4,6)), W(275,Boss(1),G(0,60,4,12)),
                W(305,G(0,96),S(0,3,8))
            });
            return NameBoss(Stage(106,5,26,"범람의 탑",28,
                new[]{FloodWest(),Path(11,11,11,7,9,7,9,1,0,1,0,0),FloodEast(false),FloodSouth()},Empty,waves.ToArray()),
                "역류 차단기","범람의 탑 봉인");
        }

        static CampaignStage NameBoss(CampaignStage stage, string name, string victory)
        {
            stage.BossName = name;
            stage.BossVictoryTitle = victory;
            return stage;
        }

        public static CampaignStage[] ProgressionStages(int index)
        {
            var gate = NameBoss(Stage(index+1,2,22,"굶주림의 탑",28,
                new[]{Path(-23,6,-13,6,-13,-6,-5,-6,-5,3,0,3,0,0),Path(0,11,0,0),
                    Path(23,-6,13,-6,13,6,5,6,5,-3,0,-3,0,0),Empty},Empty,
                W(5,G(0,72)),W(35,G(2,72,0,12)),W(65,G(0,60,0,12),B(2,3,5)),
                W(95,G(2,64,0,12),R(0,18,6,8)),W(125,G(0,72),S(2,3,5)),W(155,G(2,64),R(0,22,4,8)),
                W(185,Boss(1),G(0,64,4,12)),W(215,G(2,72),S(0,3,6)),W(245,G(0,64),G(2,64,4,12))),
                "집하관 — 끝없는 입","굶주림의 탑 봉인");
            gate.PredecessorId = "C02-S021";
            gate.FirstReward = 34; // Synthetic sentinel, not a campaign economy decision.
            return new[] {
                new CampaignStage { Index = index, Chapter = 2, Number = 21, StableId = "C02-S021" }, gate,
                new CampaignStage { Index = index+2, Chapter = 3, Number = 1, StableId = "C03-S001", PredecessorId = gate.Id }
            };
        }

        static object BossRun(DefenseSimulation g)
        {
            CampaignStage stage = g.Rules.Stage;
            SpawnGroup group = g.Schedule.Single(s => s.Kind == EnemyKind.Boss);
            Check(g.BossWave == group.Wave && g.BossWave < g.WaveCount-1, stage.Id + " non-final boss wave missing");
            double? entry = null, killed = null, firstAllSpawned = null;
            int? enteredLane = null;
            V2 entryPosition = new V2();
            var warning = new List<double>();
            var impact = new List<double>();
            var cards = new List<string>();
            g.PrototypeSpawned = (enemy, spawn) => {
                if (enemy.Kind != EnemyKind.Boss) return;
                entry = g.Elapsed;
                entryPosition = enemy.Position;
                enteredLane = enemy.Lane;
            };
            // Prioritize actual early roads first, then arm the boss and late reinforcement lanes.
            int[] priority = g.Schedule.GroupBy(s => s.Lane).OrderBy(items => items.Min(s => s.Start)).Select(items => items.Key).ToArray();
            var chosen = new HashSet<int>();
            var order = new List<(int Site, TowerKind Kind)>();
            for (int round = 0; round < 4; round++) {
                foreach (int lane in priority) {
                    V2[] road = g.Roads[lane];
                    float along = BattlefieldLayouts.RoadLength(road) * (round == 0 ? .55f : round == 1 ? .76f : round == 2 ? .36f : .9f);
                    V2 point = BattlefieldLayouts.Along(road, along, out _);
                    int site = Enumerable.Range(0,g.BoardSockets.Length).Where(i => !chosen.Contains(i))
                        .OrderBy(i => V2.DistanceSquared(point,g.BoardSockets[i])).First();
                    chosen.Add(site);
                    order.Add((site, round % 2 == 0 ? Blast : Spear));
                }
            }
            order = order.Take(14).ToList();
            int next = 0;
            void Invest()
            {
                while (next < order.Count) {
                    var pick = order[next];
                    if (g.Credits < TowerCatalog.Get(pick.Kind).BuildCost) break;
                    Buy(g,pick.Site,pick.Kind);
                    next++;
                }
                if (next == order.Count)
                    foreach (Tower t in g.Towers)
                        if (t.Level < 2 && g.Credits >= TowerCatalog.Get(t.Kind).UpgradePrice(t.Level)) Upgrade(g,t.Socket);
            }
            Invest();
            g.Start();
            while (!Finished(g) && g.Elapsed < 420) {
                Card(g,cards);
                Invest();
                g.Events.Clear();
                // The same deterministic movement stays active during the common circular telegraph.
                V2 movement = g.Enemies.Any(e => e.Kind == EnemyKind.Boss && e.BossWindup > 0)
                    ? (g.PlayerPosition.X > 8 ? new V2(-1,0) : new V2(1,0))
                    : (new V2(g.PlayerPosition.X,g.PlayerPosition.Y + 2.2f) * -1).Normalized;
                g.Tick(Step,movement);
                if (g.AllWavesSpawned && !firstAllSpawned.HasValue) firstAllSpawned = g.Elapsed;
                foreach (CombatEvent e in g.Events) {
                    if (e.IsBossWarning) warning.Add(g.Elapsed);
                    if (e.Kind == EventKind.Hit && e.Source == -2) impact.Add(g.Elapsed);
                    if (e.Kind == EventKind.Kill && e.Source == (int)EnemyKind.Boss) killed = g.Elapsed;
                }
            }
            Check(entry.HasValue && enteredLane == group.Lane && V2.Distance(entryPosition,g.Roads[group.Lane][0]) < .0001f,
                stage.Id + " boss did not enter at authored gate");
            Check(Math.Abs(entry.Value-group.Start) <= Step+.001, stage.Id + " boss entered outside authored schedule");
            Check(warning.Count > 0 && impact.Count > 0 && Math.Abs(impact[0]-warning[0]-1.5) < .05, stage.Id + " missing shared telegraph");
            Check(g.State == RunState.Won && killed.HasValue && firstAllSpawned.HasValue && g.AllWavesSpawned &&
                g.BossDefeated && g.Enemies.Count == 0 && g.Kills == Total(g) && g.CoreHealth > 0,
                stage.Id + " boss outcome " + g.State + " @" + g.Elapsed + " " + g.Kills + "/" + Total(g));
            Check(killed < firstAllSpawned && g.Elapsed > killed, stage.Id + " needs remaining scheduled enemies after the boss");
            return new {
                stage.Id, stage.Name, stage.BossDisplayName, stage.BossVictoryTitle, g.Seed, BossWave = g.BossWave+1,
                BossLane = enteredLane, BossEntered = entry, BossPosition = new[]{entryPosition.X,entryPosition.Y},
                RoadLength = BattlefieldLayouts.RoadLength(g.Roads[group.Lane]), Warnings = warning.ToArray(), Impacts = impact.ToArray(),
                BossKilled = killed, AllSpawns = firstAllSpawned, End = g.Elapsed, State = g.State.ToString(),
                Scheduled = Total(g), g.Telemetry.Spawned, g.Kills, Remaining = g.Enemies.Count, g.CoreHealth, g.BossDefeated,
                Stars = CampaignCatalog.Stars(true,g.MinimumCoreHealth,g.Balance.CoreHealth), Towers = g.Towers.Count,
                g.Telemetry.CreditsSpent, g.SuppliesGranted, g.Telemetry.CoresGranted, Cards = cards.ToArray(),
                Purchases = g.Telemetry.Purchases.ToArray()
            };
        }

        static object Progression(string output, List<object> bosses)
        {
            int gate = CampaignCatalog.FindStage("C02-S022"), next = CampaignCatalog.FindStage("C03-S001");
            int predecessor = CampaignCatalog.FindStage("C02-S021");
            CampaignProfile SeedProfile(int balance)
            {
                var p = new CampaignProfile { MagicCores = balance, Growth = new[]{5,0,0}, Research = (int[])Research.Clone(), Loadout = Mask(Spear,Bind,Blast) };
                for (int i=0;i<20;i++) { p.FirstCleared[i]=true; p.BestStars[i]=2; }
                p.StorySeenIds.Add("C02-S006");
                p.TutorialSeenIds.Add("C02-S006");
                p.UnmappedProgress["C08-S001"] = new StageProgress();
                return p;
            }
            CampaignProfile fixture = SeedProfile(0);
            Check(!fixture.IsStageOpen(gate) && !fixture.IsStageOpen(next), "broken chapter gate before predecessor");
            fixture.FirstCleared[predecessor] = true; fixture.BestStars[predecessor] = 2;
            Check(fixture.IsStageOpen(gate) && !fixture.IsStageOpen(next), "2-21 should open only 2-22");
            var rules = new CampaignService(fixture,null).Battle(gate,false);
            var first = new DefenseSimulation(new GameBalance(),7,rules);
            var replay = new DefenseSimulation(new GameBalance(),41,rules);
            bosses.Add(BossRun(first));
            bosses.Add(BossRun(replay));
            var balances = new List<object>();
            foreach (int balance in new[]{0,506}) {
                var p = SeedProfile(balance);
                p.FirstCleared[predecessor] = true; p.BestStars[predecessor] = 2;
                string path = PathFor(output,balance);
                var store = new CampaignStore(path);
                store.Save(p);
                bool fail = false;
                var service = new CampaignService(store.Load(), value => {
                    if (fail) throw new IOException("synthetic write failure");
                    store.Save(value);
                });
                Check(service.Begin(gate,false,out string id), "2-22 begin");
                byte[] pending = File.ReadAllBytes(path);
                fail = true;
                Check(!service.Complete(id,first,out int failedReward) && failedReward == 0 &&
                    File.ReadAllBytes(path).SequenceEqual(pending) && service.Profile.PendingRun == id &&
                    service.Profile.MagicCores == balance && !service.Profile.IsStageOpen(next), "failed save leaked outcome");
                fail = false;
                Check(service.Complete(id,first,out int firstReward) && firstReward == 34, "2-22 first reward");
                var saved = store.Load();
                Check(saved.IsStageOpen(next) && saved.FirstCleared[gate] && saved.MagicCores == balance + firstReward &&
                    saved.PendingRun == "" && saved.PendingStage == -1 && !saved.PendingCrisis, "2-22 first result lost");
                byte[] firstBytes = File.ReadAllBytes(path);
                Check(service.Complete(id,first,out int duplicate) && duplicate == firstReward &&
                    firstBytes.SequenceEqual(File.ReadAllBytes(path)), "duplicate changed save");
                Check(service.Begin(gate,false,out string replayId), "2-22 replay begin");
                Check(service.Complete(replayId,replay,out int replayReward) && replayReward == 2, "2-22 replay reward");
                saved = store.Load();
                Check(saved.MagicCores == balance + firstReward + replayReward && saved.IsStageOpen(next) &&
                    saved.BestStars.Take(20).SequenceEqual(p.BestStars.Take(20)) &&
                    saved.Growth.SequenceEqual(p.Growth) && saved.Research.SequenceEqual(p.Research) &&
                    saved.StorySeenIds.SetEquals(p.StorySeenIds) && saved.TutorialSeenIds.SetEquals(p.TutorialSeenIds) &&
                    saved.UnmappedProgress.ContainsKey("C08-S001") && saved.PendingRun == "" && saved.Valid(), "2-22 v3 continuity");
                Check(File.ReadAllBytes(path).SequenceEqual(CampaignStore.Encode(saved)), "v3 canonical round-trip");
                balances.Add(new { Start = balance, FirstReward = firstReward, ReplayReward = replayReward,
                    End = saved.MagicCores, NextStage = "C03-S001", saved.LastReward, Bytes = new FileInfo(path).Length,
                    Save = System.IO.Path.GetFileName(path) });
            }
            return new { SyntheticPredecessor = "C02-S021", FirstRewardIsSentinel = true, DuplicateNoWrite = true,
                FailedCommitPreservedPending = true, Accounts = balances.ToArray() };
        }

        static string PathFor(string output, int balance) => System.IO.Path.Combine(output,
            "synthetic-gate-" + balance + "-" + Guid.NewGuid().ToString("N") + ".dat");
    }
}
