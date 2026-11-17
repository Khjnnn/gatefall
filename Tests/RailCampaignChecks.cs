using System;
using System.Linq;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class RailCampaignChecks
    {
        static void Check(bool pass,string why) { if(!pass)throw new Exception("Rail: "+why); }

        public static string Run()
        {
            var p=new CampaignProfile { Growth=new[]{6,4,3}, Research=new[]{4,2,2,2}, ExtraHearts=2 };
            int first=CampaignCatalog.FindStage("C03-S002");
            for(int i=0;i<first;i++){p.BestStars[i]=3;p.FirstCleared[i]=true;p.BestErosion[i]=0;}
            var s=new CampaignService(p,null);
            var rewards=new[]{40,40,38,42,48,38,46};
            var counts=new[]{602,641,600,666,782,590,749};
            var sites=new[]{24,26,24,26,28,24,26};
            var routeIds=new System.Collections.Generic.HashSet<string>();
            for(int j=0;j<rewards.Length;j++) {
                int i=first+j, number=j+2;
                var stage=CampaignCatalog.Stages[i];
                var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,s.Battle(i,false));
                Check(CampaignCatalog.StageId(i)=="3-"+number && stage.PredecessorId=="C03-S"+(number-1).ToString("000"),"stable predecessor");
                Check(s.Profile.IsStageOpen(i) && (number==8 || !s.Profile.IsStageOpen(i+1)),"sequential unlock");
                Check(!stage.Boss && routeIds.Add(stage.Layout.Id) && g.BoardSockets.Length==sites[j] && g.Schedule.Sum(x=>x.Count)==counts[j],"route, site and enemy budgets");
                Check(g.Schedule.All(x=>stage.Lanes.Contains(x.Lane) && g.Roads[x.Lane].Length>=2),"all spawns follow enabled roads");
                CheckBriefing(g,number);
                for(int a=0;a<g.BoardSockets.Length;a++) {
                    var pad=g.BoardSockets[a];
                    float distance=stage.Lanes.Min(l=>BattlefieldLayouts.RoadDistance(pad,g.Roads[l]));
                    Check(distance>=2.1f && distance<3.7f,"reachable road-clear pad");
                    for(int b=0;b<a;b++)Check(V2.Distance(pad,g.BoardSockets[b])>=2.14f,"touch pad spacing");
                }
                Check(s.Profile.IsTowerUnlocked(TowerKind.Piercer)==(number>2),"Piercer gate");
                Check(s.Profile.AllowsRecipe(TowerCatalog.Recipes[11])==(number>4),"Surge gate");
                Check(s.Profile.AllowsRecipe(TowerCatalog.Recipes[12])==(number>5),"Surge to Seal gate");
                Check(s.GrowthReason(0)=="3-8 클리어 후 성장","pre-clear growth lock");
                if(number==6)CheckChain(s,i);
                Check(s.Begin(i,false,out string run),"begin");
                g.Schedule.Clear();g.Start();g.Tick(.1f,new V2());
                Check(g.State==RunState.Won && s.Complete(run,g,out int reward) && reward==rewards[j],"clear and first reward");
                int paid=s.Profile.MagicCores;
                Check(s.Complete(run,g,out reward) && s.Profile.MagicCores==paid && s.Profile.PendingRun=="","idempotent clear");
                s=new CampaignService(CampaignStore.Decode(CampaignStore.Encode(s.Profile)),null);
                Check(s.Profile.FirstCleared[i] && s.Profile.BestStars[i]==3 && s.Profile.LastReward==rewards[j],"stable-ID reload");
            }
            Check(s.Profile.MagicCores==292 && !s.Profile.IsStageOpen(first+7),"seven first rewards; no fake next stage");
            for(int h=0;h<3;h++)Check(s.GrowthPrice(h)==28 && s.Grow(h),"real growth charge");
            Check(s.Profile.MagicCores==208 && s.Profile.Growth.SequenceEqual(new[]{7,5,4}),"account balances after growth");
            CheckLine();
            return "PASS seven Rail stages: campaign/save gates, reachable pads, exact enemy budgets, two-catalyst Surge/Seal, Piercer volley/armor, 292 first reward / growth 28 each.";
        }

        static void CheckBriefing(DefenseSimulation g,int number)
        {
            var fourth=g.GetWaveBriefing(3);
            var fifth=g.GetWaveBriefing(4);
            var sixth=g.GetWaveBriefing(5);
            switch(number) {
            case 2: Check(fourth.LaneCount(0)>=100 && sixth.KindCount(EnemyKind.Brute)>0,"freight follow-up armor");break;
            case 3: Check(fourth.LaneCount(0)>100 && fourth.LaneCount(1)==0 &&
                          fifth.LaneCount(1)>100 && fifth.LaneCount(0)==0,"west then north in alternating tracks");break;
            case 4: Check(fourth.LaneCount(2)>100 && fourth.LastSpawn-fourth.FirstSpawn<=7.01,"short east crowd window");break;
            case 5: Check(fourth.LaneCount(0)>100 && g.Schedule.Any(x=>x.Wave==5 && x.Lane==1 && x.Kind==EnemyKind.Siege),"west crowd and north siege");break;
            case 6: Check(fourth.LaneCount(0)>0 && fourth.LaneCount(2)>0 && fourth.KindCount(EnemyKind.Runner)>0,"parallel west/east attack");break;
            case 7: Check(!g.Schedule.Any(x=>x.Wave<2 && x.Lane==3) && g.Schedule.Any(x=>x.Wave==2 && x.Lane==3 && x.Kind==EnemyKind.Runner) &&
                          g.Schedule.Any(x=>x.Wave==4 && x.Lane==0 && x.Kind==EnemyKind.Siege),"third-wave south and fifth-wave west");break;
            case 8: Check(g.Schedule.Where(x=>x.Wave==4 && x.Kind==EnemyKind.Siege).Max(x=>x.Start+(x.Count-1)*x.Interval) <
                          g.Schedule.Where(x=>x.Wave==4 && x.Swarm).Min(x=>x.Start),"siege ahead of the crowd");break;
            }
        }

        static void CheckChain(CampaignService s,int stage)
        {
            Check(s.SetLoadout((1<<(int)TowerKind.Piercer)|7),"unlocked Piercer can enter fourth loadout slot");
            var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,s.Battle(stage,false));
            var kinds=new[]{TowerKind.Piercer,TowerKind.Binding,TowerKind.Rupture};
            for(int i=0;i<kinds.Length;i++) {
                Check(g.TryBuild(i,kinds[i],null,out string reason),reason);
                Check(g.TryUpgrade(g.TowerAt(i).Id,null,out reason),reason);
            }
            Check(g.Telemetry.CreditsSpent==305 && g.Cores==0,"initial investment");
            g.Start();g.Tick(61,new V2());
            Check(g.TryFuse(11,g.TowerAt(0).Id,g.TowerAt(1).Id,0,null,out string result),result);
            Check(g.TowerAt(0).Kind==TowerKind.Surge && g.TowerAt(1)==null && g.Cores==0,"Surge and first freed pad");
            Check(g.TryBuild(1,TowerKind.Autocannon,null,out result),result);
            Check(g.TryUpgrade(g.TowerAt(0).Id,null,out result),result);
            g.Tick(60,new V2());
            Check(g.TryFuse(12,g.TowerAt(2).Id,g.TowerAt(0).Id,2,null,out result),result);
            Check(g.TowerAt(2).Kind==TowerKind.Seal && g.TowerAt(2).InvestedCores==2 && g.TowerAt(0)==null &&
                g.TowerAt(1).Kind==TowerKind.Autocannon && g.Cores==0 && g.Telemetry.CreditsSpent==540,"Seal and replenished alternative front");
        }

        static void CheckLine()
        {
            var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false,PlayerRange=.01f},7,new CampaignBattle());
            int pad=Enumerable.Range(0,g.BoardSockets.Length).OrderByDescending(i=>g.BoardSockets[i].Length).First();
            Check(g.TryBuild(pad,TowerKind.Piercer,null,out string reason),reason);
            V2 origin=g.TowerAt(pad).Position, forward=(new V2()-origin).Normalized, side=new V2(-forward.Y,forward.X);
            Enemy Add(int id,float along,float across=0)
            {
                var e=new Enemy{Id=id,Lane=0,Position=origin+forward*along+side*across,Health=1000,MaxHealth=1000,Armor=6};
                g.Enemies.Add(e);return e;
            }
            var nearest=Add(901,1);
            var middle=Add(902,1.5f);
            var third=Add(903,2);
            var lead=Add(904,3);
            var aside=Add(905,1.7f,1);
            var behind=Add(906,-1);
            g.Start();g.Tick(.05f,new V2());
            Check(g.Events.Count(e=>e.Kind==EventKind.TowerShot)==1 &&
                new[]{nearest,middle,third}.All(e=>Math.Abs(e.Health-991)<.01f) &&
                new[]{lead,aside,behind}.All(e=>e.Health==1000),"one volley hits the first three armored targets in line only");
        }
    }
}
