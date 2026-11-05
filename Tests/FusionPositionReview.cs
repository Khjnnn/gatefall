using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class FusionPositionReview {
        public sealed class Row {
            public int Seed, Anchor, Spent, Towers, Kills, ArcShots, BindingShots, CompletedPurchases;
            public float Range, Core, Time, CoveredWest, CoveredEast, CoveredSouth;
            public string State;
        }
        static readonly float[] Times={0,0,0,0,0,0,31.6f,33.2f,63.1f,64.8f,66.4f,78.9f,91.6f,93.3f,94.9f,124.8f,126.4f,128,129.7f,151.7f,153.3f};
        static readonly int[] Sites={3,5,0,4,1,6,13,15,0,6,0,6,2,10,12,11,7,22,0,15,13};
        static readonly TowerKind[] Kinds={TowerKind.Autocannon,TowerKind.Autocannon,TowerKind.Autocannon,TowerKind.Autocannon,TowerKind.Autocannon,TowerKind.Binding,TowerKind.Rupture,TowerKind.Rupture,TowerKind.Autocannon,TowerKind.Binding,TowerKind.Arc,TowerKind.Binding,TowerKind.Autocannon,TowerKind.Autocannon,TowerKind.Autocannon,TowerKind.Autocannon,TowerKind.Autocannon,TowerKind.Autocannon,TowerKind.Arc,TowerKind.Rupture,TowerKind.Rupture};
        static int Slot(int slot,bool front) {return !front?slot:slot==0?11:slot==11?0:slot;}
        static Row Run(int seed,bool front) {
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle{Stage=CampaignCatalog.Stages[6]});
            int n=0,card=0,arc=0,binding=0;string why;
            while(n<6){if(!g.TryBuild(Slot(Sites[n],front),Kinds[n],null,out why))throw new Exception(why);n++;}
            g.Start();
            for(int frame=0;frame<8000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard){var desired=card==0?CardKind.Range:card==1?CardKind.Credits:CardKind.Blast;g.ChooseCard(g.CardOffers.Contains(desired)?desired:g.CardOffers[0]);card++;}
                if(n<Times.Length && g.Elapsed>=Times[n]) {
                    int slot=Slot(Sites[n],front);bool done;
                    if(n==10){var a=g.TowerAt(slot);var b=g.TowerAt(6);done=g.TryFuse(0,a.Id,b.Id,slot,null,out why);}
                    else if(n==8 || n==9 || n>=18){done=g.TryUpgrade(g.TowerAt(slot).Id,null,out why);}
                    else done=g.TryBuild(slot,Kinds[n],null,out why);
                    if(done)n++;
                }
                g.Tick(.05f,new V2());
                foreach(var e in g.Events)if(e.Kind==EventKind.TowerShot){if(e.Source==(int)TowerKind.Arc)arc++;if(e.Source==(int)TowerKind.Binding)binding++;}
                g.Events.Clear();
            }
            float range=g.Stats(TowerKind.Arc,1).Range;
            var row=new Row{Seed=seed,Anchor=Slot(0,front),Range=range,Spent=g.Telemetry.CreditsSpent,Towers=g.Towers.Count,Kills=g.Kills,ArcShots=arc,BindingShots=binding,CompletedPurchases=n,Core=g.MinimumCoreHealth,Time=(float)g.Elapsed,State=g.State.ToString()};
            foreach(int lane in g.OpenLanes){float length=BattlefieldLayouts.RoadLength(g.Roads[lane]),covered=0;for(float d=0;d<length;d+=.05f){int next;V2 p=BattlefieldLayouts.Along(g.Roads[lane],d,out next);if(V2.Distance(p,g.BoardSockets[row.Anchor])<=range)covered+=Math.Min(.05f,length-d);}if(lane==0)row.CoveredWest=covered;if(lane==2)row.CoveredEast=covered;if(lane==3)row.CoveredSouth=covered;}
            return row;
        }
        public static Row[] Models(){var rows=new List<Row>();foreach(int seed in new[]{7,19,41}){rows.Add(Run(seed,false));rows.Add(Run(seed,true));}return rows.ToArray();}
    }
}
