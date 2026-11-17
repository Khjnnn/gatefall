using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class MobileChecks
    {
        private static readonly List<string> results = new List<string>();
        public static string RunAll()
        {
            results.Clear();
            Check("Notch-safe screen/reference roundtrip", Coordinates);
            Check("Phone and tablet button bounds and minimum reference touch targets", Layouts);
            Check("Arena preserves 16:10 and stays inside safe area", Arena);
            Check("Joystick normalizes diagonal and tracks outside its starting region", Joystick);
            Check("Finger lift releases joystick even outside screen", Release);
            Check("Canceled touch cannot build or leave movement running", Cancel);
            Check("Second finger can construct while first finger moves", Multitouch);
            Check("Second joystick finger does not hijack the first", Ownership);
            Check("Button drag-out/drag-back does not trigger a tap", Drag);
            Check("Button triggers once on release, not on hold", Once);
            Check("Joystick crossing a build button never constructs", Crossing);
            Check("Menu commands depend on run state", Menus);
            Check("State change clears all captured fingers", StateChange);
            Check("Safe-area/rotation change clears captured input", Rotation);
            Check("Lost finger is released without an End event", Missing);
            Check("Pad tap reaches remote construction on the visible board and spends once", Integration);
            Check("Field pad regions take priority over the joystick catch circle", PadPriority);
            Check("Visible joystick wins over overlapping pad and another finger can switch view", StickOverlap);
            Check("Modal recipe commands capture one release and suppress joystick", ModalRecipe);
            Check("Closing menu releases the confirmation finger", ModalRelease);
            Check("Speed button avoids north ingress and top-bar chips, works only during live play", Speed);
            Check("North threat tag and battle notices remain separate", ThreatNoticeLayout);
            Check("Tower menus keep combat running while explicit pause and cards stop it", LiveTowerMenus);
            Check("Wave call stays between thumbs and captures the displayed wave only", WaveCall);
            Check("Ordinary notices avoid tower panels while boss alerts keep their position", TowerNotice);
            Check("Socket panel blocks disabled and empty areas ahead of pads while keeping enabled and outside actions", PanelOcclusion);
            Check("Panel boundary drags cannot activate a pad or button on release", PanelBoundary);
            Check("Clear wave headers retain their original position", ClearWaveHeader);
            Check("North entrance guards clear wave information without covering resource or speed controls", AvoidWaveHeader);
            Check("Compact wave headers retain150 reference units and deterministic placement", CompactWaveHeader);
            Check("Impossible header clearance preserves a safe fallback", FallbackWaveHeader);
            Check("Fusion instructions and fit row leave ingredient pads clear across twelve screens", FusionHeader);
            Check("Core, wave and authored boss HUD geometry preserves tower pads, roads and controls across narrow and wide profiles", HudStatusChecks.Run);
            Check("Start, wave call, speed and pad menus leave entrances, pads and the selected tower clear on every fixed board and twelve screens", HudOcclusionChecks.Run);
            Check("Home tabs, right strip, heart dialog and fake rewarded ads",()=>HomeChecks.Run());
            Check("Shop, supply modal and tower card touch regions on twelve screens",()=>ShopChecks.Run());
            Check("Daily rewards, pass and victory double on twelve screens",()=>DailyUiChecks.Run());
            results.Add("PASS: "+results.Count+" mobile input/layout checks. Hardware touch and Android/iOS binaries are not covered.");
            return string.Join(Environment.NewLine, results.ToArray());
        }
        private static MobileLayout Layout() { return new MobileLayout(2400, 1080, new Box2(90, 24, 2220, 1056)); }
        private static void ClearWaveHeader() {
            var l=new MobileLayout(2400,1080,new Box2(132,0,2268,1080));var b=l.WaveHeader(new Box2[0]);
            Near(b.Center.X,l.Width/2);Near(b.Width,181);Near(b.Y,4);Near(b.Height,52);
        }
        private static bool Hits(Box2 a,Box2 b) {return a.X<b.X+b.Width && b.X<a.X+a.Width && a.Y<b.Y+b.Height && b.Y<a.Y+a.Height;}
        private static void AvoidWaveHeader() {
            var l=new MobileLayout(2400,1080,new Box2(132,0,2268,1080));var r=new[]{new Box2(485,6,121,76)};var b=l.WaveHeader(r);
            Assert(!Hits(b,r[0]));Assert(b.X>=344 && b.X+b.Width<=l.Width-322);Near(r[0].X,485);
        }
        private static void CompactWaveHeader() {
            var l=new MobileLayout(1920,1080,new Box2(105,0,1815,1080));var r=new[]{new Box2(420.46f,36.7f,109.8f,68.8f)};var b=l.WaveHeader(r);
            Near(b.Width,150);Assert(!Hits(b,r[0]));Assert(b.X>=344 && b.X+b.Width<=l.Width-322);
            for(int i=0;i<10;i++){var next=l.WaveHeader(r);Near(next.X,b.X);Near(next.Width,b.Width);}
        }
        private static void FallbackWaveHeader() {
            var l=new MobileLayout(1920,1080,new Box2(105,0,1815,1080));var b=l.WaveHeader(new[]{new Box2(0,0,l.Width,100)});
            Near(b.Center.X,l.Width/2);Near(b.Width,181);Assert(b.X>=344 && b.X+b.Width<=l.Width-322);
        }
        private static void FusionHeader()
        {
            foreach (var screen in HudOcclusionChecks.Screens) {
                var layout=new MobileLayout(screen.Width,screen.Height,new Box2(screen.SafeLeft,0,screen.Width-screen.SafeLeft,screen.SafeHeight));
                Assert(layout.TryFusionHeader(new Box2[0], out Box2 original));
                Near(original.Center.X, layout.Width / 2); Near(original.Y, 72);
                float screenWidth = layout.SafeArea.X + layout.SafeArea.Width;
                float halfHeight = Math.Max(12.6f, 24.8f / (screenWidth / layout.ScreenHeight));
                float world = layout.ScreenHeight / (2 * halfHeight), units = world / layout.Scale;
                foreach (CampaignStage stage in CampaignCatalog.Stages) {
                    var game = new DefenseSimulation(new GameBalance(), 7, new CampaignBattle { Stage = stage });
                    var guards = new List<Box2>();
                    float cameraY = game.ExpandedArena ? 0 : .35f;
                    foreach (V2 p in game.BoardSockets) {
                        V2 center = layout.FromScreen(new V2(screenWidth / 2 + p.X * world,
                            layout.ScreenHeight / 2 + (p.Y - cameraY) * world));
                        guards.Add(new Box2(center.X - 1.2f * units, center.Y - 2.3f * units, 2.4f * units, 3.2f * units));
                    }
                    if (stage.Chapter==3 && stage.LocalNumber>1) {
                        // The live menu reserves its anchor and eligible partner pads.
                        // Test distributed ingredient sets with one anchor and five partners.
                        for(int anchor=0;anchor<guards.Count;anchor++) {
                            var ingredients=new List<Box2>();
                            for(int j=0;j<6;j++)ingredients.Add(guards[(anchor+j*5)%guards.Count]);
                            CheckFusionPlacement(layout,ingredients,stage);
                        }
                    } else CheckFusionPlacement(layout,guards,stage);
                }
                Assert(!layout.TryFusionHeader(new[] { new Box2(0, 0, layout.Width, layout.Height) }, out Box2 fallback));
                Near(fallback.X, original.X); Near(fallback.Y, original.Y);
            }
        }
        private static void CheckFusionPlacement(MobileLayout layout,List<Box2> guards,CampaignStage stage)
        {
            if (!layout.TryFusionHeader(guards, out Box2 panel))
                throw new Exception("No fusion panel placement for "+stage.Id+" at "+layout.Width);
            Assert(panel.X>=0 && panel.Y>=72 && panel.X+panel.Width<=layout.Width && panel.Y+panel.Height<=layout.Height);
            foreach(Box2 guard in guards)Assert(!Hits(panel,guard));
        }
        private static MobileInput Input() { MobileInput input = new MobileInput(Layout()); input.SetState(RunState.Running); return input; }
        private static void Check(string name, Action action) { try { action(); results.Add("PASS " + name); } catch (Exception e) { throw new Exception("FAIL " + name + ": " + e.Message, e); } }
        private static void Assert(bool value) { if (!value) throw new Exception("Assertion failed"); }
        private static void Near(float a, float b) { Assert(Math.Abs(a - b) < 0.005f); }
        private static void Tap(MobileInput input, int id, MobileAction action)
        { V2 p = input.Layout.Button(action).Center; input.Pointer(id, PointerPhase.Begin, p); input.Pointer(id, PointerPhase.End, p); }
        private static void Coordinates()
        {
            MobileLayout layout = Layout();
            V2 screen = layout.ToScreen(new V2(0, 0)); Near(screen.X, 90); Near(screen.Y, 1080);
            V2 point = new V2(563, 312); V2 converted = layout.FromScreen(layout.ToScreen(point)); Near(converted.X, point.X); Near(converted.Y, point.Y);
        }
        private static void Layouts()
        {
            foreach (MobileLayout layout in new[] { Layout(), new MobileLayout(1920,1080,new Box2(0,0,1920,1080)), new MobileLayout(2048,1536,new Box2(0,40,2048,1496)), new MobileLayout(1334,750,new Box2(0,0,1334,750)) })
                foreach (MobileAction action in new[] { MobileAction.Skill, MobileAction.Pause, MobileAction.Speed, MobileAction.Sound, MobileAction.Start, MobileAction.Restart, MobileAction.Resume })
                {
                    Box2 b = layout.Button(action);
                    Assert(b.X >= 0 && b.Y >= 0 && b.X + b.Width <= layout.Width && b.Y + b.Height <= layout.Height);
                    Assert(b.Width >= 54 && b.Height >= 54);
                    V2 p = layout.ToScreen(b.Center); Assert(layout.SafeArea.Contains(p));
                }
        }
        private static void Arena()
        {
            foreach (MobileLayout layout in new[] { Layout(), new MobileLayout(2048,1536,new Box2(0,40,2048,1496)) })
            {
                Box2 arena = layout.Arena; Near(arena.Width / arena.Height, 1.6f);
                Assert(layout.SafeArea.Contains(layout.ToScreen(new V2(arena.X, arena.Y))));
                Assert(layout.SafeArea.Contains(layout.ToScreen(new V2(arena.X + arena.Width, arena.Y + arena.Height))));
            }
        }
        private static void ThreatNoticeLayout()
        {
            foreach (MobileLayout layout in new[] { Layout(), new MobileLayout(1920,1080,new Box2(0,0,1920,1080)), new MobileLayout(2048,1536,new Box2(0,40,2048,1496)) }) {
                Box2 threat = layout.NorthThreatTag();
                foreach (int length in new[] { 1, 20, 60 }) {
                    Box2 notice = layout.NoticeBox(length, false);
                    Assert(threat.Y + threat.Height <= notice.Y);
                    Assert(threat.Y >= 56 && threat.X >= 0 && threat.X + threat.Width <= layout.Width);
                    Assert(notice.X >= 0 && notice.X + notice.Width <= layout.Width);
                }
            }
        }
        private static void Speed()
        {
            MobileInput input = Input();
            Box2 speed = input.Layout.Button(MobileAction.Speed);
            Box2 map = new Box2(input.Layout.Width - 176, 78, 160, 46);
            Assert(speed.X + speed.Width < map.X);
            foreach (MobileLayout layout in new[] { Layout(), new MobileLayout(1920,1080,new Box2(0,0,1920,1080)), new MobileLayout(2048,1536,new Box2(0,40,2048,1496)) }) {
                Box2 b=layout.Button(MobileAction.Speed);
                Assert(b.X >= 274 && b.X+b.Width <= layout.Width/2-90); // after barrier chip, before wave header
                Assert(b.Y+b.Height<=62); // boss warning below the top bar
            }
            float[] widths={2400,1920,1280,2048}, heights={1080,1080,800,1536};
            Box2[] safes={new Box2(136,0,2264,1080),new Box2(0,0,1920,1080),new Box2(0,0,1280,800),new Box2(0,40,2048,1496)};
            V2 gate=BattlefieldLayouts.Get(CampaignCatalog.Stages[9]).Roads[1][0];
            for(int i=0;i<widths.Length;i++) {
                var layout=new MobileLayout(widths[i],heights[i],safes[i]);
                float ortho=Math.Max(12.6f,24.8f/(widths[i]/heights[i])), pixels=heights[i]/(2*ortho);
                V2 center=layout.FromScreen(new V2(widths[i]/2+gate.X*pixels,heights[i]/2+(gate.Y-.35f)*pixels));
                // Full V6 rift quad (220x140 / PPU58), at maximum warning pulse 1.55.
                float scale=pixels/layout.Scale*1.55f;
                Box2 rift=new Box2(center.X-220f/58*.5f*scale,center.Y-140f/58*.53f*scale,220f/58*scale,140f/58*scale);
                Assert(!Intersects(rift,layout.Button(MobileAction.Speed)));
                if(i==0)Assert(Intersects(rift,new Box2(layout.Width/2+96,8,62,54))); // reproduce old overlap
            }
            Tap(input, 1, MobileAction.Speed); Assert(input.Actions.Count == 1 && input.Actions[0] == MobileAction.Speed);
            input.SetState(RunState.Ready); Tap(input, 2, MobileAction.Speed); Assert(input.Actions.Count == 0);
            input.SetState(RunState.Paused); Tap(input, 3, MobileAction.Speed); Assert(input.Actions.Count == 0);
            input.SetState(RunState.Running); input.Modal = true; Tap(input, 4, MobileAction.Speed); Assert(input.Actions.Count == 0);
        }
        private static bool Intersects(Box2 a,Box2 b)
        { return a.X<b.X+b.Width && b.X<a.X+a.Width && a.Y<b.Y+b.Height && b.Y<a.Y+a.Height; }
        private static void WaveCall()
        {
            foreach(MobileLayout layout in new[]{Layout(),new MobileLayout(1280,960,new Box2(0,0,1280,960)),new MobileLayout(1920,1080,new Box2(0,0,1920,1080))}) {
                Box2 b=layout.WaveCallBox(),skill=layout.Button(MobileAction.Skill);
                Assert(b.X>=layout.JoystickCenter.X+MobileLayout.JoystickCatch && b.X+b.Width<skill.X && b.Y>=0 && b.Y+b.Height<=layout.Height);
                Assert(b.Width>=54 && b.Height>=54);
            }
            var input=Input();Box2 box=input.Layout.WaveCallBox();int id=MobileLayout.WaveCallActionBase+1;
            input.Regions.Add(new TouchRegion(id,box));input.Regions.Add(new TouchRegion(400,box));
            input.Pointer(1,PointerPhase.Begin,input.Layout.JoystickCenter);input.Pointer(1,PointerPhase.Move,input.Layout.JoystickCenter+new V2(30,0));
            input.Pointer(2,PointerPhase.Begin,box.Center);input.Pointer(2,PointerPhase.End,box.Center);input.Pointer(2,PointerPhase.End,box.Center);
            Assert(input.JoystickFinger==1 && input.Movement.X>0 && input.Actions.Count==1 && (int)input.Actions[0]==id);
            input.Actions.Clear();input.Pointer(3,PointerPhase.Begin,box.Center);input.Regions.Clear();input.Regions.Add(new TouchRegion(id+1,box));input.Pointer(3,PointerPhase.End,box.Center);Assert(input.Actions.Count==0);
            input.Regions.Clear();input.Modal=true;input.Pointer(4,PointerPhase.Begin,box.Center);input.Pointer(4,PointerPhase.End,box.Center);Assert(input.Actions.Count==0);
        }
        private static void TowerNotice()
        {
            foreach(var layout in new[]{Layout(),new MobileLayout(1280,960,new Box2(0,0,1280,960)),new MobileLayout(1920,1080,new Box2(0,0,1920,1080))})foreach(bool ready in new[]{false,true})foreach(float y in new[]{70f,104f,140f,layout.Height-232}) {
                var panel=new Box2(layout.Width/2-240,y,480,222);var b=layout.NoticeBox(30,ready,panel,false);
                Assert(b.X>=0 && b.X+b.Width<=layout.Width && b.Y>=62 && b.Y+b.Height<=layout.Height);
                Assert(b.X+b.Width<=panel.X || panel.X+panel.Width<=b.X || b.Y+b.Height<=panel.Y || panel.Y+panel.Height<=b.Y);
                var critical=layout.NoticeBox(30,ready,panel,true);var original=layout.NoticeBox(30,ready);Near(critical.Y,original.Y);
            }
        }
        private static void LiveTowerMenus()
        {
            var game = new DefenseSimulation(new GameBalance { SpawnEnemies = false, OfferCards = true }, 1, new CampaignBattle());
            game.Start();
            BattleMenuTime.Open(game, false); game.Tick(2, new V2()); Near((float)game.Elapsed, 2);
            BattleMenuTime.Close(game, false); game.Tick(2, new V2()); Near((float)game.Elapsed, 4);
            BattleMenuTime.Open(game, true); game.Tick(3, new V2()); Near((float)game.Elapsed, 4);
            BattleMenuTime.Close(game, true); game.Tick(2, new V2()); Near((float)game.Elapsed, 6);
            BattleMenuTime.Open(game, false); game.Tick(24.2f, new V2());
            Assert(game.AwaitingCard && game.State == RunState.Paused);
            float elapsed = (float)game.Elapsed;
            BattleMenuTime.Close(game, false); BattleMenuTime.Close(game, true); game.Tick(2, new V2());
            Assert(game.AwaitingCard && game.State == RunState.Paused); Near((float)game.Elapsed, elapsed);
        }
        private static void Joystick()
        {
            MobileInput input = Input(); V2 origin = input.Layout.JoystickCenter;
            input.Pointer(77,PointerPhase.Begin,origin); input.Pointer(77,PointerPhase.Move,origin + new V2(100,-100));
            Near(input.Movement.Length,1); Assert(input.Movement.X > 0 && input.Movement.Y > 0);
        }
        private static void Release()
        {
            MobileInput input = Input(); input.Pointer(7,PointerPhase.Begin,input.Layout.JoystickCenter);
            input.Pointer(7,PointerPhase.Move,new V2(-100,1000)); input.Pointer(7,PointerPhase.End,new V2(-100,1000));
            Assert(input.JoystickFinger == int.MinValue && input.Movement.Length == 0);
        }
        private static void Cancel()
        {
            MobileInput input = Input(); V2 build = input.Layout.Button(MobileAction.Skill).Center;
            input.Pointer(1,PointerPhase.Begin,build); input.Pointer(1,PointerPhase.Cancel,build);
            input.Pointer(2,PointerPhase.Begin,input.Layout.JoystickCenter + new V2(30,0)); input.Pointer(2,PointerPhase.Cancel,new V2());
            Assert(input.Actions.Count == 0 && input.Movement.Length == 0);
        }
        private static void Multitouch()
        {
            MobileInput input = Input(); input.Pointer(21,PointerPhase.Begin,input.Layout.JoystickCenter + new V2(30,0));
            Tap(input,83,MobileAction.Skill); Assert(input.Movement.X > 0 && input.JoystickFinger == 21 && input.Actions[0] == MobileAction.Skill);
        }
        private static void Ownership()
        {
            MobileInput input = Input(); input.Pointer(6,PointerPhase.Begin,input.Layout.JoystickCenter + new V2(30,0));
            input.Pointer(4,PointerPhase.Begin,input.Layout.JoystickCenter + new V2(-30,0)); input.Pointer(4,PointerPhase.End,new V2());
            Assert(input.JoystickFinger == 6 && input.Movement.X > 0);
        }
        private static void Drag()
        {
            MobileInput input = Input(); V2 p = input.Layout.Button(MobileAction.Skill).Center;
            input.Pointer(1,PointerPhase.Begin,p); input.Pointer(1,PointerPhase.Move,p + new V2(200,0)); input.Pointer(1,PointerPhase.End,p);
            Assert(input.Actions.Count == 0);
        }
        private static void Once()
        {
            MobileInput input = Input(); V2 p = input.Layout.Button(MobileAction.Skill).Center;
            input.Pointer(1,PointerPhase.Begin,p); for(int i=0;i<100;i++) input.Pointer(1,PointerPhase.Move,p);
            Assert(input.Actions.Count == 0); input.Pointer(1,PointerPhase.End,p); input.Pointer(1,PointerPhase.End,p); Assert(input.Actions.Count == 1);
        }
        private static void Crossing()
        {
            MobileInput input = Input(); input.Pointer(1,PointerPhase.Begin,input.Layout.JoystickCenter);
            V2 p = input.Layout.Button(MobileAction.Skill).Center; input.Pointer(1,PointerPhase.Move,p); input.Pointer(1,PointerPhase.End,p); Assert(input.Actions.Count == 0);
        }
        private static void Menus()
        {
            MobileInput input = Input(); input.SetState(RunState.Ready); Tap(input,1,MobileAction.Skill); Assert(input.Actions.Count == 0);
            Tap(input,2,MobileAction.Start); Assert(input.Actions[0] == MobileAction.Start);
            input.SetState(RunState.Paused); Tap(input,2,MobileAction.Resume); Assert(input.Actions[0] == MobileAction.Resume);
            input.SetState(RunState.Lost); Tap(input,1,MobileAction.Restart); Assert(input.Actions[0] == MobileAction.Restart);
        }
        private static void StateChange()
        {
            MobileInput input = Input(); input.Pointer(1,PointerPhase.Begin,input.Layout.JoystickCenter + new V2(30,0));
            input.SetState(RunState.Paused); input.SetState(RunState.Running); input.Pointer(1,PointerPhase.Move,new V2(200,400)); Assert(input.Movement.Length == 0);
        }
        private static void Rotation()
        {
            MobileInput input = Input(); V2 build = input.Layout.Button(MobileAction.Skill).Center;
            input.Pointer(2,PointerPhase.Begin,build); input.Pointer(1,PointerPhase.Begin,input.Layout.JoystickCenter + new V2(30,0));
            input.SetLayout(new MobileLayout(1920,1080,new Box2(0,0,1850,1080))); input.Pointer(2,PointerPhase.End,build);
            Assert(input.Actions.Count == 0 && input.Movement.Length == 0);
        }
        private static void Missing()
        {
            MobileInput input = Input(); input.Pointer(1,PointerPhase.Begin,input.Layout.JoystickCenter + new V2(30,0));
            input.ReleaseMissingJoystick(false); Assert(input.Movement.Length == 0 && input.JoystickFinger == int.MinValue);
        }
        private static void Integration()
        {
            // v0.5: the whole board is on screen, so the HUD registers each pad as a touch region (400+socket).
            MobileInput input = Input(); DefenseSimulation game = new DefenseSimulation(new GameBalance { SpawnEnemies = false },1,new CampaignBattle()); game.Start();
            Box2 pad = new Box2(700,150,80,60); input.Regions.Add(new TouchRegion(400+5,pad));
            input.Pointer(9,PointerPhase.Begin,pad.Center); input.Pointer(9,PointerPhase.End,pad.Center); input.Pointer(9,PointerPhase.End,pad.Center);
            string reason; Assert(input.Actions.Count == 1);
            foreach(MobileAction action in input.Actions) Assert(game.TryBuild((int)action-400,TowerKind.Autocannon,"tap-1",out reason));
            Assert(!game.TryBuild(5,TowerKind.Autocannon,"tap-1",out reason));
            Assert(game.Towers.Count == 1 && game.Credits == 100 && V2.Distance(game.PlayerPosition,game.BoardSockets[5]) > DefenseSimulation.BuildDistance);
        }
        private static void PadPriority()
        {
            MobileInput input = Input(); V2 inside = input.Layout.JoystickCenter + new V2(70,-40);
            Assert(V2.Distance(inside,input.Layout.JoystickCenter) <= MobileLayout.JoystickCatch);
            input.Regions.Add(new TouchRegion(402,new Box2(inside.X-30,inside.Y-30,60,60)));
            input.Pointer(3,PointerPhase.Begin,inside); Assert(input.JoystickFinger == int.MinValue);
            input.Pointer(3,PointerPhase.End,inside); Assert(input.Actions.Count == 1 && (int)input.Actions[0] == 402);
            input.Pointer(4,PointerPhase.Begin,input.Layout.JoystickCenter + new V2(-100,10)); Assert(input.JoystickFinger == 4);
        }
        private static void StickOverlap()
        {
            MobileInput input=Input(); V2 stick=input.Layout.JoystickCenter;
            input.Regions.Add(new TouchRegion(402,new Box2(stick.X-30,stick.Y-30,60,60)));
            var map=new Box2(800,100,80,60);input.Regions.Add(new TouchRegion((int)MobileAction.Map,map));
            input.Pointer(1,PointerPhase.Begin,stick); input.Pointer(1,PointerPhase.Move,stick+new V2(30,0));
            input.Pointer(2,PointerPhase.Begin,map.Center);input.Pointer(2,PointerPhase.End,map.Center);
            Assert(input.JoystickFinger==1 && input.Movement.X>0 && input.Actions.Count==1 && input.Actions[0]==MobileAction.Map);
        }
        private static void ModalRecipe()
        {
            MobileInput input=Input(); input.Modal=true; Box2 box=new Box2(400,300,250,60); input.Regions.Add(new TouchRegion(315,box));
            input.Pointer(1,PointerPhase.Begin,input.Layout.JoystickCenter); Assert(input.JoystickFinger==int.MinValue);
            input.Pointer(2,PointerPhase.Begin,box.Center); input.Pointer(2,PointerPhase.End,box.Center); input.Pointer(2,PointerPhase.End,box.Center);
            Assert(input.Actions.Count==1 && (int)input.Actions[0]==315);
        }
        private static void ModalRelease()
        {
            MobileInput input=Input(); input.Modal=true; Box2 box=input.Layout.Button(MobileAction.Skill); input.Regions.Add(new TouchRegion(315,box));
            input.Pointer(9,PointerPhase.Begin,box.Center); input.Reset(); input.Modal=false; input.Regions.Clear(); input.Pointer(9,PointerPhase.End,box.Center);
            Assert(input.Actions.Count==0 && input.Movement.Length==0);
        }
        private static MobileInput PanelInput(bool block,RunState state)
        {
            var input=Input();input.SetState(state);input.Modal=true;
            input.Regions.Add(new TouchRegion(210,new Box2(120,236,140,124)));
            if(block)input.Regions.Add(new TouchRegion(0,new Box2(100,150,500,222)));
            input.Regions.Add(new TouchRegion(402,new Box2(270,260,100,90)));
            input.Regions.Add(new TouchRegion(403,new Box2(450,150,100,50)));
            input.Regions.Add(new TouchRegion(404,new Box2(760,350,60,60)));
            input.Regions.Add(new TouchRegion(405,new Box2(120,236,140,124)));
            input.Regions.Add(new TouchRegion(100,new Box2(0,0,input.Layout.Width,input.Layout.Height)));
            return input;
        }
        private static void At(MobileInput input,V2 point)
        {input.Pointer(9,PointerPhase.Begin,point);input.Pointer(9,PointerPhase.End,point);}
        private static void PanelOcclusion()
        {
            var old=PanelInput(false,RunState.Running);At(old,new V2(320,300));Assert((int)old.Actions[0]==402);
            foreach(var state in new[]{RunState.Ready,RunState.Running}) {
                var input=PanelInput(true,state);
                At(input,new V2(320,300));At(input,new V2(500,170));Assert(input.Actions.Count==0);
                At(input,new V2(190,298));At(input,new V2(790,380));At(input,new V2(700,200));
                Assert(input.Actions.Count==3 && (int)input.Actions[0]==210 && (int)input.Actions[1]==404 && (int)input.Actions[2]==100);
                Assert(input.JoystickFinger==int.MinValue && input.Movement.Length==0);
            }
        }
        private static void PanelBoundary()
        {
            var input=PanelInput(true,RunState.Running);
            input.Pointer(1,PointerPhase.Begin,new V2(320,300));input.Pointer(1,PointerPhase.End,new V2(790,380));
            input.Pointer(2,PointerPhase.Begin,new V2(790,380));input.Pointer(2,PointerPhase.End,new V2(500,170));
            input.Pointer(3,PointerPhase.Begin,new V2(190,298));input.Pointer(3,PointerPhase.End,new V2(320,300));
            input.Pointer(4,PointerPhase.Begin,new V2(320,300));input.Pointer(4,PointerPhase.End,new V2(190,298));
            Assert(input.Actions.Count==0);
        }
    }
}

namespace Gatefall.Tests
{
    public static class HomeChecks
    {
        static void Require(bool value,string message) {if(!value)throw new Exception("Home: "+message);}
        public static string Run()
        {
            int hits=HudOcclusionChecks.HomeGeometry();
            foreach(var screen in HudOcclusionChecks.Screens) {
                var layout=new MobileLayout(screen.Width,screen.Height,new Box2(screen.SafeLeft,0,screen.Width-screen.SafeLeft,screen.SafeHeight));
                var input=new MobileInput(layout);input.SetState(RunState.Ready);input.Modal=true;
                var regions=new List<TouchRegion>();
                for(int i=0;i<Gatefall.HomeMenu.Tabs.Length;i++)regions.Add(new TouchRegion(Gatefall.HomeMenu.Tabs[i].Action,layout.HomeNavigation(i,Gatefall.HomeMenu.Tabs.Length)));
                for(int i=0;i<Gatefall.HomeMenu.Right.Length;i++)regions.Add(new TouchRegion(Gatefall.HomeMenu.Right[i].Action,layout.HomeRight(i,Gatefall.HomeMenu.Right.Length)));
                regions.Add(new TouchRegion(600,layout.HomePanel()));
                regions.Add(new TouchRegion(880,layout.HomePass()));
                int[] topActions={602,0,684,0,0,685,604};
                for(int i=0;i<topActions.Length;i++)if(topActions[i]!=0)regions.Add(new TouchRegion(topActions[i],layout.HomeTop(i)));
                foreach(var region in regions) {
                    input.Reset();input.Modal=true;input.Regions.Clear();input.Regions.AddRange(regions);
                    input.Pointer(7,PointerPhase.Begin,region.Box.Center);input.Pointer(7,PointerPhase.End,region.Box.Center);
                    Require(input.Actions.Count==1 && input.Actions[0]==region.Action,"wrong home action");hits++;
                }
                // A modal replaces the home regions entirely, including the large map panel.
                input.Reset();input.Regions.Clear();input.Modal=true;
                for(int i=0;i<3;i++)input.Regions.Add(new TouchRegion(696+i,layout.HeartDialogButton(i)));
                for(int i=0;i<3;i++) {
                    input.Actions.Clear();V2 p=layout.HeartDialogButton(i).Center;
                    input.Pointer(8,PointerPhase.Begin,p);input.Pointer(8,PointerPhase.End,p);
                    Require(input.Actions.Count==1 && (int)input.Actions[0]==696+i,"wrong dialog action");hits++;
                }
                // Expanded data lists use the same router, including the folded fourth 'more' entry.
                regions.Clear();
                for(int i=0;i<7;i++)regions.Add(new TouchRegion(900+i,layout.HomeNavigation(i,7)));
                for(int i=0;i<5;i++)regions.Add(new TouchRegion(910+i,layout.HomeRight(i,5,true)));
                foreach(var region in regions) {
                    input.Reset();input.Regions.Clear();input.Regions.AddRange(regions);input.Modal=true;
                    input.Pointer(9,PointerPhase.Begin,region.Box.Center);input.Pointer(9,PointerPhase.End,region.Box.Center);
                    Require(input.Actions.Count==1 && input.Actions[0]==region.Action,"expanded list action");hits++;
                }
                for(int first=0;first<=2;first++) {
                    var boxes=new List<Box2>{layout.HomeTabArrow(false),layout.HomeTabArrow(true)};
                    for(int i=first;i<first+7;i++)boxes.Add(layout.HomeNavigation(i,9,first));
                    for(int i=0;i<boxes.Count;i++)for(int j=i+1;j<boxes.Count;j++)
                        Require(!Intersects(boxes[i],boxes[j]),"scroll arrows overlap tabs");
                    Require(layout.HomeNavigation(first==0?8:0,9,first).Width==0,"off-page tab exposed");
                }
            }
            Require(Gatefall.HomeMenu.Tabs.Length==5 && Gatefall.HomeMenu.Tabs[2].Action==606,"center launch tab");
            Require(Gatefall.HomeMenu.Tabs[0].UnlockStage==2 && Gatefall.HomeMenu.Tabs[1].UnlockStage==2,"shop/card unlock gates");
            int rewarded=0,failed=0;var ads=new Gatefall.FakeRewardedAdService();
            ads.Show("heart-shortage",()=>rewarded++,()=>failed++);
            Require(rewarded==1 && failed==0,"default ad success");
            ads.Succeeds=false;ads.Show("heart-shortage",()=>rewarded++,()=>failed++);
            ads.IsReady=false;ads.Succeeds=true;ads.Show("heart-shortage",()=>rewarded++,()=>failed++);
            Require(rewarded==1 && failed==2,"failed/unready ads must not reward");
            long utc=10000;int writes=0;
            var profile=new CampaignProfile{Hearts=0,ExtraHearts=0,CrystalsFree=30,HeartAnchorUtc=utc,LastSeenUtc=utc};
            var service=new CampaignService(profile,p=>writes++,()=>utc,()=>1);
            string id;Require(!service.Begin(0,false,out id) && service.BeginReason(0,false)=="하트 부족" && writes==0,"empty launch");
            Require(service.RefillHeartByAd() && service.Begin(0,false,out id) && service.Profile.Hearts==0 && writes==2,"reward then launch");
            Require(service.Resume(out id) && writes==2,"same run resumes without spending");
            Require(service.RefillHeartsByCrystals() && service.Begin(0,false,out id) && service.Profile.Hearts==4 && service.Profile.CrystalsFree==0,"crystals then new attempt");
            return "PASS: home geometry on 12 screens; 5/7 tabs, 3/5 right entries; "+hits+" bounds/touch checks; ad success/failure/unready and refill-to-launch.";
        }
        static bool Intersects(Box2 a,Box2 b) {return a.X<b.X+b.Width && b.X<a.X+a.Width && a.Y<b.Y+b.Height && b.Y<a.Y+a.Height;}
    }
}

namespace Gatefall.Tests
{
    public static class DailyUiChecks
    {
        static void Require(bool ok,string message){if(!ok)throw new Exception("Daily UI: "+message);}
        public static string Run()
        {
            int bounds=HudOcclusionChecks.DailyGeometry(),hits=0;
            foreach(var screen in HudOcclusionChecks.Screens) {
                var l=new MobileLayout(screen.Width,screen.Height,new Box2(screen.SafeLeft,0,screen.Width-screen.SafeLeft,screen.SafeHeight));
                var back=new TouchRegion(603,l.DailyBack());
                var groups=new List<List<TouchRegion>>();
                groups.Add(new List<TouchRegion>{back,new TouchRegion(881,l.DailyPrimary()),new TouchRegion(882,l.RouletteOdds())});
                groups.Add(new List<TouchRegion>{back,new TouchRegion(883,l.DailyPrimary())});
                var missions=new List<TouchRegion>{back};for(int i=0;i<6;i++)missions.Add(new TouchRegion(900+i,l.MissionClaim(i)));groups.Add(missions);
                for(int first=0;first<30;first++) {
                    var pass=new List<TouchRegion>{back,new TouchRegion(884,l.PassCap()),new TouchRegion(885,l.DailyScroll(false)),new TouchRegion(886,l.DailyScroll(true))};
                    for(int i=0;i<l.DailyVisibleRows() && first+i<30;i++) {
                        pass.Add(new TouchRegion(920+first+i,l.PassClaim(i,false)));pass.Add(new TouchRegion(950+first+i,l.PassClaim(i,true)));
                    }
                    groups.Add(pass);
                }
                groups.Add(new List<TouchRegion>{back,new TouchRegion(885,l.DailyScroll(false)),new TouchRegion(886,l.DailyScroll(true))});
                Box2 r=l.VictoryPanel();float by=r.Y+r.Height-82;
                groups.Add(new List<TouchRegion>{new TouchRegion(887,l.VictoryDouble()),new TouchRegion(600,new Box2(r.X+24,by,150,60)),new TouchRegion((int)MobileAction.Restart,new Box2(r.X+184,by,150,60)),new TouchRegion(607,new Box2(r.X+344,by,232,60))});
                foreach(var group in groups)foreach(var region in group) {
                    Box2 b=region.Box;Require(b.Width>=48 && b.Height>=48,"small target");
                    foreach(var other in group)if(other.Action!=region.Action)Require(!HudOcclusionChecks.Hits(b,other.Box),"overlapping target");
                    var input=new MobileInput(l);input.Modal=true;input.Regions.AddRange(group);
                    V2 p=l.FromScreen(l.ToScreen(b.Center));input.Pointer(1,PointerPhase.Begin,p);input.Pointer(1,PointerPhase.End,p);input.Pointer(1,PointerPhase.End,p);
                    Require(input.Actions.Count==1 && input.Actions[0]==region.Action && input.Movement.Length==0,"wrong or duplicate action");hits++;
                    input.Reset();input.Pointer(2,PointerPhase.Begin,p);input.Pointer(2,PointerPhase.Cancel,p);Require(input.Actions.Count==0,"canceled claim");
                }
            }
            var d=new DailyState{LocalDay=7,Season=1};
            for(int today=0;today<7;today++) {
                d.AttendanceDay=6;d.AttendancePosition=today;
                for(int i=0;i<7;i++)Require(Gatefall.DailyLabels.AttendanceState(d,i)==(i<today?"수령 완료":i==today?"오늘":"잠김"),"attendance before claim");
                d.AttendanceDay=7;d.AttendancePosition=(today+1)%7;
                for(int i=0;i<7;i++)Require(Gatefall.DailyLabels.AttendanceState(d,i)==(i<=today?"수령 완료":"잠김"),"attendance after claim/wrap");
            }
            Require(Gatefall.DailyLabels.DaysLeft(d)==21,"season days");
            decimal sum=0;foreach(var row in DailyCatalog.RouletteDisclosure()){sum+=row.Percent;Require(Gatefall.DailyLabels.Reward(row.Reward)!="","roulette label");}
            Require(sum==100m && DailyCatalog.Missions().Length==6 && DailyCatalog.PassRewards().Length==30,"catalog rows");
            return "PASS: daily/attendance/roulette/pass/victory geometry on12 safe-area screens; "+bounds+" bounds and "+hits+" touch checks; all30 pass offsets, cancellation, attendance wrap and exact catalog labels.";
        }
    }
    public static class ShopChecks
    {
        static void Require(bool ok,string message) {if(!ok)throw new Exception("Shop: "+message);}
        public static string Run()
        {
            int checks=HudOcclusionChecks.ShopGeometry(),hits=0;
            foreach(var screen in HudOcclusionChecks.Screens) {
                var l=new MobileLayout(screen.Width,screen.Height,new Box2(screen.SafeLeft,0,screen.Width-screen.SafeLeft,screen.SafeHeight));
                var groups=new List<List<TouchRegion>>();var daily=new List<TouchRegion>();
                for(int i=0;i<5;i++)daily.Add(new TouchRegion(800+i,l.ShopTab(i)));
                for(int i=0;i<6;i++)daily.Add(new TouchRegion(810+i,l.ShopSlotButton(i)));
                daily.Add(new TouchRegion(808,l.ShopFooter()));groups.Add(daily);
                groups.Add(new List<TouchRegion>{new TouchRegion(805,l.ShopProductButton(0)),new TouchRegion(806,l.ShopProductButton(1)),new TouchRegion(807,l.ShopFooter())});
                groups.Add(new List<TouchRegion>{new TouchRegion(819,l.SupplyClose()),new TouchRegion(820,l.SupplyScroll(false)),new TouchRegion(821,l.SupplyScroll(true)),new TouchRegion(822,l.SupplyKindTab(0)),new TouchRegion(823,l.SupplyKindTab(1))});
                var cards=new List<TouchRegion>{new TouchRegion(835,l.ShopTab(0,2)),new TouchRegion(836,l.ShopTab(1,2)),new TouchRegion(837,l.TowerCardPage(false)),new TouchRegion(838,l.TowerCardPage(true))};
                for(int i=0;i<6;i++)cards.Add(new TouchRegion(860+i,l.TowerGradeButton(i)));groups.Add(cards);
                groups.Add(new List<TouchRegion>{new TouchRegion(809,new Box2(48, l.ShopProduct(0).Y+l.ShopProduct(0).Height-76,l.Width-96,52))});
                foreach(var group in groups)foreach(var region in group) {
                    Require(region.Box.Width>=48 && region.Box.Height>=48,"small target");
                    var input=new MobileInput(l);input.Modal=true;input.Regions.AddRange(group);
                    V2 point=l.FromScreen(l.ToScreen(region.Box.Center));
                    input.Pointer(1,PointerPhase.Begin,point);input.Pointer(1,PointerPhase.End,point);input.Pointer(1,PointerPhase.End,point);
                    Require(input.Actions.Count==1 && input.Actions[0]==region.Action && input.Movement.Length==0,"wrong action/duplicate/joystick");hits++;
                    input.Reset();input.Pointer(2,PointerPhase.Begin,point);input.Pointer(2,PointerPhase.Cancel,point);Require(input.Actions.Count==0,"cancel fired");
                }
                var modal=new MobileInput(l);modal.Modal=true;
                modal.Regions.Add(new TouchRegion(820,l.SupplyScroll(false)));modal.Regions.Add(new TouchRegion(821,l.SupplyScroll(true)));
                modal.Regions.Add(new TouchRegion(819,l.SupplyPanel()));
                modal.Pointer(3,PointerPhase.Begin,l.SupplyScroll(true).Center);modal.Pointer(3,PointerPhase.End,l.SupplyScroll(true).Center);
                Require(modal.Actions.Count==1 && (int)modal.Actions[0]==821,"result background stole scroll");
                modal.Reset();modal.Pointer(3,PointerPhase.Begin,l.SupplyRow(0,false).Center);modal.Pointer(3,PointerPhase.End,l.SupplyRow(0,false).Center);
                Require(modal.Actions.Count==1 && (int)modal.Actions[0]==819,"second tap did not close");
            }
            return "PASS: shop/cards/results/odds on 12 safe-area screens; "+checks+" geometry checks and "+hits+" touch targets, cancellation and modal priority.";
        }
    }
}
