using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class DailyChecks
    {
        const int Epoch=DailyCatalog.SeasonEpochDay;
        static void Require(bool pass,string why){if(!pass)throw new Exception("Daily: "+why);}
        static bool Same(CampaignProfile a,CampaignProfile b){return CampaignStore.Encode(a).SequenceEqual(CampaignStore.Encode(b));}
        static CampaignProfile Open()
        {
            var p=new CampaignProfile{MagicCores=1000,ExtraHearts=100,CrystalsFree=100,CrystalsPaid=100};
            for(int i=0;i<6;i++){p.FirstCleared[i]=true;p.BestStars[i]=3;}return p;
        }
        sealed class Fixture
        {
            public long Now=100000;
            public int Day=Epoch+56,Writes;
            public bool Fail;
            public CampaignProfile Saved;
            public CampaignService Service;
            public Fixture(CampaignProfile p=null)
            {Service=new CampaignService(p??Open(),q=>{if(Fail)throw new IOException("injected");Writes++;Saved=q.Copy();},()=>Now,()=>Day);}
            public DefenseSimulation Result(bool won=true,bool crisis=false,int fusions=0,float health=1)
            {
                var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,Service.Battle(0,crisis));
                typeof(DefenseSimulation).GetProperty("State").SetValue(g,won ? RunState.Won : RunState.Lost,null);
                typeof(DefenseSimulation).GetProperty("MinimumCoreHealth").SetValue(g,g.Balance.CoreHealth*health,null);
                g.Telemetry.Fusions=fusions;return g;
            }
            public void Win(bool crisis=false,int fusions=0,float health=1)
            {
                string run;int reward;Require(Service.Begin(0,crisis,out run),"begin");int writes=Writes;
                Require(Service.Complete(run,Result(true,crisis,fusions,health),out reward) && Writes==writes+1,"victory one commit");
            }
        }
        static void Rollback(Fixture f,Func<bool> mutation,string why)
        {
            var before=f.Service.Profile.Copy();int writes=f.Writes;f.Fail=true;
            Require(!mutation() && Same(before,f.Service.Profile) && f.Writes==writes && f.Service.Error.StartsWith("저장 실패:"),why+" rollback");
            f.Fail=false;Require(mutation() && f.Writes==writes+1 && f.Saved.SaveSerial==before.SaveSerial+1,why+" retry one commit");
            Require(Same(f.Service.Profile,f.Saved),why+" persisted state");
        }
        static void ReadyMissions(Fixture f)
        {
            f.Win(false,2);f.Win(true);f.Win();
            Require(f.Service.OpenBox(BoxKind.Normal,n=>0)!=null && f.Service.RefillHeartByAd(),"mission events");
        }
        // Independent legacy writer: v3 has no daily fields, tickets, pass entitlement or order IDs.
        static byte[] V3(CampaignProfile p)
        {
            byte[] bytes;using(var m=new MemoryStream()){using(var w=new BinaryWriter(m,Encoding.UTF8,true)){
                w.Write("GATEFALL-RAON");w.Write(3);w.Write(p.MagicCores);w.Write(p.SelectedHunter);w.Write(p.StoryMask);w.Write(p.TutorialMask);w.Write(p.Muted);
                foreach(int n in p.Research)w.Write(n);foreach(int n in p.Growth)w.Write(n);w.Write(p.Loadout);
                w.Write(p.PendingRun);w.Write("");w.Write(p.PendingCrisis);w.Write(p.LastRewardRun);w.Write(p.LastReward);
                w.Write(CampaignCatalog.Stages.Length);foreach(var s in CampaignCatalog.Stages.OrderBy(s=>s.Id,StringComparer.Ordinal)){
                    int i=s.Index;w.Write(s.Id);w.Write(p.BestStars[i]);w.Write(p.CrisisStars[i]);w.Write(p.BestErosion[i]);w.Write(p.FirstCleared[i]);}
                w.Write(0);w.Write(0);
            }bytes=m.ToArray();}
            using(var sha=SHA256.Create()){var all=new byte[32+bytes.Length];Buffer.BlockCopy(sha.ComputeHash(bytes),0,all,0,32);Buffer.BlockCopy(bytes,0,all,32,bytes.Length);return all;}
        }
        public static string Run()
        {
            var report=new List<string>();var f=new Fixture();var s=f.Service;
            var preview=s.DailyStatus();Require(f.Writes==0 && s.Profile.Daily.LocalDay==-1 && preview.LocalDay==Epoch+56,"read-only refreshed preview");
            preview.Progress[0]=3;preview.PurchaseLedger.Add("preview",new PurchaseEntry("preview",PurchaseKind.OtherNonConsumable,0));Require(s.Profile.Daily.Progress[0]==0 && s.Profile.Daily.PurchaseLedger.Count==0,"preview aliases profile");
            foreach(var mission in DailyCatalog.Missions())Require(!s.ClaimDailyMission(mission.Mission),"incomplete claim");
            string run;int reward;Require(s.Begin(0,false,out run),"loss launch");Require(s.Complete(run,f.Result(false,false,20),out reward),"loss commit");
            Require(s.Profile.Daily.Progress.All(n=>n==0) && s.Profile.Daily.PassPoints==0 && !s.DoubleLastReward(),"defeat must not progress");
            f.Win(false,1,.2f);Require(s.Profile.Daily.Progress[0]==1 && s.Profile.Daily.Progress[1]==0 && s.Profile.Daily.Progress[2]==1,"low-star victory telemetry");
            ReadyMissions(f);Require(s.Profile.Daily.Progress.SequenceEqual(new[]{3,1,2,1,1,1}),"six event targets/saturation");
            int cores=s.Profile.MagicCores,points=s.Profile.Daily.PassPoints;
            foreach(var mission in DailyCatalog.Missions()){
                int writes=f.Writes;Require(s.ClaimDailyMission(mission.Mission) && f.Writes==writes+1,"claim one commit");
                var before=s.Profile.Copy();Require(!s.ClaimDailyMission(mission.Mission) && Same(before,s.Profile),"duplicate mission");}
            Require(s.Profile.MagicCores==cores+18 && s.Profile.Daily.PassPoints==points+30 && s.Profile.Daily.NormalTickets==1,"mission rewards/completion ticket");
            var complete=s.Profile.Copy();Require(s.Complete(s.Profile.LastRewardRun,f.Result(),out reward) && Same(complete,s.Profile),"victory replay progressed missions");
            Require(!s.ClaimDailyMission((DailyMission)6),"invalid mission");
            f.Day=Epoch+55;f.Now=99999;Require(!s.ClaimDailyMission(DailyMission.Wins) && s.DailyStatus().MissionClaims==63,"backward mission claims");
            f.Day=Epoch+56;Require(!s.ClaimDailyMission(DailyMission.Wins),"clock restored duplicate mission");
            f.Day=Epoch+57;var daily=s.DailyStatus();Require(daily.Progress.All(n=>n==0) && daily.MissionClaims==0 && daily.PassPoints==points+30,"new day mission preview");
            f.Win();Require(s.Profile.Daily.Progress[0]==1 && s.Profile.Daily.Progress[1]==1 && s.Profile.Daily.Progress[2]==0,"new day event");
            report.Add("PASS six missions from victories/fusions/crisis/boxes/ads; one claim and completion ticket; loss, duplicate run, day rollover/backward clock");

            f=new Fixture();s=f.Service;int[] heartRewards={1,0,1,0,2,0,0},coreRewards={0,5,0,5,0,10,0};
            for(int i=0;i<7;i++){
                int hearts=s.Profile.Hearts+s.Profile.ExtraHearts;cores=s.Profile.MagicCores;int writes=f.Writes;
                Require(s.ClaimAttendance() && f.Writes==writes+1,"attendance "+i);
                Require(s.Profile.Hearts+s.Profile.ExtraHearts==hearts+heartRewards[i] && s.Profile.MagicCores==cores+coreRewards[i],"attendance reward "+i);
                var before=s.Profile.Copy();Require(!s.ClaimAttendance() && Same(before,s.Profile),"attendance twice");
                int current=f.Day;f.Day--;Require(!s.ClaimAttendance(),"backward attendance");f.Day=current;Require(!s.ClaimAttendance(),"restored attendance");
                f.Day+=i==1 ? 3 : 1;
            }
            Require(s.Profile.Daily.AttendancePosition==0 && s.Profile.Daily.PremiumTickets==1,"attendance wrap/day seven");
            Require(s.ClaimAttendance() && s.Profile.Daily.AttendancePosition==1,"attendance next strip");
            f=new Fixture();s=f.Service;Require(s.ClaimAttendance(),"attendance before season");f.Day=Epoch+84;
            Require(s.ClaimAttendance() && s.Profile.Daily.AttendancePosition==2,"season preserves strip");
            report.Add("PASS 7-day attendance: exact rewards, skipped days preserve position, wrap, season independence and no backward-clock reclaims");

            f=new Fixture();s=f.Service;var table=DailyCatalog.RouletteDisclosure();
            Require(table.Sum(x=>x.Percent)==100m && table.Sum(x=>x.Weight)==10000,"disclosure exact total");
            int[] counts=new int[table.Length];for(int roll=0;roll<10000;roll++){
                var slot=DailyCatalog.Roll(n=>roll);int index=Array.FindIndex(table,x=>ReferenceEquals(x,slot));Require(index>=0,"roll outside disclosure");counts[index]++;}
            Require(counts.SequenceEqual(table.Select(x=>x.Weight)),"disclosure/roll exact distribution");
            table[0]=null;Require(DailyCatalog.RouletteDisclosure()[0]!=null,"table array alias");
            int boundary=0;foreach(var slot in DailyCatalog.RouletteDisclosure()){
                f=new Fixture();s=f.Service;var before=s.Profile.Copy();int roll=boundary;
                var result=s.Spin(n=>roll);Require(result!=null && result.Reward.Kind==slot.Reward.Kind && result.Reward.Amount==slot.Reward.Amount,"roulette boundary");
                Require(s.Profile.Daily.RouletteSpins==1 && s.Profile.Daily.Progress[5]==1 && f.Writes==1,"spin/ad one commit");
                switch(slot.Reward.Kind){
                    case DailyRewardKind.MagicCores:Require(s.Profile.MagicCores==before.MagicCores+slot.Reward.Amount,"roulette cores");break;
                    case DailyRewardKind.Hearts:Require(s.Profile.ExtraHearts==before.ExtraHearts+slot.Reward.Amount,"roulette hearts");break;
                    case DailyRewardKind.NormalTicket:Require(s.Profile.Daily.NormalTickets==1,"roulette ticket");break;
                    case DailyRewardKind.FreeCrystals:Require(s.Profile.CrystalsFree==before.CrystalsFree+5 && s.Profile.CrystalsPaid==before.CrystalsPaid,"roulette free crystals");break;
                }boundary+=slot.Weight;
            }
            f=new Fixture();s=f.Service;Require(s.Spin(new Random(7))!=null,"random overload");
            Require(s.Spin(n=>0)==null && s.SpinReason()=="룰렛 대기 시간","immediate cooldown");
            f.Now+=14399;Require(s.Spin(n=>0)==null,"cooldown boundary -1");f.Now++;Require(s.Spin(n=>0)!=null,"exact four hours");
            f.Now+=14400;Require(s.Spin(n=>0)!=null,"third spin");f.Now+=14400;Require(s.Spin(n=>0)==null && s.SpinReason()=="룰렛 일일 한도","three spin limit");
            f.Day--;Require(s.Spin(n=>0)==null,"backward day spin quota");f.Day+=2;f.Now=s.Profile.Daily.LastSpinUtc+14399;
            Require(s.Spin(n=>0)==null,"cooldown crosses midnight");f.Now++;Require(s.Spin(n=>0)!=null && s.Profile.Daily.RouletteSpins==1,"next day spin reset");
            f.Now-=20000;Require(s.Spin(n=>0)==null,"backward UTC cooldown");f.Now+=34400;Require(s.Spin(n=>0)!=null,"clock recovered cooldown");
            f=new Fixture();s=f.Service;var snap=s.Profile.Copy();foreach(int invalidRoll in new[]{-1,10000})Require(s.Spin(n=>invalidRoll)==null && Same(snap,s.Profile),"invalid RNG");
            Require(s.Spin((Random)null)==null && s.Spin((Func<int,int>)null)==null && s.Spin(n=>{throw new Exception("rng");})==null && Same(snap,s.Profile),"null/throw RNG");
            report.Add("PASS roulette: exact 100% disclosure and all 10,000 outcomes, every slot reward, 3/day, 4-hour boundary/midnight/backward UTC, invalid RNG");

            f=new Fixture();s=f.Service;Require(!s.DoubleLastReward(),"no victory doubling");
            for(int i=0;i<5;i++){
                f.Win();cores=s.Profile.MagicCores;int writes=f.Writes;
                Require(s.DoubleLastReward() && f.Writes==writes+1 && s.Profile.MagicCores==cores+s.Profile.LastReward,"double amount/commit");
                var before=s.Profile.Copy();Require(!s.DoubleLastReward() && Same(before,s.Profile),"double same run");
                Require(s.Complete(s.Profile.LastRewardRun,f.Result(),out reward) && reward==2 && Same(before,s.Profile),"completion after double");
            }
            f.Win();Require(!s.DoubleLastReward(),"five double limit");f.Day--;Require(!s.DoubleLastReward(),"backward double quota");
            f.Day+=2;Require(s.DoubleLastReward() && s.Profile.Daily.RewardDoubles==1,"next day double reset");
            var doubled=new CampaignService(CampaignStore.Decode(CampaignStore.Encode(s.Profile)),q=>{throw new Exception("duplicate double save");},()=>f.Now,()=>f.Day);
            Require(!doubled.DoubleLastReward(),"double replay after reload");
            f.Day++;Require(!s.DoubleLastReward(),"same run double on next day");f.Win(true);Require(s.DoubleLastReward(),"new crisis victory double");
            Require(s.Begin(0,false,out run) && s.Complete(run,f.Result(false),out reward) && !s.DoubleLastReward(),"defeat cannot double");
            report.Add("PASS reward doubling: latest result only, exact base cores, once per run across days/reloads, 5/day and crisis rewards");

            f=new Fixture();s=f.Service;for(int i=0;i<15;i++)f.Win();Require(s.Profile.Daily.PassPoints==60 && s.Profile.Daily.PointsToday==60,"victory daily cap");
            ReadyMissions(f);foreach(var offer in DailyCatalog.Missions())Require(s.ClaimDailyMission(offer.Mission),"capped mission claim");
            Require(s.Profile.Daily.PassPoints==60,"missions bypassed cap");
            Require(!s.ClaimPassTier(0,false) && !s.ClaimPassTier(31,false) && !s.ClaimPassTier(2,false) && !s.ClaimPassTier(1,true),"pass claim eligibility");
            Require(s.ClaimPassTier(1,false) && !s.ClaimPassTier(1,false),"free tier once");
            int free=s.Profile.CrystalsFree,paid=s.Profile.CrystalsPaid;Require(s.SpendCrystalsToRaiseDailyCap() && s.Profile.CrystalsFree==free-30 && s.Profile.CrystalsPaid==paid,"cap price/free first");
            Require(!s.SpendCrystalsToRaiseDailyCap(),"cap once per day");for(int i=0;i<15;i++)f.Win();
            Require(s.Profile.Daily.PassPoints==120 && s.Profile.Daily.PointsToday==120,"raised cap exact");
            f.Day--;Require(!s.SpendCrystalsToRaiseDailyCap() && s.DailyStatus().FreeClaims==1,"backward cap/claims");
            f.Day+=2;f.Win();Require(s.Profile.Daily.PointsToday==5 && !s.Profile.Daily.CapRaised && s.Profile.Daily.PassPoints==125,"daily point reset");
            f=new Fixture(new CampaignProfile());Require(!f.Service.SpendCrystalsToRaiseDailyCap(),"cap insufficient crystals");
            var p=Open();p.CrystalsFree=10;p.CrystalsPaid=20;f=new Fixture(p);Require(f.Service.SpendCrystalsToRaiseDailyCap() && f.Service.Profile.CrystalsFree==0 && f.Service.Profile.CrystalsPaid==0,"cap mixed spend");
            report.Add("PASS pass points: victories/missions share 60 daily cap, 30 crystals buys +60 once, free-first spending and daily reset");

            p=Open();HeartRules.Refresh(p,100000,Epoch+56);p.Daily.PassPoints=1500;p.Daily.PointsToday=60;
            f=new Fixture(p);s=f.Service;Require(s.GrantPassPurchase("GPA.pass:season-3"),"pass IAP entitlement");
            var rows=DailyCatalog.PassRewards();Require(rows.Length==30 && rows.Select(x=>x.Tier).SequenceEqual(Enumerable.Range(1,30)),"thirty data rows");
            foreach(var row in rows)foreach(bool paidTrack in new[]{false,true}){
                int writes=f.Writes;Require(s.ClaimPassTier(row.Tier,paidTrack) && f.Writes==writes+1,"track claim");
                var before=s.Profile.Copy();Require(!s.ClaimPassTier(row.Tier,paidTrack) && Same(before,s.Profile),"track duplicate");}
            Require(s.Profile.Daily.FreeClaims==(1<<30)-1 && s.Profile.Daily.PaidClaims==(1<<30)-1,"all claim bits");
            Require(s.Profile.Daily.NormalTickets==3 && s.Profile.Daily.PremiumTickets==6 && s.Profile.MagicCores==1045 &&
                s.Profile.ExtraHearts==118 && s.Profile.CrystalsFree==360 && (s.Profile.Supply.OwnedCosmetics&1)!=0,"full track totals");
            f.Win();Require(s.Profile.Daily.PassPoints==1500,"points stop at tier thirty");
            var copy=s.Profile.Copy();copy.Daily.Progress[0]=0;copy.Daily.PurchaseLedger.Add("copy",new PurchaseEntry("copy",PurchaseKind.OtherNonConsumable,0));Require(s.Profile.Daily.Progress[0]==1 && !s.Profile.Daily.PurchaseLedger.ContainsKey("copy"),"state deep copy");
            rows[0]=null;Require(DailyCatalog.PassRewards()[0]!=null,"pass table array alias");
            f.Day=Epoch+83;Require(s.RefreshHearts() && s.Profile.Daily.PassPurchased && s.Profile.Daily.FreeClaims==(1<<30)-1,"last season day");
            f.Day=Epoch+84;Require(s.RefreshHearts() && s.Profile.Daily.Season==4 && s.Profile.Daily.PassPoints==0 && s.Profile.Daily.FreeClaims==0 &&
                s.Profile.Daily.PaidClaims==0 && !s.Profile.Daily.PassPurchased && s.Profile.Daily.PurchaseLedger.Count==1 && s.Profile.Daily.NormalTickets==3,"new season reset/preservation");
            int oldWrites=f.Writes;Require(s.GrantPassPurchase("GPA.pass:season-3") && !s.Profile.Daily.PassPurchased && f.Writes==oldWrites,"old season order cannot re-unlock");
            Require(s.GrantPassPurchase("GPA.pass:season-4") && s.Profile.Daily.PassPurchased,"new season purchase");
            f.Day=Epoch+56;Require(s.DailyStatus().Season==4 && !s.ClaimPassTier(1,false),"backward season");
            int e=DailyCatalog.SeasonEpochDay;Require(DailyCatalog.SeasonNumber(0)==1 && DailyCatalog.SeasonNumber(e+27)==1 && DailyCatalog.SeasonNumber(e+28)==2,"epoch/season boundaries (season 1 starts 2026-11-16)");
            report.Add("PASS 30 tiers and both data-driven tracks, totals/cosmetic, per-track claim bits, season epoch/boundary and preserved orders/tickets");

            f=new Fixture();s=f.Service;paid=s.Profile.CrystalsPaid;
            Require(s.GrantCrystalPurchase("GPA.crystal:1",60) && s.Profile.CrystalsPaid==paid+60 && s.Profile.CrystalsFree==100,"paid crystal purchase");
            var purchase=s.Profile.Copy();oldWrites=f.Writes;
            Require(s.GrantCrystalPurchase("GPA.crystal:1",999) && s.GrantPassPurchase("GPA.crystal:1") && Same(purchase,s.Profile) && f.Writes==oldWrites,"shared idempotency");
            Require(s.GrantPassPurchase("GPA.pass:1") && s.GrantCrystalPurchase("GPA.pass:1",100) && s.Profile.CrystalsPaid==paid+60,"pass/crystal same order");
            var loaded=new CampaignService(CampaignStore.Decode(CampaignStore.Encode(s.Profile)),q=>{throw new Exception("duplicate write");},()=>f.Now,()=>f.Day);
            Require(loaded.GrantPassPurchase("GPA.pass:1") && loaded.GrantCrystalPurchase("GPA.crystal:1",60),"order reload idempotency");
            purchase=s.Profile.Copy();foreach(string id in new[]{null,""," ","bad\nid",new string('x',257)})Require(!s.GrantPassPurchase(id) && !s.GrantCrystalPurchase(id,60),"invalid order");
            Require(!s.GrantCrystalPurchase("bad-amount",0) && !s.GrantCrystalPurchase("bad-amount",-1) && Same(purchase,s.Profile),"purchase input no mutation");
            p=Open();p.CrystalsPaid=int.MaxValue;f=new Fixture(p);Require(!f.Service.GrantCrystalPurchase("overflow",1) && f.Service.Profile.Daily.PurchaseLedger.Count==0,"crystal overflow consumes order");
            report.Add("PASS shared persistent opaque order IDs, cross-product/reload idempotency, paid crystals, invalid inputs and integer overflow");

            f=new Fixture();s=f.Service;ReadyMissions(f);
            foreach(var offer in DailyCatalog.Missions())Rollback(f,()=>s.ClaimDailyMission(offer.Mission),"mission "+offer.Mission);
            Require(s.Profile.Daily.NormalTickets==1,"failed final mission ticket");
            Rollback(f,()=>s.ClaimAttendance(),"attendance");Rollback(f,()=>s.Spin(n=>0)!=null,"roulette");
            Rollback(f,()=>s.DoubleLastReward(),"double");Rollback(f,()=>s.SpendCrystalsToRaiseDailyCap(),"cap raise");
            Rollback(f,()=>s.GrantPassPurchase("rollback-pass"),"pass purchase");Rollback(f,()=>s.GrantCrystalPurchase("rollback-crystals",60),"crystal purchase");
            f.Win();Require(s.Profile.Daily.PassPoints>=50,"rollback tier setup");
            Rollback(f,()=>s.ClaimPassTier(1,false),"free tier");Rollback(f,()=>s.ClaimPassTier(1,true),"paid tier");
            Rollback(f,()=>s.OpenBox(BoxKind.Normal,n=>0)!=null,"normal ticket open");
            p=Open();p.Daily.PremiumTickets=1;f=new Fixture(p);s=f.Service;Rollback(f,()=>s.OpenBox(BoxKind.Premium,n=>0)!=null,"premium ticket open");
            f=new Fixture();s=f.Service;Rollback(f,()=>s.RefillHeartByAd(),"heart ad mission");Rollback(f,()=>s.RefreshShopByAd(),"shop ad mission");
            Require(s.Begin(0,true,out run),"rollback crisis begin");snap=s.Profile.Copy();f.Fail=true;
            Require(!s.Complete(run,f.Result(true,true,2),out reward) && reward==0 && Same(snap,s.Profile),"victory mission rollback");
            f.Fail=false;Require(s.Complete(run,f.Result(true,true,2),out reward) && s.Profile.Daily.Progress[3]==1 && s.Profile.Daily.Progress[2]==2,"retry crisis telemetry");
            f.Day++;Rollback(f,()=>s.ClaimAttendance(),"new day attendance/reset");
            f.Day=Epoch+84;Rollback(f,()=>s.GrantPassPurchase("next-season"),"new season reset/purchase");
            report.Add("PASS injected save failures atomically roll back every mutation, mission completion ticket, ad hooks, victory telemetry and day/season resets");

            p=Open();p.MagicCores=200;p.CrystalsFree=200;p.CrystalsPaid=50;HeartRules.Refresh(p,100000,Epoch+56);
            p.Daily.NormalTickets=1;p.Daily.PremiumTickets=1;p.DailyLimits.NamedCounters["normal-boxes"]=5;
            f=new Fixture(p);s=f.Service;Require(s.OpenBoxReason(BoxKind.Normal)=="" && s.OpenBox(BoxKind.Normal,n=>0)!=null &&
                s.Profile.MagicCores==205 && s.Profile.Daily.NormalTickets==0 && s.Profile.DailyLimits.NamedCounters["normal-boxes"]==5,"ticket before cores/bypasses purchase quota");
            Require(s.OpenBox(BoxKind.Normal,n=>0)==null,"purchased normal limit");
            free=s.Profile.CrystalsFree;paid=s.Profile.CrystalsPaid;Require(s.OpenBox(BoxKind.Premium,n=>0)!=null && s.Profile.Daily.PremiumTickets==0 &&
                s.Profile.CrystalsFree==free && s.Profile.CrystalsPaid==paid,"premium ticket before crystals");
            f.Day++;Require(s.OpenBox(BoxKind.Normal,n=>0)!=null && s.Profile.DailyLimits.NamedCounters["normal-boxes"]==1,"purchase after rollover");
            p=new CampaignProfile();p.Daily.NormalTickets=1;f=new Fixture(p);Require(f.Service.OpenBox(BoxKind.Normal,n=>0)==null,"ticket bypassed supply unlock");
            p=Open();p.Daily.NormalTickets=1;f=new Fixture(p);snap=f.Service.Profile.Copy();Require(f.Service.OpenBox(BoxKind.Normal,n=>-1)==null && Same(snap,f.Service.Profile),"failed box RNG consumed ticket");
            report.Add("PASS normal/premium tickets spend before currencies, purchased-normal 5/day stays separate, unlock and failed-roll preservation");

            p=Open();p.Hearts=5;p.ExtraHearts=999;f=new Fixture(p);snap=f.Service.Profile.Copy();
            Require(!f.Service.ClaimAttendance() && f.Service.Spin(n=>7500)==null && Same(snap,f.Service.Profile),"full hearts consumed claim/spin");
            p=Open();p.MagicCores=100000000;f=new Fixture(p);snap=f.Service.Profile.Copy();
            Require(f.Service.Spin(n=>0)==null && Same(snap,f.Service.Profile),"core overflow consumed spin");
            f=new Fixture();s=f.Service;ReadyMissions(f);s.Profile.Daily.NormalTickets=int.MaxValue;
            foreach(var offer in DailyCatalog.Missions().Take(5))Require(s.ClaimDailyMission(offer.Mission),"ticket-cap mission setup");
            snap=s.Profile.Copy();Require(!s.ClaimDailyMission(DailyMission.Ad) && Same(snap,s.Profile),"full completion-ticket storage consumed last mission");
            f=new Fixture();s=f.Service;Require(s.PurchaseShopSlot(5),"shop high-water setup");f.Day=Epoch+57;Require(s.RefreshHearts(),"day advance without shop");
            f.Day=Epoch+56;Require(s.DailyShop().LocalDay==Epoch+57 && s.PurchaseShopSlot(5),"shop preview follows persisted high-water day");
            Require(!s.PurchaseShopSlot(5),"backward shop free reward twice");
            report.Add("PASS reward capacity failures preserve claims/spins/completion ticket; shop preview follows the persisted high-water date");

            p=Open();var legacy=V3(p);var migrated=CampaignStore.Decode(legacy);var d=migrated.Daily;
            Require(d.Valid() && d.LocalDay==-1 && d.Progress.All(n=>n==0) && d.MissionClaims==0 && d.AttendancePosition==0 && d.AttendanceDay==-1 &&
                d.NormalTickets==0 && d.PremiumTickets==0 && d.Season==0 && d.PassPoints==0 && !d.PassPurchased && !d.CapRaised && d.PurchaseLedger.Count==0 &&
                d.RouletteSpins==0 && d.LastSpinUtc==-1 && d.RewardDoubles==0 && d.DoubledRun=="","v3 migration defaults");
            Require(V3(migrated).SequenceEqual(legacy) && migrated.HeartRewardsBackfilled,"migration changed old fields");
            Require(migrated.AncestorHashes.Count==0 && migrated.CommitHash!=p.CommitHash &&
                CampaignStore.Decode(legacy).CommitHash!=migrated.CommitHash,"v3 starts independent lineage");
            Require(CloudSync.Compare(legacy,legacy)==CloudSaveComparison.Same,"identical validated v3 snapshots match");
            p=Open();HeartRules.Refresh(p,100000,Epoch+56);d=p.Daily;
            d.Progress=new[]{3,1,2,1,1,1};d.MissionClaims=63;d.AttendancePosition=6;d.AttendanceDay=Epoch+56;
            d.RouletteSpins=3;d.RewardDoubles=5;d.LastSpinUtc=99999;d.DoubledRun="last-victory";d.NormalTickets=7;d.PremiumTickets=9;
            d.PassPoints=1500;d.PointsToday=120;d.CapRaised=true;d.FreeClaims=(1<<30)-1;d.PaidClaims=(1<<30)-1;
            d.PurchaseLedger.Add("GPA.Z:opaque/order",new PurchaseEntry("GPA.Z:opaque/order",PurchaseKind.Pass,0,3));d.PurchaseLedger.Add("GPA.A:other-order",new PurchaseEntry("GPA.A:other-order",PurchaseKind.Crystals,60));p.Supply.OwnedCosmetics=7;
            byte[] bytes=CampaignStore.Encode(p);var restored=CampaignStore.Decode(bytes);Require(bytes.SequenceEqual(CampaignStore.Encode(restored)),"populated v4 byte identity");
            Require(restored.Daily.PurchaseLedger.Keys.OrderBy(x=>x).SequenceEqual(d.PurchaseLedger.Keys.OrderBy(x=>x)) && restored.Daily.MissionClaims==63 && restored.Daily.PaidClaims==(1<<30)-1,"populated v4 fields");
            var invalid=new Action<DailyState>[] {q=>q.LocalDay=-2,q=>q.MissionClaims=64,q=>q.Progress[0]=4,q=>q.AttendancePosition=7,q=>q.AttendanceDay=Epoch+57,
                q=>q.RouletteSpins=4,q=>q.LastSpinUtc=-2,q=>q.RewardDoubles=6,q=>q.NormalTickets=-1,q=>q.PremiumTickets=-1,q=>q.Season=999,q=>q.PassPoints=1501,
                q=>q.PointsToday=121,q=>q.PurchaseLedger.Clear(),q=>q.FreeClaims=int.MaxValue,q=>q.PurchaseLedger.Add("bad\nid",new PurchaseEntry("bad\nid",PurchaseKind.Crystals,1))};
            foreach(var change in invalid){var bad=p.Copy();change(bad.Daily);Require(!bad.Valid(),"invalid daily state accepted");}
            bytes[bytes.Length-1]^=1;bool rejected=false;try{CampaignStore.Decode(bytes);}catch(InvalidDataException){rejected=true;}Require(rejected,"daily checksum");
            report.Add("PASS independent v3 -> v4 default migration, old fields preserved, populated v4 byte-identical re-encode, deep copies, validation and checksum");
            return string.Join("\n",report);
        }
    }
}
