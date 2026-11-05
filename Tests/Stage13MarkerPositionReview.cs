using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // A receipt-derived policy across explicit seeds, never an exact replay of the native run.
    public static class Stage13MarkerPositionReview {
        public sealed class Action { public double Time; public string Command; public TowerKind Kind; public int Site; }
        public sealed class Result {
            public int Seed, MarkerSite, CompletedActions, Spent, Credits, Towers, Kills, Spawned, Remaining;
            public double Time, MaximumPurchaseDelay;
            public float Core, Hunter;
            public string State;
            public bool AllSpawned;
            public string[] Cards, Purchases;
            public float[] KindDamage;
            public int MarkedHits;
            public float MarkBonus;
            public StageThreatObservation Observation;
        }
        static int Slot(int original,int marker) { return marker==14?original:original==14?marker:original==marker?14:original; }
        static bool Execute(DefenseSimulation g,Action a,int marker,out string why) {
            int site=Slot(a.Site,marker);var t=g.TowerAt(site);
            if(a.Command=="build")return g.TryBuild(site,a.Kind,null,out why);
            if(a.Command=="upgrade") {
                if(t==null || t.Kind!=a.Kind)throw new Exception("Missing scheduled upgrade tower at"+site);
                return g.TryUpgrade(t.Id,null,out why);
            }
            if(a.Command=="fusion") {
                var partner=g.TowerAt(16);
                if(site!=17 || t==null || partner==null)throw new Exception("Fixed Arc materials differ");
                return g.TryFuse(0,t.Id,partner.Id,17,null,out why);
            }
            throw new Exception("Unknown scheduled action");
        }
        public static Result Run(int seed,int marker,Action[] actions) {
            if(marker!=14 && marker!=7 && marker!=22)throw new Exception("Unreviewed marker position");
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle {
                Stage=CampaignCatalog.Stages[12],Loadout=263,MarkerUnlocked=true,ObserverUnlocked=false
            });
            var audit=new StageThreatObservation(g.BoardSockets.Length);var chosen=new List<string>();
            int n=0;double delay=0;string why;
            while(n<actions.Length && actions[n].Time==0) {
                if(!Execute(g,actions[n],marker,out why))throw new Exception("Preparation: "+why);n++;
            }
            if(g.Credits!=5 || g.Towers.Count!=7)throw new Exception("Initial315/7-tower fixture differs");
            g.Start();
            for(int frame=0;frame<6000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard) {
                    CardKind desired=chosen.Count==0?CardKind.Blast:CardKind.Range;
                    CardKind pick=g.CardOffers.Contains(desired)?desired:g.CardOffers[0];
                    if(!g.ChooseCard(pick))throw new Exception("Card choice failed");chosen.Add(pick.ToString());
                }
                if(n<actions.Length && g.Elapsed+.00001>=actions[n].Time && Execute(g,actions[n],marker,out why)) {
                    delay=Math.Max(delay,g.Elapsed-actions[n].Time);n++;
                }
                g.Tick(.05f,new V2());audit.Sample(g,.05f);g.Events.Clear();
            }
            return new Result {Seed=seed,MarkerSite=marker,CompletedActions=n,Spent=g.Telemetry.CreditsSpent,Credits=g.Credits,
                Towers=g.Towers.Count,Kills=g.Kills,Spawned=g.Telemetry.Spawned,Remaining=g.Enemies.Count,
                Time=g.Elapsed,MaximumPurchaseDelay=delay,Core=g.MinimumCoreHealth,Hunter=g.PlayerHealth,State=g.State.ToString(),
                AllSpawned=g.AllWavesSpawned,Cards=chosen.ToArray(),Purchases=g.Telemetry.Purchases.ToArray(),
                KindDamage=(float[])g.Telemetry.TowerDamage.Clone(),MarkedHits=g.Telemetry.MarkedHits,
                MarkBonus=g.Telemetry.MarkBonusDamage,Observation=audit};
        }
    }
}
