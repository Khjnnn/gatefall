using System;
using System.Collections.Generic;
using System.Linq;
using Gatefall.Core;

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
            Require(rows.Length==1170 && volleys.Length==140,"Expanded v2 fixture count changed.");
            report.AddRange(RunMechanics());
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
            foreach(int seed in new[]{7,19,41,101,202}) {
                var marked=volleys.Single(v=>v.Route=="Observer" && v.Formation=="marked elite spread" && v.Policy=="inherited" && v.Seed==seed);
                var legacy=volleys.Single(v=>v.Route=="Observer" && v.Formation=="marked elite spread" && v.Policy=="legacy" && v.Seed==seed);
                var unmarked=volleys.Single(v=>v.Route=="Observer" && v.Formation=="unmarked elite spread" && v.Policy=="inherited" && v.Seed==seed);
                Require(marked.Kills>=legacy.Kills+2 && marked.ThreatHp==0 && legacy.ThreatHp>=155 && marked.BlastRadius==4,
                    "Observer must clearly beat legacy against a marked elite behind the front line.");
                Require(marked.Kills>=unmarked.Kills+2 && unmarked.BlastRadius==2,"The spread volley must reward marker setup.");
            }
            report.Add("PASS Observer marked elite spread: three kills vs legacy/unmarked zero, on all five seeds");
            return report.ToArray();
        }

        static DefenseSimulation Impact(float mark,SealInheritance inheritance,out Enemy primary,out Enemy[] escorts,bool previous=false,bool edge=false)
        {
            var game=new DefenseSimulation(new GameBalance { SpawnEnemies=false,OfferCards=false,PlayerDamage=0,PlayerRange=.1f },7);
            TowerStats stats=game.Stats(new Tower { Kind=TowerKind.Seal,Inheritance=inheritance });
            if(previous) { stats.MarkedRadiusMultiplier=1; stats.Slow=.35f; }
            V2 origin=new V2(15,8);
            primary=new Enemy { Id=9000,Position=origin,Health=1000,MaxHealth=1000,MarkRemaining=mark,MarkBonus=mark>0 ? 5 : 0 };
            escorts=(edge ? new[]{2f,3.99f,4.01f} : new[]{2f,3.99f,4.01f,1f,1.5f}).Select((distance,i)=>new Enemy {
                Id=9001+i,Position=origin+new V2(distance,0),Health=60,MaxHealth=60 }).ToArray();
            game.Enemies.Add(primary);game.Enemies.AddRange(escorts);
            game.Projectiles.Add(new Projectile { Id=9100,SourceId=123,TargetId=primary.Id,Kind=TowerKind.Seal,FromTower=true,
                Stats=stats,Position=primary.Position,Speed=20 });
            game.Start();game.Tick(.01f,new V2());return game;
        }

        public static string[] RunMechanics()
        {
            Enemy primary;Enemy[] escorts;
            var game=Impact(3,SealInheritance.MarkedBlast,out primary,out escorts);
            Require(Math.Abs(primary.Health-827)<.001f && game.Kills==3,"Marked direct damage or four-target cap changed.");
            Require(escorts[0].Health==0 && escorts[3].Health==0 && escorts[4].Health==0 && escorts[1].Health==60 && escorts[2].Health==60,
                "Expanded splash must retain lead ordering and its target cap.");
            Require(primary.MarkRemaining==3 && escorts.All(e=>e.MarkRemaining==0),"Only the primary may receive the Observer mark.");
            Require(game.Events.Single(e=>e.Kind==EventKind.Blast).Value==4 && Math.Abs(primary.SlowFraction-.30f)<.001f,
                "Marked blast radius/event or traded slow changed.");
            game=Impact(3,SealInheritance.MarkedBlast,out primary,out escorts,edge:true);
            Require(game.Kills==2 && escorts[1].Health==0 && escorts[2].Health==60,"Expanded radius boundary changed.");
            game=Impact(3,SealInheritance.MarkedBlast,out primary,out escorts,previous:true,edge:true);
            Require(game.Kills==1 && escorts[1].Health==60,"The new radius must reach an enemy the v0.33.1 Observer cannot.");
            foreach(float mark in new[]{0f,.005f}) {
                game=Impact(mark,SealInheritance.MarkedBlast,out primary,out escorts);
                Require(primary.Health==880 && game.Events.Single(e=>e.Kind==EventKind.Blast).Value==2,
                    "Absent/expired mark must not be replaced by this impact's new mark before the radius check.");
            }
            game.Projectiles.Add(new Projectile { Id=9200,SourceId=123,TargetId=primary.Id,Kind=TowerKind.Seal,FromTower=true,
                Stats=game.Stats(new Tower { Kind=TowerKind.Seal,Inheritance=SealInheritance.MarkedBlast }),Position=primary.Position,Speed=20 });
            game.Tick(.01f,new V2());
            Require(Math.Abs(primary.Health-707)<.001f && game.Events.Last(e=>e.Kind==EventKind.Blast).Value==4,
                "A surviving primary's refreshed mark must reward the next shot.");
            game=Impact(3,SealInheritance.None,out primary,out escorts);
            Require(primary.Health==875 && game.Events.Single(e=>e.Kind==EventKind.Blast).Value==2 && Math.Abs(primary.SlowFraction-.35f)<.001f,
                "Legacy Seal changed.");
            return new[]{"PASS Observer mechanics: live/expired mark before self-mark, next-shot payoff, direct x1.4, radius x2/boundary, primary-only mark, target cap/order, legacy unchanged"};
        }
    }
}
