using System.Collections.Generic;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed partial class MobileHud
    {
        SupplyResult revealedBox;
        float supplyRevealAt;
        static Color RarityColor(SupplyRarity rarity)
        {return rarity==SupplyRarity.Legendary?Amber:rarity==SupplyRarity.Epic?FusionColor:rarity==SupplyRarity.Rare?Ally:Paper;}
        void ShopHeader(string title)
        {
            Rect full=Full();Fill(new Rect(full.x,full.y,full.width,88-full.y),GatefallUiStyle.ColorPanelFoot);
            IconButton(603,layout.ShopTop(0),"ui-glyph-home");
            Text(HomeRect(layout.ShopTop(1)),title,24,Paper,TextAnchor.MiddleLeft,2);
            for(int i=2;i<4;i++) {
                Rect r=HomeRect(layout.ShopTop(i));Skin(r,"ui-chip");r.x+=12;r.width-=24;
                Text(r,i==2?"결정 "+((long)controller.Profile.CrystalsFree+controller.Profile.CrystalsPaid):"마핵 "+controller.Profile.MagicCores,17,Paper,TextAnchor.MiddleLeft,1);
            }
        }
        void Shop()
        {
            Fill(Full(),GatefallUiStyle.ColorMaterialGraphite);ShopHeader("상점");
            for(int i=0;i<ShopLabels.Tabs.Length;i++)Button(800+i,layout.ShopTab(i),ShopLabels.Tabs[i],!controller.ShopAdPending,(int)controller.ShopTab==i?Look.Primary:Look.Secondary,null,17);
            if(controller.ShopTab==ShopSection.Daily)DailyShop();
            else if(controller.ShopTab==ShopSection.Boxes)SupplyBoxes();
            else if(controller.ShopTab==ShopSection.Hearts)ShopHearts();
            else {
                Rect r=HomeRect(layout.ShopProduct(0));r.width=W-48;Skin(r,"ui-panel");
                Text(new Rect(r.x+24,r.y+28,r.width-48,48),controller.ShopTab==ShopSection.Crystals?"결정 구매":"시작 묶음 · 장 완료 묶음 · 방어 기록 패스",22,Muted,TextAnchor.MiddleLeft,2);
                Button(0,new Box2(r.x+24,r.y+100,r.width-48,64),"출시 준비 중",false,Look.Secondary);
            }
        }
        void DailyShop()
        {
            DailyShopState shop=controller.Campaign.DailyShop();
            foreach(DailyShopOffer offer in shop.Offers()) {
                Rect r=HomeRect(layout.ShopSlot(offer.Slot));Skin(r,offer.Purchased?"ui-chip":"ui-panel");
                Color color=offer.Kind==ShopOfferKind.Card?RarityColor(SupplyCatalog.Rarity(offer.Tower)):Amber;
                if(offer.Kind==ShopOfferKind.Card)Tex(new Rect(r.x+12,r.y+8,44,54),art.TowerIcon(offer.Tower,1),offer.Purchased?Muted:Color.white);
                else Glyph(new Rect(r.x+16,r.y+16,32,32),"ui-glyph-ward",color);
                Text(new Rect(r.x+64,r.y+10,r.width-76,32),ShopLabels.Offer(offer),17,offer.Purchased?Muted:Paper,TextAnchor.MiddleLeft,2);
                string reason=controller.Campaign.ShopPurchaseReason(offer.Slot);
                Text(new Rect(r.x+12,r.y+56,r.width-24,28),offer.Purchased?"판매 완료":reason==""?(offer.Kind==ShopOfferKind.Card?ShopLabels.Rarity(SupplyCatalog.Rarity(offer.Tower)):offer.Slot==4?"오늘 하트 "+shop.HeartsBought+"/3":"오늘 무료 보급"):reason,14,reason==""?color:Muted,TextAnchor.MiddleLeft,1);
                Button(810+offer.Slot,layout.ShopSlotButton(offer.Slot),offer.Purchased?"판매 완료":offer.Price==0?"무료 받기":"마핵 "+offer.Price+" · 구매",!offer.Purchased && !controller.ShopAdPending,Look.Secondary,null,17);
            }
            bool ready=controller.RewardedAds!=null && controller.RewardedAds.IsReady;
            string label=controller.ShopAdPending?"광고 확인 중":!ready?"광고 준비 중":"광고로 새로 고침 "+shop.RefreshCount+"/2";
            Button(808,layout.ShopFooter(),label,ready && shop.RefreshCount<2 && !controller.ShopAdPending,Look.Secondary,null,17);
        }
        void SupplyBoxes()
        {
            for(int i=0;i<2;i++) {
                BoxKind kind=(BoxKind)i;Rect r=HomeRect(layout.ShopProduct(i));Skin(r,"ui-panel");
                Glyph(new Rect(r.center.x-30,r.y+18,60,60),"ui-glyph-ward",i==0?Ally:Amber);
                Text(new Rect(r.x+20,r.y+88,r.width-40,38),i==0?"일반 보급 상자":"고급 보급 상자",23,Paper,TextAnchor.MiddleCenter,2);
                string detail="카드 "+SupplyCatalog.CardsPerBox(kind,controller.Profile.ChapterReached)+"장 · 마핵 "+SupplyCatalog.MinCores(kind)+"~"+SupplyCatalog.MaxCores(kind);
                Text(new Rect(r.x+20,r.y+134,r.width-40,30),detail,17,Muted,TextAnchor.MiddleCenter,1);
                Text(new Rect(r.x+20,r.y+170,r.width-40,30),i==0?"오늘 남은 "+controller.NormalBoxesRemaining+"/5":"천장 "+controller.Profile.Supply.PremiumPity+"/"+SupplyCatalog.PityBoxes,17,Amber,TextAnchor.MiddleCenter,1);
                string reason=controller.Campaign.OpenBoxReason(kind);
                Text(new Rect(r.x+20,r.y+206,r.width-40,30),reason,15,Threat,TextAnchor.MiddleCenter,1);
                Button(805+i,layout.ShopProductButton(i),i==0?"마핵 40 · 열기":"결정 100 · 열기",true,Look.Primary,null,19);
            }
            Button(807,layout.ShopFooter(),"확률표 · 천장 규칙",true,Look.Secondary,null,17);
        }
        void ShopHearts()
        {
            Rect r=HomeRect(layout.ShopProduct(0));r.width=W-48;Skin(r,"ui-panel");
            var p=controller.Profile;
            Text(new Rect(r.x+24,r.y+28,r.width-48,48),"하트 5개",26,Paper,TextAnchor.MiddleCenter,2);
            Text(new Rect(r.x+24,r.y+100,r.width-48,60),"보유 ♥ "+p.Hearts+"/5 · 여분 +"+p.ExtraHearts+"\n기본 하트를 채운 나머지는 여분으로 보관합니다",18,Muted,TextAnchor.MiddleCenter,1);
            Button(809,new Box2(r.x+24,r.yMax-76,r.width-48,52),"결정 "+HeartRules.CrystalPrice+" → 하트 5",true,Look.Primary,null,20);
        }
        void TowerCardShop()
        {
            Fill(Full(),GatefallUiStyle.ColorMaterialGraphite);ShopHeader("결계주");
            Button(835,layout.ShopTab(0,2),"카드",true,Look.Primary,null,18);
            Button(836,layout.ShopTab(1,2),controller.Profile.FirstCleared[9]?"편성":"편성 · 1-10 클리어 후 해금",controller.Profile.FirstCleared[9],Look.Secondary,null,18);
            for(int slot=0;slot<6 && controller.TowerCardFirst+slot<SupplyCatalog.TowerCount;slot++) {
                TowerKind kind=(TowerKind)(controller.TowerCardFirst+slot);var p=controller.Profile;
                int grade=p.Supply.Grades[(int)kind];bool open=p.IsTowerUnlocked(kind);
                Rect r=HomeRect(layout.TowerCard(slot));Skin(r,"ui-panel");
                Tex(new Rect(r.x+12,r.y+8,42,54),art.TowerIcon(kind,1),open?Color.white:Muted);
                Text(new Rect(r.x+64,r.y+8,r.width-76,28),ShopLabels.Tower(kind)+" · 등급 "+grade,17,open?RarityColor(SupplyCatalog.Rarity(kind)):Muted,TextAnchor.MiddleLeft,2);
                Text(new Rect(r.x+64,r.y+36,r.width-76,26),open?"피해 "+ShopLabels.Bonus(p,kind):ShopLabels.Unlock(kind),14,open?Paper:Muted,TextAnchor.MiddleLeft,1);
                int need=grade==6?SupplyCatalog.RequiredCards(6):SupplyCatalog.RequiredCards(grade+1);
                Bar(new Rect(r.x+12,r.y+72,r.width-24,6),need==0?1:p.Supply.Cards[(int)kind]/(float)need,open?Ally:Muted);
                string reason=controller.Campaign.GradeUpReason(kind);
                Text(new Rect(r.x+12,r.y+84,r.width-24,26),ShopLabels.Progress(p,kind)+(open && reason!=""?" · "+reason:""),14,Muted,TextAnchor.MiddleLeft,1);
                Button(860+(int)kind,layout.TowerGradeButton(slot),!open?"잠김":grade==6?"최대 등급":"등급 올리기 · 마핵 "+SupplyCatalog.GradeCost(grade),open && grade<6,Look.Upgrade,null,16);
            }
            Button(837,layout.TowerCardPage(false),"이전",controller.TowerCardFirst>0,Look.Secondary);
            Button(838,layout.TowerCardPage(true),"다음",controller.TowerCardFirst+6<SupplyCatalog.TowerCount,Look.Secondary);
            Text(HomeRect(layout.TowerCardRule()),"등급 보너스는 위기 작전에 적용되지 않습니다\n"+(controller.TowerCardFirst/6+1)+" / 3",15,Paper,TextAnchor.MiddleCenter,1);
        }
        void SupplyOverlay()
        {
            if(!controller.SupplyModal)return;
            controller.TouchInput.Modal=true;controller.TouchInput.Regions.Clear();
            // The currency row remains visible above the modal safe-area panel.
            Rect panel=HomeRect(layout.SupplyPanel());Fill(panel,GatefallUiStyle.ColorMaterialGraphite);Skin(panel,"ui-panel");
            Button(819,layout.SupplyClose(),"닫기",true,Look.Secondary,null,17);
            bool odds=controller.SupplyOddsOpen;
            if(odds) {
                Button(822,layout.SupplyKindTab(0),"일반 확률",true,controller.SupplyOddsKind==BoxKind.Normal?Look.Primary:Look.Secondary,null,16);
                Button(823,layout.SupplyKindTab(1),"고급 확률",true,controller.SupplyOddsKind==BoxKind.Premium?Look.Primary:Look.Secondary,null,16);
                Text(new Rect(336,100,W-496,48),"천장 "+controller.Profile.Supply.PremiumPity+"/"+SupplyCatalog.PityBoxes,18,Amber,TextAnchor.MiddleCenter,1);
            } else {
                if(!ReferenceEquals(revealedBox,controller.BoxResult)){revealedBox=controller.BoxResult;supplyRevealAt=Time.unscaledTime;}
                Fill(new Rect(panel.x+12,panel.y+6,(panel.width-24)*Mathf.Clamp01((Time.unscaledTime-supplyRevealAt)/.16f),3),Ally);
                Text(new Rect(44,100,W-208,28),"보급 완료 · 마핵 +"+controller.BoxResult.Cores,22,Paper,TextAnchor.MiddleLeft,2);
                Text(new Rect(44,130,W-208,24),controller.BoxResult.PityFired?"천장 보장 발동 · 종결 카드 1장 획득":"카드와 외형은 아래에서 확인하세요",16,controller.BoxResult.PityFired?Amber:Muted,TextAnchor.MiddleLeft,1);
            }
            string[] rows=odds?ShopLabels.OddsRows(controller.Campaign,controller.SupplyOddsKind):ShopLabels.ResultRows(controller.BoxResult,controller.Profile);
            int visible=layout.SupplyVisibleRows(odds),first=Mathf.Min(controller.SupplyFirst,Mathf.Max(0,rows.Length-visible));
            var kinds=new List<TowerKind>();if(!odds)foreach(TowerKind kind in controller.BoxResult.Cards)if(!kinds.Contains(kind))kinds.Add(kind);
            for(int i=0;i<visible && first+i<rows.Length;i++) {
                int index=first+i;Rect r=HomeRect(layout.SupplyRow(i,odds));Color color=Paper;
                if(!odds && index<kinds.Count) {
                    TowerKind kind=kinds[index];Tex(new Rect(r.x,r.y+2,40,50),art.TowerIcon(kind,1),Color.white);
                    color=RarityColor(SupplyCatalog.Rarity(kind));r.x+=56;r.width-=56;
                }
                Text(r,rows[index],odds?16:18,color,TextAnchor.MiddleLeft,1);
            }
            if(odds)Text(HomeRect(layout.SupplyRule()),controller.Campaign.OddsTable(controller.SupplyOddsKind).PityRule,14,Muted,TextAnchor.UpperLeft,0);
            Button(820,layout.SupplyScroll(false),"위로",first>0,Look.Secondary,null,17);
            Button(821,layout.SupplyScroll(true),"아래로",first+visible<rows.Length,Look.Secondary,null,17);
            Text(new Rect(200,H-80,W-400,48),odds?"현재 해금 기준 · "+(first+1)+"–"+Mathf.Min(first+visible,rows.Length)+" / "+rows.Length:"한 번 더 누르면 닫습니다",15,Muted,TextAnchor.MiddleCenter,1);
            // Last priority: a second tap anywhere outside the scroll controls closes the result.
            if(!odds)controller.TouchInput.Regions.Add(new TouchRegion(819,layout.SupplyPanel()));
        }
    }
}
