using System;
using Gatefall.Core;

namespace Gatefall
{
    public static class DailyLabels
    {
        public static readonly string[] Missions={"작전 3회 승리","3별 1회","조합 2회","위기 작전 1회","상자 1개 열기","광고 1회 보기"};
        public static string Reward(DailyReward r)
        {
            switch(r.Kind) {
                case DailyRewardKind.MagicCores:return "마핵 "+r.Amount;
                case DailyRewardKind.Hearts:return "하트 "+r.Amount;
                case DailyRewardKind.NormalTicket:return "일반 상자권 "+r.Amount;
                case DailyRewardKind.PremiumTicket:return "고급 상자권 "+r.Amount;
                case DailyRewardKind.FreeCrystals:return "결정 "+r.Amount;
                default:return "외형";
            }
        }
        public static bool CanGrant(CampaignProfile p,DailyReward r)
        {
            switch(r.Kind) {
                case DailyRewardKind.MagicCores:return (long)p.MagicCores+r.Amount<=100000000;
                case DailyRewardKind.Hearts:return HeartRules.MaximumHearts-p.Hearts+HeartRules.MaximumExtraHearts-p.ExtraHearts>=r.Amount;
                case DailyRewardKind.NormalTicket:return p.Daily.NormalTickets<=int.MaxValue-r.Amount;
                case DailyRewardKind.PremiumTicket:return p.Daily.PremiumTickets<=int.MaxValue-r.Amount;
                case DailyRewardKind.FreeCrystals:return p.CrystalsFree<=int.MaxValue-r.Amount;
                default:return true;
            }
        }
        public static bool MissionReady(CampaignProfile p,DailyState d,int index)
        {
            var offer=DailyCatalog.Missions()[index];int bit=1<<index;
            return (d.MissionClaims&bit)==0 && d.Progress[index]>=offer.Target &&
                CanGrant(p,new DailyReward(DailyRewardKind.MagicCores,offer.MagicCores)) &&
                ((d.MissionClaims|bit)!=63 || d.NormalTickets<int.MaxValue);
        }
        public static bool PassReady(CampaignProfile p,DailyState d,PassTier row,bool paid)
        {return d.PassPoints>=row.Tier*DailyCatalog.PointsPerTier && (!paid || d.PassPurchased) &&
            ((paid?d.PaidClaims:d.FreeClaims)&(1<<(row.Tier-1)))==0 && CanGrant(p,paid?row.Paid:row.Free);}
        public static int DaysLeft(DailyState d)
        {return DailyCatalog.SeasonDays-(d.LocalDay-DailyCatalog.SeasonEpochDay)%DailyCatalog.SeasonDays;}
        public static string AttendanceState(DailyState d,int index)
        {
            bool claimed=d.AttendanceDay>=d.LocalDay;
            int today=claimed?(d.AttendancePosition+6)%7:d.AttendancePosition;
            return index<today || claimed && index==today?"수령 완료":index==today?"오늘":"잠김";
        }
        public static int ClaimedMissions(DailyState d)
        {int n=0;for(int i=0;i<6;i++)if((d.MissionClaims&(1<<i))!=0)n++;return n;}
    }
}
