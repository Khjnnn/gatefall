using System;
using System.Linq;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class AccountGrowthChecks {
        static void Assert(bool v,string why){if(!v)throw new Exception(why);}
        static CampaignProfile Cleared(int count){var p=new CampaignProfile();for(int i=0;i<count;i++){p.BestStars[i]=3;p.FirstCleared[i]=true;p.MagicCores+=CampaignCatalog.Stages[i].FirstReward;}return p;}
        public static CampaignProfile Recommended(int stage,int hunter=0,int below=0){
            var service=new CampaignService(Cleared(stage),null);Assert(service.SelectHunter(hunter),"Recommended hunter unavailable");
            int wanted=Math.Max(0,CampaignCatalog.RecommendedHunterGrowth(stage,hunter)-below);
            while(service.Profile.Growth[hunter]<wanted)Assert(service.Grow(hunter),"Recommended growth unaffordable/gated at1-"+(stage+1)+" "+service.Error);
            for(int line=0;line<4;line++){
                int rank=Math.Max(0,CampaignCatalog.Stages[stage].RecommendedResearch[line]-below);
                while(service.Profile.Research[line]<rank)Assert(service.Research(line),"Recommended research unaffordable/gated at1-"+(stage+1)+" "+service.Error);
            }
            return service.Profile.Copy();
        }
        public static string Run(){
            Assert(CampaignCatalog.ResearchCosts.SequenceEqual(new[]{2,4,6}) && CampaignCatalog.GrowthCosts.SequenceEqual(new[]{4,6,8,12,18,26}),"Historical prices changed");
            for(int hunter=0;hunter<3;hunter++)for(int growth=0;growth<6-CampaignCatalog.HunterStart[hunter];growth++){
                int gate=Math.Max(CampaignCatalog.GrowthUnlocks[hunter][growth],hunter==1?3:hunter==2?6:0);
                if(gate>0){var before=Cleared(gate-1);before.MagicCores=1000;before.Growth[hunter]=growth;var svc=new CampaignService(before,null);Assert(!svc.Grow(hunter) && svc.Profile.MagicCores==1000 && svc.Profile.Growth[hunter]==growth,"Growth bypassed milestone");}
                var after=Cleared(gate);after.MagicCores=1000;after.Growth[hunter]=growth;var open=new CampaignService(after,null);
                Assert(open.Grow(hunter) && open.Profile.Growth[hunter]==growth+1 && open.Profile.MagicCores==1000-CampaignCatalog.GrowthCosts[CampaignCatalog.HunterStart[hunter]+growth],"Milestone growth cost");
            }
            for(int stage=0;stage<14;stage++)for(int hunter=0;hunter<3;hunter++){
                if(!Cleared(stage).IsHunterOpen(hunter))continue;
                var p=Recommended(stage,hunter);Assert(p.Valid() && p.MagicCores>=0,"Recommended profile invalid");
                var round=CampaignStore.Decode(CampaignStore.Encode(p));Assert(round.Growth.SequenceEqual(p.Growth) && round.Research.SequenceEqual(p.Research),"Recommended v2 roundtrip");
            }
            var legacy=Cleared(6);legacy.MagicCores=100;legacy.Research=new[]{2,2,2,2};var refunds=new CampaignService(legacy,null);
            Assert(refunds.ResearchInvestment()==24 && refunds.Respec() && refunds.Profile.MagicCores==124 && refunds.Respec() && refunds.Profile.MagicCores==124,"Historical refund changed or minted");
            for(int i=0;i<4;i++)Assert(refunds.Research(i) && refunds.Research(i),"Rebuy legacy research");Assert(refunds.Profile.MagicCores==100,"Historical rebuy differs");
            var locked=Cleared(9);locked.Research[0]=2;var gateResearch=new CampaignService(locked,null);Assert(!gateResearch.Research(0),"Research3 opened before1-10");
            locked=Cleared(10);locked.Research[0]=2;gateResearch=new CampaignService(locked,null);int cores=locked.MagicCores;Assert(gateResearch.Research(0) && gateResearch.Profile.MagicCores==cores-6,"Research3 milestone/cost");
            var max=new CampaignService(Cleared(14),null);foreach(int hunter in new[]{0,1,2})while(max.Grow(hunter)){}for(int line=0;line<4;line++)while(max.Research(line)){}
            Assert(max.Profile.Growth.SequenceEqual(new[]{6,3,2}) && max.Profile.Research.All(r=>r==3) && max.Profile.MagicCores==100,"Implemented growth sink not222/322");
            int total=max.Profile.MagicCores;Assert(max.Respec() && max.Profile.MagicCores==total+48 && max.Respec() && max.Profile.MagicCores==total+48,"Full research refund mint");
            var battle=new CampaignBattle{Stage=CampaignCatalog.Stages[13],Research=new[]{3,2,2,1},EarnedGrowth=4};var g=new DefenseSimulation(new GameBalance(),7,battle);g.Cards[(int)CardKind.Ballistic]=1;
            float raw=64*CampaignCatalog.Stages[13].HybridDamage;Assert(Math.Abs(g.Stats(TowerKind.Shatter,1).Damage-raw*(1+.15f+.24f+.12f))<.002f,"Research/card lines multiplied or double applied");
            return "PASS account growth: all hunter milestones, affordable recommendations for every available hunter/stage, v2, historical/full refunds, research3 and222/322 investment";
        }
    }
}
