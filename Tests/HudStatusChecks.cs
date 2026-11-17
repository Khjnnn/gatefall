using System;
using System.Collections.Generic;
using System.Linq;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class HudStatusChecks {
        public sealed class Row {
            public int Stage, Width, Height, BeforeWaveClashes, AfterWaveClashes;
            public float X, Y, W, H, SpeedX;
            public bool Compact, Clear, WaveControlsClear;
        }
        static bool Overlap(Box2 a,Box2 b) { return a.X<b.X+b.Width+4 && b.X-4<a.X+a.Width && a.Y<b.Y+b.Height+4 && b.Y-4<a.Y+a.Height; }
        public static Row[] Review() {
            var rows=new List<Row>();
            foreach(var screen in new[]{new[]{2400,1080,929},new[]{1920,1080,959},new[]{1280,720,639}}) {
                var l=new MobileLayout(screen[0],screen[1],new Box2(0,0,screen[0],screen[2]));
                float world=(float)(screen[1]/(2*Math.Max(12.6,24.8/(screen[0]/(double)screen[1])))/l.Scale);
                foreach(var stage in CampaignCatalog.Stages) {
                    var game=new DefenseSimulation(new GameBalance(),7,new CampaignBattle{Stage=stage});
                    float camera=game.ExpandedArena?0:.35f;
                    Func<V2,V2> project=p=>new V2((screen[0]/2f+p.X*world*l.Scale-l.Origin.X)/l.Scale,(screen[1]/2f-(p.Y-camera)*world*l.Scale-l.Origin.Y)/l.Scale);
                    var guards=new List<Box2>();
                    foreach(var pad in game.BoardSockets) {
                        var p=project(pad);guards.Add(new Box2(p.X-1.2f*world,p.Y-2.3f*world,2.4f*world,3.2f*world));
                    }
                    var rifts=new List<Box2>();var allRifts=new List<Box2>();
                    foreach(int lane in game.OpenLanes) {var p=project(game.GatePositions[lane]);var r=new Box2(p.X-2.55f*world,p.Y-1.6f*world,5.1f*world,3.2f*world);guards.Add(r);allRifts.Add(r);if(r.Y<62 && r.Y+r.Height>0)rifts.Add(r);}
                    // Same order as the HUD: controls leave entrances first, then the wave header avoids the speed button.
                    bool speedOnRift=allRifts.Any(r=>Overlap(new Box2(274,8,62,54),r));
                    l.PlaceControls(game.ExpandedArena?null:guards,game.ExpandedArena?null:allRifts);
                    if(!game.ExpandedArena)guards.Add(l.Button(MobileAction.Speed));
                    var previousWave=l.WaveHeader(rifts);
                    var wave=game.ExpandedArena ? previousWave : l.WaveHeader(rifts,guards);
                    int previousClashes=guards.Count(g=>Overlap(previousWave,g));
                    int waveClashes=guards.Count(g=>Overlap(wave,g));
                    guards.Add(stage.Boss?new Box2(l.Width/2-180,0,360,105):new Box2(wave.X,0,wave.Width,105));
                    var chip=l.CoreStatusBox(guards);var speed=l.Button(MobileAction.Speed);bool clear=!Overlap(chip,speed);foreach(var g in guards)if(Overlap(chip,g) && !(g.X==speed.X && g.Y==speed.Y && g.Width==speed.Width))clear=false;
                    if(speed.X!=274 && (!speedOnRift || allRifts.Any(r=>Overlap(speed,r)) || Overlap(speed,wave) || speed.X+speed.Width>l.Width-322-150-8))clear=false;
                    foreach(var socket in game.BoardSockets) {
                        var p=project(socket);var badge=new Box2(p.X-.55f*world,p.Y-3.45f*world,1.1f*world,1.1f*world);
                        if(Overlap(chip,badge)) {
                            var compactBadge=new Box2(p.X+.354f*world,p.Y-2.246f*world,.792f*world,1.092f*world);
                            if(Overlap(chip,compactBadge))clear=false;
                        }
                    }
                    rows.Add(new Row{Stage=stage.Index+1,Width=screen[0],Height=screen[1],X=chip.X,Y=chip.Y,W=chip.Width,H=chip.Height,SpeedX=speedOnRift?274:speed.X,Compact=chip.Width<252,Clear=clear,
                        BeforeWaveClashes=previousClashes,AfterWaveClashes=waveClashes,WaveControlsClear=!Overlap(wave,speed) && wave.X+wave.Width<=l.Width-322});
                }
            }
            return rows.ToArray();
        }
        public static void Run() {
            var rows=Review();if(rows.Length!=CampaignCatalog.Stages.Length*3)throw new Exception("Missing status projection cases");
            foreach(var r in rows)if(!r.Clear || r.Y<0 || r.Y+r.H>62 || r.SpeedX!=274)throw new Exception("Status hides a tower/pad/rift,or moved a control on1-"+r.Stage);
            foreach (var r in rows)
                if (!r.WaveControlsClear || r.AfterWaveClashes > r.BeforeWaveClashes)
                    throw new Exception("Wave header added an overlap on "+CampaignCatalog.StageId(r.Stage-1));
            var rail=rows.Single(r=>r.Stage==CampaignCatalog.FindStage("C03-S001")+1 && r.Width==2400);
            if (rail.BeforeWaveClashes!=3 || rail.AfterWaveClashes!=0)
                throw new Exception("Rail wave header does not clear the three north tower sites");
            CheckNarrowWaveAndBossDock();
            var l=new MobileLayout(2400,1080,new Box2(0,0,2400,929));
            var clear=l.CoreStatusBox(null);if(clear.X!=14 || clear.Y!=10 || clear.Width!=252)throw new Exception("Clear chip changed");
            var impossible=l.CoreStatusBox(new[]{new Box2(0,0,1000,1000)});if(impossible.Width!=252 || impossible.Y!=10)throw new Exception("Unsafe fallback changed");
            var guards=new[]{new Box2(220,10,45,55)};var first=l.CoreStatusBox(guards);var second=l.CoreStatusBox(guards);
            if(first.Width!=176 || first.X!=second.X || first.Y!=second.Y || guards[0].X!=220)throw new Exception("Compact layout is unstable/mutates input");
        }

        static void CheckNarrowWaveAndBossDock()
        {
            foreach (var screen in new[]{new[]{2400,1080,0,929},new[]{2400,1080,0,952},new[]{1920,1080,68,1080},
                new[]{1280,720,45,720},new[]{1280,576,0,495},new[]{1920,1080,0,959},new[]{1280,720,0,639},
                new[]{1280,720,0,652},new[]{1280,720,0,720},new[]{1280,800,0,710}}) {
                var layout = new MobileLayout(screen[0],screen[1],new Box2(screen[2],0,screen[0]-screen[2],screen[3]));
                float world = (float)(screen[1]/(2*Math.Max(12.6,24.8/(screen[0]/(double)screen[1]))));
                float unit = world/layout.Scale;
                V2 Project(V2 p) => layout.FromScreen(new V2(screen[0]/2f+p.X*world,screen[1]/2f+(p.Y-.35f)*world));
                foreach (string id in new[]{"C02-S018","C02-S022","C03-S001"}) {
                    var stage = CampaignCatalog.Stages[CampaignCatalog.FindStage(id)];
                    var game = new DefenseSimulation(new GameBalance(),7,new CampaignBattle{Stage=stage});
                    var pads = game.BoardSockets.Select(Project).Select(p=>new Box2(p.X-1.2f*unit,p.Y-2.3f*unit,2.4f*unit,3.2f*unit));
                    var rifts = game.OpenLanes.Select(lane=>Project(game.GatePositions[lane]))
                        .Select(p=>new Box2(p.X-2.55f*unit,p.Y-1.6f*unit,5.1f*unit,3.2f*unit)).ToArray();
                    var guards = pads.Concat(rifts).ToList();
                    layout.PlaceControls(guards,rifts);
                    guards.Add(layout.Button(MobileAction.Speed));
                    Box2 wave = layout.WaveHeader(rifts.Where(b=>b.Y<62 && b.Y+b.Height>0).ToArray(),guards);
                    if (guards.Any(g=>Overlap(g,wave)))
                        throw new Exception("Wave HUD hides a pad/rift on "+id+" at "+string.Join("x",screen));
                    if (id=="C03-S001" && screen[3]==639 && wave.Height!=MobileLayout.CompactWaveHeight)
                        throw new Exception("Rail does not use its compact wave header");
                    if (!stage.Boss) continue;

                    int bossLane = game.Schedule.First(g=>g.Kind==EnemyKind.Boss).Lane;
                    foreach (int lane in game.OpenLanes) {
                        V2[] road = game.Roads[lane];
                        bool bossRoad = lane == bossLane;
                        for (int i=1; i<road.Length; i++)
                            guards.Add(MobileLayout.RoadGuard(Project(road[i-1]),Project(road[i]),
                                (bossRoad?2.1f:1.3f)*unit,(bossRoad?4f:1.3f)*unit,(bossRoad?1.2f:1.3f)*unit));
                    }
                    Box2 dock = layout.BossStatusDock(guards);
                    if (dock.Width!=200 || dock.X<14 || dock.X+dock.Width>layout.Width-14 || dock.Y<84 ||
                        Overlap(dock,wave) || Overlap(dock,layout.Button(MobileAction.Skill)) ||
                        Overlap(dock,layout.WaveCallBox()) || guards.Any(g=>Overlap(g,dock)))
                        throw new Exception("Market boss dock hides the route, a pad, or a control at "+string.Join("x",screen));
                }
            }
        }
    }
}
