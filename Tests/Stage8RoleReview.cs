using System;
using System.Collections.Generic;
using System.Globalization;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Paired policy review, not an optimal strategy or a human playtest.
    public static class Stage8RoleReview {
        static readonly CardKind[] priority={CardKind.Ballistic,CardKind.Range,CardKind.Hunter,CardKind.Credits,CardKind.Control,CardKind.Blast};
        static void Choose(DefenseSimulation g) {if(!g.AwaitingCard)return;foreach(var c in priority)if(g.CardOffers.Contains(c)){g.ChooseCard(c);return;}}
        static void Build(DefenseSimulation g,int socket,TowerKind kind){string why;if(!g.TryBuild(socket,kind,null,out why))throw new Exception("opening build "+socket+": "+why);}
        static string N(double x){return x.ToString("F2",CultureInfo.InvariantCulture);}
        static void Pattern(DefenseSimulation g,string pattern) {
            if(pattern=="baseline")return;
            if(pattern.StartsWith("burst")) {
                for(int wave=0;wave<6;wave++) {
                    var groups=g.Schedule.FindAll(x=>x.Wave==wave);
                    var fast=groups.FindAll(x=>x.Kind==EnemyKind.Runner);var rest=groups.FindAll(x=>x.Kind!=EnemyKind.Runner);
                    for(int i=0;i<fast.Count;i++)fast[i].Start=wave*30+(pattern=="burstFront"?5:13)+i*.25;
                    for(int i=0;i<rest.Count;i++)rest[i].Start=wave*30+5+(pattern=="burstFront"?3:0)+i*((pattern=="burstFront"?19.0:22.0)/Math.Max(1,rest.Count-1));
                    groups.Sort((a,b)=>a.Start.CompareTo(b.Start));bool[] seen=new bool[3];foreach(var x in groups){x.Warned=seen[x.Lane];seen[x.Lane]=true;}
                }
                return;
            }
            double window=Double.Parse(pattern.Substring(4),CultureInfo.InvariantCulture);
            for(int wave=2;wave<6;wave++) {
                var groups=g.Schedule.FindAll(x=>x.Wave==wave);bool[] seen=new bool[3];
                for(int j=0;j<groups.Count;j++) {
                    var x=groups[j];int main=wave%2;x.Lane=j%4==3?g.OpenLanes[1-main]:g.OpenLanes[main];
                    x.Kind=j%4==3?EnemyKind.Grunt:EnemyKind.Runner;
                    if(wave>=4 && j==groups.Count-1)x.Kind=EnemyKind.Brute;
                    x.Start=wave*30+5+(j/4)*(window/Math.Max(1,(groups.Count-1)/4))+(j%4)*.18;
                    x.Warned=seen[x.Lane];seen[x.Lane]=true;
                }
            }
        }
        static string Run(int seed,bool cards,TowerKind third,int socket,int timing,bool calls,bool reinforce,string pattern,bool support=false) {
            var g=new DefenseSimulation(new GameBalance {OfferCards=cards},seed,new CampaignBattle {Stage=CampaignCatalog.Stages[7]});
            g.WaveCallsEnabled=g.Rules.MobileWaveCalls;
            Pattern(g,pattern);
            Build(g,3,TowerKind.Autocannon);Build(g,4,TowerKind.Autocannon);
            if(support)Build(g,2,TowerKind.Autocannon);
            bool thirdBuilt=timing==0;if(thirdBuilt)Build(g,socket,third);
            var fill=new List<int>();foreach(int p in new[]{2,0,1,7,9,5,6,8})if(p!=socket && (!support || p!=2))fill.Add(p);
            g.Start();double actionAt=0,thirdAt=thirdBuilt?0:-1,slowSeconds=0;int builtLater=0,peak=0;var reached=new HashSet<int>();
            var cardLog=new List<string>();
            for(int frame=0;frame<6000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard){int before=g.Credits;Choose(g);cardLog.Add(N(g.Elapsed)+":"+String.Join(",",g.Cards)+":"+(g.Credits-before));}
                if(calls && g.CanCallNextWave()==""){string why;if(!g.TryCallNextWave(g.NextCallWave,null,out why))throw new Exception(why);if(g.AwaitingCard){Choose(g);cardLog.Add(N(g.Elapsed)+":"+String.Join(",",g.Cards));}}
                if(g.Elapsed>=actionAt) {
                    string why;bool acted=false;
                    if(!thirdBuilt && g.SuppliesGranted>=timing && g.TryBuild(socket,third,null,out why)){thirdBuilt=true;thirdAt=g.Elapsed;acted=true;}
                    if(!acted && reinforce) {
                        int[] upgrade=support?new[]{3,4,2,socket}:new[]{3,4,socket};
                        for(int i=0;i<upgrade.Length;i++) {var t=g.TowerAt(upgrade[i]);if(g.SuppliesGranted>=i+1 && t!=null && t.Level==1 && g.TryUpgrade(t.Id,null,out why)){acted=true;break;}}
                        if(!acted && builtLater<2 && g.SuppliesGranted>=4+builtLater && g.TryBuild(fill[builtLater],TowerKind.Autocannon,null,out why)){builtLater++;acted=true;}
                    }
                    actionAt=g.Elapsed+(acted?2:.1);
                }
                g.Tick(.05f,new V2());peak=Math.Max(peak,g.Enemies.Count);
                foreach(var e in g.Enemies)if(e.Kind==EnemyKind.Runner){if(e.SlowFraction>0)slowSeconds+=.05;if(e.Position.Length<=1.16f)reached.Add(e.Id);}
                g.Events.Clear();
            }
            int total=0;foreach(int n in g.Rules.Stage.Counts)total+=n;
            if(g.SuppliesGranted>5 || g.Telemetry.CardOffers>3 || g.Telemetry.CoresGranted>2 || g.Telemetry.Spawned>total)throw new Exception("budget/spawn cap");
            if(g.State==RunState.Won && (!g.AllWavesSpawned || g.Enemies.Count!=0 || g.CoreHealth<=0 || g.Kills!=total))throw new Exception("victory contract");
            return String.Join("\t",new[]{seed.ToString(),cards.ToString(),third.ToString(),socket.ToString(),timing.ToString(),calls.ToString(),reinforce.ToString(),g.State.ToString(),N(g.Elapsed),N(g.MinimumCoreHealth),N(g.PlayerHealth),g.Kills.ToString(),g.Telemetry.Spawned.ToString(),g.Enemies.Count.ToString(),g.Telemetry.CreditsSpent.ToString(),g.Towers.Count.ToString(),N(thirdAt),N(slowSeconds),reached.Count.ToString(),peak.ToString(),g.Telemetry.WaveCalls.ToString(),g.SuppliesGranted.ToString(),g.Telemetry.CardOffers.ToString(),g.Telemetry.CoresGranted.ToString(),N(g.Telemetry.TowerDamage[(int)TowerKind.Binding]),String.Join(";",cardLog),String.Join(";",g.Telemetry.Purchases)});
        }
        public static string RunAll(string pattern) {
            var rows=new List<string>{"seed\tcards\tthird\tsocket\ttiming\tcalls\treinforce\tstate\tseconds\tminimumCore\thunter\tkills\tspawned\tremaining\tspent\ttowers\tthirdBuiltAt\trunnerSlowSeconds\trunnersAtCore\tpeakEnemies\twaveCalls\tsupplies\tcardOffers\tcatalysts\tbindingDamage\tcardLog\tpurchases"};
            foreach(int seed in new[]{7,19,41})foreach(bool cards in new[]{false,true})foreach(var third in new[]{TowerKind.Autocannon,TowerKind.Binding})foreach(int socket in new[]{2,7,9})foreach(int timing in new[]{0,1,2})foreach(bool calls in new[]{false,true})foreach(bool reinforce in new[]{false,true})rows.Add(Run(seed,cards,third,socket,timing,calls,reinforce,pattern));
            return String.Join("\n",rows);
        }
        public static string RunSupportAll(string pattern) {
            var rows=new List<string>{"seed\tcards\tthird\tsocket\ttiming\tcalls\treinforce\tstate\tseconds\tminimumCore\thunter\tkills\tspawned\tremaining\tspent\ttowers\tthirdBuiltAt\trunnerSlowSeconds\trunnersAtCore\tpeakEnemies\twaveCalls\tsupplies\tcardOffers\tcatalysts\tbindingDamage\tcardLog\tpurchases"};
            foreach(int seed in new[]{7,19,41})foreach(bool cards in new[]{false,true})foreach(var third in new[]{TowerKind.Autocannon,TowerKind.Binding})foreach(int socket in new[]{0,7,9})foreach(int timing in new[]{1,2,3})foreach(bool calls in new[]{false,true})foreach(bool reinforce in new[]{false,true})rows.Add(Run(seed,cards,third,socket,timing,calls,reinforce,pattern,true));
            return String.Join("\n",rows);
        }
    }
}
