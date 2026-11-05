using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class Stage13InvestmentReview {
        static bool Execute(DefenseSimulation g,Stage13MarkerPositionReview.Action a,out string why) {
            var t=g.TowerAt(a.Site);
            if(a.Command=="build")return g.TryBuild(a.Site,a.Kind,null,out why);
            if(a.Command=="upgrade") {
                if(t==null || t.Kind!=a.Kind)throw new Exception("Missing shared upgrade material");
                return g.TryUpgrade(t.Id,null,out why);
            }
            var partner=g.TowerAt(16);
            if(a.Command!="fusion" || a.Site!=17 || t==null || partner==null)throw new Exception("Unexpected branch action");
            return g.TryFuse(0,t.Id,partner.Id,17,null,out why);
        }
        public static Stage13MarkerPositionReview.Result Run(int seed,string plan,Stage13MarkerPositionReview.Action[] native) {
            if(plan!="hold425" && plan!="spear465" && plan!="arc465" && plan!="north465" && plan!="west465")throw new Exception("Unknown purchase branch");
            var actions=new List<Stage13MarkerPositionReview.Action>();
            for(int i=0;i<10;i++)actions.Add(native[i]);
            if(native[10].Command!="fusion" || native[10].Site!=17 || Math.Abs(native[10].Time-71.5)>.001)throw new Exception("Shared native prefix differs");
            if(plan=="arc465")actions.Add(native[10]);
            if(plan=="spear465" || plan=="north465" || plan=="west465")actions.Add(new Stage13MarkerPositionReview.Action {Time=71.5,Command="build",Kind=TowerKind.Autocannon,Site=plan=="north465"?13:plan=="west465"?9:23});
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle {Stage=CampaignCatalog.Stages[12],Loadout=263,MarkerUnlocked=true,ObserverUnlocked=false});
            var audit=new StageThreatObservation(g.BoardSockets.Length);var cards=new List<string>();
            int n=0;double delay=0;string why;
            while(n<actions.Count && actions[n].Time==0) {if(!Execute(g,actions[n],out why))throw new Exception("Preparation: "+why);n++;}
            if(g.Credits!=5 || g.Towers.Count!=7)throw new Exception("Shared initial315 differs");
            g.Start();
            for(int frame=0;frame<6500 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard) {
                    CardKind desired=cards.Count==0?CardKind.Blast:CardKind.Range;
                    CardKind pick=g.CardOffers.Contains(desired)?desired:g.CardOffers[0];
                    if(!g.ChooseCard(pick))throw new Exception("Card selection failed");cards.Add(pick.ToString());
                }
                if(n<actions.Count && g.Elapsed+.00001>=actions[n].Time && Execute(g,actions[n],out why)) {delay=Math.Max(delay,g.Elapsed-actions[n].Time);n++;}
                g.Tick(.05f,new V2());audit.Sample(g,.05f);g.Events.Clear();
            }
            return new Stage13MarkerPositionReview.Result {Seed=seed,MarkerSite=14,CompletedActions=n,Spent=g.Telemetry.CreditsSpent,Credits=g.Credits,
                Towers=g.Towers.Count,Kills=g.Kills,Spawned=g.Telemetry.Spawned,Remaining=g.Enemies.Count,Time=g.Elapsed,MaximumPurchaseDelay=delay,
                Core=g.MinimumCoreHealth,Hunter=g.PlayerHealth,State=g.State.ToString(),AllSpawned=g.AllWavesSpawned,Cards=cards.ToArray(),
                Purchases=g.Telemetry.Purchases.ToArray(),KindDamage=(float[])g.Telemetry.TowerDamage.Clone(),MarkedHits=g.Telemetry.MarkedHits,
                MarkBonus=g.Telemetry.MarkBonusDamage,Observation=audit};
        }
    }
}
