using System;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class FourFrontChecks
    {
        static void Require(bool ok,string reason) {if(!ok)throw new Exception(reason);}
        public static string Verify()
        {
            var stage=CampaignCatalog.Stages[9];
            var g=new DefenseSimulation(new GameBalance {OfferCards=false,PlayerDamage=0,PlayerRange=0,CoreHealth=1000000},7,new CampaignBattle {Stage=stage});
            Require(g.OpenLanes.Length==4 && g.Roads.Length==4 && g.GatePositions.Length==4,"four active roads/gates");
            Require(V2.Distance(g.GatePositions[3],new V2(-8,-11))<.001f,"south entrance");
            float shortest=1000;
            foreach(int lane in g.OpenLanes) {
                var road=g.Roads[lane];float length=0;
                Require(V2.Distance(road[0],g.GatePositions[lane])<.001f && road[road.Length-1].Length==0,"road endpoints");
                for(int i=1;i<road.Length;i++)length+=V2.Distance(road[i-1],road[i]);
                shortest=Math.Min(shortest,length);
                Require(length>32,"long approach length");
            }
            for(int wave=0;wave<6;wave++) {
                var brief=g.GetWaveBriefing(wave);
                Require(brief.Total==stage.Counts[wave] && brief.LaneCount(3)>0 && brief.LaneCount(4)==0,"four-lane briefing budget");
                Require(brief.Summary.Contains("남"),"south briefing label");
            }
            bool[,] warnings=new bool[6,4];int southSamples=0;float maxDeviation=0;
            g.Start();
            for(int frame=0;frame<2100;frame++) {
                g.Tick(.1f,new V2());
                foreach(var e in g.Events)if(e.IsEntranceWarning) {Require(!e.IsBossWarning,"ingress misclassified as boss");warnings[e.Wave,(int)e.Value]=true;}
                foreach(var enemy in g.Enemies)if(enemy.Lane==3 && enemy.BlockOwner==0) {
                    float deviation=BattlefieldLayouts.RoadDistance(enemy.Position,g.Roads[3]);
                    maxDeviation=Math.Max(maxDeviation,deviation);southSamples++;
                    Require(deviation<.8f,"south enemy followed wrong road");
                }
                g.Events.Clear();
            }
            for(int wave=0;wave<6;wave++)for(int lane=0;lane<4;lane++)Require(warnings[wave,lane],"missing ingress warning");
            Require(g.Telemetry.Spawned==864 && g.AllWavesSpawned && southSamples>100,"full spawn budget and south flow");
            var boss=new DefenseSimulation(new GameBalance {OfferCards=false,SpawnEnemies=false,PlayerDamage=0,PlayerRange=0},7,new CampaignBattle {Stage=CampaignCatalog.Stages[5]});
            boss.Schedule.Clear();boss.Enemies.Add(new Enemy {Id=9900,Kind=EnemyKind.Boss,Health=10000,MaxHealth=10000,Position=new V2(0,5),BossNext=0});
            boss.Start();boss.Tick(.02f,new V2());bool bossWarning=false;
            foreach(var e in boss.Events)if(e.IsBossWarning) {Require(!e.IsEntranceWarning,"boss misclassified as entrance");bossWarning=true;}
            Require(bossWarning,"boss attack keeps unscheduled warning tag; south rift must filter Wave>=0");
            return "PASS four fronts /24 ingress warnings /864 scheduled enemies /south samples="+southSamples+" max deviation="+maxDeviation.ToString("F3")+" /shortest approach="+shortest.ToString("F2")+" /boss warning distinct. Geometry/flow model, not human play or Android rendering.";
        }
    }
}
