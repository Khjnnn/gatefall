using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Deterministic automated policies; these are not a substitute for human play.
    public static class WideBattleReview {
        static void Build(DefenseSimulation g,int slot,TowerKind kind) { string why; if(!g.TryBuild(slot,kind,null,out why))throw new Exception("build "+slot+": "+why); }
        static void Upgrade(DefenseSimulation g,int slot) { string why; var t=g.TowerAt(slot); if(t!=null && t.Level==1)g.TryUpgrade(t.Id,null,out why); }
        static void Fuse(DefenseSimulation g,int anchor,int partner) { string why; var a=g.TowerAt(anchor);var b=g.TowerAt(partner);if(a!=null && b!=null)g.TryFuse(0,a.Id,b.Id,anchor,null,out why); }
        static string Run(string mode,int seed) {
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle {Stage=CampaignCatalog.Stages[3]});
            int[] order;
            if(mode=="normal") {Build(g,2,TowerKind.Autocannon);Build(g,6,TowerKind.Autocannon);Upgrade(g,2);Upgrade(g,6);order=new[]{3,0,4,8,10,7,1,9,11,5};}
            else if(mode=="spread") {Build(g,2,TowerKind.Autocannon);Build(g,8,TowerKind.Autocannon);Build(g,4,TowerKind.Binding);order=new[]{6,3,10,7,0,1,9,11,5};}
            else {Build(g,2,TowerKind.Autocannon);Build(g,6,TowerKind.Binding);Upgrade(g,2);Upgrade(g,6);order=new[]{4,8,3,10,7,0,1,9,11,5};}
            g.Start();int peak=g.Towers.Count, next=0;float nextReport=30;var laneHits=new int[3];var reports=new List<string>();var cooldown=new Dictionary<int,float>();
            for(int frame=0;frame<5000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard)g.ChooseCard(g.CardOffers.Contains(CardKind.Credits)?CardKind.Credits:g.CardOffers[0]);
                if(mode=="fusion" && g.Telemetry.Fusions==0 && g.Cores>0 && g.Credits>=40)Fuse(g,2,6);
                if(mode!="normal")foreach(var t in g.Towers.ToArray())if(t.Level==1 && g.Credits>=TowerCatalog.Get(t.Kind).UpgradeCost && TowerCatalog.Get(t.Kind).UpgradeCost>0 && (mode!="fusion" || g.Telemetry.Fusions>0))Upgrade(g,t.Socket);
                if((mode!="fusion" || g.Telemetry.Fusions>0) && next<order.Length && g.Credits>=40 && g.TowerAt(order[next])==null) { Build(g,order[next],TowerKind.Autocannon);next++; }
                peak=Math.Max(peak,g.Towers.Count);
                cooldown.Clear();foreach(var e in g.Enemies)cooldown[e.Id]=e.AttackCooldown;
                g.Tick(.05f,new V2());
                foreach(var e in g.Enemies)if(cooldown.ContainsKey(e.Id) && cooldown[e.Id]<=.05f && e.AttackCooldown>.5f && e.BlockOwner==0 && V2.Distance(e.Position,new V2())<2.3f)laneHits[e.Lane]++;
                g.Events.Clear();
                if(g.Elapsed>=nextReport || g.State!=RunState.Running) {reports.Add(String.Format("t={0:F1} core={1:F0} player={2:F0} towers={3} credits={4} catalyst={5} kills={6}",g.Elapsed,g.CoreHealth,g.PlayerHealth,g.Towers.Count,g.Credits,g.Cores,g.Kills));nextReport+=30;}
            }
            return String.Format("{0} seed={1} result={2} maxTowers={3} spent={4} fusionAt={5:F1} coreAttacksByLane(W,N,E)={6}/{7}/{8}\n  {9}\n  purchases: {10}",mode,seed,g.State,peak,g.Telemetry.CreditsSpent,g.Telemetry.FirstFusion,laneHits[0],laneHits[1],laneHits[2],String.Join(" | ",reports),String.Join("; ",g.Telemetry.Purchases));
        }
        public static string RunAll(){var lines=new List<string>();foreach(int seed in new[]{7,19,41})foreach(string mode in new[]{"normal","spread","fusion"})lines.Add(Run(mode,seed));return String.Join("\n",lines);}
    }
}
