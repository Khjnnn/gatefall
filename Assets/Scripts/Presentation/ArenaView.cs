using System.Collections.Generic;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    // What the controller wants highlighted on the board this frame.
    public sealed class ArenaSelection
    {
        public int Selected = -1;
        public bool ShowRange;
        public int PreviewKind = -1;
        public readonly List<int> Partners = new List<int>();
        public bool[] FusionReady = new bool[0];
        public void EnsureCapacity(int count) { if (FusionReady.Length != count) FusionReady = new bool[count]; }
        public bool HasPointer;
        public V2 Pointer;
        public void Clear() { Selected = -1; ShowRange = false; PreviewKind = -1; Partners.Clear(); System.Array.Clear(FusionReady, 0, FusionReady.Length); HasPointer = false; }
    }

    // Battlefield presentation reads shared route data without changing simulation state.
    // V12 terrain and roads surround the smaller V6 combat sprites.
    public sealed partial class ArenaView
    {
        // Kept for older callers; new code uses GatefallArt colours.
        public static readonly Color Cyan = GatefallArt.Ally, Gold = GatefallArt.Amber, Pink = GatefallArt.Threat;

        private sealed class EnemyVisual
        {
            public Transform Root; public SpriteRenderer Body, HpBack, HpLag, HpFill, Slow, Shield, SiegeMark, TargetMark;
            public float LastX, LastHealth, TrailHealth; public bool Left, WasHit;
            public readonly MaterialPropertyBlock Flash = new MaterialPropertyBlock();
        }
        private sealed class TowerVisual { public Transform Root; public SpriteRenderer Body; public Vector2 Kick; public int Version, VolleyFrame = -1; public float Pop, Recoil; }
        private sealed class Fx
        {
            public SpriteRenderer Sprite; public Vector3 Velocity; public float Life, Total, Scale0, Scale1, Spin;
            public Vector2 Stretch; public Color Color; public bool Gravity; public float FadePower; public int SpawnFrame;
        }

        private readonly Transform root;
        private readonly GatefallArt art;
        private readonly DefenseSimulation board;
        private readonly List<Object> owned = new List<Object>();
        private readonly Sprite square;
        private readonly SpriteRenderer[] pads, padMarks, padPlus, rifts;
        private readonly float[] riftAlarm;
        private readonly TowerVisual[] towers;
        private readonly SpriteRenderer[] glyphs;
        private readonly Dictionary<int, EnemyVisual> enemies = new Dictionary<int, EnemyVisual>();
        private readonly Stack<EnemyVisual> enemyPool = new Stack<EnemyVisual>();
        private readonly Dictionary<int, SpriteRenderer> shots = new Dictionary<int, SpriteRenderer>();
        private readonly Dictionary<int, Vector3> shotStart = new Dictionary<int, Vector3>();
        private readonly Stack<SpriteRenderer> shotPool = new Stack<SpriteRenderer>();
        private const int MaxActiveEffects = 220;
        public int ActiveEffectCount { get { return effects.Count; } }
        public int PeakEffectCount { get; private set; }
        private readonly List<Fx> effects = new List<Fx>(MaxActiveEffects);
        private readonly Stack<Fx> effectRecords = new Stack<Fx>();
        private readonly Stack<SpriteRenderer> fxPool = new Stack<SpriteRenderer>();
        private readonly List<SpriteRenderer> links = new List<SpriteRenderer>();
        private readonly List<int> stale = new List<int>();
        private readonly HashSet<int> seen = new HashSet<int>();
        private readonly Transform hunter;
        private readonly SpriteRenderer hunterBody, hunterRing, hunterShield, hunterHpBack, hunterHpFill;
        private readonly SpriteRenderer core, coreDome, coreShield, rangeRing, rangeFill, bossDanger, bossCountdown, pointer;
        private float coreFlash, hunterFlash, time;
        private float hunterStride, hunterMove, hunterRecoil, nextFootstep;
        private int hunterShotFrame = -1, hunterHurtFrame = -1, coreHurtFrame = -1;
        private Vector2 hunterDirection, hunterKick;
        private V2 lastPlayer;

        public ArenaView(Transform parent, DefenseSimulation game, GatefallArt art)
        {
            board = game; this.art = art;
            towers = new TowerVisual[game.BoardSockets.Length]; glyphs = new SpriteRenderer[game.BoardSockets.Length];
            root = new GameObject("Arena v0.15").transform;
            root.SetParent(parent, false);
            Texture2D white = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            Color[] fill = new Color[16]; for (int i = 0; i < 16; i++) fill[i] = Color.white; white.SetPixels(fill); white.Apply();
            square = Sprite.Create(white, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f), 4); owned.Add(white); owned.Add(square);
            InitializeCombatEffects();

            BuildGround(game);

            rifts = new SpriteRenderer[game.GatePositions.Length]; riftAlarm = new float[rifts.Length];
            for (int i = 0; i < rifts.Length; i++) {
                if (System.Array.IndexOf(game.OpenLanes, i) < 0) continue;
                rifts[i] = Make("Rift " + i, art.Sprite("fx-rift"), Point(game.GatePositions[i]), Vector3.one, Color.white, 150);
            }
            pads = new SpriteRenderer[game.BoardSockets.Length]; padMarks = new SpriteRenderer[pads.Length]; padPlus = new SpriteRenderer[pads.Length * 2];
            for (int i = 0; i < pads.Length; i++) {
                pads[i] = Make("Pad " + (i + 1), art.Sprite("pad"), Point(game.BoardSockets[i]), Vector3.one * .78f, Color.white, 120);
                padMarks[i] = Make("Pad mark", art.Sprite("fx-select"), Point(game.BoardSockets[i]), Vector3.one, GatefallArt.Amber, 130);
                padMarks[i].enabled = false;
                // Empty-pad marker: a plain amber plus, hidden once something is built.
                padPlus[i * 2] = Make("Pad plus", square, Point(game.BoardSockets[i]) + new Vector3(0, .12f, 0), new Vector3(.50f, .12f, 1), GatefallArt.Alpha(GatefallArt.Paper, .98f), 125);
                padPlus[i * 2 + 1] = Make("Pad plus", square, Point(game.BoardSockets[i]) + new Vector3(0, .12f, 0), new Vector3(.12f, .34f, 1), GatefallArt.Alpha(GatefallArt.Paper, .98f), 125);
            }
            rangeFill = Make("Range fill", art.Sprite("fx-disc"), Vector3.zero, Vector3.one, new Color(1, 1, 1, .07f), 140);
            rangeRing = Make("Range ring", art.Sprite("fx-range"), Vector3.zero, Vector3.one, new Color(1, 1, 1, .5f), 141);

            coreDome = Make("Barrier dome", art.Sprite("fx-ring-soft"), new Vector3(0, .1f, 0), new Vector3(3.8f, 3.3f, 1), GatefallArt.Alpha(GatefallArt.Ally, .55f), 160);
            coreShield = Make("Emergency shield", art.Sprite("fx-disc"), new Vector3(0, .4f, 0), new Vector3(10.4f, 8.8f, 1), GatefallArt.Alpha(GatefallArt.Ally, .0f), 3200);
            core = Make("City barrier", art.Sprite("core"), Vector3.zero, Vector3.one * .76f, Color.white, Order(0));

            hunter = new GameObject("Hunter").transform; hunter.SetParent(root, false);
            int hunterIndex = game.Rules != null ? Mathf.Clamp(game.Rules.Hunter, 0, 2) : 0;
            hunterRing = Child(hunter, "Ally ring", art.Sprite("fx-ring"), Vector3.zero, new Vector3(1.4f, .68f, 1), GatefallArt.Alpha(GatefallArt.Ally, .85f), 0);
            hunterBody = Child(hunter, "Body", art.Sprite("hunter-" + hunterIndex), Vector3.zero, Vector3.one * .8f, Color.white, 0);
            hunterShield = Child(hunter, "Guard", art.Sprite("fx-ring"), new Vector3(0, .7f, 0), Vector3.one * 1.6f, GatefallArt.Alpha(GatefallArt.Hex("6E9CE0"), .0f), 0);
            hunterHpBack = Child(hunter, "Hp back", square, new Vector3(0, 2.2f, 0), new Vector3(1.1f, .16f, 1), GatefallArt.Alpha(GatefallArt.Ink, .85f), 0);
            hunterHpFill = Child(hunter, "Hp fill", square, new Vector3(0, 2.2f, 0), new Vector3(1.0f, .09f, 1), GatefallArt.Ally, 0);

            bossDanger = Make("Boss telegraph", art.Sprite("fx-danger"), Vector3.zero, Vector3.one, Color.white, 3100); bossDanger.enabled = false;
            bossCountdown = Make("Boss countdown", art.Sprite("fx-disc"), Vector3.zero, Vector3.one, GatefallArt.Alpha(GatefallArt.Threat, .25f), 3101); bossCountdown.enabled = false;
            pointer = Make("Tutorial pointer", art.Sprite("icon-hand"), Vector3.zero, Vector3.one * 1.2f, Color.white, 4100); pointer.enabled = false;
            for (int i = 0; i < glyphs.Length; i++) { glyphs[i] = Make("Fusion ready", art.Sprite("fx-fusion-ready"), Vector3.zero, Vector3.one * 1.1f, Color.white, 4000); glyphs[i].enabled = false; }
            lastPlayer = game.PlayerPosition;
        }

        public void Sync(DefenseSimulation game, float visualTime, float dt, ArenaSelection selection)
        {
            time = visualTime;
            SyncPads(game, selection);
            SyncTowers(game, dt, selection);
            SyncHunter(game, dt);
            SyncCore(game, dt);
            SyncEnemies(game, dt);
            SyncShots(game);
            SyncLinks(game);
            SyncBoss(game);
            for (int i = 0; i < rifts.Length; i++) {
                if (rifts[i] == null) continue;
                riftAlarm[i] = Mathf.Max(0, riftAlarm[i] - dt);
                bool active = game.ActiveGate == i && game.State == RunState.Running;
                float pulse = 1 + .06f * Mathf.Sin(time * 3 + i) + riftAlarm[i] * .35f;
                rifts[i].transform.localScale = new Vector3(pulse, pulse, 1);
                rifts[i].color = new Color(1, 1, 1, active || riftAlarm[i] > 0 ? 1 : .78f);
            }
            pointer.enabled = selection.HasPointer;
            if (selection.HasPointer) pointer.transform.position = Point(selection.Pointer) + new Vector3(0, (board.TowerAt(System.Array.IndexOf(board.BoardSockets, selection.Pointer)) != null ? 3f : 1.4f) + .25f * Mathf.Abs(Mathf.Sin(time * 4)), 0);
            UpdateEffects();
        }

        private void SyncPads(DefenseSimulation game, ArenaSelection selection)
        {
            for (int i = 0; i < pads.Length; i++) {
                bool selected = selection.Selected == i, partner = selection.Partners.Contains(i);
                padMarks[i].enabled = selected || partner;
                if (padMarks[i].enabled) {
                    float s = .78f * (1 + .06f * Mathf.Sin(time * 6));
                    padMarks[i].transform.localScale = new Vector3(s, s, 1);
                    padMarks[i].color = partner ? GatefallArt.Ally : GatefallArt.Amber;
                }
                bool empty = game.TowerAt(i) == null;
                padPlus[i * 2].enabled = padPlus[i * 2 + 1].enabled = empty;
                pads[i].color = empty && game.IsPreparing ? Color.Lerp(Color.white, new Color(1, .97f, .88f), .5f + .5f * Mathf.Sin(time * 3 + i)) : Color.white;
            }
            bool range = selection.ShowRange && selection.Selected >= 0;
            rangeRing.enabled = rangeFill.enabled = range;
            if (range) {
                Tower tower = game.TowerAt(selection.Selected);
                bool preview = selection.PreviewKind >= 0;
                float radius = preview ? game.Stats((TowerKind)selection.PreviewKind, 1).Range : tower != null ? game.Stats(tower).Range : 0;
                rangeRing.enabled = rangeFill.enabled = radius > 0;
                Vector3 p = Point(game.BoardSockets[selection.Selected]);
                rangeRing.transform.position = rangeFill.transform.position = p;
                rangeRing.transform.localScale = Vector3.one * (radius * 2 / RangeDiameter);
                rangeFill.transform.localScale = Vector3.one * (radius * 2 / DiscDiameter);
                rangeRing.color = GatefallArt.Alpha(preview ? GatefallArt.Amber : tower != null ? GatefallArt.RoleColor(tower.Kind) : Color.white, .85f);
                rangeFill.color = GatefallArt.Alpha(preview ? GatefallArt.Amber : Color.white, .07f);
            }
        }

        private void SyncTowers(DefenseSimulation game, float dt, ArenaSelection selection)
        {
            for (int i = 0; i < towers.Length; i++) {
                Tower tower = i < game.BoardSockets.Length ? game.TowerAt(i) : null;
                int version = tower == null ? 0 : tower.Id * 4 + tower.Level;
                TowerVisual visual = towers[i];
                if (visual == null) towers[i] = visual = new TowerVisual();
                if (visual.Version != version) {
                    if (visual.Root != null) Object.Destroy(visual.Root.gameObject);
                    visual.Root = null; visual.Version = version;
                    if (tower != null) {
                        visual.Root = new GameObject("Tower " + tower.Kind + " L" + tower.Level).transform;
                        visual.Root.SetParent(root, false); visual.Root.position = Point(tower.Position);
                        visual.Body = Child(visual.Root, "Body", art.Tower(tower.Kind, tower.Level), Vector3.zero, Vector3.one, Color.white, Order(tower.Position.Y));
                        visual.Pop = .35f;
                        if (visual.VolleyFrame != Time.frameCount) { visual.Recoil = 0; visual.Kick = Vector2.zero; }
                    }
                }
                glyphs[i].enabled = tower != null && i < selection.FusionReady.Length && selection.FusionReady[i];
                if (tower == null) continue;
                visual.Pop = Mathf.Max(0, visual.Pop - dt);
                float t = 1 - visual.Pop / .35f;
                float pop = visual.Pop > 0 ? (t < .6f ? Mathf.Lerp(.55f, 1.12f, t / .6f) : Mathf.Lerp(1.12f, 1, (t - .6f) / .4f)) : 1;
                float breathe = tower.Commission > 0 ? .92f + .08f * Mathf.Sin(time * 18) : 1;
                visual.Root.localScale = new Vector3(pop * .70f, pop * breathe * .70f, 1);
                visual.Recoil = DecayAfterEventFrame(visual.Recoil, visual.VolleyFrame, dt * 7);
                float kick = visual.Recoil * visual.Recoil;
                visual.Body.transform.localScale = new Vector3(1 + kick * .065f, 1 - kick * .09f, 1);
                visual.Body.transform.localPosition = new Vector3(visual.Kick.x * kick * .24f, visual.Kick.y * kick * .18f + (1 - pop) * .28f, 0);
                visual.Body.transform.localRotation = Quaternion.Euler(0, 0, -visual.Kick.x * kick * 8);
                if (Mathf.Abs(tower.Aim.X) > .12f) visual.Body.flipX = tower.Aim.X < 0;
                bool broken = tower.Kind == TowerKind.Barrier && tower.Cooldown > 0;
                visual.Body.color = broken ? new Color(.55f, .58f, .66f, 1) : GatefallArt.TowerTint(tower.Kind, tower.Level);
                if (glyphs[i].enabled) glyphs[i].transform.position = Point(tower.Position) + new Vector3(0, 2.9f + .15f * Mathf.Sin(time * 5), 0);
            }
        }

        private void SyncHunter(DefenseSimulation game, float dt)
        {
            hunterBody.enabled = hunterRing.enabled = hunterShield.enabled = hunterHpBack.enabled = hunterHpFill.enabled = !game.HunterDown;
            Vector3 p = Point(game.PlayerPosition);
            Vector2 step = Point(game.PlayerPosition - lastPlayer);
            float distance = step.magnitude;
            bool moving = dt > 0 && distance > .002f && !game.HunterDown;
            if (moving) { hunterDirection = step / distance; hunterStride += distance * 5.4f; }
            hunterMove = Mathf.MoveTowards(hunterMove, moving ? 1 : 0, dt * 9);
            hunterRecoil = DecayAfterEventFrame(hunterRecoil, hunterShotFrame, dt * 7);
            float facing = hunterRecoil > .18f ? -hunterKick.x : moving ? hunterDirection.x : 0;
            if (Mathf.Abs(facing) > .12f) hunterBody.flipX = facing < 0;
            lastPlayer = game.PlayerPosition;
            hunter.position = p;
            float stride = Mathf.Sin(hunterStride) * hunterMove, kick = hunterRecoil * hunterRecoil;
            hunterBody.transform.localPosition = new Vector3(hunterKick.x * kick * .23f, Mathf.Abs(stride) * .15f + hunterKick.y * kick * .16f, 0);
            hunterBody.transform.localRotation = Quaternion.Euler(0, 0, -hunterDirection.x * hunterMove * 9 + stride * 3 - hunterKick.x * kick * 8);
            hunterBody.transform.localScale = new Vector3(.8f + Mathf.Abs(stride) * .02f, .8f - Mathf.Abs(stride) * .028f, .8f);
            nextFootstep = Mathf.Max(0, nextFootstep - distance);
            if (moving && nextFootstep <= 0) {
                nextFootstep = .86f;
                FootstepEffect(p, hunterDirection);
            }
            hunterBody.sortingOrder = Order(game.PlayerPosition.Y) + 2;
            hunterRing.sortingOrder = 200;
            hunterFlash = DecayAfterEventFrame(hunterFlash, hunterHurtFrame, dt);
            bool blink = game.HunterInvulnerability > 0 && Mathf.Sin(time * 40) > 0;
            hunterBody.color = hunterFlash > 0 ? new Color(1, .62f, .66f, 1) : new Color(1, 1, 1, blink ? .55f : 1);
            hunterShield.color = GatefallArt.Alpha(GatefallArt.Hex("8DB4F2"), game.HunterShield > 0 ? .75f : 0);
            hunterShield.sortingOrder = Order(game.PlayerPosition.Y) + 3;
            float hp = Mathf.Clamp01(game.PlayerHealth / game.Balance.PlayerHealth);
            hunterHpBack.sortingOrder = 4050; hunterHpFill.sortingOrder = 4051;
            hunterHpFill.transform.localScale = new Vector3(1.0f * hp, .09f, 1);
            hunterHpFill.transform.localPosition = new Vector3(-.5f * (1 - hp), 2.2f, 0);
            hunterHpFill.color = hp < .35f ? GatefallArt.Threat : GatefallArt.Ally;
        }

        private void SyncCore(DefenseSimulation game, float dt)
        {
            coreFlash = DecayAfterEventFrame(coreFlash, coreHurtFrame, dt);
            float health = Mathf.Clamp01(game.CoreHealth / game.Balance.CoreHealth);
            Color dome = Color.Lerp(GatefallArt.Threat, GatefallArt.Ally, Mathf.InverseLerp(.15f, .7f, health));
            coreDome.color = GatefallArt.Alpha(coreFlash > 0 ? GatefallArt.Threat : dome, .45f + .2f * Mathf.Sin(time * 2) + coreFlash);
            float s = 1 + .02f * Mathf.Sin(time * 2);
            coreDome.transform.localScale = new Vector3(3.8f * s, 3.3f * s, 1);
            coreShield.color = GatefallArt.Alpha(GatefallArt.Ally, game.CoreShield > 0 ? .16f + .05f * Mathf.Sin(time * 8) : 0);
            core.color = coreFlash > 0 ? new Color(1, .8f, .84f, 1) : Color.white;
        }

        private void SyncEnemies(DefenseSimulation game, float dt)
        {
            seen.Clear();
            foreach (Enemy enemy in game.Enemies) {
                if (enemy.Kind == EnemyKind.Grunt) continue;
                seen.Add(enemy.Id);
                EnemyVisual v;
                if (!enemies.TryGetValue(enemy.Id, out v)) {
                    v = enemyPool.Count > 0 ? enemyPool.Pop() : MakeEnemy();
                    v.Root.gameObject.SetActive(true);
                    v.Body.sprite = art.Sprite(GatefallArt.EnemyKey(enemy.Kind));
                    v.LastHealth = enemy.MaxHealth;
                    v.TrailHealth = Mathf.Clamp01(enemy.Health / enemy.MaxHealth);
                    v.WasHit = false; v.Body.SetPropertyBlock(null);
                    v.Body.transform.localScale = Vector3.one * EnemyScale(enemy.Kind);
                    v.SiegeMark.enabled = enemy.Kind == EnemyKind.Siege;
                    v.LastX = enemy.Position.X; v.Left = enemy.Position.X > 0;
                    v.Root.position = Point(enemy.Position);
                    enemies.Add(enemy.Id, v);
                }
                float dx = enemy.Position.X - v.LastX; v.LastX = enemy.Position.X;
                if (enemy.BlockOwner < 0) v.Left = game.PlayerPosition.X < enemy.Position.X;
                else if (Mathf.Abs(dx) > .0005f) v.Left = dx < 0;
                v.Body.flipX = v.Left;
                Vector3 destination = Point(enemy.Position);
                if ((v.Root.position - destination).sqrMagnitude > 9) v.Root.position = destination;
                else v.Root.position = Vector3.Lerp(v.Root.position, destination, 1 - Mathf.Exp(-32 * Mathf.Max(0, dt)));
                float bob = enemy.Kind == EnemyKind.Boss ? .04f : enemy.Kind == EnemyKind.Brute || enemy.Kind == EnemyKind.Siege ? .05f : .09f;
                float rate = enemy.Kind == EnemyKind.Runner ? 22 : enemy.Kind == EnemyKind.Boss ? 5 : 13;
                float hitStrength = EnemyHitStrength(v.LastHealth, enemy);
                v.LastHealth = enemy.Health;
                float hitSquash = hitStrength * .12f;
                v.Body.transform.localPosition = new Vector3(0, Mathf.Abs(Mathf.Sin(time * rate + enemy.Id)) * bob - hitSquash * .3f, 0);
                v.Body.transform.localScale = new Vector3(1 + hitSquash, 1 - hitSquash, 1) * EnemyScale(enemy.Kind);
                int order = Order(enemy.Position.Y);
                v.Body.sortingOrder = order;
                v.SiegeMark.sortingOrder = order + 2;
                v.Body.color = Color.white;
                bool hit = hitStrength > 0;
                if (hit != v.WasHit) { v.Flash.SetFloat("_Flash", hit ? .85f : 0); v.Body.SetPropertyBlock(v.Flash); v.WasHit = hit; }
                float top = EnemyBarHeight(enemy.Kind);
                v.SiegeMark.transform.localPosition = new Vector3(0, top + .28f, 0);
                float width = enemy.Kind == EnemyKind.Boss ? 3.2f : enemy.Kind == EnemyKind.Brute || enemy.Kind == EnemyKind.Siege ? 1.7f : 1.2f;
                width *= .78f;
                v.TargetMark.enabled=enemy.MarkRemaining>0;
                v.TargetMark.sortingOrder=order+3;
                v.TargetMark.transform.localPosition=new Vector3(0,top+.35f,0);
                float hp = Mathf.Clamp01(enemy.Health / enemy.MaxHealth);
                bool showHp = enemy.Kind != EnemyKind.Boss && (enemy.Kind != EnemyKind.Runner || hp < .999f);
                v.HpBack.enabled = v.HpLag.enabled = v.HpFill.enabled = showHp;
                if (showHp) {
                    v.TrailHealth = Mathf.Max(hp, Mathf.MoveTowards(v.TrailHealth, hp, dt * (hit ? .65f : 1.6f)));
                    v.HpBack.transform.localPosition = new Vector3(0, top, 0);
                    v.HpBack.transform.localScale = new Vector3(width + .10f, .19f, 1);
                    HealthStrip(v.HpLag, width, top, v.TrailHealth);
                    HealthStrip(v.HpFill, width, top, hp);
                    v.HpFill.color = EnemyHealthColor(hp);
                }
                v.Slow.enabled = enemy.SlowFraction > 0;
                if (v.Slow.enabled) { v.Slow.transform.localScale = new Vector3(width + .5f, (width + .5f) * .45f, 1); v.Slow.color = GatefallArt.Alpha(GatefallArt.RoleColor(TowerKind.Binding), .55f + .2f * Mathf.Sin(time * 9)); }
                v.Shield.enabled = enemy.BlockOwner != 0;
                if (v.Shield.enabled) {
                    V2 from = enemy.BlockOwner < 0 ? game.PlayerPosition : (game.TowerById(enemy.BlockOwner) != null ? game.TowerById(enemy.BlockOwner).Position : enemy.Position);
                    V2 dir = (enemy.Position - from).Normalized;
                    v.Shield.transform.localPosition = new Vector3(-dir.X * .4f, -dir.Y * .4f + .35f, 0);
                    v.Shield.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.Y, dir.X) * Mathf.Rad2Deg + 90);
                    v.Shield.sortingOrder = order + 1;
                }
            }
            stale.Clear();
            foreach (KeyValuePair<int, EnemyVisual> pair in enemies) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
            foreach (int id in stale) { enemies[id].Root.gameObject.SetActive(false); enemyPool.Push(enemies[id]); enemies.Remove(id); }
            SyncSwarm(game, dt);
        }

        private static float EnemyBarHeight(EnemyKind kind)
        {
            float height = kind == EnemyKind.Boss ? 5.6f : kind == EnemyKind.Brute ? 2.75f : kind == EnemyKind.Siege ? 2.4f : 1.6f;
            return height * EnemyScale(kind);
        }

        private static void HealthStrip(SpriteRenderer strip, float width, float height, float hp)
        {
            strip.transform.localPosition = new Vector3(-width * .5f * (1 - hp), height, 0);
            strip.transform.localScale = new Vector3(width * hp, .10f, 1);
        }

        private static Color EnemyHealthColor(float hp)
        { return Color.Lerp(new Color(1, .32f, .41f), new Color(1, .72f, .49f), Mathf.InverseLerp(.22f, .72f, hp)); }

        public V2 EnemyHealthAnchor(Enemy enemy)
        {
            EnemyVisual visual;
            if (!enemies.TryGetValue(enemy.Id, out visual)) return enemy.Position;
            Vector3 position = visual.Root.position;
            return new V2(position.x, position.y + EnemyBarHeight(enemy.Kind) + (enemy.MarkRemaining > 0 || enemy.Kind == EnemyKind.Siege ? .75f : .46f));
        }

        private void SyncShots(DefenseSimulation game)
        {
            seen.Clear();
            foreach (Projectile shot in game.Projectiles) {
                seen.Add(shot.Id);
                SpriteRenderer sprite;
                if (!shots.TryGetValue(shot.Id, out sprite)) {
                    sprite = shotPool.Count > 0 ? shotPool.Pop() : Make("Shot", null, Vector3.zero, Vector3.one, Color.white, 3000);
                    sprite.gameObject.SetActive(true);
                    shots.Add(shot.Id, sprite); shotStart[shot.Id] = Point(shot.Position);
                    StyleShot(sprite, shot);
                }
                Vector3 p = Point(shot.Position);
                Vector3 shotEnd = Point(shot.ImpactPosition);
                Enemy shotTarget = shot.FixedImpact ? null : game.LivingEnemy(shot.TargetId);
                if (shotTarget != null) shotEnd = Point(shotTarget.Position);
                if (shot.FromTower && shot.Kind == TowerKind.Rupture) {
                    Vector3 start = shotStart[shot.Id], end = Point(shot.ImpactPosition);
                    float total = Vector3.Distance(start, end), left = Vector3.Distance(p, end);
                    float t = total > .01f ? 1 - left / total : 1;
                    p.y += 2.2f * 4 * t * (1 - t);
                    sprite.transform.localScale = Vector3.one * (.6f + .25f * Mathf.Sin(t * Mathf.PI));
                } else {
                    Vector3 dir = shotEnd - p;
                    if (dir.sqrMagnitude > .0001f) sprite.transform.rotation = Quaternion.Euler(0, 0, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                }
                Vector3 shotOrigin = shotStart[shot.Id];
                float travel = Vector3.Distance(shotOrigin, shotEnd);
                float progress = travel > .01f ? Mathf.Clamp01(1 - Vector3.Distance(Point(shot.Position), shotEnd) / travel) : 1;
                sprite.transform.position = p + new Vector3(0, Mathf.Lerp(shot.FromTower ? 1.05f : .9f, .38f, progress), 0);
            }
            stale.Clear();
            foreach (KeyValuePair<int, SpriteRenderer> pair in shots) if (!seen.Contains(pair.Key)) stale.Add(pair.Key);
            foreach (int id in stale) { shots[id].gameObject.SetActive(false); shotPool.Push(shots[id]); shots.Remove(id); shotStart.Remove(id); }
        }

        private void StyleShot(SpriteRenderer sprite, Projectile shot)
        {
            // Neutralise the cyan baked into the shared tracer before applying role colours.
            // Explicitly reset pooled materials when a renderer changes projectile family.
            sprite.sharedMaterial = shot.FromTower && shot.Kind != TowerKind.Autocannon && shot.Kind != TowerKind.Rupture ? silhouetteMaterial : defaultEffectMaterial;
            sprite.transform.rotation = Quaternion.identity;
            if (!shot.FromTower) { sprite.sprite = art.Sprite("fx-tracer"); sprite.color = Color.white; sprite.transform.localScale = new Vector3(2.2f, 1.15f, 1); return; }
            switch (shot.Kind) {
                case TowerKind.Autocannon: sprite.sprite = art.Sprite("fx-bolt"); sprite.color = Color.white; sprite.transform.localScale = new Vector3(2.0f, 1.05f, 1); break;
                case TowerKind.Shatter: sprite.sprite = art.Sprite("fx-bolt"); sprite.color = GatefallArt.Hex("FFB070"); sprite.transform.localScale = new Vector3(2.15f, 1.35f, 1); break;
                case TowerKind.Seal: sprite.sprite = art.Sprite("fx-bolt"); sprite.color = GatefallArt.Hex("FFF6DA"); sprite.transform.localScale = new Vector3(1.3f, 2f, 1); break;
                case TowerKind.Rupture: sprite.sprite = art.Sprite("fx-shell"); sprite.color = Color.white; break;
                case TowerKind.Binding: sprite.sprite = art.Sprite("fx-tracer"); sprite.color = GatefallArt.RoleColor(TowerKind.Binding); sprite.transform.localScale = new Vector3(1.7f, 1.35f, 1); break;
                case TowerKind.Arc: sprite.sprite = art.Sprite("fx-tracer"); sprite.color = GatefallArt.RoleColor(TowerKind.Arc); sprite.transform.localScale = new Vector3(2.1f, 1.3f, 1); break;
                case TowerKind.Gravity: sprite.sprite = art.Sprite("fx-disc"); sprite.color = GatefallArt.RoleColor(TowerKind.Gravity); sprite.transform.localScale = Vector3.one * .45f; break;
                default: sprite.sprite = art.Sprite("fx-disc"); sprite.color = GatefallArt.RoleColor(shot.Kind); sprite.transform.localScale = Vector3.one * .38f; break;
            }
        }

        private void SyncLinks(DefenseSimulation game)
        {
            int n = 0;
            for (int i = 0; i < game.Towers.Count; i++)
                for (int j = i + 1; j < game.Towers.Count; j++) {
                    Tower a = game.Towers[i], b = game.Towers[j];
                    if (a.Kind != b.Kind || !game.HasResonance(a) || !game.HasResonance(b) || V2.Distance(a.Position, b.Position) > 6) continue;
                    // Resonance is a passive bonus: a quiet dotted thread, not something to manage.
                    for (int k = 1; k <= 5; k++) {
                        if (n == links.Count) links.Add(Make("Resonance", art.Sprite("fx-disc"), Vector3.zero, Vector3.one * .16f, Color.white, 145));
                        SpriteRenderer dot = links[n++]; dot.enabled = true;
                        float t = k / 6f + .03f * Mathf.Sin(time * 3 + k);
                        dot.transform.position = Vector3.Lerp(Point(a.Position), Point(b.Position), t);
                        dot.color = GatefallArt.Alpha(GatefallArt.RoleColor(a.Kind), .7f);
                    }
                }
            for (int i = n; i < links.Count; i++) links[i].enabled = false;
        }

        private void SyncBoss(DefenseSimulation game)
        {
            Enemy boss = game.Enemies.Find(e => e.Kind == EnemyKind.Boss && e.Health > 0 && e.BossWindup > 0);
            bossDanger.enabled = bossCountdown.enabled = boss != null;
            if (boss == null) return;
            float t = 1 - boss.BossWindup / 1.5f;
            Vector3 p = Point(boss.BossTarget);
            bossDanger.transform.position = bossCountdown.transform.position = p;
            bossDanger.transform.localScale = Vector3.one * (4.4f / 2.4f) * (1 + .03f * Mathf.Sin(time * 20));
            bossCountdown.transform.localScale = Vector3.one * (4.4f * t / DiscDiameter);
            bossCountdown.color = GatefallArt.Alpha(GatefallArt.Threat, .22f + .2f * t);
        }

        public void Effect(CombatEvent e)
        {
            Vector3 p = Point(e.Position);
            CountCombatEffect(e.Kind, e.Source);
            switch (e.Kind) {
                case EventKind.Kill:
                    DeathEffect(e);
                    break;
                case EventKind.Hit:
                    HitEffect(e);
                    break;
                case EventKind.Blast:
                    BlastEffect(e);
                    break;
                case EventKind.WardBurst:
                    WardBurstEffect(p, e.Value);
                    break;
                case EventKind.MarkFollowUp:
                    MarkFollowUpEffect(p);
                    break;
                case EventKind.TowerShot:
                    TowerShotEffect(e, p);
                    break;
                case EventKind.HunterShot:
                    hunterKick = (p - Point(e.Target)).normalized; hunterRecoil = 1;
                    hunterShotFrame = Time.frameCount;
                    MuzzleEffect(e);
                    break;
                case EventKind.Build: case EventKind.Upgrade:
                    if (e.Value == 0) { Spawn("fx-ring", p + new Vector3(0, .5f, 0), Vector3.zero, .4f, .5f, 3f, GatefallArt.Alpha(GatefallArt.Ally, .9f)); break; }
                    Spawn("fx-ring", p, Vector3.zero, .45f, .8f, 3.2f, GatefallArt.Alpha(GatefallArt.Amber, .95f)).Stretch = new Vector2(1, .5f);
                    Burst(p + new Vector3(0, 1.2f, 0), "fx-shard", GatefallArt.Amber, e.Kind == EventKind.Upgrade ? 12 : 8, 3.2f, .5f, .2f);
                    break;
                case EventKind.Fusion:
                    Spawn("fx-pillar", p + new Vector3(0, 1.5f, 0), Vector3.zero, .35f, .6f, 1.4f, GatefallArt.Alpha(GatefallArt.Ally, .7f)).Stretch = new Vector2(1, 1.5f);
                    Spawn("fx-ring", p, Vector3.zero, .6f, .6f, 4.6f, GatefallArt.Alpha(GatefallArt.Ally, .95f)).Stretch = new Vector2(1, .5f);
                    Spawn("fx-ring-soft", p + new Vector3(0, 1.4f, 0), Vector3.zero, .5f, .5f, 3.4f, GatefallArt.Alpha(GatefallArt.Amber, .9f));
                    Burst(p + new Vector3(0, 1.4f, 0), "fx-shard", GatefallArt.Ally, 10, 3.6f, .6f, .24f);
                    Burst(p + new Vector3(0, 1.4f, 0), "fx-shard", GatefallArt.Amber, 8, 2.8f, .6f, .2f);
                    break;
                case EventKind.Sale:
                    Spawn("fx-disc", p + new Vector3(0, .4f, 0), Vector3.zero, .4f, .5f, 2.4f, GatefallArt.Alpha(GatefallArt.Hex("B9C4CF"), .5f));
                    break;
                case EventKind.CoreHurt:
                    coreFlash = .18f; coreHurtFrame = Time.frameCount;
                    break;
                case EventKind.HunterHurt:
                    hunterFlash = .15f; hunterHurtFrame = Time.frameCount;
                    Burst(p + new Vector3(0, .8f, 0), "fx-shard", GatefallArt.Threat, 4, 2, .3f, .16f);
                    break;
                case EventKind.HunterDown:
                    Pulse(p, 2.2f, .30f, GatefallArt.Threat, true);
                    break;
                case EventKind.HunterRevived:
                    Pulse(p, 2.5f, .38f, GatefallArt.Ally, true);
                    Neutral("fx-pillar", p + Vector3.up, Vector3.zero, .27f, .56f, .18f, GatefallArt.Ally);
                    break;
                case EventKind.Shield:
                    Spawn("fx-ring-soft", new Vector3(0, .3f, 0), Vector3.zero, .8f, 2, 7, GatefallArt.Alpha(GatefallArt.Ally, 1)).Stretch = new Vector2(1, .86f);
                    break;
                case EventKind.Warning:
                    if (e.IsEntranceWarning && e.Value < rifts.Length) riftAlarm[(int)e.Value] = 1.4f;
                    break;
                case EventKind.Wave:
                    int gate = board.ActiveGate; if (gate >= 0 && gate < rifts.Length) riftAlarm[gate] = 1.0f;
                    break;
            }
        }

        private void Lightning(Vector3 a, Vector3 b)
        {
            Vector3 previous = a;
            for (int i = 1; i <= 4; i++) {
                Vector3 next = Vector3.Lerp(a, b, i / 4f);
                if (i < 4) next += (Vector3)(Random.insideUnitCircle * .28f);
                Line(previous, next, .16f, .12f, GatefallArt.Hex("FFD45A"));
                Line(previous, next, .075f, .045f, Color.white);
                previous = next;
            }
        }

        private void Burst(Vector3 p, string sprite, Color color, int count, float speed, float life, float size)
        {
            for (int i = 0; i < count; i++) {
                float a = (i + Random.value * .6f) / count * Mathf.PI * 2;
                Fx f = Spawn(sprite, p, new Vector3(Mathf.Cos(a), Mathf.Sin(a) * .7f + .5f, 0) * speed * (.6f + Random.value * .5f), life, size, size * .4f, color);
                f.Gravity = true; f.Spin = Random.Range(-400, 400);
            }
        }

        private Fx Spawn(string sprite, Vector3 p, Vector3 velocity, float life, float scale0, float scale1, Color color)
        {
            Fx fx;
            if (effects.Count >= MaxActiveEffects) { fx = effects[0]; effects.RemoveAt(0); EffectRecycleCount++; }
            else fx = effectRecords.Count > 0 ? effectRecords.Pop() : new Fx();
            SpriteRenderer renderer = fx.Sprite != null ? fx.Sprite : fxPool.Count > 0 ? fxPool.Pop() : Make("Fx", null, Vector3.zero, Vector3.one, Color.white, 3300);
            renderer.gameObject.SetActive(true);
            renderer.sharedMaterial = defaultEffectMaterial;
            if (sprite != null) renderer.sprite = art.Sprite(sprite);
            renderer.transform.position = p; renderer.transform.rotation = Quaternion.identity; renderer.transform.localScale = Vector3.one * scale0; renderer.color = color; renderer.sortingOrder = 3300; renderer.flipX = false;
            fx.Sprite = renderer; fx.Velocity = velocity; fx.Life = fx.Total = life; fx.Scale0 = scale0; fx.Scale1 = scale1; fx.Color = color; fx.Stretch = Vector2.one; fx.Spin = 0; fx.Gravity = false; fx.FadePower = 2;
            fx.SpawnFrame = Time.frameCount;
            effects.Add(fx);
            PeakEffectCount = Mathf.Max(PeakEffectCount, effects.Count);
            return fx;
        }

        private void UpdateEffects()
        {
            // Short visual feedback stays smooth during UI pauses; at most220 pooled effects can exist.
            float step = Mathf.Clamp(Time.unscaledDeltaTime, 0, .05f);
            int frame = Time.frameCount;
            for (int i = effects.Count - 1; i >= 0; i--) {
                Fx f = effects[i];
                // Time before an effect existed should not consume its first visible frame.
                float age = f.SpawnFrame == frame ? 0 : step;
                f.Life -= age;
                if (f.Life <= 0) { f.Sprite.gameObject.SetActive(false); effectRecords.Push(f); effects.RemoveAt(i); continue; }
                float t = 1 - f.Life / f.Total;
                if (f.Gravity) f.Velocity += new Vector3(0, -9f, 0) * age;
                f.Sprite.transform.position += f.Velocity * age;
                if (f.Spin != 0) f.Sprite.transform.Rotate(0, 0, f.Spin * age);
                float s = Mathf.Lerp(f.Scale0, f.Scale1, 1 - (1 - t) * (1 - t));
                f.Sprite.transform.localScale = new Vector3(s * f.Stretch.x, s * f.Stretch.y, 1);
                f.Sprite.color = GatefallArt.Alpha(f.Color, f.Color.a * (1 - Mathf.Pow(t, f.FadePower)));
            }
        }

        public void SetVisible(bool visible) { if (root != null && root.gameObject.activeSelf != visible) root.gameObject.SetActive(visible); }

        public void Dispose()
        {
            if (root != null) { root.gameObject.SetActive(false); Object.Destroy(root.gameObject); }
            foreach (Object item in owned) if (item != null) Object.Destroy(item);
        }

        private static float EnemyScale(EnemyKind kind) { return kind == EnemyKind.Boss ? .60f : kind == EnemyKind.Siege ? .75f : kind == EnemyKind.Grunt ? .42f : kind == EnemyKind.Runner ? .46f : .68f; }

        private EnemyVisual MakeEnemy()
        {
            EnemyVisual v = new EnemyVisual();
            v.Root = new GameObject("Enemy").transform; v.Root.SetParent(root, false);
            v.Slow = Child(v.Root, "Slow", art.Sprite("fx-ring"), new Vector3(0, .05f, 0), Vector3.one, Color.white, 190);
            v.Body = Child(v.Root, "Body", null, Vector3.zero, Vector3.one, Color.white, 1000);
            v.Body.sharedMaterial = enemyFlashMaterial;
            v.SiegeMark = Child(v.Root, "Siege warning", art.Sprite("fx-danger"), new Vector3(0, 2.8f, 0), Vector3.one * .45f, GatefallArt.Threat, 1001);
            v.TargetMark = Child(v.Root, "Target mark", art.Sprite("fx-select"), new Vector3(0, 2, 0), Vector3.one * .34f, GatefallArt.Hex("66DDF2"), 1002);
            v.Shield = Child(v.Root, "Held", art.Sprite("fx-shield"), Vector3.zero, Vector3.one * .65f, Color.white, 1000);
            v.HpBack = Child(v.Root, "Hp back", square, Vector3.zero, Vector3.one, GatefallArt.Alpha(GatefallArt.Ink, .85f), 4050);
            v.HpLag = Child(v.Root, "Recent damage", square, Vector3.zero, Vector3.one, GatefallArt.Paper, 4051);
            v.HpFill = Child(v.Root, "Hp fill", square, Vector3.zero, Vector3.one, GatefallArt.Threat, 4052);
            return v;
        }

        private SpriteRenderer Make(string name, Sprite sprite, Vector3 position, Vector3 scale, Color color, int order)
        { return Child(root, name, sprite, position, scale, color, order, true); }
        private SpriteRenderer Child(Transform parent, string name, Sprite sprite, Vector3 position, Vector3 scale, Color color, int order, bool world = false)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position; obj.transform.localScale = scale;
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite; renderer.color = color; renderer.sortingOrder = order;
            return renderer;
        }
        // World diameters of the generated effect textures at 100 px per unit.
        private const float RingDiameter = 1.2f, DiscDiameter = .6f, RangeDiameter = 5.0f;
        // Ground-footprint y-sorting: lower on screen draws in front.
        public static int Order(float y) { return 1000 + Mathf.RoundToInt((12 - y) * 40); }
        public static Vector3 Point(V2 p) { return new Vector3(p.X, p.Y, 0); }
        public static Color Hex(string value) { return GatefallArt.Hex(value); }
        public static Color Alpha(Color color, float alpha) { return GatefallArt.Alpha(color, alpha); }
        public static Color TowerColor(TowerKind kind) { return GatefallArt.RoleColor(kind); }
    }
}
