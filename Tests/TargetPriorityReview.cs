using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Geometric distance to the end of the remaining road, not arrival time or blocked duration.
    public static class TargetPriorityReview {
        public sealed class Board {
            public int Stage, Sockets, DifferingSockets, SameLaneSockets;
            public float Range, LargestRouteGap;
            public int ExampleSocket, RadialLane, RouteLane;
            public float[] RadialPosition, RoutePosition;
            public float RadialRemaining, RouteRemaining;
        }
        public static float Remaining(DefenseSimulation game, Enemy enemy) {
            var road=game.Roads[Math.Max(0,Math.Min(game.Roads.Length-1,enemy.Lane))];
            if(road.Length<2)return enemy.Position.Length;
            int next=Math.Max(1,Math.Min(road.Length-1,enemy.Waypoint));
            float result=V2.Distance(enemy.Position,road[next]);
            for(int i=next+1;i<road.Length;i++)result+=V2.Distance(road[i-1],road[i]);
            return result;
        }
        public static Board[] StaticBoards() {
            var result=new List<Board>();
            foreach(var stage in CampaignCatalog.Stages) {
                var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,new CampaignBattle{Stage=stage});
                var row=new Board{Stage=stage.Index+1,Sockets=g.BoardSockets.Length,Range=g.Stats(TowerKind.Autocannon,1).Range,ExampleSocket=-1};
                var samples=new List<Enemy>();
                foreach(int lane in g.OpenLanes) {
                    float length=BattlefieldLayouts.RoadLength(g.Roads[lane]);
                    for(float along=0;along<length-1.12f;along+=1) {
                        int next;var position=BattlefieldLayouts.Along(g.Roads[lane],along,out next);
                        var sample=new Enemy{Id=samples.Count+1,Lane=lane,Waypoint=next,Position=position,Health=1};
                        if(Math.Abs(Remaining(g,sample)-(length-along))>.001f)throw new Exception("Along/remaining metric mismatch");
                        samples.Add(sample);
                    }
                }
                for(int pad=0;pad<g.BoardSockets.Length;pad++) {
                    var visible=samples.FindAll(e=>V2.Distance(g.BoardSockets[pad],e.Position)<=row.Range);
                    bool differs=false,sameLane=false;
                    for(int i=0;i<visible.Count;i++)for(int j=i+1;j<visible.Count;j++) {
                        var a=visible[i];var b=visible[j];
                        if(a.Position.Length>b.Position.Length){var temp=a;a=b;b=temp;}
                        if(b.Position.Length-a.Position.Length<.2f)continue;
                        float ar=Remaining(g,a),br=Remaining(g,b),gap=ar-br;
                        if(gap<=.5f)continue;
                        differs=true;sameLane|=a.Lane==b.Lane;
                        if(gap>row.LargestRouteGap){row.LargestRouteGap=gap;row.ExampleSocket=pad;row.RadialLane=a.Lane;row.RouteLane=b.Lane;row.RadialPosition=new[]{a.Position.X,a.Position.Y};row.RoutePosition=new[]{b.Position.X,b.Position.Y};row.RadialRemaining=ar;row.RouteRemaining=br;}
                    }
                    if(differs)row.DifferingSockets++;if(sameLane)row.SameLaneSockets++;
                }
                result.Add(row);
            }
            return result.ToArray();
        }
    }
}
