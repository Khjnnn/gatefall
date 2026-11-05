using System;

namespace Gatefall {
    // Diagnostic window only. A transition frame belongs partly to the preceding inactive phase.
    public sealed class ActiveCombatTiming {
        public struct Snapshot {
            public int Frames, Over33Milliseconds;
            public float TotalSeconds, WorstSeconds;
            public float AverageMilliseconds { get { return Frames>0 ? TotalSeconds/Frames*1000 : float.NaN; } }
        }
        readonly float[] frames=new float[600];
        int count,index; bool previousActive;
        void ClearSamples() { count=0;index=0; }
        public void Observe(float seconds,bool active) {
            if(!active) { ClearSamples();previousActive=false;return; }
            if(!previousActive) { previousActive=true;ClearSamples();return; }
            frames[index]=seconds;index=(index+1)%frames.Length;count=Math.Min(count+1,frames.Length);
        }
        public Snapshot Take() {
            var s=new Snapshot {Frames=count};
            for(int i=0;i<count;i++) {
                s.TotalSeconds+=frames[i];s.WorstSeconds=Math.Max(s.WorstSeconds,frames[i]);
                if(frames[i]>1/30f)s.Over33Milliseconds++;
            }
            ClearSamples();return s; // A log boundary does not end consecutive active play.
        }
    }
}
