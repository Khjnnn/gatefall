using System;
using System.Collections.Generic;

namespace Gatefall.Core
{
    // Gatefall-authored paths. The renderer and the simulation consume these same points.
    public sealed class BattlefieldLayout
    {
        public string Id;
        public int Theme;
        public V2[][] Roads;
        public V2[] Sockets;
    }

    public static class BattlefieldLayouts
    {
        public const float HalfWidth = 24, HalfHeight = 12, RoadHalfWidth = 1.05f;
        private static readonly Dictionary<int, BattlefieldLayout> cache = new Dictionary<int, BattlefieldLayout>();
        // Keep the original sockets stable; added sites use the space beside the same roads.
        public static int OriginalSocketCount(int stage) { return stage == 0 ? 7 : stage == 3 ? 12 : 10; }
        private static V2[] P(params float[] xy)
        {
            var points = new V2[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = new V2(xy[i * 2], xy[i * 2 + 1]);
            return points;
        }
        private static BattlefieldLayout L(string id, int theme, V2[] west, V2[] north, V2[] east, V2[] south = null)
        { return new BattlefieldLayout { Id = id, Theme = theme, Roads = new[] { west, north, east, south ?? new V2[0] } }; }
        private static readonly V2[] none = new V2[0];
        private static readonly BattlefieldLayout[] definitions = {
            L("junction-s-bend",0,
              P(-23,1,-17,1,-12,5,6,5,6,-4,0,-4,0,0),none,none),
            L("underpass-dual-switchback",1,
              P(-23,-4,-16,-4,-16,4,-7,4,-7,-2,-2,-2,0,0),none,
              P(23,5,15,5,15,-3,6,-3,6,2,2,2,0,0)),
            L("depot-shared-neck",0,
              P(-23,-5,-16,-5,-16,2,-8,2,-3,2,0,0),
              P(-7,11,-7,7,-12,7,-12,2,-8,2,-3,2,0,0),none),
            L("outpost-three-fronts",2,
              P(-23,4,-17,4,-17,-4,-9,-4,-9,2,-3,2,0,0),
              P(6,11,6,7,-3,7,-3,2,0,0),
              P(23,-2,16,-2,16,5,9,5,9,-3,3,-3,0,0)),
            L("tower-outer-courts",3,
              P(-23,-4,-17,-4,-17,5,-9,5,-9,-3,-3,-3,0,0),none,
              P(23,5,17,5,17,-4,9,-4,9,3,3,3,0,0)),
            L("seal-procession-square",3,
              P(-23,0,-16,0,-16,-6,-8,-6,-8,-1,-3,-1,0,0),
              P(0,11,0,8,7,8,7,2,3,2,0,0),
              P(23,0,16,0,16,6,9,6,9,1,3,1,0,0)),
            L("evacuation-three-short-south",0,
              P(-23,5,-18,5,-18,-1,-11,-1,-11,7,-4,7,-4,2,0,0),none,
              P(23,-1,19,-1,19,-6,10,-6,10,2,4,2,0,0),
              P(-13,-11,-13,-7,-6,-7,-6,-2,-2,-2,0,0)),
            L("alley-three-staggered-courts",1,
              P(-23,-6,-14,-6,-14,1,-7,1,-7,-3,-3,-3,0,0),
              P(12,11,12,6,5,6,5,1,0,1,0,0),
              P(23,-3,17,-3,17,3,10,3,10,-3,3,-3,0,0)),
            L("distribution-rooftop-loop",2,
              P(-23,0,-17,0,-17,6,-7,6,-7,0,0,0),none,
              P(23,-3,13,-3,13,4,5,4,5,0,0,0)),
            L("observatory-four-fronts-ring",1,
              P(-23,3,-17,3,-17,8,-7,8,-7,2,-3,2,0,0),
              P(8,11,8,8,16,8,16,2,6,2,6,-3,2,-3,0,0),
              P(23,-1,19,-1,19,-7,9,-7,9,-3,2,-3,0,0),
              P(-8,-11,-8,-8,-16,-8,-16,-2,-6,-2,-3,-2,0,0)),
            L("marker-offset-training-courts",3,
              P(-23,4,-16,4,-16,-4,-7,-4,-7,2,-2,2,0,0),none,
              P(23,-1,16,-1,16,-6,7,-6,7,1,0,1,0,0)),
            L("west-crossing-forward-yards",0,
              P(-23,-3,-18,-3,-18,4,-10,4,-10,-3,-4,-3,0,0),
              P(-2,11,-2,7,7,7,7,-2,2,-2,0,0),none),
            L("north-link-shared-corner",2,
              P(-23,3,-15,3,-15,-5,-6,-5,-6,0,0,0),
              P(9,11,9,7,-6,7,-6,0,0,0),
              P(23,-4,15,-4,15,3,6,3,6,0,0,0)),
            L("observer-three-short-north",1,
              P(-23,-4,-17,-4,-17,4,-8,4,-8,6,6,6,6,2,2,2,0,0),
              P(7.5f,11,7.5f,6,6,6,6,2,2,2,0,0),
              P(23,6,14,6,14,-5,6,-5,6,2,2,2,0,0))
        };

        public static BattlefieldLayout Get(CampaignStage stage)
        {
            if (stage == null) return null;
            BattlefieldLayout layout;
            if (cache.TryGetValue(stage.Index, out layout)) return layout;
            var d = definitions[stage.Index];
            layout = new BattlefieldLayout { Id = d.Id, Theme = d.Theme, Roads = d.Roads,
                Sockets = PlaceSockets(d.Roads, stage.Lanes, stage.SocketCount, stage.Index) };
            cache.Add(stage.Index, layout);
            return layout;
        }

        public static float RoadDistance(V2 point, V2[] road)
        {
            float best = float.MaxValue;
            for (int i = 1; i < road.Length; i++) {
                V2 a = road[i - 1], d = road[i] - a;
                float denominator = d.X * d.X + d.Y * d.Y;
                if (denominator < .0001f) continue;
                float t = Math.Max(0, Math.Min(1, ((point.X - a.X) * d.X + (point.Y - a.Y) * d.Y) / denominator));
                best = Math.Min(best, V2.Distance(point, a + d * t));
            }
            return best;
        }
        public static float RoadLength(V2[] road)
        { float n = 0; for (int i = 1; i < road.Length; i++) n += V2.Distance(road[i - 1], road[i]); return n; }
        public static V2 Along(V2[] road, float distance, out int nextWaypoint)
        {
            for (int i = 1; i < road.Length; i++) {
                float segment = V2.Distance(road[i - 1], road[i]);
                if (distance <= segment) { nextWaypoint = i; return road[i - 1] + (road[i] - road[i - 1]) * (segment > 0 ? distance / segment : 0); }
                distance -= segment;
            }
            nextWaypoint = road.Length - 1;
            return road[road.Length - 1];
        }

        private static V2[] PlaceSockets(V2[][] roads, int[] lanes, int count, int stage)
        {
            float maxReach = stage < 3 ? 4f : 3.25f;
            var originalCandidates = new List<V2>();
            var extraCandidates = new List<V2>();
            for (int ix = 0; ix <= 64; ix++) for (int iy = 0; iy <= 22; iy++) {
                V2 p = new V2(-20.8f + ix * .65f, -7f + iy * .65f);
                if (p.Length < 4.1f || p.X < -16 && p.Y < -5.1f || p.X > 17 && p.Y < -5.8f) continue;
                float near = float.MaxValue;
                foreach (int lane in lanes) near = Math.Min(near, RoadDistance(p, roads[lane]));
                if (near >= 2.55f && near <= maxReach) originalCandidates.Add(p);
                if (near >= 2.15f && near <= maxReach) extraCandidates.Add(p);
            }
            var result = new List<V2>();
            float[] portions = { .72f, .43f, .88f, .2f, .57f, .32f, .8f };
            float[] forwardPortions = { .16f, .34f, .50f, .65f, .82f };
            for (int n = 0; n < count; n++) {
                bool original = n < OriginalSocketCount(stage);
                var candidates = original ? originalCandidates : extraCandidates;
                int lane = lanes[n % lanes.Length], unused;
                V2 desired = Along(roads[lane], RoadLength(roads[lane]) * ((stage==10 || stage==11) && n>=OriginalSocketCount(stage) ? forwardPortions[((n-OriginalSocketCount(stage))/lanes.Length)%forwardPortions.Length] : portions[(n / lanes.Length) % portions.Length]), out unused);
                int chosen = -1; float best = float.MaxValue;
                for (int i = 0; i < candidates.Count; i++) {
                    V2 p = candidates[i]; bool separated = true;
                    foreach (V2 old in result) if (V2.Distance(p, old) < (original ? 3.45f : 2.6f)) { separated = false; break; }
                    if (!separated) continue;
                    float d = RoadDistance(p, roads[lane]);
                    float score = V2.Distance(p, desired) + (d > maxReach ? 30 : 0) + (((n + stage) % 2 == 0 ? p.Y : -p.Y) * .015f);
                    if (score < best) { best = score; chosen = i; }
                }
                if (chosen < 0) throw new InvalidOperationException("Insufficient clear tower sites in stage " + (stage + 1));
                result.Add(candidates[chosen]);
            }
            return result.ToArray();
        }
    }
}

