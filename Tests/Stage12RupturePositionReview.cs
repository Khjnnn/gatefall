using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Current product core, an explicit policy derived from the native receipt. Native seed/first card are unknown.
    public static class Stage12RupturePositionReview {
        public sealed class Action {
            public double Time;
            public string Command;
            public TowerKind Kind;
            public int Site;
        }
        public sealed class Result {
            public int Seed, RuptureSite, CompletedActions, Spent, Credits, Towers, Kills, Spawned, Remaining;
            public double Time, FirstFusion, MaximumPurchaseDelay;
            public float Core, Hunter;
            public string State;
            public bool AllSpawned;
            public string[] Cards, Purchases;
            public float[] KindDamage;
            public StageThreatObservation Observation;
        }
        static int Slot(int slot,int front) { return front==22?slot:slot==22?front:slot==front?22:slot; }
        static bool Execute(DefenseSimulation g,Action a,int front,out string why) {
            int site=Slot(a.Site,front);var t=g.TowerAt(site);
            if(a.Command=="build")return g.TryBuild(site,a.Kind,null,out why);
            if(a.Command=="upgrade") {
                if(t==null)throw new Exception("Missing upgrade material at"+site);
                return g.TryUpgrade(t.Id,null,out why);
            }
            if(a.Command=="fusion") {
                var partner=g.TowerAt(16);
                if(t==null || partner==null)throw new Exception("Missing fusion material");
                return g.TryFuse(0,t.Id,partner.Id,site,null,out why);
            }
            throw new Exception("Unknown scheduled action");
        }
        public static Result Run(int seed,int front,Action[] actions) {
            if(front!=22 && front!=6 && front!=10)throw new Exception("Invalid selected site");
            var rules=new CampaignBattle {Stage=CampaignCatalog.Stages[11],Loadout=263,MarkerUnlocked=true,ObserverUnlocked=false};
            var g=new DefenseSimulation(new GameBalance(),seed,rules);
            var audit=new StageThreatObservation(g.BoardSockets.Length);var chosen=new List<string>();
            int n=0;double delay=0;string why;
            while(n<actions.Length && actions[n].Time==0) {
                if(!Execute(g,actions[n],front,out why))throw new Exception("Preparation: "+why);n++;
            }
            if(g.Credits!=0 || g.Towers.Count!=6)throw new Exception("Initial320/6-tower fixture differs");
            g.Start();
            for(int frame=0;frame<6000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard) {
                    CardKind desired=chosen.Count==0?CardKind.Ballistic:CardKind.Range;
                    CardKind pick=g.CardOffers.Contains(desired)?desired:g.CardOffers[0];
                    if(!g.ChooseCard(pick))throw new Exception("Card selection failed");chosen.Add(pick.ToString());
                }
                if(n<actions.Length && g.Elapsed+.00001>=actions[n].Time && Execute(g,actions[n],front,out why)) {
                    delay=Math.Max(delay,g.Elapsed-actions[n].Time);n++;
                }
                g.Tick(.05f,new V2());audit.Sample(g,.05f);g.Events.Clear();
            }
            return new Result {Seed=seed,RuptureSite=front,CompletedActions=n,Spent=g.Telemetry.CreditsSpent,Credits=g.Credits,
                Towers=g.Towers.Count,Kills=g.Kills,Spawned=g.Telemetry.Spawned,Remaining=g.Enemies.Count,Time=g.Elapsed,
                FirstFusion=g.Telemetry.FirstFusion,MaximumPurchaseDelay=delay,Core=g.MinimumCoreHealth,Hunter=g.PlayerHealth,
                State=g.State.ToString(),AllSpawned=g.AllWavesSpawned,Cards=chosen.ToArray(),Purchases=g.Telemetry.Purchases.ToArray(),
                KindDamage=(float[])g.Telemetry.TowerDamage.Clone(),Observation=audit};
        }
    }
}
