using System;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class OwnerRuleChecks
    {
        static void Require(bool pass,string why){if(!pass)throw new Exception("Owner rules: "+why);}
        static CampaignProfile Open(int stage,int hearts)
        {
            var p=new CampaignProfile{Hearts=hearts,HeartAnchorUtc=10000,LastSeenUtc=10000};
            for(int i=0;i<stage;i++){p.FirstCleared[i]=true;p.BestStars[i]=3;}return p;
        }
        static CampaignService Service(CampaignProfile p)
        {return new CampaignService(p,null,()=>10000,()=>DailyCatalog.SeasonEpochDay);}
        static DefenseSimulation Victory(CampaignService s,int stage)
        {
            var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,s.Battle(stage,false));
            typeof(DefenseSimulation).GetProperty("State").SetValue(g,RunState.Won,null);return g;
        }
        public static string Run()
        {
            string run="";
            // A real save written by 0.33.1 on the owned emulator (season label 742 from the old 1970 epoch). 0.33.2 first
            // rejected it and opened an empty profile; it must load with progress intact and the season relabelled.
            string fixture=System.Environment.GetEnvironmentVariable("GATEFALL_FIXTURES")+"/v0331-season742.bin";
            var legacy=CampaignStore.Decode(System.IO.File.ReadAllBytes(fixture));
            Require(legacy!=null && legacy.Valid() && legacy.StarsTotal==108 && legacy.Daily.Season==DailyCatalog.SeasonNumber(legacy.Daily.LocalDay) && legacy.Daily.Season<10,
                "0.33.1 save with the 1970-epoch season label loads with progress and a relabelled season");
            for(int stage=0;stage<3;stage++)foreach(int hearts in new[]{0,1,5}) {
                var s=Service(Open(stage,hearts));
                Require(HeartRules.IsFreeLaunch(stage,false) && s.BeginReason(stage,false)=="" && s.Begin(stage,false,out run),"tutorial launch "+stage);
                Require(s.Profile.Hearts==hearts && s.Profile.ExtraHearts==0,"tutorial spends no heart "+stage);
                Require(s.Abandon(run) && s.Begin(stage,false,out run) && s.Profile.Hearts==hearts,"tutorial retry stays free "+stage);
            }
            var charged=Service(Open(3,0));
            Require(!HeartRules.IsFreeLaunch(3,false) && charged.BeginReason(3,false)=="하트 부족" && !charged.Begin(3,false,out run),"1-4 requires heart");
            charged=Service(Open(3,1));Require(charged.Begin(3,false,out run) && charged.Profile.Hearts==0,"1-4 spends one");
            charged=Service(Open(6,0));Require(!HeartRules.IsFreeLaunch(0,true) && charged.BeginReason(0,true)=="하트 부족" && !charged.Begin(0,true,out run),"crisis 1-1 requires heart");
            charged=Service(Open(6,1));Require(charged.Begin(0,true,out run) && charged.Profile.Hearts==0,"crisis 1-1 spends one");
            var clear=Service(Open(13,1));int reward;
            Require(clear.Begin(13,false,out run),"1-14 begin");int before=clear.Profile.Hearts+clear.Profile.ExtraHearts;
            Require(clear.Complete(run,Victory(clear,13),out reward) && clear.Profile.Hearts+clear.Profile.ExtraHearts==before+11,"1-14 first clear grants 11");
            Require(clear.Begin(13,false,out run),"1-14 replay begin");before=clear.Profile.Hearts+clear.Profile.ExtraHearts;
            Require(clear.Complete(run,Victory(clear,13),out reward) && clear.Profile.Hearts+clear.Profile.ExtraHearts==before,"1-14 replay grants zero");
            int e=DailyCatalog.SeasonEpochDay;
            Require(e==20773 && new DateTime(1970,1,1).AddDays(e)==new DateTime(2026,11,16) && new DateTime(2026,11,16).DayOfWeek==DayOfWeek.Monday,"season epoch Monday");
            Require(DailyCatalog.SeasonNumber(20772)==1 && DailyCatalog.SeasonNumber(20773)==1 && DailyCatalog.SeasonNumber(20800)==1 && DailyCatalog.SeasonNumber(20801)==2,"season one and two boundaries");
            foreach(int day in new[]{0,e-1,e,e+27,e+28,int.MaxValue}) {
                var d=new DailyState{LocalDay=day,Season=DailyCatalog.SeasonNumber(day)};
                Require(d.Valid(),"daily season validity "+day);d.Season++;Require(!d.Valid(),"mismatched season rejected "+day);
            }
            int maximum=DailyCatalog.SeasonNumber(int.MaxValue);
            Require(new PurchaseEntry("last",PurchaseKind.Pass,0,maximum).Valid() && !new PurchaseEntry("future",PurchaseKind.Pass,0,maximum+1).Valid(),"purchase season bound");
            Require(Gatefall.DailyLabels.DaysLeft(new DailyState{LocalDay=e-1,Season=1})==29 && Gatefall.DailyLabels.DaysLeft(new DailyState{LocalDay=0,Season=1})==e+28,"pre-epoch days until season one end");
            Require(Gatefall.DailyLabels.DaysLeft(new DailyState{LocalDay=e,Season=1})==28 && Gatefall.DailyLabels.DaysLeft(new DailyState{LocalDay=e+27,Season=1})==1 && Gatefall.DailyLabels.DaysLeft(new DailyState{LocalDay=e+28,Season=2})==28,"season days left boundaries");
            return "PASS tutorial 1-1..1-3 free at zero/full hearts and on retry; paid 1-4/crisis; 1-14 +11/replay +0; season epoch, strict validity, purchase bounds and days left";
        }
    }
}
