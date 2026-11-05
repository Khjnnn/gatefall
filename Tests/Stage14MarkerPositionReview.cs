using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class Stage14MarkerPositionReview {
        public sealed class Command {public double Time;public string Action;public TowerKind Kind;public int Site;}
        public sealed class Row {
            public int Seed,MarkerSite,Spent,Credits,Towers,CompletedOrders,Kills,Spawned,Remaining,MarkedHits,MarkerShots,FiredSites,FinalFiredTowers,PeakRecent3,PeakEnemies;
            public float Core,Time,MarkerDamage,MarkBonus,MaxPurchaseDelay;public bool AllSpawned;public string State;
            public int[] SiteShots;public string[] Cards,PurchaseLog;
        }
        static Row Run(Command[] fixture,int seed,int markerSite) {
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle {Stage=CampaignCatalog.Stages[13],Loadout=263,MarkerUnlocked=true,ObserverUnlocked=false});
            if(g.Credits!=320 || fixture.Length!=23 || !g.Rules.MobileWaveCalls)throw new Exception("Expected native prefix/starting economy/call eligibility");
            foreach(var c in fixture)if(c.Kind!=TowerKind.Marker && c.Site==markerSite)throw new Exception("Marker candidate collides with purchase");
            string why;int next=0;
            while(next<fixture.Length && fixture[next].Time==0){var c=fixture[next];if(!g.TryBuild(c.Kind==TowerKind.Marker?markerSite:c.Site,c.Kind,null,out why))throw new Exception("Initial purchase: "+why);next++;}
            if(next!=7 || g.Telemetry.CreditsSpent!=315)throw new Exception("Initial seven315 differ");
            var cards=new List<string>();int card=0,markerShots=0,peakRecent=0,peakEnemies=0;float maxDelay=0;
            var shots=new int[g.BoardSockets.Length];var perId=new Dictionary<int,int>();var lastId=new Dictionary<int,double>();
            g.Events.Clear();g.Start();
            for(int frame=0;frame<8000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++){
                if(g.AwaitingCard){var wanted=card<2?CardKind.Blast:CardKind.Ballistic;var chosen=g.CardOffers.Contains(wanted)?wanted:g.CardOffers[0];cards.Add(chosen.ToString());if(!g.ChooseCard(chosen))throw new Exception("Card rejected");card++;}
                if(next<fixture.Length && g.Elapsed>=fixture[next].Time){
                    var c=fixture[next];int site=c.Kind==TowerKind.Marker?markerSite:c.Site;bool done;
                    if(c.Action=="build")done=g.TryBuild(site,c.Kind,null,out why);
                    else if(c.Action=="upgrade")done=g.TryUpgrade(g.TowerAt(site).Id,null,out why);
                    else done=g.TryFuse(0,g.TowerAt(22).Id,g.TowerAt(8).Id,22,null,out why);
                    if(done){maxDelay=Math.Max(maxDelay,(float)(g.Elapsed-c.Time));next++;}
                    if(g.Telemetry.CreditsSpent>940)throw new Exception("Common budget940 exceeded");
                }
                g.Tick(.05f,new V2());
                foreach(var e in g.Events)if(e.Kind==EventKind.TowerShot){
                    int site=-1;for(int i=0;i<shots.Length;i++)if(V2.DistanceSquared(g.BoardSockets[i],e.Position)<.001f){site=i;shots[i]++;break;}
                    if(site<0)throw new Exception("Shot site absent");var tower=g.TowerAt(site);
                    if(tower==null || (int)tower.Kind!=e.Source)throw new Exception("Shot source identity differs");
                    if(!perId.ContainsKey(tower.Id))perId[tower.Id]=0;perId[tower.Id]++;lastId[tower.Id]=g.Elapsed;
                    if(e.Source==(int)TowerKind.Marker)markerShots++;
                }
                int recent=0,alive=0;foreach(var t in g.Towers){double time;if(lastId.TryGetValue(t.Id,out time) && g.Elapsed-time<=3)recent++;}
                foreach(var e in g.Enemies)if(e.Health>0)alive++;peakRecent=Math.Max(peakRecent,recent);peakEnemies=Math.Max(peakEnemies,alive);g.Events.Clear();
            }
            int fired=0,finalFired=0;foreach(int n in shots)if(n>0)fired++;foreach(var t in g.Towers)if(perId.ContainsKey(t.Id) && perId[t.Id]>0)finalFired++;
            return new Row{Seed=seed,MarkerSite=markerSite,Spent=g.Telemetry.CreditsSpent,Credits=g.Credits,Towers=g.Towers.Count,CompletedOrders=next,Kills=g.Kills,Spawned=g.Telemetry.Spawned,Remaining=g.Enemies.Count,
                MarkedHits=g.Telemetry.MarkedHits,MarkerShots=markerShots,FiredSites=fired,FinalFiredTowers=finalFired,PeakRecent3=peakRecent,PeakEnemies=peakEnemies,Core=g.MinimumCoreHealth,Time=(float)g.Elapsed,
                MarkerDamage=g.Telemetry.TowerDamage[(int)TowerKind.Marker],MarkBonus=g.Telemetry.MarkBonusDamage,MaxPurchaseDelay=maxDelay,AllSpawned=g.AllWavesSpawned,State=g.State.ToString(),SiteShots=shots,Cards=cards.ToArray(),PurchaseLog=g.Telemetry.Purchases.ToArray()};
        }
        public static Row[] Models(Command[] fixture,int[] seeds,int[] markers){var rows=new List<Row>();foreach(int seed in seeds)foreach(int site in markers)rows.Add(Run(fixture,seed,site));return rows.ToArray();}
    }
}
