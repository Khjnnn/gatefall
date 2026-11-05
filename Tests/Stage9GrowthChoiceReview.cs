using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Limited comparison on the current stage9 pressure. This never changes the product balance.
    public static class Stage9GrowthChoiceReview {
        public sealed class Result {
            public int Seed, Spent, Credits, Towers, Spawned, Kills, Remaining, Fusions, CoreDamageEvents, CoreContacts, SkillUses;
            public int FiredSites, FrequentFiringSites, PeakActive3Seconds, PeakProjectiles, PeakEnemies, CompletedActions;
            public int Supplies, SupplyCredits, CardOffers, Catalysts, StrongBindingTargets, RunnerBindingTargets;
            public float Core, Hunter, CoreDamage, Time, StrongBindingSlowSeconds, MarginalStrongBindingSlowSeconds, MarginalRunnerBindingSlowSeconds;
            public string Branch, State;
            public bool AllSpawned;
            public double FirstFusion, MaxPurchaseDelay;
            public string[] Purchases, Cards;
            public int[] SiteShots, KindShots;
            public float[] KindDamage;
        }
        static readonly int[] Opening={0,12,9,10,11,15};
        static readonly TowerKind[] OpeningKinds={TowerKind.Autocannon,TowerKind.Binding,TowerKind.Autocannon,TowerKind.Rupture,TowerKind.Autocannon,TowerKind.Autocannon};
        public static Result Run(int seed,string branch) {
            if(branch!="prepare_only" && branch!="fusion_refill" && branch!="direct_upgrades")throw new ArgumentException("Unknown branch");
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle{Stage=CampaignCatalog.Stages[8]});
            string why;
            for(int i=0;i<Opening.Length;i++)if(!g.TryBuild(Opening[i],OpeningKinds[i],null,out why))throw new Exception("Opening: "+why);
            var row=new Result{Seed=seed,Branch=branch};
            var bindingIds=new HashSet<int>();var strongIds=new HashSet<int>();var runnerIds=new HashSet<int>();var contacts=new HashSet<int>();
            var cards=new List<string>();int[] siteShots=new int[g.BoardSockets.Length],kindShots=new int[TowerCatalog.All.Length];
            double[] last=new double[siteShots.Length];for(int i=0;i<last.Length;i++)last[i]=-99;
            int action=0;double[] times={29,30,31,60,61};int desired=branch=="prepare_only"?3:5;
            g.Start();
            for(int frame=0;frame<8000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard){CardKind card=g.CardOffers[0];cards.Add(card.ToString());if(!g.ChooseCard(card))throw new Exception("Card rejected");}
                if(action<desired && g.Elapsed>=times[action]) {
                    bool done=false;
                    if(action==0)done=g.TryUpgrade(g.TowerAt(0).Id,null,out why);
                    else if(action==1)done=g.TryUpgrade(g.TowerAt(branch=="direct_upgrades"?9:12).Id,null,out why);
                    else if(action==2)done=g.TryBuild(20,TowerKind.Autocannon,null,out why);
                    else if(action==3 && branch=="fusion_refill")done=g.TryFuse(0,g.TowerAt(0).Id,g.TowerAt(12).Id,0,null,out why);
                    else if(action==4 && branch=="fusion_refill")done=g.TryBuild(12,TowerKind.Autocannon,null,out why);
                    else if(action==3)done=g.TryUpgrade(g.TowerAt(10).Id,null,out why);
                    else done=g.TryUpgrade(g.TowerAt(20).Id,null,out why);
                    if(done){row.MaxPurchaseDelay=Math.Max(row.MaxPurchaseDelay,g.Elapsed-times[action]);action++;}
                }
                foreach(Tower t in g.Towers)if(t.Kind==TowerKind.Binding)bindingIds.Add(t.Id);
                foreach(Enemy e in g.Enemies)if(e.Health>0) {
                    float own=0,other=0;
                    foreach(SlowEffect s in e.Slows)if(s.Remaining>0){if(bindingIds.Contains(s.Source))own=Math.Max(own,s.Fraction);else other=Math.Max(other,s.Fraction);}
                    if(own<=0)continue;
                    float resistance=e.Kind==EnemyKind.Siege?.4f:e.Kind==EnemyKind.Brute?.65f:1;
                    float marginal=Math.Max(0,own-other)*resistance*.05f;
                    if(e.Kind==EnemyKind.Brute || e.Kind==EnemyKind.Siege || e.Kind==EnemyKind.Boss){strongIds.Add(e.Id);row.StrongBindingSlowSeconds+=.05f;row.MarginalStrongBindingSlowSeconds+=marginal;}
                    if(e.Kind==EnemyKind.Runner){runnerIds.Add(e.Id);row.MarginalRunnerBindingSlowSeconds+=marginal;}
                }
                g.Tick(.05f,new V2());
                foreach(Enemy e in g.Enemies)if(e.Health>0 && e.BlockOwner==0 && e.Waypoint==g.Roads[e.Lane].Length-1 && V2.Distance(e.Position,g.Roads[e.Lane][e.Waypoint])<=1.14f)contacts.Add(e.Id);
                if(g.UseSkill())row.SkillUses++;
                foreach(CombatEvent e in g.Events) {
                    if(e.Kind==EventKind.CoreHurt){row.CoreDamageEvents++;row.CoreDamage+=e.Value;}
                    if(e.Kind==EventKind.TowerShot){
                        if(e.Source>=0)kindShots[e.Source]++;
                        for(int slot=0;slot<siteShots.Length;slot++)if(V2.DistanceSquared(g.BoardSockets[slot],e.Position)<.001f){siteShots[slot]++;last[slot]=g.Elapsed;break;}
                    }
                }
                int active=0,alive=0;foreach(Tower t in g.Towers)if(g.Elapsed-last[t.Socket]<=3)active++;
                foreach(Enemy e in g.Enemies)if(e.Health>0)alive++;
                row.PeakActive3Seconds=Math.Max(row.PeakActive3Seconds,active);row.PeakEnemies=Math.Max(row.PeakEnemies,alive);row.PeakProjectiles=Math.Max(row.PeakProjectiles,g.Projectiles.Count);
                g.Events.Clear();
            }
            foreach(int count in siteShots){if(count>0)row.FiredSites++;if(count>=10)row.FrequentFiringSites++;}
            row.CompletedActions=Opening.Length+action;row.Spent=g.Telemetry.CreditsSpent;row.Credits=g.Credits;row.Towers=g.Towers.Count;
            row.Spawned=g.Telemetry.Spawned;row.Kills=g.Kills;row.Remaining=g.Enemies.Count;row.Fusions=g.Telemetry.Fusions;
            row.Core=g.MinimumCoreHealth;row.Hunter=g.PlayerHealth;row.Time=(float)g.Elapsed;row.State=g.State.ToString();row.AllSpawned=g.AllWavesSpawned;
            row.FirstFusion=g.Telemetry.FirstFusion;row.Purchases=g.Telemetry.Purchases.ToArray();row.Cards=cards.ToArray();
            row.SiteShots=siteShots;row.KindShots=kindShots;row.KindDamage=(float[])g.Telemetry.TowerDamage.Clone();row.CoreContacts=contacts.Count;
            row.Supplies=g.SuppliesGranted;row.SupplyCredits=g.SuppliesGranted*g.WaveSupplyCredits;row.CardOffers=g.Telemetry.CardOffers;row.Catalysts=g.Telemetry.CoresGranted;
            row.StrongBindingTargets=strongIds.Count;row.RunnerBindingTargets=runnerIds.Count;
            return row;
        }
    }
}
