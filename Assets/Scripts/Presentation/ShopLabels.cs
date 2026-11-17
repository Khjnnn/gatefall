using System;
using System.Collections.Generic;
using System.Globalization;
using Gatefall.Core;

namespace Gatefall
{
    internal static class ShopLabels
    {
        internal static readonly string[] Tabs={"일일 상점","상자","하트","결정","묶음"};
        internal static string Tower(TowerKind kind) { return TowerCatalog.Get(kind).Name.Replace(" 결계주",""); }
        internal static string Rarity(SupplyRarity rarity) {return new[]{"일반","희귀","영웅","전설"}[(int)rarity];}
        internal static string Percent(decimal value) { return value.ToString("0.####",CultureInfo.InvariantCulture)+"%"; }
        internal static string Progress(CampaignProfile p,TowerKind kind)
        { int i=(int)kind,g=p.Supply.Grades[i];return p.Supply.Cards[i]+(g==6?" · 최대 등급":" / "+SupplyCatalog.RequiredCards(g+1)); }
        internal static string Bonus(CampaignProfile p,TowerKind kind)
        { return "+"+(Supply.DamageBonus(p.Supply,kind)*100).ToString("0",CultureInfo.InvariantCulture)+"%"; }
        internal static string Unlock(TowerKind kind)
        {
            // Resolve gates from the current Core unlock predicate, including fusion outcomes.
            var p=new CampaignProfile();if(p.IsTowerUnlocked(kind))return "기본 해금";
            foreach(var stage in CampaignCatalog.Stages){p.FirstCleared[stage.Index]=true;p.BestStars[stage.Index]=1;
                if(p.IsTowerUnlocked(kind))return CampaignCatalog.StageId(stage.Index)+" 클리어 후 해금";}
            return "작전 진행으로 해금";
        }
        internal static string Offer(DailyShopOffer offer)
        { return offer.Kind==ShopOfferKind.Card?Tower(offer.Tower)+" 카드 +1":offer.Kind==ShopOfferKind.Heart?"하트 +1":"무료 마핵 +10"; }
        internal static string[] OddsRows(CampaignService campaign,BoxKind kind)
        {
            SupplyOdds table=campaign.OddsTable(kind);var rows=new List<string>();
            for(int r=0;r<table.RarityPercents.Length;r++)rows.Add(Rarity((SupplyRarity)r)+" · 추첨 "+Percent(table.RollPercents[r])+" · 현재 "+Percent(table.RarityPercents[r]));
            for(int i=0;i<table.PerKind.Length;i++)if(campaign.Profile.IsTowerUnlocked((TowerKind)i))rows.Add(Tower((TowerKind)i)+" · "+Percent(table.PerKind[i]));
            rows.Add("미보유 외형 합계 · "+Percent(table.CosmeticPercent));return rows.ToArray();
        }
        internal static string[] ResultRows(SupplyResult result,CampaignProfile p)
        {
            var rows=new List<string>();var seen=new HashSet<TowerKind>();
            foreach(TowerKind kind in result.Cards)if(seen.Add(kind)) {
                int n=0;foreach(TowerKind card in result.Cards)if(card==kind)n++;
                rows.Add(Tower(kind)+" +"+n+" · "+Progress(p,kind)+(p.Supply.Grades[(int)kind]==6?" · 마핵 전환":""));
            }
            foreach(int id in result.Cosmetics)rows.Add(SupplyCatalog.CosmeticName(id)+" 외형 획득");return rows.ToArray();
        }
    }
}
