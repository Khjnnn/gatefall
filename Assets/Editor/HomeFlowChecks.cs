#if GATEFALL_HOME_CHECKS
// Host-only harness: compile the real controller without loading Unity or invoking its lifecycle.
using System;
using Gatefall.Core;
namespace UnityEngine
{
    public static class Time { public static float unscaledTime; }
    public static class Application { public static string persistentDataPath=""; }
}
namespace Gatefall
{
    public enum GameMenu { None, Pause }
    public sealed partial class GatefallGame
    {
        public DefenseSimulation Simulation { get; private set; }
        public MobileInput TouchInput { get; private set; }
        public GameMenu Menu { get; private set; }
        sealed class TestSound { public bool Muted; }
        TestSound sound=new TestSound();
        void NewRun(bool start) { Simulation=new DefenseSimulation(new GameBalance(),7,activeBattle);TouchInput.Reset(); }
        void ShowNotice(string text,float seconds) { }
        void ToggleSound() { }
        static void Require(bool ok,string message) {if(!ok)throw new Exception("Home flow: "+message);}
        sealed class DeferredAd : IRewardedAdService
        {
            public bool IsReady { get { return true; } }
            public Action Reward,Fail;
            public void Show(string placement,Action rewarded,Action failed) {Reward=rewarded;Fail=failed;}
        }
        static GatefallGame Host(int hearts,int crystals=0)
        {
            long now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var p=new CampaignProfile{Hearts=hearts,CrystalsFree=crystals,LastSeenUtc=now,HeartAnchorUtc=now};
            return new GatefallGame{Campaign=new CampaignService(p,x=>{},()=>now,()=>1),
                Simulation=new DefenseSimulation(new GameBalance(),7),TouchInput=new MobileInput(new MobileLayout(960,600,new Box2()))};
        }
        public static string RunHomeFlowChecks()
        {
            var g=Host(0);g.BeginPreparation();Require(g.HeartShortage && g.ScreenPage==CampaignScreen.Home,"empty Home launch");
            var ad=new DeferredAd();g.RewardedAds=ad;g.CampaignAction(696);
            Require(g.HeartAdPending && g.Profile.DailyLimits.HeartAdsUsed==0,"reward before callback");
            g.CampaignAction(697);g.CampaignAction(698);Require(g.HeartShortage,"pending ad must capture modal");
            ad.Reward();string first=g.CurrentRunId;ad.Reward();ad.Fail();
            Require(!g.HeartShortage && g.ScreenPage==CampaignScreen.Battle && g.Profile.Hearts==0 && g.Profile.DailyLimits.HeartAdsUsed==1 && g.CurrentRunId==first,"one reward/launch per callback");
            g.BeginPreparation();Require(g.HeartShortage && g.CurrentRunId==first,"result retry shortage keeps old run");
            g.CampaignAction(698);Require(!g.HeartShortage && g.CurrentRunId==first,"wait keeps existing result/run");
            g=Host(0,30);g.ScreenPage=CampaignScreen.City;g.BeginPreparation();g.CampaignAction(697);
            Require(g.ScreenPage==CampaignScreen.Battle && g.Profile.Hearts==4 && g.Profile.CrystalsFree==0,"map crystal refill launches");
            g=Host(0);g.RewardedAds=new FakeRewardedAdService{IsReady=false};g.BeginPreparation();g.CampaignAction(696);
            Require(g.HeartShortage && !g.HeartAdPending && g.Profile.DailyLimits.HeartAdsUsed==0,"unready ad");
            g.RewardedAds=new FakeRewardedAdService{Succeeds=false};g.CampaignAction(696);
            Require(g.HeartShortage && g.Profile.Hearts==0 && g.Profile.DailyLimits.HeartAdsUsed==0,"failed ad");
            g=Host(0);var p=g.Profile.Copy();p.FirstCleared[0]=true;p.BestStars[0]=3;
            g.Campaign=new CampaignService(p,x=>{});g.ResultSaved=true;g.ScreenPage=CampaignScreen.Battle;
            g.activeBattle=g.Campaign.Battle(0,false);g.NewRun(false);g.CampaignAction(607);
            Require(g.HeartShortage && g.SelectedStage==0,"next shortage preserves result selection");
            g.CampaignAction(696);Require(g.SelectedStage==1 && g.Profile.PendingStage==1 && g.Profile.Hearts==0,"next refill launches intended operation");
            g=Host(2);g.BeginPreparation();string interrupted=g.CurrentRunId;
            g.SetPage(CampaignScreen.Home);g.BeginPreparation();
            Require(g.Profile.Hearts==0 && g.CurrentRunId!=interrupted,"interrupted attempt is not a free retry");
            g=Host(0);p=g.Profile.Copy();for(int i=0;i<6;i++){p.FirstCleared[i]=true;p.BestStars[i]=3;}
            g.Campaign=new CampaignService(p,x=>{});g.CrisisSelection=true;g.BeginPreparation();g.CampaignAction(696);
            Require(g.Profile.PendingCrisis && g.Simulation.Rules.Crisis,"crisis refill keeps mode");
            int writes=0;g=Host(1);g.Campaign=new CampaignService(g.Profile,x=>writes++);
            UnityEngine.Time.unscaledTime=0;g.RefreshHomeHearts();g.TickHomeHearts();Require(writes==1,"refresh on open only once");
            UnityEngine.Time.unscaledTime=1;g.TickHomeHearts();g.TickHomeHearts();Require(writes==1,"idle ticks must not rewrite an unchanged profile");
            g.SetPage(CampaignScreen.Growth);UnityEngine.Time.unscaledTime=2;g.TickHomeHearts();Require(writes==1,"hidden Home does not refresh");
            g=Host(0);g.Campaign=new CampaignService(g.Profile,x=>{throw new Exception("disk full");});g.BeginPreparation();g.CampaignAction(696);
            Require(g.HeartShortage && g.CurrentRunId=="" && g.Profile.DailyLimits.HeartAdsUsed==0,"failed reward save cannot launch");
            return "PASS: real CampaignController Home/map/retry/next/crisis launches, interrupted attempt charge, one-second refresh, deferred/duplicate/failed ads and failed reward saves (host stubs only).";
        }
        static GatefallGame DailyHost()
        {
            var g=ShopHost();var p=g.Profile.Copy();p.Daily=g.Campaign.DailyStatus();
            foreach(var row in DailyCatalog.Missions())p.Daily.Progress[(int)row.Mission]=row.Target;
            p.Daily.PassPoints=150;Require(p.Valid(),"daily fixture validity");
            g.Campaign=new CampaignService(p,x=>{},()=>p.LastSeenUtc,()=>1);g.DailyRandom=max=>0;return g;
        }
        static GatefallGame VictoryHost(bool won=true)
        {
            var g=ShopHost();g.BeginPreparation();
            typeof(DefenseSimulation).GetProperty("State").SetValue(g.Simulation,won?RunState.Won:RunState.Lost,null);
            g.SaveCampaignOutcome();Require(g.ResultSaved,"victory fixture saved");return g;
        }
        public static string RunDailyFlowChecks()
        {
            var g=DailyHost();g.CampaignAction(682);var expected=Expected(g);
            Require(g.ScreenPage==CampaignScreen.Missions && g.HomeNotice(682),"missions open/dot");
            for(int i=0;i<6;i++) {
                Require(expected.ClaimDailyMission((DailyMission)i),"mission Core");g.CampaignAction(900+i);Same(g,expected,"mission "+i);
                g.CampaignAction(900+i);Require(!expected.ClaimDailyMission((DailyMission)i) && g.CampaignError==expected.Error,"mission duplicate reason");Same(g,expected,"mission duplicate");
            }
            Require(g.Profile.Daily.NormalTickets==1 && !g.HomeNotice(682),"all clear ticket/dot");
            g=ShopHost();g.CampaignAction(682);expected=Expected(g);Require(!expected.ClaimDailyMission(DailyMission.Wins),"incomplete Core");
            g.CampaignAction(900);Require(g.CampaignError==expected.Error,"incomplete mission reason");Same(g,expected,"incomplete mission");
            g=DailyHost();g.CampaignAction(683);expected=Expected(g);Require(g.HomeNotice(683) && expected.ClaimAttendance(),"attendance Core");
            g.CampaignAction(883);Same(g,expected,"attendance");Require(!g.HomeNotice(683),"attendance dot clears on claim");
            g.CampaignAction(883);Require(!expected.ClaimAttendance() && expected.Error==g.CampaignError,"attendance duplicate reason");Same(g,expected,"attendance duplicate");
            g=DailyHost();g.CampaignAction(681);expected=Expected(g);var ad=new DeferredAd();g.RewardedAds=ad;
            Require(g.HomeNotice(681),"spin dot");g.CampaignAction(881);Same(g,expected,"spin before callback");Require(g.DailyAdPending,"spin pending");
            g.CampaignAction(603);Require(g.ScreenPage==CampaignScreen.Roulette,"ad captures back");
            var slot=expected.Spin(max=>0);Require(slot!=null,"spin Core");ad.Reward();ad.Reward();ad.Fail();Same(g,expected,"spin once");
            Require(!g.HomeNotice(681) && g.RouletteResult.Contains(DailyLabels.Reward(slot.Reward)),"spin result/cooldown dot");
            g.CampaignAction(881);Require(g.CampaignError==g.Campaign.SpinReason(),"spin refusal reason");Same(g,expected,"cooldown refusal");
            byte[] before=CampaignStore.Encode(g.Profile);g.CampaignAction(882);g.CampaignAction(886);Require(g.RouletteDisclosureOpen && g.DailyFirst>0,"odds scroll");
            g.CampaignAction(881);Require(Convert.ToBase64String(before)==Convert.ToBase64String(CampaignStore.Encode(g.Profile)),"disclosure captures spin");g.CampaignAction(603);Require(!g.RouletteDisclosureOpen && g.ScreenPage==CampaignScreen.Roulette,"odds back");
            g=DailyHost();g.CampaignAction(880);expected=Expected(g);Require(g.HomeNotice(880) && expected.ClaimPassTier(1,false),"pass Core");g.CampaignAction(920);Same(g,expected,"free pass tier");
            Require(!expected.ClaimPassTier(1,true),"paid locked Core");g.CampaignAction(950);Require(g.CampaignError==expected.Error,"paid refusal reason");Same(g,expected,"locked paid tier");
            Require(expected.SpendCrystalsToRaiseDailyCap(),"raise Core");g.CampaignAction(884);Same(g,expected,"raise daily cap");
            Require(!expected.SpendCrystalsToRaiseDailyCap(),"raise twice Core");g.CampaignAction(884);Require(g.CampaignError==expected.Error,"raise twice reason");Same(g,expected,"raise twice");
            g=DailyHost();var p=g.Profile.Copy();p.Daily.PurchaseLedger.Add("audit-pass",new PurchaseEntry("audit-pass",PurchaseKind.Pass,0,p.Daily.Season));g.Campaign=new CampaignService(p,x=>{},()=>p.LastSeenUtc,()=>1);g.CampaignAction(880);expected=Expected(g);
            Require(expected.ClaimPassTier(1,true),"owned paid Core");g.CampaignAction(950);Same(g,expected,"owned paid tier");
            g=VictoryHost();expected=Expected(g);ad=new DeferredAd();g.RewardedAds=ad;
            Require(g.CanDoubleReward,"double available");int amount=g.ResultReward;g.CampaignAction(887);Same(g,expected,"double before callback");
            string run=g.CurrentRunId;g.CampaignAction(606);g.CampaignAction(600);Require(g.CurrentRunId==run && g.ScreenPage==CampaignScreen.Battle,"double captures navigation");
            Require(expected.DoubleLastReward(),"double Core");ad.Reward();ad.Reward();ad.Fail();Same(g,expected,"double once");
            Require(!g.CanDoubleReward && g.DisplayResultReward==amount*2,"double chip and button");g.CampaignAction(887);Same(g,expected,"double duplicate");
            g=VictoryHost(false);Require(!g.CanDoubleReward,"no defeat double");expected=Expected(g);g.CampaignAction(887);Same(g,expected,"defeat refusal");
            foreach(bool doubleReward in new[]{false,true}) {
                g=doubleReward?VictoryHost():DailyHost();if(!doubleReward)g.CampaignAction(681);expected=Expected(g);
                g.RewardedAds=new FakeRewardedAdService{Succeeds=false};g.CampaignAction(doubleReward?887:881);Same(g,expected,"failed ad");Require(!g.DailyAdPending && g.CampaignError!="","ad failure text");
                g.RewardedAds=new FakeRewardedAdService{IsReady=false};g.CampaignAction(doubleReward?887:881);Same(g,expected,"unready ad");
                g.RewardedAds=new FakeRewardedAdService();g.Campaign=new CampaignService(g.Profile,x=>{throw new Exception("disk full");},()=>g.Profile.LastSeenUtc,()=>1);
                g.CampaignAction(doubleReward?887:881);Same(g,expected,"ad save rollback");Require(g.CampaignError.Contains("저장 실패"),"ad save failure text");
            }
            g=VictoryHost();p=g.Profile.Copy();p.Daily.RewardDoubles=5;g.Campaign=new CampaignService(p,x=>{},()=>p.LastSeenUtc,()=>1);Require(!g.CanDoubleReward,"double cap");
            g=VictoryHost();p=g.Profile.Copy();p.MagicCores=100000000;g.Campaign=new CampaignService(p,x=>{},()=>p.LastSeenUtc,()=>1);Require(!g.CanDoubleReward,"double balance cap");
            g=VictoryHost();p=g.Profile.Copy();p.SaveSerial=long.MaxValue;g.Campaign=new CampaignService(p,x=>{},()=>p.LastSeenUtc,()=>1);Require(!g.CanDoubleReward,"double save serial cap");
            g=DailyHost();Require(g.HomeNotice(680),"free shop dot");g.CampaignAction(680);g.CampaignAction(815);Require(!g.HomeNotice(680),"free shop dot claimed");
            g.CampaignAction(808);Require(g.HomeNotice(680),"refreshed free shop dot");
            g=DailyHost();p=g.Profile.Copy();p.MagicCores=100000000;g.Campaign=new CampaignService(p,x=>{},()=>p.LastSeenUtc,()=>1);Require(!g.HomeNotice(682) && !g.HomeNotice(680),"capacity suppresses reward dots");
            g=DailyHost();p=g.Profile.Copy();p.Daily.PassPoints=0;g.Campaign=new CampaignService(p,x=>{},()=>p.LastSeenUtc,()=>1);Require(!g.HomeNotice(880),"unearned pass dot");
            g=DailyHost();p=g.Profile.Copy();p.Daily.RouletteSpins=3;g.Campaign=new CampaignService(p,x=>{},()=>p.LastSeenUtc,()=>1);Require(!g.HomeNotice(681),"spin daily cap dot");
            g=DailyHost();g.CampaignAction(880);before=CampaignStore.Encode(g.Profile);for(int i=0;i<20;i++)g.CampaignAction(886);
            Require(g.DailyFirst+g.TouchInput.Layout.DailyVisibleRows()==30,"pass last page");g.CampaignAction(885);Require(g.DailyFirst<25,"pass up");
            Require(Convert.ToBase64String(before)==Convert.ToBase64String(CampaignStore.Encode(g.Profile)),"pass scrolling mutated profile");
            return "PASS: six mission claims/all-clear, attendance, roulette, free/owned paid pass, cap raise and victory double match complete Core profile bytes; real dots, refusal reasons, deferred/duplicate/failed ads, save rollback and pass/odds paging.";
        }
        static GatefallGame ShopHost(int cleared=3)
        {
            var g=Host(5,250);var p=g.Profile.Copy();p.MagicCores=500;p.CrystalsPaid=200;
            for(int i=0;i<=cleared;i++){p.FirstCleared[i]=true;p.BestStars[i]=3;}
            p.Supply.Cards[0]=4;
            g.Campaign=new CampaignService(p,x=>{},()=>p.LastSeenUtc,()=>1);g.SupplyRandom=max=>0;return g;
        }
        static CampaignService Expected(GatefallGame g)
        {long utc=g.Profile.LastSeenUtc;return new CampaignService(g.Profile,x=>{},()=>utc,()=>1);}
        static void Same(GatefallGame g,CampaignService expected,string action)
        {Require(Convert.ToBase64String(CampaignStore.Encode(g.Profile))==Convert.ToBase64String(CampaignStore.Encode(expected.Profile)),action+" differs from Core");}
        public static string RunShopFlowChecks()
        {
            var g=ShopHost();g.CampaignAction(680);Require(g.ScreenPage==CampaignScreen.Shop,"shop tab");
            var expected=Expected(g);var offer=expected.DailyShop().Offers()[0];
            Require(expected.PurchaseShopSlot(0),"prepared shop Core purchase");g.CampaignAction(810);Same(g,expected,"buy shop slot");
            Require(g.Profile.Shop.Offers()[0].Purchased && g.Profile.Supply.Cards[(int)offer.Tower]==expected.Profile.Supply.Cards[(int)offer.Tower],"purchased/card display");
            g.CampaignAction(810);Require(g.CampaignError==g.Campaign.ShopPurchaseReason(0),"sold refusal text");Same(g,expected,"sold refusal");
            g.CampaignAction(801);expected=Expected(g);var result=expected.OpenBox(BoxKind.Normal,max=>0);
            g.CampaignAction(805);Same(g,expected,"normal box");Require(g.BoxResult!=null && g.BoxResult.Cores==result.Cores && g.BoxResult.Cards.Count==result.Cards.Count && g.NormalBoxesRemaining==4,"normal result");
            string[] rows=ShopLabels.ResultRows(g.BoxResult,g.Profile);Require(rows.Length>0 && rows[0].Contains("+"),"result rows");
            g.CampaignAction(805);Same(g,expected,"result captures duplicate purchase");
            g.CampaignAction(819);Require(!g.SupplyModal,"second tap close");
            byte[] before=CampaignStore.Encode(g.Profile);g.CampaignAction(807);
            Require(g.SupplyOddsOpen && ShopLabels.OddsRows(g.Campaign,BoxKind.Normal).Length>4,"odds opens from Core");
            g.CampaignAction(823);Require(g.SupplyOddsKind==BoxKind.Premium && g.SupplyFirst==0,"premium odds switch");
            g.CampaignAction(821);Require(g.SupplyFirst>0,"odds scroll");g.CampaignAction(819);
            Require(Convert.ToBase64String(before)==Convert.ToBase64String(CampaignStore.Encode(g.Profile)),"odds mutated profile");
            g=ShopHost();g.CampaignAction(652);Require(g.ScreenPage==CampaignScreen.Loadout && g.TowerCards,"cards unlock before loadout");
            expected=Expected(g);Require(expected.GradeUp(TowerKind.Autocannon),"prepared grade Core");g.CampaignAction(860);Same(g,expected,"grade tower");
            Require(ShopLabels.Bonus(g.Profile,TowerKind.Autocannon)=="+2%" && ShopLabels.Progress(g.Profile,TowerKind.Autocannon)=="4 / 10","grade display");
            g.CampaignAction(860);Require(g.CampaignError==g.Campaign.GradeUpReason(TowerKind.Autocannon),"grade refusal text");Same(g,expected,"grade refusal");
            g.CampaignAction(836);Require(g.TowerCards && g.CampaignError=="1-10 클리어 후 해금","loadout stays locked");
            g=ShopHost(9);g.CampaignAction(652);Require(!g.TowerCards,"existing loadout kept");g.CampaignAction(835);Require(g.TowerCards,"card subtab");
            g=ShopHost();g.CampaignAction(680);var ad=new DeferredAd();g.RewardedAds=ad;expected=Expected(g);
            g.CampaignAction(808);Require(g.ShopAdPending,"shop deferred ad");Same(g,expected,"before ad callback");
            g.CampaignAction(603);Require(g.ScreenPage==CampaignScreen.Shop,"pending ad captures navigation");
            Require(expected.RefreshShopByAd(),"prepared refresh Core");ad.Reward();ad.Reward();ad.Fail();Same(g,expected,"ad grants once");
            g.RewardedAds=new FakeRewardedAdService{Succeeds=false};g.CampaignAction(808);Same(g,expected,"failed ad");
            g.RewardedAds=new FakeRewardedAdService{IsReady=false};g.CampaignAction(808);Same(g,expected,"unready ad");
            g.RewardedAds=new FakeRewardedAdService();g.CampaignAction(808);Require(expected.RefreshShopByAd(),"second ad");Same(g,expected,"second ad");
            g.CampaignAction(808);Same(g,expected,"refresh cap");
            g=ShopHost();g.CampaignAction(684);Require(g.ShopTab==ShopSection.Hearts,"heart shortcut");expected=Expected(g);
            Require(expected.RefillHeartsByCrystals(),"prepared heart refill");g.CampaignAction(809);Same(g,expected,"shop hearts");
            g.CampaignAction(685);Require(g.ShopTab==ShopSection.Crystals,"crystal shortcut");before=CampaignStore.Encode(g.Profile);
            g.CampaignAction(804);g.CampaignAction(809);Require(Convert.ToBase64String(before)==Convert.ToBase64String(CampaignStore.Encode(g.Profile)),"disabled IAP rows");
            g=ShopHost();g.CampaignAction(680);g.CampaignAction(801);expected=Expected(g);Require(expected.OpenBox(BoxKind.Premium,max=>0)!=null,"premium Core");g.CampaignAction(806);Same(g,expected,"premium free-first crystals");
            g=ShopHost();g.CampaignAction(680);g.CampaignAction(801);expected=Expected(g);
            g.Campaign=new CampaignService(g.Profile,x=>{throw new Exception("disk full");},()=>g.Profile.LastSeenUtc,()=>1);
            g.CampaignAction(805);Require(g.BoxResult==null && g.CampaignError.Contains("저장 실패"),"failed box save");Same(g,expected,"box save rollback");
            g=ShopHost();var pity=g.Profile.Copy();pity.Supply.PremiumPity=SupplyCatalog.PityBoxes-1;
            g.Campaign=new CampaignService(pity,x=>{},()=>pity.LastSeenUtc,()=>1);g.CampaignAction(680);g.CampaignAction(801);
            expected=Expected(g);result=expected.OpenBox(BoxKind.Premium,max=>0);g.CampaignAction(806);Same(g,expected,"pity box");
            Require(g.BoxResult.PityFired && result.PityFired && g.Profile.Supply.PremiumPity==0,"pity result line source");
            foreach(TowerKind kind in g.BoxResult.Cards)Require(ShopLabels.ResultRows(g.BoxResult,g.Profile).Length>0 && ShopLabels.Progress(g.Profile,kind).Contains(" / "),"post-pity card progress");
            g=ShopHost();g.CampaignAction(680);g.CampaignAction(801);
            for(int i=0;i<5;i++){g.CampaignAction(805);Require(g.BoxResult!=null,"normal box within cap");g.CampaignAction(819);}
            expected=Expected(g);g.CampaignAction(805);Same(g,expected,"daily box cap");Require(g.BoxResult==null && g.NormalBoxesRemaining==0 && g.CampaignError==g.Campaign.OpenBoxReason(BoxKind.Normal),"daily cap reason");
            g=ShopHost();g.CampaignAction(680);g.CampaignAction(801);pity=g.Profile.Copy();pity.Supply.Grades[0]=6;pity.Supply.Cards[0]=80;
            g.Campaign=new CampaignService(pity,x=>{},()=>pity.LastSeenUtc,()=>1);expected=Expected(g);result=expected.OpenBox(BoxKind.Normal,max=>0);g.CampaignAction(805);Same(g,expected,"surplus box");
            Require(g.BoxResult.SurplusCores>0 && ShopLabels.ResultRows(g.BoxResult,g.Profile)[0].Contains("마핵 전환"),"surplus result");
            g=ShopHost(0);g.CampaignAction(680);Require(g.ScreenPage==CampaignScreen.Home,"shop lock");
            foreach(int id in new[]{681,682,683}){g.CampaignAction(id);Require(g.DailyPage,"daily page replaces placeholder");g.CampaignAction(603);}
            return "PASS: shop purchase, normal/premium box, odds read/scroll, tower grade, hearts and rewarded refresh match complete Core profile bytes; gates, refusals, duplicate/failed callbacks and save rollback.";
        }
    }
}
#endif
