using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class HudStatusChecks {
        public sealed class Row { public int Stage,Width,Height; public float X,Y,W,H,SpeedX; public bool Compact,Clear; }
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
                    var rifts=new List<Box2>();
                    foreach(int lane in game.OpenLanes) {var p=project(game.GatePositions[lane]);var r=new Box2(p.X-2.55f*world,p.Y-1.6f*world,5.1f*world,3.2f*world);guards.Add(r);if(r.Y<62 && r.Y+r.Height>0)rifts.Add(r);}
                    var wave=l.WaveHeader(rifts);guards.Add(stage.Boss?new Box2(l.Width/2-180,0,360,105):new Box2(wave.X,0,wave.Width,105));
                    var chip=l.CoreStatusBox(guards);var speed=l.Button(MobileAction.Speed);bool clear=!Overlap(chip,speed);foreach(var g in guards)if(Overlap(chip,g))clear=false;
                    foreach(var socket in game.BoardSockets) {
                        var p=project(socket);var badge=new Box2(p.X-.55f*world,p.Y-3.45f*world,1.1f*world,1.1f*world);
                        if(Overlap(chip,badge)) {
                            var compactBadge=new Box2(p.X+.354f*world,p.Y-2.246f*world,.792f*world,1.092f*world);
                            if(Overlap(chip,compactBadge))clear=false;
                        }
                    }
                    rows.Add(new Row{Stage=stage.Index+1,Width=screen[0],Height=screen[1],X=chip.X,Y=chip.Y,W=chip.Width,H=chip.Height,SpeedX=speed.X,Compact=chip.Width<252,Clear=clear});
                }
            }
            return rows.ToArray();
        }
        public static void Run() {
            var rows=Review();if(rows.Length!=42)throw new Exception("Missing status projection cases");
            foreach(var r in rows)if(!r.Clear || r.Y<0 || r.Y+r.H>62 || r.SpeedX!=274)throw new Exception("Status hides a tower/pad/rift,or moved a control on1-"+r.Stage);
            var l=new MobileLayout(2400,1080,new Box2(0,0,2400,929));
            var clear=l.CoreStatusBox(null);if(clear.X!=14 || clear.Y!=10 || clear.Width!=252)throw new Exception("Clear chip changed");
            var impossible=l.CoreStatusBox(new[]{new Box2(0,0,1000,1000)});if(impossible.Width!=252 || impossible.Y!=10)throw new Exception("Unsafe fallback changed");
            var guards=new[]{new Box2(220,10,45,55)};var first=l.CoreStatusBox(guards);var second=l.CoreStatusBox(guards);
            if(first.Width!=176 || first.X!=second.X || first.Y!=second.Y || guards[0].X!=220)throw new Exception("Compact layout is unstable/mutates input");
        }
    }
}
