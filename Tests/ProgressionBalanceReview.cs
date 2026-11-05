using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class ProgressionBalanceReview {
        public sealed class Row {
            public int Stage,Seed,Limit,Budget,Spent,Credits,AccountSpent,Growth,Towers,Fusions,Stars,FiredCurrent,FiredLifetime,PeakRecent3,ParticipationSamples;
            public int[] Research,Cards;public string Profile,Policy,State;public float Core;public double End,MeanRecent3,ParticipationEnd;
            public string[] Ledger;
        }
        public static Row[] Run(int[] stages,int[] limits,string[] profiles,int[] budgets=null,float lateSwarmHealth=0){
            foreach(var stage in CampaignCatalog.Stages)BattlefieldLayouts.Get(stage);
            var plans=new[]{
                new CompetentPlanReview.Plan{Spread=.5f,Cap=1,Upgrade=1},
                new CompetentPlanReview.Plan{Rupture=.25f,Binding=.15f,Spread=.5f,Cap=1,Upgrade=2},
                new CompetentPlanReview.Plan{Rupture=.25f,Binding=.15f,Spread=.5f,Cap=1,Upgrade=2,Fuse=true},
                new CompetentPlanReview.Plan{Rupture=.4f,Spread=.5f,Cap=1,Upgrade=2,Fuse=true}
            };
            var output=new List<Row>();
            foreach(int limit in limits){
                var old=CampaignCatalog.Stages.Select(s=>s.TowerLimit).ToArray();
                var oldHealth=CampaignCatalog.Stages.Select(s=>s.SwarmHealth).ToArray();
                if(limit>0)foreach(var s in CampaignCatalog.Stages)if(s.Index>=6)s.TowerLimit=limit;
                if(lateSwarmHealth>0)foreach(var s in CampaignCatalog.Stages)if(s.Index>=10)s.SwarmHealth=lateSwarmHealth;
                try{
                    var jobs=new List<Tuple<int,int,int,string,int>>();
                    foreach(int stage in stages)foreach(int seed in new[]{7,19,41})foreach(string profile in profiles)for(int p=0;p<plans.Length;p++)
                        foreach(int budget in budgets ?? new[]{stage<9?880:1080})jobs.Add(Tuple.Create(stage,seed,p,profile,budget));
                    var rows=new Row[jobs.Count];
                    Parallel.For(0,jobs.Count,new ParallelOptions{MaxDegreeOfParallelism=4},i=>{
                        var j=jobs[i];int stage=j.Item1;var p=j.Item4=="zero"?new CampaignProfile():AccountGrowthChecks.Recommended(stage,0,j.Item4=="below"?1:0);
                        CampaignBattle rules=j.Item4=="zero"?new CampaignBattle{Stage=CampaignCatalog.Stages[stage]}:new CampaignService(p,null).Battle(stage,false);
                        var cooldowns=new Dictionary<int,float>();var fired=new HashSet<int>();var last=new Dictionary<int,double>();double total=0,end=0;int samples=0,peak=0;
                        Action<DefenseSimulation> observe=g=>{
                            foreach(var t in g.Towers){float previous;if(cooldowns.TryGetValue(t.Id,out previous) && t.Kind!=TowerKind.Barrier && t.Commission<=0 && t.Cooldown>previous+.0001f){fired.Add(t.Id);last[t.Id]=g.Elapsed;}cooldowns[t.Id]=t.Cooldown;}
                            if(g.Elapsed>=60 && g.Elapsed<=170){int recent=g.Towers.Count(t=>last.ContainsKey(t.Id) && last[t.Id]>=g.Elapsed-3);total+=recent;samples++;peak=Math.Max(peak,recent);end=g.Elapsed;}
                        };
                        var game=CompetentPlanReview.Play(stage,j.Item2,plans[j.Item3],false,j.Item5,rules,observe);
                        if(game.Telemetry.CreditsSpent>j.Item5)throw new Exception("Spending cap exceeded");
                        int income=CampaignCatalog.Stages.Take(stage).Sum(s=>s.FirstReward);
                        rows[i]=new Row{Stage=stage+1,Seed=j.Item2,Limit=game.TowerLimit,Budget=j.Item5,Spent=game.Telemetry.CreditsSpent,Credits=game.Credits,
                            AccountSpent=j.Item4=="zero"?0:income-p.MagicCores,Growth=rules.EarnedGrowth,Research=rules.Research,Cards=game.Cards,
                            Towers=game.Towers.Count,Fusions=game.Telemetry.Fusions,Stars=CampaignCatalog.Stars(game.State==RunState.Won,game.MinimumCoreHealth,game.Balance.CoreHealth),
                            FiredCurrent=game.Towers.Count(t=>fired.Contains(t.Id)),FiredLifetime=fired.Count,PeakRecent3=peak,ParticipationSamples=samples,ParticipationEnd=end,
                            MeanRecent3=samples==0?0:total/samples,Profile=j.Item4,Policy=plans[j.Item3].ToString(),State=game.State.ToString(),Core=game.MinimumCoreHealth,End=game.Elapsed,
                            Ledger=game.Telemetry.Purchases.ToArray()};
                    });output.AddRange(rows);
                }finally{for(int i=0;i<old.Length;i++){CampaignCatalog.Stages[i].TowerLimit=old[i];CampaignCatalog.Stages[i].SwarmHealth=oldHealth[i];}}
            }return output.ToArray();
        }
    }
}
