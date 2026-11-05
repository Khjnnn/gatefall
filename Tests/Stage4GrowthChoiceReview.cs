using System;
using System.Collections.Generic;
using System.Reflection;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class Stage4GrowthChoiceReview {
        public sealed class Purchase {
            public string Action;public int Site,Spent,Credits,Towers;public double Target,Actual;
        }
        public sealed class Row {
            public int Seed,Spent,Credits,Towers,Kills,Spawned,Remaining,CompletedOrders,Fusions,PeakEnemies,FiredSites,FrequentSites,PeakRecent3,CoreDamageEvents,HunterDowns;
            public string Policy,State;public float Core,PlayerHealth,Time,MaxPurchaseDelay;public bool AllSpawned;
            public int[] InitialSites,SiteShots,KindShots;public float[] KindDamage,RequestedCoreDamageByLane;public string[] Cards,PurchaseLog;public Purchase[] Orders;
        }
        static Row Run(int seed,string policy,bool easternAnchor=false) {
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle {Stage=CampaignCatalog.Stages[3]});
            var order=(List<int>)typeof(AlleyDensityReview).GetMethod("Order",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{g});
            int anchor=order[0],partner=-1;float best=float.MaxValue;
            foreach(int site in order)if(site!=anchor){float d=V2.Distance(g.BoardSockets[site],g.BoardSockets[anchor]);if(d<best){best=d;partner=site;}}
            order.Remove(partner);order.Insert(1,partner);string why;
            var initial=new List<int>();for(int i=0;i<6;i++){
                if(!g.TryBuild(order[i],i==1?TowerKind.Binding:TowerKind.Autocannon,null,out why))throw new Exception("Initial purchase: "+why);
                initial.Add(order[i]);
            }
            if(g.Telemetry.CreditsSpent!=245 || g.Towers.Count!=6)throw new Exception("Initial245/6 differs");
            // Same initial six kinds/sites and Binding19; change the Spear material upgraded at15s.
            if(easternAnchor)anchor=order[3];
            double[] targets=policy=="upgrade"?new double[]{15,35,65,80,95}:new double[]{15,35,65,80};
            var purchases=new List<Purchase>();var cards=new List<string>();int next=0,peakAlive=0,peakRecent=0,coreEvents=0,downs=0;
            var shots=new int[g.BoardSockets.Length];var kindShots=new int[TowerCatalog.All.Length];var laneDamage=new float[4];var last=new double[shots.Length];
            for(int i=0;i<last.Length;i++)last[i]=-99;
            g.Start();float maxDelay=0;
            for(int frame=0;frame<8000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++){
                if(g.AwaitingCard){cards.Add(g.CardOffers[0].ToString());if(!g.ChooseCard(g.CardOffers[0]))throw new Exception("Card rejected");}
                if(next<targets.Length && g.Elapsed>=targets[next]){
                    bool done=false;int site=-1;string action="";
                    if(policy=="expand"){
                        if(next<3){site=order[6+next];action="build-spear";done=g.TryBuild(site,TowerKind.Autocannon,null,out why);}
                        else {site=anchor;action="upgrade-spear";done=g.TryUpgrade(g.TowerAt(site).Id,null,out why);}
                    }else if(policy=="upgrade"){
                        site=order[next==0?0:next+1];action="upgrade-spear";done=g.TryUpgrade(g.TowerAt(site).Id,null,out why);
                    }else {
                        site=next==1 || next==3?partner:anchor;
                        if(next<2){action=next==0?"upgrade-spear":"upgrade-binding";done=g.TryUpgrade(g.TowerAt(site).Id,null,out why);}
                        else if(next==2){action="fuse-arc";done=g.TryFuse(0,g.TowerAt(anchor).Id,g.TowerAt(partner).Id,anchor,null,out why);}
                        else {action="refill-spear";done=g.TryBuild(partner,TowerKind.Autocannon,null,out why);}
                    }
                    if(done){maxDelay=Math.Max(maxDelay,(float)(g.Elapsed-targets[next]));purchases.Add(new Purchase{Action=action,Site=site,Target=targets[next],Actual=g.Elapsed,Spent=g.Telemetry.CreditsSpent,Credits=g.Credits,Towers=g.Towers.Count});next++;}
                    if(g.Telemetry.CreditsSpent>395)throw new Exception("Budget exceeded");
                }
                g.Tick(.05f,new V2()); // Normal hero autoattack remains; no manual skill/support/movement/call.
                foreach(CombatEvent e in g.Events){
                    if(e.Kind==EventKind.TowerShot){
                        if(e.Source>=0 && e.Source<kindShots.Length)kindShots[e.Source]++;
                        for(int i=0;i<shots.Length;i++)if(V2.DistanceSquared(g.BoardSockets[i],e.Position)<.001f){shots[i]++;last[i]=g.Elapsed;break;}
                    }
                    if(e.Kind==EventKind.CoreHurt){coreEvents++;if(e.Source>=0 && e.Source<4)laneDamage[e.Source]+=e.Value;}
                    if(e.Kind==EventKind.HunterDown)downs++;
                }
                int alive=0,recent=0;foreach(var e in g.Enemies)if(e.Health>0)alive++;
                foreach(var t in g.Towers)if(g.Elapsed-last[t.Socket]<=3)recent++;
                peakAlive=Math.Max(peakAlive,alive);peakRecent=Math.Max(peakRecent,recent);g.Events.Clear();
            }
            int fired=0,frequent=0;foreach(int n in shots){if(n>0)fired++;if(n>=10)frequent++;}
            return new Row{Seed=seed,Policy=policy,Spent=g.Telemetry.CreditsSpent,Credits=g.Credits,Towers=g.Towers.Count,Kills=g.Kills,Spawned=g.Telemetry.Spawned,Remaining=g.Enemies.Count,
                CompletedOrders=next,Fusions=g.Telemetry.Fusions,PeakEnemies=peakAlive,FiredSites=fired,FrequentSites=frequent,PeakRecent3=peakRecent,CoreDamageEvents=coreEvents,HunterDowns=downs,
                State=g.State.ToString(),Core=g.MinimumCoreHealth,PlayerHealth=g.PlayerHealth,Time=(float)g.Elapsed,MaxPurchaseDelay=maxDelay,AllSpawned=g.AllWavesSpawned,
                InitialSites=initial.ToArray(),SiteShots=shots,KindShots=kindShots,KindDamage=(float[])g.Telemetry.TowerDamage.Clone(),RequestedCoreDamageByLane=laneDamage,Cards=cards.ToArray(),PurchaseLog=g.Telemetry.Purchases.ToArray(),Orders=purchases.ToArray()};
        }
        public static Row[] Models(){var rows=new List<Row>();foreach(int seed in new[]{7,19,41})foreach(string policy in new[]{"expand","upgrade","fusion"})rows.Add(Run(seed,policy));return rows.ToArray();}
        public static Row Control(){return Run(7,"fusion");}
        public static Row[] EasternFusionModels(){var rows=new List<Row>();foreach(int seed in new[]{7,19,41})rows.Add(Run(seed,"fusion",true));return rows.ToArray();}
    }
}
