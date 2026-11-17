using System;
using Gatefall.Core;

namespace Gatefall
{
    internal enum ShopSection { Daily, Boxes, Hearts, Crystals, Bundles }
    public sealed partial class GatefallGame
    {
        internal ShopSection ShopTab { get; private set; }
        internal bool TowerCards { get; private set; }
        internal int TowerCardFirst { get; private set; }
        internal bool ShopAdPending { get; private set; }
        internal SupplyResult BoxResult { get; private set; }
        internal bool SupplyOddsOpen { get; private set; }
        internal BoxKind SupplyOddsKind { get; private set; }
        internal int SupplyFirst { get; private set; }
        internal Func<int,int> SupplyRandom { get; set; } = new Random().Next;
        internal bool SupplyModal { get { return BoxResult!=null || SupplyOddsOpen; } }
        internal int NormalBoxesRemaining {
            get { int used;Profile.DailyLimits.NamedCounters.TryGetValue("normal-boxes",out used);return Math.Max(0,5-used); }
        }
        void OpenShop(ShopSection tab)
        {
            if(!Profile.FirstCleared[2]){CampaignError="1-3 클리어 후 해금";return;}
            ShopTab=tab;CampaignError="";SetPage(CampaignScreen.Shop);RefreshHomeHearts();
        }
        void RefreshShopAd()
        {
            if(ShopAdPending || RewardedAds==null || !RewardedAds.IsReady || Campaign.DailyShop().RefreshCount>=2)return;
            ShopAdPending=true;bool completed=false;CampaignError="";TouchInput.Reset();
            RewardedAds.Show("daily-shop",()=> {
                if(completed)return;completed=true;ShopAdPending=false;
                CampaignError=Campaign.RefreshShopByAd()?"":Campaign.Error;TouchInput.Reset();
            },()=> {if(completed)return;completed=true;ShopAdPending=false;CampaignError="광고를 완료하지 못했습니다. 다시 시도해 주세요.";TouchInput.Reset();});
        }
        // UI actions share the same transactional Core paths as the host checks.
        bool ShopAction(int action)
        {
            if(ShopAdPending)return true;
            if(SupplyModal) {
                if(action==819 || action==603){BoxResult=null;SupplyOddsOpen=false;SupplyFirst=0;TouchInput.Reset();}
                else if(SupplyOddsOpen && (action==822 || action==823)){SupplyOddsKind=action==822?BoxKind.Normal:BoxKind.Premium;SupplyFirst=0;TouchInput.Reset();}
                else if(action==820 || action==821) {
                    int count=SupplyOddsOpen?ShopLabels.OddsRows(Campaign,SupplyOddsKind).Length:ShopLabels.ResultRows(BoxResult,Profile).Length;
                    int visible=TouchInput.Layout.SupplyVisibleRows(SupplyOddsOpen);
                    SupplyFirst=Math.Max(0,Math.Min(Math.Max(0,count-visible),SupplyFirst+(action==820?-visible:visible)));TouchInput.Reset();
                }
                return true;
            }
            if(action==680 || action==684 || action==685) {
                OpenShop(action==684?ShopSection.Hearts:action==685?ShopSection.Crystals:ShopSection.Daily);homeRead.Add(action);return true;
            }
            if(action==652 && Profile.FirstCleared[2]) {
                TowerCards=!Profile.FirstCleared[9];TowerCardFirst=0;CampaignError="";SetPage(CampaignScreen.Loadout);return true;
            }
            if(ScreenPage==CampaignScreen.Loadout) {
                if(action==835 || action==836) {
                    if(action==836 && !Profile.FirstCleared[9]){CampaignError="1-10 클리어 후 해금";return true;}
                    TowerCards=action==835;CampaignError="";TouchInput.Reset();return true;
                }
                if(TowerCards && (action==837 || action==838)) {
                    TowerCardFirst=Math.Max(0,Math.Min(12,TowerCardFirst+(action==837?-6:6)));TouchInput.Reset();return true;
                }
                if(TowerCards && action>=860 && action<860+SupplyCatalog.TowerCount) {
                    CampaignError=Campaign.GradeUp((TowerKind)(action-860))?"":Campaign.Error;TouchInput.Reset();return true;
                }
            }
            if(ScreenPage!=CampaignScreen.Shop)return false;
            if(action>=800 && action<=804){ShopTab=(ShopSection)(action-800);CampaignError="";TouchInput.Reset();return true;}
            if(action>=810 && action<=815 && ShopTab==ShopSection.Daily) {
                CampaignError=Campaign.PurchaseShopSlot(action-810)?"":Campaign.Error;TouchInput.Reset();return true;
            }
            if((action==805 || action==806) && ShopTab==ShopSection.Boxes) {
                BoxResult=Campaign.OpenBox(action==805?BoxKind.Normal:BoxKind.Premium,SupplyRandom);
                CampaignError=BoxResult==null?Campaign.Error:"";SupplyFirst=0;TouchInput.Reset();return true;
            }
            if(action==807 && ShopTab==ShopSection.Boxes){SupplyOddsOpen=true;SupplyOddsKind=BoxKind.Normal;SupplyFirst=0;TouchInput.Reset();return true;}
            if(action==808 && ShopTab==ShopSection.Daily){RefreshShopAd();return true;}
            if(action==809 && ShopTab==ShopSection.Hearts){CampaignError=Campaign.RefillHeartsByCrystals()?"하트 +5":Campaign.Error;TouchInput.Reset();return true;}
            return action>=800 && action<=874;
        }
    }
}
