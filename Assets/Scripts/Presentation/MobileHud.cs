using System.Collections.Generic;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    // UI is drawn from a snapshot rebuilt every frame. Only the finger-aware router executes actions:
    // every button here registers a TouchRegion with the same box it draws.
    public sealed partial class MobileHud
    {
        private enum Kind { Skin, Texture, Text, Fill, Crop }
        public enum Look { Primary, Secondary, Ghost, Danger }
        private struct Item
        {
            public Kind Kind; public Rect Rect, Uv; public string Skin, Text; public Texture Texture; public Color Color;
            public int Size, Weight; public TextAnchor Align; public bool Shadow;
        }
        private readonly List<Item> items = new List<Item>();
        private readonly GatefallGame controller;
        private readonly GatefallArt art;
        private readonly Dictionary<string, GUIStyle> skins = new Dictionary<string, GUIStyle>();
        private GUIStyle label;
        private DefenseSimulation game;
        private MobileLayout layout;
        private float W, H;
        private Box2 activeTowerPanel;
        private Rect bossPanelGuard;
        private readonly List<Box2> headerRifts = new List<Box2>(4);
        private readonly List<Box2> fusionTowerGuards = new List<Box2>(24);
        private readonly List<Rect> enemyHealthLabels = new List<Rect>(8);
        private const float BossHealthBarY = 84, BossHealthBarHeight = 12;
        private const string BossWarningText = "문지기 공격 예고 · 붉은 원에서 벗어나세요";
        private static readonly Color Paper = GatefallArt.Paper, Muted = GatefallArt.Muted, Ink = GatefallArt.Ink, Amber = GatefallArt.Amber, Ally = GatefallArt.Ally, Threat = GatefallArt.Threat;

        public MobileHud(GatefallGame controller, GatefallArt art) { this.controller = controller; this.art = art; }
        public void Dispose() { DisposeCanvas(); }

        public void Refresh()
        {
            game = controller.Simulation; layout = controller.Layout; W = layout.Width; H = layout.Height;
            items.Clear(); controller.TouchInput.Regions.Clear();
            activeTowerPanel=new Box2();
            controller.TouchInput.Modal = controller.Menu != GameMenu.None || game.AwaitingCard || game.State == RunState.Won || game.State == RunState.Lost;
            if (controller.ScreenPage != CampaignScreen.Battle) { controller.TouchInput.Modal = true; CampaignPage(); return; }
            bool ended = game.State == RunState.Won || game.State == RunState.Lost;
            if (!ended && !game.AwaitingCard && controller.Menu == GameMenu.None && game.State == RunState.Running) EnemyHealthLabels();
            Enemy boss = ended ? null : game.Enemies.Find(e => e.Kind == EnemyKind.Boss && e.Health > 0);
            bossPanelGuard = new Rect();
            // Keep the menu in one place during the final wave, including attack windup and cleanup.
            CampaignStage stage = game.Rules?.Stage;
            if (!ended && stage != null && stage.Boss && (boss != null || game.Wave >= stage.Counts.Length - 1)) {
                Box2 warning = layout.NoticeBox(BossWarningText.Length, false);
                float width = Mathf.Max(360, warning.Width);
                bossPanelGuard = new Rect((W - width) / 2, 62, width, warning.Y + warning.Height - 62);
            }
            if (!ended) TopBar(boss);
            if (!ended && game.ExpandedArena && !controller.Overview && controller.Menu == GameMenu.None && !game.AwaitingCard) OffscreenThreats();
            if (ended) { if (controller.IsCampaign) CampaignResult(); else LabResult(); }
            else if (game.AwaitingCard) Cards(boss != null);
            else if (controller.Menu == GameMenu.Socket) {
                SocketPanel();
                if (game.State == RunState.Running) EnemyHealthLabels(
                    new Rect(activeTowerPanel.X, activeTowerPanel.Y, activeTowerPanel.Width, activeTowerPanel.Height));
                // Buttons win first; the rest of the panel consumes touches before field pads.
                controller.TouchInput.Regions.Add(new TouchRegion(0, activeTowerPanel));
                Pads(); controller.TouchInput.Regions.Add(new TouchRegion(100, new Box2(0, 0, W, H)));
            }
            else if (controller.Menu == GameMenu.FusionPick) FusionPick();
            else if (controller.Menu == GameMenu.Pause) PauseScreen();
            else { Controls(); Pads(); }
            if (!ended && controller.Menu != GameMenu.Pause && !game.AwaitingCard) {
                if (boss != null && boss.BossWindup > 0) Toast(BossWarningText, true);
                else if (controller.NoticeUntil > Time.unscaledTime) Toast(controller.Notice);
            }
        }

        // ---------------------------------------------------------------- battle
        private void EnemyHealthLabels(Rect? towerPanel = null)
        {
            enemyHealthLabels.Clear();
            Rect barrierHud = new Rect(0, 0, 342, 63);
            Rect waveHud = new Rect(W / 2 - 180, 0, 360, 105);
            Rect creditsHud = new Rect(W - 350, 0, 350, 83);
            foreach (Enemy enemy in game.Enemies) {
                if (enemy.Kind != EnemyKind.Brute && enemy.Kind != EnemyKind.Siege) continue;
                Vector2 point = controller.EnemyHealthReference(enemy);
                float width = enemy.Armor > 0 ? 64 : 46;
                Rect area = new Rect(point.x - width/2, point.y - 8, width, 16);
                if (area.xMin < 6 || area.xMax > W - 6 || area.yMin < 6 || area.yMax > H - 92) continue;
                if (area.Overlaps(barrierHud) || area.Overlaps(waveHud) || area.Overlaps(creditsHud)) continue;
                // Socket menus are drawn after the HUD. Keep their bounds and the movable
                // wave header clear while preserving enemy labels in the rest of the arena.
                if (towerPanel.HasValue && (area.yMin < 60 || area.Overlaps(towerPanel.Value))) continue;
                if (game.HunterDown && area.Overlaps(new Rect(14, 68, 244, 42))) continue;
                bool overlaps = false;
                foreach (Rect other in enemyHealthLabels) if (other.Overlaps(area)) { overlaps = true; break; }
                if (overlaps) continue;
                enemyHealthLabels.Add(area);
                Fill(area, new Color(.07f, .09f, .12f, .83f));
                Text(area, (enemy.Armor > 0 ? "장갑 " : "") + Mathf.CeilToInt(enemy.Health), 12,
                    enemy.Armor > 0 ? Amber : Paper, TextAnchor.MiddleCenter, 2);
                if (enemyHealthLabels.Count == 8) break;
            }
        }

        private void OffscreenThreats()
        {
            int left = 0, right = 0, north = 0, south = 0;
            foreach (Enemy e in game.Enemies) {
                Vector3 p = controller.GameCamera.WorldToViewportPoint(new Vector3(e.Position.X, e.Position.Y, 0));
                if (p.x < 0) left++; else if (p.x > 1) right++;
                else if (p.y > 1) north++; else if (p.y < 0) south++;
            }
            if (left > 0) ThreatTag(new Rect(12, H / 2 - 30, 130, 44), "◀ 적 " + left);
            if (right > 0) ThreatTag(new Rect(W - 142, H / 2 - 30, 130, 44), "적 " + right + " ▶");
            if (north > 0) { Box2 b = layout.NorthThreatTag(); ThreatTag(new Rect(b.X, b.Y, b.Width, b.Height), "▲ 적 " + north); }
            if (south > 0) ThreatTag(new Rect(W / 2 - 65, H - 165, 130, 44), "▼ 적 " + south);
        }
        private void ThreatTag(Rect r, string text)
        {
            Skin(r, "ui-chip"); Text(r, text, 16, Threat, TextAnchor.MiddleCenter, 2);
        }

        private void TopBar(Enemy boss)
        {
            float hp = Mathf.Clamp01(game.CoreHealth / game.Balance.CoreHealth);
            Skin(new Rect(14, 10, 252, 50), "ui-chip");
            Icon(new Rect(24, 15, 40, 40), "icon-barrier", Color.white);
            bool crisis = game.Rules != null && !game.Rules.Lab && game.Rules.Crisis;
            Text(new Rect(70, 12, 120, 18), crisis ? "위기 · 도시 결계" : "도시 결계", 13,
                crisis ? Amber : Muted, TextAnchor.MiddleLeft, 0);
            Bar(new Rect(70, 33, 136, 12), hp, hp < .3f ? Threat : Ally);
            Text(new Rect(208, 14, 52, 40), Mathf.CeilToInt(game.CoreHealth).ToString(), 20, Paper, TextAnchor.MiddleCenter, 2);
            if (game.HunterDown) {
                Skin(new Rect(14, 68, 244, 42), "ui-chip");
                Text(new Rect(22, 68, 228, 42), "헌터 부활까지 " + Mathf.CeilToInt(game.HunterRespawnRemaining) + "초", 16, Amber, TextAnchor.MiddleCenter, 2);
            }

            Box2 wave = new Box2(W/2-90.5f,4,181,52);
            if(game.WaveClearVictory && !game.IsPreparing && (!game.ExpandedArena || controller.Overview || controller.Menu==GameMenu.FusionPick)) {
                headerRifts.Clear();
                // Current rift ink at its1.55 maximum warning scale fits a5.1x3.2 world-unit guard.
                // Reuse this list and fixed guard so a pulsing rift does not move the header every frame.
                float halfX=controller.WorldToReferenceScale(2.55f),halfY=controller.WorldToReferenceScale(1.6f);
                foreach(int lane in game.OpenLanes) {
                    Vector2 p=controller.WorldToReference(game.GatePositions[lane],0);
                    Box2 r=new Box2(p.x-halfX,p.y-halfY,halfX*2,halfY*2);
                    if(r.Y<62 && r.Y+r.Height>0)headerRifts.Add(r);
                }
                wave=layout.WaveHeader(headerRifts);
            }
            float waveWidth=Mathf.Min(180,wave.Width),waveX=wave.Center.X;
            string progress = game.WaveClearVictory ? (game.AllWavesSpawned ? "잔여 적 " + game.Enemies.Count : "웨이브 " + (game.Wave + 1) + "/" + game.WaveCount) : Clock(game.Remaining);
            Text(new Rect(waveX-waveWidth/2, 4, waveWidth, game.WaveClearVictory ? 32 : 40), progress, game.WaveClearVictory ? 24 : 30, Paper, TextAnchor.MiddleCenter, 2, true);
            if(game.WaveClearVictory && !game.IsPreparing)Text(new Rect(waveX-waveWidth/2, 34, waveWidth, 14), "경과 " + Clock((float)game.Elapsed), 11, Paper, TextAnchor.MiddleCenter, 1, true);
            if (game.IsPreparing) Text(new Rect(W / 2 - 150, 44, 300, 22), "배치 준비 · 시간 정지", 14, Amber, TextAnchor.MiddleCenter, 1, true);
            else {
                float gap = wave.Width<181?4:5, seg = (wave.Width-5*gap)/6, x0 = wave.X;
                for (int i = 0; i < 6; i++) {
                    Rect r = new Rect(x0 + i * (seg + gap), 48, seg, 8);
                    Fill(r, GatefallArt.Alpha(Ink, .75f));
                    float f = game.WaveProgressFraction(i);
                    bool bossWave = game.Rules != null && game.Rules.Stage != null && game.Rules.Stage.Boss && i == 5;
                    if (f > 0) Fill(new Rect(r.x, r.y, r.width * f, r.height), bossWave ? Threat : Paper);
                    else if (bossWave) Fill(new Rect(r.x + seg / 2 - 2, r.y, 4, r.height), Threat);
                }
            }
            float right = W - 92;
            bool catalysts = game.Rules != null && (game.Rules.Lab || game.Rules.Stage.HasCatalysts);
            if (catalysts) {
                Skin(new Rect(right - 86, 10, 86, 50), "ui-chip");
                Icon(new Rect(right - 80, 17, 36, 36), "icon-catalyst", Color.white);
                Text(new Rect(right - 42, 12, 36, 46), game.Cores.ToString(), 22, Ally, TextAnchor.MiddleCenter, 2);
                right -= 94;
            }
            Skin(new Rect(right - 128, 10, 128, 50), "ui-chip");
            Icon(new Rect(right - 122, 15, 40, 40), "icon-stone", Color.white);
            Text(new Rect(right - 80, 12, 74, 46), game.Credits.ToString(), 24, Amber, TextAnchor.MiddleCenter, 2);
            int towerCapacity = game.TowerLimit > 0 ? game.TowerLimit : game.BoardSockets.Length;
            Text(new Rect(right - 118, 62, 118, 18), "결계주 " + game.Towers.Count + "/" + towerCapacity, 12,
                game.TowerLimit > 0 && game.Towers.Count >= game.TowerLimit ? Threat : Paper, TextAnchor.MiddleRight, 1);
            if (controller.Menu != GameMenu.Pause && !game.AwaitingCard && (game.State == RunState.Running || game.IsPreparing))
                IconButton((int)MobileAction.Pause, layout.Button(MobileAction.Pause), "icon-pause");

            if (boss != null) {
                Text(new Rect(W / 2 - 170, 62, 340, 20), "잿빛 문지기  " + Mathf.CeilToInt(boss.Health) + " / " + Mathf.CeilToInt(boss.MaxHealth), 14, Threat, TextAnchor.MiddleCenter, 2, true);
                Bar(new Rect(W / 2 - 170, BossHealthBarY, 340, BossHealthBarHeight), boss.Health / boss.MaxHealth, Threat);
            } else if (game.Rules != null && game.Rules.Stage != null && game.Rules.Stage.Boss && game.BossDefeated) {
                Skin(new Rect(W / 2 - 180, 62, 360, 38), "ui-chip");
                Text(new Rect(W / 2 - 180, 62, 360, 18), "문지기 처치 완료", 13, Ally, TextAnchor.MiddleCenter, 2);
                Text(new Rect(W / 2 - 180, 80, 360, 20), "남은 적을 모두 처치하세요", 14, Paper, TextAnchor.MiddleCenter, 2);
            } else if(game.WaveClearVictory && game.AllWavesSpawned && !game.IsPreparing && (!game.ExpandedArena || controller.Overview)) {
                Text(new Rect(W / 2 - 180, 62, 360, 20), "최종 웨이브 출현 완료 · 남은 적을 처치하세요", 13, Ally, TextAnchor.MiddleCenter, 2);
            }
        }

        private void Controls()
        {
            if(game.CanCallNextWave()=="") {
                Box2 b=layout.WaveCallBox();WaveBriefing next=game.GetWaveBriefing(game.NextCallWave);
                Button(MobileLayout.WaveCallActionBase+game.NextCallWave,b,"",true,Look.Primary);
                Text(new Rect(b.X+12,b.Y+3,b.Width-24,26),"앞당겨 호출 · "+game.CallAdvanceSeconds.ToString("F0")+"초",18,Ink,TextAnchor.MiddleCenter,1);
                Text(new Rect(b.X+12,b.Y+30,b.Width-24,23),next.Summary,13,Ink,TextAnchor.MiddleCenter,1);
                Text(new Rect(b.X+12,b.Y+54,b.Width-24,24),"적 "+game.Enemies.Count+"마리와 겹침 · 보급"+game.WaveSupplyCredits+" 먼저",14,Ink,TextAnchor.MiddleCenter,1);
            }
            if (game.State == RunState.Running)
                Button((int)MobileAction.Speed, layout.Button(MobileAction.Speed), controller.DoubleSpeed ? "2×" : "1.25×", true, controller.DoubleSpeed ? Look.Primary : Look.Secondary, null, 18);
            if (game.ExpandedArena && (game.State == RunState.Running || game.IsPreparing))
                Button((int)MobileAction.Map, new Box2(W - 176, 78, 160, 46), controller.Overview ? "추적 보기" : "전체 보기", true, Look.Secondary, null, 15);
            if (game.IsPreparing) {
                Button((int)MobileAction.Start, layout.Button(MobileAction.Start), "범람 시작", true, Look.Primary, "icon-play", 22);
                if (controller.LessonText != "") {
                    float lessonWidth = Mathf.Min(660, W - 380);
                    Rect r = new Rect((W - lessonWidth) / 2, 70, lessonWidth, W > 1100 ? 48 : 72);
                    Skin(r, "ui-panel-flat");
                    Text(new Rect(r.x + 18, r.y + 4, r.width - 36, r.height - 8), controller.LessonText.Replace(". ", ".\n"), 15, Paper, TextAnchor.MiddleCenter, 0);
                }
            }
            if (game.State == RunState.Running && game.Rules != null) {
                Box2 b = layout.Button(MobileAction.Skill);
                Rect r = new Rect(b.X, b.Y, b.Width, b.Height);
                float cd = game.SkillCooldown;
                float active = game.HunterSkillActiveRemaining;
                bool ready = cd <= 0 && !game.HunterDown;
                bool showingEffect = active > 0 && !game.HunterDown;
                string label = controller.SkillName;
                if (game.HunterDown) label = "부활 대기";
                else if (showingEffect) label += " " + Mathf.CeilToInt(active) + "초";
                else if (!ready) label = Mathf.CeilToInt(cd) + "초";
                Color ring = showingEffect ? Amber : ready ? Ally : Muted;
                Tex(r, art.Texture("fx-disc"), GatefallArt.Alpha(GatefallArt.Slate, .82f));
                Tex(new Rect(r.x - 4, r.y - 4, r.width + 8, r.height + 8), art.Texture("fx-ring"), GatefallArt.Alpha(ring, .9f));
                Icon(new Rect(r.x + 28, r.y + 22, 72, 72), "icon-skill-" + Mathf.Clamp(game.Rules.Hunter, 0, 2), ready || showingEffect ? Color.white : new Color(1, 1, 1, .35f));
                int fontSize = showingEffect || ready || game.HunterDown ? 15 : 20;
                Text(new Rect(r.x, r.y + r.height - 34, r.width, 24), label, fontSize, showingEffect ? Amber : Paper, TextAnchor.MiddleCenter, 2, true);
                if (ready) controller.TouchInput.Regions.Add(new TouchRegion(670, b));
            }
            if (game.State == RunState.Running || game.IsPreparing) {
                V2 c = layout.JoystickCenter, m = controller.TouchInput.Movement;
                Tex(new Rect(c.X - 62, c.Y - 62, 124, 124), art.Texture("fx-disc"), new Color(.07f, .1f, .14f, .32f));
                Tex(new Rect(c.X - 66, c.Y - 66, 132, 132), art.Texture("fx-ring"), new Color(1, 1, 1, .45f));
                Tex(new Rect(c.X + m.X * 44 - 28, c.Y - m.Y * 44 - 28, 56, 56), art.Texture("fx-disc"), GatefallArt.Alpha(Ally, .9f));
            }
        }

        // Every pad on the visible board is a touch target. Registered after HUD buttons so those win.
        private void Pads()
        {
            if (!(game.State == RunState.Running || game.IsPreparing)) return;
            for (int i = 0; i < game.BoardSockets.Length; i++) controller.TouchInput.Regions.Add(new TouchRegion(400 + i, PadBox(game.BoardSockets[i])));
        }

        // Tap target for a pad and the tower standing on it: 2.4 units wide, 0.9 below to 2.3 above the footprint.
        private Box2 PadBox(V2 socket)
        {
            Vector2 p = controller.WorldToReference(socket, 0);
            float w = controller.WorldToReferenceScale(2.4f), below = controller.WorldToReferenceScale(.9f), above = controller.WorldToReferenceScale(2.3f);
            return new Box2(p.x - w / 2, p.y - above, w, above + below);
        }

        private static string ShortRole(TowerKind k)
        {
            switch (k) {
                case TowerKind.Autocannon: return "단일"; case TowerKind.Binding: return "감속"; case TowerKind.Rupture: return "범위";
                case TowerKind.Barrier: return "저지"; case TowerKind.Arc: return "연쇄"; case TowerKind.Shatter: return "집중";
                case TowerKind.Gravity: return "흡인"; case TowerKind.Marker: return "표식"; case TowerKind.Observer: return "집중"; case TowerKind.Beacon: return "표식·감속"; default: return "종결";
            }
        }

        private Rect PlacePanel(int socket, float w, float h)
        {
            Vector2 p = controller.WorldToReference(game.BoardSockets[socket], 0);
            float x = Mathf.Clamp(p.x - w / 2, 12, W - w - 12);
            float y = p.y > H * .5f ? p.y - h - controller.WorldToReferenceScale(2.9f) : p.y + controller.WorldToReferenceScale(.9f);
            y = Mathf.Clamp(y, 70, H - h - 10);
            if (bossPanelGuard.width > 0 && bossPanelGuard.Overlaps(new Rect(x, y, w, h)))
                y = Mathf.Min(bossPanelGuard.yMax + 6, H - h - 10);
            activeTowerPanel=new Box2(x,y,w,h);
            return new Rect(x, y, w, h);
        }

        private void SocketPanel()
        {
            int socket = controller.SelectedSocket; if (socket < 0) return;
            Tower tower = game.TowerAt(socket);
            if (tower == null) {
                List<TowerKind> kinds = new List<TowerKind>();
                foreach (TowerKind k in new[] { TowerKind.Autocannon, TowerKind.Binding, TowerKind.Rupture, TowerKind.Barrier, TowerKind.Marker }) if (game.Allows(k)) kinds.Add(k);
                float w = 28 + kinds.Count * 124 + (kinds.Count - 1) * 8;
                Rect r = PlacePanel(socket, Mathf.Max(w, 160), 196);
                Skin(r, "ui-panel");
                bool atLimit = game.TowerLimit > 0 && game.Towers.Count >= game.TowerLimit;
                Text(new Rect(r.x + 18, r.y + 10, 200, 24), atLimit ? "결계주 상한 " + game.TowerLimit + "기" : "결계주 건설", 15, atLimit ? Threat : Muted, TextAnchor.MiddleLeft, 1);
                for (int i = 0; i < kinds.Count; i++) {
                    TowerKind k = kinds[i]; TowerDefinition d = TowerCatalog.Get(k);
                    string reason = game.CanBuild(socket, k);
                    Rect t = new Rect(r.x + 14 + i * 132, r.y + 40, 124, 144);
                    Button(200 + (int)k, Box(t), "", reason == "", Look.Secondary);
                    Tex(new Rect(t.x + 22, t.y + 2, 80, 100), art.TowerIcon(k, 1), reason == "" || reason.StartsWith("마석") ? Color.white : new Color(1, 1, 1, .4f));
                    Skin(new Rect(t.x + 6, t.y + 6, 44, 22), "ui-chip");
                    Text(new Rect(t.x + 6, t.y + 6, 44, 22), ShortRole(k), 12, GatefallArt.RoleColor(k), TextAnchor.MiddleCenter, 2);
                    Text(new Rect(t.x, t.y + 96, t.width, 22), d.Name.Replace(" 결계주", ""), 16, Paper, TextAnchor.MiddleCenter, 2);
                    Icon(new Rect(t.x + 30, t.y + 119, 20, 20), "icon-stone", Color.white);
                    Text(new Rect(t.x + 52, t.y + 117, 50, 24), d.BuildCost.ToString(), 17, game.Credits >= d.BuildCost ? Amber : Threat, TextAnchor.MiddleLeft, 2);
                }
            } else TowerPanel(tower);
        }

        private void TowerPanel(Tower tower)
        {
            TowerDefinition d = TowerCatalog.Get(tower.Kind);
            List<GatefallGame.FusionOption> options = controller.FusionOptions(tower);
            bool canLevel = tower.Level < game.TowerLevelCap(tower.Kind);
            if (controller.TowerRecipesExpanded) { TowerRecipePanel(tower, options); return; }
            Rect r = PlacePanel(tower.Socket, 464, 222);
            Skin(r, "ui-panel");
            Tex(new Rect(r.x + 12, r.y + 8, 56, 70), art.TowerIcon(tower.Kind, tower.Level), GatefallArt.TowerTint(tower.Kind, tower.Level));
            Text(new Rect(r.x + 76, r.y + 10, 330, 28), d.Name + "  Lv" + tower.Level, 19, Paper, TextAnchor.MiddleLeft, 2);
            Button(100, new Box2(r.x + r.width - 44, r.y + 8, 32, 30), "×", true, Look.Secondary, null, 18);
            TowerStats now = game.Stats(tower);
            TowerStats next = canLevel ? game.Stats(tower, tower.Level + 1) : null;
            string line = StatLine(tower.Kind, now, next) + " · 사거리 " + now.Range.ToString("0.#");
            if (tower.Kind == TowerKind.Beacon && next != null && next.Range != now.Range) line += "→" + next.Range.ToString("0.#");
            Text(new Rect(r.x + 76, r.y + 40, r.width - 90, 22), line, 14, Muted, TextAnchor.MiddleLeft, 0);
            string field = tower.Kind == TowerKind.Barrier ? WardStatus(tower, now.MaxTargets) :
                game.IsPreparing ? "배치 준비" : "범위 적 " + EnemiesInRange(tower.Position, now.Range) + " · 이번 전투 " + controller.TowerShotCount(tower.Id) + "발";
            if (tower.Kind == TowerKind.Barrier && tower.Cooldown <= 0) field += " · 파괴 시 감속 " + DefenseSimulation.WardBurstSlowPercent + "%";
            if (tower.Kind == TowerKind.Marker || tower.Kind == TowerKind.Observer || tower.Kind == TowerKind.Beacon) field += " · 표식 연계";
            if (game.HasResonance(tower)) field += " · 공명 +5%";
            Text(new Rect(r.x + 76, r.y + 62, r.width - 90, 18), field, 13, Muted, TextAnchor.MiddleLeft, 1);
            float x = r.x + 14, y = r.y + 86;
            Rect up = new Rect(x, y, 140, 124);
            string reason = canLevel ? game.CanUpgrade(tower.Id) : "강화 완료";
            Button(210, Box(up), "", canLevel && reason == "", Look.Secondary);
            Icon(new Rect(up.x + 46, up.y + 8, 48, 48), "icon-upgrade", canLevel ? Color.white : new Color(1, 1, 1, .35f));
            Text(new Rect(up.x, up.y + 58, up.width, 24), canLevel ? "Lv" + (tower.Level + 1) + " 강화" : "강화 완료", 17, Paper, TextAnchor.MiddleCenter, 2);
            if (canLevel) Cost(new Rect(up.x, up.y + 88, up.width, 26), d.UpgradePrice(tower.Level), 0);
            else Text(new Rect(up.x, up.y + 88, up.width, 26), "Lv" + tower.Level, 16, Muted, TextAnchor.MiddleCenter, 1);
            Rect fusion = new Rect(x + 148, y, 140, 124);
            int ready = options.FindAll(o => o.Reason == "").Count;
            string fusionStatus = ready > 0 ? "가능 " + ready + "개" : "재료 확인";
            if (options.Count == 0)
                fusionStatus = System.Array.Exists(TowerCatalog.Recipes, r => r.First == tower.Kind || r.Second == tower.Kind) ? "해금·편성 필요" : "후속 조합 없음";
            Button(212, Box(fusion), "", options.Count > 0, Look.Secondary);
            Tex(new Rect(fusion.x + 46, fusion.y + 8, 48, 48), art.TowerIcon(tower.Kind, 2), options.Count > 0 ? Color.white : new Color(1, 1, 1, .35f));
            Text(new Rect(fusion.x, fusion.y + 58, fusion.width, 24), "조합", 17, Ally, TextAnchor.MiddleCenter, 2);
            Text(new Rect(fusion.x, fusion.y + 88, fusion.width, 26), fusionStatus, 14, ready > 0 ? Amber : Muted, TextAnchor.MiddleCenter, 1);
            Rect sell = new Rect(x + 296, y, 140, 124);
            Button(211, Box(sell), "", game.CanSell(tower.Id) == "", controller.SellArmed ? Look.Danger : Look.Secondary);
            Icon(new Rect(sell.x + 46, sell.y + 8, 48, 48), "icon-sell", Color.white);
            Text(new Rect(sell.x, sell.y + 58, sell.width, 24), controller.SellArmed ? "한 번 더 누르면 판매" : "판매", controller.SellArmed ? 13 : 17, Paper, TextAnchor.MiddleCenter, 2);
            Text(new Rect(sell.x, sell.y + 88, sell.width, 26), "+" + game.SaleRefund(tower) + (tower.InvestedCores > 0 ? "  촉매+" + tower.InvestedCores : ""), 16, Amber, TextAnchor.MiddleCenter, 2);
        }

        private string WardStatus(Tower tower, int capacity)
        {
            if (tower.Cooldown > 0) return "재충전 " + Mathf.CeilToInt(tower.Cooldown) + "초 · 저지 중단";
            string durability = "내구 " + Mathf.Max(0, Mathf.CeilToInt(tower.WardHealth)) + "/" + Mathf.CeilToInt(game.WardMaximum(tower));
            if (tower.Commission > 0) return durability + " · 가동 준비";
            int held = 0;
            foreach (Enemy enemy in game.Enemies)
                if (enemy.Health > 0 && enemy.BlockOwner == tower.Id) held++;
            return durability + " · 저지 중 " + held + "/" + capacity;
        }

        private void TowerRecipePanel(Tower tower, List<GatefallGame.FusionOption> options)
        {
            int rows = Mathf.Max(1, (options.Count + 1) / 2);
            Rect r = PlacePanel(tower.Socket, 464, 78 + rows * 116);
            Skin(r, "ui-panel");
            Text(new Rect(r.x + 16, r.y + 8, 330, 26), "조합 · " + TowerCatalog.Get(tower.Kind).Name.Replace(" 결계주", ""), 18, Paper, TextAnchor.MiddleLeft, 2);
            Text(new Rect(r.x + 16, r.y + 37, 330, 24), "선택한 자리에 Lv1 생성 · 재료 Lv2 이상", 13, Muted, TextAnchor.MiddleLeft, 1);
            Button(213, new Box2(r.x + r.width - 92, r.y + 8, 78, 40), "뒤로", true, Look.Secondary, null, 15);
            for (int i = 0; i < options.Count; i++) {
                GatefallGame.FusionOption o = options[i]; FusionRecipe recipe = TowerCatalog.Recipes[o.Recipe];
                Rect t = new Rect(r.x + 14 + (i % 2) * 224, r.y + 68 + (i / 2) * 116, 212, 104);
                Button(220 + o.Recipe, Box(t), "", o.Reason == "", Look.Secondary);
                Color tint = GatefallArt.TowerTint(recipe.Result, 1);
                if (o.Reason != "") tint.a = .5f;
                Tex(new Rect(t.x + 6, t.y + 4, 44, 55), art.TowerIcon(recipe.Result, 1), tint);
                Text(new Rect(t.x + 54, t.y + 10, 150, 25), TowerCatalog.Get(recipe.Result).Name.Replace(" 결계주", ""), 17, GatefallArt.RoleColor(recipe.Result), TextAnchor.MiddleLeft, 2);
                if (o.Reason == "") {
                    string description = recipe.Result == TowerKind.Beacon
                        ? game.Stats(recipe.Result, 1).MaxTargets + "명 표식·감속"
                        : "재료 2개 소비";
                    Text(new Rect(t.x + 54, t.y + 36, 150, 20), description, 12, Muted, TextAnchor.MiddleLeft, 1);
                    Cost(new Rect(t.x + 32, t.y + 70, 148, 26), recipe.Credits, 1);
                } else Text(new Rect(t.x + 54, t.y + 36, 150, 58), o.Reason, 13, Muted, TextAnchor.MiddleLeft, 0);
            }
        }

        private int EnemiesInRange(V2 position, float range)
        {
            int count = 0; float squared = range * range;
            foreach (Enemy enemy in game.Enemies) if (enemy.Health > 0 && V2.DistanceSquared(position, enemy.Position) <= squared) count++;
            return count;
        }

        private static string StatLine(TowerKind kind, TowerStats s, TowerStats next)
        {
            if (kind == TowerKind.Marker) return "표식 3초 · 추가 피해 " + s.MarkBonus.ToString("0") + (next == null ? "" : "→" + next.MarkBonus.ToString("0"));
            if(kind==TowerKind.Observer)return "피해 " + s.Damage.ToString("0") + (next==null ? "" : "→" + next.Damage.ToString("0"));
            if (kind == TowerKind.Barrier) return "저지 2명 · 재충전 " + s.Interval.ToString("0.#") + (next != null ? "→" + next.Interval.ToString("0.#") : "") + "초";
            if (kind == TowerKind.Binding || kind == TowerKind.Beacon) {
                string targets = "최대 " + s.MaxTargets + (next != null && (kind == TowerKind.Binding || next.MaxTargets != s.MaxTargets) ? "→" + next.MaxTargets : "") + "명";
                string slow = "감속 " + (s.Slow * 100).ToString("0") + (next != null ? "→" + (next.Slow * 100).ToString("0") : "") + "%";
                string mark = kind == TowerKind.Beacon ? " · 표식 +" + s.MarkBonus.ToString("0") +
                    (next != null && next.MarkBonus != s.MarkBonus ? "→" + next.MarkBonus.ToString("0") : "") : "";
                return targets + " · " + slow + mark;
            }
            string dmg = (s.Chain != null ? s.Chain[0] : s.Damage).ToString("0");
            if (next != null) dmg += "→" + (next.Chain != null ? next.Chain[0] : next.Damage).ToString("0");
            string extra = s.Slow > 0 ? " · 감속 " + (s.Slow * 100).ToString("0") + (next != null ? "→" + (next.Slow * 100).ToString("0") : "") + "%" :
                s.Radius > 0 ? " · 범위 " + s.Radius.ToString("0.#") + (next != null ? "→" + next.Radius.ToString("0.#") : "") : "";
            if (s.Secondary > 0) {
                string splash = s.Secondary.ToString("0") + (next == null ? "" : "→" + next.Secondary.ToString("0"));
                return "직격 " + dmg + " · 폭발 " + splash + extra;
            }
            return (s.Chain != null ? "연쇄 " : "피해 ") + dmg + extra;
        }

        private void Cost(Rect r, int credits, int catalysts)
        {
            float total = 52 + (catalysts > 0 ? 44 : 0), x = r.x + (r.width - total) / 2;
            Icon(new Rect(x, r.y + 2, 22, 22), "icon-stone", Color.white);
            Text(new Rect(x + 24, r.y, 30, r.height), credits.ToString(), 17, game.Credits >= credits ? Amber : Threat, TextAnchor.MiddleLeft, 2);
            if (catalysts > 0) {
                Icon(new Rect(x + 58, r.y + 2, 22, 22), "icon-catalyst", Color.white);
                Text(new Rect(x + 82, r.y, 20, r.height), catalysts.ToString(), 17, game.Cores >= catalysts ? Ally : Threat, TextAnchor.MiddleLeft, 2);
            }
        }

        private void FusionPick()
        {
            FusionRecipe recipe = TowerCatalog.Recipes[controller.PickRecipe];
            Tower anchor = game.TowerAt(controller.SelectedSocket);
            TowerKind other = anchor != null && anchor.Kind == recipe.First ? recipe.Second : recipe.First;
            float range = game.Stats(recipe.Result, 1).Range;
            fusionTowerGuards.Clear();
            if (anchor != null) fusionTowerGuards.Add(PadBox(anchor.Position));
            foreach (int socket in controller.PickPartners) fusionTowerGuards.Add(PadBox(game.BoardSockets[socket]));
            if (bossPanelGuard.width > 0) fusionTowerGuards.Add(Box(bossPanelGuard));
            bool clear = layout.TryFusionHeader(fusionTowerGuards, out activeTowerPanel);
            Rect r = new Rect(activeTowerPanel.X, activeTowerPanel.Y, activeTowerPanel.Width, activeTowerPanel.Height);
            Skin(r, "ui-panel");
            Tex(new Rect(r.x + 12, r.y + 6, 42, 52), art.TowerIcon(recipe.Result, 1), GatefallArt.TowerTint(recipe.Result, 1));
            Text(new Rect(r.x + 62, r.y + 6, 420, 26), "빛나는 " + TowerCatalog.Get(other).Name.Replace(" 결계주", "") + " Lv2 이상을 탭하세요", 18, Paper, TextAnchor.MiddleLeft, 2);
            Text(new Rect(r.x + 62, r.y + 34, 420, 22), "주황색 자리에 " + TowerCatalog.Get(recipe.Result).Name.Replace(" 결계주", "") + " 생성 · 사거리 " + range.ToString("0.#"), 14, Amber, TextAnchor.MiddleLeft, 0);
            const string vacancy = "상대 재료 자리가 빕니다";
            string field = game.IsPreparing ? vacancy + " · 다른 길은 취소 후 재선택"
                : "범위 내 적 " + (anchor == null ? 0 : EnemiesInRange(anchor.Position, range)) + " · " + vacancy + " · 취소 후 재선택 가능";
            Text(new Rect(r.x + 62, r.y + 60, 420, 22), field, 13, Muted, TextAnchor.MiddleLeft, 0);
            Button(100, new Box2(r.x + r.width - 112, r.y + 8, 100, 48), "취소", true, Look.Secondary, null, 16);
            // On a fallback layout, keep any partner behind the panel selectable.
            if (clear) controller.TouchInput.Regions.Add(new TouchRegion(0, activeTowerPanel));
            foreach (int socket in controller.PickPartners) controller.TouchInput.Regions.Add(new TouchRegion(450 + socket, PadBox(game.BoardSockets[socket])));
        }

        private void Cards(bool bossAlive)
        {
            Fill(Full(), new Color(0, 0, 0, .5f));
            float titleTop = bossAlive ? BossHealthBarY + BossHealthBarHeight + 6 : 86;
            Text(new Rect(W / 2 - 300, titleTop, 600, 36), "잔광 성장 · 하나를 고르세요", 24, Paper, TextAnchor.MiddleCenter, 2, true);
            if (controller.CardInterruptsTowerMenu)
                Text(new Rect(W / 2 - 300, titleTop + 33, 600, 20), "선택 후 이전 화면으로 돌아갑니다", 14, Paper, TextAnchor.MiddleCenter, 0);
            int n = game.CardOffers.Count; float cw = 236, gap = 20, x0 = W / 2 - (n * cw + (n - 1) * gap) / 2;
            int reachable = game.ReachableTowerFamilies();
            for (int i = 0; i < n; i++) {
                CardKind c = game.CardOffers[i];
                Rect r = new Rect(x0 + i * (cw + gap), titleTop + 54, cw, 280);
                Button(500 + (int)c, Box(r), "", controller.CanSelectGrowthCard, Look.Secondary);
                Skin(r, "ui-panel");
                Texture icon = CardIcon(c);
                Tex(new Rect(r.x + cw / 2 - 44, r.y + 18, 88, c <= CardKind.Blast ? 110 : 88), icon, Color.white);
                Text(new Rect(r.x + 10, r.y + 140, cw - 20, 32), TowerCatalog.CardName(c), 21, Amber, TextAnchor.MiddleCenter, 2);
                Text(new Rect(r.x + 16, r.y + 176, cw - 32, 56), CardText(c, reachable), 16, Paper, TextAnchor.MiddleCenter, 0);
                Text(new Rect(r.x + 10, r.y + 240, cw - 20, 24), c == CardKind.Credits ? "즉시 획득" : "보유 " + game.Cards[(int)c] + " / 2", 13, Muted, TextAnchor.MiddleCenter, 0);
            }
        }
        // Player-facing wording; TowerCatalog keeps the design-document terms.
        private static string CardText(CardKind c, int reachable)
        {
            switch (c) {
                case CardKind.Ballistic: return TaggedCardText(c, reachable, "피해 +15%");
                case CardKind.Control: return TaggedCardText(c, reachable, "감속 지속 +0.4초");
                case CardKind.Blast: return TaggedCardText(c, reachable, "폭발 반경 +0.25");
                case CardKind.Range: return "모든 결계주 사거리 +10%";
                case CardKind.Credits: return "마석 +35 즉시";
                default: return "각성자 공격 간격 -10%";
            }
        }
        private static string TaggedCardText(CardKind card, int reachable, string effect)
        {
            TowerTag tag = TowerCatalog.CardTag(card);
            var names = new List<string>();
            foreach (TowerDefinition tower in TowerCatalog.All)
                if ((tower.Tags & tag) != 0 && (reachable & (1 << (int)tower.Kind)) != 0)
                    names.Add(tower.Name.Replace(" 결계주", ""));
            if (names.Count == 0) return effect;
            return names.Count == 1 ? names[0] + " " + effect : effect + "\n(" + string.Join("·", names) + ")";
        }
        private Texture CardIcon(CardKind c)
        {
            switch (c) {
                case CardKind.Ballistic: return art.TowerIcon(TowerKind.Autocannon, 2);
                case CardKind.Control: return art.TowerIcon(TowerKind.Binding, 2);
                case CardKind.Blast: return art.TowerIcon(TowerKind.Rupture, 2);
                case CardKind.Credits: return art.Texture("icon-stone");
                case CardKind.Hunter: return art.Texture("icon-skill-" + (game.Rules != null ? Mathf.Clamp(game.Rules.Hunter, 0, 2) : 0));
                default: return art.Texture("icon-upgrade");
            }
        }

        private void PauseScreen()
        {
            Fill(Full(), new Color(0, 0, 0, .5f));
            Rect r = new Rect(W / 2 - 220, H / 2 - 160, 440, 330);
            Skin(r, "ui-panel");
            Text(new Rect(r.x + 24, r.y + 14, 300, 40), "일시정지", 24, Paper, TextAnchor.MiddleLeft, 2);
            if(controller.CampaignError!="")Text(new Rect(r.x+24,r.y+62,r.width-48,42), "기록을 저장하지 못했습니다.\n'작전 포기 · 지도로'를 다시 눌러 주세요.", 14, Threat, TextAnchor.MiddleCenter, 1);
            IconButton((int)MobileAction.Sound, layout.Button(MobileAction.Sound), controller.Muted ? "icon-sound-off" : "icon-sound-on");
            Button((int)MobileAction.Resume, layout.Button(MobileAction.Resume), "계속하기", true, Look.Primary, "icon-play", 20);
            Button((int)MobileAction.Restart, layout.Button(MobileAction.Restart), "처음부터 다시", true, Look.Secondary, null, 17);
            Button(600, new Box2(W / 2 - 170, H / 2 + 96, 340, 52), controller.IsCampaign ? "작전 포기 · 지도로" : "실험실 나가기", true, Look.Ghost, "icon-retreat", 16);
        }

        private void LabResult()
        {
            bool won = game.State == RunState.Won;
            Fill(Full(), new Color(0, 0, 0, .5f));
            Rect r = new Rect(W / 2 - 280, H / 2 - 170, 560, 330);
            Skin(r, "ui-panel");
            Text(new Rect(r.x, r.y + 24, r.width, 44), won ? "실험실 방어 성공" : "방어 실패", 30, won ? Amber : Threat, TextAnchor.MiddleCenter, 2);
            Text(new Rect(r.x + 30, r.y + 90, r.width - 60, 80), (won ? "보상 없는 실험실 기록입니다." : game.DefeatReason) + "\n처치 " + game.Kills + " · 조합 " + game.Telemetry.Fusions + "회", 17, Paper, TextAnchor.MiddleCenter, 0);
            Button((int)MobileAction.Restart, new Box2(r.x + 30, r.y + 250, 240, 58), "다시 시작", true, Look.Primary, null, 18);
            Button(604, new Box2(r.x + 290, r.y + 250, 240, 58), "설정으로", true, Look.Secondary, null, 18);
        }

        private void Toast(string text, bool bossWarning = false)
        {
            if (string.IsNullOrEmpty(text)) return;
            Box2 b = layout.NoticeBox(text.Length, game.IsPreparing, activeTowerPanel, bossWarning);
            Rect r = new Rect(b.X, b.Y, b.Width, b.Height);
            Skin(r, "ui-chip");
            Text(r, text, 16, Paper, TextAnchor.MiddleCenter, 1);
        }

        // ---------------------------------------------------------------- kit
        private static Box2 Box(Rect r) { return new Box2(r.x, r.y, r.width, r.height); }
        private void Skin(Rect r, string skin) { items.Add(new Item { Kind = Kind.Skin, Rect = r, Skin = skin, Color = Color.white }); }
        private void Fill(Rect r, Color c) { items.Add(new Item { Kind = Kind.Fill, Rect = r, Color = c }); }
        private void Tex(Rect r, Texture t, Color c) { if (t != null) items.Add(new Item { Kind = Kind.Texture, Rect = r, Texture = t, Color = c }); }
        private void Crop(Rect r, Texture t, Rect uv, Color c) { if (t != null) items.Add(new Item { Kind = Kind.Crop, Rect = r, Texture = t, Uv = uv, Color = c }); }
        private void Icon(Rect r, string name, Color c) { Tex(r, art.Texture(name), c); }
        private void Text(Rect r, string text, int size, Color c, TextAnchor align, int weight, bool shadow = false)
        { items.Add(new Item { Kind = Kind.Text, Rect = r, Text = text, Size = size, Color = c, Align = align, Weight = weight, Shadow = shadow }); }
        private void Bar(Rect r, float f, Color c)
        {
            Skin(r, "ui-bar-back");
            if (f > 0) items.Add(new Item { Kind = Kind.Skin, Rect = new Rect(r.x, r.y, Mathf.Max(r.height, r.width * Mathf.Clamp01(f)), r.height), Skin = "ui-bar", Color = c });
        }
        private void IconButton(int id, Box2 b, string icon)
        {
            Rect r = new Rect(b.X, b.Y, b.Width, b.Height);
            Skin(r, "ui-button");
            float s = Mathf.Min(r.width, r.height) * .62f;
            Icon(new Rect(r.center.x - s / 2, r.center.y - s / 2, s, s), icon, Color.white);
            controller.TouchInput.Regions.Add(new TouchRegion(id, b));
        }
        private void Button(int id, Box2 b, string text, bool enabled, Look look, string icon = null, int size = 18)
        {
            Rect r = new Rect(b.X, b.Y, b.Width, b.Height);
            string skin = !enabled ? "ui-button-disabled" : look == Look.Primary ? "ui-button-primary" : look == Look.Danger ? "ui-button-danger" : look == Look.Ghost ? null : "ui-button";
            if (skin != null) Skin(r, skin);
            Color color = !enabled ? Muted : look == Look.Primary ? Ink : Paper;
            if (!string.IsNullOrEmpty(text)) {
                if (icon != null) {
                    float s = Mathf.Min(r.height * .5f, 30), tw = text.Length * size * .95f + s + 10, x = r.center.x - tw / 2;
                    Icon(new Rect(x, r.center.y - s / 2, s, s), icon, look == Look.Primary ? Ink : Color.white);
                    Text(new Rect(x + s + 10, r.y, tw - s - 10 + 20, r.height), text, size, color, TextAnchor.MiddleLeft, 2);
                } else Text(r, text, size, color, TextAnchor.MiddleCenter, 2);
            }
            if (enabled) controller.TouchInput.Regions.Add(new TouchRegion(id, b));
        }

        // Shared by the IMGUI and canvas back ends so a skin looks the same in both.
        private Texture2D SkinTexture(string name)
        { return name == "ui-bar-back" ? art.Texture("ui-bar") : name == "ui-button-danger" ? art.Texture("ui-button") : art.Texture(name); }
        private static int SkinBorder(string name)
        { return name == "ui-panel" ? 26 : name == "ui-chip" ? 22 : name.StartsWith("ui-bar") ? 7 : 16; }
        private static Color SkinTint(Item item)
        { return item.Skin == "ui-bar-back" ? new Color(.05f, .07f, .1f, .8f) : item.Skin == "ui-button-danger" ? new Color(1, .45f, .5f, 1) : item.Color; }
        private static readonly Color TextShadow = new Color(.04f, .06f, .09f, .85f);

        private GUIStyle SkinStyle(string name)
        {
            GUIStyle style;
            if (skins.TryGetValue(name, out style)) return style;
            int border = SkinBorder(name);
            style = new GUIStyle { border = new RectOffset(border, border, border, border) };
            style.normal.background = SkinTexture(name);
            skins[name] = style;
            return style;
        }

        public void Draw()
        {
            if (game == null || Event.current.type != EventType.Repaint) return;
            if (label == null) label = new GUIStyle(GUI.skin.label) { wordWrap = true, richText = false, padding = new RectOffset(), clipping = TextClipping.Overflow };
            Matrix4x4 matrix = GUI.matrix; Color color = GUI.color;
            GUI.matrix = Matrix4x4.TRS(new Vector3(layout.Origin.X, layout.Origin.Y, 0), Quaternion.identity, Vector3.one * layout.Scale);
            foreach (Item item in items) {
                switch (item.Kind) {
                    case Kind.Fill: GUI.color = item.Color; GUI.DrawTexture(item.Rect, Texture2D.whiteTexture); break;
                    case Kind.Texture: GUI.color = item.Color; GUI.DrawTexture(item.Rect, item.Texture, ScaleMode.ScaleToFit); break;
                    case Kind.Crop: GUI.color = item.Color; GUI.DrawTextureWithTexCoords(item.Rect, item.Texture, item.Uv); break;
                    case Kind.Skin:
                        GUI.color = SkinTint(item); SkinStyle(item.Skin).Draw(item.Rect, false, false, false, false); break;
                    case Kind.Text:
                        label.font = TextFont(item.Weight);
                        label.fontSize = item.Size; label.alignment = item.Align; label.fontStyle = FontStyle.Normal;
                        GUI.color = Color.white;
                        if (item.Shadow) { label.normal.textColor = TextShadow; GUI.Label(new Rect(item.Rect.x + 1.5f, item.Rect.y + 2, item.Rect.width, item.Rect.height), item.Text, label); }
                        label.normal.textColor = item.Color; GUI.Label(item.Rect, item.Text, label); break;
                }
            }
            GUI.matrix = matrix; GUI.color = color;
        }

        private static string Clock(float time) { int n = Mathf.CeilToInt(time); return (n / 60).ToString("0") + ":" + (n % 60).ToString("00"); }
        public static string GateName(int index) { return index == 0 ? "서쪽" : index == 1 ? "북쪽" : index == 2 ? "동쪽" : "남쪽"; }
    }
}
