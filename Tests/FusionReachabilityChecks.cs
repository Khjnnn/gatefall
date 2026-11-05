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
            return "PASS: family/gate/owned-resource cases and read-only state/card-RNG comparison";
        }
    }
}
