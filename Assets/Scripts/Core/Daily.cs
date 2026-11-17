using System;
using System.IO;
using System.Collections.Generic;

namespace Gatefall.Core
{
    public enum DailyMission { Wins, ThreeStars, Fusions, Crisis, Box, Ad }
    public enum DailyRewardKind { MagicCores, Hearts, NormalTicket, PremiumTicket, FreeCrystals, Cosmetic }
    public sealed class DailyReward
    {
        public readonly DailyRewardKind Kind;
        public readonly int Amount;
        public DailyReward(DailyRewardKind kind,int amount){Kind=kind;Amount=amount;}
    }
    public sealed class MissionOffer
    {
        public readonly DailyMission Mission;
        public readonly int Target, MagicCores, PassPoints;
        public MissionOffer(DailyMission mission,int target,int cores,int points)
        {Mission=mission;Target=target;MagicCores=cores;PassPoints=points;}
    }
    public sealed class RouletteSlot
    {
        public readonly DailyReward Reward;
        public readonly int Weight;
        public decimal Percent {get {return Weight/100m;}}
        public RouletteSlot(DailyRewardKind kind,int amount,int weight){Reward=new DailyReward(kind,amount);Weight=weight;}
    }
    public sealed class PassTier
    {
        public readonly int Tier;
        public readonly DailyReward Free, Paid;
        public PassTier(int tier,DailyReward free,DailyReward paid){Tier=tier;Free=free;Paid=paid;}
    }
    public static class DailyCatalog
    {
        public const int SeasonEpochDay=0, SeasonDays=28, TierCount=30, PointsPerTier=50;
        public const int VictoryPoints=5, DailyPointCap=60, CapCrystalPrice=30;
        public const int RouletteDailyLimit=3, RouletteCooldownSeconds=14400, DoubleDailyLimit=5;
        static readonly MissionOffer[] missions={new MissionOffer(DailyMission.Wins,3,3,5),new MissionOffer(DailyMission.ThreeStars,1,3,5),
            new MissionOffer(DailyMission.Fusions,2,3,5),new MissionOffer(DailyMission.Crisis,1,3,5),
            new MissionOffer(DailyMission.Box,1,3,5),new MissionOffer(DailyMission.Ad,1,3,5)};
        static readonly DailyReward[] attendance={new DailyReward(DailyRewardKind.Hearts,1),new DailyReward(DailyRewardKind.MagicCores,5),
            new DailyReward(DailyRewardKind.Hearts,1),new DailyReward(DailyRewardKind.MagicCores,5),new DailyReward(DailyRewardKind.Hearts,2),
            new DailyReward(DailyRewardKind.MagicCores,10),new DailyReward(DailyRewardKind.PremiumTicket,1)};
        // Ten thousand integer outcomes; this same table supplies rolls and exact disclosure percentages.
        static readonly RouletteSlot[] roulette={new RouletteSlot(DailyRewardKind.MagicCores,10,4500),new RouletteSlot(DailyRewardKind.MagicCores,20,2000),
            new RouletteSlot(DailyRewardKind.MagicCores,30,700),new RouletteSlot(DailyRewardKind.MagicCores,40,200),new RouletteSlot(DailyRewardKind.MagicCores,50,100),
            new RouletteSlot(DailyRewardKind.Hearts,1,1200),new RouletteSlot(DailyRewardKind.Hearts,2,500),new RouletteSlot(DailyRewardKind.Hearts,3,200),
            new RouletteSlot(DailyRewardKind.NormalTicket,1,500),new RouletteSlot(DailyRewardKind.FreeCrystals,5,100)};
        static readonly PassTier[] tiers=BuildTiers();
        static PassTier[] BuildTiers()
        {
            var rows=new PassTier[TierCount];for(int tier=1;tier<=TierCount;tier++){
                var free=tier%10==0 ? new DailyReward(DailyRewardKind.NormalTicket,1) : tier%3==0 ? new DailyReward(DailyRewardKind.MagicCores,5) : new DailyReward(DailyRewardKind.Hearts,1);
                var paid=tier%10==0 ? new DailyReward(DailyRewardKind.PremiumTicket,2) : tier==15 ? new DailyReward(DailyRewardKind.Cosmetic,0) : new DailyReward(DailyRewardKind.FreeCrystals,10);
                rows[tier-1]=new PassTier(tier,free,paid);
            }return rows;
        }
        public static MissionOffer[] Missions(){return (MissionOffer[])missions.Clone();}
        public static DailyReward[] Attendance(){return (DailyReward[])attendance.Clone();}
        public static RouletteSlot[] RouletteDisclosure(){return (RouletteSlot[])roulette.Clone();}
        public static PassTier[] PassRewards(){return (PassTier[])tiers.Clone();}
        public static int SeasonNumber(int localDay)
        {if(localDay<SeasonEpochDay)throw new ArgumentOutOfRangeException("localDay");return (localDay-SeasonEpochDay)/SeasonDays+1;}
        internal static RouletteSlot Roll(Func<int,int> rng)
        {
            if(rng==null)throw new ArgumentNullException("rng");int roll=rng(10000);
            if(roll<0 || roll>=10000)throw new ArgumentOutOfRangeException("rng");
            foreach(var slot in roulette){if(roll<slot.Weight)return slot;roll-=slot.Weight;}throw new InvalidOperationException("잘못된 룰렛 확률");
        }
    }
    public enum PurchaseKind { Crystals, Pass, OtherNonConsumable }
    public sealed class PurchaseEntry
    {
        public readonly string OrderId;
        public readonly PurchaseKind Kind;
        public readonly int Amount, Season;
        public PurchaseEntry(string orderId,PurchaseKind kind,int amount,int season=0)
        {OrderId=orderId;Kind=kind;Amount=amount;Season=season;}
        public bool Valid()
        {return DailyState.ValidOrderId(OrderId) && (Kind==PurchaseKind.Crystals ? Amount>0 && Season==0 :
            Kind==PurchaseKind.Pass ? Amount==0 && Season>0 && Season<=DailyCatalog.SeasonNumber(int.MaxValue) :
            Kind==PurchaseKind.OtherNonConsumable && Amount==0 && Season==0);}
    }
    public sealed class DailyState
    {
        public int LocalDay=-1, MissionClaims;
        public int[] Progress=new int[6];
        public int AttendancePosition, AttendanceDay=-1;
        public int RouletteSpins, RewardDoubles;
        public long LastSpinUtc=-1;
        public string DoubledRun="";
        public int NormalTickets, PremiumTickets;
        public int Season, PassPoints, PointsToday, FreeClaims, PaidClaims;
        public bool CapRaised;
        public bool PassPurchased {get {return HasPass(Season);}}
        // Shared across all IAP products and retained across seasons. Order IDs are opaque, case sensitive.
        public Dictionary<string,PurchaseEntry> PurchaseLedger=new Dictionary<string,PurchaseEntry>(StringComparer.Ordinal);
        public bool HasPass(int season)
        {foreach(var entry in PurchaseLedger.Values)if(entry.Kind==PurchaseKind.Pass && entry.Season==season)return true;return false;}
        public DailyState Copy()
        {var p=(DailyState)MemberwiseClone();p.Progress=(int[])Progress.Clone();p.PurchaseLedger=new Dictionary<string,PurchaseEntry>(PurchaseLedger,StringComparer.Ordinal);return p;}
        public static bool ValidOrderId(string id)
        {if(string.IsNullOrWhiteSpace(id) || id.Length>256)return false;foreach(char c in id)if(char.IsControl(c))return false;return true;}
        public bool Valid()
        {
            if(LocalDay< -1 || MissionClaims<0 || MissionClaims>63 || Progress==null || Progress.Length!=6 ||
                AttendancePosition<0 || AttendancePosition>6 || AttendanceDay< -1 || AttendanceDay>LocalDay ||
                RouletteSpins<0 || RouletteSpins>3 || RewardDoubles<0 || RewardDoubles>5 || LastSpinUtc< -1 ||
                DoubledRun==null || DoubledRun.Length>64 || NormalTickets<0 || PremiumTickets<0 || Season<0 ||
                Season!=(LocalDay<0 ? 0 : DailyCatalog.SeasonNumber(LocalDay)) || PassPoints<0 || PassPoints>DailyCatalog.TierCount*DailyCatalog.PointsPerTier ||
                PointsToday<0 || PointsToday>(CapRaised ? 2 : 1)*DailyCatalog.DailyPointCap || FreeClaims<0 || PaidClaims<0 ||
                (FreeClaims>>30)!=0 || (PaidClaims>>30)!=0 || PurchaseLedger==null || PurchaseLedger.Count>4096)return false;
            foreach(var order in PurchaseLedger)if(order.Value==null || order.Key!=order.Value.OrderId || !order.Value.Valid())return false;
            int unlocked=PassPoints/DailyCatalog.PointsPerTier, mask=(1<<unlocked)-1;
            if((FreeClaims&~mask)!=0 || (PaidClaims&~mask)!=0 || !PassPurchased && PaidClaims!=0)return false;
            var offers=DailyCatalog.Missions();for(int i=0;i<6;i++)if(Progress[i]<0 || Progress[i]>offers[i].Target ||
                (MissionClaims&(1<<i))!=0 && Progress[i]!=offers[i].Target)return false;
            return true;
        }
        internal void Write(BinaryWriter w)
        {
            w.Write(LocalDay);w.Write(MissionClaims);foreach(int n in Progress)w.Write(n);
            w.Write(AttendancePosition);w.Write(AttendanceDay);w.Write(RouletteSpins);w.Write(RewardDoubles);w.Write(LastSpinUtc);w.Write(DoubledRun);
            w.Write(NormalTickets);w.Write(PremiumTickets);w.Write(Season);w.Write(PassPoints);w.Write(PointsToday);
            w.Write(FreeClaims);w.Write(PaidClaims);w.Write(CapRaised);
            var ids=new List<string>(PurchaseLedger.Keys);ids.Sort(StringComparer.Ordinal);w.Write(ids.Count);
            foreach(string id in ids){var entry=PurchaseLedger[id];w.Write(id);w.Write((int)entry.Kind);w.Write(entry.Amount);w.Write(entry.Season);}
        }
        internal static DailyState Read(BinaryReader r)
        {
            var p=new DailyState{LocalDay=r.ReadInt32(),MissionClaims=r.ReadInt32()};for(int i=0;i<6;i++)p.Progress[i]=r.ReadInt32();
            p.AttendancePosition=r.ReadInt32();p.AttendanceDay=r.ReadInt32();p.RouletteSpins=r.ReadInt32();p.RewardDoubles=r.ReadInt32();p.LastSpinUtc=r.ReadInt64();p.DoubledRun=r.ReadString();
            p.NormalTickets=r.ReadInt32();p.PremiumTickets=r.ReadInt32();p.Season=r.ReadInt32();p.PassPoints=r.ReadInt32();p.PointsToday=r.ReadInt32();
            p.FreeClaims=r.ReadInt32();p.PaidClaims=r.ReadInt32();p.CapRaised=r.ReadBoolean();
            int count=r.ReadInt32();if(count<0 || count>4096)throw new InvalidDataException("잘못된 주문 수");
            for(int i=0;i<count;i++){
                string id=r.ReadString();var entry=new PurchaseEntry(id,(PurchaseKind)r.ReadInt32(),r.ReadInt32(),r.ReadInt32());
                if(p.PurchaseLedger.ContainsKey(id))throw new InvalidDataException("중복 주문 번호");p.PurchaseLedger.Add(id,entry);}
            if(!p.Valid())throw new InvalidDataException("잘못된 일일 기록");return p;
        }
    }
    internal static class DailyRules
    {
        internal static void Refresh(CampaignProfile p,int day)
        {
            var d=p.Daily;if(day<=d.LocalDay)return;
            d.LocalDay=day;Array.Clear(d.Progress,0,6);d.MissionClaims=0;d.RouletteSpins=0;d.RewardDoubles=0;d.PointsToday=0;d.CapRaised=false;
            int season=DailyCatalog.SeasonNumber(day);if(season==d.Season)return;
            d.Season=season;d.PassPoints=0;d.FreeClaims=d.PaidClaims=0;
        }
        internal static void Progress(CampaignProfile p,DailyMission mission,int amount)
        {int i=(int)mission;p.Daily.Progress[i]=(int)Math.Min(DailyCatalog.Missions()[i].Target,(long)p.Daily.Progress[i]+Math.Max(0,amount));}
        internal static void Points(CampaignProfile p,int amount)
        {
            var d=p.Daily;int added=Math.Min(amount,Math.Min((d.CapRaised ? 2 : 1)*DailyCatalog.DailyPointCap-d.PointsToday,
                DailyCatalog.TierCount*DailyCatalog.PointsPerTier-d.PassPoints));d.PassPoints+=added;d.PointsToday+=added;
        }
        internal static bool Grant(CampaignProfile p,DailyReward reward)
        {
            switch(reward.Kind){
                case DailyRewardKind.MagicCores:if((long)p.MagicCores+reward.Amount>100000000)return false;p.MagicCores+=reward.Amount;break;
                case DailyRewardKind.Hearts:if(5-p.Hearts+999-p.ExtraHearts<reward.Amount)return false;HeartRules.Grant(p,reward.Amount);break;
                case DailyRewardKind.NormalTicket:if(p.Daily.NormalTickets>int.MaxValue-reward.Amount)return false;p.Daily.NormalTickets+=reward.Amount;break;
                case DailyRewardKind.PremiumTicket:if(p.Daily.PremiumTickets>int.MaxValue-reward.Amount)return false;p.Daily.PremiumTickets+=reward.Amount;break;
                case DailyRewardKind.FreeCrystals:if(p.CrystalsFree>int.MaxValue-reward.Amount)return false;p.CrystalsFree+=reward.Amount;break;
                case DailyRewardKind.Cosmetic:p.Supply.OwnedCosmetics|=1<<reward.Amount;break;
                default:return false;
            }return true;
        }
    }
}
