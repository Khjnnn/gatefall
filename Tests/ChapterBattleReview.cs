using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class ChapterBattleReview {
        static void Choose(DefenseSimulation g) { if(g.AwaitingCard) g.ChooseCard(g.CardOffers[0]); }
        static int Closest(DefenseSimulation g, V2 p) {
            int best=-1;float distance=float.MaxValue;
            for(int i=0;i<g.BoardSockets.Length;i++) { float d=V2.Distance(g.BoardSockets[i],p); if(d<distance) {best=i;distance=d;} }
            return best;
        }
        static string Run(int stage,string policy,int seed) {
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle { Stage=CampaignCatalog.Stages[stage] });
            int[] order;
            if(policy=="west" || policy=="west_fixed" || policy=="west_blast") {
                var positions=new[]{new V2(-9,1),new V2(-6,3),new V2(-3,3),new V2(0,-3),new V2(4,1)};
                var slots=new List<int>();foreach(var p in positions){int s=Closest(g,p);if(!slots.Contains(s))slots.Add(s);}order=slots.ToArray();
            } else {
                var slots=new List<int>();foreach(int lane in g.OpenLanes){V2[] road=g.Roads[lane];int s=Closest(g,road[road.Length/2]);if(!slots.Contains(s))slots.Add(s);}
                for(int i=0;i<g.BoardSockets.Length;i++)if(!slots.Contains(i))slots.Add(i);order=slots.ToArray();
            }
            int next=0;string why;int built=0;float min=300;bool boss=false;
            if(policy!="idle") while(next<order.Length && g.Credits>=40 && built<(policy.EndsWith("_fixed") ? 2 : 3)) {
                TowerKind kind=policy=="west_blast" && stage>=3 && next==1 ? TowerKind.Rupture : policy=="roles" && stage>=1 && next==1 ? TowerKind.Binding : TowerKind.Autocannon;
                if(g.CanBuild(order[next],kind)=="") {g.TryBuild(order[next],kind,null,out why);built++;}next++;
            }
            g.Start();double nextAction=3;
            for(int frame=0;frame<12000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                Choose(g);
                if(policy!="idle" && !policy.EndsWith("_fixed") && g.Elapsed>=nextAction) {
                    bool acted=false;
                    foreach(var t in g.Towers.ToArray())if(t.Level==1 && g.CanUpgrade(t.Id)=="") {g.TryUpgrade(t.Id,null,out why);acted=true;break;}
                    if(!acted && next<order.Length && g.Credits>=40) {if(g.TryBuild(order[next],TowerKind.Autocannon,null,out why))built++;next++;}
                    nextAction=g.Elapsed+2;
                }
                g.Tick(.05f,new V2());min=Math.Min(min,g.CoreHealth);boss|=g.BossDefeated;g.Events.Clear();
            }
            return String.Format("stage=1-{0} policy={1} seed={2} result={3} t={4:F1} core={5:F0} hunter={6:F0} built={7} kills={8} boss={9}",stage+1,policy,seed,g.State,g.Elapsed,min,g.PlayerHealth,built,g.Kills,boss);
        }
        public static string RunAll(){var lines=new List<string>();for(int stage=0;stage<CampaignCatalog.Stages.Length;stage++)foreach(string policy in new[]{"idle","west_fixed","split_fixed","west","west_blast","roles"})foreach(int seed in new[]{7,19,41})lines.Add(Run(stage,policy,seed));return String.Join("\n",lines);}

        static void Buy(DefenseSimulation g,int slot,TowerKind kind) {string why;if(!g.TryBuild(slot,kind,null,out why))throw new Exception("post-boss build "+slot+": "+why);}
        static void Level(DefenseSimulation g,int slot) {string why;var t=g.TowerAt(slot);if(t==null || !g.TryUpgrade(t.Id,null,out why))throw new Exception("post-boss upgrade "+slot);}
        static string PostBoss(int seed,int hunter,string policy,bool cards) {
            bool fusion=policy=="arc-rebuild";
            var g=new DefenseSimulation(new GameBalance {OfferCards=cards},seed,new CampaignBattle {Stage=CampaignCatalog.Stages[6],Hunter=hunter});
            Buy(g,1,TowerKind.Autocannon);Buy(g,8,TowerKind.Autocannon);
            if(fusion)Buy(g,6,TowerKind.Binding);
            g.Start();int step=0,downs=0,revivals=0;
            float[] times=fusion?new[]{30.5f,32.5f,60.5f,62.5f,90.5f,120.5f,150.5f}:new[]{30.5f,60.5f,90.5f,120.5f,150.5f};
            for(int frame=0;frame<5000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard)g.ChooseCard(g.CardOffers[0]);
                if(policy!="fixed" && step<times.Length && g.Elapsed>=times[step]) {
                    if(fusion) {
                        if(step==0)Level(g,1);
                        else if(step==1)Level(g,6);
                        else if(step==2) {string why;if(!g.TryFuse(0,g.TowerAt(1).Id,g.TowerAt(6).Id,1,null,out why))throw new Exception("post-boss fusion: "+why);}
                        else if(step==3)Buy(g,6,TowerKind.Autocannon);
                        else if(step==4)Level(g,8);
                        else if(step==5)Buy(g,7,TowerKind.Autocannon);
                        else Level(g,1);
                    } else {
                        if(step==0)Buy(g,3,TowerKind.Autocannon);
                        else if(step==1)Buy(g,7,TowerKind.Autocannon);
                        else if(step==2)Level(g,1);
                        else if(step==3)Level(g,8);
                        else Level(g,3);
                    }
                    step++;
                }
                g.Tick(.05f,new V2());
                foreach(var e in g.Events){if(e.Kind==EventKind.HunterDown)downs++;if(e.Kind==EventKind.HunterRevived)revivals++;}
                g.Events.Clear();
            }
            return String.Format("stage=1-7 hunter={0} seed={1} cards={2} policy={3} result={4} t={5:F1} minCore={6:F1} hunterHP={7:F1} kills={8} towers={9} spent={10} creditsLeft={11} fusion={12:F1} downs={13} revivals={14}\n  purchases: {15}",CampaignCatalog.Hunters[hunter],seed,cards,policy,g.State,g.Elapsed,g.MinimumCoreHealth,g.PlayerHealth,g.Kills,g.Towers.Count,g.Telemetry.CreditsSpent,g.Credits,g.Telemetry.FirstFusion,downs,revivals,String.Join("; ",g.Telemetry.Purchases));
        }
        public static string RunPostBoss(){var lines=new List<string>();foreach(int hunter in new[]{0,2})foreach(int seed in new[]{7,19,41})foreach(bool cards in new[]{false,true})foreach(string policy in new[]{"fixed","reinforce","arc-rebuild"})lines.Add(PostBoss(seed,hunter,policy,cards));return String.Join("\n",lines);}
    }
}
