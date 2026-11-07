using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed partial class MobileHud
    {
        // Shared logical geometry: native touch routing and both renderers consume these exact boxes.
        internal static Rect HomePanel(float width,float height) { return new Rect(32,130,360,height-258); }
        internal static Rect HomeNavigation(int index,float width,float height) {
            return index==0 ? new Rect(32,height-104,360,72) : new Rect(width-548+(index-1)*176,height-104,164,72);
        }
        internal static Rect HomeLaunch(float width,float height) {
            Rect panel=HomePanel(width,height);return new Rect(panel.x+24,panel.yMax-86,panel.width-48,62);
        }

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
            // Shade only the brand area so the ward crystal silhouette stays visible at wide aspects.
            Fill(new Rect(full.x,full.y,430-full.x,110-full.y),GatefallArt.Alpha(GatefallUiStyle.ColorPanelFoot,.78f));
            Glyph(new Rect(32,25,58,58),"ui-glyph-ward",GatefallUiStyle.ColorMaterialBrassLight);
            Text(new Rect(102,22,350,52),"GATEFALL",GatefallUiStyle.FontSizeBrand,Paper,TextAnchor.MiddleLeft,2);
            Text(new Rect(104,74,320,22),(controller.CurrentChapter==2?"다솜동":"종현동")+" · 도시 결계 본부",14,Muted,TextAnchor.MiddleLeft,1);
            Fill(new Rect(104,102,64,2),GatefallUiStyle.ColorMaterialBrass);
            HomeCurrency(new Rect(W-228,30,196,48),"icon-core",profile.MagicCores.ToString(),Paper);

            Rect panel=HomePanel(W,H);Skin(panel,"ui-panel");
            Fill(new Rect(panel.x+24,panel.y+22,3,20),GatefallUiStyle.ColorMaterialBrass);
            Text(new Rect(panel.x+36,panel.y+18,290,28),"선택 작전 "+CampaignCatalog.StageId(stage.Index),14,Amber,TextAnchor.MiddleLeft,1);
            Text(new Rect(panel.x+24,panel.y+52,312,60),stage.Name,GatefallUiStyle.FontSizeHomeTitle,Paper,TextAnchor.UpperLeft,2);
            Text(new Rect(panel.x+24,panel.y+120,312,44),"결계주를 정비하고\n라온시의 다음 방어선을 지켜내세요.",15,Muted,TextAnchor.UpperLeft,0);
            float progressY=panel.yMax-144;
            Text(new Rect(panel.x+24,progressY-28,312,24),"챕터 "+controller.CurrentChapter+(controller.CrisisSelection?" · 위기 방어 기록":" · 방어 기록"),13,Muted,TextAnchor.MiddleLeft,1);
            int first=CampaignCatalog.ChapterStart(controller.CurrentChapter),count=CampaignCatalog.ChapterCount(controller.CurrentChapter),stars=0;
            int[] records=controller.CrisisSelection?profile.CrisisStars:profile.BestStars;
            for(int i=first;i<first+count;i++)stars+=records[i];
            Bar(new Rect(panel.x+24,progressY,224,8),stars/(count*3f),Ally);
            Text(new Rect(panel.x+260,progressY-8,76,24),stars+" / "+(count*3),13,Paper,TextAnchor.MiddleRight,1);
            HomeAction(606,HomeLaunch(W,H),"작전 출전","ui-glyph-play",!controller.SaveBlocked);

            HomeNavigationCard(600,HomeNavigation(0,W,H),"작전 지도","작전 선택 · 방어 기록","ui-glyph-map",true,Ally);
            HomeNavigationCard(652,HomeNavigation(1,W,H),"편성",profile.FirstCleared[9]?"결계주 관리":"1-10 클리어","ui-glyph-loadout",profile.FirstCleared[9],GatefallUiStyle.ColorMaterialBrassLight);
            HomeNavigationCard(602,HomeNavigation(2,W,H),"성장","헌터 · 연구","ui-glyph-hunter",true,Ally);
            HomeNavigationCard(604,HomeNavigation(3,W,H),"설정","사운드 · 기록","ui-glyph-gear",true,Muted);
        }

        private void HomeCurrency(Rect r,string icon,string value,Color color)
        {
            Skin(r,"ui-chip");Icon(new Rect(r.x+12,r.y+10,28,28),icon,Color.white);
            Text(new Rect(r.x+48,r.y,r.width-60,r.height),value,20,color,TextAnchor.MiddleLeft,2);
        }
        private void HomeAction(int id,Rect r,string label,string glyph,bool enabled)
        {
            Button(id,Box(r),null,enabled,Look.Primary);
            Glyph(new Rect(r.x+20,r.center.y-17,34,34),glyph,enabled?Ink:Muted);
            Text(new Rect(r.x+70,r.y,r.width-100,r.height),label,22,enabled?Ink:Muted,TextAnchor.MiddleCenter,2);
            // A pair of small metal corner details gives all hub actions the same equipment construction.
            Fill(new Rect(r.x+12,r.y+9,22,2),GatefallArt.Alpha(Color.white,.35f));
        }
        private void HomeNavigationCard(int id,Rect r,string title,string subtitle,string glyph,bool enabled,Color accent)
        {
            Button(id,Box(r),null,enabled,Look.Secondary);
            Glyph(new Rect(r.x+14,r.y+17,36,36),enabled?glyph:"ui-glyph-lock",enabled?accent:Muted);
            Text(new Rect(r.x+60,r.y+10,r.width-70,28),title,19,enabled?Paper:Muted,TextAnchor.MiddleLeft,2);
            Text(new Rect(r.x+60,r.y+39,r.width-70,22),subtitle,12,Muted,TextAnchor.MiddleLeft,0);
            Fill(new Rect(r.x+16,r.y+7,18,2),GatefallArt.Alpha(enabled?accent:Muted,.7f));
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
