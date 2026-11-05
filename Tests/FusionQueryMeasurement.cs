using System;
using System.Diagnostics;
using Gatefall.Core;

namespace Gatefall.Tests {
    public static class FusionQueryMeasurement {
        public sealed class Row { public int Towers,Iterations,Checksum;public long AllocatedBytes;public double Milliseconds; }
        public static Row Measure(int towers,int iterations) {
            var g=new DefenseSimulation(new GameBalance(),7,new CampaignBattle {Stage=CampaignCatalog.Stages[12],Loadout=263,MarkerUnlocked=true,ObserverUnlocked=true});
            // Query-only occupied-board fixture; no claim of free in-game construction or a battle run.
            for(int i=0;i<towers;i++)g.Towers.Add(new Tower {Id=i+1,Socket=i,Kind=TowerKind.Autocannon,Level=1,Position=g.BoardSockets[i]});
            int mask=g.ReachableTowerFamilies(),checksum=0;
            for(int i=0;i<1000;i++)g.ReachableTowerFamilies();
            var clock=new Stopwatch();clock.Start();clock.Stop();clock.Reset();
            long before=GC.GetAllocatedBytesForCurrentThread();
            clock.Start();for(int i=0;i<iterations;i++)checksum+=g.ReachableTowerFamilies();clock.Stop();
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            if(checksum!=mask*iterations || g.Towers.Count!=towers || g.Elapsed!=0)throw new Exception("Read-only measurement fixture changed");
            return new Row {Towers=towers,Iterations=iterations,Checksum=checksum,AllocatedBytes=allocated,Milliseconds=clock.Elapsed.TotalMilliseconds};
        }
    }
}
