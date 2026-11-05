using System;
using System.Collections.Generic;
using System.Linq;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class MapRoleReview {
        public sealed class Outcome {
            public int stage,seed,budget,spent,credits,growth,towers,fusions,spawned,remaining,stars;
            public string profile,policy,state;public float core;public double end;
            public bool allSpawned;public int[] research,cards;public float[] laneDamage;public string[] ledger;
        }
        public sealed class Row {
            public Outcome outcome;
            public int firedCurrent,firedLifetime,peakRecent3,maxAlive,maxNearCore,unmatchedShots;
            public double meanRecent3;
            public int[] kindShots=new int[11];public float[] kindDamage;
        }
        public static Row Play(int seed,string profile,int budget,string policy) {
            if((seed!=7 && seed!=19 && seed!=41) || (profile!="below" && profile!="recommended") || (policy!="spear" && policy!="area-fusion"))throw new ArgumentException("Unknown map review policy");
            var account=AccountGrowthChecks.Recommended(8,0,profile=="below"?1:0);
            var rules=new CampaignService(account,null).Battle(8,false);
            var plan=new CompetentPlanReview.Plan{Spread=.5f,Cap=1,Upgrade=1};
            if(policy=="area-fusion"){plan.Rupture=.4f;plan.Upgrade=2;plan.Fuse=true;}
            var row=new Row();var fired=new HashSet<int>();var last=new Dictionary<int,double>();double total=0;int samples=0;
            Action<DefenseSimulation> observe=g=>{
                // Match actual shot events by unique occupied position+kind, instead of cooldown jumps on upgrades.
                foreach(var e in g.Events)if(e.Kind==EventKind.TowerShot){
                    Tower source=null;foreach(var t in g.Towers)if((int)t.Kind==e.Source && V2.DistanceSquared(t.Position,e.Position)<.000001f){source=t;break;}
                    if(source==null){row.unmatchedShots++;continue;}
                    fired.Add(source.Id);last[source.Id]=g.Elapsed;row.kindShots[e.Source]++;
                }
                if(g.Elapsed>=60 && g.Elapsed<=170){int n=g.Towers.Count(t=>last.ContainsKey(t.Id) && last[t.Id]>=g.Elapsed-3);total+=n;samples++;row.peakRecent3=Math.Max(row.peakRecent3,n);}
                row.maxAlive=Math.Max(row.maxAlive,g.Enemies.Count);row.maxNearCore=Math.Max(row.maxNearCore,g.Enemies.Count(e=>e.Position.Length<6));
            };
            var result=CompetentPlanReview.Play(8,seed,plan,false,budget,rules,observe);
            row.firedCurrent=result.Towers.Count(t=>fired.Contains(t.Id));row.firedLifetime=fired.Count;row.meanRecent3=samples==0?0:total/samples;row.kindDamage=(float[])result.Telemetry.TowerDamage.Clone();
            row.outcome=new Outcome{stage=9,seed=seed,profile=profile,budget=budget,policy=policy,state=result.State.ToString(),core=result.MinimumCoreHealth,end=result.Elapsed,spent=result.Telemetry.CreditsSpent,credits=result.Credits,growth=rules.EarnedGrowth,research=rules.Research,cards=result.Cards,towers=result.Towers.Count,fusions=result.Telemetry.Fusions,spawned=result.Telemetry.Spawned,remaining=result.Enemies.Count,allSpawned=result.AllWavesSpawned,stars=CampaignCatalog.Stars(result.State==RunState.Won,result.MinimumCoreHealth,result.Balance.CoreHealth),laneDamage=result.Telemetry.CoreDamageByLane,ledger=result.Telemetry.Purchases.ToArray()};
            return row;
        }
    }
}
