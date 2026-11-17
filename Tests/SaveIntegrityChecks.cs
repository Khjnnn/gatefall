using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class SaveIntegrityChecks
    {
        static void Require(bool ok,string why){if(!ok)throw new Exception("Save integrity: "+why);}
        static byte[] Encode(CampaignProfile p){return CampaignStore.Encode(p);}
        static bool Same(CampaignProfile a,CampaignProfile b){return Encode(a).SequenceEqual(Encode(b));}
        public static string Run()
        {
            var report=new List<string>();var root=new CampaignProfile();var snapshots=new List<CampaignProfile>{root.Copy()};
            bool fail=false;CampaignProfile saved=null;
            var service=new CampaignService(root,p=>{saved=p.Copy();if(fail)throw new IOException("injected");},()=>100000,()=>56);
            for(int i=0;i<CampaignProfile.AncestorLimit+1;i++){
                var before=service.Profile.Copy();Require(service.SetMuted(i%2==0),"commit");var p=service.Profile;
                Require(p.CommitHash!=before.CommitHash && p.AncestorHashes[0]==before.CommitHash && Same(p,saved),"persisted hash/parent");
                Require(p.AncestorHashes.Count==Math.Min(i+1,CampaignProfile.AncestorLimit),"history bound");
                snapshots.Add(CampaignStore.Decode(Encode(p)));
            }
            var latest=snapshots.Last();Require(CloudSync.Compare(latest,snapshots[1])==CloudSaveComparison.LocalNewer &&
                CloudSync.Compare(snapshots[1],latest)==CloudSaveComparison.CloudNewer,"oldest retained ancestor");
            Require(CloudSync.Compare(root,latest)==CloudSaveComparison.Diverged,"evicted ancestor is unproven");
            Require(Encode(latest).Length-Encode(root).Length<2300,"bounded lineage storage cost");
            var detached=latest.Copy();detached.AncestorHashes.Clear();Require(latest.AncestorHashes.Count==32,"detached history");
            fail=true;var previous=service.Profile.Copy();Require(!service.SetMuted(false) && Same(previous,service.Profile),"failed commit rolls back lineage");
            fail=false;Require(service.SetMuted(false) && service.Profile.AncestorHashes[0]==previous.CommitHash,"retry descends from committed parent");
            previous=service.Profile.Copy();service.DailyStatus();service.BeginReason(0,false);Require(Same(previous,service.Profile),"queries do not advance lineage");
            var fork=new CampaignService(snapshots[1],p=>{},()=>100000,()=>56);Require(fork.SetMuted(!snapshots[2].Muted),"branch commit");
            Require(CloudSync.Compare(fork.Profile,snapshots[2])==CloudSaveComparison.Diverged,"same-profile branch divergence");
            report.Add("PASS every commit persists hash/parent, 32 ancestor bound/eviction, reload comparison, branches, detached copies, query and failed-save rollback");

            var a=new CampaignService(new CampaignProfile(),p=>{},()=>100000,()=>56);
            Require(a.GrantCrystalPurchase("shared",60) && a.GrantPassPurchase("season3"),"purchase commits");
            Require(a.Profile.Daily.PurchaseLedger["shared"].Kind==PurchaseKind.Crystals && a.Profile.Daily.PurchaseLedger["shared"].Amount==60 &&
                a.Profile.Daily.PurchaseLedger["season3"].Season==3,"typed grant metadata");
            var left=a.Profile.Copy();left.CrystalsPaid=5;var right=a.Profile.Copy();right.CrystalsPaid=0;
            var other=new CampaignService(right,p=>{},()=>100000,()=>84);Require(other.GrantCrystalPurchase("other",330) && other.GrantPassPurchase("season4"),"independent purchases");
            var merged=CloudSync.Resolve(left,other.Profile);
            Require(merged.CrystalsPaid==335 && merged.Daily.Season==3 && merged.Daily.HasPass(3) && merged.Daily.HasPass(4),"spent balance plus only missing order");
            Require(Same(merged,CloudSync.Resolve(CampaignStore.Decode(Encode(merged)),other.Profile)),"merge reload retry idempotent");
            var next=new CampaignService(merged,p=>{},()=>100000,()=>84);Require(next.RefreshHearts() && next.Profile.Daily.PassPurchased,"restored entitlement activates in its season");
            previous=next.Profile.Copy();Require(next.GrantCrystalPurchase("other",999) && Same(previous,next.Profile),"restored crystal order never credited twice");
            Require(next.GrantPassPurchase("season3") && Same(previous,next.Profile),"old pass replay never grants current season");
            var entry=new PurchaseEntry("non-consumable",PurchaseKind.OtherNonConsumable,0);left.Daily.PurchaseLedger.Add(entry.OrderId,entry);
            Require(CloudSync.Resolve(other.Profile,left).Daily.PurchaseLedger[entry.OrderId].Kind==PurchaseKind.OtherNonConsumable,"other entitlement retained");
            report.Add("PASS purchase metadata, overlapping/disjoint spent credits, reload/retry exactly once, multi-season entitlement refresh and other non-consumable union");

            foreach(var mutate in new Action<CampaignProfile>[] {
                p=>p.CommitHash="bad",p=>p.AncestorHashes.Add(p.CommitHash),p=>p.AncestorHashes.Add(null),
                p=>p.AncestorHashes=Enumerable.Repeat(new string('a',64),33).ToList(),
                p=>p.Daily.PurchaseLedger.Add("x",null),p=>p.Daily.PurchaseLedger.Add("x",new PurchaseEntry("y",PurchaseKind.Crystals,1)),
                p=>p.Daily.PurchaseLedger.Add("x",new PurchaseEntry("x",(PurchaseKind)99,1)),
                p=>p.Daily.PurchaseLedger.Add("x",new PurchaseEntry("x",PurchaseKind.Crystals,0)),
                p=>p.Daily.PurchaseLedger.Add("x",new PurchaseEntry("x",PurchaseKind.Pass,0,0)),
                p=>p.Daily.PurchaseLedger.Add("x",new PurchaseEntry("x",PurchaseKind.OtherNonConsumable,1))}){
                var bad=new CampaignProfile();mutate(bad);Require(!bad.Valid(),"invalid lineage/ledger rejected");
                bool rejected=false;try{Encode(bad);}catch(InvalidDataException){rejected=true;}Require(rejected,"invalid encode rejected");
            }
            report.Add("PASS invalid hashes/history, null/mismatched orders, unknown kinds, zero crystal amount and invalid pass/non-consumable metadata rejected");
            return string.Join("\n",report);
        }
    }
}
