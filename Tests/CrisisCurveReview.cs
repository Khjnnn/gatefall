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
            public int PolicyIndex, Spawned, Remaining, Hunter, Motion, HunterShots, HunterDowns, HunterRevives, SkillUses;
            public double HunterDownSeconds, MovementDistance;
            public bool AllSpawned, BossDefeated;
            public float[] LaneDamage, TowerDamage;
            public CoreHit[] Hits;
            public Snapshot[] Timeline;
        }
        public static Case Play(int stage, int seed, string profile, int budget, int policy, int hunter = 0, int motion = 0) {
            if (stage < 10 || stage > 13 || budget < 0 || (profile != "below" && profile != "recommended") || (policy != 0 && policy != 3))
                throw new ArgumentException("Curve review requires late crisis stages, valid profile/budget and spear or blast-fusion policy");
            if (hunter < 0 || hunter > 2 || motion < 0 || motion > 2) throw new ArgumentException("Unknown hunter/motion policy");
            foreach (var entry in CampaignCatalog.Stages) BattlefieldLayouts.Get(entry);
            var account = AccountGrowthChecks.Recommended(stage, hunter, profile == "below" ? 1 : 0);
            var rules = new CampaignService(account, null).Battle(stage, true);
            var plan = policy == 0 ? new CompetentPlanReview.Plan { Spread = .5f, Cap = 1, Upgrade = 1 } :
                new CompetentPlanReview.Plan { Rupture = .4f, Spread = .5f, Cap = 1, Upgrade = 2, Fuse = true };
            var cooldowns = new Dictionary<int, float>(); var fired = new HashSet<int>(); var last = new Dictionary<int, double>();
            var hits = new List<CoreHit>(); var timeline = new List<Snapshot>();
            double total = 0, end = 0; int samples = 0, peak = 0, nextSnapshot = 0;
            int hunterShots = 0, hunterDowns = 0, hunterRevives = 0, skillUses = 0;
            double downSeconds = 0, distance = 0, previousTime = 0;
            V2 previousPosition = new V2(0, -2.2f);
            double[] checkpoints = { 60, 120, 150, 180 };
            Action<DefenseSimulation> observe = game => {
                bool revived = false;
                foreach (var item in game.Events) {
                    if (item.Kind == EventKind.HunterShot) hunterShots++;
                    if (item.Kind == EventKind.HunterDown) hunterDowns++;
                    if (item.Kind == EventKind.HunterRevived) { hunterRevives++; revived = true; }
                }
                if (game.HunterDown) downSeconds += game.Elapsed - previousTime;
                if (!revived) distance += V2.Distance(previousPosition, game.PlayerPosition);
                previousTime = game.Elapsed; previousPosition = game.PlayerPosition;
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
            Func<DefenseSimulation, V2> move = null;
            if (motion > 0) move = game => {
                Enemy urgent = null; float closest = float.MaxValue;
                foreach (var enemy in game.Enemies) if (enemy.Health > 0 && enemy.Position.Length < closest) { closest = enemy.Position.Length; urgent = enemy; }
                V2 target = urgent == null ? new V2(0, -2.2f) : urgent.Position + new V2(0, -.8f);
                if (motion == 2 && urgent != null && V2.Distance(game.PlayerPosition, urgent.Position) <= 3 && game.UseSkill()) skillUses++;
                V2 delta = target - game.PlayerPosition;
                return delta.Length > .2f ? delta.Normalized : new V2();
            };
            var result = CompetentPlanReview.Play(stage, seed, plan, true, budget, rules, observe, move);
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
                PolicyIndex = policy, Hunter = hunter, Motion = motion, HunterShots = hunterShots, HunterDowns = hunterDowns, HunterRevives = hunterRevives,
                SkillUses = skillUses, HunterDownSeconds = downSeconds, MovementDistance = distance,
                Spawned = result.Telemetry.Spawned, Remaining = result.Enemies.Count,
                AllSpawned = result.AllWavesSpawned, BossDefeated = result.BossDefeated,
                LaneDamage = result.Telemetry.CoreDamageByLane, TowerDamage = result.Telemetry.TowerDamage,
                Hits = hits.ToArray(), Timeline = timeline.ToArray()
            };
        }
    }
}
