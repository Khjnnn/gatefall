using System;
using System.Collections.Generic;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class LateInvestmentReview {
        public sealed class Row {
            public int Stage,Seed,Cap,Spent,Credits,Towers,Kills,Spawned,Remaining,Fired,Frequent,RecentPeak,PeakEnemies,Fusions;
            public string Policy,State; public double Time; public float Core;
            public int[] Shots; public string[] Purchases,ChosenCards;
        }
        static int Closest(DefenseSimulation g,V2 p,HashSet<int> used) {
            int best=-1;float distance=float.MaxValue;
            for(int i=0;i<g.BoardSockets.Length;i++)if(!used.Contains(i)){float d=V2.DistanceSquared(g.BoardSockets[i],p);if(d<distance){distance=d;best=i;}}
            if(best>=0)used.Add(best);return best;
        }
        public static Row Run(int stage,int seed,int cap,string policy) {return RunMode(stage,seed,cap,policy,false); }
        public static Row RunMode(int stage,int seed,int cap,string policy,bool crisis) {
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle {Stage=CampaignCatalog.Stages[stage-1],Crisis=crisis,Loadout=263,MarkerUnlocked=true,ObserverUnlocked=false});
            var used=new HashSet<int>();var slots=new List<int>();
            foreach(int lane in g.OpenLanes){var road=g.Roads[lane];int s=Closest(g,road[road.Length/2],used);slots.Add(s);if(policy=="marker" && slots.Count==1)slots.Add(Closest(g,g.BoardSockets[s]+new V2(2,0),used));}
            for(int i=0;i<g.BoardSockets.Length;i++)if(used.Add(i))slots.Add(i);
            string why;g.TryBuild(slots[0],TowerKind.Autocannon,null,out why);
            g.TryBuild(slots[1],policy=="marker"?TowerKind.Marker:policy=="arc"?TowerKind.Binding:TowerKind.Autocannon,null,out why);
            int next=2;bool refilled=false;double at=3;g.Start();
            var shots=new Dictionary<int,int>();var last=new Dictionary<int,double>();var cards=new List<string>();
            int peak=0,peakRecent=0;var siteShots=new int[g.BoardSockets.Length];
            for(int f=0;f<7200 && g.State!=RunState.Won && g.State!=RunState.Lost;f++){
                if(g.AwaitingCard){CardKind pick=g.CardOffers[0];cards.Add(pick.ToString());g.ChooseCard(pick);}
                if(g.Elapsed>=at){
                    bool acted=false;
                    if(policy=="arc" && g.Telemetry.Fusions==0){var a=g.Towers.Find(t=>t.Kind==TowerKind.Autocannon);var b=g.Towers.Find(t=>t.Kind==TowerKind.Binding);if(a!=null && b!=null && g.CanFuse(0,a.Id,b.Id,a.Socket)=="")acted=g.TryFuse(0,a.Id,b.Id,a.Socket,null,out why);}
                    if(!acted && policy=="arc" && g.Telemetry.Fusions>0 && !refilled){acted=g.TryBuild(slots[1],TowerKind.Autocannon,null,out why);if(acted)refilled=true;}
                    if(!acted)foreach(var t in g.Towers.ToArray())if(t.Level==1 && g.CanUpgrade(t.Id)==""){acted=g.TryUpgrade(t.Id,null,out why);if(acted)break;}
                    if(!acted && next<slots.Count && g.Towers.Count<cap && g.Credits>=40){if(g.TryBuild(slots[next],TowerKind.Autocannon,null,out why))next++;}
                    at=g.Elapsed+2;
                }
                g.Tick(.05f,new V2());peak=Math.Max(peak,g.Enemies.Count);
                foreach(var e in g.Events)if(e.Kind==EventKind.TowerShot)foreach(var t in g.Towers)if(V2.DistanceSquared(t.Position,e.Position)<.001f){int n;shots.TryGetValue(t.Id,out n);shots[t.Id]=n+1;last[t.Id]=g.Elapsed;siteShots[t.Socket]++;break;}
                int recent=0;foreach(var t in g.Towers){double when;if(last.TryGetValue(t.Id,out when)&&g.Elapsed-when<=3)recent++;}peakRecent=Math.Max(peakRecent,recent);
                g.Events.Clear();
            }
            int fired=0,frequent=0;foreach(var t in g.Towers){int n;if(shots.TryGetValue(t.Id,out n)){if(n>0)fired++;if(n>=10)frequent++;}}
            return new Row{Stage=stage,Seed=seed,Cap=cap,Policy=policy,State=g.State.ToString(),Time=g.Elapsed,Core=g.MinimumCoreHealth,Spent=g.Telemetry.CreditsSpent,Credits=g.Credits,Towers=g.Towers.Count,Kills=g.Kills,Spawned=g.Telemetry.Spawned,Remaining=g.Enemies.Count,Fired=fired,Frequent=frequent,RecentPeak=peakRecent,PeakEnemies=peak,Fusions=g.Telemetry.Fusions,Shots=siteShots,Purchases=g.Telemetry.Purchases.ToArray(),ChosenCards=cards.ToArray()};
        }
    }
}
