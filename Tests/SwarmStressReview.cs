using System;
using System.Collections.Generic;
using System.Diagnostics;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class SwarmStressReview
    {
        public sealed class Result {public int Enemies,Towers,Steps,Projectiles;public double Milliseconds,AllocatedBytes,HealthSum;public float Core,Damage;}
        public static Result[] Run()
        {
            var rows=new List<Result>();
            foreach(int count in new[]{128,384,768}) {
                var g=new DefenseSimulation(new GameBalance {SpawnEnemies=false,OfferCards=false,StartingCredits=10000,PlayerDamage=0,PlayerRange=0,CoreHealth=30000},7,new CampaignBattle {Stage=CampaignCatalog.Stages[3]});
                g.Schedule.Clear();for(int i=0;i<100;i++)g.Tick(.1f,new V2(1,-1));string why;
                for(int i=0;i<g.BoardSockets.Length;i++){g.TryBuild(i,i%4==0 ? TowerKind.Rupture : TowerKind.Autocannon,null,out why);g.TryUpgrade(g.TowerAt(i).Id,null,out why);}
                for(int i=0;i<count;i++) {
                    int lane=i%3,node;var road=g.Roads[lane];V2 p=BattlefieldLayouts.Along(road,BattlefieldLayouts.RoadLength(road)*(.08f+.82f*(i/(float)count)),out node);
                    g.Enemies.Add(new Enemy {Id=10000+i,Lane=lane,Waypoint=node,Kind=i%16==0?EnemyKind.Brute:EnemyKind.Grunt,Position=p,Health=50000,MaxHealth=50000});
                }
                g.Start();for(int i=0;i<12;i++){g.Tick(1f/60,new V2());g.Events.Clear();}
                long before=GC.GetAllocatedBytesForCurrentThread();var clock=Stopwatch.StartNew();
                for(int i=0;i<90;i++){g.Tick(1f/60,new V2());g.Events.Clear();}
                clock.Stop();double health=0;foreach(var e in g.Enemies)health+=e.Health;float damage=0;foreach(float value in g.Telemetry.TowerDamage)damage+=value;
                rows.Add(new Result {Enemies=count,Towers=g.Towers.Count,Steps=90,Milliseconds=clock.Elapsed.TotalMilliseconds,AllocatedBytes=GC.GetAllocatedBytesForCurrentThread()-before,Core=g.CoreHealth,Damage=damage,HealthSum=health,Projectiles=g.Projectiles.Count});
            }return rows.ToArray();
        }
    }
}
