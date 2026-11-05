using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class BasicReinforcementReview {
        public sealed class Row {
            public int Seed, Spent, Credits, Towers, Spawned, Remaining, CompletedOrders, TotalOrders, ChoiceCost, Budget;
            public bool Control, AllSpawned, Mixed, Front, Late;
            public int Hunter, Motion, Growth, AccountSpent, HunterShots, HunterDowns, SkillUses;
            public string State, PrefixHash;
            public double End, ChoiceAt, MaxPurchaseDelay, StrongSlowSamples, RunnerSlowSamples;
            public float Core;public int[] Cards, Shots;public float[] Damage, LaneDamage;public string[] Ledger;
        }
        static string Digest(DefenseSimulation g) {
            var text=new StringBuilder();
            typeof(EarlyCombatDigest).GetMethod("Value",BindingFlags.Static|BindingFlags.NonPublic).Invoke(null,new object[]{text,new object[]{g.RunContext(),g.State,g.Elapsed,g.Credits,g.Cores,g.Kills,g.CoreHealth,g.MinimumCoreHealth,g.PlayerHealth,g.Enemies,g.Towers,g.Projectiles,g.Schedule,g.Telemetry}});
            using(var hash=SHA256.Create())return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(text.ToString())).Select(x=>x.ToString("x2")));
        }
        public static Row Play(int seed,bool control,bool mixed=false,bool front=false,bool late=false,int hunter=0,int motion=0) {
            if(front && !mixed)throw new ArgumentException("Front placement is a separate mixed-policy pilot");
            if(late && (mixed || front))throw new ArgumentException("Order review uses the original170 package only");
            if(hunter<0 || hunter>2 || motion<0 || motion>2)throw new ArgumentException("Unknown hunter/motion policy");
            foreach(var stage in CampaignCatalog.Stages)BattlefieldLayouts.Get(stage);
            var profile=AccountGrowthChecks.Recommended(11,hunter,1);
            var game=new DefenseSimulation(new GameBalance(),seed,new CampaignService(profile,null).Battle(11,true));
            var row=new Row{Seed=seed,Control=control,Mixed=mixed,Front=front,Late=late,Hunter=hunter,Motion=motion,Growth=profile.Growth[hunter],AccountSpent=CampaignCatalog.Stages.Take(11).Sum(s=>s.FirstReward)-profile.MagicCores,Budget=mixed?650:710,Shots=new int[TowerCatalog.All.Length]};
            int choicePrice=mixed?110:170;
            if(mixed && (TowerCatalog.Get(TowerKind.Binding).BuildCost+TowerCatalog.Get(TowerKind.Rupture).BuildCost!=110 ||
                2*TowerCatalog.Get(TowerKind.Autocannon).BuildCost+TowerCatalog.Get(TowerKind.Autocannon).UpgradeCost!=110))throw new Exception("Mixed choice cost changed");
            var times=new List<float>();var actions=new List<Func<bool>>();string why;
            Action<float,int,TowerKind> build=(time,site,kind)=>{times.Add(time);actions.Add(()=>game.TryBuild(site,kind,null,out why));};
            Action<float,int> upgrade=(time,site)=>{times.Add(time);actions.Add(()=>game.TowerAt(site)!=null && game.TryUpgrade(game.TowerAt(site).Id,null,out why));};
            foreach(int site in new[]{15,2,16,13,front?8:6,9})build(0,site,TowerKind.Autocannon);
            foreach(int site in new[]{15,2,16})upgrade(30,site);
            if(late){
                foreach(int site in new[]{4,10}){build(60,site,TowerKind.Autocannon);upgrade(60,site);}
                build(90,3,TowerKind.Autocannon);upgrade(90,3);
            }
            int choiceStart=actions.Count;
            float choiceTime=late?120:60;
            if(mixed){
                build(60,front?7:8,control?TowerKind.Binding:TowerKind.Autocannon);
                if(!control)upgrade(60,front?7:8);
                build(60,front?6:7,control?TowerKind.Rupture:TowerKind.Autocannon);
            }else{
                foreach(int site in new[]{8,7}){build(choiceTime,site,control?TowerKind.Binding:TowerKind.Autocannon);upgrade(choiceTime,site);}
                if(!control)upgrade(choiceTime,13);
            }
            int choiceEnd=actions.Count;
            if(!late){
                build(90,4,TowerKind.Autocannon);upgrade(90,4);
                foreach(int site in new[]{10,3}){build(120,site,TowerKind.Autocannon);upgrade(120,site);}
            }
            int index=0,choiceSpent=0;bool captured=false,priced=false;
            Action act=()=>{
                if(game.CardOffers.Count>0 && !game.ChooseCard(game.CardOffers[0]))throw new Exception("First offered card selection failed");
                while(index<actions.Count && game.Elapsed+.0001>=times[index]){
                    if(index==choiceStart && !captured){
                        if(game.Credits<choicePrice)break;
                        row.PrefixHash=Digest(game);row.ChoiceAt=game.Elapsed;choiceSpent=game.Telemetry.CreditsSpent;captured=true;
                    }
                    if(!actions[index]())break;
                    row.MaxPurchaseDelay=Math.Max(row.MaxPurchaseDelay,game.Elapsed-times[index]);index++;
                    if(index==choiceEnd && !priced){row.ChoiceCost=game.Telemetry.CreditsSpent-choiceSpent;priced=true;if(row.ChoiceCost!=choicePrice)throw new Exception("Reinforcement pair cost differs");}
                }
            };
            act();game.Start();
            while(game.State!=RunState.Won && game.State!=RunState.Lost && game.Elapsed<400){
                act();if(game.State!=RunState.Running)throw new Exception("Fixture cannot advance while paused");
                V2 movement=new V2();
                if(motion>0){
                    Enemy urgent=null;float closest=float.MaxValue;
                    foreach(var e in game.Enemies)if(e.Health>0 && e.Position.Length<closest){closest=e.Position.Length;urgent=e;}
                    V2 target=urgent==null?new V2(0,-2.2f):urgent.Position+new V2(0,-.8f);
                    if(motion==2 && urgent!=null && V2.Distance(game.PlayerPosition,urgent.Position)<=3 && game.UseSkill())row.SkillUses++;
                    V2 delta=target-game.PlayerPosition;if(delta.Length>.2f)movement=delta.Normalized;
                }
                double before=game.Elapsed;game.Tick(1f/30,movement);double dt=game.Elapsed-before;
                foreach(var e in game.Events){
                    if(e.Kind==EventKind.TowerShot && e.Source>=0 && e.Source<row.Shots.Length)row.Shots[e.Source]++;
                    if(e.Kind==EventKind.HunterShot)row.HunterShots++;
                    if(e.Kind==EventKind.HunterDown)row.HunterDowns++;
                }
                foreach(var e in game.Enemies)if(e.Health>0){if(!e.Swarm && (e.Kind==EnemyKind.Brute || e.Kind==EnemyKind.Siege))row.StrongSlowSamples+=e.SlowFraction*dt;if(e.Kind==EnemyKind.Runner)row.RunnerSlowSamples+=e.SlowFraction*dt;}
                if(game.Credits<0 || game.Telemetry.CreditsSpent>row.Budget)throw new Exception("Fixture exceeded actual spending ceiling");
                game.Events.Clear();
            }
            row.Spent=game.Telemetry.CreditsSpent;row.Credits=game.Credits;row.Towers=game.Towers.Count;row.Spawned=game.Telemetry.Spawned;row.Remaining=game.Enemies.Count;row.CompletedOrders=index;row.TotalOrders=actions.Count;
            row.AllSpawned=game.AllWavesSpawned;row.State=game.State.ToString();row.End=game.Elapsed;row.Core=game.MinimumCoreHealth;row.Cards=(int[])game.Cards.Clone();row.Damage=(float[])game.Telemetry.TowerDamage.Clone();row.LaneDamage=(float[])game.Telemetry.CoreDamageByLane.Clone();row.Ledger=game.Telemetry.Purchases.ToArray();
            if(!captured || !priced || row.State=="Running")throw new Exception("Incomplete reinforcement fixture");
            return row;
        }
    }
}
