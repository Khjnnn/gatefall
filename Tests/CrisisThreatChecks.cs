using System;
using System.Collections.Generic;
using System.Reflection;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class CrisisThreatChecks {
        public sealed class SpawnRow {
            public int Stage,Wave,Lane,Count,Waypoint;public bool Crisis,Swarm;public string Kind;
            public double Start,Interval;public float Health,Speed,Armor,X,Y;
        }
        static readonly MethodInfo Spawn=typeof(DefenseSimulation).GetMethod("Spawn",BindingFlags.Instance|BindingFlags.NonPublic);
        static Enemy Create(int stage,bool crisis,int wave,EnemyKind kind){
            var g=new DefenseSimulation(new GameBalance(),7,new CampaignBattle{Stage=CampaignCatalog.Stages[stage],Crisis=crisis});
            Spawn.Invoke(g,new object[]{new SpawnGroup{Kind=kind,Wave=wave,Lane=g.OpenLanes[0],Count=1}});
            return g.Enemies[0];
        }
        public static string Run(){
            int pairs=0;
            foreach(int stage in new[]{10,11})foreach(int wave in new[]{2,3,5})foreach(var kind in new[]{EnemyKind.Brute,EnemyKind.Siege}){
                var normal=Create(stage,false,wave,kind);var crisis=Create(stage,true,wave,kind);
                if(Math.Abs(crisis.MaxHealth-normal.MaxHealth*1.25f)>.002f || Math.Abs(crisis.Speed-normal.Speed)>.00001f)
                    throw new Exception("Crisis must inherit the late threat before its common health bonus:1-"+(stage+1)+" wave"+(wave+1)+" "+kind);
                if(crisis.Armor!=6 || normal.Armor!=6)throw new Exception("Armor changed in crisis threat adjustment");
                pairs++;
            }
            return "PASS "+pairs+" normal/crisis heavy pairs:1-11/12 early and late health inheritance, equal speed and armor6";
        }
        // One representative per actual spawn group: members share kind/wave/health/speed. This is not a battle.
        public static SpawnRow[] Snapshot(){
            var rows=new List<SpawnRow>();
            for(int stage=0;stage<14;stage++)foreach(bool crisis in new[]{false,true}){
                var g=new DefenseSimulation(new GameBalance(),7,new CampaignBattle{Stage=CampaignCatalog.Stages[stage],Crisis=crisis});
                foreach(var group in g.Schedule){
                    Spawn.Invoke(g,new object[]{group});var e=g.Enemies[g.Enemies.Count-1];
                    rows.Add(new SpawnRow{Stage=stage+1,Crisis=crisis,Wave=group.Wave,Lane=group.Lane,Kind=group.Kind.ToString(),Swarm=group.Swarm,
                        Count=group.Count,Start=group.Start,Interval=group.Interval,Health=e.MaxHealth,Speed=e.Speed,Armor=e.Armor,
                        X=e.Position.X,Y=e.Position.Y,Waypoint=e.Waypoint});g.Enemies.Clear();
                }
            }return rows.ToArray();
        }
    }
}
