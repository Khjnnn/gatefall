using System;

namespace Gatefall.Core
{
    public sealed partial class CampaignService
    {
        const string NormalBoxes="normal-boxes";
        static int Used(CampaignProfile p,string key)
        {int count;return p.DailyLimits.NamedCounters.TryGetValue(key,out count) ? count : 0;}
        string SupplyLock() {return Profile.FirstCleared[2] ? "" : "1-3 클리어 후 해금";}
        public string OpenBoxReason(BoxKind kind)
        {
            if(kind!=BoxKind.Normal && kind!=BoxKind.Premium)return "없는 보급 상자입니다.";
            string reason=SupplyLock();if(reason!="")return reason;
            var p=Profile.Copy();HeartRules.Refresh(p,utcClock(),localDayClock());
            if(kind==BoxKind.Normal){if(p.Daily.NormalTickets>0)return "";if(Used(p,NormalBoxes)>=5)return "일반 상자 일일 한도";return p.MagicCores<40 ? "마핵 부족" : "";}
            if(p.Daily.PremiumTickets>0)return "";
            return (long)p.CrystalsFree+p.CrystalsPaid<100 ? "결정 부족" : "";
        }
        public SupplyResult OpenBox(BoxKind kind,Random rng)
        {if(rng==null){Error="난수가 필요합니다.";return null;}return OpenBox(kind,rng.Next);}
        public SupplyResult OpenBox(BoxKind kind,Func<int,int> rng)
        {
            Error=OpenBoxReason(kind);if(Error!="")return null;
            var p=Profile.Copy();HeartRules.Refresh(p,utcClock(),localDayClock());
            if(kind==BoxKind.Normal){if(p.Daily.NormalTickets>0)p.Daily.NormalTickets--;
                else {p.MagicCores-=40;p.DailyLimits.NamedCounters[NormalBoxes]=Used(p,NormalBoxes)+1;}}
            else if(p.Daily.PremiumTickets>0)p.Daily.PremiumTickets--;
            else if(!HeartRules.SpendCrystals(p,100)){Error="결정 부족";return null;}
            SupplyResult result;
            try{result=Gatefall.Core.Supply.Open(p.Supply,kind,p.IsTowerUnlocked,p.ChapterReached,rng);p.MagicCores=checked(p.MagicCores+result.Cores);}
            catch(Exception e){Error="상자 개봉 실패: "+e.Message;return null;}
            DailyRules.Progress(p,DailyMission.Box,1);
            if(!p.Valid()){Error="보유 한도 초과";return null;}
            return Commit(p) ? result : null;
        }
        public string GradeUpReason(TowerKind kind)
        {
            string reason=SupplyLock();if(reason!="")return reason;
            if(!SupplyCatalog.IsKind(kind) || !Profile.IsTowerUnlocked(kind))return "결계주를 먼저 해금하세요.";
            return Gatefall.Core.Supply.GradeReason(Profile.Supply,kind,Profile.MagicCores);
        }
        public bool GradeUp(TowerKind kind)
        {
            Error=GradeUpReason(kind);if(Error!="")return false;
            var p=Profile.Copy();if(!Gatefall.Core.Supply.GradeUp(p.Supply,kind,ref p.MagicCores))return false;
            if(!p.Valid()){Error="보유 한도 초과";return false;}return Commit(p);
        }
        public SupplyOdds OddsTable(BoxKind kind)
        {return SupplyCatalog.OddsTable(kind,Profile.IsTowerUnlocked,Profile.Supply);}
        // Detached preview. The generated list is persisted with the first purchase or ad refresh.
        public DailyShopState DailyShop()
        {var p=DailyCopy();DailyShopState.Ensure(p,p.DailyLimits.LocalDay);return p.Shop.Copy();}
        static string ShopReason(CampaignProfile p,int slot)
        {
            if(slot<0 || slot>=6)return "없는 상점 칸입니다.";
            if((p.Shop.PurchasedMask&(1<<slot))!=0)return "이미 구매한 상품입니다.";
            if(slot==4 && p.Shop.HeartsBought>=3)return "상점 하트 일일 한도";
            if(slot==4 && p.Hearts==5 && p.ExtraHearts==999)return "하트 보관 한도";
            return p.MagicCores<p.Shop.Offers()[slot].Price ? "마핵 부족" : "";
        }
        public string ShopPurchaseReason(int slot)
        {
            string reason=SupplyLock();if(reason!="")return reason;
            var p=Profile.Copy();HeartRules.Refresh(p,utcClock(),localDayClock());DailyShopState.Ensure(p,p.DailyLimits.LocalDay);return ShopReason(p,slot);
        }
        public bool PurchaseShopSlot(int slot)
        {
            Error=SupplyLock();if(Error!="")return false;
            var p=Profile.Copy();HeartRules.Refresh(p,utcClock(),localDayClock());DailyShopState.Ensure(p,p.DailyLimits.LocalDay);
            Error=ShopReason(p,slot);if(Error!="")return false;
            var offer=p.Shop.Offers()[slot];p.MagicCores-=offer.Price;
            if(slot<4){int i=(int)offer.Tower;
                if(p.Supply.Grades[i]==6)p.MagicCores+=SupplyCatalog.SurplusCores(SupplyCatalog.Rarity(offer.Tower));
                else {if(p.Supply.Cards[i]==int.MaxValue){Error="카드 보유 한도";return false;}p.Supply.Cards[i]++;}}
            else if(slot==4){HeartRules.Grant(p,1);p.Shop.HeartsBought++;}
            else p.MagicCores+=10;
            p.Shop.PurchasedMask|=1<<slot;if(!p.Valid()){Error="보유 한도 초과";return false;}return Commit(p);
        }
        // Called only after the ad reward callback; the SDK itself stays outside Core.
        public bool RefreshShopByAd()
        {
            Error=SupplyLock();if(Error!="")return false;
            var p=Profile.Copy();HeartRules.Refresh(p,utcClock(),localDayClock());DailyShopState.Ensure(p,p.DailyLimits.LocalDay);
            if(p.Shop.RefreshCount>=2){Error="상점 갱신 일일 한도";return false;}
            p.Shop.Generate(p,p.Shop.LocalDay,p.Shop.RefreshCount+1);DailyRules.Progress(p,DailyMission.Ad,1);return Commit(p);
        }
    }
}
