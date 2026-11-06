using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Gatefall.Core;

namespace Gatefall.Tests
{
    // Limited policy comparison, not an optimal planner or a human difficulty rating.
    public static class ChapterTwoInvestmentReview
    {
        public sealed class Row
        {
            public string Key,Stage,Policy,Mode,Outcome,Error,PrefixHash,PrefixLedgerHash;
            public int Seed,Growth,PlannedSpend,Spent,Remaining,Towers,CompletedCommands,TotalCommands,Spawned,Kills,Total,Fusions,Supplies,CardsOffered,Catalysts,PeakAlive,PeakNearCore;
            public bool AllSpawned;
            public float MinimumCore,CoreAtChoice,MaximumPurchaseDelay,StrongSlowEnemySeconds;
            public double End,BarrageAt=-1,SealAt=-1;
            public int[] Cards,ChoiceCards,Shots;
            public float[] Damage,CoreDamageByLane;
            public string[] Ledger,PurchaseDelays,CoreHits;
        }
        sealed class Order
        {
            public double At;public int Slot,Other,Recipe,Cost;public TowerKind Kind;public string Op;
        }
        static Order B(double at,int slot,TowerKind kind) => new Order{At=at,Slot=slot,Kind=kind,Op="build",Cost=TowerCatalog.Get(kind).BuildCost};
        static Order U(double at,int slot,int price) => new Order{At=at,Slot=slot,Op="upgrade",Cost=price};
        static Order F(double at,int anchor,int other,int recipe) => new Order{At=at,Slot=anchor,Other=other,Recipe=recipe,Op="fusion",Cost=TowerCatalog.Recipes[recipe].Credits};
        static void Level2(List<Order> list,double at,int slot,TowerKind kind)
        {list.Add(B(at,slot,kind));list.Add(U(at,slot,TowerCatalog.Get(kind).UpgradeCost));}
        static List<Order> Plan(int stage,string policy)
        {
            bool five=stage==18;
            int rupture=five?2:1,spear=five?8:6,scatter=five?21:2,binding=five?3:4,spare=five?1:25;
            var p=new List<Order>{B(0,rupture,TowerKind.Rupture),B(0,spear,TowerKind.Autocannon),B(0,scatter,TowerKind.Scatter),B(0,binding,TowerKind.Binding),U(0,scatter,45),U(0,rupture,55)};
            Level2(p,30,spare,TowerKind.Rupture);p.Add(U(60,spear,30)); // common450 prefix
            if(policy=="scatter") {
                Level2(p,90,five?4:20,TowerKind.Autocannon);p.Add(B(90,five?7:9,TowerKind.Scatter)); //120
                Level2(p,150,five?17:10,TowerKind.Scatter);p.Add(U(150,five?spare:rupture,90));
                Level2(p,150,five?0:21,TowerKind.Autocannon); //255
            } else {
                int anchor=five?(policy=="barrage-west"?scatter:rupture):scatter;
                int partner=five?(policy=="barrage-west"?rupture:scatter):spare;
                p.Add(F(90,anchor,partner,9));Level2(p,90,partner,TowerKind.Autocannon); //120
                if(policy=="seal") {
                    p.Add(U(150,anchor,65));p.Add(U(150,binding,40));p.Add(F(150,binding,anchor,10));
                    Level2(p,150,anchor,TowerKind.Autocannon); //255, second catalyst and two vacancies accounted
                } else {
                    p.Add(U(150,anchor,65));Level2(p,150,five?7:9,TowerKind.Scatter);Level2(p,150,five?17:10,TowerKind.Scatter); //255
                }
            }
            Level2(p,180,five?6:0,TowerKind.Rupture);Level2(p,180,five?18:13,TowerKind.Autocannon);Level2(p,180,five?23:17,TowerKind.Autocannon); //260
            if(p.Sum(x=>x.Cost)!=1085)throw new Exception("Declared budget differs");
            return p;
        }
        static void Value(StringBuilder b,object v)
        {
            if(v==null){b.Append("null;");return;}
            var type=v.GetType();
            if(v is string || type.IsPrimitive || type.IsEnum){b.Append(Convert.ToString(v,CultureInfo.InvariantCulture)).Append(';');return;}
            if(v is IEnumerable){b.Append('[');foreach(var item in (IEnumerable)v)Value(b,item);b.Append(']');return;}
            foreach(var f in type.GetFields(BindingFlags.Instance|BindingFlags.Public).OrderBy(x=>x.Name,StringComparer.Ordinal)){b.Append(f.Name).Append('=');Value(b,f.GetValue(v));}
        }
        static string Hash(object value)
        {
            var b=new StringBuilder();Value(b,value);
            using(var sha=SHA256.Create())return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(b.ToString())).Select(x=>x.ToString("x2")));
        }
        static string State(DefenseSimulation g) => Hash(new object[]{g.State,g.Elapsed,g.Credits,g.Cores,g.Kills,g.PlayerPosition,g.PlayerAim,g.PlayerHealth,g.CoreHealth,g.MinimumCoreHealth,
            g.HunterRespawnRemaining,g.SkillCooldown,g.CoreShield,g.HunterShield,g.SupportCooldown,g.Cards,g.CardOffers,g.Enemies,g.Towers,g.Projectiles,g.Schedule,g.Events,g.Telemetry});
        static bool Buy(DefenseSimulation g,Order p,out string reason)
        {
            var tower=g.TowerAt(p.Slot);
            if(p.Op=="build")return g.TryBuild(p.Slot,p.Kind,null,out reason);
            if(tower==null)throw new Exception("Missing source tower for "+p.Op+" @"+p.Slot);
            if(p.Op=="upgrade"){
                if(TowerCatalog.Get(tower.Kind).UpgradePrice(tower.Level)!=p.Cost)throw new Exception("Actual upgrade price differs");
                return g.TryUpgrade(tower.Id,null,out reason);
            }
            var other=g.TowerAt(p.Other);if(other==null)throw new Exception("Missing fusion partner");
            return g.TryFuse(p.Recipe,tower.Id,other.Id,p.Slot,null,out reason);
        }
        static Row Play(int stage,int seed,bool below,bool crisis,string policy)
        {
            var row=new Row{Key=stage+":"+seed+":"+below+":"+crisis+":"+policy,Stage=CampaignCatalog.StageId(stage),Seed=seed,Growth=below?4:5,Mode=crisis?"Crisis":"Normal",Policy=policy,PlannedSpend=1085};
            try {
                var profile=ChapterTwoChecks.Entry();for(int i=14;i<stage;i++){profile.FirstCleared[i]=true;profile.BestStars[i]=3;}
                profile.Growth[0]=row.Growth;profile.Research=below?new[]{2,1,1,1}:new[]{3,2,2,2};profile.Loadout=2055;
                if(!profile.Valid() || !profile.IsStageOpen(stage))throw new Exception("Invalid profile");
                var game=new DefenseSimulation(new GameBalance(),seed,new CampaignService(profile,null).Battle(stage,crisis));
                var orders=Plan(stage,policy);row.TotalCommands=orders.Count;
                var delays=new List<string>();var hurts=new List<string>();var shots=new int[TowerCatalog.All.Length];int index=0;
                Action buy=()=>{
                    while(index<orders.Count && game.Elapsed+1e-6>=orders[index].At) {
                        var order=orders[index];
                        if(order.At>=90 && row.PrefixHash==null){row.PrefixHash=State(game);row.PrefixLedgerHash=Hash(game.Telemetry.Purchases.ToArray());row.ChoiceCards=(int[])game.Cards.Clone();row.CoreAtChoice=game.CoreHealth;}
                        if(game.Credits<order.Cost)break; // retry later, never top up money or skip a command
                        string why;int spent=game.Telemetry.CreditsSpent;
                        if(!Buy(game,order,out why))throw new Exception("Illegal plan: "+order.Op+" @"+order.Slot+": "+why);
                        if(game.Telemetry.CreditsSpent-spent!=order.Cost)throw new Exception("Purchase price differs");
                        double delay=game.Elapsed-order.At;row.MaximumPurchaseDelay=Math.Max(row.MaximumPurchaseDelay,(float)delay);
                        delays.Add(index+" "+order.Op+" @"+order.Slot+" due="+order.At.ToString("F2",CultureInfo.InvariantCulture)+" actual="+game.Elapsed.ToString("F2",CultureInfo.InvariantCulture));
                        if(order.Op=="fusion"){if(order.Recipe==9)row.BarrageAt=game.Elapsed;else if(order.Recipe==10)row.SealAt=game.Elapsed;}
                        index++;
                    }
                };
                buy();game.Start();
                while(game.State!=RunState.Won && game.State!=RunState.Lost && game.Elapsed<360) {
                    if(game.AwaitingCard)game.ChooseCard(game.CardOffers[0]);
                    buy();game.Tick(.05f,new V2());
                    row.PeakAlive=Math.Max(row.PeakAlive,game.Enemies.Count);
                    row.PeakNearCore=Math.Max(row.PeakNearCore,game.Enemies.Count(x=>V2.DistanceSquared(x.Position,new V2())<36));
                    foreach(var enemy in game.Enemies)if(enemy.Kind==EnemyKind.Brute || enemy.Kind==EnemyKind.Siege)row.StrongSlowEnemySeconds+=enemy.SlowFraction*.05f;
                    foreach(var e in game.Events){
                        if(e.Kind==EventKind.TowerShot && e.Source>=0)shots[e.Source]++;
                        if(e.Kind==EventKind.CoreHurt)hurts.Add(game.Elapsed.ToString("F2",CultureInfo.InvariantCulture)+" lane="+e.Source+" damage="+e.Value.ToString("R",CultureInfo.InvariantCulture));
                    }game.Events.Clear();
                }
                row.Outcome=game.State.ToString();row.MinimumCore=game.MinimumCoreHealth;row.End=game.Elapsed;row.Spent=game.Telemetry.CreditsSpent;row.Remaining=game.Credits;
                row.Towers=game.Towers.Count;row.CompletedCommands=index;row.Spawned=game.Telemetry.Spawned;row.Kills=game.Kills;row.Total=game.Schedule.Sum(x=>x.Count);row.AllSpawned=game.AllWavesSpawned;
                row.Fusions=game.Telemetry.Fusions;row.Supplies=game.SuppliesGranted;row.CardsOffered=game.Telemetry.CardOffers;row.Catalysts=game.Telemetry.CoresGranted;
                row.Cards=(int[])game.Cards.Clone();row.Damage=(float[])game.Telemetry.TowerDamage.Clone();row.CoreDamageByLane=(float[])game.Telemetry.CoreDamageByLane.Clone();row.Shots=shots;
                row.Ledger=game.Telemetry.Purchases.ToArray();row.PurchaseDelays=delays.ToArray();row.CoreHits=hurts.ToArray();
                if(row.Outcome!="Won" && row.Outcome!="Lost")throw new Exception("Run did not finish");
                if(row.Spent>1085 || row.Supplies>5 || row.CardsOffered>3 || row.Catalysts>2 || row.Towers>14)throw new Exception("Economic/cap failure");
                if(row.CompletedCommands==row.TotalCommands && row.Spent!=1085)throw new Exception("Completed plan not equal cost");
                if(row.Outcome=="Won" && (!row.AllSpawned || row.Kills!=row.Total || game.Enemies.Count!=0))throw new Exception("Invalid finite-wave victory");
            }catch(Exception e){row.Error=e.ToString();}
            return row;
        }
        public static Row[] Run(bool pilot,string[] excluded)
        {
            foreach(var s in CampaignCatalog.Stages)BattlefieldLayouts.Get(s);
            var skip=new HashSet<string>(excluded??new string[0]);var cases=new List<Tuple<int,int,bool,bool,string>>();
            foreach(int stage in new[]{18,19})foreach(int seed in pilot?new[]{7,41}:new[]{7,19,41})foreach(bool below in pilot?new[]{false}:new[]{false,true})foreach(bool crisis in new[]{false,true})
            foreach(string policy in stage==18?new[]{"scatter","barrage-front","barrage-west"}:new[]{"scatter","barrage-front","seal"}){
                string key=stage+":"+seed+":"+below+":"+crisis+":"+policy;if(!skip.Contains(key))cases.Add(Tuple.Create(stage,seed,below,crisis,policy));
            }
            var rows=new Row[cases.Count];Parallel.For(0,cases.Count,new ParallelOptions{MaxDegreeOfParallelism=3},i=>{var c=cases[i];rows[i]=Play(c.Item1,c.Item2,c.Item3,c.Item4,c.Item5);});return rows;
        }
    }
}
