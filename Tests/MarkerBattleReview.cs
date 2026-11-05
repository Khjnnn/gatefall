using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Same scripted positions and action schedule; costs and tactical effects legitimately differ.
    public static class MarkerBattleReview {
        static int Closest(DefenseSimulation g,V2 p,HashSet<int> used) {
            int best=-1;float d=float.MaxValue;
            for(int i=0;i<g.BoardSockets.Length;i++) {float x=V2.Distance(g.BoardSockets[i],p);if(!used.Contains(i) && x<d){best=i;d=x;}}
            if(best>=0)used.Add(best);return best;
        }
        static string Run(int stage,string plan,int seed,bool cards) {
            var rules=new CampaignBattle {Stage=CampaignCatalog.Stages[stage],MarkerUnlocked=true,ObserverUnlocked=true,
                Loadout=(1<<(int)TowerKind.Autocannon)|(1<<(int)TowerKind.Binding)|(1<<(int)TowerKind.Rupture)|(1<<(int)TowerKind.Marker)};
            var g=new DefenseSimulation(new GameBalance {OfferCards=cards},seed,rules);
            var used=new HashSet<int>();var slots=new List<int>();
            for(int i=0;i<g.OpenLanes.Length;i++) {
                int lane=g.OpenLanes[i];var path=g.Roads[lane];int s=Closest(g,path[path.Length/2],used);if(s>=0)slots.Add(s);
                if(i==0 && plan.StartsWith("cluster_") && s>=0) {int neighbor=Closest(g,g.BoardSockets[s]+new V2(2,0),used);if(neighbor>=0)slots.Add(neighbor);}
            }
            for(int i=0;i<g.BoardSockets.Length;i++)if(used.Add(i))slots.Add(i);
            string why;int built=0;TowerKind second=plan.EndsWith("spears") ? TowerKind.Autocannon : TowerKind.Marker;
            if(g.TryBuild(slots[0],TowerKind.Autocannon,null,out why))built++;
            if(g.TryBuild(slots[1],second,null,out why))built++;
            int next=2;g.Start();double actionAt=3;
            for(int frame=0;frame<4500 && g.State!=RunState.Lost && g.State!=RunState.Won;frame++) {
                if(g.AwaitingCard)g.ChooseCard(g.CardOffers[0]);
                if(g.Elapsed>=actionAt) {
                    bool acted=false;
                    if(plan.EndsWith("fuse") && g.Telemetry.Fusions==0) {
                        var marker=g.Towers.Find(t=>t.Kind==TowerKind.Marker);var spear=g.Towers.Find(t=>t.Kind==TowerKind.Autocannon);
                        if(marker!=null && spear!=null && g.CanFuse(6,marker.Id,spear.Id,marker.Socket)=="")
                            acted=g.TryFuse(6,marker.Id,spear.Id,marker.Socket,null,out why);
                    }
                    if(!acted)foreach(var tower in g.Towers.ToArray()) {
                        if(tower.Level==1 && g.CanUpgrade(tower.Id)=="") {acted=g.TryUpgrade(tower.Id,null,out why);if(acted)break;}
                    }
                    if(!acted && next<slots.Count && g.Credits>=40) {if(g.TryBuild(slots[next],TowerKind.Autocannon,null,out why))built++;next++;}
                    actionAt=g.Elapsed+2;
                }
                g.Tick(.05f,new V2());g.Events.Clear();
            }
            var t=g.Telemetry;
            return String.Format("stage=1-{0} plan={1} seed={2} cards={3} result={4} t={5:F1} core={6:F0} hunter={7:F0} built={8} kills={9} spent={10} left={11} fusions={12} firstFusion={13:F1} spearDmg={14:F0} markerDmg={15:F0} observerDmg={16:F0} markedHits={17} bonusDmg={18:F0}",stage+1,plan,seed,cards,g.State,g.Elapsed,g.MinimumCoreHealth,g.PlayerHealth,built,g.Kills,t.CreditsSpent,g.Credits,t.Fusions,t.FirstFusion,t.TowerDamage[(int)TowerKind.Autocannon],t.TowerDamage[(int)TowerKind.Marker],t.TowerDamage[(int)TowerKind.Observer],t.MarkedHits,t.MarkBonusDamage);
        }
        public static string RunAll() {
            var lines=new List<string>();for(int stage=10;stage<=13;stage++)foreach(bool cards in new[]{false,true})
                foreach(int seed in (cards ? new[]{7,19,41} : new[]{7}))foreach(string plan in new[]{"spears","marker","fuse","cluster_spears","cluster_marker","cluster_fuse"})lines.Add(Run(stage,plan,seed,cards));
            return String.Join("\n",lines);
        }
    }
}
