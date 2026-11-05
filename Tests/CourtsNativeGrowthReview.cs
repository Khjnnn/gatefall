using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Captured native tenths/order/positions; unknown native RNG and .05 tick make this a model, not a replay.
    public static class CourtsNativeGrowthReview {
        public sealed class Command { public double Time; public string Action; public TowerKind Kind; public int Site; }
        public sealed class TowerRow { public int Site,Id,Level,Shots; public string Kind; }
        public sealed class Row {
            public int Seed,Spent,Credits,Towers,CompletedOrders,Kills,Spawned,Remaining,FinalFired,RecentPeak,PeakEnemies;
            public float Core,Time,MaxDelay; public bool AllSpawned; public string State,Choice;
            public string[] Cards,PurchaseLog; public double[] ActualTimes; public int[] SiteShots,KindShots;
            public float[] KindDamage; public TowerRow[] FinalTowers;
        }
        public static Row Run(Command[] fixture,int seed,bool lateUpgrade) {
            if(fixture.Length!=26)throw new Exception("Expected26 observed receipts");
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle {Stage=CampaignCatalog.Stages[4],Loadout=135});
            if(g.Credits!=280 || g.BoardSockets.Length!=24 || g.OpenLanes.Length!=2)throw new Exception("Stage5 contract differs");
            string why;int next=0;var actual=new List<double>();
            while(next<fixture.Length && fixture[next].Time==0) {var c=fixture[next];if(c.Action!="build" || !g.TryBuild(c.Site,c.Kind,null,out why))throw new Exception("Initial purchase rejected");actual.Add(0);next++;}
            if(next!=6 || g.Telemetry.CreditsSpent!=270)throw new Exception("Initial6/270 differs");
            int card=0,peakRecent=0,peakEnemies=0;float delay=0;
            var cards=new List<string>();var siteShots=new int[24];var kindShots=new int[TowerCatalog.All.Length];
            var shots=new Dictionary<int,int>();var last=new Dictionary<int,double>();g.Events.Clear();g.Start();
            for(int frame=0;frame<10000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard) {var wanted=card==1?CardKind.Range:CardKind.Ballistic;var chosen=g.CardOffers.Contains(wanted)?wanted:g.CardOffers[0];cards.Add(chosen.ToString());if(!g.ChooseCard(chosen))throw new Exception("Card rejected");card++;}
                if(next<fixture.Length && g.Elapsed>=fixture[next].Time) {
                    var c=fixture[next];bool done;
                    if(lateUpgrade && next==25) {var tower=g.TowerAt(3);if(c.Site!=9 || c.Time!=169.4 || c.Action!="build" || tower==null || tower.Kind!=TowerKind.Autocannon || tower.Level!=1)throw new Exception("Late one-order replacement differs");done=g.TryUpgrade(tower.Id,null,out why);}
                    else if(c.Action=="build")done=g.TryBuild(c.Site,c.Kind,null,out why);
                    else if(c.Action=="upgrade") {var tower=g.TowerAt(c.Site);if(tower==null || tower.Kind!=c.Kind)throw new Exception("Upgrade kind/site differs");done=g.TryUpgrade(tower.Id,null,out why);}
                    else {int recipe=c.Kind==TowerKind.Arc?0:1;int other=c.Kind==TowerKind.Arc?11:20;var a=g.TowerAt(c.Site);var b=g.TowerAt(other);if(a==null || b==null || a.Kind!=TowerKind.Autocannon || b.Kind!=(c.Kind==TowerKind.Arc?TowerKind.Binding:TowerKind.Rupture))throw new Exception("Fusion materials differ");done=g.TryFuse(recipe,a.Id,b.Id,c.Site,null,out why);}
                    if(done){delay=Math.Max(delay,(float)(g.Elapsed-c.Time));actual.Add(g.Elapsed);next++;}
                    if(g.Telemetry.CreditsSpent>1095)throw new Exception("Captured spend cap exceeded");
                }
                g.Tick(.05f,new V2());
                foreach(var e in g.Events)if(e.Kind==EventKind.TowerShot) {
                    int site=-1;for(int i=0;i<24;i++)if(V2.DistanceSquared(g.BoardSockets[i],e.Position)<.001f){site=i;break;}
                    var tower=site<0?null:g.TowerAt(site);if(tower==null || (int)tower.Kind!=e.Source)throw new Exception("Shot site/kind identity differs");
                    siteShots[site]++;kindShots[e.Source]++;if(!shots.ContainsKey(tower.Id))shots[tower.Id]=0;shots[tower.Id]++;last[tower.Id]=g.Elapsed;
                }
                int recent=0,alive=0;foreach(var tower in g.Towers){double time;if(last.TryGetValue(tower.Id,out time)&&g.Elapsed-time<=3)recent++;}
                foreach(var enemy in g.Enemies)if(enemy.Health>0)alive++;peakRecent=Math.Max(peakRecent,recent);peakEnemies=Math.Max(peakEnemies,alive);g.Events.Clear();
            }
            var final=new List<TowerRow>();int fired=0;foreach(var tower in g.Towers){int n;shots.TryGetValue(tower.Id,out n);if(n>0)fired++;final.Add(new TowerRow{Site=tower.Socket,Id=tower.Id,Kind=tower.Kind.ToString(),Level=tower.Level,Shots=n});}
            return new Row{Seed=seed,Choice=lateUpgrade?"last-build9-to-upgrade3":"captured-control",State=g.State.ToString(),Core=g.MinimumCoreHealth,Time=(float)g.Elapsed,Spent=g.Telemetry.CreditsSpent,Credits=g.Credits,Towers=g.Towers.Count,CompletedOrders=next,Kills=g.Kills,Spawned=g.Telemetry.Spawned,Remaining=g.Enemies.Count,AllSpawned=g.AllWavesSpawned,FinalFired=fired,RecentPeak=peakRecent,PeakEnemies=peakEnemies,MaxDelay=delay,Cards=cards.ToArray(),PurchaseLog=g.Telemetry.Purchases.ToArray(),ActualTimes=actual.ToArray(),SiteShots=siteShots,KindShots=kindShots,KindDamage=(float[])g.Telemetry.TowerDamage.Clone(),FinalTowers=final.ToArray()};
        }
    }
}
