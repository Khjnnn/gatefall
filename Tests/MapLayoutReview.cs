using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class MapLayoutReview
    {
        public sealed class Result { public int Stage, Seed, Towers, Kills, Spent, Fusions; public string Policy, State; public float Core, Time; }
        static void Assert(bool b,string text) { if(!b)throw new Exception(text); }
        public static string Geometry()
        {
            var ids=new HashSet<string>();var shapes=new HashSet<string>();int sites=0;
            foreach(var stage in CampaignCatalog.Stages) {
                var g=new DefenseSimulation(new GameBalance {OfferCards=false},7,new CampaignBattle {Stage=stage});
                Assert(ids.Add(BattlefieldLayouts.Get(stage).Id),"repeated layout id");
                var signature=new List<string>();foreach(int lane in g.OpenLanes)foreach(var p in g.Roads[lane])signature.Add(lane+":"+p.X+","+p.Y);
                Assert(shapes.Add(String.Join("/",signature)),"repeated route geometry");
                Assert(g.BoardSockets.Length==stage.SocketCount,"site count");
                foreach(int lane in g.OpenLanes) {
                    var road=g.Roads[lane];Assert(road.Length>=4 && V2.Distance(road[0],g.GatePositions[lane])<.001f && road[road.Length-1].Length<.001f,"gate/road mismatch");
                    bool reachable=false;foreach(var p in g.BoardSockets)if(BattlefieldLayouts.RoadDistance(p,road)<5.2f)reachable=true;
                    Assert(reachable,"lane without a firing site");
                    foreach(var p in road)Assert(Math.Abs(p.X)<=g.HalfWidth && Math.Abs(p.Y)<=g.HalfHeight,"road outside battlefield");
                }
                for(int i=0;i<g.BoardSockets.Length;i++) {
                    V2 p=g.BoardSockets[i];float distance=float.MaxValue;
                    foreach(int lane in g.OpenLanes)distance=Math.Min(distance,BattlefieldLayouts.RoadDistance(p,g.Roads[lane]));
                    Assert(distance>=2.1f && distance<=4.05f,"pad intersects street or cannot reach enemies");
                    Assert(p.Length>=4.05f,"pad overlaps barrier");
                    for(int j=0;j<i;j++)Assert(V2.Distance(p,g.BoardSockets[j])>=2.55f,"overlapping touch sites");
                    sites++;
                }
                if(stage.Boss) {
                    g.Schedule.RemoveAll(group=>group.Kind!=EnemyKind.Boss);g.Start();g.Tick(150.1f,new V2());
                    var boss=g.Enemies.Find(e=>e.Kind==EnemyKind.Boss);
                    Assert(boss!=null && BattlefieldLayouts.RoadDistance(boss.Position,g.Roads[1])<.02f,"boss starts off road");
                }
            }
            return "PASS 14 unique layouts / "+sites+" clear reachable separated sites / used roads connect gates to barrier / boss on actual road. Touch rectangle size is unchanged; Android interaction is a separate check.";
        }
        static List<int> Order(DefenseSimulation g)
        {
            var order=new List<int>();
            foreach(int lane in g.OpenLanes) {
                int node;var road=g.Roads[lane];V2 target=BattlefieldLayouts.Along(road,Math.Max(0,BattlefieldLayouts.RoadLength(road)-7),out node);
                int best=-1;float distance=float.MaxValue;
                for(int i=0;i<g.BoardSockets.Length;i++)if(!order.Contains(i) && V2.Distance(target,g.BoardSockets[i])<distance){best=i;distance=V2.Distance(target,g.BoardSockets[i]);}
                if(best>=0)order.Add(best);
            }
            for(int i=0;i<g.BoardSockets.Length;i++)if(!order.Contains(i))order.Add(i);
            return order;
        }
        static Result Run(int index,int seed,string policy)
        {
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle {Stage=CampaignCatalog.Stages[index]});
            var order=Order(g);string why;bool covered=policy=="fusion-cover";bool fusion=(policy=="fusion-rush" || covered) && index>=2;int anchor=order[0],partner=order[1];
            if(fusion)foreach(int slot in order)if(slot!=anchor && V2.Distance(g.BoardSockets[slot],g.BoardSockets[anchor])<V2.Distance(g.BoardSockets[partner],g.BoardSockets[anchor]))partner=slot;
            if(policy!="idle") {
                g.TryBuild(anchor,TowerKind.Autocannon,null,out why);
                if(fusion && !covered) {g.TryUpgrade(g.TowerAt(anchor).Id,null,out why);g.TryBuild(partner,TowerKind.Binding,null,out why);}
                else if(fusion) {g.TryBuild(order[1],TowerKind.Autocannon,null,out why);g.TryBuild(order[2],TowerKind.Autocannon,null,out why);partner=order[3];float closest=float.MaxValue;foreach(int slot in order)if(g.TowerAt(slot)==null && V2.Distance(g.BoardSockets[anchor],g.BoardSockets[slot])<closest){closest=V2.Distance(g.BoardSockets[anchor],g.BoardSockets[slot]);partner=slot;}}
                else {g.TryBuild(partner,TowerKind.Autocannon,null,out why);if(policy=="upgrade")g.TryUpgrade(g.TowerAt(anchor).Id,null,out why);else if(order.Count>2)g.TryBuild(order[2],TowerKind.Autocannon,null,out why);}
            }
            if(policy=="opening-fixed")foreach(var t in g.Towers)g.TryUpgrade(t.Id,null,out why);
            g.Start();double next=3;
            for(int frame=0;frame<8000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard)g.ChooseCard(g.CardOffers[0]);
                if(policy!="idle" && policy!="opening-fixed" && g.Elapsed>=next) {
                    bool acted=false;
                    if(fusion && g.Telemetry.Fusions==0) {
                        var b=g.TowerAt(partner);
                        if(g.TowerAt(anchor).Level==1)acted=g.TryUpgrade(g.TowerAt(anchor).Id,null,out why);
                        else if(b==null)acted=g.TryBuild(partner,TowerKind.Binding,null,out why);
                        else if(b.Level==1)acted=g.TryUpgrade(b.Id,null,out why);
                        else if(b!=null && g.Cores>0)acted=g.TryFuse(0,g.TowerAt(anchor).Id,b.Id,anchor,null,out why);
                    } else {
                        if(covered && fusion && g.Telemetry.Fusions>0 && g.TowerAt(partner)==null)acted=g.TryBuild(partner,TowerKind.Autocannon,null,out why);
                        if(!acted && policy=="spread" && g.Towers.Count<6)foreach(int slot in order)if(g.TowerAt(slot)==null){acted=g.TryBuild(slot,TowerKind.Autocannon,null,out why);break;}
                        if(!acted)foreach(var t in g.Towers)if(t.Level==1 && g.CanUpgrade(t.Id)==""){acted=g.TryUpgrade(t.Id,null,out why);break;}
                        if(!acted)foreach(int slot in order)if(g.TowerAt(slot)==null){g.TryBuild(slot,TowerKind.Autocannon,null,out why);break;}
                    }
                    next=g.Elapsed+3;
                }
                if(g.Enemies.Exists(e=>e.Kind==EnemyKind.Boss && e.BossWindup>0 && V2.Distance(g.PlayerPosition,e.BossTarget)<2.5f))g.Tick(.05f,new V2(1,0));
                else g.Tick(.05f,new V2());
                g.UseSkill();g.Events.Clear();
            }
            return new Result {Stage=index+1,Seed=seed,Policy=policy,State=g.State.ToString(),Core=g.MinimumCoreHealth,Time=(float)g.Elapsed,Towers=g.Towers.Count,Kills=g.Kills,Spent=g.Telemetry.CreditsSpent,Fusions=g.Telemetry.Fusions};
        }
        public static Result[] Models()
        {var r=new List<Result>();for(int i=0;i<14;i++)foreach(int seed in new[]{7,19,41})foreach(string policy in new[]{"idle","opening-fixed","spread","upgrade","fusion-rush","fusion-cover"})r.Add(Run(i,seed,policy));return r.ToArray();}
    }
}
