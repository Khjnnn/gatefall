using System;

namespace Gatefall.Tests {
    public static class ActiveCombatTimingChecks {
        static void Assert(bool condition,string text) { if(!condition)throw new Exception(text); }
        static void Close(float actual,float expected,string text) { Assert(Math.Abs(actual-expected)<.0001f,text); }
        public static string Run() {
            var t=new ActiveCombatTiming();
            t.Observe(50,false);var s=t.Take();Assert(s.Frames==0 && float.IsNaN(s.AverageMilliseconds),"Inactive time reported as combat");
            t.Observe(12,true);t.Observe(.016f,true);t.Observe(.024f,true);s=t.Take();
            Assert(s.Frames==2,"Entry boundary counted");Close(s.AverageMilliseconds,20,"Active average");
            t.Observe(.05f,true);s=t.Take();Assert(s.Frames==1 && s.Over33Milliseconds==1,"Log boundary discarded active frame");
            t.Observe(.02f,true);t.Observe(30,false);s=t.Take();Assert(s.Frames==0,"Paused log retained old active interval");
            t.Observe(9,true);t.Observe(.02f,true);t.Observe(1.25f,true);s=t.Take();
            Assert(s.Frames==2 && s.Over33Milliseconds==1,"Resume boundary or real long frame lost");Close(s.WorstSeconds,1.25f,"Genuine active stall clipped");
            t.Observe(10,false);t.Observe(8,true);t.Observe(.01f,true);s=t.Take();
            Assert(s.Frames==1,"Card/focus transition leaked into resumed samples");Close(s.AverageMilliseconds,10,"Post-card average");
            double expected=0;
            for(int i=0;i<700;i++){float dt=.001f+i*.000001f;t.Observe(dt,true);if(i>=100)expected+=dt;}
            s=t.Take();Assert(s.Frames==600,"Bounded window size");Close(s.TotalSeconds,(float)expected,"Ring window kept wrong samples");
            Assert(t.Take().Frames==0,"Snapshot did not clear samples");
            return "PASS: inactive/entry/resume/card-focus/log boundaries, genuine long frame retained, last600 ring and empty snapshot";
        }
    }
}
