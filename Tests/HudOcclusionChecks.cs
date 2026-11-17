using System;
using System.Collections.Generic;
using System.Linq;
using Gatefall.Core;

namespace Gatefall.Tests
{
    // Projects every implemented board onto phone/tablet profiles with the same camera rule as the game and
    // reports which fixed controls or context panels cover an entrance, a pad or a road.
    public static class HudOcclusionChecks
    {
        public sealed class Screen
        {
            public int Width, Height, SafeLeft, SafeHeight;
            public override string ToString() { return Width+"x"+Height+" safe "+SafeLeft+",0,"+(Width-SafeLeft)+","+SafeHeight; }
        }
        public static readonly Screen[] Screens = {
            new Screen{Width=1280,Height=720,SafeLeft=0,SafeHeight=652}, new Screen{Width=1280,Height=720,SafeLeft=0,SafeHeight=639},
            new Screen{Width=1280,Height=720,SafeLeft=0,SafeHeight=720}, new Screen{Width=1280,Height=720,SafeLeft=45,SafeHeight=720},
            new Screen{Width=1280,Height=576,SafeLeft=0,SafeHeight=495}, new Screen{Width=1280,Height=800,SafeLeft=0,SafeHeight=710},
            new Screen{Width=1280,Height=800,SafeLeft=0,SafeHeight=800}, new Screen{Width=1920,Height=1080,SafeLeft=0,SafeHeight=959},
            new Screen{Width=1920,Height=1080,SafeLeft=68,SafeHeight=1080}, new Screen{Width=2400,Height=1080,SafeLeft=0,SafeHeight=929},
            new Screen{Width=2400,Height=1080,SafeLeft=136,SafeHeight=1080}, new Screen{Width=2048,Height=1536,SafeLeft=0,SafeHeight=1496}
        };

        public sealed class Board
        {
            public MobileLayout Layout;
            public float Unit;
            public Box2[] Pads, Rifts, Roads;
            public V2[] PadCenters;
            public List<Box2> Guards = new List<Box2>();
        }

        public static Board Project(CampaignStage stage, Screen screen, int loadout = 0)
        {
            var layout = new MobileLayout(screen.Width, screen.Height, new Box2(screen.SafeLeft, 0, screen.Width-screen.SafeLeft, screen.SafeHeight));
            var battle = new CampaignBattle { Stage = stage };
            if (loadout != 0) battle.Loadout = loadout;
            var game = new DefenseSimulation(new GameBalance(), 7, battle);
            float half = Math.Max(12.6f, 24.8f/(screen.Width/(float)screen.Height));
            float world = screen.Height/(2*half), unit = world/layout.Scale, camera = game.ExpandedArena ? 0 : .35f;
            Func<V2,V2> project = p => layout.FromScreen(new V2(screen.Width/2f+p.X*world, screen.Height/2f+(p.Y-camera)*world));
            var board = new Board { Layout = layout, Unit = unit };
            board.PadCenters = game.BoardSockets.Select(project).ToArray();
            board.Pads = board.PadCenters.Select(p => new Box2(p.X-1.2f*unit, p.Y-2.3f*unit, 2.4f*unit, 3.2f*unit)).ToArray();
            board.Rifts = game.OpenLanes.Select(lane => project(game.GatePositions[lane]))
                .Select(p => new Box2(p.X-2.55f*unit, p.Y-1.6f*unit, 5.1f*unit, 3.2f*unit)).ToArray();
            var roads = new List<Box2>();
            foreach (int lane in game.OpenLanes) {
                V2[] road = game.Roads[lane];
                for (int i = 1; i < road.Length; i++)
                    roads.Add(MobileLayout.RoadGuard(project(road[i-1]), project(road[i]),
                        BattlefieldLayouts.RoadHalfWidth*unit, BattlefieldLayouts.RoadHalfWidth*unit, BattlefieldLayouts.RoadHalfWidth*unit));
            }
            board.Roads = roads.ToArray();
            // Same order and content as MobileHud.ResolveCoreStatus: pads, then entrances.
            board.Guards.AddRange(board.Pads); board.Guards.AddRange(board.Rifts);
            return board;
        }

        // Shared edges are not overlaps: panels are placed exactly against the pad's touch box.
        public static bool Hits(Box2 a, Box2 b) { return Covered(a, b) > 0; }
        // Fraction of the target hidden by the control. Under 2% is float noise or a shared edge.
        public static float Covered(Box2 control, Box2 target)
        {
            float w = Math.Min(control.X+control.Width, target.X+target.Width)-Math.Max(control.X, target.X);
            float h = Math.Min(control.Y+control.Height, target.Y+target.Height)-Math.Max(control.Y, target.Y);
            if (w <= 0 || h <= 0) return 0;
            float f = w*h/(target.Width*target.Height);
            return f < .02f ? 0 : f;
        }
        public const float Major = .2f;
        static int Count(Box2 box, Box2[] targets) { return targets.Count(t => Hits(box, t)); }
        static int CountMajor(Box2 box, Box2[] targets) { return targets.Count(t => Covered(box, t) >= Major); }

        // The placement shipped through v0.32.0, kept here to measure what changed.
        public static Box2 LegacyPanel(MobileLayout l, V2 pad, float unit, float w, float h)
        {
            float x = Math.Max(12, Math.Min(l.Width-w-12, pad.X-w/2));
            float y = pad.Y > l.Height*.5f ? pad.Y-h-2.9f*unit : pad.Y+.9f*unit;
            return new Box2(x, Math.Max(70, Math.Min(l.Height-h-10, y)), w, h);
        }
        public static Box2 LegacyStart(MobileLayout l) { return new Box2(l.Width/2-160, l.Height-104, 320, 76); }
        public static Box2 LegacyCall(MobileLayout l) { return new Box2(l.Width/2-190, l.Height-112, 380, 84); }
        public static Box2 LegacySpeed() { return new Box2(274, 8, 62, 54); }

        public sealed class Row
        {
            public string Stage, Screen;
            public int Chapter;
            // Any = control touches the target's box; Major = at least 20% of the target is hidden.
            public int StartBefore, StartBeforeMajor, Start, StartMajor, StartRoad;
            public int CallBefore, CallBeforeMajor, Call, CallMajor, CallRoad;
            public int SpeedBefore, SpeedBeforeMajor, Speed, SpeedMajor, SpeedRiftBefore, SpeedRift;
            public int Skill, SkillMajor, Stick, StickMajor, Pause, PauseMajor, Currency, CurrencyMajor;
            public int PanelBefore, PanelBeforeMajor, Panel, PanelMajor, PanelCases;
            public float StartX, StartY, StartH, CallX, CallY, CallH, SpeedX;
        }

        public static readonly float[][] PanelSizes = {
            new[]{548f,196f}, // build menu with four families
            new[]{464f,250f}, // tower menu
            new[]{464f,194f}, new[]{464f,310f} // recipe menu with one or two rows (four-family loadouts reach four recipes)
        };

        public static Row[] Review()
        {
            var rows = new List<Row>();
            foreach (CampaignStage stage in CampaignCatalog.Stages)
                foreach (Screen screen in Screens) {
                    if (stage.Index == 3) continue; // 1-4 follows the hunter with a moving camera; no fixed projection.
                    Board b = Project(stage, screen); MobileLayout l = b.Layout;
                    Box2[] world = b.Guards.ToArray();
                    var row = new Row { Stage = CampaignCatalog.StageId(stage.Index), Chapter = stage.Chapter, Screen = screen.ToString() };
                    Box2 start = LegacyStart(l), call = LegacyCall(l), speed = LegacySpeed();
                    row.StartBefore = Count(start, world); row.StartBeforeMajor = CountMajor(start, world);
                    row.CallBefore = Count(call, world); row.CallBeforeMajor = CountMajor(call, world);
                    row.SpeedBefore = Count(speed, world); row.SpeedBeforeMajor = CountMajor(speed, world); row.SpeedRiftBefore = Count(speed, b.Rifts);
                    l.PlaceControls(b.Guards, b.Rifts);
                    start = l.Button(MobileAction.Start); call = l.WaveCallBox(); speed = l.Button(MobileAction.Speed);
                    row.StartX = start.X; row.StartY = start.Y; row.StartH = start.Height;
                    row.CallX = call.X; row.CallY = call.Y; row.CallH = call.Height; row.SpeedX = speed.X;
                    row.Start = Count(start, world); row.StartMajor = CountMajor(start, world); row.StartRoad = Count(start, b.Roads);
                    row.Call = Count(call, world); row.CallMajor = CountMajor(call, world); row.CallRoad = Count(call, b.Roads);
                    row.Speed = Count(speed, world); row.SpeedMajor = CountMajor(speed, world); row.SpeedRift = Count(speed, b.Rifts);
                    Box2 skill = l.Button(MobileAction.Skill), pause = l.Button(MobileAction.Pause);
                    V2 c = l.JoystickCenter; Box2 stick = new Box2(c.X-62, c.Y-62, 124, 124);
                    Box2 currency = new Box2(l.Width-314, 10, 222, 50);
                    row.Skill = Count(skill, world); row.SkillMajor = CountMajor(skill, world);
                    row.Stick = Count(stick, world); row.StickMajor = CountMajor(stick, world);
                    row.Pause = Count(pause, world); row.PauseMajor = CountMajor(pause, world);
                    row.Currency = Count(currency, world); row.CurrencyMajor = CountMajor(currency, world);
                    for (int i = 0; i < b.Pads.Length; i++)
                        foreach (float[] size in PanelSizes) {
                            row.PanelCases++;
                            float before = Covered(LegacyPanel(l, b.PadCenters[i], b.Unit, size[0], size[1]), b.Pads[i]);
                            float after = Covered(l.PanelBox(b.PadCenters[i], b.Unit, size[0], size[1]), b.Pads[i]);
                            if (before > 0) row.PanelBefore++; if (before >= Major) row.PanelBeforeMajor++;
                            if (after > 0) row.Panel++; if (after >= Major) row.PanelMajor++;
                        }
                    rows.Add(row);
                }
            return rows.ToArray();
        }
        static void Require(bool pass, string why) { if (!pass) throw new Exception("HUD occlusion: "+why); }

        public static void Run()
        {
            HomeGeometry();
            Row[] rows = Review();
            Require(rows.Length == (CampaignCatalog.Stages.Length-1)*Screens.Length, "missing projection cases");
            foreach (Row r in rows) {
                string at = r.Stage+" at "+r.Screen;
                Require(r.Start == 0, "start button covers an entrance or pad on "+at);
                Require(r.Call == 0, "wave call covers an entrance or pad on "+at);
                // An entrance outranks the top of a pad: the speed button may trade one for the other, never the reverse.
                Require(r.SpeedRift <= r.SpeedRiftBefore && (r.SpeedX == 274 || r.SpeedRift == 0), "speed button covers an entrance after moving on "+at);
                // Only the 4:3-wide 960-unit reference is too narrow to seat the 548-unit build menu beside a centre pad.
                Require(r.PanelMajor == 0 || r.Screen == "1280x800 safe 0,0,1280,800", "a pad menu covers its own tower on "+at);
                Require(r.Panel <= r.PanelBefore, "pad menu placement regressed on "+at);
            }
            Require(rows.Sum(r => r.PanelMajor) <= 13 && rows.Sum(r => r.PanelBeforeMajor) > 7000, "pad menu coverage budget");
            CheckReadability();
            FusionGeometry();
            // Thumb and top-row controls stay where they are, so every pad must keep a tappable part beside them:
            // at most 70% of a pad's touch box may lie under the stick, skill button, currency chip or speed button
            // (worst measured case 64%: 3-4 under the currency chip).
            foreach (CampaignStage stage in CampaignCatalog.Stages)
                foreach (Screen screen in Screens) {
                    if (stage.Index == 3) continue;
                    Board fixedBoard = Project(stage, screen); MobileLayout fixedLayout = fixedBoard.Layout; fixedLayout.PlaceControls(fixedBoard.Guards, fixedBoard.Rifts);
                    V2 thumbCenter = fixedLayout.JoystickCenter;
                    Box2[] fixedControls = { new Box2(thumbCenter.X-MobileLayout.JoystickRadius, thumbCenter.Y-MobileLayout.JoystickRadius, 2*MobileLayout.JoystickRadius, 2*MobileLayout.JoystickRadius),
                        fixedLayout.Button(MobileAction.Skill), new Box2(fixedLayout.Width-314, 10, 222, 50), fixedLayout.Button(MobileAction.Speed) };
                    foreach (Box2 pad in fixedBoard.Pads)
                        Require(fixedControls.Sum(k => Covered(k, pad)) <= .7f, "a pad is buried under fixed controls on "+CampaignCatalog.StageId(stage.Index)+" at "+screen);
                }

            // The reported case: 3-7 on a 1280x720 phone with a 652-pixel safe height.
            CampaignStage staff = CampaignCatalog.Stages[CampaignCatalog.FindStage("C03-S007")];
            Board board = Project(staff, Screens[0]); MobileLayout l = board.Layout;
            Box2 south = board.Rifts[board.Rifts.Length-1];
            Require(Covered(LegacyStart(l), south) >= Major, "the old start button no longer reproduces the hidden south entrance");
            l.PlaceControls(board.Guards, board.Rifts);
            Box2 start = l.Button(MobileAction.Start), call = l.WaveCallBox(), skill = l.Button(MobileAction.Skill);
            float thumb = l.JoystickCenter.X+MobileLayout.JoystickCatch;
            Require(!Hits(start, south) && start.Width == 320 && start.Height == 76 && start.Y == l.Height-104, "3-7 start keeps its size and row");
            Require(start.X >= thumb && start.X+start.Width <= l.Width-16, "3-7 start stays reachable");
            Require(!Hits(call, south) && call.X >= thumb && call.X+call.Width < skill.X, "3-7 wave call stays between the thumbs");
            Require(l.HitButton(start.Center, RunState.Ready) == MobileAction.Start &&
                l.HitButton(new V2(south.Center.X, south.Center.Y), RunState.Ready) == MobileAction.None, "taps follow the moved start button");

            foreach (CampaignStage stage in CampaignCatalog.Stages) {
                if (stage.Index == 3) continue;
                foreach (Screen screen in Screens) {
                    Board b = Project(stage, screen); MobileLayout layout = b.Layout;
                    layout.PlaceControls(b.Guards, b.Rifts);
                    Box2 first = layout.Button(MobileAction.Start), firstCall = layout.WaveCallBox(), speed = layout.Button(MobileAction.Speed);
                    float left = layout.JoystickCenter.X+MobileLayout.JoystickCatch;
                    foreach (Box2 box in new[]{first, firstCall}) {
                        Require(box.Width >= 54 && box.Height >= 54 && box.X >= left && box.Y >= 0 &&
                            box.X+box.Width <= layout.Width && box.Y+box.Height <= layout.Height-8+.01f, "bottom control bounds on "+stage.Id);
                        Require(layout.SafeArea.Contains(layout.ToScreen(box.Center)), "bottom control leaves the safe area");
                    }
                    Require(firstCall.X+firstCall.Width < layout.Button(MobileAction.Skill).X, "wave call under the skill button on "+stage.Id);
                    Require(speed.Y == 8 && speed.X >= 274 && speed.X+speed.Width <= layout.Width-322-150-8, "speed button bounds on "+stage.Id);
                    if (!b.Guards.Any(g => Near(LegacyStart(layout), g))) Require(first.X == LegacyStart(layout).X && first.Y == LegacyStart(layout).Y && first.Height == 76, "a clear start button moved on "+stage.Id);
                    if (!b.Guards.Any(g => Near(LegacyCall(layout), g))) Require(firstCall.X == LegacyCall(layout).X && firstCall.Y == LegacyCall(layout).Y && firstCall.Height == 84, "a clear wave call moved on "+stage.Id);
                    if (!b.Rifts.Any(g => Near(LegacySpeed(), g))) Require(speed.X == 274, "a clear speed button moved on "+stage.Id);
                    layout.PlaceControls(b.Guards, b.Rifts);
                    Require(layout.Button(MobileAction.Start).X == first.X && layout.WaveCallBox().X == firstCall.X, "placement is not deterministic");
                    layout.PlaceControls(null, null);
                    Require(layout.Button(MobileAction.Start).X == LegacyStart(layout).X && layout.WaveCallBox().Y == LegacyCall(layout).Y &&
                        layout.Button(MobileAction.Speed).X == 274, "defaults are not restored");
                    for (int i = 0; i < b.Pads.Length; i++)
                        foreach (float[] size in PanelSizes) {
                            Box2 panel = layout.PanelBox(b.PadCenters[i], b.Unit, size[0], size[1]);
                            Require(panel.X >= 12-.01f && panel.X+panel.Width <= layout.Width-12+.01f && panel.Y >= 70-.01f &&
                                panel.Y+panel.Height <= layout.Height-10+.01f, "pad menu leaves the screen on "+stage.Id);
                            Box2 legacy = LegacyPanel(layout, b.PadCenters[i], b.Unit, size[0], size[1]);
                            if (legacy.Y+legacy.Height <= b.Pads[i].Y+.4f || legacy.Y >= b.Pads[i].Y+b.Pads[i].Height-.4f) Require(Math.Abs(panel.X-legacy.X) < .01f && Math.Abs(panel.Y-legacy.Y) < .01f, "a clear pad menu moved on "+stage.Id);
                        }
                }
            }
        }

        public static int FusionGeometry()
        {
            int checks=0;
            foreach(CampaignStage stage in CampaignCatalog.Stages)foreach(Screen screen in Screens) {
                if(stage.Index==3)continue;
                Board b=Project(stage,screen);MobileLayout l=b.Layout;
                for(int anchor=0;anchor<b.Pads.Length;anchor++) {
                    var guards=new List<Box2>{b.Pads[anchor],b.Pads[(anchor+1)%b.Pads.Length]};
                    Require(l.TryFusionHeader(guards,out Box2 panel),"fusion guidance placement "+stage.Id+" "+screen);
                    Box2 fit=MobileLayout.FusionFitBox(panel);
                    var title=new Box2(panel.X+62,panel.Y+6,Math.Min(420,panel.Width-184),26);
                    var result=new Box2(panel.X+62,panel.Y+34,title.Width,20);
                    var field=new Box2(panel.X+62,panel.Y+panel.Height-24,Math.Min(420,panel.Width-74),18);
                    var cancel=new Box2(panel.X+panel.Width-112,panel.Y+8,100,48);
                    foreach(Box2 label in new[]{title,result,fit,field}) {
                        Require(label.X>=panel.X && label.Y>=panel.Y && label.X+label.Width<=panel.X+panel.Width && label.Y+label.Height<=panel.Y+panel.Height,"fusion label outside panel");
                        Require(l.SafeArea.Contains(l.ToScreen(new V2(label.X,label.Y))) && l.SafeArea.Contains(l.ToScreen(new V2(label.X+label.Width,label.Y+label.Height))),"fusion label outside safe area");
                        Require(!Hits(label,cancel) && !guards.Any(g=>Hits(label,g)),"fusion label covers cancel or ingredient");
                    }
                    Require(fit.Width>=324 && fit.Height==18 && !Hits(fit,title) && !Hits(fit,result) && !Hits(fit,field),"fusion fit row overlaps guidance");
                    checks++;
                }
                // Force the compact fallback contract as well as the panel chosen on this board.
                Box2 compact=MobileLayout.FusionFitBox(new Box2(12,72,508,96));
                Require(compact.Width==324 && compact.Y+compact.Height==144,"compact fusion fit geometry");
            }
            return checks;
        }

        static void CheckReadability()
        {
            foreach (CampaignStage stage in CampaignCatalog.Stages) {
                if (stage.Index == 3) continue;
                var game = new DefenseSimulation(new GameBalance(), 7, new CampaignBattle { Stage=stage });
                int maxNotice = Enumerable.Range(0, game.WaveCount).Max(i => 5 + game.GetWaveBriefing(i).Summary.Length);
                foreach (Screen screen in Screens) {
                    Board board = Project(stage, screen);
                    MobileLayout layout = board.Layout;
                    layout.PlaceControls(board.Guards, board.Rifts);
                    var world = board.Guards.Concat(new[]{layout.Button(MobileAction.Speed)}).ToArray();
                    Box2 core = layout.CoreStatusBox(world);
                    Box2 wave = layout.WaveHeader(board.Rifts.Where(b=>b.Y<62 && b.Y+b.Height>0).ToArray(), world);
                    Box2 count = layout.TowerCountBox(stage.HasCatalysts, wave, world);
                    var occupied = new List<Box2>();
                    layout.ReadabilityControls(core, wave, count, stage.HasCatalysts, occupied);
                    if (stage.Boss) occupied.Add(new Box2(layout.Width/2-180, 62, 360, 82));
                    string scenario = stage.Id + " " + screen;
                    Require(layout.SafeArea.Contains(layout.ToScreen(count.Center)) && !Hits(count,wave), "tower count box " + scenario);
                    foreach (Box2 rift in board.Rifts) {
                        Box2 tag = layout.EntranceLabel(rift, world, board.Roads, occupied);
                        Require(tag.Width > 0 && tag.X >= 0 && tag.X+tag.Width <= layout.Width &&
                            tag.Y >= 0 && tag.Y+tag.Height <= layout.Height, "missing or clipped entrance label " + scenario);
                        Require(!world.Concat(board.Roads).Concat(occupied).Any(b => Hits(tag,b)), "entrance label covers a road, pad, or control " + scenario);
                        occupied.Add(tag);
                    }
                    Box2 notice = layout.BattleNoticeBox(maxNotice, wave, world, occupied);
                    Require(notice.Width > 0 && notice.Y >= wave.Y+wave.Height &&
                        notice.X >= 0 && notice.X+notice.Width <= layout.Width && notice.Y+notice.Height <= layout.Height,
                        "missing or clipped notice " + scenario);
                    Require(!occupied.Any(b => Hits(notice,b)) && !world.Any(b => Covered(notice,b) >= Major), "notice covers a control or at least 20 percent of a pad " + scenario);
                }
            }
        }
        public static int HomeGeometry()
        {
            int checks=0;
            foreach(Screen screen in Screens) {
                var l=new MobileLayout(screen.Width,screen.Height,new Box2(screen.SafeLeft,0,screen.Width-screen.SafeLeft,screen.SafeHeight));
                foreach(int tabs in new[]{5,7})foreach(int right in new[]{3,5})foreach(bool expanded in new[]{false,true}) {
                    var boxes=new List<Box2>();
                    for(int i=0;i<7;i++)boxes.Add(l.HomeTop(i));
                    boxes.Add(l.HomePanel());
                    boxes.Add(l.HomePass());
                    for(int i=0;i<tabs;i++)boxes.Add(l.HomeNavigation(i,tabs));
                    for(int i=0;i<right;i++){Box2 b=l.HomeRight(i,right,expanded);if(b.Width>0)boxes.Add(b);}
                    foreach(Box2 b in boxes) {
                        Require(b.Width>=48 && b.Height>=48 && b.X>=0 && b.Y>=0 && b.X+b.Width<=l.Width+.01f && b.Y+b.Height<=l.Height+.01f,"home outside safe area "+screen);
                        Require(l.SafeArea.Contains(l.ToScreen(b.Center)),"home physical safe area "+screen);checks++;
                    }
                    for(int i=0;i<boxes.Count;i++)for(int j=i+1;j<boxes.Count;j++)Require(!Hits(boxes[i],boxes[j]),"home overlap "+screen+" boxes "+i+"/"+j);
                }
                Box2 dialog=l.HeartDialog();
                Require(dialog.X>=0 && dialog.Y>=0 && dialog.X+dialog.Width<=l.Width && dialog.Y+dialog.Height<=l.Height,"heart dialog safe area");
                for(int i=0;i<3;i++) {
                    Box2 b=l.HeartDialogButton(i);
                    Require(dialog.Contains(new V2(b.X,b.Y)) && dialog.Contains(new V2(b.X+b.Width,b.Y+b.Height)),"dialog button outside panel");
                    for(int j=0;j<i;j++)Require(!Hits(b,l.HeartDialogButton(j)),"dialog button overlap");checks++;
                }
            }
            Require(new MobileLayout(960,720,new Box2()).HomeCompact,"960 tab explanations must hide");
            return checks;
        }
        public static int DailyGeometry()
        {
            int checks=0;
            foreach(Screen screen in Screens) {
                var l=new MobileLayout(screen.Width,screen.Height,new Box2(screen.SafeLeft,0,screen.Width-screen.SafeLeft,screen.SafeHeight));
                var boxes=new List<Box2>{l.DailyBack(),l.DailyTitle(),l.DailySummary(),l.DailyFooter(),l.MissionAllClear(),l.DailyPrimary(),l.RouletteOdds(),l.PassCap(),l.DailyPageLabel(),l.DailyScroll(false),l.DailyScroll(true),l.VictoryPanel(),l.VictoryDouble()};
                for(int i=0;i<6;i++){boxes.Add(l.MissionRow(i));boxes.Add(l.MissionClaim(i));}
                for(int i=0;i<7;i++)boxes.Add(l.AttendanceDay(i));
                for(int i=0;i<l.DailyVisibleRows();i++) {
                    Box2 row=l.DailyRow(i);boxes.Add(row);
                    foreach(bool paid in new[]{false,true}){Box2 button=l.PassClaim(i,paid);boxes.Add(button);Require(row.Contains(button.Center),"pass button outside row");}
                    Require(row.Y+row.Height<=l.DailyScroll(false).Y,"daily rows cover footer");
                    if(i>0)Require(!Hits(row,l.DailyRow(i-1)),"daily rows overlap");
                }
                foreach(Box2 b in boxes) {
                    Require(b.Width>0 && b.Height>0 && b.X>=0 && b.Y>=0 && b.X+b.Width<=l.Width+.01f && b.Y+b.Height<=l.Height+.01f,"daily safe bounds "+screen);
                    Require(l.SafeArea.Contains(l.ToScreen(new V2(b.X,b.Y))) && l.SafeArea.Contains(l.ToScreen(new V2(b.X+b.Width,b.Y+b.Height))),"daily physical safe area "+screen);checks++;
                }
                Require(l.VictoryPanel().Contains(l.VictoryDouble().Center),"double outside result");
                for(int i=0;i<6;i++) {
                    Require(l.MissionRow(i).Contains(l.MissionClaim(i).Center),"mission button outside row");
                    Require(!Hits(l.MissionRow(i),l.MissionAllClear()),"all clear overlaps mission");
                }
                for(int i=0;i<7;i++) {
                    Require(!Hits(l.AttendanceDay(i),l.DailyPrimary()),"attendance covers claim");
                    for(int j=0;j<i;j++)Require(!Hits(l.AttendanceDay(i),l.AttendanceDay(j)),"attendance days overlap");
                }
            }
            return checks;
        }
        public static int ShopGeometry()
        {
            int checks=0;
            foreach(Screen screen in Screens) {
                var l=new MobileLayout(screen.Width,screen.Height,new Box2(screen.SafeLeft,0,screen.Width-screen.SafeLeft,screen.SafeHeight));
                var boxes=new List<Box2>{l.ShopPanel(),l.ShopFooter(),l.SupplyPanel(),l.SupplyClose(),l.SupplyRule(),l.TowerCardRule(),l.SupplyScroll(false),l.SupplyScroll(true)};
                for(int i=0;i<5;i++)boxes.Add(l.ShopTab(i));
                for(int i=0;i<2;i++){boxes.Add(l.ShopProduct(i));boxes.Add(l.ShopProductButton(i));boxes.Add(l.SupplyKindTab(i));boxes.Add(l.ShopTab(i,2));}
                for(int i=0;i<6;i++){boxes.Add(l.ShopSlot(i));boxes.Add(l.ShopSlotButton(i));boxes.Add(l.TowerCard(i));boxes.Add(l.TowerGradeButton(i));}
                foreach(bool odds in new[]{false,true}) {
                    Box2 last=new Box2();
                    for(int i=0;i<l.SupplyVisibleRows(odds);i++) {
                        Box2 row=l.SupplyRow(i,odds);boxes.Add(row);
                        Require(!Hits(row,l.SupplyScroll(false)) && !Hits(row,l.SupplyScroll(true)),"supply rows cover scroll");
                        if(odds)Require(!Hits(row,l.SupplyRule()),"odds rows cover rule");
                        if(i>0)Require(!Hits(last,row),"supply rows overlap");last=row;
                    }
                }
                foreach(Box2 b in boxes) {
                    Require(b.Width>0 && b.Height>0 && b.X>=0 && b.Y>=88 && b.X+b.Width<=l.Width+.01f && b.Y+b.Height<=l.Height+.01f,"shop bounds "+screen);
                    Require(l.SafeArea.Contains(l.ToScreen(new V2(b.X,b.Y))) && l.SafeArea.Contains(l.ToScreen(new V2(b.X+b.Width,b.Y+b.Height))),"shop physical safe area "+screen);
                    for(int top=0;top<7;top++)Require(!Hits(b,l.HomeTop(top)),"shop covers top currency row");checks++;
                }
                var groups=new List<List<Box2>>();
                var daily=new List<Box2>{l.ShopFooter()};for(int i=0;i<5;i++)daily.Add(l.ShopTab(i));for(int i=0;i<6;i++)daily.Add(l.ShopSlotButton(i));groups.Add(daily);
                groups.Add(new List<Box2>{l.ShopProductButton(0),l.ShopProductButton(1),l.ShopFooter()});
                groups.Add(new List<Box2>{l.SupplyClose(),l.SupplyKindTab(0),l.SupplyKindTab(1),l.SupplyScroll(false),l.SupplyScroll(true)});
                var cards=new List<Box2>{l.ShopTab(0,2),l.ShopTab(1,2),l.TowerCardPage(false),l.TowerCardPage(true),l.TowerCardRule()};
                for(int i=0;i<6;i++) {
                    Box2 card=l.TowerCard(i),button=l.TowerGradeButton(i);
                    Require(card.Contains(button.Center) && card.Y+110<=button.Y,"grade progress and reason overlap button");cards.Add(button);
                    Require(!Hits(card,l.TowerCardRule()) && !Hits(card,l.TowerCardPage(false)) && !Hits(card,l.TowerCardPage(true)),"card covers footer");
                }
                groups.Add(cards);
                foreach(var group in groups)for(int i=0;i<group.Count;i++)for(int j=i+1;j<group.Count;j++)Require(!Hits(group[i],group[j]),"shop touch/label overlap "+screen+" "+i+"/"+j);
            }
            return checks;
        }
        // Same clearance the layout uses: boxes closer than four reference units count as touching.
        static bool Near(Box2 a, Box2 b) { return a.X<b.X+b.Width+4 && b.X-4<a.X+a.Width && a.Y<b.Y+b.Height+4 && b.Y-4<a.Y+a.Height; }
    }
}
