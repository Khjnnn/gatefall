using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Fixed purchase fixture: compare anchor choice separately from reinvesting freed sockets.
    public static class FairSealInvestmentReview {
        public sealed class Row {
            public int Seed, Anchor, Supplement, Spent, Credits, Towers, Cores, Actions, Fusions, Kills, PeakRecent3;
            public bool Crisis, Seal;
            public string State, PrefixHash, Profile;
            public bool Compact;
            public float Core;
            public double End, MeanRecent3;
            public int[] Cards, Shots;
            public float[] Damage, PrefixDamage;
            public string[] Ledger;
            public string[] CoreHits;
            // Effective damage after the shared choice, already included in Damage (never add twice).
            public double[] PostSwarm, PostStrong, PostOther;
        }

        static DefenseSimulation observed;
        static Row observation;
        static float[] observedSum;
        static int observedHits;
        // Called only by an exact, removable line in a product-external core copy.
        public static void ObserveDamage(DefenseSimulation game, Enemy enemy, Projectile shot, float effective) {
            if (game != observed || observation == null || observation.PrefixHash == null || !shot.FromTower) return;
            var target = enemy.Swarm ? observation.PostSwarm :
                enemy.Kind == EnemyKind.Brute || enemy.Kind == EnemyKind.Siege || enemy.Kind == EnemyKind.Boss ? observation.PostStrong : observation.PostOther;
            target[(int)shot.Kind] += effective;
            observedSum[(int)shot.Kind] += effective;
            observedHits++;
        }

        static string Prefix(DefenseSimulation game) {
            var text = new StringBuilder();
            typeof(EarlyCombatDigest).GetMethod("Value", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { text,
                new object[] { game.RunContext(), game.State, game.Elapsed, game.Credits, game.Cores, game.Kills, game.CoreHealth,
                    game.MinimumCoreHealth, game.PlayerHealth, game.Enemies, game.Towers, game.Projectiles, game.Schedule, game.Telemetry } });
            using (var hash = SHA256.Create()) return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())).Select(x => x.ToString("x2")));
        }

        public static Row Play(int seed, bool seal, bool crisis, int anchor, bool supplement) {
            return PlayFixture(seed, seal, crisis, anchor, supplement, false, false);
        }

        public static Row PlayFixture(int seed, bool seal, bool crisis, int anchor, bool supplement, bool compact, bool below) {
            if (anchor != 4 && anchor != 7) throw new ArgumentException("Anchor must be one of the shared materials: 4 or 7");
            var game = new DefenseSimulation(new GameBalance(), seed, new CampaignBattle {
                Stage = CampaignCatalog.Stages[13], Crisis = crisis, EarnedGrowth = below ? 4 : 5,
                Research = below ? new[] { 2, 1, 1, 1 } : new[] { 3, 2, 2, 2 }, Loadout = 135
            });
            var row = new Row { Seed = seed, Seal = seal, Crisis = crisis, Anchor = seal ? anchor : -1, Supplement = supplement ? 120 : 0,
                Compact = compact, Profile = below ? "below" : "recommended",
                Shots = new int[TowerCatalog.All.Length], PostSwarm = new double[TowerCatalog.All.Length],
                PostStrong = new double[TowerCatalog.All.Length], PostOther = new double[TowerCatalog.All.Length] };
            observed = game; observation = row; observedSum = null; observedHits = 0;
            try {
                var times = new List<float>(); var actions = new List<Func<bool>>(); string why;
                Action<float, int, TowerKind> build = (time, site, kind) => { times.Add(time); actions.Add(() => game.TryBuild(site, kind, null, out why)); };
                Action<float, int> upgrade = (time, site) => { times.Add(time); actions.Add(() => game.TowerAt(site) != null && game.TryUpgrade(game.TowerAt(site).Id, null, out why)); };
                Action<float, int, int, int, int> fuse = (time, recipe, a, b, output) => { times.Add(time); actions.Add(() => game.TowerAt(a) != null && game.TowerAt(b) != null && game.TryFuse(recipe, game.TowerAt(a).Id, game.TowerAt(b).Id, output, null, out why)); };
                build(0, 4, TowerKind.Autocannon); build(0, 1, TowerKind.Binding); build(0, 7, TowerKind.Rupture);
                foreach (int site in new[] { 9, 2, 0 }) build(0, site, TowerKind.Autocannon);
                upgrade(0, 4); upgrade(30, 1);
                foreach (int site in new[] { 11, 23 }) build(30, site, TowerKind.Autocannon);
                fuse(62, 0, 4, 1, 4); build(62, 1, TowerKind.Autocannon); upgrade(62, 4);
                upgrade(90, 7);
                if (!compact) foreach (int site in new[] { 14, 12, 3, 6 }) build(90, site, TowerKind.Autocannon);
                int prefixCount = actions.Count;
                if (seal) fuse(122, 3, 4, 7, anchor);
                else foreach (int site in new[] { 8, 10 }) build(122, site, TowerKind.Autocannon);
                foreach (int site in new[] { 9, 2, 0 }) upgrade(150, site);
                if (supplement) {
                    if (seal) foreach (int site in new[] { anchor == 4 ? 7 : 4, 8, 10 }) build(150, site, TowerKind.Autocannon);
                    else foreach (int site in compact ? new[] { 1, 11, 8, 10 } : new[] { 1, 11, 14, 12 }) upgrade(150, site);
                }
                int index = 0, samples = 0; double participation = 0;
                var coreHits = new List<string>();
                var cooldowns = new Dictionary<int, float>(); var lastShot = new Dictionary<int, double>();
                while (index < actions.Count && times[index] == 0 && actions[index]()) index++;
                game.Start();
                while (game.Elapsed < 320 && game.State != RunState.Won && game.State != RunState.Lost) {
                    if (game.AwaitingCard) foreach (var card in new[] { CardKind.Ballistic, CardKind.Range, CardKind.Credits, CardKind.Hunter, CardKind.Blast, CardKind.Control })
                        if (game.CardOffers.Contains(card)) { game.ChooseCard(card); break; }
                    if (index == prefixCount && game.Elapsed >= 122 && row.PrefixHash == null) {
                        row.PrefixHash = Prefix(game); row.PrefixDamage = (float[])game.Telemetry.TowerDamage.Clone();
                        observedSum = (float[])row.PrefixDamage.Clone();
                        if (game.Towers.Count != (compact ? 8 : 12) || game.TowerAt(4).Kind != TowerKind.Arc || game.TowerAt(4).Level != 2 ||
                            game.TowerAt(7).Kind != TowerKind.Rupture || game.TowerAt(7).Level != 2) throw new Exception("Shared material fixture changed");
                    }
                    while (index < actions.Count && game.Elapsed >= times[index] && actions[index]()) index++;
                    foreach (var tower in game.Towers) cooldowns[tower.Id] = tower.Cooldown;
                    game.Tick(1f / 30, new V2());
                    foreach (var tower in game.Towers) {
                        float previous;
                        if (cooldowns.TryGetValue(tower.Id, out previous) && tower.Commission <= 0 && tower.Cooldown > previous + .0001f) {
                            row.Shots[(int)tower.Kind]++; lastShot[tower.Id] = game.Elapsed;
                        }
                    }
                    if (game.Elapsed >= 122 && game.Elapsed <= 180) {
                        int recent = game.Towers.Count(t => lastShot.ContainsKey(t.Id) && lastShot[t.Id] >= game.Elapsed - 3);
                        samples++; participation += recent; row.PeakRecent3 = Math.Max(row.PeakRecent3, recent);
                    }
                    foreach (var hit in game.Events) if (hit.Kind == EventKind.CoreHurt)
                        coreHits.Add(game.Elapsed.ToString("F2") + " lane=" + hit.Source + " damage=" + hit.Value.ToString("R"));
                    game.Events.Clear();
                }
                int expected = (supplement ? 1065 : 945) - (compact ? 160 : 0);
                if (index != actions.Count || row.PrefixHash == null || game.Telemetry.CreditsSpent != expected || game.Towers.Count != ((seal && !supplement ? 11 : 14) - (compact ? 4 : 0)))
                    throw new Exception("Incomplete or unequal investment fixture: " + game.Telemetry.CreditsSpent + "/" + expected + " actions=" + index + "/" + actions.Count);
                if (observedHits > 0 && !observedSum.SequenceEqual(game.Telemetry.TowerDamage))
                    throw new Exception("Read probe float accumulation differs from product telemetry");
                row.State = game.State.ToString(); row.Core = game.MinimumCoreHealth; row.End = game.Elapsed;
                row.Spent = game.Telemetry.CreditsSpent; row.Credits = game.Credits; row.Towers = game.Towers.Count; row.Cores = game.Cores;
                row.Actions = index; row.Fusions = game.Telemetry.Fusions; row.Kills = game.Kills; row.Cards = game.Cards;
                row.Damage = game.Telemetry.TowerDamage; row.Ledger = game.Telemetry.Purchases.ToArray(); row.CoreHits = coreHits.ToArray();
                row.MeanRecent3 = samples == 0 ? 0 : participation / samples;
                return row;
            } finally { observed = null; observation = null; observedSum = null; observedHits = 0; }
        }
    }
}
