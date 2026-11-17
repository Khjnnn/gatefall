using System;
using Gatefall.Core;

namespace Gatefall
{
    public sealed partial class GatefallGame
    {
        internal bool DailyAdPending { get; private set; }
        internal bool RouletteDisclosureOpen { get; private set; }
        internal int DailyFirst { get; private set; }
        internal string RouletteResult { get; private set; } = "";
        internal Func<int,int> DailyRandom { get; set; } = new Random().Next;
        internal bool DailyPage { get {return ScreenPage==CampaignScreen.Roulette || ScreenPage==CampaignScreen.Missions || ScreenPage==CampaignScreen.Attendance || ScreenPage==CampaignScreen.Pass;} }
        internal int RouletteWaitSeconds {
            get {long last=Campaign.DailyStatus().LastSpinUtc,now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                return last<0?0:(int)Math.Min(int.MaxValue,Math.Max(0,(double)last-now+DailyCatalog.RouletteCooldownSeconds));}
        }
        internal bool CanDoubleReward {
            get {var d=Campaign.DailyStatus();return ScreenPage==CampaignScreen.Battle && IsCampaign && Simulation.State==RunState.Won && ResultSaved &&
                CurrentRunId!="" && CurrentRunId==Profile.LastRewardRun && Profile.LastReward>0 && d.DoubledRun!=CurrentRunId &&
                d.RewardDoubles<DailyCatalog.DoubleDailyLimit && (long)Profile.MagicCores+Profile.LastReward<=100000000 && Profile.SaveSerial<long.MaxValue && !SaveBlocked;}
        }
        internal int DisplayResultReward {
            get {return ResultReward*(CurrentRunId!="" && Campaign.DailyStatus().DoubledRun==CurrentRunId?2:1);}
        }
        internal bool HomeNotice(int action)
        {
            if(SaveBlocked || Profile.SaveSerial==long.MaxValue)return false;
            var d=Campaign.DailyStatus();
            if(action==681)return Campaign.SpinReason()=="";
            if(action==682){for(int i=0;i<6;i++)if(DailyLabels.MissionReady(Profile,d,i))return true;return false;}
            if(action==683)return d.AttendanceDay<d.LocalDay && DailyLabels.CanGrant(Profile,DailyCatalog.Attendance()[d.AttendancePosition]);
            if(action==880){foreach(var row in DailyCatalog.PassRewards())if(DailyLabels.PassReady(Profile,d,row,false) || DailyLabels.PassReady(Profile,d,row,true))return true;return false;}
            if(action==680)return Profile.FirstCleared[2] && Profile.MagicCores<=100000000-10 && Campaign.ShopPurchaseReason(5)=="";
            return false;
        }
        void ShowDailyAd(bool doubleReward)
        {
            if(DailyAdPending)return;
            if(doubleReward?!CanDoubleReward:Campaign.SpinReason()!="") {CampaignError=doubleReward?"두 배로 받을 수 없는 보상입니다.":Campaign.SpinReason();return;}
            if(RewardedAds==null || !RewardedAds.IsReady){CampaignError="광고 준비 중";return;}
            DailyAdPending=true;CampaignError="";bool completed=false;TouchInput.Reset();
            RewardedAds.Show(doubleReward?"victory-double":"ad-roulette",()=> {
                if(completed)return;completed=true;DailyAdPending=false;
                if(doubleReward)CampaignError=Campaign.DoubleLastReward()?"":Campaign.Error;
                else {var slot=Campaign.Spin(DailyRandom);CampaignError=slot==null?Campaign.Error:"";if(slot!=null)RouletteResult="획득 · "+DailyLabels.Reward(slot.Reward);}
                TouchInput.Reset();
            },()=> {if(completed)return;completed=true;DailyAdPending=false;CampaignError="광고를 완료하지 못했습니다. 다시 시도해 주세요.";TouchInput.Reset();});
        }
        bool DailyAction(int action)
        {
            if(DailyAdPending)return true;
            if(action>=681 && action<=683 || action==880) {
                SetPage(action==681?CampaignScreen.Roulette:action==682?CampaignScreen.Missions:action==683?CampaignScreen.Attendance:CampaignScreen.Pass);
                DailyFirst=0;RouletteDisclosureOpen=false;CampaignError="";return true;
            }
            if(action==887){ShowDailyAd(true);return true;}
            if(!DailyPage)return false;
            if(RouletteDisclosureOpen) {
                if(action==882 || action==603){RouletteDisclosureOpen=false;DailyFirst=0;TouchInput.Reset();return true;}
                if(action!=885 && action!=886)return true;
            }
            if(action==881 && ScreenPage==CampaignScreen.Roulette)ShowDailyAd(false);
            else if(action==882 && ScreenPage==CampaignScreen.Roulette){RouletteDisclosureOpen=true;DailyFirst=0;}
            else if(action==883 && ScreenPage==CampaignScreen.Attendance)CampaignError=Campaign.ClaimAttendance()?"":Campaign.Error;
            else if(action==884 && ScreenPage==CampaignScreen.Pass)CampaignError=Campaign.SpendCrystalsToRaiseDailyCap()?"":Campaign.Error;
            else if(action>=900 && action<906 && ScreenPage==CampaignScreen.Missions)CampaignError=Campaign.ClaimDailyMission((DailyMission)(action-900))?"":Campaign.Error;
            else if(action>=920 && action<980 && ScreenPage==CampaignScreen.Pass)CampaignError=Campaign.ClaimPassTier((action-920)%30+1,action>=950)?"":Campaign.Error;
            else if(action==885 || action==886) {
                int visible=TouchInput.Layout.DailyVisibleRows(),count=RouletteDisclosureOpen?DailyCatalog.RouletteDisclosure().Length:DailyCatalog.TierCount;
                DailyFirst=Math.Max(0,Math.Min(Math.Max(0,count-visible),DailyFirst+(action==885?-visible:visible)));
            } else return false;
            TouchInput.Reset();return true;
        }
    }
}
