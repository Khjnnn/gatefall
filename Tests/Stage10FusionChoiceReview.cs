using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Gatefall.Core;
namespace Gatefall.Tests {
    // Fixed shared purchases, then a45-credit choice; anchors are separated from later common reinforcement.
    public static class Stage10FusionChoiceReview {
        public sealed class Outcome {
            public int stage,seed,budget,spent,credits,growth,towers,fusions,spawned,remaining,stars;
            public string profile,policy,state;public float core;public double end;
            public bool allSpawned;public int[] research,cards;public float[] laneDamage;public string[] ledger;
        }
        public sealed class Row {
            public Outcome outcome;public string choice,prefixHash;public bool supplement;
            public int prefixSpent,prefixTowers,actions,expectedActions,peakAlive,unmatchedShots,currentFired,supplementSite;
            public double maxPurchaseDelay;public int[] prefixCards,shots=new int[11];public float[] damage,prefixDamage;
        }
        static string Prefix(DefenseSimulation g) {
            var b=new StringBuilder();typeof(EarlyCombatDigest).GetMethod("Value",BindingFlags.NonPublic|BindingFlags.Static).Invoke(null,new object[]{b,
                new object[]{g.RunContext(),g.State,g.Elapsed,g.Credits,g.Cores,g.Kills,g.CoreHealth,g.MinimumCoreHealth,g.PlayerHealth,g.Enemies,g.Towers,g.Projectiles,g.Schedule,g.Telemetry,g.Cards,g.Enemies.Select(e=>e.Armor).ToArray()}});
            using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString())).Select(x=>x.ToString("x2")));
        }
        public static Row Play(int seed,bool below,string choice,bool supplement) {
            return PlayAt(seed,below,choice,supplement,8);
        }
        public static Row PlayAt(int seed,bool below,string choice,bool supplement,int supplementSite) {
            if(seed!=7 && seed!=19 && seed!=41)throw new ArgumentException("Selected seed only");
            if(choice!="original" && choice!="binding3" && choice!="shatter13" && choice!="shatter15")throw new ArgumentException("Unknown policy");
            if(choice=="original" && supplement)throw new ArgumentException("Original control has no reinforcement");
            if(supplementSite!=8 && !(supplement && (choice=="shatter13" && supplementSite==15 || choice=="shatter15" && supplementSite==13)))throw new ArgumentException("Refill must be the consumed empty material site");
            var account=AccountGrowthChecks.Recommended(9,0,below?1:0);var rules=new CampaignService(account,null).Battle(9,false);
            var g=new DefenseSimulation(new GameBalance(),seed,rules);var row=new Row{choice=choice,supplement=supplement,supplementSite=supplement?supplementSite:-1};
            var times=new List<float>();var acts=new List<Func<bool>>();string why;
            Action<float,int,TowerKind> build=(time,site,kind)=>{times.Add(time);acts.Add(()=>g.TryBuild(site,kind,null,out why));};
            Action<float,int> upgrade=(time,site)=>{times.Add(time);acts.Add(()=>g.TowerAt(site)!=null && g.TryUpgrade(g.TowerAt(site).Id,null,out why));};
            build(0,13,TowerKind.Autocannon);build(0,15,TowerKind.Rupture);build(0,0,TowerKind.Autocannon);build(0,22,TowerKind.Binding);
            upgrade(0,13);upgrade(0,0);build(0,18,TowerKind.Autocannon);upgrade(0,18);
            build(30,17,TowerKind.Autocannon);upgrade(30,17);upgrade(30,22);
            build(60,4,TowerKind.Rupture);build(60,14,TowerKind.Autocannon);upgrade(60,14);
            if(choice!="original"){
                upgrade(90,15);
                if(choice=="binding3")build(100,3,TowerKind.Binding);
                else {int output=choice=="shatter13"?13:15;times.Add(100);acts.Add(()=>g.TryFuse(1,g.TowerAt(13).Id,g.TowerAt(15).Id,output,null,out why));}
                if(supplement)build(120,supplementSite,TowerKind.Rupture);
            }
            var fired=new HashSet<int>();int index=0;
            CardKind[] preference={CardKind.Ballistic,CardKind.Range,CardKind.Blast,CardKind.Control,CardKind.Credits,CardKind.Hunter};
            Action act=()=>{
                if(g.AwaitingCard){foreach(var card in preference)if(g.CardOffers.Contains(card)){if(!g.ChooseCard(card))throw new Exception("Card rejected");break;}return;}
                if(choice!="original" && index==15 && g.Elapsed>=100 && row.prefixHash==null){
                    if(g.Telemetry.CreditsSpent!=620 || g.Towers.Count!=8 || g.TowerAt(13).Kind!=TowerKind.Autocannon || g.TowerAt(13).Level!=2 || g.TowerAt(15).Kind!=TowerKind.Rupture || g.TowerAt(15).Level!=2 || g.Cores<1)throw new Exception("Shared legal material prefix changed");
                    row.prefixHash=Prefix(g);row.prefixSpent=g.Telemetry.CreditsSpent;row.prefixTowers=g.Towers.Count;row.prefixCards=(int[])g.Cards.Clone();row.prefixDamage=(float[])g.Telemetry.TowerDamage.Clone();
                }
                for(int guard=0;guard<6 && index<acts.Count && g.Elapsed>=times[index];guard++){
                    if(!acts[index]())break;row.maxPurchaseDelay=Math.Max(row.maxPurchaseDelay,g.Elapsed-times[index]);index++;
                }
            };
            act();g.Start();
            while(g.State!=RunState.Won && g.State!=RunState.Lost && g.Elapsed<320){
                act();g.Tick(1f/30,new V2());
                foreach(var e in g.Events)if(e.Kind==EventKind.TowerShot){
                    Tower tower=null;foreach(var t in g.Towers)if((int)t.Kind==e.Source && V2.DistanceSquared(t.Position,e.Position)<.000001f){tower=t;break;}
                    if(tower==null){row.unmatchedShots++;continue;}fired.Add(tower.Id);row.shots[e.Source]++;
                }
                row.peakAlive=Math.Max(row.peakAlive,g.Enemies.Count);g.Events.Clear();
                if(g.State==RunState.Paused && !g.AwaitingCard)throw new Exception("Unexpected paused model");
            }
            row.actions=index;row.expectedActions=acts.Count;row.currentFired=g.Towers.Count(t=>fired.Contains(t.Id));row.damage=(float[])g.Telemetry.TowerDamage.Clone();
            row.outcome=new Outcome{stage=10,seed=seed,budget=choice=="original"?600:supplement?730:665,spent=g.Telemetry.CreditsSpent,credits=g.Credits,growth=rules.EarnedGrowth,towers=g.Towers.Count,fusions=g.Telemetry.Fusions,spawned=g.Telemetry.Spawned,remaining=g.Enemies.Count,stars=CampaignCatalog.Stars(g.State==RunState.Won,g.MinimumCoreHealth,g.Balance.CoreHealth),profile=below?"below":"recommended",policy=choice=="original"?"mixed":choice,state=g.State.ToString(),core=g.MinimumCoreHealth,end=g.Elapsed,allSpawned=g.AllWavesSpawned,research=rules.Research,cards=g.Cards,laneDamage=g.Telemetry.CoreDamageByLane,ledger=g.Telemetry.Purchases.ToArray()};
            return row;
        }
    }
}
