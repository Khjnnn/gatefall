using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class CombatPaceReview
    {
        public sealed class Result { public int Stage, Seed, Towers, Kills, Spent, Fusions, PeakEnemies, PeakProjectiles, PeakKillsPerSecond, Credits, Supplies, Cards, Catalysts; public float MeanKillsPerSecond; public string Policy, State; public float Core, Time; }
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
            g.Start();double next=3, second=1;int peakEnemies=0, peakShots=0, peakKills=0, lastKills=0;
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
                g.UseSkill();peakEnemies=Math.Max(peakEnemies,g.Enemies.Count);peakShots=Math.Max(peakShots,g.Projectiles.Count);if(g.Elapsed>=second){peakKills=Math.Max(peakKills,g.Kills-lastKills);lastKills=g.Kills;second=Math.Floor(g.Elapsed)+1;}g.Events.Clear();
            }
            return new Result {Stage=index+1,Seed=seed,Policy=policy,State=g.State.ToString(),Core=g.MinimumCoreHealth,Time=(float)g.Elapsed,Towers=g.Towers.Count,Kills=g.Kills,Spent=g.Telemetry.CreditsSpent,Fusions=g.Telemetry.Fusions,PeakEnemies=peakEnemies,PeakProjectiles=peakShots,PeakKillsPerSecond=peakKills,Credits=g.Credits,Supplies=g.SuppliesGranted,Cards=g.Telemetry.CardOffers,Catalysts=g.Telemetry.CoresGranted,MeanKillsPerSecond=(float)(g.Kills/Math.Max(1,g.Elapsed))};
        }
        public static Result[] Models()
        {return Models(14);}
        public static Result[] Models(int stages)
        {var r=new List<Result>();for(int i=0;i<Math.Min(14,stages);i++)foreach(int seed in new[]{7,19,41})foreach(string policy in new[]{"opening-fixed","spread","upgrade","fusion-cover"})r.Add(Run(i,seed,policy));return r.ToArray();}
        public sealed class DuelResult {public int Stage,Level,Shots,KillKind;public string Kind;public float Health,Speed,Seconds;}
        public static DuelResult[] Duels()
        {
            var rows=new List<DuelResult>();
            foreach(int stage in new[]{0,3,12})foreach(EnemyKind kind in new[]{EnemyKind.Grunt,EnemyKind.Runner,EnemyKind.Brute,EnemyKind.Siege})foreach(int level in new[]{1,2}) {
                var g=new DefenseSimulation(new GameBalance {OfferCards=false,PlayerDamage=0,StartingCredits=1000},7,new CampaignBattle {Stage=CampaignCatalog.Stages[stage]});
                g.Schedule.Clear();g.Schedule.Add(new SpawnGroup {Wave=0,Lane=g.OpenLanes[0],Kind=kind,Count=1,Start=0,Interval=1});
                string why;g.TryBuild(0,TowerKind.Autocannon,null,out why);if(level==2)g.TryUpgrade(g.TowerAt(0).Id,null,out why);g.Start();g.Tick(.01f,new V2());
                var enemy=g.Enemies[0];float health=enemy.Health,speed=enemy.Speed;enemy.Speed=0;enemy.Position=g.TowerAt(0).Position+new V2(2,0);enemy.Waypoint=1;
                g.Events.Clear();int shots=0,killKind=-99;double first=-1;
                for (int i = 0; i < 10000 && g.Kills == 0; i++) {
                    g.Tick(.01f, new V2());
                    foreach (var e in g.Events) {
                        if (e.Kind == EventKind.TowerShot) { shots++; if (first < 0) first = g.Elapsed; }
                        if (e.Kind == EventKind.Kill) {
                            if (e.EnemyId != enemy.Id) throw new Exception("Kill event did not preserve defeated enemy identity");
                            killKind = e.Source;
                        }
                    }
                    g.Events.Clear();
                }
                if(g.Kills!=1)throw new Exception("Duel did not finish: "+stage+"/"+kind+"/"+level+" "+g.State);
                rows.Add(new DuelResult {Stage=stage+1,Kind=kind.ToString(),Level=level,Health=health,Speed=speed,Shots=shots,Seconds=(float)(g.Elapsed-first),KillKind=killKind});
            }
            return rows.ToArray();
        }
        public sealed class Budget {public int Stage,Total,Grunts,Runners,Brutes,Siege,Boss;public double LastSpawn;}
        public static Budget[] Budgets()
        {
            var rows=new List<Budget>();foreach(var stage in CampaignCatalog.Stages) {
                var g=new DefenseSimulation(new GameBalance(),7,new CampaignBattle {Stage=stage});var row=new Budget {Stage=stage.Index+1};
                foreach(var group in g.Schedule){row.Total+=group.Count;row.LastSpawn=Math.Max(row.LastSpawn,group.Start+(group.Count-1)*group.Interval);switch(group.Kind){case EnemyKind.Grunt:row.Grunts+=group.Count;break;case EnemyKind.Runner:row.Runners+=group.Count;break;case EnemyKind.Brute:row.Brutes+=group.Count;break;case EnemyKind.Siege:row.Siege+=group.Count;break;case EnemyKind.Boss:row.Boss+=group.Count;break;}}
                rows.Add(row);
            }return rows.ToArray();
        }
    }
}

