using System;
using System.Collections.Generic;
using System.Linq;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Selected stages only; use the existing planner, accounts and spending ceilings unchanged.
    public static class CrisisCurveReview {
        public sealed class CoreHit { public double Time; public int Lane; public float Damage; }
        public sealed class Snapshot {
            public double Time; public int Enemies, Towers, Spent, Credits; public float Core;
        }
        public sealed class Case {
            public ProgressionBalanceReview.Row Outcome;
            public int PolicyIndex, Spawned, Remaining;
            public bool AllSpawned, BossDefeated;
            public float[] LaneDamage, TowerDamage;
            public CoreHit[] Hits;
            public Snapshot[] Timeline;
        }
        public static Case Play(int stage, int seed, string profile, int budget, int policy) {
            if (stage < 10 || stage > 13 || budget < 0 || (profile != "below" && profile != "recommended") || (policy != 0 && policy != 3))
                throw new ArgumentException("Curve review requires late crisis stages, valid profile/budget and spear or blast-fusion policy");
            foreach (var entry in CampaignCatalog.Stages) BattlefieldLayouts.Get(entry);
            var account = AccountGrowthChecks.Recommended(stage, 0, profile == "below" ? 1 : 0);
            var rules = new CampaignService(account, null).Battle(stage, true);
            var plan = policy == 0 ? new CompetentPlanReview.Plan { Spread = .5f, Cap = 1, Upgrade = 1 } :
                new CompetentPlanReview.Plan { Rupture = .4f, Spread = .5f, Cap = 1, Upgrade = 2, Fuse = true };
            var cooldowns = new Dictionary<int, float>(); var fired = new HashSet<int>(); var last = new Dictionary<int, double>();
            var hits = new List<CoreHit>(); var timeline = new List<Snapshot>();
            double total = 0, end = 0; int samples = 0, peak = 0, nextSnapshot = 0;
            double[] checkpoints = { 60, 120, 150, 180 };
            Action<DefenseSimulation> observe = game => {
                foreach (var tower in game.Towers) {
                    float previous;
                    if (cooldowns.TryGetValue(tower.Id, out previous) && tower.Kind != TowerKind.Barrier && tower.Commission <= 0 && tower.Cooldown > previous + .0001f) {
                        fired.Add(tower.Id); last[tower.Id] = game.Elapsed;
                    }
                    cooldowns[tower.Id] = tower.Cooldown;
                }
                if (game.Elapsed >= 60 && game.Elapsed <= 170) {
                    int recent = game.Towers.Count(t => last.ContainsKey(t.Id) && last[t.Id] >= game.Elapsed - 3);
                    total += recent; samples++; peak = Math.Max(peak, recent); end = game.Elapsed;
                }
                foreach (var item in game.Events) if (item.Kind == EventKind.CoreHurt)
                    hits.Add(new CoreHit { Time = game.Elapsed, Lane = item.Source, Damage = item.Value });
                if (nextSnapshot < checkpoints.Length && game.Elapsed >= checkpoints[nextSnapshot]) {
                    timeline.Add(new Snapshot { Time = game.Elapsed, Enemies = game.Enemies.Count, Towers = game.Towers.Count,
                        Spent = game.Telemetry.CreditsSpent, Credits = game.Credits, Core = game.CoreHealth });
                    nextSnapshot++;
                }
            };
            var result = CompetentPlanReview.Play(stage, seed, plan, true, budget, rules, observe);
            if (result.Telemetry.CreditsSpent > budget) throw new Exception("Curve review spending ceiling exceeded");
            return new Case {
                Outcome = new ProgressionBalanceReview.Row {
                    Crisis = true, Stage = stage + 1, Seed = seed, Limit = result.TowerLimit, Budget = budget,
                    Spent = result.Telemetry.CreditsSpent, Credits = result.Credits,
                    AccountSpent = CampaignCatalog.Stages.Take(stage).Sum(s => s.FirstReward) - account.MagicCores,
                    Growth = rules.EarnedGrowth, Research = rules.Research, Cards = result.Cards,
                    Towers = result.Towers.Count, Fusions = result.Telemetry.Fusions,
                    Stars = CampaignCatalog.Stars(result.State == RunState.Won, result.MinimumCoreHealth, result.Balance.CoreHealth),
                    FiredCurrent = result.Towers.Count(t => fired.Contains(t.Id)), FiredLifetime = fired.Count,
                    PeakRecent3 = peak, ParticipationSamples = samples, ParticipationEnd = end, MeanRecent3 = samples == 0 ? 0 : total / samples,
                    Profile = profile, Policy = plan.ToString(), State = result.State.ToString(), Core = result.MinimumCoreHealth,
                    End = result.Elapsed, Ledger = result.Telemetry.Purchases.ToArray()
                },
                PolicyIndex = policy, Spawned = result.Telemetry.Spawned, Remaining = result.Enemies.Count,
                AllSpawned = result.AllWavesSpawned, BossDefeated = result.BossDefeated,
                LaneDamage = result.Telemetry.CoreDamageByLane, TowerDamage = result.Telemetry.TowerDamage,
                Hits = hits.ToArray(), Timeline = timeline.ToArray()
            };
        }
    }
}
