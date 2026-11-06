using System;
using System.Reflection;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class CombatRoleChecks {
        static void Assert(bool v,string why){if(!v)throw new Exception(why);}
        static void Near(float a,float b){Assert(Math.Abs(a-b)<.002f,a+" != "+b);}
        static DefenseSimulation New(int stage=7){return new DefenseSimulation(new GameBalance{StartingCredits=2000,SpawnEnemies=false,OfferCards=false},7,new CampaignBattle{Stage=CampaignCatalog.Stages[stage],Loadout=135,MarkerUnlocked=true,ObserverUnlocked=true});}
        static Enemy Enemy(DefenseSimulation g,float armor=6){var e=new Enemy{Id=9000,Kind=EnemyKind.Brute,Health=1000,MaxHealth=1000,Armor=armor,Position=new V2(15,8)};g.Enemies.Add(e);return e;}
        static void Hit(DefenseSimulation g,Enemy e,TowerKind kind,bool hunter=false){
            var s=hunter?null:g.Stats(kind,1);
            g.Projectiles.Add(new Projectile{Id=9001,SourceId=123,TargetId=e.Id,Kind=kind,FromTower=!hunter,Stats=s,Damage=hunter?12:s.Damage,Position=e.Position,ImpactPosition=e.Position,Speed=20});
            if(g.State==RunState.Ready)g.Start();g.Tick(1f/60,new V2());
        }
        public static string Run(){
            foreach(int level in new[]{2,3}) {
                var pressure=New(11);pressure.Balance.PlayerDamage=0;string bindingReason;
                Assert(pressure.TryBuild(0,TowerKind.Binding,null,out bindingReason),bindingReason);var binding=pressure.TowerAt(0);
                for(int n=1;n<level;n++)Assert(pressure.TryUpgrade(binding.Id,null,out bindingReason),bindingReason);
                for(int n=0;n<9;n++)pressure.Enemies.Add(new Enemy{Id=9100+n,Kind=EnemyKind.Grunt,Health=100,MaxHealth=100,Position=binding.Position+new V2(1+n*.1f,.2f),Speed=0});
                pressure.Start();pressure.Tick(.3f,new V2());var targets=new System.Collections.Generic.HashSet<string>();
                foreach(var crowdEvent in pressure.Events)if(crowdEvent.Kind==EventKind.TowerShot && crowdEvent.Source==(int)TowerKind.Binding)targets.Add(crowdEvent.Target.X+","+crowdEvent.Target.Y);
                Assert(targets.Count==(level==2?6:8),"Upgraded Binding did not launch distinct crowd targets");
            }
            foreach(var stage in CampaignCatalog.Stages){if(stage.Index<6){Near(stage.EliteArmor,0);Near(stage.HybridDamage,1);}else{Assert(stage.EliteArmor>0 && stage.HybridDamage>1,"Post-seal role data");}}
            var g=New();var e=Enemy(g);Hit(g,e,TowerKind.Autocannon);Near(1000-e.Health,8);Near(g.Telemetry.TowerDamage[0],8);
            g=New();e=Enemy(g);Hit(g,e,TowerKind.Autocannon,true);Near(1000-e.Health,6);
            g=New();e=Enemy(g,99);Hit(g,e,TowerKind.Autocannon);Near(1000-e.Health,2.8f);
            foreach(var kind in new[]{TowerKind.Rupture,TowerKind.Shatter,TowerKind.Seal,TowerKind.Observer}){
                g=New();e=Enemy(g);float expected=g.Stats(kind,1).Damage;Hit(g,e,kind);Near(1000-e.Health,expected);
            }
            g=New();e=Enemy(g);e.MarkRemaining=3;e.MarkBonus=5;Hit(g,e,TowerKind.Autocannon);Near(1000-e.Health,13);Near(g.Telemetry.MarkBonusDamage,5);
            g=New();string why;Assert(g.TryBuild(0,TowerKind.Autocannon,null,out why),"Reservation tower");var t=g.TowerAt(0);t.Commission=0;t.Cooldown=0;e=Enemy(g);e.Position=t.Position+new V2(-2,0);g.Start();g.Tick(1f/60,new V2());
            var shot=g.Projectiles.Find(p=>p.FromTower && p.SourceId==t.Id);Assert(shot!=null,"In-flight shot missing");Near(shot.ReservedDamage,8);Near(e.IncomingDamage,8);g.Tick(.15f,new V2());Near(e.IncomingDamage,0);Near(1000-e.Health,8);
            var spawn=typeof(DefenseSimulation).GetMethod("Spawn",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach(var kind in new[]{EnemyKind.Grunt,EnemyKind.Runner,EnemyKind.Brute,EnemyKind.Siege,EnemyKind.Boss}){
                g=New();spawn.Invoke(g,new object[]{new SpawnGroup{Kind=kind,Lane=0,Wave=3,Count=1}});Near(g.Enemies[0].Armor,kind==EnemyKind.Brute||kind==EnemyKind.Siege?g.Rules.Stage.EliteArmor:0);
            }
            g=New();spawn.Invoke(g,new object[]{new SpawnGroup{Kind=EnemyKind.Brute,Lane=0,Wave=3,Count=1,Swarm=true}});Near(g.Enemies[0].Armor,0);
            g=New(0);spawn.Invoke(g,new object[]{new SpawnGroup{Kind=EnemyKind.Brute,Lane=0,Wave=3,Count=1}});Near(g.Enemies[0].Armor,0);
            var before=New(0);g=New();Near(g.Stats(TowerKind.Autocannon,1).Damage,before.Stats(TowerKind.Autocannon,1).Damage);Near(g.Stats(TowerKind.Arc,1).Chain[0],before.Stats(TowerKind.Arc,1).Chain[0]*g.Rules.Stage.HybridDamage);Near(g.Stats(TowerKind.Seal,1).Damage,before.Stats(TowerKind.Seal,1).Damage);
            return "PASS combat roles: post-seal armor, hunter/spear reduction and floor, pierce, mark accounting, matching in-flight reservation, spawn kinds and tier2-only power";
        }
    }
}
