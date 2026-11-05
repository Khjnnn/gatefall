using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Common opening and action times; different costs and roles are reported, not normalized.
    public static class Stage5RoleReview {
        static void Build(DefenseSimulation g,int slot,TowerKind kind) {string why;if(!g.TryBuild(slot,kind,null,out why))throw new Exception("build "+slot+": "+why);}
        static void Upgrade(DefenseSimulation g,int slot) {string why;var t=g.TowerAt(slot);if(t==null || !g.TryUpgrade(t.Id,null,out why))throw new Exception("upgrade "+slot);}
        static string Run(int seed,TowerKind third,bool cards,int thirdSlot=0) {
            var g=new DefenseSimulation(new GameBalance { OfferCards=cards },seed,new CampaignBattle {Stage=CampaignCatalog.Stages[4]});
            Build(g,4,TowerKind.Autocannon);Build(g,1,TowerKind.Autocannon);g.Start();
            var later=new List<int>();foreach(int slot in new[]{6,2,0,3,5})if(slot!=thirdSlot)later.Add(slot);
            float[] times={30.5f,32.5f,60.5f,62.5f,90.5f,120.5f,150.5f};int step=0;
            for(int i=0;i<5000 && g.State!=RunState.Won && g.State!=RunState.Lost;i++) {
                if(g.AwaitingCard)g.ChooseCard(g.CardOffers[0]);
                if(step<times.Length && g.Elapsed>=times[step]) {
                    if(step==0)Build(g,thirdSlot,third);
                    else if(step==1)Upgrade(g,4);
                    else if(step==2)Build(g,later[0],TowerKind.Autocannon);
                    else if(step==3)Upgrade(g,1);
                    else if(step==4)Upgrade(g,thirdSlot);
                    else if(step==5)Build(g,later[1],TowerKind.Autocannon);
                    else Upgrade(g,later[0]);
                    step++;
                }
                g.Tick(.05f,new V2());g.Events.Clear();
            }
            return String.Format("seed={0} cards={1} third={2}@{13} result={3} time={4:F1} core={5:F1} hunter={6:F1} kills={7} towers={8} spent={9} spearDamage={10:F1} ruptureDamage={11:F1}\n  purchases: {12}",seed,cards,third,g.State,g.Elapsed,g.MinimumCoreHealth,g.PlayerHealth,g.Kills,g.Towers.Count,g.Telemetry.CreditsSpent,g.Telemetry.TowerDamage[(int)TowerKind.Autocannon],g.Telemetry.TowerDamage[(int)TowerKind.Rupture],String.Join("; ",g.Telemetry.Purchases),thirdSlot);
        }
        public static string RunAll() {var lines=new List<string>();foreach(int seed in new[]{7,19,41})foreach(bool cards in new[]{false,true})foreach(TowerKind kind in new[]{TowerKind.Autocannon,TowerKind.Rupture,TowerKind.Barrier})lines.Add(Run(seed,kind,cards));return String.Join("\n",lines);}
        public static string RunLocations() {var lines=new List<string>();foreach(int slot in new[]{0,3,6})foreach(int seed in new[]{7,19,41})foreach(bool cards in new[]{false,true})foreach(TowerKind kind in new[]{TowerKind.Autocannon,TowerKind.Rupture,TowerKind.Barrier})lines.Add(Run(seed,kind,cards,slot));return String.Join("\n",lines);}
    }
}
