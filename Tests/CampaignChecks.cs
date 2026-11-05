using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class CampaignChecks
    {
        static readonly List<string> output=new List<string>();
        static int count;
        static void Assert(bool v,string why) {if(!v)throw new Exception(why);}
        static void Near(float a,float b) {Assert(Math.Abs(a-b)<.02f,a+" != "+b);}
        static void Check(string name,Action action) {action();count++;output.Add("PASS "+name);}
        static float RoadDistance(V2 p,V2[] road) {
            float best=float.MaxValue;
            for(int i=1;i<road.Length;i++) {
                V2 a=road[i-1],b=road[i];float x=b.X-a.X,y=b.Y-a.Y;
                float t=Math.Max(0,Math.Min(1,((p.X-a.X)*x+(p.Y-a.Y)*y)/(x*x+y*y)));
                best=Math.Min(best,V2.Distance(p,new V2(a.X+t*x,a.Y+t*y)));
            }
            return best;
        }
        static DefenseSimulation New(int stage=-1,bool spawn=false,int credits=140,bool cards=false)
        {return new DefenseSimulation(new GameBalance {SpawnEnemies=spawn,OfferCards=cards,StartingCredits=credits},7,new CampaignBattle {Stage=stage<0 ? null : CampaignCatalog.Stages[stage]});}
        static void Move(DefenseSimulation g,int slot)
        {
            for(int i=0;i<1600 && V2.Distance(g.PlayerPosition,g.BoardSockets[slot])>.15f;i++) {
                V2 d=Steer(g.PlayerPosition,g.BoardSockets[slot]);
                g.Tick(1f/60,d.Normalized);g.Events.Clear();
            }
            Assert(V2.Distance(g.PlayerPosition,g.BoardSockets[slot])<DefenseSimulation.BuildDistance,"cannot reach slot "+slot);
        }
        static V2 Steer(V2 position,V2 destination)
        {
            V2 d=destination-position;
            float projection=-(position.X*d.X+position.Y*d.Y)/Math.Max(.001f,d.X*d.X+d.Y*d.Y);
            if(projection>0 && projection<1 && (position+d*projection).Length<1.6f) {
                V2 tangent=new V2(-position.Y,position.X).Normalized;
                if(tangent.X*d.X+tangent.Y*d.Y<0)tangent=tangent*-1;
                d=tangent+position.Normalized*.25f;
            }
            return d;
        }
        static Tower Build(DefenseSimulation g,int slot,TowerKind kind,bool level2=false)
        {Move(g,slot);string r;Assert(g.TryBuild(slot,kind,null,out r),r);var t=g.TowerAt(slot);if(level2)Assert(g.TryUpgrade(t.Id,null,out r),r);return t;}
        static DefenseSimulation Win(int stage)
        {
            var g=New(stage);
            g.Schedule.Clear(); // Persistence fixture: no pending waves; real spawning is covered separately.
            if(stage==5)g.Enemies.Add(new Enemy {Id=999,Kind=EnemyKind.Boss,Health=1,MaxHealth=1,Position=g.PlayerPosition+new V2(1,0),Speed=0});
            g.Start();g.Tick(180,new V2());Assert(g.State==RunState.Won,"fixture failed to win");return g;
        }
        static CampaignService ClearedProfile()
        {
            var svc=new CampaignService(new CampaignProfile(),null);
            for(int i=0;i<6;i++) {string id;Assert(svc.Begin(i,false,out id),"begin");int reward;Assert(svc.Complete(id,Win(i),out reward),"complete");}
            return svc;
        }
        static byte[] LegacyV1(CampaignProfile p)
        {
            byte[] payload;
            using(var mem=new MemoryStream()) {
                using(var w=new BinaryWriter(mem,Encoding.UTF8,true)) {
                    w.Write("GATEFALL-RAON");w.Write(1);w.Write(p.MagicCores);w.Write(p.SelectedHunter);w.Write(p.StoryMask);w.Write(p.TutorialMask);w.Write(p.Muted);
                    for(int i=0;i<6;i++){w.Write(p.BestStars[i]);w.Write(p.CrisisStars[i]);w.Write(p.BestErosion[i]);w.Write(p.FirstCleared[i]);}
                    foreach(int x in p.Research)w.Write(x);foreach(int x in p.Growth)w.Write(x);
                    w.Write(p.PendingRun);w.Write(p.PendingStage);w.Write(p.PendingCrisis);w.Write(p.LastRewardRun);w.Write(p.LastReward);
                }payload=mem.ToArray();
            }
            byte[] hash;using(var sha=SHA256.Create())hash=sha.ComputeHash(payload);
            byte[] data=new byte[hash.Length+payload.Length];Buffer.BlockCopy(hash,0,data,0,hash.Length);Buffer.BlockCopy(payload,0,data,hash.Length,payload.Length);return data;
        }
        public static string RunAll(string directory)
        {
            output.Clear();count=0;
            Check("Learning cues remain on retries until first clear and preserve old saves",()=>{
                var p=new CampaignProfile();
                Assert(p.ShouldShowLesson(0),"first attempt");
                p.TutorialMask |= 1;
                var retry=CampaignStore.Decode(CampaignStore.Encode(p));
                Assert(retry.ShouldShowLesson(0),"failed retry");
                retry.FirstCleared[0]=true;retry.BestStars[0]=1;
                Assert(!retry.ShouldShowLesson(0),"cleared replay");
                retry.TutorialMask=0;
                Assert(retry.ShouldShowLesson(0),"old save without tutorial flag");
                Assert(!retry.ShouldShowLesson(-1) && !retry.ShouldShowLesson(14),"invalid stage");
            });
            Check("Legacy six-stage profile migrates without changing progress and v2 roundtrips",()=>{
                var original=ClearedProfile().Profile;original.SelectedHunter=1;original.Research[0]=2;original.Muted=true;
                var migrated=CampaignStore.Decode(LegacyV1(original));
                Assert(migrated.MagicCores==original.MagicCores && migrated.SelectedHunter==1 && migrated.Research[0]==2 && migrated.Muted,"legacy fields");
                for(int i=0;i<6;i++)Assert(migrated.BestStars[i]==original.BestStars[i] && migrated.FirstCleared[i],"legacy stage");
                Assert(migrated.IsStageOpen(6) && !migrated.IsStageOpen(7) && migrated.Loadout==135,"new frontier/default loadout");
                var saved=CampaignStore.Decode(CampaignStore.Encode(migrated));Assert(saved.IsStageOpen(6) && saved.Loadout==135,"v2 roundtrip");
            });
            Check("New stages unlock marker and observer after their clears and validate loadout",()=>{
                var svc=ClearedProfile();string id,reason;int reward;
                for(int i=6;i<=9;i++){Assert(svc.Begin(i,false,out id),"begin "+i);Assert(svc.Complete(id,Win(i),out reward),"complete "+i);}
                Assert(svc.Profile.FirstCleared[9] && svc.Profile.IsStageOpen(10),"marker unlock");
                Assert(!svc.SetLoadout(135|(1<<8)),"five towers accepted");
                Assert(svc.SetLoadout((135&~(1<<7))|(1<<8)),"marker swap");
                var battle=new DefenseSimulation(new GameBalance(),7,svc.Battle(10,false));
                Assert(battle.Allows(TowerKind.Marker) && !battle.Allows(TowerKind.Barrier) && !battle.Allows(TowerKind.Observer),"loadout or recipe gate");
                var finalStage=CampaignCatalog.Stages[13];
                Assert(finalStage.CityBriefing(svc.Profile)==finalStage.Briefing,"Observer preview must announce the first unlock");
                for(int i=10;i<=13;i++){Assert(svc.Begin(i,false,out id),"begin "+i);Assert(svc.Complete(id,Win(i),out reward),"complete "+i);}
                Assert(finalStage.CityBriefing(svc.Profile).Contains("표식 Lv2 + 창 Lv2"),"Observer replay recipe");
                var withoutMarker=svc.Profile.Copy();withoutMarker.Loadout=135;
                var withoutSpear=svc.Profile.Copy();withoutSpear.Loadout&=~(1<<(int)TowerKind.Autocannon);
                foreach(var incomplete in new[]{withoutMarker,withoutSpear})
                    Assert(incomplete.Valid() && finalStage.CityBriefing(incomplete).Contains("편성에 표식과 창"),"Observer replay missing materials");
                battle=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false,StartingCredits=1000},7,svc.Battle(13,false));
                Assert(battle.Allows(TowerKind.Observer),"observer unlock");
                Assert(battle.TryBuild(1,TowerKind.Marker,null,out reason),reason);Assert(battle.TryBuild(2,TowerKind.Autocannon,null,out reason),reason);
                var a=battle.TowerAt(1);var b=battle.TowerAt(2);Assert(battle.TryUpgrade(a.Id,null,out reason),reason);Assert(battle.TryUpgrade(b.Id,null,out reason),reason);
                battle.Start();battle.Tick(60,new V2());Assert(battle.TryFuse(6,a.Id,b.Id,1,null,out reason),reason);
                Assert(battle.TowerAt(1).Kind==TowerKind.Observer && battle.TowerAt(2)==null && battle.Cores==0,"observer fusion");
                Assert(CampaignStore.Decode(CampaignStore.Encode(svc.Profile)).Loadout==svc.Profile.Loadout,"saved loadout");
            });
            Check("The Observer capstone needs the unlock, both catalysts and the same 455 investment",()=>{
                var profile=new CampaignProfile {Loadout=261};
                for(int i=0;i<13;i++){profile.FirstCleared[i]=true;profile.BestStars[i]=3;}
                Assert(profile.Valid() && !profile.LoadoutResults().Contains(TowerKind.Seal),"locked Observer route in loadout");
                profile.FirstCleared[13]=true;profile.BestStars[13]=3;
                Assert(profile.Valid() && profile.LoadoutResults().Contains(TowerKind.Seal),"no-Binding loadout route missing");
                var service=new CampaignService(profile,null);
                foreach(int anchor in new[]{1,4}) {
                    var g=new DefenseSimulation(new GameBalance {SpawnEnemies=false,OfferCards=false,StartingCredits=1000},7,service.Battle(13,false));
                    Assert((g.ReachableTowerFamilies() & (1<<(int)TowerKind.Seal))!=0,"runtime reachability disagrees");
                    string why;
                    Assert(g.TryBuild(1,TowerKind.Autocannon,null,out why),why);
                    Assert(g.TryBuild(13,TowerKind.Marker,null,out why),why);
                    Assert(g.TryBuild(4,TowerKind.Rupture,null,out why),why);
                    var spear=g.TowerAt(1);var marker=g.TowerAt(13);var rupture=g.TowerAt(4);
                    Assert(g.TryUpgrade(spear.Id,null,out why),why);
                    Assert(g.TryUpgrade(marker.Id,null,out why),why);
                    Assert(g.TryUpgrade(rupture.Id,null,out why),why);
                    g.Start();g.Tick(60.05f,new V2());
                    Assert(g.TryFuse(6,spear.Id,marker.Id,1,null,out why),why);
                    var observer=g.TowerAt(1);
                    Assert(g.TryUpgrade(observer.Id,null,out why),why);
                    int credits=g.Credits;
                    Assert(!g.TryFuse(8,observer.Id,rupture.Id,anchor,"seal",out why) && why.Contains("촉매"),"second catalyst missing");
                    Assert(g.Credits==credits && g.Towers.Count==2 && g.Cores==0,"unfunded capstone changed state");
                    g.Tick(60.1f,new V2());
                    Assert(g.TryFuse(8,observer.Id,rupture.Id,anchor,"seal",out why),why);
                    var seal=g.TowerAt(anchor);
                    Assert(seal.Kind==TowerKind.Seal && seal.Level==1 && g.Towers.Count==1 && g.Cores==0,"capstone result");
                    Assert(seal.PaidCredits==455 && seal.InvestedCores==2 && g.SaleRefund(seal)==341 && g.Telemetry.CreditsSpent==455,"capstone investment");
                }
            });
            Check("Unlock victory messages follow the pre-battle snapshot and distinguish crisis",()=>{
                foreach(int stage in new[]{9,13}) {
                    var first=new CampaignBattle { Stage=CampaignCatalog.Stages[stage], MarkerUnlocked=stage==13 };
                    Assert(first.VictorySummary()==first.Stage.ClearStory.Split('\n')[0],"first clear announcement");
                    var replay=first.Copy();replay.MarkerUnlocked=true;replay.ObserverUnlocked=stage==13;
                    Assert(replay.VictorySummary().Contains("다시") && !replay.VictorySummary().Contains("해금"),"replay unlock announcement");
                    replay.Crisis=true;Assert(replay.VictorySummary()=="위기 작전 별점에 기록됩니다.","crisis result");
                    Assert(first.VictorySummary().Contains("해금"),"replay mutated the first snapshot");
                }
                var normal=new CampaignBattle {Stage=CampaignCatalog.Stages[11],MarkerUnlocked=true,ObserverUnlocked=true};
                Assert(normal.VictorySummary()==normal.Stage.ClearStory.Split('\n')[0],"unrelated result");
            });
            Check("Late operations permit Lv3 materials and a multi-target Beacon fusion",()=>{
                string reason;
                var early=New(9,false,1000);
                Assert(early.TryBuild(0,TowerKind.Autocannon,null,out reason),reason);
                var spear=early.TowerAt(0);
                Assert(early.TryUpgrade(spear.Id,null,out reason),reason);
                int before=early.Credits;
                Assert(!early.TryUpgrade(spear.Id,null,out reason) && early.Credits==before && spear.Level==2,"early Lv3 escaped gate");
                var battle=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false,StartingCredits=1000,PlayerDamage=0},7,
                    new CampaignBattle{Stage=CampaignCatalog.Stages[10],Loadout=258,MarkerUnlocked=true});
                Assert(battle.TryBuild(0,TowerKind.Marker,null,out reason),reason);
                Assert(battle.TryBuild(1,TowerKind.Binding,null,out reason),reason);
                var marker=battle.TowerAt(0);var binding=battle.TowerAt(1);
                Assert(battle.TryUpgrade(marker.Id,null,out reason),reason);
                Assert(battle.TryUpgrade(binding.Id,null,out reason),reason);
                before=battle.Credits;
                Assert(battle.TryUpgrade(binding.Id,null,out reason),reason);
                Assert(binding.Level==3 && battle.Credits==before-75,"Lv3 price or level");
                Assert(!battle.TryUpgrade(binding.Id,null,out reason),"Lv4 accepted");
                battle.Start();battle.Tick(60,new V2());
                Assert(battle.TryFuse(7,marker.Id,binding.Id,0,"tracking",out reason),reason);
                var result=battle.TowerAt(0);
                Assert(result.Kind==TowerKind.Beacon && result.Level==1 && result.PaidCredits==295 &&
                    result.InvestedCores==1 && battle.Cores==0 && battle.TowerAt(1)==null,"Beacon cost, anchor or consumption");
                for(int i=0;i<4;i++) battle.Enemies.Add(new Enemy{Id=900+i,Health=1000,MaxHealth=1000,Position=result.Position+new V2(1+i*.1f,0)});
                battle.Tick(2.4f,new V2());
                Assert(battle.Enemies.FindAll(e=>e.MarkBonus==5 && Math.Abs(e.SlowFraction-.3f)<.001f).Count==3,"Beacon volley must mark and slow three distinct targets");
                Assert(battle.Telemetry.MarkedHits==0,"Beacon consumed its own mark");
                Assert(battle.TryUpgrade(result.Id,null,out reason),reason);
                Assert(battle.TryUpgrade(result.Id,null,out reason),reason);
                Assert(battle.Stats(result).MaxTargets==4 && battle.Stats(result).MarkBonus==6,"Beacon Lv3 stats");
            });
            Check("Marker applies a finite non-stacking damage bonus to tower shots",()=>{
                var g=New(-1,false,1000);g.Balance.PlayerDamage=0;string reason;
                Assert(g.TryBuild(4,TowerKind.Marker,null,out reason),reason);g.Start();
                var enemy=new Enemy{Id=9999,Health=1000,MaxHealth=1000,Position=new V2(-1,-2),Speed=0};g.Enemies.Add(enemy);
                g.Tick(1,new V2());Assert(enemy.MarkRemaining>0 && enemy.MarkBonus==3,"marker failed");
                g.TowerAt(4).Commission=100;Assert(g.TryBuild(7,TowerKind.Autocannon,null,out reason),reason);g.Tick(2.0f,new V2());
                Assert(g.Telemetry.TowerDamage[(int)TowerKind.Autocannon]>=13,"marked bonus missing: "+g.Telemetry.TowerDamage[(int)TowerKind.Autocannon]+" hp="+enemy.Health+" mark="+enemy.MarkRemaining+" cannon="+g.TowerAt(7).Commission+" cooldown="+g.TowerAt(7).Cooldown);
                Assert(g.Telemetry.MarkedHits>0 && g.Telemetry.MarkBonusDamage>=3 && g.Telemetry.MarkBonusDamage<=g.Telemetry.MarkedHits*3,"mark contribution accounting");
                var followUps=g.Events.FindAll(e=>e.Kind==EventKind.MarkFollowUp);
                Assert(followUps.Count==g.Telemetry.MarkedHits && followUps.TrueForAll(e=>e.Source==(int)TowerKind.Autocannon && e.Value==3),"only effective allied follow-ups should produce the reaction");
                g.TowerAt(7).Commission=100;g.Tick(4,new V2());Assert(enemy.MarkRemaining==0 && enemy.MarkBonus==0,"mark did not expire");
            });
            Check("Marker prioritizes the foremost target reachable by an active allied attack tower",()=>{
                string reason;var g=New(-1,false,1000);g.Balance.PlayerDamage=0;
                Assert(g.TryBuild(4,TowerKind.Marker,null,out reason),reason);Assert(g.TryBuild(2,TowerKind.Autocannon,null,out reason),reason);
                var mark=g.TowerAt(4);var ally=g.TowerAt(2);mark.Commission=0;ally.Commission=0;ally.Cooldown=100;
                var lead=new Enemy{Id=8001,Health=1000,MaxHealth=1000,Position=new V2(3,0),Speed=0};
                var covered=new Enemy{Id=8002,Health=1000,MaxHealth=1000,Position=new V2(-3,-2),Speed=0};
                g.Enemies.Add(lead);g.Enemies.Add(covered);g.Start();g.Tick(.6f,new V2());
                Assert(covered.MarkRemaining>0 && lead.MarkRemaining==0,"did not prefer a reachable allied target");
                g=New(-1,false,1000);g.Balance.PlayerDamage=0;Assert(g.TryBuild(4,TowerKind.Marker,null,out reason),reason);g.TowerAt(4).Commission=0;
                lead=new Enemy{Id=8001,Health=1000,MaxHealth=1000,Position=new V2(3,0),Speed=0};
                covered=new Enemy{Id=8002,Health=1000,MaxHealth=1000,Position=new V2(-3,-2),Speed=0};
                g.Enemies.Add(lead);g.Enemies.Add(covered);g.Start();g.Tick(.6f,new V2());
                Assert(lead.MarkRemaining>0 && covered.MarkRemaining==0,"no-ally fallback is not lead target");
            });
            Check("Loadout preview accounts for selected ingredients and unlocked recipes",()=>{
                var p=new CampaignProfile();
                var results=p.LoadoutResults();
                Assert(results.Contains(TowerKind.Arc) && results.Contains(TowerKind.Shatter) && results.Contains(TowerKind.Gravity) && results.Contains(TowerKind.Seal),"existing fusion paths");
                Assert(!results.Contains(TowerKind.Observer),"locked observer listed");
                p.Loadout=(1<<(int)TowerKind.Autocannon)|(1<<(int)TowerKind.Marker);
                Assert(p.LoadoutResults().Count==0,"locked result shown");
                p.FirstCleared[13]=true;
                results=p.LoadoutResults();Assert(results.Count==1 && results[0]==TowerKind.Observer,"observer missing or other results leaked");
                p.Loadout=1<<(int)TowerKind.Marker;Assert(p.LoadoutResults().Count==0,"missing ingredient ignored");
            });
            Check("Stage roads, sockets and exact enemy budgets",()=>{
                int[] sums={330,460,450,768,780,883};
                for(int i=0;i<6;i++) {var g=New(i);int n=0;foreach(var s in g.Schedule){n+=s.Count;Assert(Array.IndexOf(g.OpenLanes,s.Lane)>=0,"spawn on closed lane");}
                    Assert(n==sums[i] && g.BoardSockets.Length==CampaignCatalog.Stages[i].SocketCount,"budget/board");}
            });
            Check("All 14 boards keep pads outside roads and in range of active lanes",()=>{
                for(int stage=0;stage<14;stage++) {var g=New(stage);
                    foreach(V2 pad in g.BoardSockets) {
                        float any=float.MaxValue,active=float.MaxValue;
                        for(int lane=0;lane<g.Roads.Length;lane++) {float d=RoadDistance(pad,g.Roads[lane]);any=Math.Min(any,d);if(Array.IndexOf(g.OpenLanes,lane)>=0)active=Math.Min(active,d);}
                        float reach=stage>=4 ? TowerCatalog.Get(TowerKind.Barrier).Levels[0].Range : TowerCatalog.Get(TowerKind.Binding).Levels[0].Range;
                        Assert(any>=2.1f && active<=reach,"stage "+(stage+1)+" pad "+pad.X+","+pad.Y+" road "+any+" active "+active);
                    }
                    for(int i=0;i<g.BoardSockets.Length;i++)for(int j=0;j<i;j++)Assert(V2.Distance(g.BoardSockets[i],g.BoardSockets[j])>=2.55f,"pads too close in stage "+(stage+1));
                }
            });
            Check("Campaign wave supplies fund more construction without changing lab economy",()=>{
                var g=New(2,false,140);g.Start();g.Tick(150,new V2());Assert(g.Credits==640 && g.Cores==2,"campaign supplies");
                var lab=New(-1,false,140);lab.Start();lab.Tick(150,new V2());Assert(lab.Credits==340 && lab.Cores==2,"lab supply changed");
            });
            Check("Hunter death releases enemies, leaves defense active, and respawns after ten combat seconds",()=>{
                var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false,PlayerHealth=7,PlayerDamage=0},7,new CampaignBattle{Stage=CampaignCatalog.Stages[4]});
                g.Start(); V2 location=g.PlayerPosition;
                var attacker=new Enemy{Id=901,Kind=EnemyKind.Grunt,Health=1000,MaxHealth=1000,Position=location+new V2(.4f,0),Speed=0};g.Enemies.Add(attacker);
                g.Tick(.05f,new V2());Assert(g.HunterDown && g.State==RunState.Running && attacker.BlockOwner==0,"death or block release");
                Assert(g.Events.FindAll(e=>e.Kind==EventKind.HunterDown).Count==1,"down event");
                float waiting=g.HunterRespawnRemaining;g.TogglePause();g.Tick(5,new V2(1,0));Near(g.HunterRespawnRemaining,waiting);g.TogglePause();
                string why;Assert(g.TryBuild(0,TowerKind.Autocannon,null,out why),why);Assert(!g.UseSkill(),"skill while down");
                g.Enemies.Clear();g.Events.Clear();g.Tick(9.8f,new V2(1,0));Assert(g.HunterDown && g.State==RunState.Running && g.HunterRespawnRemaining>0,"revived early");
                Near(V2.Distance(g.PlayerPosition,location),0);Assert(!g.Events.Exists(e=>e.Kind==EventKind.HunterShot),"down hunter fired");
                g.Tick(.3f,new V2());Assert(!g.HunterDown && g.PlayerHealth==g.Balance.PlayerHealth && g.HunterInvulnerability>1,"revival health or protection");
                Near(V2.Distance(g.PlayerPosition,new V2(0,-2.2f)),0);Assert(g.Events.FindAll(e=>e.Kind==EventKind.HunterRevived).Count==1,"revival event");
            });
            Check("Core destruction still loses during hunter respawn",()=>{
                var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false,PlayerHealth=7,PlayerDamage=0,CoreHealth=1},7,new CampaignBattle{Stage=CampaignCatalog.Stages[4]});g.Start();
                g.Enemies.Add(new Enemy{Id=902,Kind=EnemyKind.Grunt,Health=1000,MaxHealth=1000,Position=g.PlayerPosition+new V2(.4f,0),Speed=0});g.Tick(.05f,new V2());
                Assert(g.HunterDown && g.State==RunState.Running,"hunter should be down first");g.Enemies.Clear();
                g.Enemies.Add(new Enemy{Id=903,Kind=EnemyKind.Siege,Health=1000,MaxHealth=1000,Position=new V2(1.1f,0),Speed=0,Waypoint=g.Roads[0].Length-1});g.Tick(.05f,new V2());
                Assert(g.State==RunState.Lost && g.CoreHealth<=0,"core defeat bypassed");
            });
            Check("Campaign core damage preserves its entrance lane without becoming an entrance or boss warning",()=>{
                for(int lane=0;lane<3;lane++) {
                    var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false,PlayerDamage=0},7,new CampaignBattle{Stage=CampaignCatalog.Stages[12]});g.Start();g.Events.Clear();
                    g.Enemies.Add(new Enemy{Id=980+lane,Lane=lane,Kind=EnemyKind.Siege,Health=1000,MaxHealth=1000,Position=new V2(1.1f,0),Speed=0,Waypoint=g.Roads[lane].Length-1});
                    g.Tick(.05f,new V2());var hits=g.Events.FindAll(e=>e.Kind==EventKind.CoreHurt);
                    Assert(hits.Count==1 && hits[0].Source==lane,"missing damage entrance "+lane);Near(hits[0].Value,24);Near(g.CoreHealth,276);
                    Assert(hits[0].Wave==-1 && !hits[0].IsEntranceWarning && !hits[0].IsBossWarning,"damage became a warning");
                }
            });
            Check("Wave briefings describe actual multi-lane counts without changing the battle",()=>{
                var g=New(3,true);int count=g.Schedule.Count,credits=g.Credits;var b=g.GetWaveBriefing(0);
                Assert(b.Total==60 && b.LaneCount(0)==24 && b.LaneCount(1)==18 && b.LaneCount(2)==18,"first wide wave lanes");
                Assert(b.Threat==EnemyKind.Runner && b.KindCount(EnemyKind.Runner)==3 && b.Summary.Contains("서24·북18·동18"),"first wide wave threat");
                Assert(g.Elapsed==0 && g.Telemetry.Spawned==0 && g.Schedule.Count==count && g.Credits==credits,"briefing mutated combat");
                Assert(g.GetWaveBriefing(-1)==null && g.GetWaveBriefing(6)==null,"unknown wave briefing");
                g.Start();g.Tick(8,new V2());
                foreach(var e in g.Events)if(e.Kind==EventKind.Warning && e.Value<3)Assert(e.Wave==0 && e.Source==-1,"late entrance warning lost its wave/source");
                Assert(g.Events.Exists(e=>e.Kind==EventKind.Warning && e.Value==2),"third entrance warning missing");
            });
            Check("Boss wave briefing includes its earlier spawn and extra north enemy",()=>{
                var g=New(5);var b=g.GetWaveBriefing(5);
                Assert(b.Total==217 && b.LaneCount(0)==72 && b.LaneCount(1)==73 && b.LaneCount(2)==72,"boss wave budget");
                Assert(b.KindCount(EnemyKind.Boss)==1 && b.Threat==EnemyKind.Boss && b.FirstSpawn==150 && Math.Abs(b.LastSpawn-177)<.001,"boss spawn window");
            });
            Check("An early boss entrance warning names wave6 while the clock is still on wave5",()=>{
                var g=New(5,true);g.Schedule.Clear();g.Schedule.Add(new SpawnGroup{Wave=5,Lane=1,Kind=EnemyKind.Boss,Count=1,Start=150,Interval=1});
                g.Start();g.Tick(147.1f,new V2());
                Assert(g.Wave==4 && g.Events.Exists(e=>e.Kind==EventKind.Warning && e.Value==1 && e.Wave==5 && e.Source==-1),"warning used current clock slot");
                g.Events.Clear();g.Tick(2,new V2());Assert(!g.Events.Exists(e=>e.Kind==EventKind.Warning),"duplicate entrance warning");
            });
            Check("Chapter operations telegraph their distinct enemy and lane pressures",()=>{
                var one=New(0);Assert(one.Schedule.Exists(x=>x.Wave>=2 && x.Kind==EnemyKind.Runner),"late runners missing");
                var two=New(1);for(int wave=1;wave<6;wave++) {
                    var groups=two.Schedule.FindAll(x=>x.Wave==wave);int main=two.OpenLanes[wave%2];
                    Assert(groups.FindAll(x=>x.Lane==main).Count>groups.FindAll(x=>x.Lane!=main).Count,"alternating rush");
                    foreach(int lane in two.OpenLanes)Assert(groups.FindAll(x=>x.Lane==lane && !x.Warned).Count==1,"lane warning");
                }
                var three=New(2);Assert(three.Schedule.FindAll(x=>x.Wave==3 && x.Kind==EnemyKind.Brute).Count>=3,"elite pressure");
                var four=New(3);Assert(four.OpenLanes.Length==3 && four.BoardSockets.Length==24,"wide map");
                var five=New(4);Assert(five.Schedule.FindAll(x=>x.Wave==4 && x.Kind==EnemyKind.Siege).Count>=2,"siege pressure");
                var packs=five.Schedule.FindAll(x=>x.Wave==4);Assert(Math.Abs(packs[2].Start-packs[0].Start-.25)<.001 && Math.Abs(packs[4].Start-packs[2].Start-.25)<.001 && packs[0].Lane==packs[4].Lane && packs[6].Lane!=packs[0].Lane,"alternating grouped rush");
                Assert(packs[1].Kind==EnemyKind.Grunt && packs[1].Swarm && packs[1].Count==4+1 && packs[1].Lane==packs[0].Lane && Math.Abs(packs[1].Start-packs[0].Start-.25/6)<.001,"pack companions");
                foreach(int lane in five.OpenLanes)Assert(packs.FindAll(x=>x.Lane==lane && !x.Warned).Count==1,"packed lane warning");
                Near(five.Stats(TowerKind.Autocannon,1).Range,5.2f);
                var six=New(5);Assert(six.Schedule.Exists(x=>x.Kind==EnemyKind.Boss && x.Start==150),"final boss");
            });
            Check("Marker training operation sustains elites on both entrances without changing its enemy budget",()=>{
                var g=New(10);int total=0;foreach(var group in g.Schedule)total+=group.Count;
                Assert(total==816 && g.OpenLanes.Length==2,"stage 1-11 budget or lanes");
                for(int wave=2;wave<6;wave++) {
                    var elites=g.Schedule.FindAll(x=>x.Wave==wave && x.Kind==EnemyKind.Brute);
                    Assert(elites.Count>=4 && elites.Exists(x=>x.Lane==0) && elites.Exists(x=>x.Lane==2),"missing dual lane elite pressure");
                    foreach(int lane in g.OpenLanes)Assert(g.Schedule.FindAll(x=>x.Wave==wave && x.Lane==lane && !x.Warned).Count==1,"stage 1-11 warning");
                }
            });
            Check("Expanded 1-4 bounds and 24 sockets support real construction and fusion",()=>{
                var g=New(3,false,1000); string why;
                Assert(g.ExpandedArena && g.HalfWidth==24 && g.HalfHeight==12 && g.BoardSockets.Length==24,"expanded geometry");
                Assert(g.GatePositions[0].X==-23 && g.GatePositions[2].X==23,"entry positions");
                Assert(g.TryBuild(10,TowerKind.Autocannon,"wide-a",out why),why);
                Assert(g.TryBuild(11,TowerKind.Binding,"wide-b",out why),why);
                var a=g.TowerAt(10);var b=g.TowerAt(11);
                Assert(g.TryUpgrade(a.Id,"wide-up-a",out why),why);Assert(g.TryUpgrade(b.Id,"wide-up-b",out why),why);
                for(int i=0;i<100;i++)g.Tick(.1f,new V2(1,0));
                Assert(g.PlayerPosition.X>18 && g.PlayerPosition.X<=24,"wide player movement");
                g.Start();g.Tick(60,new V2());g.TogglePause();
                Assert(g.TryFuse(0,a.Id,b.Id,10,"wide-fuse",out why),why);
                Assert(g.TowerAt(10).Kind==TowerKind.Arc && g.TowerAt(11)==null,"high-index fusion");
                Assert(New(2).BoardSockets.Length==14 && New(4).BoardSockets.Length==24,"expanded standard boards");
                Assert(New(3).Credits==280,"expanded stage starting supply");
            });
            Check("Preparation moves without time, enemy or supply advancement",()=>{
                var g=New(0);V2 before=g.PlayerPosition;g.Tick(60,new V2(1,0));
                Assert(V2.Distance(before,g.PlayerPosition)>0,"no preparation movement");Near((float)g.Elapsed,0);Assert(g.Credits==240 && g.Enemies.Count==0,"preparation advanced combat");
            });
            Check("Preparation refunds100%, combat refunds75%, locked family is atomic",()=>{
                var g=New(0);var t=Build(g,0,TowerKind.Autocannon,true);string reason;
                Assert(!g.TryBuild(0,TowerKind.Barrier,null,out reason) && g.Credits==170,"locked transaction");
                Assert(g.SaleRefund(t)==70,"prepare refund");Assert(g.TrySell(t.Id,null,out reason),"sell");Assert(g.Credits==240,"refund");
                t=Build(g,0,TowerKind.Autocannon,true);g.Start();Assert(g.SaleRefund(t)==52,"combat refund");
            });
            Check("Mobile remote upgrade fusion and sale preserve position and transaction ownership",()=>{
                var g=New(-1,false,1000);V2 p=g.PlayerPosition;string reason;
                Assert(V2.Distance(p,g.BoardSockets[7])>DefenseSimulation.BuildDistance,"fixture must be remote");
                Assert(g.TryBuild(7,TowerKind.Autocannon,"remote-build",out reason),reason);var a=g.TowerAt(7);
                Assert(g.TryUpgrade(a.Id,"remote-upgrade",out reason),reason);
                Assert(g.TryBuild(1,TowerKind.Binding,null,out reason),reason);var b=g.TowerAt(1);
                Assert(g.TryUpgrade(b.Id,null,out reason),reason);g.Start();g.Tick(60,new V2());g.TogglePause();
                Assert(g.TryFuse(0,a.Id,b.Id,7,"remote-fusion",out reason),reason);
                var result=g.TowerAt(7);Assert(result.Kind==TowerKind.Arc && g.TowerAt(1)==null,"fusion result/consumption");
                int credits=g.Credits;Assert(!g.TryFuse(0,a.Id,b.Id,7,"remote-fusion",out reason) && g.Credits==credits,"duplicate fusion");
                Assert(g.TrySell(result.Id,"remote-sale",out reason),reason);
                Assert(g.Towers.Count==0 && g.Cores==1 && g.Credits==credits+146,"refund investment");
                Near(V2.Distance(p,g.PlayerPosition),0);Near((float)g.Elapsed,60);
            });
            Check("Locked recipe and catalysts follow stage unlocks",()=>{
                var g=New(1,false,1000);var a=Build(g,0,TowerKind.Autocannon,true);var b=Build(g,2,TowerKind.Binding,true);g.Start();g.Tick(121,new V2());
                Assert(g.Cores==0 && g.CanFuse(0,a.Id,b.Id,a.Socket).Contains("해금"),"locked fusion or catalyst leaked");
            });
            Check("Card pool excludes locked control and blast",()=>{
                var g=New(0,false,140,true);g.Start();g.Tick(30,new V2());Assert(g.AwaitingCard,"no cards");
                Assert(!g.CardOffers.Contains(CardKind.Control) && !g.CardOffers.Contains(CardKind.Blast),"locked card");
            });
            Check("Card offers follow reachable tower tags after loadout selection",()=>{
                int noSpear=(1<<(int)TowerKind.Binding)|(1<<(int)TowerKind.Rupture)|(1<<(int)TowerKind.Marker);
                var observed=new HashSet<CardKind>();
                foreach(int seed in new[]{7,19,41,54,83,101}) {
                    var g=new DefenseSimulation(new GameBalance {SpawnEnemies=false},seed,new CampaignBattle {
                        Stage=CampaignCatalog.Stages[10],Loadout=noSpear,MarkerUnlocked=true
                    });
                    g.Start();g.Tick(31,new V2());
                    Assert(g.AwaitingCard && g.CardOffers.Count==3 && !g.CardOffers.Contains(CardKind.Ballistic),"unusable damage card");
                    foreach(CardKind kind in g.CardOffers)observed.Add(kind);
                }
                Assert(observed.Contains(CardKind.Control) && observed.Contains(CardKind.Blast),"reachable support cards disappeared");
                var wards=new DefenseSimulation(new GameBalance {SpawnEnemies=false},7,new CampaignBattle {
                    Stage=CampaignCatalog.Stages[10],Loadout=1<<(int)TowerKind.Barrier
                });
                wards.Start();wards.Tick(31,new V2());
                Assert(wards.CardOffers.Count==3 && wards.CardOffers.Contains(CardKind.Range) &&
                    wards.CardOffers.Contains(CardKind.Credits) && wards.CardOffers.Contains(CardKind.Hunter),"ward-only fallback offers");
            });
            Check("Same-family adjacency adds only5% even with three neighbors",()=>{
                var g=New(-1,false,1000);var a=Build(g,4,TowerKind.Autocannon);Build(g,7,TowerKind.Autocannon);Build(g,2,TowerKind.Autocannon);
                Assert(g.HasResonance(a),"missing resonance");Near(g.Stats(a).Damage,14.7f);
            });
            Check("Adjacent markers never advertise resonance without a matching combat effect",()=>{
                var g=New(-1,false,1000);var a=Build(g,4,TowerKind.Marker);var b=Build(g,2,TowerKind.Marker);
                Assert(V2.Distance(a.Position,b.Position)<=6,"fixture must be adjacent");
                Assert(!g.HasResonance(a) && !g.HasResonance(b),"false marker resonance");
                Near(g.Stats(a).Damage,4);Near(g.Stats(b).Damage,4);
            });
            Check("Upgrade preview includes current resonance research and cards without mutating the battle",()=>{
                foreach(var kind in new[]{TowerKind.Autocannon,TowerKind.Binding,TowerKind.Rupture}) {
                    var g=new DefenseSimulation(new GameBalance {SpawnEnemies=false,OfferCards=false,StartingCredits=1000},7,new CampaignBattle {Research=new[]{2,2,2,0}});
                    var a=Build(g,4,kind);Build(g,7,kind);
                    g.Cards[(int)CardKind.Ballistic]=1;g.Cards[(int)CardKind.Control]=1;g.Cards[(int)CardKind.Range]=1;
                    Assert(g.HasResonance(a),"preview fixture must have resonance");
                    int credits=g.Credits, purchases=g.Telemetry.Purchases.Count;double elapsed=g.Elapsed;
                    TowerStats before=g.Stats(a), preview=g.Stats(a,2);
                    Assert(a.Level==1 && g.Credits==credits && g.Telemetry.Purchases.Count==purchases && g.Elapsed==elapsed,"preview changed battle");
                    Near(g.Stats(a).Damage,before.Damage);Near(g.Stats(a).SlowSeconds,before.SlowSeconds);
                    if(kind==TowerKind.Autocannon)Near(preview.Damage,22*(1+.15f+.16f+.05f));
                    string reason;Assert(g.TryUpgrade(a.Id,null,out reason),reason);TowerStats actual=g.Stats(a);
                    Near(preview.Damage,actual.Damage);Near(preview.SlowSeconds,actual.SlowSeconds);Near(preview.Range,actual.Range);Near(preview.Radius,actual.Radius);
                }
            });
            Check("Fusion consumes links, uses independent catalyst and snapshots",()=>{
                var g=New(-1,false,1000);var a=Build(g,4,TowerKind.Autocannon,true);var neighbor=Build(g,7,TowerKind.Autocannon);var b=Build(g,2,TowerKind.Binding,true);
                g.Start();g.Tick(60,new V2());Move(g,4);string r;
                Assert(g.TryFuse(0,a.Id,b.Id,4,null,out r),r);Assert(!g.HasResonance(neighbor) && g.Cores==0,"stale resonance/catalyst");
                Assert(g.TowerAt(4).PaidCredits==195 && g.TowerAt(2)==null,"consumption");Near(g.TowerAt(4).Commission,1);
            });
            Check("Research is added once and copied into a battle snapshot",()=>{
                var rules=new CampaignBattle {Research=new[]{2,2,2,0}};
                var g=new DefenseSimulation(new GameBalance(),7,rules);rules.Research[0]=3;
                Near(g.Stats(TowerKind.Autocannon,2).Damage,22*1.16f);
                Near(g.Stats(TowerKind.Arc,1).Chain[0],24*1.16f);
                Near(g.Stats(TowerKind.Gravity,1).SlowSeconds,1.7f);
                Near(g.Stats(TowerKind.Shatter,1).Radius,1.6f);
            });
            Check("Ward capacity, finite duration, shared immunity and movement",()=>{
                var g=New(-1,false,1000);var ward=Build(g,4,TowerKind.Barrier);Move(g,0);
                for(int i=0;i<3;i++)g.Enemies.Add(new Enemy {Id=800+i,Health=10000,MaxHealth=10000,Speed=.4f,Position=ward.Position+new V2(.1f*i,0),Lane=0});
                g.Start();g.Tick(.1f,new V2());Assert(g.Enemies.FindAll(e=>e.BlockOwner==ward.Id).Count==2,"capacity");
                var e=g.Enemies[0];g.Tick(3.2f,new V2());Assert(e.BlockOwner==0 && e.BlockImmunity>0,"permanent block");
                V2 p=e.Position;g.Tick(2.0f,new V2());Assert(V2.Distance(p,e.Position)>0,"immune enemy stuck");
            });
            Check("Ward break releases both enemies and emits one temporary nearby slow", () => {
                var game = New(-1, false, 1000);
                game.Balance.PlayerDamage = 0;
                var ward = Build(game, 4, TowerKind.Barrier);
                ward.WardHealth = 1;
                var attacker = new Enemy { Id=800, Health=100, Position=ward.Position, BlockOwner=ward.Id, BlockRemaining=2 };
                var second = new Enemy { Id=801, Health=100, Position=ward.Position, BlockOwner=ward.Id, BlockRemaining=2 };
                var runner = new Enemy { Id=802, Kind=EnemyKind.Runner, Health=100, Position=ward.Position + new V2(1, 0), BlockImmunity=4 };
                var siege = new Enemy { Id=803, Kind=EnemyKind.Siege, Health=100, Position=ward.Position };
                var boss = new Enemy { Id=804, Kind=EnemyKind.Boss, Health=100, Position=ward.Position, AttackCooldown=100 };
                var distant = new Enemy { Id=805, Health=100, Position=ward.Position + new V2(6, 0), BlockImmunity=4 };
                game.Enemies.AddRange(new[] { attacker, second, runner, siege, boss, distant });
                game.Start();
                game.Tick(.03f, new V2());
                Assert(attacker.BlockOwner == 0 && second.BlockOwner == 0 && attacker.BlockImmunity > 0 && second.BlockImmunity > 0, "both held enemies must be released");
                Near(ward.WardHealth, 0);
                Near(ward.Cooldown, game.Stats(ward).Interval);
                Near(attacker.SlowFraction, .35f);
                Near(runner.SlowFraction, .35f);
                Near(siege.SlowFraction, .14f);
                Near(boss.SlowFraction, 0);
                Near(distant.SlowFraction, 0);
                Near(runner.Health, 100);
                var bursts = game.Events.FindAll(e => e.Kind == EventKind.WardBurst);
                Assert(bursts.Count == 1, "one burst per break");
                Near(bursts[0].Value, game.Stats(ward).Range);
                game.Events.Clear();
                game.Tick(1.7f, new V2());
                Near(runner.SlowFraction, 0);
                Assert(!game.Events.Exists(e => e.Kind == EventKind.WardBurst), "burst must not repeat on cooldown");
            });
            Check("Hunter blocks only two briefly, immunity permits passage and siege bypasses",()=>{
                var g=new DefenseSimulation(new GameBalance {SpawnEnemies=false,OfferCards=false,PlayerDamage=0,PlayerHealth=10000},7,new CampaignBattle {Stage=CampaignCatalog.Stages[3]});
                var p=g.PlayerPosition;
                for(int i=0;i<3;i++)g.Enemies.Add(new Enemy {Id=700+i,Health=10000,MaxHealth=10000,Speed=1,Position=p+new V2(.2f*i,0),Lane=0});
                var siege=new Enemy {Id=704,Kind=EnemyKind.Siege,Health=10000,MaxHealth=10000,Speed=1,Position=p,Lane=2};g.Enemies.Add(siege);
                g.Start();g.Tick(.05f,new V2());Assert(g.Enemies.FindAll(e=>e.BlockOwner<0).Count==2 && siege.BlockOwner==0,"capacity or siege bypass");
                var held=g.Enemies.Find(e=>e.BlockOwner<0);for(int i=0;i<61;i++)g.Tick(.05f,new V2());
                Assert(held.BlockOwner==0 && held.BlockImmunity>0,"hunter held indefinitely");V2 old=held.Position;g.Tick(2.0f,new V2());Assert(V2.Distance(old,held.Position)>.1f,"released enemy cannot pass");
            });
            Check("Campaign waits through empty gaps and wins on final kill before180",()=>{
                var g=New(0,true);g.Schedule.Clear();g.Schedule.Add(new SpawnGroup {Wave=0,Lane=0,Count=1,Start=10,Interval=1});
                g.Start();g.Tick(9,new V2());Assert(g.State==RunState.Running && g.Enemies.Count==0 && !g.AllWavesSpawned,"empty gap won");
                g.Tick(1.1f,new V2());Assert(g.State==RunState.Running && g.AllWavesSpawned && g.Enemies.Count==1,"last spawn won early");
                g.Balance.PlayerRange=100;g.Balance.PlayerDamage=100000;g.Tick(5,new V2());
                Assert(g.State==RunState.Won && g.Enemies.Count==0 && g.Elapsed<180 && g.Telemetry.Spawned==1,"final kill did not win");
            });
            Check("Campaign cleanup continues past180 with pause and finite supplies",()=>{
                var g=New(2);foreach(var group in g.Schedule)group.Spawned=group.Count;
                g.Enemies.Add(new Enemy {Id=900,Kind=EnemyKind.Siege,Health=100000,MaxHealth=100000,Speed=0,Position=new V2(15,8)});
                g.Start();g.Tick(200,new V2());Assert(g.State==RunState.Running && g.Elapsed>199 && g.AllWavesSpawned && g.Enemies.Count==1,"cleanup cut off");
                Assert(g.Credits==640 && g.Telemetry.CoresGranted==2,"extra cleanup supply");double time=g.Elapsed;
                g.TogglePause();g.Tick(20,new V2());Assert(g.Elapsed==time,"cleanup pause advanced");g.TogglePause();
                g.Balance.PlayerRange=100;g.Balance.PlayerDamage=100000;g.Tick(15,new V2());
                Assert(g.State==RunState.Won && g.Elapsed>200 && g.Enemies.Count==0,"cleanup kill failed");
                Assert(g.Credits==645 && g.Telemetry.CoresGranted==2,"cleanup rewards duplicated");
            });
            Check("Campaign cleanup keeps card count and hunter revival after180",()=>{
                var g=New(2,false,140,true);foreach(var group in g.Schedule)group.Spawned=group.Count;
                g.Balance.PlayerDamage=0;g.Enemies.Add(new Enemy {Id=900,Kind=EnemyKind.Siege,Health=100000,MaxHealth=100000,Speed=0,Position=new V2(15,8)});
                g.Start();int cards=0;while(g.Elapsed<200){g.Tick(5,new V2());if(g.AwaitingCard){cards++;g.ChooseCard(g.CardOffers[0]);}}
                Assert(cards==3 && g.State==RunState.Running && g.Cores==2,"extra cards or timed finish");
                g.Enemies.Add(new Enemy {Id=901,Kind=EnemyKind.Brute,Health=100000,MaxHealth=100000,Speed=0,Position=g.PlayerPosition,AttackCooldown=0});
                for(int i=0;i<500 && !g.HunterDown;i++)g.Tick(.05f,new V2());Assert(g.HunterDown,"cleanup hunter did not go down");
                g.Enemies.RemoveAll(e=>e.Id==901);g.Tick(DefenseSimulation.HunterRespawnDelay+.1f,new V2());
                Assert(!g.HunterDown && g.PlayerHealth==g.Balance.PlayerHealth && g.State==RunState.Running,"cleanup revival stopped");
            });
            Check("Campaign core loss still defeats after180",()=>{
                var g=New(0);foreach(var group in g.Schedule)group.Spawned=group.Count;
                g.Balance.PlayerDamage=0;g.Enemies.Add(new Enemy {Id=900,Kind=EnemyKind.Siege,Health=100000,MaxHealth=100000,Speed=0,Position=new V2(15,8)});
                g.Start();g.Tick(181,new V2());Assert(g.State==RunState.Running,"ended before cleanup loss");
                var e=g.Enemies[0];e.Position=new V2(1.1f,0);e.Waypoint=g.Roads[0].Length-1;
                g.Tick(60,new V2());Assert(g.State==RunState.Lost && g.CoreHealth==0 && g.Elapsed>181,"cleanup core loss ignored");
            });
            Check("Boss bypasses blocks and survives the old deadline until defeated",()=>{
                var g=New(5);g.Schedule.Clear();g.Start();g.Tick(181,new V2());Assert(g.State==RunState.Running && !g.BossDefeated,"missing boss falsely won");
                g.Enemies.Add(new Enemy {Id=998,Kind=EnemyKind.Boss,Health=1,MaxHealth=1,Speed=0,Position=g.PlayerPosition+new V2(1,0)});
                g.Tick(2,new V2());Assert(g.State==RunState.Won && g.BossDefeated,"boss kill did not finish empty board");
                g=New(-1,false,1000);var t=Build(g,4,TowerKind.Barrier);var boss=new Enemy {Id=99,Kind=EnemyKind.Boss,Health=10000,MaxHealth=10000,Speed=0,Position=t.Position};g.Enemies.Add(boss);g.Start();g.Tick(.1f,new V2());
                Assert(boss.BlockOwner==0 && !boss.Engaged,"boss blocked");
            });
            Check("Skill cooldown pauses and support is finite",()=>{
                var g=New();g.Start();Assert(g.UseSkill() && !g.UseSkill(),"skill duplicate");Assert(g.UseSupport() && !g.UseSupport(),"support duplicate");
                float cooldown=g.SkillCooldown;g.TogglePause();g.Tick(20,new V2());Near(g.SkillCooldown,cooldown);g.TogglePause();g.Tick(13,new V2());Assert(g.SkillCooldown==0 && g.CoreShield==0,"timer");
            });
            Check("Active hunter effect pauses and expires before skill reuse",()=>{
                foreach (int hunter in new[] { 1, 2 }) {
                    var skill = new DefenseSimulation(new GameBalance { SpawnEnemies = false, OfferCards = false }, 7, new CampaignBattle { Hunter = hunter });
                    skill.Start();
                    float duration = hunter == 1 ? 3 : 4;
                    Near(skill.HunterSkillActiveRemaining, 0);
                    Assert(skill.UseSkill(), "activation");
                    Near(skill.HunterSkillActiveRemaining, duration);
                    Near(skill.SkillCooldown, 12);
                    skill.TogglePause(); skill.Tick(20, new V2()); skill.TogglePause();
                    Near(skill.HunterSkillActiveRemaining, duration);
                    skill.Tick(duration + .2f, new V2());
                    Near(skill.HunterSkillActiveRemaining, 0);
                    Assert(skill.SkillCooldown > 7 && !skill.UseSkill(), "effect expires before reuse");
                }
            });
            Check("Sehee sprint increases real travel and returns to normal speed after expiry",()=>{
                var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,new CampaignBattle{Hunter=1});
                Assert(!g.UseSkill(),"skill before battle");g.Start();
                V2 start=g.PlayerPosition;g.Tick(.2f,new V2(1,0));float normal=V2.Distance(start,g.PlayerPosition);
                Assert(g.UseSkill(),"sprint activation");start=g.PlayerPosition;g.Tick(.2f,new V2(1,0));
                Near(V2.Distance(start,g.PlayerPosition),normal*1.65f);
                g.Tick(3.1f,new V2());Near(g.HunterSkillActiveRemaining,0);
                start=g.PlayerPosition;g.Tick(.2f,new V2(1,0));Near(V2.Distance(start,g.PlayerPosition),normal);
                Assert(!g.UseSkill(),"sprint reused before cooldown");
            });
            Check("Doha shield protects after opening invulnerability and stops protecting on expiry",()=>{
                foreach(int growth in new[]{0,2}) {
                    var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,new CampaignBattle{Hunter=2,EarnedGrowth=growth});g.Start();
                    Action attack=()=>{g.Events.Clear();g.Enemies.Add(new Enemy{Id=980,Kind=EnemyKind.Grunt,Health=100000,MaxHealth=100000,Speed=0,Position=g.PlayerPosition+new V2(.4f,0)});g.Tick(.05f,new V2());g.Enemies.Clear();};
                    float full=g.PlayerHealth;Assert(g.UseSkill(),"shield activation");attack();
                    Near(g.PlayerHealth,full);Assert(!g.Events.Exists(e=>e.Kind==EventKind.HunterHurt),"opening protection failed");
                    g.Tick(.7f,new V2());attack();Near(full-g.PlayerHealth,1.75f);
                    var hits=g.Events.FindAll(e=>e.Kind==EventKind.HunterHurt);
                    Assert(hits.Count==1,"protected hit missing");Near(hits[0].Value,1.75f);
                    g.Tick(4f,new V2());Near(g.HunterSkillActiveRemaining,0);
                    float before=g.PlayerHealth;attack();Near(before-g.PlayerHealth,7);
                    Assert(!g.UseSkill(),"shield reused before cooldown");
                }
            });
            Check("Kang dodge follows last movement despite changed aim and stays within the arena",()=>{
                var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,new CampaignBattle());g.Start();
                g.Tick(.1f,new V2(1,0));g.Enemies.Add(new Enemy{Id=981,Kind=EnemyKind.Grunt,Health=100000,MaxHealth=100000,Speed=0,Position=g.PlayerPosition+new V2(0,2)});
                g.Tick(.02f,new V2());Assert(g.PlayerAim.Y>.9f,"fixture did not turn aim toward enemy");
                V2 start=g.PlayerPosition;Assert(g.UseSkill(),"dodge activation");Near(g.PlayerPosition.X-start.X,2.8f);Near(g.PlayerPosition.Y,start.Y);
                Assert(g.HunterInvulnerability>.7f,"dodge protection missing");
                g.Enemies.Clear();g.Tick(13f,new V2(1,0));g.TogglePause();start=g.PlayerPosition;
                Assert(!g.UseSkill(),"paused skill activation");Near(V2.Distance(start,g.PlayerPosition),0);
                g.TogglePause();Assert(g.UseSkill(),"cooled dodge at boundary");
                Assert(Math.Abs(g.PlayerPosition.X)<=g.HalfWidth && Math.Abs(g.PlayerPosition.Y)<=g.HalfHeight,"dodge left arena");
            });
            Check("Automatic emergency ward triggers below half and respects pause duration and cooldown",()=>{
                var g=New();g.Enemies.Add(new Enemy {Id=901,Kind=EnemyKind.Siege,Health=100000,MaxHealth=100000,Speed=0,Position=new V2(1.1f,0),Waypoint=4});g.Start();
                for(int i=0;i<300 && g.CoreShield<=0;i++) {g.Tick(.05f,new V2());if(g.CoreHealth>=150)Assert(g.CoreShield==0,"early trigger");}
                Assert(g.CoreHealth>0 && g.CoreHealth<150 && g.CoreShield>0,"missing automatic trigger");
                Assert(g.Events.FindAll(e=>e.Kind==EventKind.Shield).Count==1,"duplicate event");
                g.Enemies.Clear();float cd=g.SupportCooldown, shield=g.CoreShield;g.TogglePause();g.Tick(40,new V2());Near(g.SupportCooldown,cd);Near(g.CoreShield,shield);
                g.TogglePause();g.Tick(5.1f,new V2());Assert(g.CoreShield==0 && g.SupportCooldown>29,"duration/cooldown");
                g.Tick(30,new V2());Assert(g.CoreShield>0 && g.Events.FindAll(e=>e.Kind==EventKind.Shield).Count==2,"cooldown renewal");
            });
            Check("Breach direction counts actual core damage without overkill",()=>{
                var g = New(3);
                Assert(g.Telemetry.MostDamagingLane == -1, "direction before a breach");
                for (int i = 0; i < 3; i++) {
                    int lane = i == 0 ? 0 : 2;
                    g.Enemies.Add(new Enemy { Id = 901 + i, Kind = EnemyKind.Siege, Lane = lane,
                        Health = 100000, MaxHealth = 100000, Speed = 0, Position = new V2(1.1f, 0),
                        Waypoint = g.Roads[lane].Length - 1 });
                }
                g.Start();
                g.Tick(.05f, new V2());
                Near(g.Telemetry.CoreDamageByLane[0], 24);
                Near(g.Telemetry.CoreDamageByLane[2], 48);
                Assert(g.Telemetry.MostDamagingLane == 2, "east did most damage");
                g.Tick(20, new V2());
                Assert(g.State == RunState.Lost && g.Telemetry.MostDamagingLane == 2, "defeat direction");
                Near(g.Telemetry.CoreDamageByLane[0] + g.Telemetry.CoreDamageByLane[2], 300);
            });
            Check("Peak erosion records damage and precise star boundaries",()=>{
                Assert(CampaignCatalog.Stars(true,270,300)==3 && CampaignCatalog.Stars(true,269,300)==2 && CampaignCatalog.Stars(true,210,300)==2 && CampaignCatalog.Stars(true,209,300)==1 && CampaignCatalog.Stars(false,300,300)==0,"star boundaries");
                var g=New();g.Enemies.Add(new Enemy {Id=900,Kind=EnemyKind.Siege,Health=10000,MaxHealth=10000,Speed=0,Position=new V2(1.1f,0),Waypoint=4});g.Start();g.Tick(2,new V2());
                Assert(g.MinimumCoreHealth<300 && g.PeakErosion>0,"no erosion recorded");
            });
            Check("First clears unlock sequentially, total90, heroes retain grades",()=>{
                var svc=ClearedProfile();Assert(svc.Profile.MagicCores==90 && svc.Profile.StarsTotal==18,"chapter budget");
                Assert(svc.Profile.IsHunterOpen(1) && svc.Profile.IsHunterOpen(2) && svc.Profile.Grade(1)=="C급" && svc.Profile.Grade(2)=="B급","hero unlock/grades");
                Assert(svc.Profile.Grade(0).Contains("미분류"),"Kang Yoon classification");
            });
            Check("Result replay pays once, repeat pays2, wrong run rejected",()=>{
                var svc=new CampaignService(new CampaignProfile(),null);string id;Assert(svc.Begin(0,false,out id),"begin");var g=Win(0);int reward;
                Assert(!svc.Complete("wrong",g,out reward),"wrong run");Assert(svc.Complete(id,g,out reward) && reward==10,"first");Assert(svc.Complete(id,g,out reward) && svc.Profile.MagicCores==10,"duplicate");
                svc.Begin(0,false,out id);Assert(svc.Complete(id,g,out reward) && reward==2 && svc.Profile.MagicCores==12,"repeat");
            });
            Check("Failed save leaves live economy unchanged and result can retry",()=>{
                bool fail=false;var svc=new CampaignService(new CampaignProfile(),p=>{if(fail)throw new IOException("injected disk failure");});string id;svc.Begin(0,false,out id);
                fail=true;int reward;Assert(!svc.Complete(id,Win(0),out reward) && svc.Profile.MagicCores==0 && svc.Profile.PendingRun==id,"uncommitted mutation");
                fail=false;Assert(svc.Complete(id,Win(0),out reward) && svc.Profile.MagicCores==10,"retry");
            });
            Check("Abandon clears only current pending fields and preserves earned profile and rewards",()=>{
                var seed=ClearedProfile();Assert(seed.Research(0) && seed.Grow(1),"seed investments");
                var profile=seed.Profile.Copy();profile.Muted=true;profile.SelectedHunter=1;
                byte[] persisted=null;var svc=new CampaignService(profile,p=>persisted=CampaignStore.Encode(p));string id;
                Assert(svc.Begin(0,true,out id),"begin crisis");var expected=svc.Profile.Copy();
                expected.PendingRun="";expected.PendingStage=-1;expected.PendingCrisis=false;
                Assert(svc.Abandon(id),"abandon");
                Assert(Convert.ToBase64String(CampaignStore.Encode(svc.Profile))==Convert.ToBase64String(CampaignStore.Encode(expected)),"unrelated profile fields changed");
                Assert(CampaignStore.Decode(persisted).Valid() && Convert.ToBase64String(persisted)==Convert.ToBase64String(CampaignStore.Encode(expected)),"persisted record differs");
                int reward;Assert(!svc.Complete(id,Win(0),out reward) && svc.Profile.MagicCores==profile.MagicCores,"abandoned run redeemed");
                Assert(svc.Complete(profile.LastRewardRun,Win(5),out reward) && svc.Profile.MagicCores==profile.MagicCores,"old reward replay changed");
            });
            Check("Abandon rejects empty stale and duplicate IDs without writing or canceling a restart",()=>{
                int writes=0;var svc=new CampaignService(new CampaignProfile(),p=>writes++);string oldId,id;
                svc.Begin(0,false,out oldId);svc.Begin(0,false,out id);int before=writes;
                string bytes=Convert.ToBase64String(CampaignStore.Encode(svc.Profile));
                Assert(!svc.Abandon(null) && !svc.Abandon("") && !svc.Abandon(oldId) && !svc.Abandon("wrong"),"invalid ID accepted");
                Assert(writes==before && Convert.ToBase64String(CampaignStore.Encode(svc.Profile))==bytes,"rejected abandon mutated profile");
                Assert(svc.Abandon(id),"current abandon");before=writes;
                Assert(!svc.Abandon(id) && writes==before && svc.Profile.PendingStage==-1,"duplicate abandon wrote");
            });
            Check("Abandon save failure preserves pending run and supports either retry or result saving",()=>{
                bool fail=false;byte[] persisted=null;
                var svc=new CampaignService(new CampaignProfile(),p=>{if(fail)throw new IOException("injected abandon failure");persisted=CampaignStore.Encode(p);});string id;
                svc.Begin(0,false,out id);string before=Convert.ToBase64String(persisted);fail=true;
                Assert(!svc.Abandon(id) && svc.Error.Contains("저장 실패"),"failure not reported");
                Assert(Convert.ToBase64String(CampaignStore.Encode(svc.Profile))==before && Convert.ToBase64String(persisted)==before,"failed abandon mutated live/disk profile");
                fail=false;int reward;Assert(svc.Complete(id,Win(0),out reward) && reward==10,"earned result lost after failure");
                svc.Begin(0,false,out id);fail=true;Assert(!svc.Abandon(id),"second failure");
                fail=false;Assert(svc.Abandon(id) && svc.Profile.PendingRun=="" && svc.Profile.MagicCores==10,"retry failed or altered reward");
            });
            Check("Research, exact refund and growth gates cannot mint cores",()=>{
                var svc=ClearedProfile();int initial=svc.Profile.MagicCores;
                Assert(svc.Research(0) && svc.Research(0) && !svc.Research(0),"research gate");Assert(svc.ResearchInvestment()==6,"investment");
                Assert(svc.Respec() && svc.Respec() && svc.Profile.MagicCores==initial,"refund mint");
                Assert(svc.Grow(0) && svc.Grow(0) && svc.Grow(0) && !svc.Grow(0),"first seal growth cap");
                Assert(svc.Profile.Grade(0).Contains("미분류") && svc.Profile.MagicCores==initial-18,"growth cost/status");
            });
            Check("Unlocked hunters have one affordable growth step without a nonexistent world gate",()=>{
                var p=new CampaignProfile {MagicCores=36};
                for(int i=0;i<2;i++){p.BestStars[i]=3;p.FirstCleared[i]=true;}
                var svc=new CampaignService(p,null);Assert(!svc.Grow(1) && svc.Profile.MagicCores==36,"locked hunter purchase");
                p.BestStars[2]=3;p.FirstCleared[2]=true;svc=new CampaignService(p,null);
                Assert(svc.Grow(1) && svc.Profile.Grade(1)=="B급" && svc.Profile.MagicCores==24,"Sehee first growth");
                Assert(!svc.Grow(1) && svc.Profile.MagicCores==24,"Sehee repeat beyond current content");
                var g=new DefenseSimulation(new GameBalance(),7,svc.Battle(2,false));
                // Select explicitly: the earlier purchase must not silently change the deployed hunter.
                Assert(g.Rules.Hunter==0,"growth changed selection");Assert(svc.SelectHunter(1),"Sehee selection");
                g=new DefenseSimulation(new GameBalance(),7,svc.Battle(2,false));Near(g.Balance.PlayerHealth,91.8f);Near(g.Balance.PlayerDamage,10.5f);
                svc=ClearedProfile();Assert(svc.Profile.FirstSealComplete && svc.Grow(2),"Baek first growth");
                Assert(svc.Profile.Grade(2)=="A급" && svc.Profile.MagicCores==72,"Baek cost/grade");
                Assert(!svc.Grow(2) && svc.Profile.MagicCores==72,"Baek future step charged");
            });
            Check("Legacy hunter growth survives the new purchase caps and save roundtrip",()=>{
                var p=ClearedProfile().Profile;p.Growth[0]=3;p.Growth[1]=2;p.Growth[2]=1;p.SelectedHunter=1;
                var restored=CampaignStore.Decode(CampaignStore.Encode(p));var svc=new CampaignService(restored,null);
                Assert(restored.Growth[0]==3 && restored.Growth[1]==2 && restored.Growth[2]==1 && restored.SelectedHunter==1,"existing growth reset");
                Assert(!svc.Grow(1) && svc.Profile.MagicCores==p.MagicCores,"locked legacy purchase mutated economy");
                var g=new DefenseSimulation(new GameBalance(),7,svc.Battle(6,false));Near(g.Balance.PlayerHealth,98.6f);Near(g.Balance.PlayerDamage,11f);
            });
            Check("Crisis mode is gated and keeps normal stars and first reward",()=>{
                var svc=new CampaignService(new CampaignProfile(),null);string id;Assert(!svc.Begin(0,true,out id),"crisis before clear");
                svc=ClearedProfile();svc.Begin(0,true,out id);
                var g=new DefenseSimulation(new GameBalance {SpawnEnemies=false,OfferCards=false},7,svc.Battle(0,true));g.Schedule.Clear();g.Start();g.Tick(180,new V2());int reward;
                Assert(svc.Complete(id,g,out reward) && reward==2 && svc.Profile.BestStars[0]==3 && svc.Profile.CrisisStars[0]==3,"crisis ledger");
                Assert(svc.Profile.StarsTotal==18 && svc.Profile.CrisisStarsTotal==3,"independent star totals");
                Assert(svc.Complete(id,g,out reward) && svc.Profile.CrisisStarsTotal==3,"same run must not add stars");
            });
            Check("Versioned profile roundtrip, corruption detection and backup recovery",()=>{
                var p=ClearedProfile().Profile;byte[] bytes=CampaignStore.Encode(p);Assert(CampaignStore.Decode(bytes).MagicCores==90,"roundtrip");
                bytes[40]^=1;bool failed=false;try{CampaignStore.Decode(bytes);}catch(InvalidDataException){failed=true;}Assert(failed,"checksum");
                string path=Path.Combine(directory,"profile-"+Guid.NewGuid().ToString("N")+".sav");var store=new CampaignStore(path);store.Save(p);
                var next=p.Copy();next.MagicCores=88;store.Save(next);File.WriteAllBytes(path,new byte[]{1,2,3});
                Assert(store.Load().MagicCores==90 && store.Warning!="","backup");store.Save(p);Assert(store.Load().MagicCores==90,"recovered save");
            });
            output.Add("PASS: "+count+" campaign/combat/persistence checks. Scripted scenarios below are not human playtests.");
            for(int stage=0;stage<6;stage++)output.Add(Strategy(stage,false));
            for(int stage=2;stage<6;stage++)output.Add(Strategy(stage,true));
            for(int stage=0;stage<6;stage++) {
                var idle=New(stage,true,140,true);idle.Start();
                for(int i=0;i<4000 && idle.State!=RunState.Won && idle.State!=RunState.Lost;i++) {Choose(idle);idle.Tick(.05f,new V2());idle.Events.Clear();}
                output.Add("IDLE stage="+(stage+1)+" result="+idle.State+" t="+idle.Elapsed.ToString("F1"));
            }
            return string.Join("\n",output);
        }
        static void Choose(DefenseSimulation g)
        { if(g.AwaitingCard)g.ChooseCard(g.CardOffers.Contains(CardKind.Credits) ? CardKind.Credits : g.CardOffers[0]); }
        static string Strategy(int stage,bool fuse)
        {
            var g=New(stage,true,140,true);bool arcPlan=fuse && stage>=2;
            var order=new List<int>();
            foreach(int lane in g.OpenLanes) {
                V2[] road=g.Roads[lane];int node;V2 target=BattlefieldLayouts.Along(road,Math.Max(0,BattlefieldLayouts.RoadLength(road)-7),out node);int best=-1;float distance=float.MaxValue;
                for(int i=0;i<g.BoardSockets.Length;i++)if(!order.Contains(i) && V2.Distance(g.BoardSockets[i],target)<distance) {best=i;distance=V2.Distance(g.BoardSockets[i],target);}
                if(best>=0)order.Add(best);
            }
            for(int i=0;i<g.BoardSockets.Length;i++)if(!order.Contains(i))order.Add(i);
            int anchor=order[0], second=order[1];
            if(arcPlan) {float d=float.MaxValue;foreach(int i in order)if(i!=anchor && V2.Distance(g.BoardSockets[i],g.BoardSockets[anchor])<d){d=V2.Distance(g.BoardSockets[i],g.BoardSockets[anchor]);second=i;}}
            Build(g,anchor,TowerKind.Autocannon,true);
            if(arcPlan)Build(g,second,TowerKind.Binding);else Build(g,second,TowerKind.Autocannon,true);
            g.Start();int goal=-1;string why;
            for(int frame=0;frame<15000 && g.State!=RunState.Won && g.State!=RunState.Lost;frame++) {
                Choose(g);goal=-1;
                if(arcPlan && g.Telemetry.Fusions==0) {
                    var binding=g.Towers.Find(t=>t.Kind==TowerKind.Binding);
                    if(binding!=null && binding.Level==1 && g.Credits>=40) {
                        goal=binding.Socket;g.TryUpgrade(binding.Id,null,out why);
                    } else if(binding!=null && binding.Level==2 && g.Cores>0 && g.Credits>=40) {
                        goal=anchor;var spear=g.TowerAt(anchor);
                        if(spear!=null)g.TryFuse(0,spear.Id,binding.Id,anchor,null,out why);
                    }
                } else {
                    foreach(var tower in g.Towers)if(tower.Level==1 && TowerCatalog.Get(tower.Kind).UpgradeCost>0 && g.Credits>=TowerCatalog.Get(tower.Kind).UpgradeCost) {
                        goal=tower.Socket;g.TryUpgrade(tower.Id,null,out why);break;
                    }
                    if(goal<0 && g.Credits>=40) {
                        foreach(int slot in order)if(slot<g.BoardSockets.Length && g.TowerAt(slot)==null) {
                            goal=slot;g.TryBuild(slot,TowerKind.Autocannon,null,out why);break;
                        }
                    }
                }
                V2 destination=goal>=0 ? g.BoardSockets[goal] : new V2(2,-2.3f);
                if(g.Enemies.Exists(e=>e.Kind==EnemyKind.Boss && e.BossWindup>0 && V2.Distance(g.PlayerPosition,e.BossTarget)<2.5f))destination=g.PlayerPosition+new V2(3,0);
                V2 d=Steer(g.PlayerPosition,destination);
                if(g.Enemies.FindAll(e=>V2.Distance(e.Position,g.PlayerPosition)<1.2f).Count>0)g.UseSkill();
                if(g.CoreHealth<290 || g.BossSpawned)g.UseSupport();
                g.Tick(.05f,d.Length>.18f ? d.Normalized : new V2());g.Events.Clear();
            }
            Assert(g.State==RunState.Won,"normal strategy failed stage "+(stage+1)+" fusion="+arcPlan+" t="+g.Elapsed+" core="+g.CoreHealth+" boss="+g.BossDefeated+" towers="+g.Towers.Count+" credits="+g.Credits+" remaining="+g.Enemies.Count);
            return "STRATEGY stage="+(stage+1)+" mode="+(arcPlan?"fusion":"spread")+" result="+g.State+" t="+g.Elapsed.ToString("F1")+" core="+g.CoreHealth.ToString("F0")+" hunter="+g.PlayerHealth.ToString("F0")+" kills="+g.Kills+" fusions="+g.Telemetry.Fusions+" boss="+g.BossDefeated;
        }
    }
}

