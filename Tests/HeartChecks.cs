using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class HeartChecks
    {
        static void Require(bool pass,string why){if(!pass)throw new Exception("Hearts: "+why);}
        static bool Same(CampaignProfile a,CampaignProfile b){return CampaignStore.Encode(a).SequenceEqual(CampaignStore.Encode(b));}
        static CampaignProfile At(int hearts=5,int extra=0)
        { return new CampaignProfile{Hearts=hearts,ExtraHearts=extra,HeartAnchorUtc=10000,LastSeenUtc=10000}; }
        static CampaignProfile Open(int stage)
        { var p=At();for(int i=0;i<stage;i++){p.FirstCleared[i]=true;p.BestStars[i]=3;}return p; }
        static DefenseSimulation Result(CampaignService s,int stage,bool won=true,bool crisis=false,float health=1)
        {
            var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,s.Battle(stage,crisis));
            typeof(DefenseSimulation).GetProperty("State").SetValue(g,won ? RunState.Won : RunState.Lost,null);
            typeof(DefenseSimulation).GetProperty("MinimumCoreHealth").SetValue(g,g.Balance.CoreHealth*health,null);return g;
        }
        // Independent v3 fixture writer, including every old field and unknown future progress.
        static byte[] V3(CampaignProfile p)
        {
            byte[] bytes;using(var m=new MemoryStream()){using(var w=new BinaryWriter(m,Encoding.UTF8,true)){
                w.Write("GATEFALL-RAON");w.Write(3);w.Write(p.MagicCores);w.Write(p.SelectedHunter);w.Write(p.StoryMask);w.Write(p.TutorialMask);w.Write(p.Muted);
                foreach(int n in p.Research)w.Write(n);foreach(int n in p.Growth)w.Write(n);w.Write(p.Loadout);
                w.Write(p.PendingRun);w.Write(p.PendingStage<0 ? "" : CampaignCatalog.Stages[p.PendingStage].Id);w.Write(p.PendingCrisis);w.Write(p.LastRewardRun);w.Write(p.LastReward);
                var ids=CampaignCatalog.Stages.Select(s=>s.Id).Concat(p.UnmappedProgress.Keys).OrderBy(s=>s,StringComparer.Ordinal).ToArray();w.Write(ids.Length);
                foreach(string id in ids){w.Write(id);int i=CampaignCatalog.FindStage(id);var v=i<0 ? p.UnmappedProgress[id] : new StageProgress{Stars=p.BestStars[i],CrisisStars=p.CrisisStars[i],Erosion=p.BestErosion[i],FirstCleared=p.FirstCleared[i]};
                    w.Write(v.Stars);w.Write(v.CrisisStars);w.Write(v.Erosion);w.Write(v.FirstCleared);}
                foreach(var set in new[]{p.StorySeenIds,p.TutorialSeenIds}){w.Write(set.Count);foreach(string id in set.OrderBy(s=>s,StringComparer.Ordinal))w.Write(id);}
            }bytes=m.ToArray();}
            using(var sha=SHA256.Create()){var all=new byte[32+bytes.Length];Buffer.BlockCopy(sha.ComputeHash(bytes),0,all,0,32);Buffer.BlockCopy(bytes,0,all,32,bytes.Length);return all;}
        }
        public static string Run()
        {
            var report=new List<string>();var p=At(0);
            HeartRules.Refresh(p,10899,1);Require(p.Hearts==0 && HeartRules.NextRegenSeconds(p,10899,1)==1,"partial interval");
            HeartRules.Refresh(p,10900,1);Require(p.Hearts==1 && p.HeartAnchorUtc==10900,"first interval");
            HeartRules.Refresh(p,12250,1);Require(p.Hearts==2 && p.HeartAnchorUtc==11800 && HeartRules.NextRegenSeconds(p,12250,1)==450,"preserve remainder");
            HeartRules.Refresh(p,100000,1);Require(p.Hearts==5 && p.ExtraHearts==0 && HeartRules.NextRegenSeconds(p,100000,1)==0,"regen stops at five");
            Require(HeartRules.Spend(p,200000,1) && p.Hearts==4 && p.HeartAnchorUtc==200000,"full time cannot be banked");
            HeartRules.Refresh(p,200899,1);Require(p.Hearts==4,"full-stop partial");HeartRules.Refresh(p,200900,1);Require(p.Hearts==5,"full-stop interval");
            p=At(0);HeartRules.Refresh(p,8000,1);Require(p.Hearts==0 && p.HeartAnchorUtc==8000 && p.LastSeenUtc==8000,"backward clock");
            HeartRules.Refresh(p,8899,1);Require(p.Hearts==0,"backward remainder");HeartRules.Refresh(p,8900,1);Require(p.Hearts==1,"backward recovery");
            var snapshot=p.Copy();Require(HeartRules.NextRegenSeconds(p,8999,2)==801 && Same(p,snapshot),"HUD query mutates profile");
            p=At(0);p.HeartAnchorUtc=p.LastSeenUtc=0;HeartRules.Refresh(p,900,1);Require(p.Hearts==1,"Unix epoch is valid");
            report.Add("PASS regen: 900 seconds, partial intervals, full stop, backward clock and read-only HUD countdown");

            p=At(3,998);Require(HeartRules.Grant(p,5)==2 && p.Hearts==5 && p.ExtraHearts==999,"grant base first/overflow");
            Require(HeartRules.Grant(p,int.MaxValue)==int.MaxValue,"grant integer overflow");
            Require(HeartRules.Spend(p,10000,1) && p.Hearts==4 && p.ExtraHearts==999,"base spend first");
            p=At(0,2);Require(HeartRules.Spend(p,10000,1) && p.Hearts==0 && p.ExtraHearts==1,"extra spend");
            p=At(0);Require(!HeartRules.Spend(p,10000,1),"empty spend");
            p=At();p.DailyLimits.LocalDay=1;p.DailyLimits.NamedCounters.Add("shop-refresh",2);
            for(int i=0;i<10;i++)Require(HeartRules.AdRefill(p,10000,1),"ad quota "+i);
            Require(!HeartRules.AdRefill(p,10000,1) && p.ExtraHearts==10 && p.DailyLimits.HeartAdsUsed==10,"ad limit");
            Require(HeartRules.AdRefill(p,10000,2) && p.DailyLimits.HeartAdsUsed==1 && p.DailyLimits.NamedCounters.Count==0,"daily reset");
            HeartRules.Refresh(p,10000,1);Require(p.DailyLimits.HeartAdsUsed==1,"backward local day must not reset limits");
            p=At(0);p.CrystalsFree=20;p.CrystalsPaid=40;Require(HeartRules.CrystalRefill(p,10000,1) && p.Hearts==5 && p.CrystalsFree==0 && p.CrystalsPaid==30,"free before paid");
            p.CrystalsFree=40;Require(HeartRules.CrystalRefill(p,10000,1) && p.CrystalsFree==10 && p.CrystalsPaid==30 && p.ExtraHearts==5,"free-only spend");
            p.CrystalsFree=0;p.CrystalsPaid=29;snapshot=p.Copy();Require(!HeartRules.CrystalRefill(p,10000,1) && Same(p,snapshot),"insufficient crystals");
            p=At(5,997);p.CrystalsPaid=30;Require(!HeartRules.CrystalRefill(p,10000,1) && p.CrystalsPaid==30,"capacity before purchase");
            p=At(5,999);Require(!HeartRules.AdRefill(p,10000,0) && p.DailyLimits.HeartAdsUsed==0,"full storage ad");
            report.Add("PASS grant overflow, spend order, daily named counters, ten ads, crystal price/capacity and free-before-paid");

            long now=10000;int day=1,writes=0;bool fail=false;CampaignProfile saved=null;
            var svc=new CampaignService(Open(3),q=>{if(fail)throw new IOException("injected");writes++;saved=q.Copy();},()=>now,()=>day);
            string run,resume;Require(svc.Begin(3,false,out run) && writes==1 && svc.Profile.Hearts==4 && saved.PendingRun==run && saved.SaveSerial==1,"atomic launch");
            var reloaded=new CampaignService(CampaignStore.Decode(CampaignStore.Encode(saved)),q=>writes++,()=>now,()=>day);
            Require(reloaded.Resume(out resume) && resume==run && writes==1 && reloaded.Profile.Hearts==4 && reloaded.Profile.SaveSerial==1,"resume must not spend/write");
            Require(svc.Abandon(run) && svc.Profile.Hearts==4 && svc.Profile.SaveSerial==2,"abandon refunds");
            Require(svc.Begin(3,false,out run) && svc.Profile.Hearts==3,"restart costs heart");int reward;
            Require(svc.Complete(run,Result(svc,3,false),out reward) && reward==0 && svc.Profile.Hearts==3,"defeat refunds");
            snapshot=svc.Profile.Copy();fail=true;Require(!svc.Begin(3,false,out run) && Same(snapshot,svc.Profile),"launch rollback");
            fail=false;Require(svc.Begin(3,false,out run),"retry launch");snapshot=svc.Profile.Copy();fail=true;
            Require(!svc.Complete(run,Result(svc,3),out reward) && Same(snapshot,svc.Profile),"victory rollback");
            fail=false;Require(svc.Complete(run,Result(svc,3),out reward),"retry victory");long serial=svc.Profile.SaveSerial;int extra=svc.Profile.ExtraHearts;
            Require(svc.Complete(run,Result(svc,3),out reward) && svc.Profile.SaveSerial==serial && svc.Profile.ExtraHearts==extra,"duplicate result");
            var empty=Open(3);empty.Hearts=0;empty.DailyLimits.LocalDay=0;svc=new CampaignService(empty,q=>{throw new Exception("must not persist");},()=>now,()=>day);
            Require(svc.BeginReason(3,false)=="하트 부족" && !svc.Begin(3,false,out run) && svc.Error=="하트 부족" && Same(empty,svc.Profile),"empty launch changed profile");
            now=10900;svc=new CampaignService(empty,null,()=>now,()=>day);Require(svc.Begin(3,false,out run) && svc.Profile.Hearts==0 && svc.Profile.SaveSerial==1,"offline regen before launch");
            report.Add("PASS atomic launch/serial, resume, no refunds, empty launch unchanged, offline regen and failed-save rollback");

            now=10000;
            p=At(0);p.CrystalsPaid=20;p.CrystalsFree=10;fail=false;writes=0;
            svc=new CampaignService(p,q=>{if(fail)throw new IOException("injected refill");writes++;saved=q.Copy();},()=>now,()=>day);
            Require(!svc.Resume(out run) && writes==0,"empty resume");
            snapshot=svc.Profile.Copy();fail=true;
            Require(!svc.RefillHeartByAd() && Same(snapshot,svc.Profile),"ad save rollback");
            Require(!svc.RefillHeartsByCrystals() && Same(snapshot,svc.Profile),"crystal save rollback");
            fail=false;Require(svc.RefillHeartByAd() && writes==1 && saved.Hearts==1 && saved.DailyLimits.HeartAdsUsed==1 && saved.SaveSerial==1,"atomic ad refill");
            Require(svc.RefillHeartsByCrystals() && writes==2 && saved.Hearts==5 && saved.ExtraHearts==1 && saved.CrystalsFree==0 && saved.CrystalsPaid==0 && saved.SaveSerial==2,"atomic crystal refill");
            now=10900;day=2;Require(svc.RefreshHearts() && writes==3 && saved.DailyLimits.HeartAdsUsed==0 && saved.LastSeenUtc==now && saved.SaveSerial==3,"refresh commit/serial");
            report.Add("PASS ad/crystal refill and refresh commit exactly once, with currencies/limits/hearts rolled back on save failure");

            now=10000;
            for(int chapter=1;chapter<=CampaignCatalog.PlayableChapterCount;chapter++){
                int first=CampaignCatalog.ChapterStart(chapter),last=first+CampaignCatalog.ChapterCount(chapter)-1;
                foreach(int stage in new[]{first,last}){
                    int amount=chapter==1 ? (stage==last ? 11 : 1) : 0;
                    int cost=HeartRules.IsFreeLaunch(stage,false) ? 0 : 1;
                    svc=new CampaignService(Open(stage),null,()=>now,()=>day);
                    Require(svc.Begin(stage,false,out run) && svc.Complete(run,Result(svc,stage),out reward),"chapter first win");
                    Require(svc.Profile.Hearts+svc.Profile.ExtraHearts==5-cost+amount,"chapter/last/three-star reward "+chapter+"/"+stage);
                    Require(svc.Begin(stage,false,out run) && svc.Complete(run,Result(svc,stage),out reward) && svc.Profile.Hearts+svc.Profile.ExtraHearts==5-2*cost+amount,"replay reward");
                }
            }
            svc=new CampaignService(At(),null,()=>now,()=>day);Require(svc.Begin(0,false,out run) && svc.Complete(run,Result(svc,0,true,false,.2f),out reward) && svc.Profile.Hearts==5 && svc.Profile.ExtraHearts==1,"low-star first win");
            Require(svc.Begin(0,false,out run) && svc.Complete(run,Result(svc,0),out reward) && svc.Profile.Hearts==5 && svc.Profile.ExtraHearts==1,"later first three-star");
            svc=new CampaignService(Open(6),null,()=>now,()=>day);Require(svc.Begin(0,true,out run) && svc.Complete(run,Result(svc,0,true,true),out reward) && svc.Profile.Hearts==4 && svc.Profile.ExtraHearts==0,"crisis duplicated normal reward");
            report.Add("PASS chapter 1 first clear +1 and last +11, chapter 2/3+ and extra three-star +0, replay and crisis isolation");

            p=Open(CampaignCatalog.Stages.Length);p.MagicCores=4321;p.SelectedHunter=2;p.Muted=true;p.Research=new[]{4,3,2,1};p.Growth=new[]{3,2,1};p.Loadout=7;
            p.StoryMask=123;p.TutorialMask=321;p.CrisisStars[0]=2;p.BestErosion[0]=.25f;p.BestStars[1]=2;
            p.PendingRun="legacy-pending";p.PendingStage=5;p.PendingCrisis=true;p.LastRewardRun="legacy-reward";p.LastReward=17;
            p.StorySeenIds.Add("C01-S001");p.TutorialSeenIds.Add("C10-S240");p.UnmappedProgress.Add("C10-S240",new StageProgress{Stars=2,CrisisStars=1,Erosion=.4f,FirstCleared=true});
            byte[] legacy=V3(p);string directory=Path.Combine(Path.GetTempPath(),"gatefall-hearts-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,"profile.sav");File.WriteAllBytes(path,legacy);var store=new CampaignStore(path);var migrated=store.Load();
            Require(HeartRules.BackfillLimit==6 && migrated.Hearts==5 && migrated.ExtraHearts==6 && migrated.HeartRewardsBackfilled,"v3 backfill capped at six");
            Require(migrated.Supply.Cards.All(n=>n==0) && migrated.Supply.Grades.All(n=>n==1) && migrated.Supply.PremiumPity==0 && migrated.Supply.OwnedCosmetics==0 && migrated.Shop.LocalDay==-1,"v3 supply/shop defaults");
            Require(V3(migrated).SequenceEqual(legacy) && File.ReadAllBytes(path+".legacy-v3.bak").SequenceEqual(legacy),"v3 fields/backup changed");
            Require(CampaignStore.Encode(migrated).SequenceEqual(File.ReadAllBytes(path)),"v4 migration not saved");
            HeartRules.Backfill(migrated);Require(Same(migrated,store.Load()),"backfill repeated");
            migrated.Hearts=2;migrated.ExtraHearts=789;migrated.HeartAnchorUtc=1234567890123;migrated.LastSeenUtc=1234567890456;migrated.SaveSerial=9876543210;
            migrated.CrystalsFree=33;migrated.CrystalsPaid=44;migrated.DailyLimits.LocalDay=20771;migrated.DailyLimits.HeartAdsUsed=9;
            migrated.DailyLimits.NamedCounters.Add("shop-refresh",2);migrated.DailyLimits.NamedCounters.Add("result-ads",5);
            migrated.Supply.Cards[0]=20;migrated.Supply.Grades[0]=4;migrated.Supply.PremiumPity=29;migrated.Supply.OwnedCosmetics=5;
            byte[] bytes=CampaignStore.Encode(migrated);Require(bytes.SequenceEqual(CampaignStore.Encode(CampaignStore.Decode(bytes))),"v4 byte identity");
            var corrupt=(byte[])bytes.Clone();corrupt[corrupt.Length-1]^=1;bool rejected=false;try{CampaignStore.Decode(corrupt);}catch(InvalidDataException){rejected=true;}Require(rejected,"v4 checksum accepted changed heart/daily payload");
            store.Save(migrated);File.WriteAllBytes(path,new byte[]{1,2,3});Require(store.Load().HeartRewardsBackfilled && store.Warning!="","backup recovery");
            p=At(0,998);HeartRules.Backfill(p);Require(p.HeartRewardsBackfilled,"backfill flag");p=Open(CampaignCatalog.Stages.Length);p.ExtraHearts=998;HeartRules.Backfill(p);Require(p.ExtraHearts==999,"backfill cap");
            p=Open(2);HeartRules.Backfill(p);Require(p.ExtraHearts==2,"partial progress backfill");
            p=Open(14);p.Hearts=0;HeartRules.Backfill(p);Require(p.Hearts==5 && p.ExtraHearts==1,"six total, not six plus missing base");
            report.Add("PASS v3 file to v4: empty supply/shop, old-field preservation, backup/backfill, checksum recovery and byte-identical v4 with non-empty supply");

            var invalid=new Action<CampaignProfile>[] {q=>q.Hearts=-1,q=>q.Hearts=6,q=>q.ExtraHearts=-1,q=>q.ExtraHearts=1000,q=>q.CrystalsFree=-1,q=>q.CrystalsPaid=-1,
                q=>q.HeartAnchorUtc=-1,q=>q.LastSeenUtc=-1,q=>q.SaveSerial=-1,q=>q.DailyLimits.LocalDay=-1,q=>q.DailyLimits.HeartAdsUsed=11,q=>q.DailyLimits.NamedCounters.Add("bad",-1)};
            foreach(var change in invalid){p=At();change(p);Require(!p.Valid(),"invalid profile accepted");}
            p=At();p.DailyLimits.NamedCounters.Add("shop",1);snapshot=p.Copy();snapshot.DailyLimits.HeartAdsUsed=2;snapshot.DailyLimits.NamedCounters["shop"]=2;
            Require(p.DailyLimits.HeartAdsUsed==0 && p.DailyLimits.NamedCounters["shop"]==1,"daily copy alias");
            report.Add("PASS validation ranges and independent daily record copy; Core compiles as C# 7.3");
            return string.Join("\n",report);
        }
    }
}
