using System;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class EvacuationRouteChecks
    {
        static void Require(bool ok,string reason) {if(!ok)throw new Exception(reason);}
        public static string Verify()
        {
            var stage=CampaignCatalog.Stages[6];
            var g=new DefenseSimulation(new GameBalance {OfferCards=false,PlayerDamage=0,PlayerRange=0,CoreHealth=1000000},7,new CampaignBattle {Stage=stage});
            Require(g.OpenLanes.Length==3 && g.OpenLanes[0]==0 && g.OpenLanes[1]==2 && g.OpenLanes[2]==3,"west/east/south entrances");
            Require(g.Roads[1].Length==0 && g.BoardSockets.Length==24,"north inactive/full24 sites");
            Require(V2.Distance(g.GatePositions[3],new V2(-13,-11))<.001f,"south gate avoids middle call and corner skill controls");
            float west=BattlefieldLayouts.RoadLength(g.Roads[0]),east=BattlefieldLayouts.RoadLength(g.Roads[2]),south=BattlefieldLayouts.RoadLength(g.Roads[3]);
            Require(west>40 && east>35 && south>22 && south<24,"short south versus long flanks");
            for(int wave=0;wave<6;wave++) {
                var brief=g.GetWaveBriefing(wave);
                Require(brief.Total==stage.Counts[wave] && brief.LaneCount(0)>0 && brief.LaneCount(2)>0 && brief.LaneCount(3)>0 && brief.LaneCount(1)==0,"three-way wave budget/briefing");
                Require(brief.Summary.Contains("남"),"south briefing label");
            }
            for(int i=0;i<100;i++)g.Tick(.1f,new V2(1,1)); // avoid body-blocking in the road-flow check
            bool[,] warnings=new bool[6,4];int samples=0;float deviation=0;
            g.Start();
            for(int frame=0;frame<2000;frame++) {
                g.Tick(.1f,new V2());
                foreach(var e in g.Events)if(e.IsEntranceWarning) {Require(!e.IsBossWarning,"south warning classified as boss");warnings[e.Wave,(int)e.Value]=true;}
                foreach(var enemy in g.Enemies)if(enemy.BlockOwner==0) {
                    float d=BattlefieldLayouts.RoadDistance(enemy.Position,g.Roads[enemy.Lane]);samples++;deviation=Math.Max(deviation,d);
                    Require(d<.8f && Math.Abs(enemy.Position.X)<=g.HalfWidth && Math.Abs(enemy.Position.Y)<=g.HalfHeight,"actor off authored road/map");
                }
                g.Events.Clear();
            }
            for(int wave=0;wave<6;wave++)foreach(int lane in g.OpenLanes)Require(warnings[wave,lane],"missing entrance warning");
            Require(g.Telemetry.Spawned==552 && g.AllWavesSpawned,"exact full552 budget");
            return "PASS1-7 W/E/S18 entrance warnings /552 scheduled /24 sites /short south="+south.ToString("F2")+" vs west="+west.ToString("F2")+" east="+east.ToString("F2")+" /road samples="+samples+" max deviation="+deviation.ToString("F3")+". Model flow, not Android rendering or human fun.";
        }
    }
}
