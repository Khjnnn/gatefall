using System;
using System.IO;

namespace Gatefall.Core
{
    public interface ICloudSaveService
    {
        bool IsSignedIn { get; }
        void Load(Action<byte[],string> completed);
        void Save(byte[] data,string description,Action<string> completed);
    }
    // Synchronous fake. Both storage and callbacks own detached byte arrays.
    public sealed class FakeCloudSaveService : ICloudSaveService
    {
        byte[] stored;
        public bool IsSignedIn { get; set; }=true;
        public string LoadError { get; set; }
        public string SaveError { get; set; }
        public byte[] StoredData { get {return Clone(stored);} set {stored=Clone(value);} }
        public string LastDescription { get; private set; }
        public int LoadCalls { get; private set; }
        public int SaveCalls { get; private set; }
        static byte[] Clone(byte[] data){return data==null ? null : (byte[])data.Clone();}
        public void Load(Action<byte[],string> completed)
        {
            if(completed==null)throw new ArgumentNullException("completed");LoadCalls++;
            string error=!IsSignedIn ? "Not signed in" : LoadError;
            completed(string.IsNullOrEmpty(error) ? Clone(stored) : null,error);
        }
        public void Save(byte[] data,string description,Action<string> completed)
        {
            if(completed==null)throw new ArgumentNullException("completed");SaveCalls++;
            string error=!IsSignedIn ? "Not signed in" : SaveError;
            if(string.IsNullOrEmpty(error) && data==null)error="Missing save data";
            if(string.IsNullOrEmpty(error)){stored=Clone(data);LastDescription=description;}
            completed(error);
        }
    }
    public sealed class CloudSaveSummary
    {
        public readonly string FurthestOpenOperationId;
        public readonly int TotalStars, CrisisStars, MagicCores;
        public readonly long Crystals, SaveSerial, LastSeenUtc;
        public CloudSaveSummary(CampaignProfile profile)
        {
            CloudSync.RequireValid(profile);
            foreach(var stage in CampaignCatalog.Stages)if(profile.IsStageOpen(stage.Index))FurthestOpenOperationId=stage.Id;
            TotalStars=profile.StarsTotal;CrisisStars=profile.CrisisStarsTotal;MagicCores=profile.MagicCores;
            Crystals=(long)profile.CrystalsPaid+profile.CrystalsFree;SaveSerial=profile.SaveSerial;LastSeenUtc=profile.LastSeenUtc;
        }
    }
    public enum CloudSaveComparison { Same, LocalNewer, CloudNewer, Diverged }
    public static class CloudSync
    {
        internal static void RequireValid(CampaignProfile profile)
        {if(profile==null || !profile.Valid())throw new InvalidDataException("Invalid cloud profile");}
        public static CloudSaveComparison Compare(CampaignProfile local,CampaignProfile cloud)
        {return Compare(CampaignStore.Encode(local),CampaignStore.Encode(cloud));}
        public static CloudSaveComparison Compare(byte[] local,byte[] cloud)
        {
            var a=CampaignStore.Decode(local);var b=CampaignStore.Decode(cloud);
            if(a.CommitHash==b.CommitHash)return CloudSaveComparison.Same;
            // Legacy decoding starts a new root each time; identical validated snapshots still match.
            bool equal=local.Length==cloud.Length;
            for(int i=0;equal && i<local.Length;i++)equal=local[i]==cloud[i];
            if(equal)return CloudSaveComparison.Same;
            if(a.AncestorHashes.Contains(b.CommitHash))return CloudSaveComparison.LocalNewer;
            if(b.AncestorHashes.Contains(a.CommitHash))return CloudSaveComparison.CloudNewer;
            return CloudSaveComparison.Diverged;
        }
        public static bool TryDecode(byte[] data,out CampaignProfile profile,out string error)
        {
            profile=null;error="";
            try{profile=CampaignStore.Decode(data);return true;}
            catch(Exception e){error=e.Message;return false;}
        }
        public static CampaignProfile Resolve(CampaignProfile chosen,CampaignProfile other)
        {
            RequireValid(chosen);RequireValid(other);var result=chosen.Copy();
            long paid=chosen.CrystalsPaid;bool changed=false;
            foreach(var order in other.Daily.PurchaseLedger){
                PurchaseEntry existing;var entry=order.Value;
                if(result.Daily.PurchaseLedger.TryGetValue(order.Key,out existing)){
                    if(existing.Kind!=entry.Kind || existing.Amount!=entry.Amount || existing.Season!=entry.Season)throw new InvalidDataException("Conflicting purchase order");
                    continue;
                }
                result.Daily.PurchaseLedger.Add(order.Key,entry);changed=true;
                if(entry.Kind==PurchaseKind.Crystals)paid+=entry.Amount;
            }
            if(paid>int.MaxValue)throw new InvalidDataException("Paid crystal limit");result.CrystalsPaid=(int)paid;
            // Supply cosmetics are earned rewards, not recorded purchase entitlements.
            RequireValid(result);if(changed)SaveLineage.Advance(result);return result;
        }
    }
    public enum CloudUploadReason { None, VictoryCommit, Purchase, BoxOpening, ReturningHome, Battle }
    public sealed class CloudUploadPolicy
    {
        public const int CoalesceSeconds=30;
        readonly Func<long> utcClock;
        long lastUpload=-1;
        bool pending;
        public bool IsInBattle { get; set; }
        public CloudUploadPolicy(Func<long> utcClock)
        {if(utcClock==null)throw new ArgumentNullException("utcClock");this.utcClock=utcClock;}
        // True reserves an upload attempt, including failed attempts. Poll None to flush queued work.
        // The caller supplies the latest committed snapshot and sets IsInBattle before battle entry.
        public bool ShouldUpload(CloudUploadReason reason)
        {
            switch(reason){
                case CloudUploadReason.VictoryCommit:case CloudUploadReason.Purchase:
                case CloudUploadReason.BoxOpening:case CloudUploadReason.ReturningHome:pending=true;break;
                case CloudUploadReason.None:break;
                default:return false;
            }
            if(IsInBattle || !pending)return false;
            long now=utcClock();if(now<0)throw new ArgumentOutOfRangeException("utcClock");
            if(lastUpload>=0 && (now<lastUpload || now-lastUpload<CoalesceSeconds))return false;
            lastUpload=now;pending=false;return true;
        }
    }
}
