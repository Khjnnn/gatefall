using System;
using System.Collections.Generic;

namespace Gatefall.Tests {
    // Used only by a product-outside source snapshot. No game object is retained or mutated.
    public static class LaneDamageObservation {
        public sealed class Row {
            public double Time;
            public int EnemyId, Lane, Kind;
            public float CoreBefore, Requested, Effective, CoreAfter;
            public bool Shield;
        }
        public static readonly List<Row> Rows=new List<Row>();
        public static void Record(double time,int id,int lane,int kind,float before,float hit,bool shield) {
            Rows.Add(new Row {Time=time,EnemyId=id,Lane=lane,Kind=kind,CoreBefore=before,Requested=hit,
                Effective=Math.Min(before,hit),CoreAfter=Math.Max(0,before-hit),Shield=shield});
        }
    }
}
