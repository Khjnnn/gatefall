using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests {
    // Product-outside prototype; no world/camera/UI mutation. Actual Android readability is separate.
    public static class NorthHudPlacementReview {
        public static bool Overlap(Box2 a,Box2 b,float gap=0) {
            return a.X < b.X+b.Width+gap && b.X-gap < a.X+a.Width && a.Y < b.Y+b.Height+gap && b.Y-gap < a.Y+a.Height;
        }
        public static Box2 Candidate(float width,IList<Box2> rifts) {
            var original=new Box2(width/2-90.5f,4,181,52);
            bool blocked=false;
            for(int i=0;i<rifts.Count;i++)if(Overlap(original,rifts[i],4)){blocked=true;break;}
            if(!blocked)return original;
            foreach(float w in new[]{181f,150f}) {
                int reach=w>150?72:128, step=w>150?4:2;
                for(int d=0;d<=reach;d+=step)for(int sign=0;sign<(d==0?1:2);sign++) {
                    float center=width/2+(sign==0?d:-d);
                    var b=new Box2(center-w/2,4,w,52);
                    if(b.X<344 || b.X+b.Width>width-322)continue;
                    bool clash=false;for(int i=0;i<rifts.Count;i++)if(Overlap(b,rifts[i],4)){clash=true;break;}
                    if(!clash)return b;
                }
            }
            return original;
        }
    }
}
