using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class CampaignSupplyChecks
    {
        static void Require(bool pass,string why){if(!pass)throw new Exception("Campaign supply: "+why);}
        static void Near(float a,float b,string why){Require(Math.Abs(a-b)<.003f,why+": "+a+" != "+b);}
        static bool Same(CampaignProfile a,CampaignProfile b){return CampaignStore.Encode(a).SequenceEqual(CampaignStore.Encode(b));}
        static CampaignProfile Account(int cleared=4)
        {
            var p=new CampaignProfile{MagicCores=10000,CrystalsFree=60,CrystalsPaid=1000,ShopSeed=12345,HeartAnchorUtc=10000,LastSeenUtc=10000};
            for(int i=0;i<cleared;i++){p.FirstCleared[i]=true;p.BestStars[i]=3;}return p;
        }
        static void Grade(CampaignProfile p,TowerKind kind,int grade)
        {p.Supply.Grades[(int)kind]=grade;p.Supply.Cards[(int)kind]=SupplyCatalog.RequiredCards(grade);}
        static DefenseSimulation Combat(CampaignProfile p,bool crisis)
        {return new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false,PlayerDamage=0},7,new CampaignService(p,null).Battle(3,crisis));}
        static float Damage(CampaignProfile p,TowerKind kind,bool crisis)
        {
            var g=Combat(p,crisis);
            g.Towers.Add(new Tower{Id=1234,Socket=0,Kind=kind,Position=new V2(12,5),Level=1});
            g.Enemies.Add(new Enemy{Id=9999,Health=1000000,MaxHealth=1000000,Speed=0,Position=new V2(12,6)});
            g.Cards[(int)CardKind.Ballistic]=1;g.Start();for(int i=0;i<180;i++){g.Tick(1f/30,new V2());g.Events.Clear();}
            return g.Telemetry.TowerDamage[(int)kind];
        }
        public static string Run()
        {
            var report=new List<string>();int day=20000,writes=0;bool fail=false;CampaignProfile saved=null;
            Action<CampaignProfile> persist=p=>{if(fail)throw new IOException("injected");writes++;saved=p.Copy();};
            var svc=new CampaignService(Account(),persist,()=>10000,()=>day);var before=svc.Profile.Copy();
            fail=true;Require(svc.OpenBox(BoxKind.Normal,n=>0)==null && Same(before,svc.Profile),"normal save rollback");
            Require(svc.OpenBox(BoxKind.Premium,n=>0)==null && Same(before,svc.Profile),"premium save rollback");
            fail=false;var result=svc.OpenBox(BoxKind.Normal,n=>0);
            Require(result!=null && writes==1 && svc.Profile.SaveSerial==1 && svc.Profile.MagicCores==9965 && result.Cards.Count==3,"normal price/rewards/one commit");
            Require(saved.Supply.Cards.Sum()==3 && saved.DailyLimits.NamedCounters["normal-boxes"]==1,"supply and quota persisted together");
            Require(svc.OpenBox(BoxKind.Premium,new Random(19))!=null && writes==2 && svc.Profile.CrystalsFree==0 && svc.Profile.CrystalsPaid==960,"premium free before paid");
            for(int i=1;i<5;i++)Require(svc.OpenBox(BoxKind.Normal,n=>0)!=null,"normal quota "+i);
            before=svc.Profile.Copy();Require(svc.OpenBox(BoxKind.Normal,n=>0)==null && Same(before,svc.Profile) && svc.Error=="일반 상자 일일 한도","normal daily cap");
            day++;Require(svc.OpenBox(BoxKind.Normal,n=>0)!=null && svc.Profile.DailyLimits.NamedCounters["normal-boxes"]==1,"normal daily reset");
            before=svc.Profile.Copy();Require(svc.OpenBox(BoxKind.Normal,n=>n)==null && Same(before,svc.Profile),"bad RNG rollback");
            Require(svc.OpenBox((BoxKind)9,n=>0)==null && Same(before,svc.Profile),"bad box rollback");
            var poor=Account();poor.MagicCores=39;poor.CrystalsFree=99;poor.CrystalsPaid=0;
            var rejected=new CampaignService(poor,p=>{throw new Exception("must not save");},()=>10000,()=>day);
            Require(rejected.OpenBox(BoxKind.Normal,n=>0)==null && rejected.Error=="마핵 부족","core shortfall");
            Require(rejected.OpenBox(BoxKind.Premium,n=>0)==null && rejected.Error=="결정 부족" && Same(poor,rejected.Profile),"crystal shortfall");
            var locked=new CampaignService(Account(2),null,()=>10000,()=>day);
            Require(locked.OpenBox(BoxKind.Normal,n=>0)==null && !locked.PurchaseShopSlot(5) && !locked.RefreshShopByAd() && !locked.GradeUp(TowerKind.Autocannon),"1-3 supply/shop gate");
            var early=new CampaignService(Account(3),null,()=>10000,()=>day);early.Profile.Supply.PremiumPity=29;
            Require(!early.Profile.IsTowerUnlocked(TowerKind.Seal) && early.OddsTable(BoxKind.Premium).PerKind[(int)TowerKind.Seal]==0,"locked final tower odds");
            Require(early.OpenBox(BoxKind.Premium,n=>0)!=null && early.Profile.Supply.PremiumPity==29,"pity waits for final tower unlock");
            var late=new CampaignService(Account(CampaignCatalog.Stages.Length),null,()=>10000,()=>day);
            Require(late.OpenBox(BoxKind.Normal,n=>0).Cards.Count==5 && late.OpenBox(BoxKind.Premium,n=>0).Cards.Count==10,"ChapterReached card count");
            report.Add("PASS boxes: atomic currencies/rewards/pity/quota, one commit, free crystals first, five/day, reset, locks, RNG and chapter scaling");

            var account=Account();account.Supply.Cards[0]=90;svc=new CampaignService(account,persist,()=>10000,()=>day);
            before=svc.Profile.Copy();fail=true;Require(!svc.GradeUp(TowerKind.Autocannon) && Same(before,svc.Profile),"grade save rollback");
            fail=false;int start=writes;foreach(int cost in new[]{10,20,40,80,160}){
                int old=svc.Profile.MagicCores,grade=svc.Profile.Supply.Grades[0];Require(svc.GradeUp(TowerKind.Autocannon),"grade up");
                Require(svc.Profile.MagicCores==old-cost+(grade==5 ? 10 : 0),"grade cost/cumulative surplus");}
            Require(writes==start+5 && svc.Profile.Supply.Cards[0]==80 && svc.Profile.Supply.Grades[0]==6,"five single commits");
            before=svc.Profile.Copy();Require(!svc.GradeUp(TowerKind.Autocannon) && !svc.GradeUp((TowerKind)100) && Same(before,svc.Profile),"grade cap/input");
            report.Add("PASS grades: save rollback, cumulative cards, exact five costs, max-grade surplus and one commit per upgrade");

            svc=new CampaignService(Account(CampaignCatalog.Stages.Length),persist,()=>10000,()=>day);start=writes;before=svc.Profile.Copy();
            var shop=svc.DailyShop();var offers=shop.Offers();
            Require(writes==start && Same(before,svc.Profile) && offers.Length==6,"shop preview mutated account");
            Require(offers.Take(4).All(o=>o.Kind==ShopOfferKind.Card && svc.Profile.IsTowerUnlocked(o.Tower)) && offers.Take(4).Select(o=>o.Tower).Distinct().Count()==4,"four distinct unlocked cards");
            int[] prices={10,20,40,60};Require(offers.Take(4).All(o=>o.Price==prices[(int)SupplyCatalog.Rarity(o.Tower)]) && offers[4].Price==20 && offers[5].Price==0,"shop prices");
            Require(shop.CardKinds.SequenceEqual(svc.DailyShop().CardKinds),"shop deterministic query");
            for(int i=0;i<6;i++){
                before=svc.Profile.Copy();fail=true;Require(!svc.PurchaseShopSlot(i) && Same(before,svc.Profile),"shop save rollback slot "+i);
                fail=false;int cores=svc.Profile.MagicCores,hearts=svc.Profile.Hearts+svc.Profile.ExtraHearts,cards=svc.Profile.Supply.Cards.Sum();start=writes;
                Require(svc.PurchaseShopSlot(i) && writes==start+1 && saved.Shop.PurchasedMask==svc.Profile.Shop.PurchasedMask,"slot one commit "+i);
                Require(svc.Profile.MagicCores==cores-offers[i].Price+(i==5 ? 10 : 0),"slot charge/reward "+i);
                Require(svc.Profile.Supply.Cards.Sum()==cards+(i<4 ? 1 : 0) && svc.Profile.Hearts+svc.Profile.ExtraHearts==hearts+(i==4 ? 1 : 0),"slot grant "+i);
                before=svc.Profile.Copy();Require(!svc.PurchaseShopSlot(i) && Same(before,svc.Profile),"slot bought twice "+i);
            }
            before=svc.Profile.Copy();fail=true;Require(!svc.RefreshShopByAd() && Same(before,svc.Profile),"refresh save rollback");fail=false;
            for(int i=1;i<=2;i++){
                start=writes;Require(svc.RefreshShopByAd() && writes==start+1 && svc.Profile.Shop.RefreshCount==i && svc.Profile.Shop.PurchasedMask==0,"refresh quota/slots");
                Require(svc.PurchaseShopSlot(4) && svc.PurchaseShopSlot(5),"refreshed heart/free");
            }
            Require(svc.Profile.Shop.HeartsBought==3 && !svc.RefreshShopByAd(),"two refresh/three heart caps");
            before=svc.Profile.Copy();Require(!svc.PurchaseShopSlot(-1) && !svc.PurchaseShopSlot(6) && Same(before,svc.Profile),"invalid slots");
            var encoded=CampaignStore.Encode(svc.Profile);var loaded=CampaignStore.Decode(encoded);
            Require(CampaignStore.Encode(loaded).SequenceEqual(encoded) && loaded.Shop.CardKinds.SequenceEqual(svc.Profile.Shop.CardKinds),"persist exact list/purchases/seed/supply");
            var copy=loaded.Copy();copy.Supply.Cards[0]++;copy.Supply.Grades[0]=2;copy.Shop.CardKinds[0]=14;
            Require(CampaignStore.Encode(loaded).SequenceEqual(encoded),"supply/shop deep copy");
            day++;var next=svc.DailyShop();Require(next.LocalDay==day && next.RefreshCount==0 && next.PurchasedMask==0 && next.HeartsBought==0,"new day preview reset");
            Require(svc.Profile.Shop.LocalDay==day-1 && Same(svc.Profile,loaded),"new day preview wrote old state");
            var second=new CampaignService(loaded,null,()=>10000,()=>day);Require(next.CardKinds.SequenceEqual(second.DailyShop().CardKinds),"reload/new day determinism");
            Require(svc.PurchaseShopSlot(4) && svc.Profile.Shop.HeartsBought==1 && svc.Profile.Shop.LocalDay==day,"new day purchase reset");
            var changed=new HashSet<string>();for(int seed=1;seed<16;seed++){var p=Account(CampaignCatalog.Stages.Length);p.ShopSeed=seed;
                changed.Add(string.Join(",",new CampaignService(p,null,()=>10000,()=>day).DailyShop().CardKinds));}
            Require(changed.Count>1,"per-profile seed ignored");
            var full=Account();full.Hearts=5;full.ExtraHearts=999;var capped=new CampaignService(full,null,()=>10000,()=>day);
            Require(!capped.PurchaseShopSlot(4) && Same(full,capped.Profile),"full hearts charged");
            var invalid=new Action<CampaignProfile>[] {p=>p.Supply=null,p=>p.Supply.Grades[0]=7,p=>p.Shop=null,p=>p.Shop.RefreshCount=3,p=>p.Shop.PurchasedMask=64,p=>p.Shop.HeartsBought=4,p=>p.ShopSeed=-1};
            foreach(var change in invalid){var p=Account();change(p);Require(!p.Valid(),"invalid supply/shop profile accepted");}
            report.Add("PASS shop/save v4: six slots, rarity prices, rollback, deterministic seed/day/refresh, stored list, daily limits, reset, deep copy and validation");

            account=Account();account.Research=new[]{2,1,2,1};account.Growth[0]=3;var upgraded=account.Copy();
            foreach(TowerDefinition def in TowerCatalog.All){
                TowerKind kind=def.Kind;Grade(upgraded,kind,6);var a=Combat(account,false);var b=Combat(upgraded,false);var crisis=Combat(upgraded,true);
                a.Cards[0]=b.Cards[0]=crisis.Cards[0]=1;
                var sa=a.Stats(kind,1);var sb=b.Stats(kind,1);var sc=crisis.Stats(kind,1);
                float raw=kind==TowerKind.Autocannon ? 14 : def.Levels[0].Damage;
                Near(sb.Damage-sa.Damage,raw*.1f,"additive primary "+kind);Near(sc.Damage,sa.Damage,"crisis primary "+kind);
                Near(sb.Secondary-sa.Secondary,def.Levels[0].Secondary*.1f,"additive secondary "+kind);
                if(sa.Chain!=null)for(int i=0;i<sa.Chain.Length;i++){Near(sb.Chain[i]-sa.Chain[i],def.Levels[0].Chain[i]*.1f,"additive chain");Near(sc.Chain[i],sa.Chain[i],"crisis chain");}
                if(kind!=TowerKind.Barrier){float normal=Damage(account,kind,false),bonus=Damage(upgraded,kind,false);
                    Require(normal>0 && bonus>normal,"scripted bonus damage "+kind);Near(Damage(upgraded,kind,true),Damage(account,kind,true),"scripted crisis damage "+kind);}
                Grade(upgraded,kind,1);
            }
            account=Account();Grade(account,TowerKind.Autocannon,6);Grade(account,TowerKind.Binding,6);Grade(account,TowerKind.Arc,3);
            var battle=new CampaignService(account,null).Battle(3,false);var battleCopy=battle.Copy();battleCopy.Supply.Grades[3]=6;
            Require(battle.Supply.Grades[3]==3,"battle copy alias");var fusion=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false,StartingCredits=1000},7,battle);
            string why;Require(fusion.TryBuild(0,TowerKind.Autocannon,null,out why) && fusion.TryBuild(1,TowerKind.Binding,null,out why),"fusion materials");
            var first=fusion.TowerAt(0);var last=fusion.TowerAt(1);Require(fusion.TryUpgrade(first.Id,null,out why) && fusion.TryUpgrade(last.Id,null,out why),"fusion upgrades");
            typeof(DefenseSimulation).GetProperty("Cores").SetValue(fusion,1,null);Require(fusion.TryFuse(0,first.Id,last.Id,0,null,out why),"actual fusion: "+why);
            var fused=fusion.Towers.Single();Near(fusion.Stats(fused).Damage,TowerCatalog.Get(TowerKind.Arc).Levels[0].Damage*1.04f,"fused own grade");
            account.Supply.Grades[3]=6;Require(battle.Supply.Grades[3]==3 && fusion.Rules.Supply.Grades[3]==3,"battle snapshot alias");
            report.Add("PASS combat: all fifteen kinds, additive primary/secondary/chain with research/cards, scripted normal/crisis damage, actual fusion own grade and battle snapshots");
            return string.Join("\n",report);
        }
    }
}
