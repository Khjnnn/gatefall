using System;
using System.Linq;
using System.Collections.Generic;
using Gatefall.Core;
namespace Gatefall.Tests
{
    public static class ChapterTwoPlayReview
    {
        public sealed class Row
        {
            public string Stage,Policy,Outcome;public int Seed,Growth,Spent,Remaining,Towers,Kills,Spawned,Total,Fusions,Supplies,CardOffers,Catalysts;
            public bool Crisis,AllSpawned;public float MinimumCore;public double End,LastSpawn,SealAt=-1,BarrageAt=-1;
            public int[] Shots,Cards;public float[] Damage;public string[] Ledger;
        }
        public static Row[] Run(bool pilot,bool crisis=false)
        {
            foreach(var stage in CampaignCatalog.Stages)BattlefieldLayouts.Get(stage);
            var list=new List<Row>();
            foreach(int stage in pilot?new[]{14,19}:new[]{14,15,16,17,18,19})foreach(int seed in pilot||crisis?new[]{7,41}:new[]{7,19,41})
            foreach(bool below in crisis?new[]{false}:new[]{false,true})foreach(string policy in new[]{"mixed-upgrades","fusion-refill"}){
                var p=ChapterTwoChecks.Entry();for(int i=14;i<stage;i++){p.FirstCleared[i]=true;p.BestStars[i]=3;}
                p.Growth[0]=below?4:5;p.Research=below?new[]{2,1,1,1}:new[]{3,2,2,2};
                bool scatter=p.FirstCleared[15];p.Loadout=scatter?(1<<0)|(1<<1)|(1<<2)|(1<<11):135;
                var battle=new CampaignService(p,null).Battle(stage,crisis);
                var plan=new CompetentPlanReview.Plan{Rupture=.3f,Binding=.15f,Scatter=scatter?.25f:0,Spread=3,Cap=1,Upgrade=2,
                    Fuse=policy=="fusion-refill",Capstone=scatter && policy=="fusion-refill"};
                var shots=new int[TowerCatalog.All.Length];double seal=-1,barrage=-1;
                var g=CompetentPlanReview.Play(stage,seed,plan,crisis,1150,battle,x=>{
                    foreach(var e in x.Events)if(e.Kind==EventKind.TowerShot && e.Source>=0)shots[e.Source]++;
                    if(seal<0 && x.Towers.Any(t=>t.Kind==TowerKind.Seal))seal=x.Elapsed;
                    if(barrage<0 && x.Towers.Any(t=>t.Kind==TowerKind.Barrage))barrage=x.Elapsed;
                });
                list.Add(new Row{Stage=CampaignCatalog.StageId(stage),Policy=policy,Seed=seed,Growth=p.Growth[0],Crisis=crisis,Outcome=g.State.ToString(),MinimumCore=g.MinimumCoreHealth,
                    Spent=g.Telemetry.CreditsSpent,Remaining=g.Credits,Towers=g.Towers.Count,Kills=g.Kills,Spawned=g.Telemetry.Spawned,Total=g.Schedule.Sum(s=>s.Count),Fusions=g.Telemetry.Fusions,
                    End=g.Elapsed,LastSpawn=g.Schedule.Max(s=>s.Start+(s.Count-1)*s.Interval),SealAt=seal,BarrageAt=barrage,AllSpawned=g.AllWavesSpawned,
                    Supplies=g.SuppliesGranted,CardOffers=g.Telemetry.CardOffers,Catalysts=g.Telemetry.CoresGranted,Shots=shots,Damage=g.Telemetry.TowerDamage,Cards=g.Cards,Ledger=g.Telemetry.Purchases.ToArray()});
            }return list.ToArray();
        }
    }
}
