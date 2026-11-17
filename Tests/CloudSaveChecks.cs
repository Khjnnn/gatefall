using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class CloudSaveChecks
    {
        const int Epoch=DailyCatalog.SeasonEpochDay;
        static void Require(bool pass,string why){if(!pass)throw new Exception("Cloud save: "+why);}
        static byte[] Encode(CampaignProfile p){return CampaignStore.Encode(p);}
        static bool Same(CampaignProfile a,CampaignProfile b){return Encode(a).SequenceEqual(Encode(b));}
        static void Reject(Action action,string why)
        {bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Require(rejected,why);}
        static CampaignProfile Progress(int count,int seed)
        {
            var p=new CampaignProfile{ShopSeed=seed,SaveSerial=count,LastSeenUtc=1000+count,HeartAnchorUtc=990,
                Hearts=2,ExtraHearts=17,MagicCores=80+count,CrystalsFree=37,CrystalsPaid=10,
                PendingRun="pending",PendingStage=0,LastRewardRun="reward",LastReward=12,HeartRewardsBackfilled=true,
                StoryMask=3,TutorialMask=1,Muted=true,SelectedHunter=1};
            for(int i=0;i<count;i++){p.BestStars[i]=3;p.FirstCleared[i]=true;p.BestErosion[i]=.25f;}
            p.CrisisStars[0]=2;p.Research[0]=1;p.Growth[0]=1;p.StorySeenIds.Add("C01-S001");p.TutorialSeenIds.Add("C01-S002");
            p.UnmappedProgress.Add("C99-S001",new StageProgress{Stars=2,FirstCleared=true,Erosion=.5f});
            p.DailyLimits.LocalDay=Epoch+56;p.DailyLimits.HeartAdsUsed=3;p.DailyLimits.NamedCounters.Add("normal-boxes",2);
            p.Supply.Cards[0]=12;p.Supply.Grades[0]=2;p.Supply.PremiumPity=4;p.Supply.OwnedCosmetics=1;
            p.Shop.Generate(p,Epoch+56,1);p.Shop.PurchasedMask=1;p.Shop.HeartsBought=2;
            p.Daily.LocalDay=Epoch+56;p.Daily.Season=3;p.Daily.Progress[0]=3;p.Daily.MissionClaims=1;
            p.Daily.AttendanceDay=Epoch+56;p.Daily.AttendancePosition=2;p.Daily.RouletteSpins=1;p.Daily.LastSpinUtc=900;
            p.Daily.RewardDoubles=1;p.Daily.DoubledRun="reward";p.Daily.NormalTickets=2;p.Daily.PremiumTickets=3;
            p.Daily.PassPoints=100;p.Daily.PointsToday=5;p.Daily.FreeClaims=1;p.Daily.CapRaised=true;
            Require(p.Valid(),"fixture valid");return p;
        }
        static byte[] Masked(CampaignProfile p)
        {
            var copy=p.Copy();copy.CrystalsPaid=0;copy.Daily.PurchaseLedger.Clear();
            if(copy.Daily.PaidClaims!=0)Order(copy,"masked-pass",PurchaseKind.Pass,0,copy.Daily.Season);
            copy.CommitHash=new string('0',64);copy.AncestorHashes.Clear();return Encode(copy);
        }
        static void Order(CampaignProfile p,string id,PurchaseKind kind,int amount=0,int season=0)
        {p.Daily.PurchaseLedger.Add(id,new PurchaseEntry(id,kind,amount,season));}
        static CampaignProfile Commit(CampaignProfile p)
        {var service=new CampaignService(p,x=>{},()=>1000,()=>Epoch+56);Require(service.SetMuted(p.Muted),"commit fixture");return service.Profile;}
        static CampaignProfile Merge(CampaignProfile chosen,CampaignProfile other)
        {
            byte[] a=Encode(chosen),b=Encode(other);var result=CloudSync.Resolve(chosen,other);
            Require(result.Valid() && !ReferenceEquals(result,chosen),"valid detached result");
            Require(result.Daily.PurchaseLedger.Keys.OrderBy(x=>x).SequenceEqual(chosen.Daily.PurchaseLedger.Keys.Union(other.Daily.PurchaseLedger.Keys).OrderBy(x=>x)),"order union");
            long extra=other.Daily.PurchaseLedger.Values.Where(x=>!chosen.Daily.PurchaseLedger.ContainsKey(x.OrderId) && x.Kind==PurchaseKind.Crystals).Sum(x=>(long)x.Amount);
            Require(result.CrystalsPaid==chosen.CrystalsPaid+extra,"chosen paid balance plus missing orders");
            Require(result.Daily.PassPurchased==result.Daily.HasPass(chosen.Daily.Season),"season ledger entitlement");
            Require(Masked(result).SequenceEqual(Masked(chosen)),"chosen state byte-for-byte with purchases and lineage masked");
            Require(a.SequenceEqual(Encode(chosen)) && b.SequenceEqual(Encode(other)),"inputs untouched");
            Require(Same(result,CloudSync.Resolve(result,other)),"repeated merge idempotent");
            Require(Same(result,CampaignStore.Decode(Encode(result))),"merged round trip");return result;
        }
        static byte[] ChangedHeader(byte[] bytes,bool version)
        {
            bytes=(byte[])bytes.Clone();long offset;
            using(var m=new MemoryStream(bytes))using(var r=new BinaryReader(m,Encoding.UTF8)){
                m.Position=32;r.ReadString();offset=m.Position+(version ? 0 : 4);}
            Buffer.BlockCopy(BitConverter.GetBytes(version ? 5 : -1),0,bytes,(int)offset,4);
            using(var sha=SHA256.Create())Buffer.BlockCopy(sha.ComputeHash(bytes,32,bytes.Length-32),0,bytes,0,32);
            return bytes;
        }
        public static string Run()
        {
            var report=new List<string>();var a=Commit(Progress(3,7));var b=a.Copy();
            Require(CloudSync.Compare(a,b)==CloudSaveComparison.Same,"same commit");
            Require(CloudSync.Compare(Encode(a),Encode(b))==CloudSaveComparison.Same,"same bytes");
            b=Commit(a);Require(CloudSync.Compare(a,b)==CloudSaveComparison.CloudNewer && CloudSync.Compare(b,a)==CloudSaveComparison.LocalNewer,"proven ancestor");
            var fork=a.Copy();fork.MagicCores++;fork=Commit(fork);
            Require(CloudSync.Compare(b,fork)==CloudSaveComparison.Diverged,"sibling branches");
            b=Progress(4,9);b.SaveSerial=1000;b.LastSeenUtc=999999;
            Require(CloudSync.Compare(a,b)==CloudSaveComparison.Diverged,"serial/time/progress cannot prove ancestry");
            var fresh=new CampaignProfile();Require(CloudSync.Compare(a,fresh)==CloudSaveComparison.Diverged,"unrelated fresh lineage");
            Require(CloudSync.Compare(fresh,fresh.Copy())==CloudSaveComparison.Same,"fresh copied root");
            var summary=new CloudSaveSummary(a);a.CrystalsPaid=int.MaxValue;a.CrystalsFree=int.MaxValue;
            var large=new CloudSaveSummary(a);
            Require(summary.FurthestOpenOperationId==CampaignCatalog.Stages[3].Id && summary.TotalStars==9 && summary.CrisisStars==2 &&
                summary.MagicCores==83 && summary.Crystals==47 && summary.SaveSerial==4 && summary.LastSeenUtc==1003,"chooser summary");
            Require(large.Crystals==2L*int.MaxValue && summary.Crystals==47,"summary overflow and snapshot");
            report.Add("PASS hash equality, proven ancestry both directions, divergent branches/unrelated roots and chooser summary");

            a=Progress(3,7);b=Progress(4,9);a.CrystalsPaid=20;b.CrystalsPaid=90;
            foreach(var p in new[]{a,b})Order(p,"shared",PurchaseKind.Crystals,60);
            Order(a,"local",PurchaseKind.Crystals,330);Order(a,"Case",PurchaseKind.OtherNonConsumable);
            Order(b,"cloud",PurchaseKind.Crystals,700);Order(b,"case",PurchaseKind.OtherNonConsumable);
            Order(b,"pass3",PurchaseKind.Pass,0,3);b.Daily.PaidClaims=1;b.CrystalsFree=400;b.ExtraHearts=30;b.Supply.OwnedCosmetics=6;
            var merged=Merge(a,b);Merge(b,a);Require(merged.CrystalsPaid==720 && merged.Daily.PurchaseLedger.Count==6,"overlap counted once and disjoint credited");
            Require(CloudSync.Compare(merged,a)==CloudSaveComparison.LocalNewer,"merge advances chosen lineage");
            Order(merged,"detached",PurchaseKind.OtherNonConsumable);merged.Daily.Progress[0]=0;merged.Supply.Cards[0]=99;merged.Shop.CardKinds[0]=1;
            Require(!a.Daily.PurchaseLedger.ContainsKey("detached") && a.Daily.Progress[0]==3 && a.Supply.Cards[0]==12 && !b.Daily.PurchaseLedger.ContainsKey("detached"),"deep copy");
            a=Progress(3,7);b=Progress(4,9);a.CrystalsPaid=5;b.CrystalsPaid=9000;
            Require(Merge(a,b).CrystalsPaid==5,"no larger balance fallback without orders");
            Order(b,"old-pass",PurchaseKind.Pass,0,3);b.Daily.LocalDay=Epoch+84;b.Daily.Season=4;
            Require(Merge(a,b).Daily.PassPurchased && !Merge(b,a).Daily.PassPurchased,"pass only its recorded season");
            a.Daily.LocalDay=Epoch+112;a.Daily.Season=5;Require(!Merge(a,b).Daily.PassPurchased,"old season never unlocks chosen current season");
            Order(b,"future-pass",PurchaseKind.Pass,0,5);Require(Merge(a,b).Daily.PassPurchased,"matching chosen season grants pass");
            var full=a.Copy();for(int i=0;i<4096;i++)Order(full,"order-"+i,PurchaseKind.OtherNonConsumable);
            var extra=b.Copy();extra.Daily.PurchaseLedger.Clear();Order(extra,"extra",PurchaseKind.OtherNonConsumable);
            byte[] before=Encode(full),otherBefore=Encode(extra);
            Reject(()=>CloudSync.Resolve(full,extra),"oversize union rejected");
            Require(before.SequenceEqual(Encode(full)) && otherBefore.SequenceEqual(Encode(extra)),"oversize union preserves originals");
            full=Progress(3,7);full.CrystalsPaid=int.MaxValue;extra=Progress(4,9);Order(extra,"overflow",PurchaseKind.Crystals,1);
            Reject(()=>CloudSync.Resolve(full,extra),"paid overflow rejected");
            Order(full,"conflict",PurchaseKind.Crystals,60);Order(extra,"conflict",PurchaseKind.Pass,0,3);
            Reject(()=>CloudSync.Resolve(full,extra),"conflicting order metadata rejected");
            var invalid=a.Copy();invalid.Hearts=6;Reject(()=>CloudSync.Resolve(a,invalid),"invalid other");
            Reject(()=>CloudSync.Resolve(invalid,a),"invalid chosen");Reject(()=>new CloudSaveSummary(invalid),"invalid summary");
            report.Add("PASS purchase ledger union, exact missing credits, spent balance, seasonal passes, conflicts/overflow/limits, immutable entries and idempotence");

            var local=Progress(3,7);before=Encode(local);var good=Encode(b);var corrupt=(byte[])good.Clone();corrupt[corrupt.Length-1]^=1;
            var fake=new FakeCloudSaveService();ICloudSaveService service=fake;
            foreach(var bytes in new[]{null,new byte[0],new byte[64],new byte[1048577],corrupt,good.Take(70).ToArray(),ChangedHeader(good,false),ChangedHeader(good,true)}){
                CampaignProfile loaded;string error;
                Require(!CloudSync.TryDecode(bytes,out loaded,out error) && loaded==null && !string.IsNullOrEmpty(error),"reject malformed/future cloud");
                fake.StoredData=bytes;int callbacks=0;
                service.Load((data,loadError)=>{callbacks++;Require(string.IsNullOrEmpty(loadError),"transport success");
                    Require(!CloudSync.TryDecode(data,out loaded,out error) && loaded==null,"loaded cloud validation");});
                Require(callbacks==1 && before.SequenceEqual(Encode(local)),"bad cloud leaves local untouched");
            }
            CampaignProfile decoded;string decodeError;Require(CloudSync.TryDecode(good,out decoded,out decodeError) && decodeError=="" && Same(decoded,b),"valid decode");
            Reject(()=>CloudSync.Compare(before,corrupt),"compare rejects corrupt cloud");Reject(()=>CloudSync.Compare(corrupt,corrupt),"equal corrupt bytes not Same");
            report.Add("PASS null/truncated/checksum-invalid/value-invalid/future/oversize cloud rejected; local bytes untouched; valid decode and comparison validation");

            fake=new FakeCloudSaveService();service=fake;int calls=0;
            service.Load((data,error)=>{calls++;Require(data==null && string.IsNullOrEmpty(error),"empty storage");});
            fake.StoredData=good;good[0]^=1;Require(fake.StoredData[0]!=good[0],"stored setter cloned");good[0]^=1;
            var read=fake.StoredData;read[0]^=1;Require(fake.StoredData.SequenceEqual(good),"stored getter cloned");
            service.Load((data,error)=>{calls++;Require(data.SequenceEqual(good) && string.IsNullOrEmpty(error),"fake load");data[0]^=1;});
            Require(fake.StoredData.SequenceEqual(good),"callback cloned");
            fake.IsSignedIn=false;service.Load((data,error)=>{calls++;Require(data==null && !string.IsNullOrEmpty(error),"signed-out load");});
            service.Save(before,"signed out",error=>{calls++;Require(!string.IsNullOrEmpty(error),"signed-out save");});
            fake.IsSignedIn=true;fake.LoadError="load failure";
            service.Load((data,error)=>{calls++;Require(data==null && error=="load failure","load failure");});
            fake.SaveError="save failure";service.Save(before,"failure",error=>{calls++;Require(error=="save failure","save failure");});
            Require(fake.StoredData.SequenceEqual(good) && fake.LastDescription==null,"failed saves preserve storage");
            fake.SaveError=null;service.Save(null,"missing",error=>{calls++;Require(!string.IsNullOrEmpty(error),"null save");});
            Require(fake.StoredData.SequenceEqual(good),"null save preserves storage");
            var uploaded=(byte[])before.Clone();service.Save(uploaded,"victory",error=>{calls++;Require(string.IsNullOrEmpty(error),"save success");});uploaded[0]^=1;
            Require(fake.StoredData.SequenceEqual(before) && fake.LastDescription=="victory" && calls==8 && fake.LoadCalls==4 && fake.SaveCalls==4,"fake callbacks and save clone");
            report.Add("PASS fake signed-out/load/save failures, missing snapshot, exact callbacks, storage preservation and byte ownership");

            long now=0;var policy=new CloudUploadPolicy(()=>now);
            Require(!policy.ShouldUpload(CloudUploadReason.None) && !policy.ShouldUpload(CloudUploadReason.Battle),"no idle/battle upload");
            Require(policy.ShouldUpload(CloudUploadReason.VictoryCommit),"first upload at zero");
            foreach(var reason in new[]{CloudUploadReason.Purchase,CloudUploadReason.BoxOpening,CloudUploadReason.ReturningHome})Require(!policy.ShouldUpload(reason),"burst coalesced");
            now=29;Require(!policy.ShouldUpload(CloudUploadReason.None),"29 second boundary");now=30;
            Require(policy.ShouldUpload(CloudUploadReason.None) && !policy.ShouldUpload(CloudUploadReason.None),"flush exactly once at 30");
            now=0;Require(!policy.ShouldUpload(CloudUploadReason.Purchase),"backward clock");now=59;
            Require(!policy.ShouldUpload(CloudUploadReason.None),"recover clock below boundary");now=60;Require(policy.ShouldUpload(CloudUploadReason.None),"recovered pending upload");
            policy.IsInBattle=true;now=100;
            foreach(var reason in new[]{CloudUploadReason.VictoryCommit,CloudUploadReason.Purchase,CloudUploadReason.BoxOpening,CloudUploadReason.ReturningHome,CloudUploadReason.None})Require(!policy.ShouldUpload(reason),"never during battle");
            policy.IsInBattle=false;Require(policy.ShouldUpload(CloudUploadReason.None),"battle work flushed once after exit");
            now=130;Require(!policy.ShouldUpload(CloudUploadReason.Battle) && !policy.ShouldUpload((CloudUploadReason)999) && !policy.ShouldUpload(CloudUploadReason.None),"unsupported reasons do not queue");
            foreach(var reason in new[]{CloudUploadReason.Purchase,CloudUploadReason.BoxOpening,CloudUploadReason.ReturningHome}){
                Require(policy.ShouldUpload(reason),"eligible reason");now+=30;}
            now=long.MaxValue;Require(policy.ShouldUpload(CloudUploadReason.Purchase),"clock upper bound");
            Require(!policy.ShouldUpload(CloudUploadReason.ReturningHome),"upper bound coalesces without overflow");
            report.Add("PASS upload reasons, pending burst flush, 30-second boundary, backward clock, battle exclusion and overflow boundary");
            return string.Join("\n",report);
        }
    }
}
