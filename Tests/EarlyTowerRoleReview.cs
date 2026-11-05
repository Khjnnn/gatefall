using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Compare an identical stationary opening, not an optimal strategy or human play.
    public static class EarlyTowerRoleReview {
        static string Run(int stage, int seed, bool binding, bool cards) {
            var g=new DefenseSimulation(new GameBalance { OfferCards=cards },seed,new CampaignBattle { Stage=CampaignCatalog.Stages[stage] });
            string reason;
            if(!g.TryBuild(4,TowerKind.Autocannon,null,out reason) ||
               !g.TryBuild(1,TowerKind.Autocannon,null,out reason) ||
               !g.TryBuild(0,binding ? TowerKind.Binding : TowerKind.Autocannon,null,out reason))
                throw new Exception("opening build: " + reason);
            g.Start();
            for(int i=0;i<5000 && g.State!=RunState.Won && g.State!=RunState.Lost;i++) {
                if(g.AwaitingCard)g.ChooseCard(g.CardOffers[0]);
                g.Tick(.05f,new V2());g.Events.Clear();
            }
            return String.Format("stage=1-{0} seed={1} cards={2} opening={3} state={4} elapsed={5:F1} core={6:F1} kills={7} spent={8}",
                stage+1,seed,cards,binding?"two-spears-binding":"three-spears",g.State,g.Elapsed,g.MinimumCoreHealth,g.Kills,g.Telemetry.CreditsSpent);
        }
        public static string RunAll() {
            var lines=new List<string>();
            foreach(int stage in new[]{1,2})foreach(int seed in new[]{7,19,41})foreach(bool cards in new[]{false,true})foreach(bool binding in new[]{false,true})
                lines.Add(Run(stage,seed,binding,cards));
            return String.Join("\n",lines);
        }
    }
}
