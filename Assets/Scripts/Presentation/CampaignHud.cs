using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed partial class MobileHud
    {
        // Operation positions as fractions of district-map.png (keep in sync with Tools/art/env.py MAP_NODES).
        private static readonly Vector2[] MapNodes = { new Vector2(.10f, .70f), new Vector2(.25f, .56f), new Vector2(.40f, .66f), new Vector2(.54f, .46f), new Vector2(.68f, .40f), new Vector2(.80f, .33f) };
        private static readonly string[] Lanes = { "서", "북", "동", "남" };

        void CampaignPage()
        {
            if (controller.ScreenPage == CampaignScreen.City) City();
            else if (controller.ScreenPage == CampaignScreen.Growth) Growth();
            else if (controller.ScreenPage == CampaignScreen.Loadout) Loadout();
            else Settings();
            if (controller.CampaignError != "") {
                Rect r = new Rect(W / 2 - 330, H - 52, 660, 40);
                Skin(r, "ui-chip");
                Text(r, controller.CampaignError, 15, Threat, TextAnchor.MiddleCenter, 1);
            }
        }

        // Whole physical screen in reference units, including notch insets outside the safe area.
        Rect Full() { return new Rect(-layout.Origin.X / layout.Scale, -layout.Origin.Y / layout.Scale, Screen.width / layout.Scale, Screen.height / layout.Scale); }
        // Fit the whole route left of the briefing, including on 4:3 displays.
        Rect MapRect()
        {
            Texture map = art.Texture("district-map");
            float aspect = map != null ? (float)map.width / map.height : 1.6f;
            Rect area = new Rect(8, 84, W - 436, H - 100);
            float w = Mathf.Min(area.width, area.height * aspect), h = w / aspect;
            return new Rect(area.center.x - w / 2, area.center.y - h / 2, w, h);
        }

        void Header(string title, bool back)
        {
            Rect full = Full();
            Fill(new Rect(full.x, full.y, full.width, 66 - full.y), new Color(.06f, .08f, .11f, .72f));
            if (back) IconButton(600, new Box2(14, 6, 60, 54), "icon-back");
            Text(new Rect(back ? 88 : 22, 8, 520, 50), title, 24, Paper, TextAnchor.MiddleLeft, 2);
            Skin(new Rect(W - 172, 10, 156, 46), "ui-chip");
            Icon(new Rect(W - 164, 14, 38, 38), "icon-core", Color.white);
            Text(new Rect(W - 122, 10, 100, 46), controller.Profile.MagicCores.ToString(), 22, Paper, TextAnchor.MiddleLeft, 2);
        }

        void City()
        {
            CampaignProfile profile = controller.Profile;
            Rect map = MapRect(), full = Full();
            Fill(full, GatefallArt.Hex("1A222C"));
            Tex(map, art.Texture("district-map"), Color.white);
            Fill(new Rect(W - 420, full.y, full.xMax - (W - 420), full.height), new Color(.06f, .08f, .11f, .55f));
            Header("라온시 · 종현동", false);
            bool crisis = controller.CrisisSelection;
            string starsLabel = crisis ? "위기 ★ " + profile.CrisisStarsTotal : "★ " + profile.StarsTotal;
            Text(new Rect(260, 14, 192, 40), starsLabel + " / " + (CampaignCatalog.Stages.Length * 3), 17, Amber, TextAnchor.MiddleLeft, 2);
            IconButton(602, new Box2(W - 312, 8, 58, 50), "icon-growth");
            IconButton(604, new Box2(W - 244, 8, 58, 50), "icon-settings");
            if(profile.FirstCleared[9]) Button(652, new Box2(455, 10, 140, 46), "결계주 편성", true, Look.Secondary, null, 14);

            // Route between operations, then the markers.
            float mapRight = W - 420;
            Vector2[] points = new Vector2[6];
            for (int i = 0; i < 6; i++) {
                Vector2 n = MapNodes[i];
                points[i] = new Vector2(map.x + map.width * n.x, map.y + map.height * n.y);
            }
            int offset=controller.MapPage*GatefallGame.OperationsPerPage, count=Mathf.Min(GatefallGame.OperationsPerPage,CampaignCatalog.Stages.Length-offset);
            for (int i = 0; i < count-1; i++) {
                bool lit = profile.BestStars[offset+i] > 0;
                for (int k = 1; k < 10; k++) {
                    Vector2 p = Vector2.Lerp(points[i], points[i + 1], k / 10f);
                    Tex(new Rect(p.x - 4, p.y - 4, 8, 8), art.Texture("fx-disc"), lit ? GatefallArt.Alpha(Ally, .95f) : new Color(1, 1, 1, .55f));
                }
            }
            for (int i = 0; i < count; i++) {
                int stageIndex=offset+i;
                bool open = profile.IsStageOpen(stageIndex), selected = stageIndex == controller.SelectedStage;
                int stars = controller.CrisisSelection ? profile.CrisisStars[stageIndex] : profile.BestStars[stageIndex];
                float size = selected ? 74 : 60; Vector2 p = points[i];
                Rect r = new Rect(p.x - size / 2, p.y - size / 2, size, size);
                if (selected) Tex(new Rect(r.x - 10, r.y - 10, r.width + 20, r.height + 20), art.Texture("fx-ring"), Amber);
                Tex(r, art.Texture("fx-disc"), !open ? new Color(.16f, .19f, .23f, .95f) : stars > 0 ? GatefallArt.Hex("1E6E68") : GatefallArt.Hex("2A3442"));
                Tex(r, art.Texture("fx-ring"), !open ? new Color(1, 1, 1, .25f) : stars > 0 ? Ally : Amber);
                if (open) Text(r, "1-" + (stageIndex + 1), selected ? 21 : 18, Paper, TextAnchor.MiddleCenter, 2);
                else Icon(new Rect(r.center.x - 16, r.center.y - 16, 32, 32), "icon-lock", Color.white);
                if (stars > 0) for (int s = 0; s < 3; s++) Icon(new Rect(p.x - 33 + s * 22, r.yMax + 2, 22, 22), s < stars ? "icon-star" : "icon-star-empty", Color.white);
                if (open) controller.TouchInput.Regions.Add(new TouchRegion(710 + stageIndex, new Box2(r.x - 12, r.y - 12, r.width + 24, r.height + 24)));
            }
            Button(655,new Box2(map.x+12,map.yMax-52,100,44),"이전",controller.CanMoveMapPage(-1),Look.Secondary,null,14);
            Button(656,new Box2(map.x+120,map.yMax-52,100,44),"다음",controller.CanMoveMapPage(1),Look.Secondary,null,14);
            Text(new Rect(map.x+230,map.yMax-52,150,44),(controller.MapPage+1)+" / "+GatefallGame.MapPageCount,14,Paper,TextAnchor.MiddleLeft,1);
            Briefing(new Rect(W - 404, 78, 388, H - 92));
        }

        void Loadout()
        {
            Fill(Full(),GatefallArt.Hex("1A222C")); Header("결계주 편성",true);
            CampaignProfile profile = controller.Profile;
            int count = profile.LoadoutCount;
            bool full = count >= CampaignProfile.MaximumLoadoutCount;
            bool last = count <= CampaignProfile.MinimumLoadoutCount;
            string hint = full ? "새 결계주를 넣으려면 먼저 하나를 해제하세요"
                : last ? "결계주를 최소 1개 유지하세요" : "추가할 결계주를 선택하세요";
            Text(new Rect(30,72,W-60,34),"편성 " + count + " / " + CampaignProfile.MaximumLoadoutCount + " · " + hint,17,Paper,TextAnchor.MiddleLeft,1);
            var results=profile.LoadoutResults();
            string names="";foreach(TowerKind kind in results)names+=(names==""?"":" · ")+TowerCatalog.Get(kind).Name.Replace(" 결계주","");
            Text(new Rect(30,106,W-60,28),"조합 가능: "+(names==""?"없음":names)+"  ·  전투 중 강화와 촉매 필요",14,Ally,TextAnchor.MiddleLeft,0);
            TowerKind[] kinds={TowerKind.Autocannon,TowerKind.Binding,TowerKind.Rupture,TowerKind.Barrier,TowerKind.Marker};
            float width=(W-100)/5;
            for(int i=0;i<kinds.Length;i++) {
                TowerKind kind=kinds[i];bool chosen=(profile.Loadout&(1<<(int)kind))!=0;
                Rect r=new Rect(30+i*(width+10),140,width,H-210);Skin(r,chosen?"ui-selected":"ui-panel");
                Tex(new Rect(r.center.x-45,r.y+8,90,115),art.TowerIcon(kind,1),Color.white);
                Text(new Rect(r.x+4,r.y+130,r.width-8,32),TowerCatalog.Get(kind).Name.Replace(" 결계주",""),18,Paper,TextAnchor.MiddleCenter,2);
                Text(new Rect(r.x+8,r.y+166,r.width-16,70),TowerCatalog.Get(kind).Role,13,Muted,TextAnchor.UpperCenter,0);
                bool enabled = chosen ? !last : !full;
                string action = chosen ? (last ? "1개 필수" : "해제") : (full ? "편성 가득 참" : "선택");
                Button(700+i,new Box2(r.x+8,r.yMax-55,r.width-16,46),action,enabled,chosen?Look.Primary:Look.Secondary,null,14);
            }
        }

        void Briefing(Rect r)
        {
            CampaignProfile profile = controller.Profile;
            CampaignStage stage = CampaignCatalog.Stages[controller.SelectedStage];
            Skin(r, "ui-panel");
            float x = r.x + 22, w = r.width - 44, y = r.y + 16;
            Text(new Rect(x, y, w, 22), "작전 " + CampaignCatalog.StageId(stage.Index) + (stage.Boss ? "  ·  탑 봉인전" : ""), 14, Amber, TextAnchor.MiddleLeft, 1);
            Text(new Rect(x, y + 22, w, 38), stage.Name, 26, Paper, TextAnchor.MiddleLeft, 2);
            string lanes = ""; foreach (int lane in stage.Lanes) lanes += (lanes == "" ? "" : "·") + Lanes[lane];
            Text(new Rect(x, y + 64, w, 22), "진입로 " + lanes + "  ·  지점 " + stage.SocketCount + "  ·  6웨이브", 14, Muted, TextAnchor.MiddleLeft, 0);
            bool first = stage.Index == 0 && profile.BestStars[0] == 0;
            bool showDeployment = stage.Index >= 3;
            float storyHeight = stage.Index>=6 ? 56 : showDeployment ? 80 : 112;
            float briefingY = y + 94 + storyHeight;
            Text(new Rect(x, y + 94, w, storyHeight), (first ? CampaignCatalog.Prologue : stage.Story).Replace('\n', ' '), 15, Paper, TextAnchor.UpperLeft, 0);
            Text(new Rect(x, briefingY, w, 50), stage.CityBriefing(profile), 15, Ally, TextAnchor.UpperLeft, 1);
            if (showDeployment) {
                float lessonY = briefingY + 54;
                Text(new Rect(x, lessonY, w, 18), "배치 힌트", 13, Amber, TextAnchor.UpperLeft, 1);
                Text(new Rect(x, lessonY + 20, w, 70), stage.DeploymentLesson(profile).Replace(". ", ".\n"), 14, Paper, TextAnchor.UpperLeft, 0);
            }

            // Hunter chip: tap to cycle unlocked hunters (no separate screen needed).
            int hunter = profile.SelectedHunter;
            Rect chip = new Rect(x, r.yMax - 172, w, 64);
            if(stage.Index>=6) {
                int growth=CampaignCatalog.RecommendedHunterGrowth(stage.Index,hunter),focus=CampaignCatalog.ResearchFocus(profile.Loadout);
                int research=focus<0 ? 0 : stage.RecommendedResearch[focus];
                string hint=focus<0 ? "권장 성장 "+growth+" · 공격형 결계주를 함께 편성하세요" :
                    "권장 성장 "+growth+" · "+CampaignCatalog.ResearchNames[focus].Split('·')[0].Trim()+" 연구 "+research+
                    "  (현재 "+profile.Growth[hunter]+" / "+profile.Research[focus]+")";
                bool ready=profile.Growth[hunter]>=growth && (focus<0 || profile.Research[focus]>=research);
                Text(new Rect(x,chip.y-25,w,22),hint,13,ready ? Ally : Amber,TextAnchor.MiddleLeft,1);
            }
            bool many = profile.IsHunterOpen(1) || profile.IsHunterOpen(2);
            Button(many ? 623 : 0, Box(chip), "", many, Look.Secondary);
            if (!many) Skin(chip, "ui-button");
            Tex(new Rect(chip.x + 8, chip.y + 2, 52, 60), art.Texture("hunter-" + hunter), Color.white);
            Text(new Rect(chip.x + 70, chip.y + 6, 200, 28), CampaignCatalog.Hunters[hunter], 18, Paper, TextAnchor.MiddleLeft, 2);
            Text(new Rect(chip.x + 70, chip.y + 34, Mathf.Max(100, chip.width - 156), 22), CampaignCatalog.HunterRoles[hunter].Split('·')[1].Trim(), 13, Muted, TextAnchor.MiddleLeft, 0);
            if (many) Text(new Rect(chip.xMax - 78, chip.y, 70, chip.height), "교체", 14, Amber, TextAnchor.MiddleCenter, 1);

            string rewardLabel = controller.CrisisSelection ? "위기 클리어" : profile.FirstCleared[stage.Index] ? "재도전" : "첫 클리어";
            string reward = rewardLabel + " +" + profile.VictoryReward(stage.Index, controller.CrisisSelection);
            Icon(new Rect(x, r.yMax - 98, 24, 24), "icon-core", Color.white);
            Text(new Rect(x + 30, r.yMax - 100, 160, 28), reward, 15, Paper, TextAnchor.MiddleLeft, 1);
            if (profile.FirstSealComplete)
                Button(605, new Box2(x + w - 150, r.yMax - 102, 150, 32), controller.CrisisSelection ? "위기 작전 켜짐" : "위기 작전", true, controller.CrisisSelection ? Look.Danger : Look.Ghost, null, 14);
            Button(606, new Box2(x, r.yMax - 66, w, 56), "출전", !controller.SaveBlocked, Look.Primary, "icon-play", 22);
        }

        void Growth()
        {
            CampaignProfile profile = controller.Profile;
            Fill(Full(), GatefallArt.Hex("1A222C"));
            Header("성장", true);
            float colW = (W - 54) / 2;
            Rect left = new Rect(18, 80, colW, H - 96), right = new Rect(36 + colW, 80, colW, H - 96);
            Skin(left, "ui-panel"); Skin(right, "ui-panel");
            Text(new Rect(left.x + 20, left.y + 12, 300, 30), "각성자", 20, Paper, TextAnchor.MiddleLeft, 2);
            Text(new Rect(left.x + 120, left.y + 14, colW - 140, 28), "성장마다 생명 +8% · 피해 +5%", 13, Muted, TextAnchor.MiddleRight, 0);
            float ch = (left.height - 70) / 3;
            for (int i = 0; i < 3; i++) {
                bool open = profile.IsHunterOpen(i), chosen = i == profile.SelectedHunter;
                Rect c = new Rect(left.x + 14, left.y + 52 + i * ch, left.width - 28, ch - 8);
                Skin(c, chosen ? "ui-button" : "ui-panel-flat");
                if (chosen) Skin(c, "ui-selected");
                float portrait = Mathf.Min(88, c.height - 16);
                Tex(new Rect(c.x + 8, c.center.y - portrait / 2, portrait, portrait), art.Texture("hunter-" + i), open ? Color.white : new Color(.3f, .3f, .35f, 1));
                float tx = c.x + portrait + 20, textWidth = c.xMax - 174 - tx;
                Text(new Rect(tx, c.y + 6, textWidth, 28), CampaignCatalog.Hunters[i], 19, open ? Paper : Muted, TextAnchor.MiddleLeft, 2);
                Text(new Rect(tx, c.y + 34, open ? textWidth : c.xMax - tx - 12, 64), open ? profile.Grade(i) + "\n" + CampaignCatalog.HunterRoles[i].Split('·')[1].Trim()+
                    "\n생명 +"+(profile.Growth[i]*8)+"% · 피해 +"+(profile.Growth[i]*5)+"%" : i == 1 ? "1-3 종현 배급소 클리어" : "1-6 잿빛 탑 봉인", 13, Muted, TextAnchor.UpperLeft, 0);
                if (!open) continue;
                if (!chosen) Button(620 + i, new Box2(c.xMax - 162, c.y + 8, 150, 48), "출전 선택", true, Look.Secondary, null, 15);
                else Text(new Rect(c.xMax - 162, c.y + 8, 150, 40), "출전 중", 15, Amber, TextAnchor.MiddleCenter, 2);
                string reason = controller.Campaign.GrowthReason(i);
                string growthLabel = reason.Contains("후속 구역") ? "추가 성장 예정" : reason.Contains("1-6") ? "1-6 봉인 후" : reason;
                Button(630 + i, new Box2(c.xMax - 162, c.yMax - 56, 150, 48), reason == "" ? "승급 · " + controller.Campaign.GrowthPrice(i) : growthLabel, reason == "", Look.Secondary, reason == "" ? "icon-core" : null, 14);
            }

            Text(new Rect(right.x + 20, right.y + 12, 300, 30), "결계 연구", 20, Paper, TextAnchor.MiddleLeft, 2);
            Text(new Rect(right.x + 140, right.y + 14, colW - 160, 28), "유채린의 공방 · 전투 밖에서 유지", 13, Muted, TextAnchor.MiddleRight, 0);
            TowerKind[] kinds = { TowerKind.Autocannon, TowerKind.Binding, TowerKind.Rupture, TowerKind.Barrier };
            float rh = (right.height - 130) / 4;
            for (int i = 0; i < 4; i++) {
                int rank = profile.Research[i];
                Rect row = new Rect(right.x + 14, right.y + 52 + i * rh, right.width - 28, rh - 8);
                Skin(row, "ui-panel-flat");
                Tex(new Rect(row.x + 6, row.center.y - 30, 48, 60), art.TowerIcon(kinds[i], 1), Color.white);
                float tx = row.x + 66;
                Text(new Rect(tx, row.y + 4, row.xMax - 84 - tx, 26), CampaignCatalog.ResearchNames[i], 17, Paper, TextAnchor.MiddleLeft, 2);
                Text(new Rect(tx, row.y + 30, row.xMax - 162 - tx, 40), CampaignCatalog.ResearchEffects[i], 13, Muted, TextAnchor.UpperLeft, 0);
                for (int k = 0; k < 3; k++) Fill(new Rect(row.xMax - 76 + k * 20, row.y + 6, 14, 14), k < rank ? Ally : new Color(1, 1, 1, .15f));
                string reason = controller.Campaign.ResearchReason(i);
                Button(640 + i, new Box2(row.xMax - 150, row.yMax - 50, 140, 48), reason == "" ? "연구 · " + CampaignCatalog.ResearchCosts[rank] : rank >= 3 ? "최대" : "잠김", reason == "", Look.Secondary, reason == "" ? "icon-core" : null, 14);
            }
            int invested = controller.Campaign.ResearchInvestment();
            Button(645, new Box2(right.x + 14, right.yMax - 70, right.width - 28, 52), "연구 초기화 · 마핵 " + invested + " 반환", invested > 0, Look.Ghost, "icon-back", 15);
        }

        void Settings()
        {
            CampaignProfile profile = controller.Profile;
            Fill(Full(), GatefallArt.Hex("1A222C"));
            Header("설정", true);
            Rect panel = new Rect(W / 2 - 340, 84, 680, H - 100);
            Skin(panel, "ui-panel");
            float x = panel.x + 26, w = panel.width - 52;
            Text(new Rect(x, panel.y + 16, 200, 44), "소리", 18, Paper, TextAnchor.MiddleLeft, 2);
            Button(651, new Box2(x + w - 160, panel.y + 14, 160, 48), controller.Muted ? "꺼짐" : "켜짐", true, Look.Secondary, controller.Muted ? "icon-sound-off" : "icon-sound-on", 16);
            Fill(new Rect(x, panel.y + 74, w, 1), new Color(1, 1, 1, .1f));
            Text(new Rect(x, panel.y + 86, w, 28), "작전 기록", 18, Paper, TextAnchor.MiddleLeft, 2);
            int last = -1; for (int i = 0; i < CampaignCatalog.Stages.Length; i++) if (profile.BestStars[i] > 0) last = i;
            Text(new Rect(x, panel.y + 118, w, 96), last < 0 ? CampaignCatalog.Prologue : CampaignCatalog.Stages[last].ClearStory, 15, Paper, TextAnchor.UpperLeft, 0);
            Text(new Rect(x, panel.y + 214, w, 66), "별점은 전투 중 가장 크게 깎인 결계 기준입니다. 10% 이하 ★★★ · 30% 이하 ★★ · 생존 ★\n마핵은 첫 클리어 보상과 재도전 +2로 얻고, 성장 화면에서 씁니다.", 13, Muted, TextAnchor.UpperLeft, 0);
            Fill(new Rect(x, panel.yMax - 92, w, 1), new Color(1, 1, 1, .1f));
            Button(601, new Box2(x, panel.yMax - 76, 260, 52), "전투 실험실 (개발용)", true, Look.Ghost, null, 15);
            Text(new Rect(x + w - 220, panel.yMax - 76, 220, 52), "Gatefall v" + Application.version + " · Android", 13, Muted, TextAnchor.MiddleRight, 0);
        }

        void CampaignResult()
        {
            bool won = game.State == RunState.Won;
            Fill(Full(), new Color(0, 0, 0, .52f));
            Rect r = new Rect(W / 2 - 300, H / 2 - 200, 600, 400);
            Skin(r, "ui-panel");
            string title = won ? (game.Rules.Stage.Boss ? "잿빛 탑 봉인" : "범람 저지") : "방어 실패";
            if (game.Rules.Crisis) title = won ? "위기 작전 성공" : "위기 작전 실패";
            Text(new Rect(r.x, r.y + 18, r.width, 46), title, 32, won ? Amber : Threat, TextAnchor.MiddleCenter, 2);
            int stars = CampaignCatalog.Stars(won, game.MinimumCoreHealth, game.Balance.CoreHealth);
            for (int i = 0; i < 3; i++) Icon(new Rect(r.center.x - 108 + i * 76, r.y + 72, i == 1 ? 72 : 64, i == 1 ? 72 : 64), i < stars ? "icon-star" : "icon-star-empty", Color.white);
            Text(new Rect(r.x, r.y + 148, r.width, 22), "가장 크게 깎인 결계 " + (game.PeakErosion * 100).ToString("0") + "%  ·  처치 " + game.Kills + (game.Telemetry.Fusions > 0 ? "  ·  조합 " + game.Telemetry.Fusions : ""), 14, Muted, TextAnchor.MiddleCenter, 0);
            string story = won ? game.Rules.VictorySummary() : game.DefeatReason + "  " + (game.CoreHealth <= 0 ? "뚫린 진입로 쪽에 결계주를 나눠 세우세요." : "문지기 전에 화력을 모으세요.");
            if (!won && game.CoreHealth <= 0 && game.Telemetry.MostDamagingLane >= 0)
                story = game.DefeatReason + "\n다음에는 피해가 컸던 " + GateName(game.Telemetry.MostDamagingLane) + " 진입로를 보강하세요.";
            Text(new Rect(r.x + 34, r.y + 180, r.width - 68, 58), story, 16, Paper, TextAnchor.MiddleCenter, 0);
            if (controller.ResultSaved) {
                if (controller.ResultReward > 0) {
                    Skin(new Rect(r.center.x - 90, r.y + 246, 180, 44), "ui-chip");
                    Icon(new Rect(r.center.x - 78, r.y + 250, 36, 36), "icon-core", Color.white);
                    Text(new Rect(r.center.x - 36, r.y + 246, 120, 44), "+" + controller.ResultReward, 22, Paper, TextAnchor.MiddleLeft, 2);
                }
            } else Text(new Rect(r.x + 20, r.y + 246, r.width - 40, 44), "결과를 저장하지 못했습니다 · " + controller.CampaignError, 14, Threat, TextAnchor.MiddleCenter, 1);
            float by = r.yMax - 82;
            if (!controller.ResultSaved) { Button(690, new Box2(r.center.x - 150, by, 300, 60), "저장 다시 시도", true, Look.Primary, null, 18); return; }
            bool next = won && controller.SelectedStage < CampaignCatalog.Stages.Length-1 && controller.Profile.IsStageOpen(controller.SelectedStage + 1) && !game.Rules.Crisis;
            if (next) {
                Button(600, new Box2(r.x + 24, by, 150, 60), "지도", true, Look.Secondary, null, 17);
                Button((int)MobileAction.Restart, new Box2(r.x + 184, by, 150, 60), "다시", true, Look.Secondary, null, 17);
                Button(607, new Box2(r.x + 344, by, 232, 60), "다음 작전", true, Look.Primary, "icon-play", 19);
            } else {
                Button(600, new Box2(r.x + 24, by, 200, 60), "지도", true, Look.Secondary, null, 17);
                Button((int)MobileAction.Restart, new Box2(r.x + 236, by, 340, 60), won ? "다시 도전" : "다시 도전", true, Look.Primary, null, 19);
            }
        }
    }
}
