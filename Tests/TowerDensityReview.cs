using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    // The same purchasing and firing-window policy can run against the preserved old core.
    public static class TowerDensityReview
    {
        public sealed class Result
        {
            public int Stage, Seed, Sites, StartingCredits, Towers, Spent, Kills, Fusions, FiredSites, FrequentFiringSites, PeakActive3Seconds, PeakProjectiles;
            public string Policy, State; public float Core, Time;
        }
        static List<int> Order(DefenseSimulation g)
        {
            var order = new List<int>();
            foreach(float portion in new[]{.55f,.35f,.78f,.18f,.9f}) foreach(int lane in g.OpenLanes) {
                int next; V2 target=BattlefieldLayouts.Along(g.Roads[lane],BattlefieldLayouts.RoadLength(g.Roads[lane])*portion,out next);
                int best=-1;float score=float.MaxValue;
                for(int slot=0;slot<g.BoardSockets.Length;slot++) if(!order.Contains(slot)) {
                    float distance=V2.Distance(target,g.BoardSockets[slot]);
                    if(BattlefieldLayouts.RoadDistance(g.BoardSockets[slot],g.Roads[lane])>5.1f)distance+=50;
                    if(distance<score){score=distance;best=slot;}
                }
                if(best>=0)order.Add(best);
            }
            for(int i=0;i<g.BoardSockets.Length;i++)if(!order.Contains(i))order.Add(i);
            return order;
        }
        static TowerKind Kind(int sequence,int stage,string policy)
        {
            if(policy=="mixed" && sequence==1 && stage>=1)return TowerKind.Binding;
            if(policy=="mixed" && sequence==3 && stage>=3)return TowerKind.Rupture;
            return TowerKind.Autocannon;
        }
        static Result Run(int index,int seed,string policy)
        {
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle {Stage=CampaignCatalog.Stages[index]});
            var order=Order(g);string why;int initial=g.Credits,anchor=order[0],partner=order.Count>1?order[1]:order[0];
            if(policy=="mixed" && index>=2) {
                float closest=float.MaxValue;foreach(int slot in order)if(slot!=anchor){float d=V2.Distance(g.BoardSockets[slot],g.BoardSockets[anchor]);if(d<closest){closest=d;partner=slot;}}
                order.Remove(partner);order.Insert(1,partner);
            }
            int desired=index<3?10:index<9?14:16;
            int[] shots=new int[g.BoardSockets.Length];double[] last=new double[shots.Length];for(int i=0;i<last.Length;i++)last[i]=-99;
            for(int n=0;n<order.Count && g.Towers.Count<(policy=="upgrade"?3:8);n++) {
                if(!g.TryBuild(order[n],Kind(n,index,policy),null,out why))break;
            }
            g.Start();double nextBuy=1;int peakActive=0,peakProjectiles=0;
            for(int frame=0;frame<8000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard)g.ChooseCard(g.CardOffers[0]);
                if(g.Elapsed>=nextBuy) {
                    bool acted=false;
                    if(policy=="mixed" && index>=2 && g.Telemetry.Fusions==0) {
                        Tower a=g.TowerAt(anchor),b=g.TowerAt(partner);
                        if(a!=null && a.Level==1)acted=g.TryUpgrade(a.Id,null,out why);
                        else if(b!=null && b.Kind==TowerKind.Binding && b.Level==1)acted=g.TryUpgrade(b.Id,null,out why);
                        else if(a!=null && b!=null && b.Kind==TowerKind.Binding && g.Cores>0)acted=g.TryFuse(0,a.Id,b.Id,anchor,null,out why);
                    }
                    if(!acted && policy=="upgrade")foreach(Tower t in g.Towers)if(t.Level==1 && g.CanUpgrade(t.Id)==""){acted=g.TryUpgrade(t.Id,null,out why);break;}
                    if(!acted && g.Towers.Count<desired)foreach(int slot in order)if(g.TowerAt(slot)==null){acted=g.TryBuild(slot,TowerKind.Autocannon,null,out why);break;}
                    if(!acted)foreach(Tower t in g.Towers)if(t.Level==1 && g.CanUpgrade(t.Id)==""){acted=g.TryUpgrade(t.Id,null,out why);break;}
                    nextBuy=g.Elapsed+1;
                }
                bool dodge=g.Enemies.Exists(e=>e.Kind==EnemyKind.Boss && e.BossWindup>0 && V2.Distance(g.PlayerPosition,e.BossTarget)<2.5f);
                g.Tick(.05f,dodge?new V2(1,0):new V2());g.UseSkill();
                foreach(CombatEvent e in g.Events)if(e.Kind==EventKind.TowerShot)for(int slot=0;slot<shots.Length;slot++)if(V2.DistanceSquared(g.BoardSockets[slot],e.Position)<.001f){shots[slot]++;last[slot]=g.Elapsed;break;}
                int active=0;foreach(Tower t in g.Towers)if(g.Elapsed-last[t.Socket]<=3)active++;
                peakActive=Math.Max(peakActive,active);peakProjectiles=Math.Max(peakProjectiles,g.Projectiles.Count);g.Events.Clear();
            }
            int fired=0,frequent=0;foreach(int count in shots){if(count>0)fired++;if(count>=10)frequent++;}
            return new Result {Stage=index+1,Seed=seed,Policy=policy,Sites=g.BoardSockets.Length,StartingCredits=initial,Towers=g.Towers.Count,Spent=g.Telemetry.CreditsSpent,Kills=g.Kills,Fusions=g.Telemetry.Fusions,FiredSites=fired,FrequentFiringSites=frequent,PeakActive3Seconds=peakActive,PeakProjectiles=peakProjectiles,State=g.State.ToString(),Core=g.MinimumCoreHealth,Time=(float)g.Elapsed};
        }
        public static Result[] Models()
        {
            var rows=new List<Result>();for(int stage=0;stage<14;stage++)foreach(int seed in new[]{7,19,41})foreach(string policy in new[]{"many","mixed","upgrade"})rows.Add(Run(stage,seed,policy));return rows.ToArray();
        }
        public static Result[] Models(int stage)
        {
            if(stage<0 || stage>=CampaignCatalog.Stages.Length)throw new ArgumentOutOfRangeException("stage");
            var rows=new List<Result>();foreach(int seed in new[]{7,19,41})foreach(string policy in new[]{"many","mixed","upgrade"})rows.Add(Run(stage,seed,policy));return rows.ToArray();
        }
    }
}
