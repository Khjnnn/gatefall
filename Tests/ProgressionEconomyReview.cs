using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Gatefall.Core;
namespace Gatefall.Tests {
    // Economic transaction fixtures: terminal states are injected, never evidence of battle wins.
    public static class ProgressionEconomyReview {
        public sealed class Step {
            public string Policy; public int Stars,Stage,Hunter,WalletBefore,Spent,WalletAfter,Reward;
            public int[] Growth,Research; public bool RecommendationMet;
        }
        public sealed class Audit {
            public List<Step> Steps=new List<Step>(); public int CompletedPaths,Transactions;
            public int EarlyOvergrowthCost,EarlyResearchDeficit,RepeatReward,RecoveryWins;
            public bool RefundExact,FailedPurchaseAtomic;
        }
        static void Check(bool value,string why){if(!value)throw new Exception(why);}
        static string Bytes(CampaignProfile p){return Convert.ToBase64String(CampaignStore.Encode(p));}
        static int Finish(CampaignService service,int stage,int stars) {
            string id;Check(service.Begin(stage,false,out id),"Cannot begin transaction fixture");
            var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,service.Battle(stage,false));
            // Bypass combat only for reward/profile accounting, including the boss transaction.
            typeof(DefenseSimulation).GetProperty("MinimumCoreHealth",BindingFlags.Public|BindingFlags.Instance).SetValue(g,stars==3?300f:1f);
            typeof(DefenseSimulation).GetProperty("State",BindingFlags.Public|BindingFlags.Instance).SetValue(g,RunState.Won);
            int reward;Check(service.Complete(id,g,out reward),"Completion transaction failed");
            string after=Bytes(service.Profile);int duplicate;
            Check(service.Complete(id,g,out duplicate) && duplicate==reward && after==Bytes(service.Profile),"Duplicate economic outcome changed profile");
            Check(service.Profile.BestStars[stage]==stars,"Injected star outcome differs");return reward;
        }
        public static Audit Run() {
            var result=new Audit();
            foreach(string policy in new[]{"Kang","SeheeWhenOpen","DohaWhenOpen","Rotate"})foreach(int stars in new[]{1,3}) {
                var service=new CampaignService(new CampaignProfile(),null);int received=0,spent=0;
                for(int stage=0;stage<14;stage++) {
                    int hunter=policy=="SeheeWhenOpen" && stage>=3?1:policy=="DohaWhenOpen" && stage>=6?2:0;
                    if(policy=="Rotate")hunter=stage>=6?stage%3:stage>=3?stage%2:0;
                    Check(service.SelectHunter(hunter),"Policy selected locked hunter");
                    int before=service.Profile.MagicCores,rank=CampaignCatalog.RecommendedHunterGrowth(stage,hunter);
                    while(service.Profile.Growth[hunter]<rank)Check(service.Grow(hunter),"Continuous growth unaffordable: "+policy+"1-"+(stage+1));
                    for(int line=0;line<4;line++)while(service.Profile.Research[line]<CampaignCatalog.Stages[stage].RecommendedResearch[line])
                        Check(service.Research(line),"Continuous research unaffordable: "+policy+"1-"+(stage+1));
                    int purchase=before-service.Profile.MagicCores;spent+=purchase;
                    Check(service.Profile.MagicCores==received-spent,"Ledger mismatch before reward");
                    var step=new Step{Policy=policy,Stars=stars,Stage=stage+1,Hunter=hunter,WalletBefore=before,Spent=purchase,WalletAfter=service.Profile.MagicCores,Growth=(int[])service.Profile.Growth.Clone(),Research=(int[])service.Profile.Research.Clone(),RecommendationMet=true};
                    step.Reward=Finish(service,stage,stars);received+=step.Reward;
                    Check(step.Reward==CampaignCatalog.Stages[stage].FirstReward && service.Profile.MagicCores==received-spent,"First-clear reward/ledger differs");
                    service=new CampaignService(CampaignStore.Decode(CampaignStore.Encode(service.Profile)),null);
                    Check(service.Profile.Valid() && service.Profile.PendingRun=="" && !service.Profile.PendingCrisis,"Roundtrip/pending invalid");
                    result.Steps.Add(step);result.Transactions++;
                }
                Check(received==322,"First clear total differs");result.CompletedPaths++;
            }
            // An optional second early hunter growth uses the first10 cores, leaving research unfunded.
            var early=new CampaignService(new CampaignProfile(),null);Finish(early,0,3);
            int initial=early.Profile.MagicCores;Check(early.Grow(0) && early.Grow(0),"Early overgrowth unavailable");
            result.EarlyOvergrowthCost=initial-early.Profile.MagicCores;
            result.EarlyResearchDeficit=CampaignCatalog.ResearchCosts[0]*2-early.Profile.MagicCores;
            string failed=Bytes(early.Profile);Check(!early.Research(0) && Bytes(early.Profile)==failed,"Unfunded research was not atomic");result.FailedPurchaseAtomic=true;
            result.RepeatReward=early.Profile.VictoryReward(0,false);result.RecoveryWins=(result.EarlyResearchDeficit+result.RepeatReward-1)/result.RepeatReward;
            for(int i=0;i<result.RecoveryWins;i++)Check(Finish(early,0,3)==2,"Recovery repeat reward differs");
            Check(early.Research(0) && early.Research(1),"Two hypothetical repeat rewards did not fund missing research");
            int paid=early.ResearchInvestment(),wallet=early.Profile.MagicCores;
            Check(early.Respec() && early.Profile.MagicCores==wallet+paid && early.Profile.Growth[0]==2,"Research refund changed growth or lost currency");
            string refunded=Bytes(early.Profile);Check(early.Respec() && Bytes(early.Profile)==refunded,"Repeated refund minted currency");
            Check(early.Research(0) && early.Research(1) && early.Profile.MagicCores==wallet,"Refund/rebuy ledger differs");result.RefundExact=true;
            return result;
        }
    }
}
