using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class AlleyReadabilityChecks {
        static List<V2> OutsideCore(V2[] road) {
            var samples=new List<V2>();
            float length=BattlefieldLayouts.RoadLength(road);
            for(float d=0;d<=length;d+=.1f) {
                int unused;V2 p=BattlefieldLayouts.Along(road,d,out unused);
                if(p.Length>=5)samples.Add(p);
            }
            return samples;
        }
        public static string Verify() {
            var stage=CampaignCatalog.Stages[7];var layout=BattlefieldLayouts.Get(stage);
            var north=OutsideCore(layout.Roads[1]);var east=OutsideCore(layout.Roads[2]);
            if(north.Count==0 || east.Count==0)throw new Exception("Stage8 must have distinct north and east approaches");
            float minimum=float.MaxValue;
            foreach(V2 a in north)foreach(V2 b in east)minimum=Math.Min(minimum,V2.Distance(a,b));
            // Roads remain logically separate here; their curb silhouettes must leave a visible gap.
            if(minimum<2.8f)throw new Exception("Stage8 disconnected north/east roads look joined: centerlines "+minimum);
            return "PASS stage8 north/east approach separation "+minimum.ToString("F2")+" outside radius5; sampled geometry, Android rendering checked separately.";
        }
    }
}
