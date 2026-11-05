using System;
using System.Collections.Generic;

namespace Gatefall.Core
{
    public struct Box2
    {
        public float X, Y, Width, Height;
        public Box2(float x, float y, float width, float height) { X = x; Y = y; Width = width; Height = height; }
        public V2 Center { get { return new V2(X + Width / 2, Y + Height / 2); } }
        public bool Contains(V2 p) { return p.X >= X && p.Y >= Y && p.X <= X + Width && p.Y <= Y + Height; }
    }

    // v0.5 keeps the old values for save/test stability; Build/Range/Recipes/Map are no longer on-screen.
    public enum MobileAction { None, Start, Build, Pause, Resume, Restart, Range, Sound, Recipes, Map, Skill, Speed }
    public struct TouchRegion
    {
        public MobileAction Action; public Box2 Box;
        public TouchRegion(int action, Box2 box) { Action = (MobileAction)action; Box = box; }
    }
    public enum PointerPhase { Begin, Move, End, Cancel }

    // Only the explicit pause screen controls combat time; tower panels remain live.
    public static class BattleMenuTime
    {
        public static void Open(DefenseSimulation game, bool pauseScreen)
        { if (pauseScreen && game.State == RunState.Running) game.TogglePause(); }
        public static void Close(DefenseSimulation game, bool wasPauseScreen)
        { if (wasPauseScreen && game.State == RunState.Paused && !game.AwaitingCard) game.TogglePause(); }
    }

    // Shared by drawing and hit testing, in top-left-origin reference coordinates.
    // SafeArea enters in Unity's bottom-left-origin screen coordinates.
    public sealed class MobileLayout
    {
        public float Scale { get; private set; }
        public float Width { get; private set; }
        public float Height { get; private set; }
        public V2 Origin { get; private set; }
        public Box2 Arena { get; private set; }
        public Box2 SafeArea { get; private set; }
        public float ScreenHeight { get; private set; }
        public V2 JoystickCenter { get { return new V2(118, Height - 118); } }
        public const float JoystickRadius = 58;
        // A thumb rarely lands exactly on the stick: accept a generous circle around it.
        public const float JoystickCatch = 118;
        public const int WaveCallActionBase = 760;
        public Box2 WaveCallBox() { return new Box2(Width/2-190,Height-112,380,84); }

        public MobileLayout(float screenWidth, float screenHeight, Box2 safe)
        {
            ScreenHeight = Math.Max(1, screenHeight);
            float width = Math.Max(1, screenWidth);
            if (safe.Width <= 0 || safe.Height <= 0) safe = new Box2(0, 0, width, ScreenHeight);
            safe.X = Math.Max(0, Math.Min(width - 1, safe.X));
            safe.Y = Math.Max(0, Math.Min(ScreenHeight - 1, safe.Y));
            safe.Width = Math.Max(1, Math.Min(safe.Width, width - safe.X));
            safe.Height = Math.Max(1, Math.Min(safe.Height, ScreenHeight - safe.Y));
            SafeArea = safe;
            Scale = Math.Min(safe.Width / 960, safe.Height / 600);
            Width = Math.Max(960, safe.Width / Scale); Height = Math.Max(600, safe.Height / Scale);
            Origin = new V2(safe.X, ScreenHeight - safe.Y - safe.Height);
            Arena = new Box2((Width - 960) / 2, (Height - 600) / 2, 960, 600);
        }

        public V2 FromScreen(V2 screenBottomLeft)
        { return new V2((screenBottomLeft.X - Origin.X) / Scale, (ScreenHeight - screenBottomLeft.Y - Origin.Y) / Scale); }
        public V2 ToScreen(V2 point)
        { return new V2(Origin.X + point.X * Scale, ScreenHeight - Origin.Y - point.Y * Scale); }
        public Box2 Button(MobileAction action)
        {
            switch (action)
            {
                case MobileAction.Skill: return new Box2(Width - 156, Height - 156, 128, 128);
                case MobileAction.Pause: return new Box2(Width - 78, 12, 62, 58);
                // Keep the north entrances visible; this gap follows the barrier chip.
                case MobileAction.Speed: return new Box2(274, 8, 62, 54);
                case MobileAction.Start: return new Box2(Width / 2 - 160, Height - 104, 320, 76);
                case MobileAction.Resume: return new Box2(Width / 2 - 170, Height / 2 - 52, 340, 66);
                case MobileAction.Restart: return new Box2(Width / 2 - 170, Height / 2 + 26, 340, 60);
                case MobileAction.Sound: return new Box2(Width / 2 + 130, Height / 2 - 150, 60, 58);
                default: return new Box2();
            }
        }
        // Separate the north off-screen warning from notices while leaving the wave bar visible.
        public Box2 NorthThreatTag() { return new Box2(Width / 2 - 65, 62, 130, 36); }
        // Keep wave information together and clear of the largest entrance warning, without animation-time motion.
        // The right limit reserves both currency chips; the left limit follows the existing speed control.
        public Box2 WaveHeader(IList<Box2> northRifts)
        {
            Box2 original=new Box2(Width/2-90.5f,4,181,52);
            if(northRifts==null || northRifts.Count==0)return original;
            bool blocked=false;
            for(int i=0;i<northRifts.Count;i++)if(HeaderOverlaps(original,northRifts[i])){blocked=true;break;}
            if(!blocked)return original;
            float left=Button(MobileAction.Speed).X+Button(MobileAction.Speed).Width+8;
            for(int mode=0;mode<2;mode++) {
                float w=mode==0?181:150;int reach=mode==0?72:128,step=mode==0?4:2;
                for(int d=0;d<=reach;d+=step)for(int sign=0;sign<(d==0?1:2);sign++) {
                    float center=Width/2+(sign==0?d:-d);
                    Box2 b=new Box2(center-w/2,4,w,52);
                    if(b.X<left || b.X+b.Width>Width-322)continue;
                    bool clash=false;
                    for(int i=0;i<northRifts.Count;i++)if(HeaderOverlaps(b,northRifts[i])){clash=true;break;}
                    if(!clash)return b;
                }
            }
            return original;
        }
        private static bool HeaderOverlaps(Box2 a,Box2 b)
        { return a.X<b.X+b.Width+4 && b.X-4<a.X+a.Width && a.Y<b.Y+b.Height+4 && b.Y-4<a.Y+a.Height; }
        public bool TryFusionHeader(IList<Box2> towerGuards, out Box2 panel)
        {
            const float width = 640, height = 92, top = 72, margin = 12;
            float centered = (Width - width) / 2;
            for (int column = 0; column < 3; column++) {
                float x = column == 0 ? centered : column == 1 ? margin : Width - width - margin;
                for (int row = 0; row < 2; row++) {
                    var candidate = new Box2(x, row == 0 ? top : Height - height - margin, width, height);
                    bool clear = true;
                    foreach (Box2 tower in towerGuards)
                        if (HeaderOverlaps(candidate, tower)) { clear = false; break; }
                    if (clear) { panel = candidate; return true; }
                }
            }
            panel = new Box2(centered, top, width, height);
            return false;
        }
        public Box2 NoticeBox(int length, bool preparing)
        {
            float width = Math.Max(260, Math.Min(620, length * 15 + 60));
            return new Box2(Width / 2 - width / 2, preparing ? 166 : 104, width, 40);
        }
        public Box2 NoticeBox(int length,bool preparing,Box2 panel,bool critical)
        {
            Box2 b=NoticeBox(length,preparing);
            if(critical || panel.Width<=0 || panel.Height<=0 || b.X+b.Width<=panel.X || panel.X+panel.Width<=b.X || b.Y+b.Height<=panel.Y || panel.Y+panel.Height<=b.Y)return b;
            float below=panel.Y+panel.Height+8;
            if(below+b.Height<=Height-8)b.Y=below;
            else if(panel.Y-b.Height-8>=62)b.Y=panel.Y-b.Height-8;
            return b;
        }
        public MobileAction HitButton(V2 point, RunState state)
        {
            if (state == RunState.Ready)
            {
                if (Button(MobileAction.Pause).Contains(point)) return MobileAction.Pause;
                return Button(MobileAction.Start).Contains(point) ? MobileAction.Start : MobileAction.None;
            }
            if (state == RunState.Paused)
            {
                if (Button(MobileAction.Sound).Contains(point)) return MobileAction.Sound;
                if (Button(MobileAction.Resume).Contains(point)) return MobileAction.Resume;
                return Button(MobileAction.Restart).Contains(point) ? MobileAction.Restart : MobileAction.None;
            }
            if (state == RunState.Won || state == RunState.Lost)
                return Button(MobileAction.Restart).Contains(point) ? MobileAction.Restart : MobileAction.None;
            if (Button(MobileAction.Pause).Contains(point)) return MobileAction.Pause;
            if (Button(MobileAction.Speed).Contains(point)) return MobileAction.Speed;
            return Button(MobileAction.Skill).Contains(point) ? MobileAction.Skill : MobileAction.None;
        }
    }

    // Finger IDs, not touch-array indexes: movement and building can happen together.
    public sealed class MobileInput
    {
        private sealed class Capture { public V2 Start; public MobileAction Action; public bool Dragged; }
        private readonly Dictionary<int, Capture> taps = new Dictionary<int, Capture>();
        public readonly List<MobileAction> Actions = new List<MobileAction>();
        public readonly List<TouchRegion> Regions = new List<TouchRegion>();
        public bool Modal, AllowPreparation;
        public V2 Movement { get; private set; }
        public int JoystickFinger { get; private set; }
        public MobileLayout Layout { get; private set; }
        public RunState State { get; private set; }

        public MobileInput(MobileLayout layout) { Layout = layout; JoystickFinger = int.MinValue; State = RunState.Ready; }
        public void SetLayout(MobileLayout layout) { Reset(); Layout = layout; }
        public void SetState(RunState state) { if (state != State) { Reset(); State = state; } }
        public void Reset() { Movement = new V2(); JoystickFinger = int.MinValue; taps.Clear(); Actions.Clear(); }

        public void Pointer(int id, PointerPhase phase, V2 position)
        {
            if (phase == PointerPhase.Begin)
            {
                if (id == JoystickFinger || taps.ContainsKey(id)) return;
                // The visible stick remains draggable even when a distant pad projects beneath it.
                if (!Modal && (State == RunState.Running || State == RunState.Ready && AllowPreparation) && JoystickFinger == int.MinValue &&
                    V2.Distance(position, Layout.JoystickCenter) <= MobileLayout.JoystickRadius)
                { JoystickFinger = id; MoveStick(position); return; }
                MobileAction action = Hit(position);
                if (action != MobileAction.None) { taps[id] = new Capture { Start = position, Action = action }; return; }
                if (!Modal && (State == RunState.Running || State==RunState.Ready && AllowPreparation) && JoystickFinger == int.MinValue &&
                    V2.Distance(position, Layout.JoystickCenter) <= MobileLayout.JoystickCatch)
                { JoystickFinger = id; MoveStick(position); }
                return;
            }
            if (id == JoystickFinger)
            {
                if (phase == PointerPhase.End || phase == PointerPhase.Cancel) { JoystickFinger = int.MinValue; Movement = new V2(); }
                else MoveStick(position);
                return;
            }
            Capture tap;
            if (!taps.TryGetValue(id, out tap)) return;
            if (V2.Distance(position, tap.Start) > 26) tap.Dragged = true;
            if (phase == PointerPhase.End || phase == PointerPhase.Cancel)
            {
                taps.Remove(id);
                if (phase == PointerPhase.End && !tap.Dragged && Hit(position) == tap.Action)
                    Actions.Add(tap.Action);
            }
        }
        public void ReleaseMissingJoystick(bool stillPresent)
        { if (!stillPresent) { JoystickFinger = int.MinValue; Movement = new V2(); } }
        private MobileAction Hit(V2 position)
        {
            foreach (TouchRegion region in Regions) if (region.Box.Contains(position)) return region.Action;
            return Modal ? MobileAction.None : Layout.HitButton(position, State);
        }
        private void MoveStick(V2 position)
        {
            V2 delta = (position - Layout.JoystickCenter) / MobileLayout.JoystickRadius;
            if (delta.Length < 0.12f) delta = new V2();
            if (delta.Length > 1) delta = delta.Normalized;
            Movement = new V2(delta.X, -delta.Y);
        }
    }
}
