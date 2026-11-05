using System;
using System.Collections.Generic;
using System.Reflection;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class LateScopeReview {
        public sealed class Stats {public int Wave,Kind;public float Health,Speed;}
        public sealed class Contract {public int Stage,Start,Supply,Total;public bool Crisis;public V2[][] Roads;public V2[] Sites;public SpawnGroup[] Schedule;public Stats[] SpawnStats;}
        public static Contract[] Inspect(){
            var rows=new List<Contract>();var spawn=typeof(DefenseSimulation).GetMethod("Spawn",BindingFlags.Instance|BindingFlags.NonPublic);
            foreach(var s in CampaignCatalog.Stages)foreach(bool crisis in new[]{false,true}){
                var g=new DefenseSimulation(new GameBalance(),7,new CampaignBattle {Stage=s,Crisis=crisis,Loadout=263,MarkerUnlocked=true});
                int total=0;foreach(var x in g.Schedule)total+=x.Count;
                var stats=new List<Stats>();for(int wave=0;wave<6;wave++)for(int kind=0;kind<5;kind++){
                    spawn.Invoke(g,new object[]{new SpawnGroup{Wave=wave,Lane=g.OpenLanes[0],Kind=(EnemyKind)kind,Count=1}});
                    var e=g.Enemies[g.Enemies.Count-1];stats.Add(new Stats {Wave=wave,Kind=kind,Health=e.MaxHealth,Speed=e.Speed});
                }
                rows.Add(new Contract{Stage=s.Index+1,Crisis=crisis,Start=g.Balance.StartingCredits,Supply=g.WaveSupplyCredits,Total=total,Roads=g.Roads,Sites=g.BoardSockets,Schedule=g.Schedule.ToArray(),SpawnStats=stats.ToArray()});
            }return rows.ToArray();
        }
    }
}
