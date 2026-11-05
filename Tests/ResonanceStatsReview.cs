using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class ResonanceStatsReview {
        public sealed class Allocation { public string Kind; public int Calls; public long Bytes; public float Checksum; }
        public sealed class Report { public int Cases; public string Hash; public Allocation[] Allocations; }
        static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
        static void Require(bool condition, string why) { if (!condition) throw new Exception(why); }
        static string Value(TowerStats stats) {
            return string.Join(";", typeof(TowerStats).GetFields().OrderBy(f => f.Name).Select(f => {
                var value = f.GetValue(stats);
                return f.Name + "=" + (value is float[] ? string.Join(",", ((float[])value).Select(v => v.ToString("R", Invariant))) :
                    value is float ? ((float)value).ToString("R", Invariant) : value == null ? "null" : Convert.ToString(value, Invariant));
            }));
        }
        static DefenseSimulation Board(int mode) {
            CampaignBattle battle = mode == 0 ? null : mode == 1 ? new CampaignBattle() : new CampaignBattle {
                Stage = CampaignCatalog.Stages[mode == 2 ? 0 : mode == 3 ? 1 : mode == 4 ? 10 : 13],
                Crisis = mode == 6, Research = new[] { 3, 2, 2, 2 }, EarnedGrowth = 5
            };
            return new DefenseSimulation(new GameBalance(), 7, battle);
        }
        static Tower Pair(DefenseSimulation game, TowerKind kind, int level, int pattern) {
            game.Towers.Clear();
            var tower = new Tower { Id = 101, Kind = kind, Level = level, Socket = 0, Position = new V2() };
            var other = new Tower { Id = 102, Kind = kind, Level = level, Socket = 1, Position = new V2(6, 0) };
            if (pattern == 1) other.Position = new V2(6.001f, 0);
            if (pattern == 2) other.Commission = .1f;
            if (pattern == 3) tower.Commission = .1f;
            if (pattern == 4) other.Kind = kind == TowerKind.Binding ? TowerKind.Autocannon : TowerKind.Binding;
            if (pattern == 5) { tower.Cooldown = 1; other.Cooldown = 1; }
            game.Towers.Add(tower); if (pattern != 6) game.Towers.Add(other);
            return tower;
        }
        public static Report Run() {
            var text = new StringBuilder(); int cases = 0;
            foreach (int mode in Enumerable.Range(0, 7)) foreach (TowerKind kind in new[] { TowerKind.Autocannon, TowerKind.Binding, TowerKind.Rupture, TowerKind.Barrier, TowerKind.Marker, TowerKind.Arc }) {
                var game = Board(mode);
                foreach (int level in Enumerable.Range(1, TowerCatalog.Get(kind).Levels.Length)) foreach (int pattern in Enumerable.Range(0, 7)) foreach (int cards in new[] { 0, 1, 3 }) {
                    for (int i = 0; i < game.Cards.Length; i++) game.Cards[i] = cards;
                    var tower = Pair(game, kind, level, pattern);
                    string before = Value(game.Stats(tower));
                    text.Append(mode).Append(':').Append(kind).Append(':').Append(level).Append(':').Append(pattern).Append(':').Append(cards).Append(':').Append(before).Append('\n');
                    // Public callers still own a fresh mutable snapshot; the catalog must not be exposed or changed.
                    var changed = game.Stats(tower); changed.Damage = -999; if (changed.Chain != null) changed.Chain[0] = -999;
                    Require(Value(game.Stats(tower)) == before, "Stats result alias escaped: " + kind);
                    cases++;
                }
            }
            var sample = Board(5); sample.Cards[(int)CardKind.Ballistic] = 1; sample.Cards[(int)CardKind.Control] = 1;
            Require(Math.Abs(sample.Stats(Pair(sample,TowerKind.Autocannon,2,0)).Damage - 31.68f) < .0001f, "Campaign raw spear resonance changed");
            Require(Math.Abs(sample.Stats(Pair(sample,TowerKind.Binding,2,0)).SlowSeconds - 1.86f) < .0001f, "Raw binding duration resonance changed");
            Require(Math.Abs(sample.Stats(Pair(sample,TowerKind.Rupture,2,0)).Damage - 56.16f) < .0001f, "Raw rupture resonance changed");
            var allocations = new List<Allocation>();
            foreach (TowerKind kind in new[] { TowerKind.Autocannon, TowerKind.Binding, TowerKind.Rupture, TowerKind.Barrier, TowerKind.Arc }) {
                var game = Board(5); var tower = Pair(game, kind, 2, 0); float sum = 0;
                for (int i = 0; i < 1000; i++) sum += game.Stats(tower).Damage;
                long start = GC.GetAllocatedBytesForCurrentThread();
                for (int i = 0; i < 20000; i++) { var stats = game.Stats(tower); sum += stats.Damage + stats.SlowSeconds + stats.Range; }
                allocations.Add(new Allocation { Kind = kind.ToString(), Calls = 20000, Bytes = GC.GetAllocatedBytesForCurrentThread() - start, Checksum = sum });
            }
            string hash; using (var sha = SHA256.Create()) hash = string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())).Select(b => b.ToString("x2")));
            return new Report { Cases = cases, Hash = hash, Allocations = allocations.ToArray() };
        }
    }
}
