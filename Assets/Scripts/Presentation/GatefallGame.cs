using System.Collections.Generic;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    // Socket: context panel for one pad. FusionPick: choosing the partner tower on the board.
    public enum GameMenu { None, Pause, Socket, FusionPick }
    [DisallowMultipleComponent]
    public sealed partial class GatefallGame : MonoBehaviour
    {
        public struct FusionOption { public int Recipe; public List<int> Partners; public string Reason; }

        [SerializeField] private GameBalance balance = new GameBalance();
        public DefenseSimulation Simulation { get; private set; }
        public string Notice { get; private set; }
        public float NoticeUntil { get; private set; }
        public bool Muted { get { return sound != null && sound.Muted; } }
        public MobileInput TouchInput { get; private set; }
        public MobileLayout Layout { get { return TouchInput.Layout; } }
        public GameMenu Menu { get; private set; }
        private GameMenu menuBeforePause;
        public int SelectedSocket { get; private set; } = -1;
        public bool SellArmed { get; private set; }
        public bool TowerRecipesExpanded { get; private set; }
        private DefenseSimulation activityGame;
        private readonly Dictionary<int, int> towerShotCounts = new Dictionary<int, int>();
        public int TowerShotCount(int id) { int n; return towerShotCounts.TryGetValue(id, out n) ? n : 0; }
        public int PickRecipe { get; private set; }
        public readonly List<int> PickPartners = new List<int>();
        public Camera GameCamera { get { return gameCamera; } }
        private int commandSequence;
        private string pendingCardSupply;
        private const float GrowthCardInputDelay = .35f;
        private float cardInputUnlockTime;
        public bool CardInterruptsTowerMenu { get { return Menu == GameMenu.Socket || Menu == GameMenu.FusionPick; } }
        public bool CanSelectGrowthCard { get { return Time.unscaledTime >= cardInputUnlockTime; } }
        private float lastUiAction = -1;
        private bool savedRun;
        private Camera gameCamera;
        private GatefallArt art;
        private ArenaView view;
        private PrototypeAudio sound;
        private MobileHud hud;
        private readonly ArenaSelection selection = new ArenaSelection();
        private float visualTime;
        public bool Overview { get; private set; }
        public bool DoubleSpeed { get; private set; }
        private Rect lastSafeArea;
        private int lastWidth, lastHeight;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Input.multiTouchEnabled = true;
            Input.simulateMouseWithTouches = false;
            TouchInput = new MobileInput(new MobileLayout(Screen.width, Screen.height, SafeBox()));
            if (Application.isMobilePlatform)
            {
                Screen.autorotateToPortrait = false;
                Screen.autorotateToPortraitUpsideDown = false;
                Screen.autorotateToLandscapeLeft = true;
                Screen.autorotateToLandscapeRight = true;
                Screen.orientation = ScreenOrientation.AutoRotation;
            }
            GameObject cameraObject = new GameObject("Game camera");
            cameraObject.transform.SetParent(transform, false);
            gameCamera = cameraObject.AddComponent<Camera>();
            gameCamera.orthographic = true;
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = GatefallArt.Hex("5E5C57");
            gameCamera.nearClipPlane = 0.1f; gameCamera.farClipPlane = 30;
            cameraObject.AddComponent<AudioListener>();
            art = new GatefallArt();
            sound = new PrototypeAudio(gameObject);
            hud = new MobileHud(this, art);
            InitCampaign(); NewRun(false);
        }

        private void NewRun(bool start)
        {
            if (view != null) view.Dispose();
            Simulation = new DefenseSimulation(balance, System.Environment.TickCount, activeBattle);
            Simulation.WaveCallsEnabled = activeBattle != null && activeBattle.MobileWaveCalls;
            selection.EnsureCapacity(Simulation.BoardSockets.Length); Overview = Simulation.ExpandedArena; DoubleSpeed = false;
            view = new ArenaView(transform, Simulation, art);
            visualTime = 0; Notice = ""; NoticeUntil = 0; noticedCatalysts = 0; pendingCardSupply = null; lastWaveBriefing = -1;
            cardInputUnlockTime = 0;
            TutorialRun = IsCampaign && ShowLesson;
            TouchInput.Reset();
            Menu = GameMenu.None; menuBeforePause = GameMenu.None; SelectedSocket = -1; SellArmed = false; PickPartners.Clear();
            savedRun = false;
            if (start) StartRun();
            selection.Clear();
            view.Sync(Simulation, 0, 0, selection);
        }

        public void StartRun()
        {
            if (ScreenPage != CampaignScreen.Battle || Menu != GameMenu.None || Simulation.State != RunState.Ready) return;
            if (IsCampaign && ShowLesson) Campaign.MarkTutorial(SelectedStage);
            Simulation.Start();
            if (!IsCampaign || !AnnounceWave(0, false)) ShowNotice("범람 시작 · " + MobileHud.GateName(Simulation.ActiveGate) + "에서 접근합니다", 2.4f);
        }
        public void Restart() { if (IsCampaign) BeginPreparation(); else BeginLab(); }
        public void Pause()
        {
            if (ScreenPage != CampaignScreen.Battle) { ReturnCity(); return; }
            if (Simulation.AwaitingCard) return;
            if (Menu == GameMenu.Pause) {
                Menu = menuBeforePause;
                menuBeforePause = GameMenu.None;
                BattleMenuTime.Close(Simulation, true);
                RestoreTowerMenu();
                TouchInput.SetState(Simulation.State);
            } else OpenMenu(GameMenu.Pause);
        }
        public void OpenMenu(GameMenu menu)
        {
            if (ScreenPage != CampaignScreen.Battle || (Simulation.State != RunState.Running && Simulation.State != RunState.Paused && !Simulation.IsPreparing) || Simulation.AwaitingCard) return;
            if (menu == GameMenu.Pause) {
                if (Menu == GameMenu.Pause) return;
                menuBeforePause = Menu;
                SellArmed = false;
            } else TowerRecipesExpanded = false;
            BattleMenuTime.Open(Simulation, menu == GameMenu.Pause);
            Menu = menu; TouchInput.Reset(); TouchInput.SetState(Simulation.State);
        }
        public void CloseMenu()
        {
            bool wasPauseScreen = Menu == GameMenu.Pause;
            Menu = GameMenu.None; menuBeforePause = GameMenu.None; SellArmed = false; TowerRecipesExpanded = false; PickPartners.Clear(); TouchInput.Reset();
            BattleMenuTime.Close(Simulation, wasPauseScreen);
            TouchInput.SetState(Simulation.State);
        }
        private string CommandId() { return "ui-" + (++commandSequence); }
        public void ToggleSound() { sound.Muted = !sound.Muted; if (Campaign != null) Campaign.SetMuted(sound.Muted); }
        public string SkillName { get { return Simulation.Rules != null && Simulation.Rules.Hunter == 2 ? "방호" : Simulation.Rules != null && Simulation.Rules.Hunter == 1 ? "질주" : "회피"; } }

        // Keep catalog order so a change in materials or credits only updates the card's state.
        public List<FusionOption> FusionOptions(Tower tower)
        {
            List<FusionOption> list = new List<FusionOption>();
            if (tower == null) return list;
            int reachable = Simulation.ReachableTowerFamilies();
            foreach (FusionRecipe r in TowerCatalog.Recipes) {
                if (r.First != tower.Kind && r.Second != tower.Kind) continue;
                if (!Simulation.Allows(r.Result)) continue;
                TowerKind other = r.First == tower.Kind ? r.Second : r.First;
                if (!Simulation.Allows(other)) continue;
                if ((reachable & (1 << (int)other)) == 0) continue;
                List<int> partners = new List<int>(); bool lowPartner = false;
                foreach (Tower t in Simulation.Towers) {
                    if (t.Id == tower.Id || t.Kind != other) continue;
                    if (t.Level >= 2) partners.Add(t.Socket); else lowPartner = true;
                }
                string otherName = TowerCatalog.Get(other).Name.Replace(" 결계주", "");
                string reason;
                if (tower.Level < 2) reason = "이 결계주를\nLv2로 강화";
                else if (partners.Count == 0) reason = otherName + (lowPartner ? " Lv2로\n강화 필요" : " Lv2\n필요");
                else {
                    Tower partner = Simulation.TowerAt(partners[0]);
                    reason = Simulation.CanFuse(r.Id, tower.Id, partner.Id, tower.Socket);
                    if (reason.StartsWith("공명 촉매")) reason = Simulation.WaveCallsEnabled ? "촉매 필요\n(3·5웨이브)" : "촉매 필요\n(60·120초)";
                    else if (reason.StartsWith("마석")) reason = "마석 부족";
                }
                list.Add(new FusionOption { Recipe = r.Id, Partners = partners, Reason = reason });
            }
            return list;
        }

        private void Update()
        {
            FitCamera();
            TouchInput.SetState(Simulation.State);
            // A wave call can open a card during input, before the simulation tick.
            bool hadCard = Simulation.AwaitingCard;
            int touchCount = Input.touchCount;
            // The pre-input snapshot is only needed when a pointer may use its regions.
            if (touchCount > 0 || !Application.isMobilePlatform) hud.Refresh();
            PollPointers(touchCount);
            // Copy before handling: pause/restart deliberately clear captured fingers.
            MobileAction[] actions = TouchInput.Actions.ToArray();
            TouchInput.Actions.Clear();
            foreach (MobileAction action in actions) {
                if (Time.unscaledTime - lastUiAction < .12f) break;
                lastUiAction = Time.unscaledTime; HandleAction(action); break;
            }
            DevelopmentKeys();
            V2 movement = new V2();
            if (!Application.isMobilePlatform) movement = new V2((Held(KeyCode.D, KeyCode.RightArrow) ? 1 : 0) - (Held(KeyCode.A, KeyCode.LeftArrow) ? 1 : 0),
                (Held(KeyCode.W, KeyCode.UpArrow) ? 1 : 0) - (Held(KeyCode.S, KeyCode.DownArrow) ? 1 : 0));
            if (TouchInput.JoystickFinger != int.MinValue) movement = TouchInput.Movement;
            float dt = Mathf.Min(Time.deltaTime, 0.1f);
            TouchInput.AllowPreparation = Simulation.IsPreparing && ScreenPage == CampaignScreen.Battle && Menu == GameMenu.None;
            if (Menu != GameMenu.None) movement = new V2();
            float combatDt = Simulation.State == RunState.Running ? dt * (DoubleSpeed ? 2f : 1.25f) : dt;
            if (ScreenPage == CampaignScreen.Battle) Simulation.Tick(combatDt, movement);
            if (!hadCard && Simulation.AwaitingCard)
                cardInputUnlockTime = Time.unscaledTime + GrowthCardInputDelay;
            if (activityGame != Simulation) { activityGame = Simulation; towerShotCounts.Clear(); }
            foreach (CombatEvent effect in Simulation.Events) {
                if (effect.Kind == EventKind.TowerShot) {
                    foreach (Tower fired in Simulation.Towers) if (V2.DistanceSquared(fired.Position, effect.Position) < .001f) {
                        towerShotCounts[fired.Id] = TowerShotCount(fired.Id) + 1; break;
                    }
                }
                view.Effect(effect); sound.Play(effect);
                if (effect.Kind == EventKind.Supply) {
                    bool catalyst = Simulation.Telemetry.CoresGranted > noticedCatalysts;
                    noticedCatalysts = Simulation.Telemetry.CoresGranted;
                    string prefix = effect.Wave >= 0 ? "호출 보급 선지급" : "보급";
                    string supplyNotice = catalyst ? (TutorialRun && Simulation.Telemetry.Fusions == 0 && effect.Wave < 0 ? "공명 촉매 +1 · Lv2 결계주를 탭해 조합해 보세요" : prefix + " · 마석 +" + (int)effect.Value + " · 촉매 +1") : prefix + " · 마석 +" + (int)effect.Value;
                    Debug.Log("GATEFALL SUPPLY index=" + Simulation.SuppliesGranted + " elapsed=" + Simulation.Elapsed.ToString("F2") + " prepaid=" + (effect.Wave >= 0) + " credits=" + Simulation.Credits + " catalystsGranted=" + Simulation.Telemetry.CoresGranted + " cardOffers=" + Simulation.Telemetry.CardOffers);
                    if (Simulation.AwaitingCard) pendingCardSupply = supplyNotice;
                    else ShowNotice(supplyNotice, catalyst ? 3.4f : 2.2f);
                }
                else if (effect.Kind == EventKind.Wave && !IsCampaign) ShowNotice("범람 " + (Simulation.Wave + 1) + "단계 · " + MobileHud.GateName(Simulation.ActiveGate), 2.2f);
                else if (effect.IsEntranceWarning && IsCampaign && effect.Value < Simulation.GatePositions.Length && effect.Wave > lastWaveBriefing)
                    AnnounceWave(effect.Wave, true);
                else if (effect.Kind == EventKind.Shield) ShowNotice("응급 방호 자동 발동 · 결계 피해 감소", 2.4f);
                else if (effect.Kind == EventKind.HunterDown) ShowNotice("헌터가 쓰러졌습니다 · 10초 뒤 부활", 2.5f);
                else if (effect.Kind == EventKind.HunterRevived) ShowNotice("헌터 부활 · 잠시 무적", 2.5f);
                else if (effect.Kind == EventKind.CoreHurt && IsCampaign && Simulation.State == RunState.Running &&
                    effect.Source >= 0 && effect.Source < Simulation.GatePositions.Length &&
                    !Simulation.AwaitingCard && Time.unscaledTime >= NoticeUntil) {
                    ShowNotice(MobileHud.GateName(effect.Source) + "에서 결계 피해 · 해당 길을 보강하세요", 2.4f);
                    Debug.Log("GATEFALL CORE BREACH lane=" + effect.Source + " elapsed=" + Simulation.Elapsed.ToString("F2") + " core=" + Simulation.CoreHealth.ToString("F1"));
                }
            }
            Simulation.Events.Clear();
            bool animate = Simulation.State == RunState.Running || Simulation.State == RunState.Ready;
            if (animate) visualTime += combatDt; else visualTime += Time.unscaledDeltaTime * .25f;
            BuildSelection();
            view.SetVisible(ScreenPage == CampaignScreen.Battle);
            view.Sync(Simulation, visualTime, animate ? combatDt : 0, selection);
            TouchInput.SetState(Simulation.State);
            hud.Refresh();
            if (!savedRun && (Simulation.State == RunState.Won || Simulation.State == RunState.Lost)) SaveRun();
        }
        private int noticedCatalysts;
        private int lastWaveBriefing = -1;
        private bool AnnounceWave(int wave, bool preview, bool called=false)
        {
            WaveBriefing briefing = Simulation.GetWaveBriefing(wave);
            if (briefing == null) return false;
            lastWaveBriefing = wave;
            if(!called)ShowNotice((preview ? "예고 · " : "") + briefing.Summary, 2.4f);
            Debug.Log("GATEFALL WAVE BRIEFING index=" + wave + " total=" + briefing.Total + " source=" + (called ? "call" : "schedule") + " " + briefing.Summary);
            return true;
        }
        // Captured when the operation starts so the lesson survives MarkTutorial at 범람 시작.
        public bool TutorialRun { get; private set; }

        private void BuildSelection()
        {
            selection.Clear();
            if (ScreenPage != CampaignScreen.Battle) return;
            if (Menu == GameMenu.Socket || Menu == GameMenu.FusionPick) { selection.Selected = SelectedSocket; selection.ShowRange = true; }
            if (Menu == GameMenu.FusionPick) {
                selection.Partners.AddRange(PickPartners);
                selection.PreviewKind = (int)TowerCatalog.Recipes[PickRecipe].Result;
            }
            bool live = Simulation.State == RunState.Running || Simulation.IsPreparing;
            int readySocket = -1;
            if (live) foreach (Tower t in Simulation.Towers) {
                if (t.Level < 2 || t.Socket >= selection.FusionReady.Length) continue;
                // Every frame for every Lv2+ tower: same answer as an empty FusionOptions reason, without its lists and strings.
                if (Simulation.HasReadyFusion(t)) { selection.FusionReady[t.Socket] = true; readySocket = t.Socket; }
            }
            // Tutorial pointers: only on first attempts, only while nothing is open.
            if (!IsCampaign || !TutorialRun || Menu != GameMenu.None || !live) return;
            if (SelectedStage == 0) {
                // Follow the authored street instead of obsolete socket numbers.
                int firstSite = TutorialSocket(.25f, -1), secondSite = TutorialSocket(.70f, firstSite);
                Tower first = Simulation.TowerAt(firstSite), second = Simulation.TowerAt(secondSite);
                if (first == null) Point(Simulation.BoardSockets[firstSite]);
                else if (second == null && Simulation.IsPreparing && Simulation.Credits >= 40) Point(Simulation.BoardSockets[secondSite]);
                else {
                    Tower low = first.Level == 1 ? first : Simulation.Towers.Find(t => t.Level == 1);
                    if (low != null && Simulation.Credits >= TowerCatalog.Get(low.Kind).UpgradeCost) Point(low.Position);
                }
            } else if (readySocket >= 0 && Simulation.Telemetry.Fusions == 0) Point(Simulation.BoardSockets[readySocket]);
        }
        private int TutorialSocket(float portion, int exclude)
        {
            V2[] road = Simulation.Roads[0]; int node;
            V2 target = BattlefieldLayouts.Along(road, BattlefieldLayouts.RoadLength(road) * portion, out node);
            int best = -1; float distance = float.MaxValue;
            for (int i = 0; i < Simulation.BoardSockets.Length; i++) {
                float d = V2.Distance(Simulation.BoardSockets[i], target);
                if (i != exclude && d < distance) { best = i; distance = d; }
            }
            return best;
        }
        private void Point(V2 p) { selection.HasPointer = true; selection.Pointer = p; }
        public string LessonText
        {
            get
            {
                if (!IsCampaign || !TutorialRun || !Simulation.IsPreparing) return "";
                CampaignStage stage = Simulation.Rules.Stage;
                return stage.Index < 3 ? stage.Lesson : "";
            }
        }

        private void HandleAction(MobileAction action)
        {
            int id=(int)action;
            if(id>=MobileLayout.WaveCallActionBase && id<MobileLayout.WaveCallActionBase+6) { CallWave(id-MobileLayout.WaveCallActionBase);return; }
            switch (action) {
                case MobileAction.Start: StartRun(); break;
                case MobileAction.Pause:
                case MobileAction.Resume: Pause(); break;
                case MobileAction.Restart: Restart(); break;
                case MobileAction.Sound: ToggleSound(); break;
                case MobileAction.Skill: Simulation.UseSkill(); break;
                case MobileAction.Speed:
                    if (ScreenPage == CampaignScreen.Battle && Menu == GameMenu.None && Simulation.State == RunState.Running && !Simulation.AwaitingCard)
                        DoubleSpeed = !DoubleSpeed;
                    break;
                case MobileAction.Map:
                    if (Simulation.ExpandedArena && Menu == GameMenu.None) Overview = !Overview;
                    break;
                default: MenuAction((int)action); break;
            }
        }
        private void CallWave(int expectedWave)
        {
            if(ScreenPage!=CampaignScreen.Battle || Menu!=GameMenu.None || !IsCampaign || !Simulation.Rules.MobileWaveCalls)return;
            string reason;
            if(!Simulation.TryCallNextWave(expectedWave,CommandId(),out reason)) {ShowNotice(reason,2);return;}
            // The call button already showed this briefing. Keep its prepaid supply notice readable.
            AnnounceWave(expectedWave,false,true);
            TouchInput.Reset();TouchInput.SetState(Simulation.State);
            Debug.Log("GATEFALL WAVE CALL index=" + expectedWave + " elapsed=" + Simulation.Elapsed.ToString("F2") + " totalCalls=" + Simulation.Telemetry.WaveCalls + " advanced=" + Simulation.Telemetry.WaveAdvanceSeconds.ToString("F2") + " remaining=" + Simulation.Enemies.Count + " supplies=" + Simulation.SuppliesGranted);
        }
        private void MenuAction(int action)
        {
            string reason;
            if (CampaignAction(action)) return;
            if (action == 100) { CloseMenu(); return; }
            bool live = ScreenPage == CampaignScreen.Battle && (Simulation.State == RunState.Running || Simulation.State == RunState.Paused || Simulation.IsPreparing) && !Simulation.AwaitingCard;
            if (action >= 400 && action < 400 + Simulation.BoardSockets.Length && live && (Menu == GameMenu.None || Menu == GameMenu.Socket)) {
                int socket = action - 400;
                if (Menu == GameMenu.Socket && socket == SelectedSocket) { CloseMenu(); return; }
                SelectedSocket = socket; SellArmed = false;
                TowerRecipesExpanded = false;
                if (Menu == GameMenu.None) OpenMenu(GameMenu.Socket);
                return;
            }
            Tower tower = Simulation.TowerAt(SelectedSocket);
            if ((action == 212 || action == 213) && Menu == GameMenu.Socket && tower != null) {
                TowerRecipesExpanded = action == 212; SellArmed = false; TouchInput.Reset(); return;
            }
            if (action >= 200 && action < 210 && Menu == GameMenu.Socket) {
                TowerKind kind = (TowerKind)(action - 200);
                if (Simulation.TryBuild(SelectedSocket, kind, CommandId(), out reason)) { CloseMenu(); ShowNotice(TowerCatalog.Get(kind).Name + " 건설", 1.5f); }
                else ShowNotice(reason, 2.4f);
                return;
            }
            if (action == 210 && Menu == GameMenu.Socket && tower != null) {
                if (Simulation.TryUpgrade(tower.Id, CommandId(), out reason)) { CloseMenu(); ShowNotice(TowerCatalog.Get(tower.Kind).Name + " Lv" + tower.Level + " 강화", 1.5f); }
                else ShowNotice(reason, 2.4f);
                return;
            }
            if (action == 211 && Menu == GameMenu.Socket && tower != null) {
                if (!SellArmed) { SellArmed = true; return; }
                if (Simulation.TrySell(tower.Id, CommandId(), out reason)) CloseMenu(); else ShowNotice(reason, 2.4f);
                return;
            }
            if (action >= 220 && action < 220 + TowerCatalog.Recipes.Length && Menu == GameMenu.Socket && tower != null) {
                int recipe = action - 220;
                FusionOption option = FusionOptions(tower).Find(o => o.Recipe == recipe);
                if (option.Partners == null || option.Reason != "") { if (option.Reason != null) ShowNotice(option.Reason.Replace("\n", " "), 2.4f); return; }
                // Even one partner uses the existing picker so the result's position/range can be checked before spending.
                PickRecipe = recipe; PickPartners.Clear(); PickPartners.AddRange(option.Partners); SellArmed = false;
                Menu = GameMenu.FusionPick; TouchInput.Reset();
                return;
            }
            if (action >= 450 && action < 450 + Simulation.BoardSockets.Length && Menu == GameMenu.FusionPick && tower != null && PickPartners.Contains(action - 450)) {
                Fuse(PickRecipe, tower, action - 450);
                return;
            }
            if (action >= 500 && action < 506 && Simulation.AwaitingCard && CanSelectGrowthCard) {
                if (Simulation.ChooseCard((CardKind)(action - 500))) {
                    RestoreTowerMenu();
                    if (pendingCardSupply != null) { ShowNotice(pendingCardSupply + " · 성장 적용", 3.2f); pendingCardSupply = null; }
                    else ShowNotice("성장 적용 · " + TowerCatalog.CardName((CardKind)(action - 500)), 2f);
                }
            }
        }
        private void RestoreTowerMenu()
        {
            TouchInput.Reset();
            SellArmed = false;
            if (Menu != GameMenu.FusionPick) return;
            Tower anchor = Simulation.TowerAt(SelectedSocket);
            FusionOption option = FusionOptions(anchor).Find(o => o.Recipe == PickRecipe);
            PickPartners.Clear();
            if (option.Reason == "") PickPartners.AddRange(option.Partners);
            else if (anchor == null) CloseMenu();
            else { Menu = GameMenu.Socket; TowerRecipesExpanded = true; }
        }
        private void Fuse(int recipe, Tower anchor, int partnerSocket)
        {
            string reason; Tower partner = Simulation.TowerAt(partnerSocket);
            if (partner == null) return;
            if (Simulation.TryFuse(recipe, anchor.Id, partner.Id, anchor.Socket, CommandId(), out reason)) {
                ShowNotice(TowerCatalog.Get(TowerCatalog.Recipes[recipe].Result).Name + " 완성 · 빈 자리에 다시 건설 가능", 3.5f);
                CloseMenu();
            } else ShowNotice(reason, 2.4f);
        }

        // Reference-space (HUD) position of a world point, lifted by `lift` world units.
        public Vector2 WorldToReference(V2 world, float lift)
        {
            Vector3 s = gameCamera.WorldToScreenPoint(new Vector3(world.X, world.Y + lift, 0));
            V2 r = Layout.FromScreen(new V2(s.x, s.y));
            return new Vector2(r.X, r.Y);
        }
        public float WorldToReferenceScale(float units) { return units * Screen.height / (2 * gameCamera.orthographicSize) / Layout.Scale; }
        public Vector2 EnemyHealthReference(Enemy enemy) { return WorldToReference(view.EnemyHealthAnchor(enemy), 0); }

        private void PollPointers(int touchCount)
        {
            bool foundJoystick = false;
            if (touchCount > 0) {
                for (int i = 0; i < touchCount; i++) {
                    Touch touch = Input.GetTouch(i);
                    PointerPhase phase = touch.phase == TouchPhase.Began ? PointerPhase.Begin :
                        touch.phase == TouchPhase.Ended ? PointerPhase.End : touch.phase == TouchPhase.Canceled ? PointerPhase.Cancel : PointerPhase.Move;
                    TouchInput.Pointer(touch.fingerId, phase, Layout.FromScreen(new V2(touch.position.x, touch.position.y)));
                    if (TouchInput.JoystickFinger == touch.fingerId) foundJoystick = true;
                }
            } else if (!Application.isMobilePlatform) {
                V2 position = Layout.FromScreen(new V2(Input.mousePosition.x, Input.mousePosition.y));
                if (Input.GetMouseButtonDown(0)) TouchInput.Pointer(-1, PointerPhase.Begin, position);
                else if (Input.GetMouseButton(0)) TouchInput.Pointer(-1, PointerPhase.Move, position);
                if (Input.GetMouseButtonUp(0)) TouchInput.Pointer(-1, PointerPhase.End, position);
                foundJoystick = Input.GetMouseButton(0) && TouchInput.JoystickFinger == -1;
            }
            TouchInput.ReleaseMissingJoystick(foundJoystick);
        }

        // Editor-only conveniences for development; the product has no keyboard UI or hints.
        private void DevelopmentKeys()
        {
            if (Application.isMobilePlatform) return;
            if (Input.GetKeyDown(KeyCode.Escape)) Pause();
            if (ScreenPage == CampaignScreen.Battle && Input.GetKeyDown(KeyCode.Return) && Simulation.State == RunState.Ready) StartRun();
            if (ScreenPage == CampaignScreen.Battle && Input.GetKeyDown(KeyCode.Space)) Simulation.UseSkill();
        }

        // One fixed camera frames the whole 36 x 20 board with a strip on top for the HUD.
        private void FitCamera()
        {
            if (Screen.width != lastWidth || Screen.height != lastHeight || Screen.safeArea != lastSafeArea) {
                lastWidth = Screen.width; lastHeight = Screen.height; lastSafeArea = Screen.safeArea;
                TouchInput.SetLayout(new MobileLayout(Screen.width, Screen.height, SafeBox()));
                Debug.Log("GATEFALL LAYOUT screen=" + Screen.width + "x" + Screen.height + " safe=" + Screen.safeArea + " scale=" + Layout.Scale.ToString("F3") +
                    " reference=" + Layout.Width.ToString("F0") + "x" + Layout.Height.ToString("F0") + " origin=" + Layout.Origin.X.ToString("F0") + "," + Layout.Origin.Y.ToString("F0"));
            }
            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);
            gameCamera.rect = new Rect(0, 0, 1, 1);
            bool expanded = Simulation != null && Simulation.ExpandedArena && ScreenPage == CampaignScreen.Battle;
            bool full = expanded && (Overview || Menu == GameMenu.FusionPick);
            gameCamera.orthographicSize = full ? Mathf.Max(12.6f, 24.8f / aspect) : expanded ? 11f : Simulation != null && Simulation.Rules != null && Simulation.Rules.Stage != null ? Mathf.Max(12.6f, 24.8f / aspect) : Mathf.Max(11f, 18.9f / aspect);
            gameCamera.backgroundColor = expanded && full ? GatefallArt.Hex("1A2225") : GatefallArt.Hex("5E5C57");
            if (!expanded) { gameCamera.transform.position = new Vector3(0, .35f, -10); return; }
            float horizontal = gameCamera.orthographicSize * aspect;
            float maxX = Mathf.Max(0, Simulation.HalfWidth - horizontal);
            float maxY = Mathf.Max(0, Simulation.HalfHeight - gameCamera.orthographicSize);
            Vector3 target = full ? Vector3.zero : new Vector3(Mathf.Clamp(Simulation.PlayerPosition.X, -maxX, maxX), Mathf.Clamp(Simulation.PlayerPosition.Y, -maxY, maxY), 0);
            if (Menu == GameMenu.None || full) gameCamera.transform.position = new Vector3(target.x, target.y, -10);
        }
        private static Box2 SafeBox() { Rect safe = Screen.safeArea; return new Box2(safe.x, safe.y, safe.width, safe.height); }
        private static bool Held(KeyCode first, KeyCode second) { return Input.GetKey(first) || Input.GetKey(second); }
        private void ShowNotice(string message, float seconds = 3f) { Notice = message; NoticeUntil = Time.unscaledTime + seconds; }
        private void SaveRun()
        {
            savedRun = true; SaveCampaignOutcome();
            try {
                string report = "Gatefall " + Application.version + "\n" + Simulation.State + " elapsed=" + Simulation.Elapsed.ToString("F2") +
                    " core=" + Simulation.CoreHealth + " hunter=" + Simulation.PlayerHealth + " kills=" + Simulation.Kills +
                    " firstFusion=" + Simulation.Telemetry.FirstFusion + " fusions=" + Simulation.Telemetry.Fusions +
                    "\nWave clear=" + Simulation.WaveClearVictory + " allSpawned=" + Simulation.AllWavesSpawned + " spawned=" + Simulation.Telemetry.Spawned + " remainingEnemies=" + Simulation.Enemies.Count + " bossDefeated=" + Simulation.BossDefeated +
                    "\nSupplies=" + Simulation.SuppliesGranted + " supplyCredits=" + (Simulation.SuppliesGranted*Simulation.WaveSupplyCredits) + " cardOffers=" + Simulation.Telemetry.CardOffers + " catalystsGranted=" + Simulation.Telemetry.CoresGranted + " waveCalls=" + Simulation.Telemetry.WaveCalls + " advancedSeconds=" + Simulation.Telemetry.WaveAdvanceSeconds.ToString("F2") +
                    "\n" + string.Join("\n",Simulation.Telemetry.WaveCallLog) +
                    "\nTower damage: " + string.Join(",", Simulation.Telemetry.TowerDamage) +
                    "\nCore damage by lane W/N/E/S: " + string.Join(",", Simulation.Telemetry.CoreDamageByLane) +
                    "\nMark hits=" + Simulation.Telemetry.MarkedHits + " effective bonus damage=" + Simulation.Telemetry.MarkBonusDamage.ToString("F1") +
                    "\n" + string.Join("\n", Simulation.Telemetry.Purchases);
                System.IO.File.WriteAllText(System.IO.Path.Combine(Application.persistentDataPath, "last-run.txt"), report);
            } catch (System.Exception e) { Debug.LogWarning("Could not save optional run telemetry: " + e.Message); }
        }
        private void OnGUI() { if (hud != null && Simulation != null) hud.Draw(); }

        // Keep diagnostic cadence, but sample only consecutive focused combat frames.
        private readonly ActiveCombatTiming frameTiming = new ActiveCombatTiming();
        private float perfClock;
        private void LateUpdate()
        {
            if (!Debug.isDebugBuild) return;
            bool active = Application.isFocused && ScreenPage == CampaignScreen.Battle && Simulation != null &&
                Simulation.State == RunState.Running && !Simulation.AwaitingCard;
            frameTiming.Observe(Time.unscaledDeltaTime, active);
            perfClock += Time.unscaledDeltaTime;
            if (perfClock < 10) return;
            perfClock = 0;
            ActiveCombatTiming.Snapshot timing = frameTiming.Take();
            Debug.Log("GATEFALL PERF page=" + ScreenPage + " state=" + (Simulation != null ? Simulation.State.ToString() : "-") + " enemies=" + (Simulation != null ? Simulation.Enemies.Count : 0) +
                " avgMs=" + (timing.Frames > 0 ? timing.AverageMilliseconds.ToString("F1") : "na") +
                " worstMs=" + (timing.Frames > 0 ? (timing.WorstSeconds * 1000).ToString("F1") : "na") +
                " over33ms=" + timing.Over33Milliseconds + "/" + timing.Frames +
                " monoMB=" + (UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / 1048576f).ToString("F1") +
                " fx=" + (view != null ? view.ActiveEffectCount : 0) + " fxPeak=" + (view != null ? view.PeakEffectCount : 0) +
                " swarm=" + (view != null ? view.SwarmRenderedCount : 0) + " swarmBatches=" + (view != null ? view.SwarmBatchCount : 0) +
                " detailed=" + (view != null ? view.DetailedEnemyCount : 0) + " mobTex=" + (view != null ? view.SwarmTextureSize : "-") +
                " " + (view != null ? view.CombatEffectCensus : "") +
                " timingScope=ActiveCombat activeSeconds=" + timing.TotalSeconds.ToString("F3") + " focused=" + Application.isFocused);
        }
        private void Suspend()
        {
            if (TouchInput != null) TouchInput.Reset();
            if (Simulation != null && Simulation.State == RunState.Running && ScreenPage == CampaignScreen.Battle) OpenMenu(GameMenu.Pause);
        }
        private void OnApplicationFocus(bool focus) { if (!focus) Suspend(); }
        private void OnApplicationPause(bool paused) { if (paused) Suspend(); }
        private void OnDestroy()
        {
            if (hud != null) hud.Dispose();
            if (view != null) view.Dispose();
            if (sound != null) sound.Dispose();
            if (art != null) art.Dispose();
        }
    }
}
