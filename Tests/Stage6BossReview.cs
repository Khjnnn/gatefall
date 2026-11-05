using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Fixed, affordable action windows; this records model evidence, not human play.
    public static class Stage6BossReview {
        static void Build(DefenseSimulation g,int slot,TowerKind kind) { string why;if(!g.TryBuild(slot,kind,null,out why))throw new Exception("build "+slot+": "+why); }
        static void Upgrade(DefenseSimulation g,int slot) { string why;var t=g.TowerAt(slot);if(t==null || !g.TryUpgrade(t.Id,null,out why))throw new Exception("upgrade "+slot); }
        static string Run(int seed,string policy,bool cards) {
            bool fusion=policy=="arc-rebuild";
            var g=new DefenseSimulation(new GameBalance {OfferCards=cards},seed,new CampaignBattle {Stage=CampaignCatalog.Stages[5]});
            Build(g,1,TowerKind.Autocannon);Build(g,8,fusion?TowerKind.Binding:TowerKind.Autocannon);Build(g,2,TowerKind.Autocannon);g.Start();
            float[] times={30.5f,32.5f,60.5f,62.5f,90.5f,92.5f,120.5f,122.5f,150.5f};
            int step=0;double bossKilled=-1;float core150=-1,bossRemaining=0;var checkpoints=new List<string>();
            for(int frame=0;frame<5000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard)g.ChooseCard(g.CardOffers[0]);
                if(core150<0 && g.Elapsed>=150)core150=g.CoreHealth;
                if(policy!="fixed" && step<times.Length && g.Elapsed>=times[step]) {
                    if(step==0)Upgrade(g,1);
                    else if(step==1)Upgrade(g,8);
                    else if(step==2 && fusion) {string why;var a=g.TowerAt(1);var b=g.TowerAt(8);if(!g.TryFuse(0,a.Id,b.Id,8,null,out why))throw new Exception("fusion: "+why);}
                    else if(step==2)Build(g,4,TowerKind.Autocannon);
                    else if(step==3 && fusion)Build(g,1,TowerKind.Autocannon);
                    else if(step==3)Upgrade(g,2);
                    else if(step==4 && fusion)Upgrade(g,2);
                    else if(step==4)Build(g,5,TowerKind.Autocannon);
                    else if(step==5 && fusion)Build(g,4,TowerKind.Autocannon);
                    else if(step==5)Upgrade(g,4);
                    else if(step==6)Upgrade(g,fusion?1:5);
                    else if(step==7)Build(g,fusion?5:3,TowerKind.Autocannon);
                    else Upgrade(g,fusion?8:3);
                    checkpoints.Add(String.Format("{0:F1}:core={1:F1},credits={2}",g.Elapsed,g.CoreHealth,g.Credits));step++;
                }
                g.Tick(.05f,new V2());
                if(g.BossDefeated && bossKilled<0)bossKilled=g.Elapsed;
                var boss=g.Enemies.Find(e=>e.Kind==EnemyKind.Boss);bossRemaining=boss==null?0:boss.Health;
                g.Events.Clear();
            }
            return String.Format("seed={0} cards={1} policy={2} result={3} time={4:F1} minCore={5:F1} core150={6:F1} hunter={7:F1} kills={8} towers={9} spent={10} fusion={11:F1} bossSpawned={12} bossKilled={13:F1} bossRemaining={14:F1}\n  checkpoints: {15}\n  purchases: {16}",seed,cards,policy,g.State,g.Elapsed,g.MinimumCoreHealth,core150,g.PlayerHealth,g.Kills,g.Towers.Count,g.Telemetry.CreditsSpent,g.Telemetry.FirstFusion,g.BossSpawned,bossKilled,bossRemaining,String.Join("; ",checkpoints),String.Join("; ",g.Telemetry.Purchases));
        }
        public static string RunAll() {var lines=new List<string>();foreach(int seed in new[]{7,19,41})foreach(bool cards in new[]{false,true})foreach(string policy in new[]{"fixed","reinforce","arc-rebuild"})lines.Add(Run(seed,policy,cards));return String.Join("\n",lines);}
    }
}
