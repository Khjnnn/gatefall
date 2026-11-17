using System;
using System.Collections.Generic;

namespace Gatefall.Core
{
    public sealed class HeartDailyLimits
    {
        public int LocalDay, HeartAdsUsed;
        public Dictionary<string,int> NamedCounters=new Dictionary<string,int>();
        public HeartDailyLimits Copy()
        { var p=(HeartDailyLimits)MemberwiseClone();p.NamedCounters=new Dictionary<string,int>(NamedCounters);return p; }
        public bool Valid()
        {
            if(LocalDay<0 || HeartAdsUsed<0 || HeartAdsUsed>10 || NamedCounters==null || NamedCounters.Count>4096)return false;
            foreach(var x in NamedCounters)if(!StageProgress.ValidId(x.Key) || x.Value<0)return false;return true;
        }
    }
    // Deterministic profile operations: callers supply time and commit a copy atomically.
    public static class HeartRules
    {
        public const int MaximumHearts=5, MaximumExtraHearts=999, RegenSeconds=900, DailyAdLimit=10, CrystalPrice=30; public const int IdleSaveSeconds=300;
        public static void Refresh(CampaignProfile p,long utcSeconds,int localDay)
        {
            if(utcSeconds<0 || localDay<0)throw new ArgumentOutOfRangeException("clock");
            if(p.DailyLimits.LocalDay<localDay){p.DailyLimits.LocalDay=localDay;p.DailyLimits.HeartAdsUsed=0;p.DailyLimits.NamedCounters.Clear();}
            DailyRules.Refresh(p,Math.Max(p.DailyLimits.LocalDay,localDay));
            if(utcSeconds<p.LastSeenUtc || utcSeconds<p.HeartAnchorUtc || p.Hearts==MaximumHearts)
                p.HeartAnchorUtc=utcSeconds;
            else {
                int count=(int)Math.Min(MaximumHearts-p.Hearts,(utcSeconds-p.HeartAnchorUtc)/RegenSeconds);
                p.Hearts+=count;p.HeartAnchorUtc=p.Hearts==MaximumHearts ? utcSeconds : p.HeartAnchorUtc+count*RegenSeconds;
            }
            p.LastSeenUtc=utcSeconds;
        }
        public static int Grant(CampaignProfile p,int amount)
        {
            if(amount<0)throw new ArgumentOutOfRangeException("amount");
            int added=Math.Min(MaximumHearts-p.Hearts,amount);p.Hearts+=added;amount-=added;
            added=Math.Min(MaximumExtraHearts-p.ExtraHearts,amount);p.ExtraHearts+=added;return amount-added;
        }
        public static bool Spend(CampaignProfile p,long utcSeconds,int localDay)
        {
            Refresh(p,utcSeconds,localDay);
            if(p.Hearts>0){if(p.Hearts==MaximumHearts)p.HeartAnchorUtc=utcSeconds;p.Hearts--;return true;}
            if(p.ExtraHearts==0)return false;p.ExtraHearts--;return true;
        }
        public static int NextRegenSeconds(CampaignProfile p,long utcSeconds,int localDay)
        { var copy=p.Copy();Refresh(copy,utcSeconds,localDay);return copy.Hearts==MaximumHearts ? 0 : RegenSeconds-(int)(utcSeconds-copy.HeartAnchorUtc); }
        public static bool AdRefill(CampaignProfile p,long utcSeconds,int localDay)
        {
            Refresh(p,utcSeconds,localDay);
            if(p.DailyLimits.HeartAdsUsed>=DailyAdLimit || p.Hearts==MaximumHearts && p.ExtraHearts==MaximumExtraHearts)return false;
            Grant(p,1);p.DailyLimits.HeartAdsUsed++;return true;
        }
        public static bool SpendCrystals(CampaignProfile p,int amount)
        {
            if(amount<0)throw new ArgumentOutOfRangeException("amount");
            if((long)p.CrystalsFree+p.CrystalsPaid<amount)return false;
            int free=Math.Min(p.CrystalsFree,amount);p.CrystalsFree-=free;p.CrystalsPaid-=amount-free;return true;
        }
        public static bool CrystalRefill(CampaignProfile p,long utcSeconds,int localDay)
        {
            Refresh(p,utcSeconds,localDay);
            if(MaximumHearts-p.Hearts+MaximumExtraHearts-p.ExtraHearts<5 || !SpendCrystals(p,CrystalPrice))return false;
            Grant(p,5);return true;
        }
        public static int ClearReward(int stage,bool firstClear,bool firstThreeStar)
        {
            var s=CampaignCatalog.Stages[stage];int reward=firstThreeStar ? 1 : 0;
            if(firstClear){reward+=s.Chapter==1 ? 10 : s.Chapter==2 ? 3 : 2;
                if(s.LocalNumber==CampaignCatalog.ChapterCount(s.Chapter))reward+=10;}
            return reward;
        }
        public static void Backfill(CampaignProfile p)
        {
            if(p.HeartRewardsBackfilled)return;
            for(int i=0;i<p.FirstCleared.Length;i++)Grant(p,ClearReward(i,p.FirstCleared[i],p.BestStars[i]==3));
            p.HeartRewardsBackfilled=true;
        }
    }
}
