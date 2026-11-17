using System;

namespace Gatefall.Core
{
    public sealed partial class CampaignService
    {
        CampaignProfile DailyCopy()
        {var p=Profile.Copy();HeartRules.Refresh(p,utcClock(),localDayClock());return p;}
        bool DailyFailure(string reason){Error=reason;return false;}
        bool DailyCommit(CampaignProfile p)
        {if(!p.Valid())return DailyFailure("보유 한도 초과");return Commit(p);}
        // Detached, refreshed preview; a query never saves or consumes a reward.
        public DailyState DailyStatus(){return DailyCopy().Daily.Copy();}
        public bool ClaimDailyMission(DailyMission mission)
        {
            int i=(int)mission;if(i<0 || i>=6)return DailyFailure("없는 일일 임무입니다.");
            var p=DailyCopy();var d=p.Daily;var offer=DailyCatalog.Missions()[i];
            if((d.MissionClaims&(1<<i))!=0)return DailyFailure("이미 받은 임무 보상입니다.");
            if(d.Progress[i]<offer.Target)return DailyFailure("임무를 먼저 완료하세요.");
            if(!DailyRules.Grant(p,new DailyReward(DailyRewardKind.MagicCores,offer.MagicCores)))return DailyFailure("마핵 보유 한도");
            d.MissionClaims|=1<<i;DailyRules.Points(p,offer.PassPoints);
            if(d.MissionClaims==63 && !DailyRules.Grant(p,new DailyReward(DailyRewardKind.NormalTicket,1)))return DailyFailure("이용권 보유 한도");
            return DailyCommit(p);
        }
        public bool ClaimAttendance()
        {
            var p=DailyCopy();var d=p.Daily;if(d.AttendanceDay>=d.LocalDay)return DailyFailure("오늘 출석 보상을 받았습니다.");
            if(!DailyRules.Grant(p,DailyCatalog.Attendance()[d.AttendancePosition]))return DailyFailure("보상 보유 한도");
            d.AttendanceDay=d.LocalDay;d.AttendancePosition=(d.AttendancePosition+1)%7;return DailyCommit(p);
        }
        public string SpinReason()
        {long now=utcClock();var p=Profile.Copy();HeartRules.Refresh(p,now,localDayClock());return SpinReason(p,now);}
        static string SpinReason(CampaignProfile p,long now)
        {
            if(p.Daily.RouletteSpins>=DailyCatalog.RouletteDailyLimit)return "룰렛 일일 한도";
            if(p.Daily.LastSpinUtc>=0 && (now<p.Daily.LastSpinUtc || now-p.Daily.LastSpinUtc<DailyCatalog.RouletteCooldownSeconds))return "룰렛 대기 시간";
            return "";
        }
        // Called by the UI only after the rewarded ad callback.
        public RouletteSlot Spin(Random rng)
        {if(rng==null){Error="난수가 필요합니다.";return null;}return Spin(rng.Next);}
        public RouletteSlot Spin(Func<int,int> rng)
        {
            long now=utcClock();var p=Profile.Copy();HeartRules.Refresh(p,now,localDayClock());Error=SpinReason(p,now);if(Error!="")return null;
            RouletteSlot slot;try{slot=DailyCatalog.Roll(rng);}catch(Exception e){Error="룰렛 추첨 실패: "+e.Message;return null;}
            if(!DailyRules.Grant(p,slot.Reward)){Error="보상 보유 한도";return null;}
            p.Daily.RouletteSpins++;p.Daily.LastSpinUtc=now;DailyRules.Progress(p,DailyMission.Ad,1);return DailyCommit(p) ? slot : null;
        }
        public bool DoubleLastReward()
        {
            var p=DailyCopy();var d=p.Daily;
            if(p.LastRewardRun=="" || p.LastReward<=0)return DailyFailure("두 배로 받을 승리 보상이 없습니다.");
            if(d.DoubledRun==p.LastRewardRun)return DailyFailure("이미 두 배로 받은 보상입니다.");
            if(d.RewardDoubles>=DailyCatalog.DoubleDailyLimit)return DailyFailure("승리 보상 광고 일일 한도");
            if(!DailyRules.Grant(p,new DailyReward(DailyRewardKind.MagicCores,p.LastReward)))return DailyFailure("마핵 보유 한도");
            d.DoubledRun=p.LastRewardRun;d.RewardDoubles++;DailyRules.Progress(p,DailyMission.Ad,1);return DailyCommit(p);
        }
        public bool SpendCrystalsToRaiseDailyCap()
        {
            var p=DailyCopy();if(p.Daily.CapRaised)return DailyFailure("오늘 점수 한도를 이미 늘렸습니다.");
            if(!HeartRules.SpendCrystals(p,DailyCatalog.CapCrystalPrice))return DailyFailure("결정 부족");
            p.Daily.CapRaised=true;return DailyCommit(p);
        }
        public bool ClaimPassTier(int tier,bool paid)
        {
            if(tier<1 || tier>DailyCatalog.TierCount)return DailyFailure("없는 패스 단계입니다.");
            var p=DailyCopy();var d=p.Daily;int bit=1<<(tier-1);
            if(d.PassPoints<tier*DailyCatalog.PointsPerTier)return DailyFailure("패스 점수가 부족합니다.");
            if(paid && !d.PassPurchased)return DailyFailure("유료 패스를 먼저 구매하세요.");
            if(((paid ? d.PaidClaims : d.FreeClaims)&bit)!=0)return DailyFailure("이미 받은 패스 보상입니다.");
            var row=DailyCatalog.PassRewards()[tier-1];if(!DailyRules.Grant(p,paid ? row.Paid : row.Free))return DailyFailure("보상 보유 한도");
            if(paid)d.PaidClaims|=bit;else d.FreeClaims|=bit;return DailyCommit(p);
        }
        public bool GrantPassPurchase(string orderId)
        {return GrantPurchase(orderId,0,true);}
        public bool GrantCrystalPurchase(string orderId,int amount)
        {if(amount<=0)return DailyFailure("잘못된 결정 수량입니다.");return GrantPurchase(orderId,amount,false);}
        bool GrantPurchase(string orderId,int amount,bool pass)
        {
            if(!DailyState.ValidOrderId(orderId))return DailyFailure("잘못된 주문 번호입니다.");
            if(Profile.Daily.PurchaseLedger.ContainsKey(orderId)){Error="";return true;}
            var p=DailyCopy();if(p.Daily.PurchaseLedger.Count>=4096)return DailyFailure("주문 기록 보유 한도");
            if(!pass){if(p.CrystalsPaid>int.MaxValue-amount)return DailyFailure("결정 보유 한도");p.CrystalsPaid+=amount;}
            p.Daily.PurchaseLedger.Add(orderId,new PurchaseEntry(orderId,pass ? PurchaseKind.Pass : PurchaseKind.Crystals,amount,pass ? p.Daily.Season : 0));return DailyCommit(p);
        }
    }
}
