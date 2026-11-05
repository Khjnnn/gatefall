using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // 1-11 first-unlock setup. Towers stay at level 1 and no later purchases are made.
    public static class MarkerOpeningReview {
        static string Run(int seed, bool marker) {
            var rules = new CampaignBattle { Stage = CampaignCatalog.Stages[10], MarkerUnlocked = true,
                Loadout = (1 << (int)TowerKind.Autocannon) | (1 << (int)TowerKind.Binding) |
                          (1 << (int)TowerKind.Rupture) | (1 << (int)TowerKind.Marker) };
            var g = new DefenseSimulation(new GameBalance(), seed, rules);
            string why;
            if (!g.TryBuild(4, TowerKind.Autocannon, null, out why) ||
                !g.TryBuild(0, marker ? TowerKind.Marker : TowerKind.Autocannon, null, out why) ||
                !g.TryBuild(1, TowerKind.Autocannon, null, out why)) throw new Exception("Opening build failed: " + why);
            g.Start();
            for (int i = 0; i < 4500 && g.State != RunState.Won && g.State != RunState.Lost; i++) {
                if (g.AwaitingCard) g.ChooseCard(g.CardOffers[0]);
                g.Tick(.05f, new V2());
                g.Events.Clear();
            }
            return String.Format("seed={0} setup={1} result={2} elapsed={3:F1} core={4:F0} kills={5} spent={6} markedHits={7} bonus={8:F1}",
                seed, marker ? "spear-marker-spear" : "three-spears", g.State, g.Elapsed, g.MinimumCoreHealth,
                g.Kills, g.Telemetry.CreditsSpent, g.Telemetry.MarkedHits, g.Telemetry.MarkBonusDamage);
        }
        public static string RunAll() {
            var lines = new List<string>();
            foreach (int seed in new[] { 7, 19, 41 }) { lines.Add(Run(seed, true)); lines.Add(Run(seed, false)); }
            return String.Join("\n", lines);
        }
    }
}
