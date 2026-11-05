using System;
using System.Collections.Generic;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class BindingArmorReview {
        public sealed class Sample { public string Tower,Enemy;public int Level,Research,Card;public float Armor,Raw,Actual,Expected,Remaining; }
        public static Sample[] Check(bool penetrating) {
            var rows=new List<Sample>();
            foreach(var tower in new[]{TowerKind.Binding,TowerKind.Arc,TowerKind.Gravity,TowerKind.Seal,TowerKind.Beacon,TowerKind.Autocannon})
            for(int level=1;level<=Math.Min(2,TowerCatalog.Get(tower).Levels.Length);level++)foreach(int research in new[]{1,3})foreach(int card in new[]{0,2})
            foreach(var kind in new[]{EnemyKind.Grunt,EnemyKind.Brute,EnemyKind.Siege})foreach(float armor in new[]{0f,6f}) {
                var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,new CampaignBattle{Stage=CampaignCatalog.Stages[11],Research=new[]{0,research,0,0},Loadout=135});
                g.Cards[(int)CardKind.Control]=card;var stats=g.Stats(tower,level);
                var enemy=new Enemy{Id=9000,Kind=kind,Armor=armor,Health=100000,MaxHealth=100000,Speed=0,Position=new V2(15,8)};g.Enemies.Add(enemy);
                g.Projectiles.Add(new Projectile{Id=9001,TargetId=enemy.Id,SourceId=123,Kind=tower,FromTower=true,Stats=stats,Damage=stats.Damage,Position=enemy.Position,ImpactPosition=enemy.Position,Speed=20});
                g.Start();g.Tick(1f/60,new V2());
                float resistance=kind==EnemyKind.Siege?.4f:kind==EnemyKind.Brute?.65f:1;
                float expected=stats.Slow*(penetrating && tower==TowerKind.Binding && armor>0 ? 1:resistance);
                if(Math.Abs(enemy.SlowFraction-expected)>.00001f)throw new Exception("Unexpected actual slow: "+tower+"/"+kind+"/armor"+armor);
                rows.Add(new Sample{Tower=tower.ToString(),Enemy=kind.ToString(),Level=level,Research=research,Card=card,Armor=armor,Raw=stats.Slow,Actual=enemy.SlowFraction,Expected=expected,Remaining=enemy.Slows.Count>0?enemy.Slows[0].Remaining:0});
            }
            return rows.ToArray();
        }
    }
}
