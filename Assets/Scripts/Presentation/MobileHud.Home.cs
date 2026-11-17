using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed partial class MobileHud
    {
        private static Rect HomeRect(Box2 b) { return new Rect(b.X,b.Y,b.Width,b.Height); }
        internal static Rect HomePanel(float width,float height) { return HomeRect(new MobileLayout(width,height,new Box2()).HomePanel()); }
        internal static Rect HomeNavigation(int index,float width,float height) { return HomeRect(new MobileLayout(width,height,new Box2()).HomeNavigation(index)); }
        internal static Rect HomeLaunch(float width,float height) { return HomeRect(new MobileLayout(width,height,new Box2()).HomeLaunch()); }
        private static string HeartTime(int seconds) { return (seconds/60).ToString("00")+":"+(seconds%60).ToString("00"); }

        private void Home()
        {
            CampaignProfile profile=controller.Profile;
            CampaignStage stage=CampaignCatalog.Stages[controller.SelectedStage];
            Rect full=Full();Fill(full,GatefallUiStyle.ColorMaterialGraphite);
            Texture background=art.Texture("home-city");
            if(background!=null) {
                float imageAspect=(float)background.width/background.height,areaAspect=full.width/full.height;
                Rect uv=new Rect(0,0,1,1);
                if(areaAspect>imageAspect){uv.height=imageAspect/areaAspect;uv.y=(1-uv.height)/2;}
                else {uv.width=areaAspect/imageAspect;uv.x=(1-uv.width)/2;}
                Crop(full,background,uv,Color.white);
            }
            Fill(full,new Color(0,0,0,GatefallUiStyle.HomeBackdropDim));
            Fill(new Rect(full.x,full.y,full.width,88-full.y),GatefallArt.Alpha(GatefallUiStyle.ColorPanelFoot,.9f));
            Rect hunter=HomeRect(layout.HomeTop(0));
            Button(602,Box(hunter),null,true,Look.Secondary);
            Glyph(new Rect(hunter.x+10,hunter.y+12,30,30),"ui-glyph-hunter",Ally);
            Text(new Rect(hunter.x+46,hunter.y, hunter.width-50,hunter.height),CampaignCatalog.Hunters[profile.SelectedHunter]+"\n성장 "+profile.Growth[profile.SelectedHunter],14,Paper,TextAnchor.MiddleLeft,1);
            Rect heart=HomeRect(layout.HomeTop(1));Skin(heart,"ui-chip");
            string timer=controller.HeartWaitSeconds==0?"가득 참":HeartTime(controller.HeartWaitSeconds);
            Text(new Rect(heart.x+8,heart.y+3,heart.width-16,28),"♥ "+profile.Hearts+"/"+HeartRules.MaximumHearts+" · "+timer,15,Paper,TextAnchor.MiddleLeft,1);
            if(profile.ExtraHearts>0)Text(new Rect(heart.x+8,heart.y+29,heart.width-16,24),"+"+profile.ExtraHearts+" 여분",13,Amber,TextAnchor.MiddleLeft,1);
            Button(684,layout.HomeTop(2),"+",true,Look.Secondary,null,24);
            HomeCurrency(HomeRect(layout.HomeTop(3)),"icon-core",profile.MagicCores.ToString(),Paper);
            Rect crystals=HomeRect(layout.HomeTop(4));Skin(crystals,"ui-chip");
            Text(new Rect(crystals.x+12,crystals.y,crystals.width-24,crystals.height),"결정 "+((long)profile.CrystalsFree+profile.CrystalsPaid),15,Paper,TextAnchor.MiddleLeft,1);
            Button(685,layout.HomeTop(5),"+",true,Look.Secondary,null,24);
            IconButton(604,layout.HomeTop(6),"ui-glyph-gear");

            var daily=controller.Campaign.DailyStatus();
            Rect pass=HomeRect(layout.HomePass());Button(880,Box(pass),null,true,Look.Secondary);
            Text(new Rect(pass.x+16,pass.y+8,pass.width-40,24),"방어 기록 패스 · "+(daily.PassPoints/DailyCatalog.PointsPerTier)+"단계",17,Paper,TextAnchor.MiddleLeft,2);
            Bar(new Rect(pass.x+16,pass.y+42,168,8),daily.PassPoints/(float)(DailyCatalog.TierCount*DailyCatalog.PointsPerTier),Amber);
            Text(new Rect(pass.x+196,pass.y+32,pass.width-212,28),daily.PassPoints+" / "+(DailyCatalog.TierCount*DailyCatalog.PointsPerTier),13,Muted,TextAnchor.MiddleRight,1);
            if(controller.HomeNotice(880))Text(new Rect(pass.xMax-22,pass.y+4,16,20),"●",12,Threat,TextAnchor.MiddleCenter,1);
            Rect panel=HomeRect(layout.HomePanel());Button(600,Box(panel),null,true,Look.Secondary);
            Text(new Rect(panel.x+20,panel.y+18,panel.width-40,28),"선택 작전 "+CampaignCatalog.StageId(stage.Index),15,Amber,TextAnchor.MiddleLeft,1);
            Text(new Rect(panel.x+20,panel.y+54,panel.width-40,60),stage.Name,GatefallUiStyle.FontSizeHomeTitle,Paper,TextAnchor.UpperLeft,2);
            Text(new Rect(panel.x+20,panel.y+120,panel.width-40,40),CampaignCatalog.ChapterMapNames[controller.CurrentChapter-1]+"\n작전 지도로 이동",15,Muted,TextAnchor.UpperLeft,0);
            float progressY=panel.yMax-48;
            Text(new Rect(panel.x+20,progressY-36,panel.width-40,28),"챕터 "+controller.CurrentChapter+(controller.CrisisSelection?" · 위기 방어 기록":" · 방어 기록"),13,Muted,TextAnchor.MiddleLeft,1);
            int first=CampaignCatalog.ChapterStart(controller.CurrentChapter),count=CampaignCatalog.ChapterCount(controller.CurrentChapter),stars=0;
            int[] records=controller.CrisisSelection?profile.CrisisStars:profile.BestStars;
            for(int i=first;i<first+count;i++)stars+=records[i];
            Bar(new Rect(panel.x+20,progressY,208,8),stars/(count*3f),Ally);
            Text(new Rect(panel.x+236,progressY-8,80,24),stars+" / "+(count*3),13,Paper,TextAnchor.MiddleRight,1);

            for(int i=0;i<HomeMenu.Tabs.Length;i++) {
                HomeMenuEntry entry=HomeMenu.Tabs[i];Rect r=HomeRect(layout.HomeNavigation(i,HomeMenu.Tabs.Length,controller.HomeTabFirst));
                if(r.width==0)continue;
                bool unlocked=entry.UnlockStage<0 || profile.FirstCleared[entry.UnlockStage];
                if(entry.Action==606) {
                    bool empty=profile.Hearts+profile.ExtraHearts==0;
                    Button(entry.Action,Box(r),"작전 출전\n♥1"+(empty?" · 하트 부족":""),!controller.SaveBlocked,empty?Look.Danger:Look.Primary,null,layout.HomeCompact?17:20);
                } else HomeNavigationCard(entry.Action,r,entry.Name,unlocked?entry.Detail:entry.UnlockStage==9?"1-10 클리어":"1-3 클리어",entry.Glyph,unlocked,Ally);
                if(controller.HomeNotice(entry.Action))Text(new Rect(r.xMax-22,r.y+4,16,20),"●",12,Threat,TextAnchor.MiddleCenter,1);
            }
            if(HomeMenu.Tabs.Length>7) {
                Button(693,layout.HomeTabArrow(false),"‹",controller.HomeTabFirst>0,Look.Secondary);
                Button(694,layout.HomeTabArrow(true),"›",controller.HomeTabFirst+7<HomeMenu.Tabs.Length,Look.Secondary);
            }
            bool folded=HomeMenu.Right.Length>4 && !controller.HomeRightExpanded;
            int visible=folded?4:HomeMenu.Right.Length;
            for(int i=0;i<visible;i++) {
                Rect r=HomeRect(layout.HomeRight(i,HomeMenu.Right.Length,controller.HomeRightExpanded));
                if(folded && i==3){Button(695,Box(r),"더 보기",true,Look.Secondary);continue;}
                HomeMenuEntry entry=HomeMenu.Right[i];
                string detail=entry.Action==681?"남은 "+(DailyCatalog.RouletteDailyLimit-daily.RouletteSpins)+"/3":entry.Action==682?DailyLabels.ClaimedMissions(daily)+"/6 수령":daily.AttendanceDay>=daily.LocalDay?"오늘 수령 완료":"오늘 보상 받기";
                HomeNavigationCard(entry.Action,r,entry.Name,detail,entry.Glyph,true,Amber);
                if(layout.HomeCompact || r.width<150 || r.height<76)Text(new Rect(r.x+4,r.yMax-20,r.width-8,18),detail,12,Muted,TextAnchor.MiddleCenter,0);
                if(controller.HomeUnread(entry.Action))Text(new Rect(r.xMax-22,r.y+4,16,20),"●",12,Threat,TextAnchor.MiddleCenter,1);
            }
        }
        private void HomeCurrency(Rect r,string icon,string value,Color color)
        {
            Skin(r,"ui-chip");Icon(new Rect(r.x+10,r.center.y-14,28,28),icon,Color.white);
            Text(new Rect(r.x+44,r.y,r.width-52,r.height),value,16,color,TextAnchor.MiddleLeft,2);
        }
        private void HomeNavigationCard(int id,Rect r,string title,string subtitle,string glyph,bool enabled,Color accent)
        {
            Button(id,Box(r),null,enabled,Look.Secondary);
            bool compact=layout.HomeCompact || r.width<150 || r.height<76;
            if(compact) {
                float titleY=r.height<=76?30:38;
                Glyph(new Rect(r.center.x-11,r.y+4,22,22),enabled?glyph:"ui-glyph-lock",enabled?accent:Muted);
                Text(new Rect(r.x+4,r.y+titleY,r.width-8,24),title,17,enabled?Paper:Muted,TextAnchor.MiddleCenter,2);
                if(!enabled)Text(new Rect(r.x+4,r.yMax-24,r.width-8,20),subtitle,12,Muted,TextAnchor.MiddleCenter,0);
            } else {
                Glyph(new Rect(r.x+12,r.center.y-16,32,32),enabled?glyph:"ui-glyph-lock",enabled?accent:Muted);
                Text(new Rect(r.x+52,r.y+12,r.width-60,28),title,17,enabled?Paper:Muted,TextAnchor.MiddleLeft,2);
                Text(new Rect(r.x+52,r.y+42,r.width-60,22),subtitle,12,Muted,TextAnchor.MiddleLeft,0);
            }
        }
        private void HomeOverlay()
        {
            if(!controller.HeartShortage && controller.HomePlaceholder=="")return;
            controller.TouchInput.Modal=true;controller.TouchInput.Regions.Clear();
            Fill(Full(),new Color(0,0,0,.72f));
            Rect r=HomeRect(layout.HeartDialog());Skin(r,"ui-panel");
            if(!controller.HeartShortage) {
                Text(new Rect(r.x+24,r.y+48,r.width-48,48),controller.HomePlaceholder,24,Paper,TextAnchor.MiddleCenter,2);
                Text(new Rect(r.x+24,r.y+130,r.width-48,52),"준비 중",20,Muted,TextAnchor.MiddleCenter,1);
                Button(686,layout.HeartDialogButton(2),"닫기",true,Look.Secondary);return;
            }
            Text(new Rect(r.x+24,r.y+20,r.width-48,40),"하트가 부족합니다",26,Paper,TextAnchor.MiddleCenter,2);
            Text(new Rect(r.x+24,r.y+68,r.width-48,28),"출전에 ♥1이 필요합니다",17,Muted,TextAnchor.MiddleCenter,1);
            if(controller.CampaignError!="" && controller.CampaignError!="하트 부족")
                Text(new Rect(r.x+24,r.y+106,r.width-48,48),controller.CampaignError,14,Threat,TextAnchor.MiddleCenter,1);
            bool ready=controller.RewardedAds!=null && controller.RewardedAds.IsReady;
            int remaining=HeartRules.DailyAdLimit-controller.Profile.DailyLimits.HeartAdsUsed;
            string ad=controller.HeartAdPending?"광고 확인 중":!ready?"광고 준비 중":"광고 보기 (+1)";
            Button(696,layout.HeartDialogButton(0),ad+" · 남은 "+remaining+"/"+HeartRules.DailyAdLimit,ready && remaining>0 && !controller.HeartAdPending,Look.Primary,null,18);
            Button(697,layout.HeartDialogButton(1),"결정 "+HeartRules.CrystalPrice+" → 하트 5",!controller.HeartAdPending,Look.Secondary,null,18);
            Button(698,layout.HeartDialogButton(2),"기다리기 · "+HeartTime(controller.HeartWaitSeconds),!controller.HeartAdPending,Look.Ghost,null,18);
        }
        private static string SystemGlyph(string name)
        {
            if(name.StartsWith("ui-glyph-"))return name;
            switch(name) {
                case "icon-back":return "ui-glyph-back";
                case "icon-settings":return "ui-glyph-gear";
                case "icon-growth":return "ui-glyph-hunter";
                case "icon-play":return "ui-glyph-play";
                default:return null;
            }
        }
    }
}
