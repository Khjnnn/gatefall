using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Controlled model experiment: common start, fixed action times and stationary hunter.
    // A successful model run does not measure touch, player choices or fun.
    public static class Stage4FusionTimingReview {
        static void Build(DefenseSimulation g, int slot, TowerKind kind) { string why; if (!g.TryBuild(slot, kind, null, out why)) throw new Exception("build " + slot + ": " + why); }
        static void Upgrade(DefenseSimulation g, int slot) { string why; var t=g.TowerAt(slot); if(t==null || !g.TryUpgrade(t.Id,null,out why)) throw new Exception("upgrade " + slot); }
        static string Run(int seed, bool fusion) {
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle { Stage=CampaignCatalog.Stages[3] });
            Build(g,2,TowerKind.Autocannon); Upgrade(g,2);
            Build(g,10,TowerKind.Binding); Upgrade(g,10);
            Build(g,8,TowerKind.Autocannon);
            g.Start();
            int step=0; var events=new List<string>();
            float[] times={62f,64f,90f,92f,120f,122f,150f};
            for(int frame=0;frame<5000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard)g.ChooseCard(g.CardOffers[0]);
                if(step<times.Length && g.Elapsed>=times[step]) {
                    string why;
                    if(step==0 && fusion) {
                        var a=g.TowerAt(2); var b=g.TowerAt(10);
                        if(a==null || b==null || !g.TryFuse(0,a.Id,b.Id,2,null,out why))throw new Exception("fusion failed");
                    } else if(step==1 && fusion) Build(g,10,TowerKind.Autocannon);
                    else if(step==0 && !fusion) Build(g,4,TowerKind.Autocannon);
                    else if(step==1 && !fusion) Upgrade(g,8);
                    else if(step==2) Build(g,fusion?4:6,TowerKind.Autocannon);
                    else if(step==3) Upgrade(g,fusion?10:4);
                    else if(step==4) Build(g,fusion?6:3,TowerKind.Autocannon);
                    else if(step==5) Upgrade(g,fusion?8:6);
                    else if(step==6) Build(g,fusion?3:0,TowerKind.Autocannon);
                    events.Add(String.Format("{0:F1}:hp={1:F0},credits={2}",g.Elapsed,g.CoreHealth,g.Credits)); step++;
                }
                g.Tick(.05f,new V2());g.Events.Clear();
            }
            return String.Format("seed={0} policy={1} state={2} t={3:F2} core={4:F1} kills={5} towers={6} spent={7} fusion={8:F1}\n  checkpoints: {9}\n  purchases: {10}",seed,fusion?"fusion-rebuild":"reinforce",g.State,g.Elapsed,g.CoreHealth,g.Kills,g.Towers.Count,g.Telemetry.CreditsSpent,g.Telemetry.FirstFusion,String.Join("; ",events),String.Join("; ",g.Telemetry.Purchases));
        }
        public static string RunAll(){var lines=new List<string>();foreach(int seed in new[]{7,19,41})foreach(bool fusion in new[]{false,true})lines.Add(Run(seed,fusion));return String.Join("\n",lines);}
    }
}
