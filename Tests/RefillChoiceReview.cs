using System;
using System.Collections.Generic;
using System.Reflection;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Analysis-only observer. Its caller is inserted into a verified copy, never product code.
    public static class RefillImpactObserver {
        public static int TowerId=-1, Hits;
        public static float Damage, StrongDamage, RunnerDamage;
        public static void Reset(){TowerId=-1;Hits=0;Damage=StrongDamage=RunnerDamage=0;}
        public static void Record(int source,float damage,EnemyKind kind){if(TowerId<0 || source!=TowerId)return;Hits++;Damage+=damage;if(kind==EnemyKind.Brute || kind==EnemyKind.Siege || kind==EnemyKind.Boss)StrongDamage+=damage;if(kind==EnemyKind.Runner)RunnerDamage+=damage;}
    }
    public static class RefillChoiceReview {
        static T[] Policy<T>(string name){return (T[])typeof(FusionPositionReview).GetField(name,BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);}
        static readonly float[] Times=Policy<float>("Times");static readonly int[] Sites=Policy<int>("Sites");static readonly TowerKind[] Kinds=Policy<TowerKind>("Kinds");
        public sealed class Row {
            public BindingPositionReview.Row Metrics;
            public bool SpearRefill;
            public int RefillShots, RefillHits;
            public float RefillDamage, RefillStrongDamage, RefillRunnerDamage;
            public float InitialBindingDamageAtRefill;
            public int LingeringBindingProjectilesAtRefill;
            public string[] Purchases;
        }
        static int Slot(int slot,int binding){return slot==0?11:slot==11?0:slot==6?binding:slot;}
        public static Row Run(int seed,int binding,bool spear) {
            RefillImpactObserver.Reset();
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle{Stage=CampaignCatalog.Stages[6]});
            var used=new HashSet<int>();foreach(int s in Sites)used.Add(Slot(s,6));if(used.Contains(binding))throw new Exception("Candidate collision");
            var m=new BindingPositionReview.Row{Seed=seed,BindingSocket=binding};var row=new Row{Metrics=m,SpearRefill=spear};
            var bindingIds=new HashSet<int>();var strongIds=new HashSet<int>();var runnerIds=new HashSet<int>();int n=0,card=0;bool refilled=false;string why;
            while(n<6){if(!g.TryBuild(Slot(Sites[n],binding),Kinds[n],null,out why))throw new Exception(why);n++;}g.Start();
            for(int frame=0;frame<8000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard){var desired=card==0?CardKind.Range:card==1?CardKind.Credits:CardKind.Blast;g.ChooseCard(g.CardOffers.Contains(desired)?desired:g.CardOffers[0]);card++;}
                if(n<Times.Length && g.Elapsed>=Times[n]) {
                    int slot=Slot(Sites[n],binding);bool done;
                    if(n==10)done=g.TryFuse(0,g.TowerAt(slot).Id,g.TowerAt(binding).Id,slot,null,out why);
                    else if(n==8 || n==9 || n>=18)done=g.TryUpgrade(g.TowerAt(slot).Id,null,out why);
                    else done=g.TryBuild(slot,n==11 && spear?TowerKind.Autocannon:Kinds[n],null,out why);
                    if(done){
                        m.MaxPurchaseDelay=Math.Max(m.MaxPurchaseDelay,(float)g.Elapsed-Times[n]);
                        if(n==11){refilled=true;RefillImpactObserver.TowerId=g.TowerAt(slot).Id;row.InitialBindingDamageAtRefill=g.Telemetry.TowerDamage[(int)TowerKind.Binding];foreach(var p in g.Projectiles)if(p.FromTower && p.Kind==TowerKind.Binding)row.LingeringBindingProjectilesAtRefill++;}
                        n++;
                    }
                }
                foreach(var t in g.Towers)if(t.Kind==TowerKind.Binding)bindingIds.Add(t.Id);
                foreach(var e in g.Enemies)if(e.Health>0) {
                    float own=0,other=0;foreach(var s in e.Slows)if(s.Remaining>0){if(bindingIds.Contains(s.Source))own=Math.Max(own,s.Fraction);else other=Math.Max(other,s.Fraction);}
                    if(own<=0)continue;float resistance=e.Kind==EnemyKind.Siege?.4f:e.Kind==EnemyKind.Brute?.65f:1;float marginal=Math.Max(0,own-other)*resistance*.05f;
                    if(e.Kind==EnemyKind.Brute || e.Kind==EnemyKind.Siege || e.Kind==EnemyKind.Boss){strongIds.Add(e.Id);m.StrongSlowedEnemySeconds+=.05f;m.MarginalStrongSlowFractionSeconds+=marginal;}
                    if(e.Kind==EnemyKind.Runner){runnerIds.Add(e.Id);m.RunnerSlowedEnemySeconds+=.05f;m.MarginalRunnerSlowFractionSeconds+=marginal;}
                }
                g.Tick(.05f,new V2());
                foreach(var e in g.Events) {
                    if(e.Kind==EventKind.TowerShot && e.Source==(int)TowerKind.Arc)m.ArcShots++;
                    if(e.Kind==EventKind.TowerShot && e.Source==(int)TowerKind.Binding){m.BindingShots++;if(g.Telemetry.Fusions==0)m.BindingShotsBeforeFusion++;else m.BindingShotsAfterFusion++;}
                    if(e.Kind==EventKind.Hit && e.Source==(int)TowerKind.Binding)m.BindingHits++;
                    if(refilled && e.Kind==EventKind.TowerShot && e.Source==(int)(spear?TowerKind.Autocannon:TowerKind.Binding) && V2.DistanceSquared(e.Position,g.BoardSockets[binding])<.001f)row.RefillShots++;
                }g.Events.Clear();
            }
            m.Spent=g.Telemetry.CreditsSpent;m.Towers=g.Towers.Count;m.Kills=g.Kills;m.CompletedPurchases=n;m.FinalCredits=g.Credits;m.Core=g.MinimumCoreHealth;m.Time=(float)g.Elapsed;m.FirstFusion=g.Telemetry.FirstFusion;m.State=g.State.ToString();m.BindingDamage=g.Telemetry.TowerDamage[(int)TowerKind.Binding];m.StrongSlowedDistinct=strongIds.Count;m.RunnersSlowedDistinct=runnerIds.Count;m.Cards=(int[])g.Cards.Clone();
            row.RefillHits=RefillImpactObserver.Hits;row.RefillDamage=RefillImpactObserver.Damage;row.RefillStrongDamage=RefillImpactObserver.StrongDamage;row.RefillRunnerDamage=RefillImpactObserver.RunnerDamage;row.Purchases=g.Telemetry.Purchases.ToArray();return row;
        }
        public static Row[] SpearModels(){var rows=new List<Row>();foreach(int seed in new[]{7,19,41})foreach(int socket in new[]{9,23,16})rows.Add(Run(seed,socket,true));return rows.ToArray();}
    }
}
