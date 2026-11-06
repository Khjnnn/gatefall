using System;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class SwarmChecks
    {
        static void Require(bool ok,string reason) {if(!ok)throw new Exception(reason);}
        static DefenseSimulation Empty(int stage=0)
        {
            var g=new DefenseSimulation(new GameBalance {SpawnEnemies=false,OfferCards=false,StartingCredits=1000,PlayerDamage=0,PlayerRange=0},7,new CampaignBattle {Stage=CampaignCatalog.Stages[stage]});
            g.Schedule.Clear();return g;
        }
        public static string Verify()
        {
            int waves=0;
            foreach(var stage in CampaignCatalog.LegacyStages) {
                var g=new DefenseSimulation(new GameBalance(),7,new CampaignBattle {Stage=stage});
                for(int wave=0;wave<6;wave++) {
                    int total=0,warnings=0;foreach(var group in g.Schedule)if(group.Wave==wave && group.Kind!=EnemyKind.Boss) {
                        total+=group.Count;if(!group.Warned)warnings++;
                        if(group.Swarm)Require(group.Kind==EnemyKind.Grunt && group.Count==stage.SwarmMultiplier-1 && group.Interval>0,"follower group");
                    }
                    Require(total==stage.Counts[wave] && warnings==g.OpenLanes.Length,"wave total or warning cardinality");waves++;
                }
            }
            var reservation=Empty();string why;int a=0,b=-1;
            for(int i=1;i<reservation.BoardSockets.Length;i++)if(V2.Distance(reservation.BoardSockets[a],reservation.BoardSockets[i])<9){b=i;break;}
            Require(b>=0,"nearby test pads");reservation.TryBuild(a,TowerKind.Autocannon,null,out why);reservation.TryBuild(b,TowerKind.Autocannon,null,out why);
            V2 midpoint=(reservation.BoardSockets[a]+reservation.BoardSockets[b])*.5f;
            reservation.Enemies.Add(new Enemy {Id=9001,Kind=EnemyKind.Grunt,Health=5,MaxHealth=5,Position=midpoint});
            reservation.Enemies.Add(new Enemy {Id=9002,Kind=EnemyKind.Grunt,Health=5,MaxHealth=5,Position=midpoint});
            reservation.Start();reservation.Tick(.01f,new V2());
            Require(reservation.Projectiles.Count==2 && reservation.Projectiles[0].TargetId!=reservation.Projectiles[1].TargetId,"duplicate lethal single-target shots");
            foreach(var shot in reservation.Projectiles)shot.Life=0;reservation.Tick(.01f,new V2());
            foreach(var enemy in reservation.Enemies)Require(enemy.IncomingDamage==0,"expired projectile leaked reservation");

            var loot=new DefenseSimulation(new GameBalance {SpawnEnemies=false,OfferCards=false,PlayerFireInterval=.001f},7,new CampaignBattle {Stage=CampaignCatalog.Stages[0]});loot.Schedule.Clear();int initial=loot.Credits;
            for(int i=0;i<7;i++)loot.Enemies.Add(new Enemy {Id=9100+i,Kind=EnemyKind.Grunt,Swarm=i<4,Health=1,MaxHealth=1,Position=loot.PlayerPosition+new V2(1,0)});
            loot.Start();loot.Tick(2,new V2());Require(loot.State==RunState.Won && loot.Kills==7 && loot.Credits==initial+4,"quarter-credit swarm loot or legacy kill loot");

            var breach=Empty();for(int i=0;i<100;i++)breach.Tick(.1f,new V2(1,-1));
            breach.Enemies.Add(new Enemy {Id=9201,Kind=EnemyKind.Grunt,Swarm=true,Health=1000,MaxHealth=1000,Position=new V2(1.1f,0),Waypoint=breach.Roads[0].Length-1});
            breach.Start();breach.Tick(.02f,new V2());Require(breach.CoreHealth==299,"tiny swarm breach damage");
            breach.Enemies.Add(new Enemy {Id=9202,Kind=EnemyKind.Grunt,Health=1000,MaxHealth=1000,Position=new V2(1.1f,0),Waypoint=breach.Roads[0].Length-1});
            breach.Tick(.02f,new V2());Require(breach.CoreHealth==292,"original lead enemy damage changed");
            return "PASS: "+waves+" grouped swarm waves; exact quarter-credit loot, distinct lethal-shot reservations, expiry release, tiny breach1 vs original7. "+Paths()+" Android mesh appearance is checked separately.";
        }
        static string Paths()
        {
            float maximum=0;int samples=0;
            foreach(var stage in CampaignCatalog.LegacyStages) {
                var g=new DefenseSimulation(new GameBalance {OfferCards=false,CoreHealth=1000000,PlayerDamage=0,PlayerRange=0},7,new CampaignBattle {Stage=stage});
                for(int i=0;i<100;i++)g.Tick(.1f,new V2(1,-1));g.Start();
                for(int frame=0;frame<80;frame++) {
                    g.Tick(1,new V2());g.Events.Clear();
                    foreach(var enemy in g.Enemies)if(enemy.Swarm && enemy.BlockOwner==0) {
                        float distance=BattlefieldLayouts.RoadDistance(enemy.Position,g.Roads[enemy.Lane]);maximum=Math.Max(maximum,distance);samples++;
                        Require(distance<.8f && Math.Abs(enemy.Position.X)<=g.HalfWidth && Math.Abs(enemy.Position.Y)<=g.HalfHeight,"swarm left visible road or map");
                    }
                }
            }
            return "14-road swarm flow "+samples+" samples / max centerline distance="+maximum.ToString("F3")+" (<0.8); no towers/attacks, 80s sampling, not a difficulty test.";
        }
    }
}
