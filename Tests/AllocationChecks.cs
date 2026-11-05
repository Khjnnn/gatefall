using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    // Host .NET allocation guards for paths that run on every simulation step or every rendered frame.
    // The ward check compares two enemy counts instead of absolute bytes, so it does not depend on the runtime's
    // object sizes. Not an Android/IL2CPP GC or frame-time measurement.
    public static class AllocationChecks
    {
        static readonly List<string> output = new List<string>();
        static int count;
        static void Assert(bool v, string why) { if (!v) throw new Exception(why); }
        static void Check(string name, Func<string> action) { string detail = action(); count++; output.Add("PASS " + name + (detail == "" ? "" : " · " + detail)); }

        public static string RunAll()
        {
            output.Clear(); count = 0;
            Check("Ward blocking cost per step does not grow with the number of enemies", WardBlockingScale);
            Check("Fusion-ready pad marker matches the fusion menu rule on random lab and campaign boards", ReadyFusionMatchesMenu);
            Check("Fusion-ready pad marker allocates nothing per frame", ReadyFusionAllocation);
            output.Add("PASS: " + count + " allocation checks. Host .NET only; Android frame time and GC pauses are not measured.");
            return string.Join("\n", output);
        }

        // ---------------------------------------------------------------- per-step ward blocking
        static string WardBlockingScale()
        {
            int heldFew, heldMany;
            long few = WardStepBytes(40, out heldFew);
            long many = WardStepBytes(400, out heldMany);
            Assert(heldFew > 0 && heldMany > 0, "Scenario did not exercise ward blocking");
            // Before v0.27.37 every unblocked enemy resolved Stats() for every active ward on every step (about 10x here).
            Assert(many <= few * 3 / 2 + 64 * 1024, "Ward blocking allocations scale with enemies: 40 enemies " + few + " bytes, 400 enemies " + many + " bytes per 120 steps");
            return "24 wards, 120 steps: 40 enemies " + few / 1024 + "KB, 400 enemies " + many / 1024 + "KB";
        }

        static long WardStepBytes(int enemies, out int held)
        {
            var balance = new GameBalance { SpawnEnemies = false, OfferCards = false, StartingCredits = 10000, PlayerDamage = 0, PlayerRange = 0, CoreHealth = 1000000 };
            var g = new DefenseSimulation(balance, 7, new CampaignBattle { Stage = CampaignCatalog.Stages[5] });
            g.Schedule.Clear(); string why;
            for (int i = 0; i < g.BoardSockets.Length; i++) Assert(g.TryBuild(i, TowerKind.Barrier, null, out why), why);
            for (int i = 0; i < enemies; i++) {
                int lane = g.OpenLanes[i % g.OpenLanes.Length], node; V2[] road = g.Roads[lane];
                V2 p = BattlefieldLayouts.Along(road, BattlefieldLayouts.RoadLength(road) * (.05f + .9f * (i / (float)enemies)), out node);
                g.Enemies.Add(new Enemy { Id = 10000 + i, Lane = lane, Waypoint = node, Kind = EnemyKind.Grunt, Position = p, Health = 50000, MaxHealth = 50000, Speed = 0 });
            }
            g.Start();
            for (int i = 0; i < 12; i++) { g.Tick(1f / 60, new V2()); g.Events.Clear(); }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 120; i++) { g.Tick(1f / 60, new V2()); g.Events.Clear(); }
            long bytes = GC.GetAllocatedBytesForCurrentThread() - before;
            held = 0; foreach (Enemy e in g.Enemies) if (e.BlockOwner > 0) held++;
            return bytes;
        }

        // ---------------------------------------------------------------- per-frame fusion-ready marker
        // Unity-free copy of the readiness rule in GatefallGame.FusionOptions: a menu entry whose reason is empty.
        static bool MenuReady(DefenseSimulation g, Tower tower)
        {
            if (tower == null) return false;
            int reachable = g.ReachableTowerFamilies();
            foreach (FusionRecipe r in TowerCatalog.Recipes) {
                if (r.First != tower.Kind && r.Second != tower.Kind) continue;
                if (!g.Allows(r.Result)) continue;
                TowerKind other = r.First == tower.Kind ? r.Second : r.First;
                if (!g.Allows(other)) continue;
                if ((reachable & (1 << (int)other)) == 0) continue;
                var partners = new List<int>();
                foreach (Tower t in g.Towers) { if (t.Id == tower.Id || t.Kind != other) continue; if (t.Level >= 2) partners.Add(t.Socket); }
                if (tower.Level < 2 || partners.Count == 0) continue;
                if (g.CanFuse(r.Id, tower.Id, g.TowerAt(partners[0]).Id, tower.Socket) == "") return true;
            }
            return false;
        }

        static int Mask(params TowerKind[] kinds) { int m = 0; foreach (TowerKind k in kinds) m |= 1 << (int)k; return m; }

        static DefenseSimulation Board(int stage, int seed)
        {
            var balance = new GameBalance { StartingCredits = 1200 };
            CampaignBattle battle = stage < 0 ? new CampaignBattle() : new CampaignBattle {
                Stage = CampaignCatalog.Stages[stage], MarkerUnlocked = stage >= 10, ObserverUnlocked = stage >= 13,
                Loadout = stage >= 10 ? Mask(TowerKind.Autocannon, TowerKind.Binding, TowerKind.Rupture, TowerKind.Marker) : 135 };
            return new DefenseSimulation(balance, seed, battle);
        }

        static string ReadyFusionMatchesMenu()
        {
            int compared = 0, ready = 0, notReady = 0;
            TowerKind[] kinds = { TowerKind.Autocannon, TowerKind.Binding, TowerKind.Rupture, TowerKind.Marker, TowerKind.Barrier };
            foreach (int stage in new[] { -1, 2, 5, 10, 13 }) for (int seed = 1; seed <= 3; seed++) {
                var g = Board(stage, seed); var rng = new Random(seed * 101 + stage); string why; int command = 0;
                g.Start();
                for (int step = 0; step < 11000 && g.State != RunState.Won && g.State != RunState.Lost; step++) {
                    if (step % 6 == 0) {
                        if (g.AwaitingCard) g.ChooseCard(g.CardOffers[rng.Next(g.CardOffers.Count)]);
                        // Explicit pauses last one action so paused boards are compared without stalling the run.
                        else if (g.State == RunState.Paused) g.TogglePause();
                        int roll = rng.Next(100);
                        if (roll < 40) g.TryBuild(rng.Next(g.BoardSockets.Length), kinds[rng.Next(kinds.Length)], "a" + command++, out why);
                        else if (roll < 75 && g.Towers.Count > 0) g.TryUpgrade(g.Towers[rng.Next(g.Towers.Count)].Id, "a" + command++, out why);
                        else if (roll < 85 && g.Towers.Count > 1) {
                            Tower a = g.Towers[rng.Next(g.Towers.Count)], b = g.Towers[rng.Next(g.Towers.Count)];
                            g.TryFuse(rng.Next(TowerCatalog.Recipes.Length), a.Id, b.Id, a.Socket, "a" + command++, out why);
                        }
                        else if (roll < 88 && g.Towers.Count > 0) g.TrySell(g.Towers[rng.Next(g.Towers.Count)].Id, "a" + command++, out why);
                        else if (roll < 90) g.TogglePause();
                    }
                    if (step % 5 == 0) foreach (Tower t in g.Towers) {
                        bool expected = MenuReady(g, t), actual = g.HasReadyFusion(t);
                        Assert(expected == actual, "Stage " + stage + " seed " + seed + " t=" + g.Elapsed.ToString("F2") + " tower " + t.Kind + " Lv" + t.Level + ": menu " + expected + ", marker " + actual);
                        compared++; if (actual) ready++; else notReady++;
                    }
                    g.Tick(1f / 60, new V2()); g.Events.Clear();
                }
            }
            Assert(ready >= 50 && notReady >= 50, "Random boards did not cover both outcomes: ready " + ready + ", not ready " + notReady);
            return compared + " comparisons, ready " + ready;
        }

        static string ReadyFusionAllocation()
        {
            // Lab board at 60s: one catalyst, enough credits, Lv2 pairs for every first-tier recipe plus unmatched towers.
            var g = Board(-1, 7); string why; g.Start();
            TowerKind[] layout = { TowerKind.Autocannon, TowerKind.Binding, TowerKind.Rupture, TowerKind.Marker, TowerKind.Autocannon, TowerKind.Barrier, TowerKind.Binding, TowerKind.Rupture };
            for (int i = 0; i < layout.Length && i < g.BoardSockets.Length; i++) {
                Assert(g.TryBuild(i, layout[i], null, out why), why);
                if (i < 6) Assert(g.TryUpgrade(g.TowerAt(i).Id, null, out why), why);
            }
            while (g.Cores < 1 && g.Elapsed < 70) { if (g.AwaitingCard) g.ChooseCard(g.CardOffers[0]); g.Tick(1f / 60, new V2()); g.Events.Clear(); }
            if (g.AwaitingCard) g.ChooseCard(g.CardOffers[0]);
            int ready = 0; foreach (Tower t in g.Towers) if (g.HasReadyFusion(t)) ready++;
            Assert(ready > 0 && ready < g.Towers.Count, "Scenario needs both ready and unready towers: " + ready + "/" + g.Towers.Count);
            for (int i = 0; i < 50; i++) foreach (Tower t in g.Towers) { g.HasReadyFusion(t); MenuReady(g, t); }
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int frame = 0; frame < 600; frame++) foreach (Tower t in g.Towers) g.HasReadyFusion(t);
            long marker = GC.GetAllocatedBytesForCurrentThread() - before;
            before = GC.GetAllocatedBytesForCurrentThread();
            for (int frame = 0; frame < 600; frame++) foreach (Tower t in g.Towers) MenuReady(g, t);
            long menu = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert(marker == 0, "HasReadyFusion allocated " + marker + " bytes over 600 frames");
            return g.Towers.Count + " towers, 600 frames: marker 0 bytes, menu rule " + menu / 600 + " bytes/frame";
        }
    }
}
