using System;
using System.Collections.Generic;
using System.Linq;

namespace Gatefall.Tests
{
    public static class SealInheritanceChecks
    {
        static void Require(bool condition,string message)
        { if(!condition)throw new InvalidOperationException(message); }

        public static string[] Run()
        { return Run(SealInheritanceReview.Run(true),SealInheritanceReview.RunVolleys(true)); }

        public static string[] Run(SealInheritanceReview.Row[] rows,SealInheritanceReview.Volley[] volleys)
        {
            var report=new List<string>();
            Require(rows.Length==1170 && volleys.Length==120,"Expanded v2 fixture count changed.");
            foreach(string route in new[]{"Observer","Barrage","Surge"}) {
                foreach(string variant in new[]{"ceiling","reserved"}) {
                    var legacy=rows.Where(r=>r.Route==route && r.Variant==variant && r.Policy=="legacy").ToArray();
                    var inherited=rows.Where(r=>r.Route==route && r.Variant==variant && r.Policy=="inherited").ToArray();
                    Require(legacy.Length==(route=="Surge" ? 45 : 75) && legacy.Length==inherited.Length,"Missing paired games.");
                    int wins=legacy.Count(r=>r.Outcome=="Won"), stars=legacy.Count(r=>r.Stars==3);
                    int newWins=inherited.Count(r=>r.Outcome=="Won"), newStars=inherited.Count(r=>r.Stars==3);
                    Require(newWins>=wins && newStars>=stars,route+" "+variant+" regressed wins or three stars.");
                    Require((newStars-stars)*10<=legacy.Length,route+" "+variant+" exceeds the 10% three-star band.");
                    for(int i=0;i<legacy.Length;i++) {
                        var a=legacy[i]; var b=inherited[i];
                        Require(a.Stage==b.Stage && a.PrimaryLane==b.PrimaryLane && a.Budget==b.Budget && a.Seed==b.Seed,"Unpaired condition.");
                        Require(a.Outcome!="Won" || b.Outcome=="Won",route+" lost a condition won by legacy: "+a.Stage+
                            " lane "+a.PrimaryLane+" budget "+a.Budget+" seed "+a.Seed+" "+variant);
                    }
                    report.Add("PASS "+route+" "+variant+": wins "+wins+" -> "+newWins+"/"+legacy.Length+", three stars "+stars+" -> "+newStars);
                }
                var compact=volleys.Where(v=>v.Route==route && v.Formation=="compact quartet" && v.Policy=="inherited").ToArray();
                Require(compact.Length==5 && compact.All(v=>v.Kills>=2),route+" must kill at least two compact enemies per shot.");
                report.Add("PASS "+route+" compact quartet: "+compact[0].Kills+" kills on all five seeds");
            }
            return report.ToArray();
        }
    }
}
