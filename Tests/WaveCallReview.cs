using System;
using System.Collections.Generic;
using System.Globalization;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Controlled API/model review. No mobile interaction or human fun is evaluated here.
    public static class WaveCallReview {
        static int checks;
        static void Check(bool ok,string label) {if(!ok)throw new Exception(label);checks++;}
        static void Cards(DefenseSimulation g) {if(g.AwaitingCard && !g.ChooseCard(g.CardOffers[0]))throw new Exception("card choice");}
        static void Until(DefenseSimulation g,double time) {
            while(g.Elapsed<time-.00001 && g.State!=RunState.Won && g.State!=RunState.Lost) {Cards(g);g.Tick((float)Math.Min(.05,time-g.Elapsed),new V2());g.Events.Clear();}
        }
        static DefenseSimulation Quiet(int stage,bool cards=true) {
            return new DefenseSimulation(new GameBalance {SpawnEnemies=false,OfferCards=cards},7,new CampaignBattle {Stage=CampaignCatalog.Stages[stage]}) {WaveCallsEnabled=true};
        }
        static string Snapshot(DefenseSimulation g) {
            var s=new List<string> {g.State.ToString(),g.Elapsed.ToString("R"),g.Credits.ToString(),g.Cores.ToString(),g.Wave.ToString(),g.SuppliesGranted.ToString(),g.Telemetry.WaveCalls.ToString(),g.Telemetry.CardOffers.ToString()};
            foreach(var x in g.Schedule)s.Add(x.Start.ToString("R")+":"+x.Spawned+":"+x.Warned);
            foreach(var c in g.CardOffers)s.Add(c.ToString());return String.Join("|",s);
        }
        static void Rejected(DefenseSimulation g,int expected,string command,string label) {string before=Snapshot(g),why;Check(!g.TryCallNextWave(expected,command,out why) && why!="" && Snapshot(g)==before,label);}
        public static string Verify() {
            checks=0;string why;
            var disabled=Quiet(3);disabled.WaveCallsEnabled=false;disabled.Start();Until(disabled,22);Rejected(disabled,1,null,"disabled API is atomic");
            for(int i=0;i<3;i++){var early=Quiet(i);early.Start();Until(early,22);Rejected(early,1,null,"easy opening unchanged "+i);}
            var g=Quiet(3);Rejected(g,1,null,"ready call rejected");g.Start();Until(g,15);Rejected(g,1,null,"too early call rejected");Until(g,22);
            Check(Math.Abs(g.WaveProgressFraction(0)-22f/30)<.001 && g.WaveProgressFraction(1)==0,"uncalled wave bar uses original milestones");
            var current=new List<double>();foreach(var x in g.Schedule)if(x.Wave==0)current.Add(x.Start);
            double beforeTime=g.Elapsed;V2 beforePosition=g.PlayerPosition;
            g.Towers.Add(new Tower {Id=999,Socket=0,Kind=TowerKind.Autocannon,Cooldown=3,Commission=1});
            g.Projectiles.Add(new Projectile {Id=1000,Life=2});
            Rejected(g,2,"stale","wrong wave is atomic");
            Check(g.TryCallNextWave(1,"first",out why),"first call succeeds");
            Check(g.Elapsed==beforeTime && g.PlayerPosition.X==beforePosition.X && g.PlayerPosition.Y==beforePosition.Y && g.Towers[0].Cooldown==3 && g.Towers[0].Commission==1 && g.Projectiles[0].Life==2,"call does not skip actor time");
            Check(Math.Abs(g.GetWaveBriefing(1).FirstSpawn-29)<.0001 && g.GetWaveBriefing(1).FirstSpawn-g.Elapsed>=3,"six second advance with at least three second lead");
            int c=0;foreach(var x in g.Schedule)if(x.Wave==0)Check(current[c++]==x.Start,"current wave spawn not compressed");
            Check(g.SuppliesGranted==1 && g.Credits==g.Balance.StartingCredits+100 && g.AwaitingCard && g.Telemetry.CardOffers==1,"first supply/card advanced, no extra payout");
            Check(g.WaveProgressFraction(0)==1 && g.WaveProgressFraction(1)==0 && g.Events.Exists(e=>e.Kind==EventKind.Supply && e.Wave==1 && e.Value==100),"called bar and prepaid supply share new milestone");
            Rejected(g,1,"first","duplicate command atomic");Rejected(g,1,null,"duplicate wave atomic");Rejected(g,2,null,"card blocks next call");
            Cards(g);g.TogglePause();Rejected(g,2,null,"pause call atomic");g.TogglePause();
            g.Towers.Clear();g.Projectiles.Clear();
            for(int next=2;next<6;next++){Until(g,g.GetWaveBriefing(next).FirstSpawn-13);Check(g.TryCallNextWave(next,"call-"+next,out why),"call "+next);Cards(g);}
            Check(g.SuppliesGranted==5 && g.Telemetry.CardOffers==3 && g.Telemetry.CoresGranted==2 && g.Telemetry.WaveCalls==5 && Math.Abs(g.Telemetry.WaveAdvanceSeconds-30)<.001,"fixed five supplies/three cards/two catalysts and thirty second cap");
            int credits=g.Credits;Until(g,220);Check(g.Credits==credits && g.SuppliesGranted==5 && g.Telemetry.CardOffers==3 && g.Telemetry.CoresGranted==2,"cleanup cannot repay original milestones");Rejected(g,6,null,"no call after final wave");
            var boss=Quiet(5,false);boss.Start();Until(boss,137);Check(boss.TryCallNextWave(5,null,out why),"boss final call");Check(Math.Abs(boss.GetWaveBriefing(5).FirstSpawn-144)<.0001 && boss.GetWaveBriefing(5).KindCount(EnemyKind.Boss)==1,"boss keeps count and warning lead");
            var original=Quiet(3);original.Start();Until(original,26);original.TogglePause();Rejected(original,1,null,"pause in actual window");original.TogglePause();Until(original,30);Check(original.SuppliesGranted==1 && original.Telemetry.WaveCalls==0 && Math.Abs(original.GetWaveBriefing(1).FirstSpawn-35)<.0001,"no call preserves thirty second supply and thirty-five second spawn");
            var retry=Quiet(3);retry.Start();Until(retry,22);Rejected(retry,2,"retry","failed ID not consumed");Check(retry.TryCallNextWave(1,"retry",out why),"failed ID can be retried for correct wave");
            var lab=new DefenseSimulation(new GameBalance(),7,new CampaignBattle()) {WaveCallsEnabled=true};Rejected(lab,1,null,"lab has no campaign call");
            Check(!(new CampaignBattle()).MobileWaveCalls && !(new CampaignBattle {Stage=CampaignCatalog.Stages[2]}).MobileWaveCalls && (new CampaignBattle {Stage=CampaignCatalog.Stages[3]}).MobileWaveCalls && !(new CampaignBattle {Stage=CampaignCatalog.Stages[3],Crisis=true}).MobileWaveCalls && (new CampaignBattle {Stage=CampaignCatalog.Stages[13]}).MobileWaveCalls,"mobile policy excludes opening, lab and crisis");
            var lost=new DefenseSimulation(new GameBalance {OfferCards=false},7,new CampaignBattle {Stage=CampaignCatalog.Stages[3]}) {WaveCallsEnabled=true};lost.Start();Until(lost,300);Check(lost.State==RunState.Lost,"terminal loss fixture");Rejected(lost,lost.NextCallWave,null,"loss cannot call");
            var won=new DefenseSimulation(new GameBalance {PlayerRange=100,PlayerDamage=100000,PlayerFireInterval=.01f},7,new CampaignBattle {Stage=CampaignCatalog.Stages[3]}) {WaveCallsEnabled=true};won.Start();Until(won,250);Check(won.State==RunState.Won,"terminal victory fixture");Rejected(won,won.NextCallWave,null,"victory cannot call");
            var liveBoss=new DefenseSimulation(new GameBalance {CoreHealth=100000,PlayerHealth=100000,OfferCards=false},7,new CampaignBattle {Stage=CampaignCatalog.Stages[5]}) {WaveCallsEnabled=true};liveBoss.Start();Until(liveBoss,141);
            Check(liveBoss.TryCallNextWave(5,null,out why) && liveBoss.Telemetry.CardOffers==0,"boss call honors cards-disabled setting");liveBoss.Events.Clear();liveBoss.Tick(.02f,new V2());Check(liveBoss.Events.Exists(e=>e.Kind==EventKind.Warning && e.Wave==5 && e.Value==1),"shifted boss warning uses original wave ID before spawn");Until(liveBoss,144.02);Check(liveBoss.BossSpawned,"shifted boss actually spawns at144");
            return "PASS: "+checks+" wave-call invariants. Model checks do not cover Android interaction or human fun.";
        }
        static void Build(DefenseSimulation g,int slot,TowerKind kind) {string why;if(!g.TryBuild(slot,kind,null,out why))throw new Exception("opening build "+slot+" "+why);}
        static void Upgrade(DefenseSimulation g,int slot) {string why;var t=g.TowerAt(slot);if(t==null || !g.TryUpgrade(t.Id,null,out why))throw new Exception("opening upgrade "+slot);}
        static int Closest(DefenseSimulation g,V2 point,List<int> used) {
            int best=-1;float distance=float.MaxValue;for(int i=0;i<g.BoardSockets.Length;i++)if(!used.Contains(i)) {float d=V2.Distance(g.BoardSockets[i],point);if(d<distance){best=i;distance=d;}}
            used.Add(best);return best;
        }
        static string Run(int stage,string policy,string calling,int seed,bool crisis) {
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle {Stage=CampaignCatalog.Stages[stage],Crisis=crisis}) {WaveCallsEnabled=true};
            // Shared 125-credit opening; 1-4 retains its existing additional starting credits.
            var used=new List<int>();int west=Closest(g,new V2(-13,3),used),north=Closest(g,new V2(2,5.7f),used),east=Closest(g,new V2(13,-3),used);
            Build(g,west,TowerKind.Autocannon);Build(g,north,TowerKind.Binding);Build(g,east,TowerKind.Autocannon);
            g.Start();double nextAction=2,fused=-1,rebuilt=-1;int peak=0,step=0;
            var empty=new List<int>();for(int i=0;i<g.BoardSockets.Length;i++)if(!used.Contains(i))empty.Add(i);int[] fill=empty.ToArray();
            for(int frame=0;frame<6000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                Cards(g);
                if(calling!="none" && (calling=="earliest" || g.Enemies.Count==0) && g.CanCallNextWave()=="") {string reason;if(!g.TryCallNextWave(g.NextCallWave,null,out reason))throw new Exception(reason);Cards(g);}
                if(g.Elapsed>=nextAction) {
                    string why;bool acted=false;
                    if(policy=="fusion" && fused<0) {
                        var a=g.TowerAt(west);var b=g.TowerAt(north);
                        if(a!=null && b!=null) {
                            if(a.Level<2)acted=g.TryUpgrade(a.Id,null,out why);
                            else if(b.Level<2)acted=g.TryUpgrade(b.Id,null,out why);
                            else if(g.TryFuse(0,a.Id,b.Id,west,null,out why)){acted=true;fused=g.Elapsed;}
                        }
                    } else if(policy=="fusion" && g.TowerAt(north)==null) {
                        if(g.TryBuild(north,TowerKind.Autocannon,null,out why)){acted=true;rebuilt=g.Elapsed;}
                    } else {
                        if(policy!="split")foreach(var t in g.Towers.ToArray())if(g.TryUpgrade(t.Id,null,out why)){acted=true;break;}
                        if(!acted && step<fill.Length){int slot=fill[step];if(g.TryBuild(slot,TowerKind.Autocannon,null,out why)){acted=true;step++;}}
                        if(!acted && policy=="split" && step>=fill.Length)foreach(var t in g.Towers.ToArray())if(g.TryUpgrade(t.Id,null,out why)){acted=true;break;}
                    }
                    nextAction=g.Elapsed+2;
                }
                g.Tick(.05f,new V2());peak=Math.Max(peak,g.Enemies.Count);g.Events.Clear();
            }
            if(g.SuppliesGranted>5 || g.Telemetry.CardOffers>3 || g.Telemetry.CoresGranted>2)throw new Exception("budget cap");
            int expected=g.Rules.Stage.Boss?1:0;foreach(int n in g.Rules.Stage.Counts)expected+=n;
            if(g.Telemetry.Spawned>expected || (g.State==RunState.Won && g.Kills!=expected))throw new Exception("spawn conservation");
            if(g.State==RunState.Won && (!g.AllWavesSpawned || g.Enemies.Count!=0 || g.CoreHealth<=0 || (g.Rules.Stage.Boss && !g.BossDefeated)))throw new Exception("victory contract");
            return String.Join("\t",new[]{"1-"+(stage+1),crisis?"crisis":"normal",policy,calling,seed.ToString(),g.State.ToString(),g.Elapsed.ToString("F2",CultureInfo.InvariantCulture),g.CoreHealth.ToString("F1",CultureInfo.InvariantCulture),g.Telemetry.WaveCalls.ToString(),g.Telemetry.WaveAdvanceSeconds.ToString("F2",CultureInfo.InvariantCulture),g.SuppliesGranted.ToString(),g.Telemetry.CardOffers.ToString(),g.Telemetry.CoresGranted.ToString(),g.Kills.ToString(),g.Telemetry.Spawned.ToString(),g.Enemies.Count.ToString(),g.Telemetry.CreditsSpent.ToString(),g.Towers.Count.ToString(),peak.ToString(),fused.ToString("F2",CultureInfo.InvariantCulture),rebuilt.ToString("F2",CultureInfo.InvariantCulture),g.BossDefeated.ToString(),String.Join(";",g.Telemetry.WaveCallLog)});
        }
        public static string RunAll() {
            var rows=new List<string> {"stage\tdifficulty\tpolicy\tcalling\tseed\tstate\tseconds\tcore\tcalls\tadvancedSeconds\tsupplies\tcards\tcatalysts\tkills\tspawned\tremaining\tspent\ttowers\tpeakEnemies\tfirstFusion\trefill\tbossDefeated\tcallLog"};
            foreach(int stage in new[]{3,5,7,13})foreach(bool crisis in new[]{false,true})foreach(string policy in new[]{"split","reinforce","fusion"})foreach(string calling in new[]{"none","earliest","clear"})foreach(int seed in new[]{7,19,41})rows.Add(Run(stage,policy,calling,seed,crisis));
            return String.Join("\n",rows);
        }
    }
}
