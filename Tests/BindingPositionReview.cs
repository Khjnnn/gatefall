using System;
using System.Collections.Generic;
using System.Reflection;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class BindingPositionReview {
        // Reuse the preserved purchase fixture without changing or duplicating its21 commands.
        static T[] Policy<T>(string name) {return (T[])typeof(FusionPositionReview).GetField(name,BindingFlags.Static|BindingFlags.NonPublic).GetValue(null);}
        static readonly float[] Times=Policy<float>("Times");
        static readonly int[] Sites=Policy<int>("Sites");
        static readonly TowerKind[] Kinds=Policy<TowerKind>("Kinds");
        public sealed class Row {
            public int Seed, BindingSocket, Spent, Towers, Kills, ArcShots, BindingShots, BindingShotsBeforeFusion, BindingShotsAfterFusion, BindingHits, CompletedPurchases, FinalCredits, StrongSlowedDistinct, RunnersSlowedDistinct;
            public float Core, Time, BindingDamage, StrongSlowedEnemySeconds, RunnerSlowedEnemySeconds, MarginalStrongSlowFractionSeconds, MarginalRunnerSlowFractionSeconds, MaxPurchaseDelay;
            public double FirstFusion;
            public string State;
            public int[] Cards;
        }
        static int Slot(int slot,int binding) {return slot==0?11:slot==11?0:slot==6?binding:slot;}
        static Row Run(int seed,int binding) {
            var g=new DefenseSimulation(new GameBalance(),seed,new CampaignBattle{Stage=CampaignCatalog.Stages[6]});
            var used=new HashSet<int>();foreach(int s in Sites)used.Add(Slot(s,6));
            if(binding==6 || binding<0 || binding>=g.BoardSockets.Length || used.Contains(binding))throw new Exception("Candidate collides with an existing purchase");
            string why;if(g.CanBuild(binding,TowerKind.Binding)!="")throw new Exception("Candidate not constructible");
            var row=new Row{Seed=seed,BindingSocket=binding};
            var bindingIds=new HashSet<int>();var strongIds=new HashSet<int>();var runnerIds=new HashSet<int>();int n=0,card=0;
            while(n<6){if(!g.TryBuild(Slot(Sites[n],binding),Kinds[n],null,out why))throw new Exception(why);n++;}
            g.Start();
            for(int frame=0;frame<8000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                if(g.AwaitingCard){var desired=card==0?CardKind.Range:card==1?CardKind.Credits:CardKind.Blast;if(!g.ChooseCard(g.CardOffers.Contains(desired)?desired:g.CardOffers[0]))throw new Exception("Card rejected");card++;}
                if(n<Times.Length && g.Elapsed>=Times[n]) {
                    int slot=Slot(Sites[n],binding);bool done;
                    if(n==10)done=g.TryFuse(0,g.TowerAt(slot).Id,g.TowerAt(binding).Id,slot,null,out why);
                    else if(n==8 || n==9 || n>=18)done=g.TryUpgrade(g.TowerAt(slot).Id,null,out why);
                    else done=g.TryBuild(slot,Kinds[n],null,out why);
                    if(done){row.MaxPurchaseDelay=Math.Max(row.MaxPurchaseDelay,(float)g.Elapsed-Times[n]);n++;}
                }
                foreach(var t in g.Towers)if(t.Kind==TowerKind.Binding)bindingIds.Add(t.Id);
                foreach(var e in g.Enemies)if(e.Health>0) {
                    float own=0,other=0;foreach(var s in e.Slows)if(s.Remaining>0){if(bindingIds.Contains(s.Source))own=Math.Max(own,s.Fraction);else other=Math.Max(other,s.Fraction);}
                    if(own<=0)continue;
                    float resistance=e.Kind==EnemyKind.Siege?.4f:e.Kind==EnemyKind.Brute?.65f:1;
                    float marginal=Math.Max(0,own-other)*resistance*.05f;
                    if(e.Kind==EnemyKind.Brute || e.Kind==EnemyKind.Siege || e.Kind==EnemyKind.Boss){strongIds.Add(e.Id);row.StrongSlowedEnemySeconds+=.05f;row.MarginalStrongSlowFractionSeconds+=marginal;}
                    if(e.Kind==EnemyKind.Runner){runnerIds.Add(e.Id);row.RunnerSlowedEnemySeconds+=.05f;row.MarginalRunnerSlowFractionSeconds+=marginal;}
                }
                g.Tick(.05f,new V2());
                foreach(var e in g.Events) {
                    if(e.Kind==EventKind.TowerShot && e.Source==(int)TowerKind.Arc)row.ArcShots++;
                    if(e.Kind==EventKind.TowerShot && e.Source==(int)TowerKind.Binding){row.BindingShots++;if(g.Telemetry.Fusions==0)row.BindingShotsBeforeFusion++;else row.BindingShotsAfterFusion++;}
                    if(e.Kind==EventKind.Hit && e.Source==(int)TowerKind.Binding)row.BindingHits++;
                }
                g.Events.Clear();
            }
            row.Spent=g.Telemetry.CreditsSpent;row.Towers=g.Towers.Count;row.Kills=g.Kills;row.CompletedPurchases=n;row.FinalCredits=g.Credits;row.Core=g.MinimumCoreHealth;row.Time=(float)g.Elapsed;row.FirstFusion=g.Telemetry.FirstFusion;row.State=g.State.ToString();row.BindingDamage=g.Telemetry.TowerDamage[(int)TowerKind.Binding];row.StrongSlowedDistinct=strongIds.Count;row.RunnersSlowedDistinct=runnerIds.Count;row.Cards=(int[])g.Cards.Clone();return row;
        }
        public static Row[] Models(){var rows=new List<Row>();foreach(int seed in new[]{7,19,41})foreach(int socket in new[]{9,23,16})rows.Add(Run(seed,socket));return rows.ToArray();}
    }
}
