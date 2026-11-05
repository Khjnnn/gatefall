using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Gatefall.Core;
namespace Gatefall.Tests {
    public static class RunObservationChecks {
        static void Assert(bool value,string why){if(!value)throw new Exception(why);}
        static string Encoded(CampaignProfile p){return Convert.ToBase64String(CampaignStore.Encode(p));}
        static CampaignProfile Profile(){
            var p=new CampaignProfile{MagicCores=42};for(int i=0;i<8;i++){p.BestStars[i]=3;p.FirstCleared[i]=true;}return p;
        }
        static DefenseSimulation Finished(CampaignService service,bool crisis,bool won){
            var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},-123,service.Battle(7,crisis));
            g.Schedule.Clear();g.Start();
            // Domain transaction fixture: combat victory/loss itself is covered by the simulation checks.
            if(!won)typeof(DefenseSimulation).GetProperty("CoreHealth",BindingFlags.Instance|BindingFlags.Public).SetValue(g,0f);
            g.Tick(.01f,new V2());Assert(g.State==(won ? RunState.Won : RunState.Lost),"Terminal fixture differs");return g;
        }
        public static string Run(){
            foreach(int seed in new[]{7,-123,int.MinValue})foreach(bool crisis in new[]{false,true}){
                var rules=new CampaignBattle{Stage=CampaignCatalog.Stages[11],Crisis=crisis,Hunter=1,EarnedGrowth=2,Research=new[]{3,2,2,2}};
                var g=new DefenseSimulation(new GameBalance{SpawnEnemies=false},seed,rules);
                var control=new DefenseSimulation(new GameBalance{SpawnEnemies=false},seed,rules);
                Assert(g.Seed==seed && g.RunContext().Contains("stage=1-12 mode="+(crisis ? "Crisis" : "Normal")+" seed="+seed.ToString(System.Globalization.CultureInfo.InvariantCulture)),"Seed/mode/stage context");
                Assert(g.RunContext().Contains("hunter=1 growth=2 research=3,2,2,2 loadout=135 cards=0,0,0,0,0,0"),"Account/card context");
                for(int i=0;i<100;i++)g.RunContext();g.Start();control.Start();
                for(int frame=0;frame<95*30;frame++){
                    Assert(g.CardOffers.SequenceEqual(control.CardOffers),"Observation consumed card RNG");
                    if(g.AwaitingCard){g.ChooseCard(g.CardOffers[0]);control.ChooseCard(control.CardOffers[0]);}
                    if(frame%30==0)g.RunContext();
                    g.Tick(1f/30,new V2());control.Tick(1f/30,new V2());g.Events.Clear();control.Events.Clear();
                }
                Assert(g.State==control.State && g.Elapsed==control.Elapsed && g.Credits==control.Credits && g.CoreHealth==control.CoreHealth && g.Cards.SequenceEqual(control.Cards),"Read-only context changed game");
            }
            Assert(new DefenseSimulation(new GameBalance(),4).RunContext().StartsWith("Context stage=lab mode=Lab seed=4 "),"Lab context");
            foreach(bool crisis in new[]{false,true})foreach(bool won in new[]{false,true}){
                bool fail=false;int writes=0;byte[] written=null;
                var service=new CampaignService(Profile(),p=>{if(fail)throw new IOException("injected result save failure");written=CampaignStore.Encode(p);writes++;});
                string id;Assert(service.Begin(7,crisis,out id),"Begin result fixture");var game=Finished(service,crisis,won);
                string pending=Encoded(service.Profile);fail=true;int reward;
                Assert(!service.Complete(id,game,out reward) && reward==0 && Encoded(service.Profile)==pending,"Failed result changed pending/account");
                fail=false;Assert(service.Complete(id,game,out reward) && reward==(won?2:0),"Result retry reward");
                Assert(service.Profile.PendingRun=="" && service.Profile.PendingStage==-1 && !service.Profile.PendingCrisis,"Outcome left pending mode");
                Assert(game.RunContext().Contains("mode="+(crisis ? "Crisis" : "Normal")),"Profile cleanup erased recorded battle mode");
                Assert(service.Profile.MagicCores==(won?44:42) && !CampaignStore.Decode(written).PendingCrisis,"Persisted outcome differs");
                string complete=Encoded(service.Profile);int saved=writes;
                Assert(service.Complete(id,game,out reward) && Encoded(service.Profile)==complete && writes==saved,"Duplicate outcome changed account or rewrote save");
            }
            const string oldId="0123456789abcdef0123456789abcdef";
            var legacy=Profile();legacy.LastRewardRun=oldId;legacy.LastReward=2;legacy.PendingCrisis=true;
            legacy=CampaignStore.Decode(CampaignStore.Encode(legacy));string bytes=Encoded(legacy);int legacyWrites=0;
            var old=new CampaignService(legacy,p=>{legacyWrites++;});int oldReward;
            Assert(old.Complete(oldId,null,out oldReward) && oldReward==2 && Encoded(old.Profile)==bytes && legacyWrites==0,"Legacy duplicate not preserved");
            string normalId;Assert(old.Begin(7,false,out normalId) && !old.Profile.PendingCrisis,"Legacy stale mode blocked normal begin");
            return "PASS: seed/mode/account/card context, six read-only RNG pairs, lab, normal/crisis win/loss failure+retry+duplicate, legacyv2/stale-mode compatibility";
        }
    }
}
