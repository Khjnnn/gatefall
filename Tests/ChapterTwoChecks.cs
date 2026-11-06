using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using System.Collections.Generic;
using Gatefall.Core;
namespace Gatefall.Tests
{
    public static class ChapterTwoChecks
    {
        static void A(bool value,string why){if(!value)throw new Exception(why);}
        public static CampaignProfile Entry()
        {var p=new CampaignProfile{MagicCores=280};for(int i=0;i<14;i++){p.BestStars[i]=3;p.FirstCleared[i]=true;p.BestErosion[i]=0;}p.Growth=new[]{5,3,2};p.Research=new[]{3,2,2,2};return p;}
        static byte[] LegacyV2(CampaignProfile p)
        {
            byte[] bytes;using(var m=new MemoryStream()){using(var w=new BinaryWriter(m,Encoding.UTF8,true)){
                w.Write("GATEFALL-RAON");w.Write(2);w.Write(p.MagicCores);w.Write(p.SelectedHunter);w.Write(p.StoryMask);w.Write(p.TutorialMask);w.Write(p.Muted);
                for(int i=0;i<6;i++){w.Write(p.BestStars[i]);w.Write(p.CrisisStars[i]);w.Write(p.BestErosion[i]);w.Write(p.FirstCleared[i]);}
                foreach(int x in p.Research)w.Write(x);foreach(int x in p.Growth)w.Write(x);
                w.Write(p.PendingRun);w.Write(p.PendingStage);w.Write(p.PendingCrisis);w.Write(p.LastRewardRun);w.Write(p.LastReward);
                for(int i=6;i<14;i++){w.Write(p.BestStars[i]);w.Write(p.CrisisStars[i]);w.Write(p.BestErosion[i]);w.Write(p.FirstCleared[i]);}w.Write(p.Loadout);
            }bytes=m.ToArray();}using(var sha=SHA256.Create()){var all=new byte[bytes.Length+32];Buffer.BlockCopy(sha.ComputeHash(bytes),0,all,0,32);Buffer.BlockCopy(bytes,0,all,32,bytes.Length);return all;}
        }
        static DefenseSimulation Victory(CampaignBattle b)
        {var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,b);g.Schedule.Clear();g.Start();g.Tick(.1f,new V2());A(g.State==RunState.Won,"no-boss result fixture");return g;}
        public static string Run(string directory)
        {
            var report=new List<string>();string why;
            A(CampaignCatalog.Stages.Length==20 && CampaignCatalog.StageId(14)=="2-1" && CampaignCatalog.StageId(19)=="2-6","stage identity");
            var maps=new HashSet<string>();
            for(int i=14;i<20;i++){
                var s=CampaignCatalog.Stages[i];var g=new DefenseSimulation(new GameBalance(),7,new CampaignBattle{Stage=s});
                A(!s.Boss && maps.Add(BattlefieldLayouts.Get(s).Id),"distinct maps/no inherited boss");
                A(g.BoardSockets.Length==s.SocketCount && g.WaveCount==s.Counts.Length,"board/wave count");
                foreach(var p in g.BoardSockets){float distance=s.Lanes.Min(l=>BattlefieldLayouts.RoadDistance(p,g.Roads[l]));A(distance>=2.1f && distance<3.7f,"road overlap/out of new base range");}
                for(int a=0;a<g.BoardSockets.Length;a++)for(int b=0;b<a;b++)A(V2.Distance(g.BoardSockets[a],g.BoardSockets[b])>=2.55,"site separation");
                for(int w=0;w<g.WaveCount;w++)A(g.GetWaveBriefing(w).Total==s.Counts[w],"wave schedule exact budget");
                for(int w=0;w<g.WaveCount;w++)A(g.Schedule.Count(x=>x.Wave==w && !x.Warned)==g.Schedule.Where(x=>x.Wave==w).Select(x=>x.Lane).Distinct().Count(),"duplicate entrance warning");
                foreach(var group in g.Schedule)A(s.Lanes.Contains(group.Lane) && g.Roads[group.Lane].Length>=2,"spawn on missing road");
                A(g.WaveProgressFraction(g.WaveCount)==0,"invalid progress index");
            }report.Add("PASS six distinct paths,24/26/28 clear sites and6/7/8 explicit finite waves; no chapter2 boss");
            var cold=new DefenseSimulation(new GameBalance(),7,new CampaignBattle{Stage=CampaignCatalog.Stages[18]});
            A(!cold.Schedule.Any(x=>x.Lane==1 && x.Wave<3) && cold.GetWaveBriefing(3).LaneCount(1)>0,"advertised north activation");
            report.Add("PASS cold logistics north absent in waves1~3 and present in previewed wave4");
            var old=Entry();old.Muted=true;old.SelectedHunter=2;old.CrisisStars[11]=2;old.StoryMask=16383;old.TutorialMask=12288;
            old.PendingRun="pending-legacy";old.PendingStage=11;old.PendingCrisis=true;old.LastReward=36;old.LastRewardRun="paid-legacy";
            byte[] legacy=LegacyV2(old);var migrated=CampaignStore.Decode(legacy);
            A(migrated.Valid() && migrated.MagicCores==280 && migrated.IsStageOpen(14) && !migrated.IsStageOpen(15),"migration frontier/currency");
            A(migrated.PendingRun==old.PendingRun && migrated.PendingStage==11 && migrated.PendingCrisis && migrated.LastRewardRun==old.LastRewardRun && migrated.LastReward==36 && migrated.Muted && migrated.Loadout==old.Loadout,"migration transaction/config");
            A(migrated.Growth.SequenceEqual(old.Growth) && migrated.Research.SequenceEqual(old.Research) && migrated.BestStars.Take(14).SequenceEqual(old.BestStars.Take(14)) && migrated.CrisisStars.Take(14).SequenceEqual(old.CrisisStars.Take(14)),"migration progress/growth");
            A(migrated.StorySeenIds.Count==14 && migrated.TutorialSeenIds.Count==2,"legacy bit mapping");
            string fixture=Path.Combine(directory,"store-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(fixture);string save=Path.Combine(fixture,"migration.sav");File.WriteAllBytes(save,legacy);
            var store=new CampaignStore(save);var loaded=store.Load();A(File.ReadAllBytes(save+".legacy-v2.bak").SequenceEqual(legacy),"legacy backup differs");
            A(CampaignStore.Encode(loaded).SequenceEqual(File.ReadAllBytes(save)),"atomic migration not committed");
            byte[] good=File.ReadAllBytes(save);Directory.CreateDirectory(save+".tmp");bool failed=false;try{store.Save(new CampaignProfile());}catch(Exception e){if(!(e is IOException) && !(e is UnauthorizedAccessException))throw;failed=true;}Directory.Delete(save+".tmp");A(failed && File.ReadAllBytes(save).SequenceEqual(good),"failed write changed original");
            report.Add("PASS v2 ID migration: original backup/currency/stars/erosion/growth/research/loadout/mute/pending/reward preserved; failed write atomic");
            migrated.TutorialSeenIds.Add("C04-S032");migrated.TutorialSeenIds.Add("C07-S064");migrated.StorySeenIds.Add("C10-S240");
            migrated.UnmappedProgress.Add("C10-S240",new StageProgress{Stars=2,CrisisStars=1,Erosion=.2f,FirstCleared=true});
            var rt=CampaignStore.Decode(CampaignStore.Encode(migrated));A(rt.TutorialSeenIds.SetEquals(migrated.TutorialSeenIds) && rt.StorySeenIds.SetEquals(migrated.StorySeenIds) && rt.UnmappedProgress["C10-S240"].Stars==2,"ID boundary/future records");
            report.Add("PASS32/64/240 IDs and unknown future progress survive deterministic v3 roundtrip");
            byte[] future=(byte[])good.Clone();using(var m=new MemoryStream(future,true)){m.Position=32;using(var r=new BinaryReader(m,Encoding.UTF8,true)){r.ReadString();long at=m.Position;using(var w=new BinaryWriter(m,Encoding.UTF8,true)){m.Position=at;w.Write(99);}}}
            using(var sha=SHA256.Create())Buffer.BlockCopy(sha.ComputeHash(future,32,future.Length-32),0,future,0,32);
            File.WriteAllBytes(save,future);File.WriteAllBytes(save+".bak",good);bool newerRejected=false;try{store.Load();}catch(IOException){newerRejected=true;}
            A(newerRejected && File.ReadAllBytes(save).SequenceEqual(future) && File.ReadAllBytes(save+".bak").SequenceEqual(good),"newer save silently rolled back");
            report.Add("PASS newer-format primary never silently falls back to an older backup or overwrites new progress");
            var svc=new CampaignService(Entry(),null);A(!svc.SetLoadout(1<<11),"locked scatter selected");
            for(int i=14;i<20;i++){
                string id;A(svc.Begin(i,false,out id),"chapter progression begin");int reward;var battle=svc.Battle(i,false);
                A(svc.Complete(id,Victory(battle),out reward),"chapter progression commit");A(reward==CampaignCatalog.Stages[i].FirstReward,"first reward");int credits=svc.Profile.MagicCores;A(svc.Complete(id,Victory(battle),out reward) && svc.Profile.MagicCores==credits,"duplicate payment");
                A(svc.MarkStory(i) && svc.MarkTutorial(i),"chapter seen commit");A(svc.Profile.HasSeenStory(i) && !svc.Profile.ShouldShowLesson(i),"chapter ID seen");
                if(i==15){A(svc.SetLoadout((1<<0)|(1<<1)|(1<<2)|(1<<11)),"scatter unlock/loadout");A(!svc.Profile.FirstCleared[17],"premature barrage");}
            }
            A(svc.Profile.IsTowerUnlocked(TowerKind.Scatter) && svc.Profile.LoadoutResults().Contains(TowerKind.Barrage),"new loadout reachability");
            report.Add("PASS normal progression2-1→2-6,2-2 scatter/2-4 barrage/2-5 seal route, first rewards exactly once and seen IDs");
            var fuse=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,svc.Battle(19,false));
            A(fuse.TryBuild(0,TowerKind.Scatter,null,out why),why);A(fuse.TryBuild(1,TowerKind.Rupture,null,out why),why);A(fuse.TryBuild(2,TowerKind.Binding,null,out why),why);
            foreach(var t in fuse.Towers.ToArray())A(fuse.TryUpgrade(t.Id,null,out why),why);
            fuse.Start();fuse.Tick(60.1f,new V2());A(fuse.TryFuse(9,fuse.TowerAt(0).Id,fuse.TowerAt(1).Id,0,"barrage",out why),why);A(fuse.TowerAt(1)==null && fuse.TowerAt(0).Kind==TowerKind.Barrage && fuse.Cores==0,"barrage transition");
            A(fuse.TryUpgrade(fuse.TowerAt(0).Id,null,out why),why);fuse.Tick(60.1f,new V2());A(fuse.TryFuse(10,fuse.TowerAt(0).Id,fuse.TowerAt(2).Id,0,"seal",out why),why);
            A(fuse.TowerAt(0).Kind==TowerKind.Seal && fuse.TowerAt(2)==null && fuse.Cores==0 && fuse.Telemetry.CoresGranted==2 && fuse.Telemetry.CreditsSpent==495,"seal actual cost/catalyst budget");
            A(fuse.TryBuild(1,TowerKind.Autocannon,null,out why) && fuse.TryBuild(2,TowerKind.Autocannon,null,out why),"empty sites cannot be refilled");
            fuse.Tick(180,new V2());A(fuse.SuppliesGranted==5 && fuse.Telemetry.CardOffers==0 && fuse.Telemetry.CoresGranted==2,"long-battle economy inflated");
            report.Add("PASS265 barrage→495 seal/2 catalysts→575 with two replacement spears; five supplies only in8-wave battle");
            var volley=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false,StartingCredits=1000,PlayerDamage=0},7,new CampaignBattle());
            A(volley.TryBuild(0,TowerKind.Scatter,null,out why),why);var tower=volley.TowerAt(0);V2 origin=tower.Position;
            for(int i=0;i<7;i++)volley.Enemies.Add(new Enemy{Id=900+i,Kind=EnemyKind.Brute,Lane=0,Health=1000,MaxHealth=1000,Speed=0,Armor=6,Position=origin+new V2(2,.04f*i)});
            volley.Enemies.Add(new Enemy{Id=999,Kind=EnemyKind.Grunt,Lane=0,Health=1000,MaxHealth=1000,Speed=0,Position=origin+new V2(-2,0)});
            volley.Start();volley.Tick(.2f,new V2());
            A(volley.Enemies.Count(x=>x.Id>=900 && x.Id<907 && x.Health<1000)==4 && volley.LivingEnemy(999).Health==1000,"cone cap/behind target");
            A(Math.Abs(volley.Enemies.Where(x=>x.Id>=900 && x.Id<907).Min(x=>x.Health)-997)<.01f,"scatter armor not applied");
            report.Add("PASS cone volley distinct-target cap, behind-target exclusion and armor reduction");
            var calls=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=true},7,svc.Battle(19,false));calls.WaveCallsEnabled=true;calls.Start();
            while(calls.Elapsed<235){if(calls.AwaitingCard)calls.ChooseCard(calls.CardOffers[0]);if(calls.CanCallNextWave()=="")A(calls.TryCallNextWave(calls.NextCallWave,null,out why),why);calls.Tick(.05f,new V2());calls.Events.Clear();}
            A(calls.Wave==7 && calls.Telemetry.CoresGranted==2 && calls.SuppliesGranted==5 && calls.Telemetry.CardOffers==3,"late-wave calls/supply/card cap");
            report.Add("PASS wave7/8 calls/progress without out-of-range access or extra supplies/cards/catalysts");
            return string.Join("\n",report);
        }
    }
}
