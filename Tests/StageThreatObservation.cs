using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Reads completed ticks/events before the harness clears them; never changes combat state.
    public sealed class StageThreatObservation {
        public int PeakAlive, PeakHeavyNearSwarm, CoreDamageEvents, PeakActive3Seconds;
        public double LateHeavyNearSwarmSeconds, LateHeavyIsolatedSeconds, LateHeavyNearCoreSeconds;
        public double LastHeavyKill = -1, LastLightKill = -1, FirstCoreDamage = -1;
        public float CoreDamage, MinimumHeavyRadius = float.MaxValue;
        public readonly int[] SiteShots, KindShots = new int[TowerCatalog.All.Length];
        readonly double[] lastShots;
        readonly HashSet<int> coreApproaches = new HashSet<int>();
        public StageThreatObservation(int sites) {
            SiteShots = new int[sites]; lastShots = new double[sites];
            for (int i = 0; i < sites; i++) lastShots[i] = -99;
        }
        public int CoreApproachStrongTargets { get { return coreApproaches.Count; } }
        public int FiredSites { get { int n = 0; foreach (int s in SiteShots) if (s > 0) n++; return n; } }
        public int FrequentFiringSites { get { int n = 0; foreach (int s in SiteShots) if (s >= 10) n++; return n; } }
        public void Sample(DefenseSimulation g, float dt) {
            foreach (CombatEvent e in g.Events) {
                if (e.Kind == EventKind.CoreHurt) {
                    CoreDamageEvents++; CoreDamage += e.Value;
                    if (FirstCoreDamage < 0) FirstCoreDamage = g.Elapsed;
                }
                if (e.Kind == EventKind.Kill) {
                    if (e.Source == (int)EnemyKind.Brute || e.Source == (int)EnemyKind.Siege) LastHeavyKill = g.Elapsed;
                    else if (e.Source == (int)EnemyKind.Grunt || e.Source == (int)EnemyKind.Runner) LastLightKill = g.Elapsed;
                }
                if (e.Kind == EventKind.TowerShot) {
                    if (e.Source >= 0 && e.Source < KindShots.Length) KindShots[e.Source]++;
                    for (int s = 0; s < SiteShots.Length; s++) if (V2.DistanceSquared(e.Position, g.BoardSockets[s]) < .001f) {
                        SiteShots[s]++; lastShots[s] = g.Elapsed; break;
                    }
                }
            }
            int alive = 0, nearby = 0;
            foreach (Enemy heavy in g.Enemies) {
                if (heavy.Health <= 0) continue;
                alive++;
                if (g.Elapsed < 90 || heavy.Kind != EnemyKind.Brute && heavy.Kind != EnemyKind.Siege) continue;
                float radius = heavy.Position.Length;
                MinimumHeavyRadius = Math.Min(MinimumHeavyRadius, radius);
                bool closeSwarm = false;
                foreach (Enemy other in g.Enemies) if (other.Health > 0 && other.Swarm && V2.DistanceSquared(heavy.Position, other.Position) <= 27.04f) {
                    closeSwarm = true; break;
                }
                // Geometric coincidence within5.2 units, not additional delay or causal pressure.
                if (closeSwarm) { nearby++; LateHeavyNearSwarmSeconds += dt; }
                else LateHeavyIsolatedSeconds += dt;
                if (radius <= 5) LateHeavyNearCoreSeconds += dt;
                if (heavy.BlockOwner == 0 && heavy.Waypoint == g.Roads[heavy.Lane].Length - 1 &&
                    V2.Distance(heavy.Position, g.Roads[heavy.Lane][heavy.Waypoint]) <= 1.14f) coreApproaches.Add(heavy.Id);
            }
            PeakAlive = Math.Max(PeakAlive, alive); PeakHeavyNearSwarm = Math.Max(PeakHeavyNearSwarm, nearby);
            int active = 0;
            foreach (Tower t in g.Towers) if (g.Elapsed - lastShots[t.Socket] <= 3) active++;
            PeakActive3Seconds = Math.Max(PeakActive3Seconds, active);
        }
    }
}
