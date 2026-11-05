using System;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class FusionReachabilityChecks {
        static void Assert(bool condition,string message) { if(!condition)throw new Exception(message); }
        static DefenseSimulation New(int mask,bool observer=false,int stage=12) {
            return new DefenseSimulation(new GameBalance(),7,new CampaignBattle {
                Stage=CampaignCatalog.Stages[stage],Loadout=mask,MarkerUnlocked=true,ObserverUnlocked=observer
            });
        }
        static bool Has(int mask,TowerKind kind) { return (mask&(1<<(int)kind))!=0; }
        static void Fusions(DefenseSimulation g,params TowerKind[] expected) {
            int mask=g.ReachableTowerFamilies();
            foreach(TowerDefinition d in TowerCatalog.All)if(d.Tier>1)
                Assert(Has(mask,d.Kind)==Array.Exists(expected,k=>k==d.Kind),"Unexpected family "+d.Kind+" for loadout"+g.Rules.Loadout);
        }
        public static string Run() {
            Fusions(New(1));
            Fusions(New(3),TowerKind.Arc);
            Fusions(New(5),TowerKind.Shatter);
            Fusions(New(6),TowerKind.Gravity);
            Fusions(New(7),TowerKind.Arc,TowerKind.Shatter,TowerKind.Gravity,TowerKind.Seal);
            Fusions(New(257)); // Marker+Spear, Observer still locked.
            Fusions(New(257,true),TowerKind.Observer);
            Fusions(New(263,true),TowerKind.Arc,TowerKind.Shatter,TowerKind.Gravity,TowerKind.Seal,TowerKind.Observer,TowerKind.Beacon);
            Fusions(New(258),TowerKind.Beacon);
            Fusions(New(258,false,9));
            Fusions(New(1,false,0));
            Fusions(New(1,false,2),TowerKind.Arc); // Early operations ignore the future loadout mask.
            var owned=New(1);owned.Towers.Add(new Tower {Id=99,Kind=TowerKind.Gravity,Level=2,Socket=0,Position=owned.BoardSockets[0]});
            Fusions(owned,TowerKind.Gravity,TowerKind.Seal); // A real owned ingredient must not disappear.
            var missingResources=New(3);string reason;
            for(int i=0;i<8;i++)Assert(missingResources.TryBuild(i,TowerKind.Autocannon,null,out reason),"Zero-credit fixture build failed");
            Assert(missingResources.Credits==0,"Zero-credit fixture differs");
            Fusions(missingResources,TowerKind.Arc);
            Assert(missingResources.Cores==0,"Catalyst fixture differs");
            var queried=New(263,true);var control=New(263,true);
            int credits=queried.Credits,events=queried.Events.Count,groups=queried.Schedule.Count;
            for(int i=0;i<100;i++)queried.ReachableTowerFamilies();
            Assert(queried.Credits==credits && queried.Events.Count==events && queried.Schedule.Count==groups && queried.Towers.Count==0 && queried.Elapsed==0,"Query mutated preparation");
            queried.Start();control.Start();queried.Tick(30.1f,new V2());control.Tick(30.1f,new V2());
            Assert(queried.State==control.State && queried.Credits==control.Credits && queried.Enemies.Count==control.Enemies.Count &&
                queried.CardOffers.Count==control.CardOffers.Count,"Query changed subsequent state");
            for(int i=0;i<queried.CardOffers.Count;i++)Assert(queried.CardOffers[i]==control.CardOffers[i],"Query consumed card RNG");
            NestedFusionAtTowerLimit();
            return "PASS: family/gate/owned-resource cases, read-only state/card-RNG comparison, nested fusion at cap and refill";
        }
        static void NestedFusionAtTowerLimit() {
            var g=new DefenseSimulation(new GameBalance {StartingCredits=5000,SpawnEnemies=false,OfferCards=false},7,
                new CampaignBattle {Stage=CampaignCatalog.Stages[13],Loadout=135});
            string why;
            Assert(g.TryBuild(4,TowerKind.Autocannon,null,out why) && g.TryBuild(1,TowerKind.Binding,null,out why) &&
                g.TryBuild(7,TowerKind.Rupture,null,out why),"Nested material construction");
            foreach(int site in new[]{4,1,7})Assert(g.TryUpgrade(g.TowerAt(site).Id,null,out why),"Nested material upgrade");
            g.Start();g.Tick(61,new V2());
            Assert(g.Cores==1 && g.TryFuse(0,g.TowerAt(4).Id,g.TowerAt(1).Id,4,null,out why),"First catalyst fusion");
            Assert(g.TryUpgrade(g.TowerAt(4).Id,null,out why),"Intermediate Lv2");
            for(int i=0;g.Towers.Count<g.TowerLimit;i++)
                if(g.TowerAt(i)==null)Assert(g.TryBuild(i,TowerKind.Autocannon,null,out why),"Fill available cap");
            int count=g.Towers.Count,unused=0;
            while(g.TowerAt(unused)!=null)unused++;
            int credits=g.Credits;
            Assert(!g.TryBuild(unused,TowerKind.Autocannon,null,out why) && g.Towers.Count==count && g.Credits==credits,"Full cap must reject extra construction atomically");
            g.Tick(60,new V2());credits=g.Credits;
            Assert(g.Cores==1 && g.TryFuse(3,g.TowerAt(4).Id,g.TowerAt(7).Id,4,null,out why),"Nested fusion must work at full cap");
            Assert(g.Cores==0 && g.Credits==credits-80 && g.Towers.Count==count-1 && g.TowerAt(7)==null &&
                g.TowerAt(4).Kind==TowerKind.Seal && g.TowerAt(4).InvestedCores==2,"Nested cost, catalysts, anchor and freed partner");
            Assert(g.TryBuild(7,TowerKind.Autocannon,null,out why) && g.Towers.Count==count && g.Credits==credits-120,"Freed material site can refill to cap");
        }
    }
}
