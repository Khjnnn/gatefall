using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Gatefall.Core;
namespace Gatefall.Tests
{
    // Replay recorded successful orders; fixtures explicitly retain their actual timing and affordability limits.
    public static class ChapterTwoInvestmentFollowup
    {
        public sealed class Fixture
        {
            public string BaseKey,Stage,Policy,Mode,Variant;public int Seed,Growth;
            public string[] Ledger,PurchaseDelays;
        }
        public sealed class Row
        {
            public string BaseKey,Variant,Stage,Policy,Mode,Outcome,Error,PrefixHash;
            public int Seed,Growth,PlannedSpend,Spent,Remaining,Towers,Completed,Commands,Kills,Spawned,Total,PeakAfter150;
            public float MinimumCore,MaximumDelay,StrongSlowAfter150;public double End,SealAt=-1;
            public int[] Cards,Shots;public float[] Damage,DamageAt150,CoreDamageByLane,CoreAt150ByLane,CoreAfter150ByLane;
            public string[] Ledger,CoreHits,ActualOrders;
        }
        sealed class Order {public double At,OriginalDue;public string Op;public TowerKind Kind;public int Slot,Other,Recipe,Cost;}
        static List<Order> Orders(Fixture f)
        {
            var orders=new List<Order>();var levels=new Dictionary<int,int>();
            if(f.Ledger.Length!=f.PurchaseDelays.Length)throw new Exception("Recorded order counts differ");
            bool fused=false;
            for(int i=0;i<f.Ledger.Length;i++){
                var timing=Regex.Match(f.PurchaseDelays[i],@"due=([\d.]+) actual=([\d.]+)");if(!timing.Success)throw new Exception("Missing precise recorded timing");
                double due=double.Parse(timing.Groups[1].Value,CultureInfo.InvariantCulture),at=double.Parse(timing.Groups[2].Value,CultureInfo.InvariantCulture);
                if(f.Variant.StartsWith("cap825") && due>=180)continue;
                var line=f.Ledger[i];var m=Regex.Match(line,@"^[\d.]+ (build|upgrade) (\w+) @(\d+)$");var order=new Order{At=at,OriginalDue=due};
                if(m.Success){
                    order.Op=m.Groups[1].Value;order.Kind=(TowerKind)Enum.Parse(typeof(TowerKind),m.Groups[2].Value);order.Slot=int.Parse(m.Groups[3].Value);
                    if(f.Variant=="spare-pair" && fused && order.Slot==2)order.Slot=1;
                    if(f.Variant=="cap825-seal-east" && fused && order.Slot==2 && order.Kind==TowerKind.Autocannon)order.Slot=4;
                    if(order.Op=="build"){levels[order.Slot]=1;order.Cost=TowerCatalog.Get(order.Kind).BuildCost;}
                    else {order.Cost=TowerCatalog.Get(order.Kind).UpgradePrice(levels[order.Slot]);levels[order.Slot]++;}
                }else{
                    m=Regex.Match(line,@"^[\d.]+ fusion .+ -> (\w+) @(\d+)$");if(!m.Success)throw new Exception("Unsupported recorded order");
                    order.Op="fusion";order.Kind=(TowerKind)Enum.Parse(typeof(TowerKind),m.Groups[1].Value);order.Slot=int.Parse(m.Groups[2].Value);
                    if(order.Kind==TowerKind.Barrage){
                        order.Recipe=9;
                        order.Other=f.Stage=="2-5"?(f.Policy=="barrage-west"?2:21):25;
                        if(f.Variant=="spare-pair")order.Other=1;
                    }else if(order.Kind==TowerKind.Seal){order.Recipe=10;order.Other=2;if(f.Variant=="cap825-seal-east"){order.Slot=2;order.Other=4;}}
                    else throw new Exception("Unsupported recorded fusion");
                    levels[order.Slot]=1;order.Cost=TowerCatalog.Recipes[order.Recipe].Credits;fused=true;
                }
                orders.Add(order);
            }
            return orders;
        }
        static Row Play(Fixture f)
        {
            var row=new Row{BaseKey=f.BaseKey,Variant=f.Variant,Stage=f.Stage,Policy=f.Policy,Mode=f.Mode,Seed=f.Seed,Growth=f.Growth};
            try {
                int stage=f.Stage=="2-5"?18:19;var profile=ChapterTwoChecks.Entry();
                for(int i=14;i<stage;i++){profile.FirstCleared[i]=true;profile.BestStars[i]=3;}
                profile.Growth[0]=f.Growth;profile.Research=f.Growth==4?new[]{2,1,1,1}:new[]{3,2,2,2};profile.Loadout=2055;
                var g=new DefenseSimulation(new GameBalance(),f.Seed,new CampaignService(profile,null).Battle(stage,f.Mode=="Crisis"));
                var plan=Orders(f);row.Commands=plan.Count;row.PlannedSpend=plan.Sum(x=>x.Cost);
                if(f.Variant.StartsWith("cap825") && row.PlannedSpend!=825)throw new Exception("Restricted package must cost825");
                int index=0;var shots=new int[TowerCatalog.All.Length];var hits=new List<string>();var actual=new List<string>();
                var state=typeof(ChapterTwoInvestmentReview).GetMethod("State",BindingFlags.Static|BindingFlags.NonPublic);
                Action buy=()=>{
                    while(index<plan.Count && g.Elapsed+1e-6>=plan[index].At){
                        var o=plan[index];if(o.OriginalDue>=90 && row.PrefixHash==null)row.PrefixHash=(string)state.Invoke(null,new object[]{g});
                        if(g.Credits<o.Cost)break;
                        string reason;bool ok;var t=g.TowerAt(o.Slot);int spent=g.Telemetry.CreditsSpent;
                        if(o.Op=="build")ok=g.TryBuild(o.Slot,o.Kind,null,out reason);
                        else if(o.Op=="upgrade")ok=g.TryUpgrade(t.Id,null,out reason);
                        else {var other=g.TowerAt(o.Other);ok=g.TryFuse(o.Recipe,t.Id,other.Id,o.Slot,null,out reason);if(ok && o.Recipe==10)row.SealAt=g.Elapsed;}
                        if(!ok || g.Telemetry.CreditsSpent-spent!=o.Cost)throw new Exception("Replay command/price mismatch: "+o.Op+" @"+o.Slot+" "+reason);
                        row.MaximumDelay=Math.Max(row.MaximumDelay,(float)(g.Elapsed-o.At));actual.Add(index+" "+o.Op+" @"+o.Slot+" actual="+g.Elapsed.ToString("F2",CultureInfo.InvariantCulture));index++;
                    }
                };
                buy();g.Start();
                while(g.State!=RunState.Won && g.State!=RunState.Lost && g.Elapsed<360){
                    if(g.AwaitingCard)g.ChooseCard(g.CardOffers[0]);
                    if(g.Elapsed>=150 && row.DamageAt150==null){row.DamageAt150=(float[])g.Telemetry.TowerDamage.Clone();row.CoreAt150ByLane=(float[])g.Telemetry.CoreDamageByLane.Clone();}
                    buy();g.Tick(.05f,new V2());
                    if(g.Elapsed>=150){row.PeakAfter150=Math.Max(row.PeakAfter150,g.Enemies.Count);foreach(var e in g.Enemies)if(e.Kind==EnemyKind.Brute || e.Kind==EnemyKind.Siege)row.StrongSlowAfter150+=e.SlowFraction*.05f;}
                    foreach(var e in g.Events){if(e.Kind==EventKind.TowerShot && e.Source>=0)shots[e.Source]++;if(e.Kind==EventKind.CoreHurt)hits.Add(g.Elapsed.ToString("F2",CultureInfo.InvariantCulture)+" lane="+e.Source+" damage="+e.Value.ToString("R",CultureInfo.InvariantCulture));}g.Events.Clear();
                }
                row.Outcome=g.State.ToString();row.End=g.Elapsed;row.MinimumCore=g.MinimumCoreHealth;row.Spent=g.Telemetry.CreditsSpent;row.Remaining=g.Credits;row.Towers=g.Towers.Count;row.Completed=index;
                row.Kills=g.Kills;row.Spawned=g.Telemetry.Spawned;row.Total=g.Schedule.Sum(x=>x.Count);row.Cards=(int[])g.Cards.Clone();row.Damage=(float[])g.Telemetry.TowerDamage.Clone();row.CoreDamageByLane=(float[])g.Telemetry.CoreDamageByLane.Clone();
                row.CoreAfter150ByLane=row.CoreDamageByLane.Select((x,i)=>x-(row.CoreAt150ByLane??new float[4])[i]).ToArray();row.Shots=shots;row.CoreHits=hits.ToArray();row.Ledger=g.Telemetry.Purchases.ToArray();row.ActualOrders=actual.ToArray();
                if(row.Spent>row.PlannedSpend || g.SuppliesGranted>5 || g.Telemetry.CoresGranted>2 || g.Telemetry.CardOffers>3)throw new Exception("Economic cap failure");
                if(g.State==RunState.Won && (!g.AllWavesSpawned || g.Enemies.Count!=0 || g.Kills!=row.Total))throw new Exception("Invalid victory");
            }catch(Exception e){row.Error=e.ToString();}return row;
        }
        public static Row[] Run(Fixture[] fixtures)
        {
            foreach(var stage in CampaignCatalog.Stages)BattlefieldLayouts.Get(stage);var rows=new Row[fixtures.Length];
            Parallel.For(0,fixtures.Length,new ParallelOptions{MaxDegreeOfParallelism=3},i=>rows[i]=Play(fixtures[i]));return rows;
        }
    }
}
