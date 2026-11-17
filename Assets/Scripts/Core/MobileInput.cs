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
        public Box2 WaveCallBox() { return waveCall.Width>0 ? waveCall : DefaultWaveCall(); }
        private Box2 DefaultWaveCall() { return new Box2(Width/2-190,Height-112,380,84); }
        private Box2 DefaultStart() { return new Box2(Width / 2 - 160, Height - 104, 320, 76); }
        private Box2 DefaultSpeed() { return new Box2(274, 8, 62, 54); }
        private Box2 start, waveCall, speed;
        public const float CompactControlHeight = 56, NarrowStartWidth = 220, NarrowWaveCallWidth = 280;
        // Fixed controls share the edges of the board with entrances and the outermost pads.
        // Each keeps its usual place when that is clear. Otherwise the two bottom buttons drop into the
        // safe-area margin, become shorter and narrower, then slide between the thumbs; the speed button
        // slides along the top bar when it would sit on an entrance. A null or empty list restores every default.
        public void PlaceControls(IList<Box2> padsAndRifts,IList<Box2> rifts)
        {
            start=waveCall=speed=new Box2();
            if(padsAndRifts==null || padsAndRifts.Count==0)return;
            Box2 preferred=DefaultSpeed();
            if(!StatusClear(preferred,rifts)) {
                // First a slot clear of everything; on a crowded top edge at least leave the entrance visible.
                for(int pass=0;pass<2 && speed.Width==0;pass++)
                    for(float x=preferred.X;x+preferred.Width<=Width-322-150-8;x+=2) {  // leave room for a compact wave header
                        var candidate=new Box2(x,preferred.Y,preferred.Width,preferred.Height);
                        if(StatusClear(candidate,pass==0?padsAndRifts:rifts)){speed=candidate;break;}
                    }
            }
            float left=JoystickCenter.X+JoystickCatch+8;
            start=BottomControl(DefaultStart(),NarrowStartWidth,left,Width-16,padsAndRifts);
            waveCall=BottomControl(DefaultWaveCall(),NarrowWaveCallWidth,left,Button(MobileAction.Skill).X-8,padsAndRifts);
        }
        private Box2 BottomControl(Box2 preferred,float narrow,float left,float right,IList<Box2> guards)
        {
            if(StatusClear(preferred,guards))return preferred;
            float low=Height-8-CompactControlHeight,centre=preferred.Center.X;
            Box2[] shapes={preferred,
                new Box2(preferred.X,Height-8-preferred.Height,preferred.Width,preferred.Height),
                new Box2(preferred.X,low,preferred.Width,CompactControlHeight),
                new Box2(centre-narrow/2,low,narrow,CompactControlHeight)};
            for(int i=1;i<shapes.Length;i++)if(StatusClear(shapes[i],guards))return shapes[i];
            foreach(Box2 shape in shapes) {
                Box2 best=shape;float bestDistance=float.PositiveInfinity;
                for(float x=left;x+shape.Width<=right;x+=2) {
                    var candidate=new Box2(x,shape.Y,shape.Width,shape.Height);
                    float distance=Math.Abs(candidate.Center.X-centre);
                    if(distance>=bestDistance || !StatusClear(candidate,guards))continue;
                    best=candidate;bestDistance=distance;
                }
                if(!float.IsPositiveInfinity(bestDistance))return best;
            }
            // Nothing along the bottom is completely clear: hide as little as possible with the smallest shape.
            Box2 least=shapes[3];float hidden=float.PositiveInfinity;
            for(float x=left;x+narrow<=right;x+=2) {
                var candidate=new Box2(x,low,narrow,CompactControlHeight);
                float area=0;
                for(int i=0;i<guards.Count;i++) {
                    Box2 g=guards[i];
                    float w=Math.Min(candidate.X+candidate.Width,g.X+g.Width)-Math.Max(candidate.X,g.X);
                    float h=Math.Min(candidate.Y+candidate.Height,g.Y+g.Height)-Math.Max(candidate.Y,g.Y);
                    if(w>0 && h>0)area+=w*h;
                }
                area+=Math.Abs(candidate.Center.X-centre)*.001f;
                if(area<hidden){least=candidate;hidden=area;}
            }
            return least;
        }
        // Context panel for one pad. Keep the previous above/below rule whenever it fits; when the screen is
        // too short for either, sit beside the pad instead of being clamped on top of the selected tower.
        public Box2 PanelBox(V2 pad,float unit,float width,float height)
        {
            float top=70,bottom=Math.Max(top,Height-height-10);
            float x=Math.Max(12,Math.Min(Width-width-12,pad.X-width/2));
            float above=pad.Y-height-2.9f*unit,below=pad.Y+.9f*unit;
            bool preferAbove=pad.Y>Height*.5f;
            float first=preferAbove?above:below,second=preferAbove?below:above;
            // The selected pad and the tower standing on it.
            float padTop=pad.Y-2.3f*unit+.5f,padBottom=pad.Y+.9f*unit-.5f;
            var usual=new Box2(x,Math.Max(top,Math.Min(bottom,first)),width,height);
            if(usual.Y+height<=padTop || usual.Y>=padBottom)return usual;
            if(second>=top && second<=bottom)return new Box2(x,second,width,height);
            float gap=1.6f*unit,right=pad.X+gap,leftSide=pad.X-gap-width;
            bool rightFits=right+width<=Width-12,leftFits=leftSide>=12;
            float y=Math.Max(top,Math.Min(bottom,pad.Y-unit-height/2));
            if(rightFits && (!leftFits || pad.X<Width/2))return new Box2(right,y,width,height);
            if(leftFits)return new Box2(leftSide,y,width,height);
            return usual;
        }

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
        public bool HomeCompact { get { return Width<=960.5f; } }
        public Box2 HomePass() { return new Box2(24,108,336,76); }
        public Box2 HomePanel() { return new Box2(24,196,336,Height-340); }
        // Hunter, hearts, heart shop, cores, crystals, crystal shop, settings.
        public Box2 HomeTop(int index)
        {
            float extra=Width-960;
            switch(index) {
                case 0:return new Box2(24,16,154,56);
                case 1:return new Box2(190,16,208,56);
                case 2:return new Box2(406,16,48,56);
                case 3:return new Box2(466+extra*.3f,16,152,56);
                case 4:return new Box2(630+extra*.6f,16,194,56);
                case 5:return new Box2(832+extra,16,48,56);
                case 6:return new Box2(888+extra,16,48,56);
                default:return new Box2();
            }
        }
        public Box2 HomeNavigation(int index,int count=5,int first=0)
        {
            if(count<1 || index<first || index>=Math.Min(count,first+7))return new Box2();
            int visible=Math.Min(7,count-first);float inset=count>7?80:24;
            float width=(Width-2*inset-(visible-1)*8)/visible;
            return new Box2(inset+(index-first)*(width+8),Height-116,width,92);
        }
        public Box2 HomeTabArrow(bool next) { return new Box2(next?Width-72:24,Height-116,48,92); }
        public Box2 HomeLaunch() { return HomeNavigation(2); }
        public Box2 HomeRight(int index,int count=3,bool expanded=false)
        {
            int visible=count>4 && !expanded ? 4 : count;
            if(index<0 || index>=visible || visible<1)return new Box2();
            float height=Math.Min(76,(Height-260-(visible-1)*8)/visible);
            return new Box2(Width-192,108+index*(height+8),168,height);
        }
        public Box2 HeartDialog() { return new Box2(Width/2-260,Height/2-210,520,420); }
        public Box2 HeartDialogButton(int index)
        { Box2 r=HeartDialog();return index<0 || index>2 ? new Box2() : new Box2(r.X+24,r.Y+166+index*76,r.Width-48,64); }
        public Box2 ShopPanel() { return new Box2(24,88,Width-48,Height-112); }
        public Box2 ShopTop(int index)
        { return index==0?new Box2(24,16,56,56):index==1?new Box2(96,16,200,56):index==2?new Box2(Width-528,16,252,56):new Box2(Width-264,16,240,56); }
        public Box2 ShopTab(int index,int count=5)
        { float w=(Width-48-(count-1)*8)/count;return new Box2(24+index*(w+8),88,w,48); }
        public Box2 ShopSlot(int index)
        { float w=(Width-72)/3,h=(Height-256)/2;return new Box2(24+index%3*(w+12),148+index/3*(h+12),w,h); }
        public Box2 ShopSlotButton(int index)
        { Box2 r=ShopSlot(index);return new Box2(r.X+12,r.Y+r.Height-60,r.Width-24,48); }
        public Box2 ShopFooter() { return new Box2(24,Height-84,Width-48,48); }
        public Box2 ShopProduct(int index)
        { float w=(Width-60)/2;return new Box2(24+index*(w+12),148,w,Height-256); }
        public Box2 ShopProductButton(int index)
        { Box2 r=ShopProduct(index);return new Box2(r.X+20,r.Y+r.Height-68,r.Width-40,52); }
        public Box2 SupplyPanel() { return ShopPanel(); }
        public Box2 SupplyClose() { return new Box2(Width-144,100,108,48); }
        public Box2 SupplyKindTab(int index) { return new Box2(24+index*148,100,140,48); }
        public int SupplyVisibleRows(bool odds)
        { return Math.Max(1,(int)((Height-(odds?392:256))/(odds?32:56))); }
        public Box2 SupplyRow(int index,bool odds)
        { return new Box2(44,160+index*(odds?32:56),Width-88,odds?32:56); }
        public Box2 SupplyRule() { return new Box2(44,Height-224,Width-88,132); }
        public Box2 SupplyScroll(bool next)
        { return new Box2(next?Width-184:44,Height-80,140,48); }
        public Box2 TowerCard(int index)
        { float w=(Width-72)/3,h=(Height-264)/2;return new Box2(24+index%3*(w+12),148+index/3*(h+12),w,h); }
        public Box2 TowerGradeButton(int index)
        { Box2 r=TowerCard(index);return new Box2(r.X+12,r.Y+r.Height-56,r.Width-24,48); }
        public Box2 TowerCardPage(bool next) { return SupplyScroll(next); }
        public Box2 TowerCardRule() { return new Box2(200,Height-80,Width-400,48); }
        public Box2 DailyBack() { return new Box2(24,16,56,56); }
        public Box2 DailyTitle() { return new Box2(96,16,Width-120,56); }
        public Box2 DailySummary() { return new Box2(24,88,Width-48,48); }
        public Box2 DailyFooter() { return new Box2(24,Height-66,Width-48,48); }
        public Box2 MissionRow(int index) { return new Box2(24,144+index*56,Width-48,52); }
        public Box2 MissionClaim(int index) { return new Box2(Width-160,146+index*56,124,48); }
        public Box2 MissionAllClear() { return new Box2(24,488,Width-48,32); }
        public Box2 AttendanceDay(int index)
        { float w=(Width-96)/7;return new Box2(24+index*(w+8),160,w,168); }
        public Box2 DailyPrimary() { return new Box2(Width/2-220,Height-140,440,56); }
        public Box2 RouletteOdds() { return new Box2(Width-148,88,124,48); }
        public int DailyVisibleRows() { return Math.Max(1,(int)((Height-240)/64)); }
        public Box2 DailyRow(int index) { return new Box2(24,160+index*64,Width-48,56); }
        public Box2 PassClaim(int index,bool paid)
        { return new Box2(paid?Width-160:Width/2-144,164+index*64,124,48); }
        public Box2 PassCap() { return new Box2(Width-292,88,268,48); }
        public Box2 DailyScroll(bool next) { return new Box2(next?Width-164:24,Height-66,140,48); }
        public Box2 DailyPageLabel() { return new Box2(176,Height-66,Width-352,48); }
        public Box2 VictoryPanel() { return new Box2(Width/2-300,Height/2-250,600,500); }
        public Box2 VictoryDouble() { Box2 r=VictoryPanel();return new Box2(r.X+80,r.Y+304,440,52); }
        public Box2 Button(MobileAction action)
        {
            switch (action)
            {
                case MobileAction.Skill: return new Box2(Width - 156, Height - 156, 128, 128);
                case MobileAction.Pause: return new Box2(Width - 78, 12, 62, 58);
                // Keep the north entrances visible; this gap follows the barrier chip.
                case MobileAction.Speed: return speed.Width>0 ? speed : DefaultSpeed();
                case MobileAction.Start: return start.Width>0 ? start : DefaultStart();
                case MobileAction.Resume: return new Box2(Width / 2 - 170, Height / 2 - 52, 340, 66);
                case MobileAction.Restart: return new Box2(Width / 2 - 170, Height / 2 + 26, 340, 60);
                case MobileAction.Sound: return new Box2(Width / 2 + 130, Height / 2 - 150, 60, 58);
                default: return new Box2();
            }
        }
        // Separate the north off-screen warning from notices while leaving the wave bar visible.
        public Box2 NorthThreatTag() { return new Box2(Width / 2 - 65, 62, 130, 36); }
        // Full-board views reserve tower bodies and vacant pads.
        // Keep the normal chip when clear; use a smaller read-only status chip only when needed.
        public Box2 CoreStatusBox(IList<Box2> worldGuards)
        {
            Box2 original=new Box2(14,10,252,50);
            if(StatusClear(original,worldGuards))return original;
            Box2 compact=new Box2(14,4,176,26);
            return StatusClear(compact,worldGuards)?compact:original;
        }
        private static bool StatusClear(Box2 chip,IList<Box2> guards)
        {
            if(guards!=null)for(int i=0;i<guards.Count;i++)if(HeaderOverlaps(chip,guards[i]))return false;
            return true;
        }
        // Keep wave information together and clear of the largest entrance warning, without animation-time motion.
        // The right limit reserves both currency chips; the left limit follows the existing speed control.
        public const float CompactWaveHeight = 28;
        public Box2 WaveHeader(IList<Box2> northRifts, IList<Box2> worldGuards = null)
        {
            Box2 preferred = RiftWaveHeader(northRifts);
            if (StatusClear(preferred, worldGuards)) return preferred;

            // Search only between the speed control and currency chips. If the top row
            // is too crowded, preserve the rift-aware placement rather than hide a control.
            Box2 speed = Button(MobileAction.Speed);
            float left = speed.X + speed.Width + 8, right = Width - 322;
            for (int mode = 0; mode < 3; mode++) {
                float width = mode == 0 ? 181 : 150;
                float height = mode == 2 ? CompactWaveHeight : preferred.Height;
                Box2 best = preferred;
                float bestDistance = float.PositiveInfinity;
                for (float x = left; x + width <= right; x += 2) {
                    Box2 candidate = new Box2(x, preferred.Y, width, height);
                    float distance = Math.Abs(candidate.Center.X - preferred.Center.X);
                    if (distance >= bestDistance || !StatusClear(candidate, northRifts) || !StatusClear(candidate, worldGuards)) continue;
                    best = candidate;
                    bestDistance = distance;
                }
                if (!float.IsPositiveInfinity(bestDistance)) return best;
            }
            return preferred;
        }
        public Box2 BossStatusDock(IList<Box2> worldGuards)
        {
            const float width = 200;
            Box2 revival = new Box2(14, 68, 244, 42);
            Box2 joystick = new Box2(0, Height - 2 * JoystickCatch, 2 * JoystickCatch, 2 * JoystickCatch);
            Box2 skill = Button(MobileAction.Skill), waveCall = WaveCallBox();
            // Start beneath the currency and tower count. Both edges are available, with
            // the center reserved for the approach and existing notices.
            for (int height = 48; height >= 44; height -= 4)
                for (int y = 84; y + height + 4 <= waveCall.Y; y += 2)
                    for (int inset = 14; inset + width <= Width / 2 - 4; inset += 2) {
                        Box2 right = new Box2(Width - width - inset, y, width, height);
                        if (StatusClear(right, worldGuards) && !HeaderOverlaps(right, skill)) return right;
                        Box2 left = new Box2(inset, y, width, height);
                        if (StatusClear(left, worldGuards) && !HeaderOverlaps(left, revival) && !HeaderOverlaps(left, joystick)) return left;
                    }
            return new Box2();
        }
        public static Box2 RoadGuard(V2 a, V2 b, float halfWidth, float above, float below)
        {
            return new Box2(Math.Min(a.X,b.X)-halfWidth, Math.Min(a.Y,b.Y)-above,
                Math.Abs(a.X-b.X)+2*halfWidth, Math.Abs(a.Y-b.Y)+above+below);
        }
        private Box2 RiftWaveHeader(IList<Box2> northRifts)
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

        public const float EntranceLabelWidth = 64, EntranceLabelHeight = 26;
        public Box2 EntranceLabel(Box2 rift, IList<Box2> world, IList<Box2> roads, IList<Box2> occupied)
        {
            const float margin = 6;
            float width = EntranceLabelWidth, height = EntranceLabelHeight;
            Box2 Clamp(float x, float y) => new Box2(
                Math.Max(margin, Math.Min(Width - margin - width, x)),
                Math.Max(margin, Math.Min(Height - margin - height, y)), width, height);
            for (int distance = 6; distance <= 144; distance += 6) {
                for (int offset = 0; offset <= 168; offset += 6) {
                    for (int sign = 0; sign < (offset == 0 ? 1 : 2); sign++) {
                        float shift = sign == 0 ? offset : -offset;
                        var candidate = Clamp(rift.Center.X - width / 2 + shift, rift.Y - height - distance);
                        if (Clear(candidate)) return candidate;
                        candidate = Clamp(rift.Center.X - width / 2 + shift, rift.Y + rift.Height + distance);
                        if (Clear(candidate)) return candidate;
                        candidate = Clamp(rift.X - width - distance, rift.Center.Y - height / 2 + shift);
                        if (Clear(candidate)) return candidate;
                        candidate = Clamp(rift.X + rift.Width + distance, rift.Center.Y - height / 2 + shift);
                        if (Clear(candidate)) return candidate;
                    }
                }
            }
            Box2 nearest = new Box2();
            float best = float.PositiveInfinity;
            for (float y = margin; y + height <= Height - margin; y += 6)
                for (float x = margin; x + width <= Width - margin; x += 6) {
                    var candidate = new Box2(x, y, width, height);
                    float distance = V2.DistanceSquared(candidate.Center, rift.Center);
                    if (distance >= best || !Clear(candidate)) continue;
                    best = distance;
                    nearest = candidate;
                }
            return nearest;

            bool Clear(Box2 candidate) => StatusClear(candidate, world) && StatusClear(candidate, roads) && StatusClear(candidate, occupied);
        }

        public Box2 TowerCountBox(bool catalysts, Box2 wave, IList<Box2> world)
        {
            float currencyLeft = Width - (catalysts ? 314 : 220);
            var inline = new Box2(currencyLeft - 124, 10, 116, 50);
            Box2 speed = Button(MobileAction.Speed);
            if (!HeaderOverlaps(inline, wave) && !HeaderOverlaps(inline, speed) &&
                inline.X >= 274 && StatusClear(inline, world)) return inline;

            float right = Width - 92;
            for (int y = 64; y <= 104; y += 4)
                for (float x = right - 94; x >= currencyLeft; x -= 4) {
                    var compact = new Box2(x, y, 94, 30);
                    if (StatusClear(compact, world) && !HeaderOverlaps(compact, wave)) return compact;
                }
            return new Box2(right - 94, 64, 94, 30);
        }

        public void ReadabilityControls(Box2 core, Box2 wave, Box2 towerCount, bool catalysts, List<Box2> output)
        {
            output.Clear();
            output.Add(core);
            output.Add(wave);
            output.Add(towerCount);
            output.Add(new Box2(Width - (catalysts ? 314 : 220), 10, catalysts ? 222 : 128, 50));
            output.Add(Button(MobileAction.Pause));
            output.Add(Button(MobileAction.Speed));
            output.Add(Button(MobileAction.Start));
            output.Add(WaveCallBox());
            output.Add(Button(MobileAction.Skill));
            V2 stick = JoystickCenter;
            output.Add(new Box2(stick.X - 66, stick.Y - 66, 132, 132));
        }

        public Box2 BattleNoticeBox(int length, Box2 wave, IList<Box2> world, IList<Box2> reserved)
        {
            float width = BattleNoticeWidth(length);
            float top = wave.Y + wave.Height + 8;
            float preferred = Math.Max(8, Math.Min(Width - width - 8, wave.Center.X - width / 2));
            Box2 TryRow(float y, float height)
            {
                int steps = (int)Math.Ceiling(Width / 8);
                for (int step = 0; step <= steps; step++)
                    for (int sign = 0; sign < (step == 0 ? 1 : 2); sign++) {
                        float x = preferred + (sign == 0 ? step : -step) * 8;
                        if (x < 8 || x + width > Width - 8) continue;
                        var candidate = new Box2(x, y, width, height);
                        if (!StatusClear(candidate, reserved)) continue;
                        bool clear = true;
                        foreach (Box2 guard in world) {
                            if (OverlapFraction(candidate, guard) < .2f) continue;
                            clear = false;
                            break;
                        }
                        if (clear) return candidate;
                    }
                return new Box2();
            }
            Box2 result = TryRow(top, 40);
            if (result.Width > 0) return result;
            for (float y = top; y + 36 <= Height - 8; y += 8) {
                result = TryRow(y, 36);
                if (result.Width > 0) return result;
            }
            return new Box2();
        }
        public static float BattleNoticeWidth(int length) => Math.Max(220, Math.Min(540, length * 12 + 36));

        public static float OverlapFraction(Box2 cover, Box2 target)
        {
            if (target.Width <= 0 || target.Height <= 0) return 0;
            float width = Math.Max(0, Math.Min(cover.X + cover.Width, target.X + target.Width) - Math.Max(cover.X, target.X));
            float height = Math.Max(0, Math.Min(cover.Y + cover.Height, target.Y + target.Height) - Math.Max(cover.Y, target.Y));
            return width * height / (target.Width * target.Height);
        }
        public bool TryFusionHeader(IList<Box2> towerGuards, out Box2 panel)
        {
            const float width = 640, height = 112, top = 72, margin = 12;
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
            // A rail yard can use both the north and south rows for building.
            // Search the remaining spaces before allowing the panel to cover a pad.
            const float compact=508, compactHeight=96;
            for(int y=(int)top;y+compactHeight+margin<=Height;y+=4)
                for(int x=(int)margin;x+compact+margin<=Width;x+=4) {
                    var candidate=new Box2(x,y,compact,compactHeight);
                    if(StatusClear(candidate,towerGuards)){panel=candidate;return true;}
                }
            panel = new Box2(centered, top, width, height);
            return false;
        }
        // Third guidance row; keep the cancel button's right column clear even on the compact panel.
        public static Box2 FusionFitBox(Box2 panel)
        { return new Box2(panel.X+62,panel.Y+54,Math.Min(420,panel.Width-184),18); }
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
