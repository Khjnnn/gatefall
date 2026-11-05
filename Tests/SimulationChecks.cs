using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class SimulationChecks
    {
        private static readonly List<string> report=new List<string>();
        private static int checks;
        private static void Assert(bool value,string why) { if(!value)throw new Exception(why); }
        private static void Near(double a,double b,double tolerance=.02) { Assert(Math.Abs(a-b)<tolerance,a+" != "+b); }
        private static void Check(string name,Action action) { action(); checks++; report.Add("PASS "+name); }
        private static DefenseSimulation New(int credits=1000,bool cards=false,bool spawn=false)
        { var g=new DefenseSimulation(new GameBalance { StartingCredits=credits,OfferCards=cards,SpawnEnemies=spawn },7); g.Start(); return g; }
        private static void Advance(DefenseSimulation g,float seconds) { g.Tick(seconds,new V2()); g.Events.Clear(); }
        private static void Move(DefenseSimulation g,int socket)
        {
            for(int i=0;i<1200 && V2.Distance(g.PlayerPosition,DefenseSimulation.Sockets[socket])>.12f;i++) {
                V2 d=DefenseSimulation.Sockets[socket]-g.PlayerPosition;
                // Route around the solid core rather than pushing through its centre.
                if(g.PlayerPosition.Length<1.5f && V2.Distance(g.PlayerPosition+d.Normalized*.2f,new V2())<1.35f)d=new V2(2,-2)-g.PlayerPosition;
                g.Tick(1f/60,d.Normalized); g.Events.Clear();
            }
            Assert(V2.Distance(g.PlayerPosition,DefenseSimulation.Sockets[socket])<DefenseSimulation.BuildDistance,"Could not reach socket "+socket);
        }
        private static Tower Build(DefenseSimulation g,int socket,TowerKind kind,bool upgrade=true)
        { Move(g,socket); string why; Assert(g.TryBuild(socket,kind,null,out why),why); Tower t=g.TowerAt(socket); if(upgrade)Assert(g.TryUpgrade(t.Id,null,out why),why); return t; }
        private static Tower Fuse(DefenseSimulation g,int recipe,Tower a,Tower b,int socket,string token=null)
        { Move(g,socket); string why; Assert(g.TryFuse(recipe,a.Id,b.Id,socket,token,out why),why); return g.TowerAt(socket); }
        private static Enemy Dummy(DefenseSimulation g,int id,V2 position,EnemyKind kind=EnemyKind.Grunt,float hp=100)
        { var e=new Enemy { Id=id,Kind=kind,Position=position,Health=hp,MaxHealth=hp,Speed=0 }; g.Enemies.Add(e); return e; }
        private static string Fingerprint(DefenseSimulation g)
        { string s=g.Credits+"/"+g.Cores; foreach(Tower t in g.Towers)s+="|"+t.Id+":"+t.Kind+":"+t.Level+":"+t.PaidCredits; return s; }
        private static void Impact(DefenseSimulation g,TowerKind kind,Enemy primary,bool fixedImpact=false)
        { g.Projectiles.Add(new Projectile { Id=9000,SourceId=6000,TargetId=primary.Id,Position=primary.Position,ImpactPosition=primary.Position,Kind=kind,FromTower=true,FixedImpact=fixedImpact,Stats=g.Stats(kind,1),Speed=20 }); Advance(g,.02f); }
        public static string RunAll()
        {
            report.Clear(); checks=0;
            Check("Ready gates commands; start grants only initial140",()=> {
                var g=new DefenseSimulation(new GameBalance(),1); string r; Assert(!g.TryBuild(4,out r),"build before start"); Advance(g,10); Near(g.Elapsed,0); g.Start(); Assert(g.Credits==140,"initial economy"); });
            Check("Nearby construction, exact spending, duplicate command rejection",()=> {
                var g=New(140); string r; Assert(!g.TryBuild(0,out r),"remote construction"); Assert(g.TryBuild(4,TowerKind.Binding,"b",out r),r);
                Assert(g.Credits==95 && g.TowerAt(4).PaidCredits==45,"cost"); string before=Fingerprint(g); Assert(!g.TryBuild(4,TowerKind.Binding,"b",out r),"duplicate"); Assert(before==Fingerprint(g),"mutated"); });
            Check("Three basics have different upgrade costs and no direct hybrid purchase",()=> {
                var g=New(); string r; Assert(!g.TryBuild(4,TowerKind.Arc,null,out r),"direct arc");
                var a=Build(g,4,TowerKind.Autocannon); var b=Build(g,7,TowerKind.Binding); var c=Build(g,2,TowerKind.Rupture);
                Assert(a.PaidCredits==70 && b.PaidCredits==85 && c.PaidCredits==120,"upgrade totals"); });
            Check("Invalid upgrade and insufficient funds are atomic",()=> {
                var g=New(40); var a=Build(g,4,TowerKind.Autocannon,false); string before=Fingerprint(g),r;
                Assert(!g.TryUpgrade(a.Id,"u",out r),"free upgrade"); Assert(before==Fingerprint(g),"mutation"); Assert(!g.TryUpgrade(-1,null,out r),"missing id"); });
            Check("All first fusion recipes consume real towers, commutative IDs and inherit investments",()=> {
                for(int i=0;i<3;i++) { var g=New(); var r=TowerCatalog.Recipes[i]; var a=Build(g,4,r.First); var b=Build(g,7,r.Second); Advance(g,60);
                    var t=Fuse(g,i,b,a,4); Assert(t.PaidCredits==new[]{195,235,250}[i] && t.InvestedCores==1,"inheritance");
                    Assert(g.Towers.Count==1 && g.TowerAt(7)==null && g.TowerById(a.Id)==null && t.Id!=b.Id,"material consumption"); }
            });
            Check("All capstone paths cost455, invest2 cores and sell for341+2",()=> {
                for(int i=0;i<3;i++) { var g=New(); var r=TowerCatalog.Recipes[i]; var a=Build(g,4,r.First); var b=Build(g,7,r.Second); Advance(g,60);
                    var hybrid=Fuse(g,i,a,b,4); string why; Assert(g.TryUpgrade(hybrid.Id,null,out why),why);
                    var c=Build(g,7,TowerCatalog.Recipes[i+3].Second); if(g.Elapsed<120)Advance(g,(float)(120-g.Elapsed)+.02f);
                    var seal=Fuse(g,i+3,hybrid,c,4,"seal"); Assert(seal.PaidCredits==455 && seal.InvestedCores==2,"capstone accounting");
                    Assert(g.Telemetry.CreditsSpent==455,"double spending history"); string before=Fingerprint(g);
                    Assert(!g.TryFuse(i+3,hybrid.Id,c.Id,4,"seal",out why),"replayed command"); Assert(before==Fingerprint(g),"replay mutated");
                    int credits=g.Credits; Assert(g.TrySell(seal.Id,"sell",out why),why); Assert(g.Credits==credits+341 && g.Cores==2 && g.Towers.Count==0,"sale");
                    Assert(!g.TrySell(seal.Id,"sell",out why),"double refund"); }
            });
            Check("Failed fusion preserves board and economy for ID, level, anchor, range, funds and cores",()=> {
                var g=New(155); var a=Build(g,4,TowerKind.Autocannon); var b=Build(g,7,TowerKind.Binding); Move(g,4); string before=Fingerprint(g),r;
                foreach(int[] args in new[]{new[]{a.Id,a.Id,4},new[]{a.Id,-2,4},new[]{a.Id,b.Id,0},new[]{a.Id,b.Id,4}})
                    Assert(!g.TryFuse(0,args[0],args[1],args[2],null,out r) && Fingerprint(g)==before,"failure mutated state");
                Advance(g,30); before=Fingerprint(g); Assert(!g.TryFuse(0,a.Id,b.Id,4,null,out r) && r.Contains("촉매") && Fingerprint(g)==before,"missing catalyst");
                b.Level=1; Advance(g,30); before=Fingerprint(g); Assert(!g.TryFuse(0,a.Id,b.Id,4,null,out r) && Fingerprint(g)==before,"level mismatch");
                b.Level=2; Move(g,0); before=Fingerprint(g); Assert(!g.TryFuse(0,a.Id,b.Id,4,null,out r) && Fingerprint(g)==before,"remote fusion");
            });
            Check("First Arc is reachable from guaranteed income at60 without kills",()=> {
                var g=New(140); var a=Build(g,4,TowerKind.Autocannon); var b=Build(g,7,TowerKind.Binding,false);
                if(g.Elapsed<30)Advance(g,(float)(30-g.Elapsed)+.02f); string r; Move(g,7); Assert(g.TryUpgrade(b.Id,null,out r),r);
                if(g.Elapsed<60)Advance(g,(float)(60-g.Elapsed)+.02f); var arc=Fuse(g,0,a,b,4);
                Assert(arc.Kind==TowerKind.Arc && g.Kills==0 && g.Credits==25,"first fusion economy"); });
            Check("Paused menu purchases work but paused combat cannot advance",()=> {
                var g=New(); g.TogglePause(); double time=g.Elapsed; string r; Assert(g.TryBuild(4,out r),r); Advance(g,25); Near(g.Elapsed,time); Assert(g.State==RunState.Paused,"unpaused"); });
            Check("Upgrade preserves remaining attack-cooldown fraction",()=> {
                var g=New(); var t=Build(g,4,TowerKind.Autocannon,false); t.Kind=TowerKind.Arc; t.Cooldown=.375f; string r;
                Assert(g.TryUpgrade(t.Id,null,out r),r); Near(t.Cooldown,.325); });
            Check("Fusion waits commission plus full interval before its first shot",()=> {
                var g=New(); var a=Build(g,4,TowerKind.Autocannon); var b=Build(g,7,TowerKind.Binding); Advance(g,60); var arc=Fuse(g,0,a,b,4);
                g.Balance.PlayerDamage=0; Dummy(g,500,new V2(0,-6),EnemyKind.Grunt,10000); g.Events.Clear(); g.Tick(1.7f,new V2());
                Assert(!g.Events.Exists(e=>e.Kind==EventKind.TowerShot),"instant fusion shot"); g.Tick(.1f,new V2()); Assert(g.Events.Exists(e=>e.Kind==EventKind.TowerShot),"never fires"); });
            Check("Projectile keeps launch damage after upgrade and sale",()=> {
                var g=New(); g.Balance.PlayerDamage=0; var t=Build(g,4,TowerKind.Autocannon,false); Enemy e=Dummy(g,501,new V2(0,-6));
                Advance(g,.01f); Assert(g.Projectiles.Exists(p=>p.FromTower),"no launch"); string r; Assert(g.TryUpgrade(t.Id,null,out r),r); Assert(g.TrySell(t.Id,null,out r),r);
                Advance(g,.3f); Near(e.Health,90); });
            Check("Arc visits3 unique enemies and never bounces back",()=> {
                var g=New(); g.Balance.PlayerDamage=0;
                var a=Dummy(g,501,new V2(7,0)); var b=Dummy(g,502,new V2(8,0)); var c=Dummy(g,503,new V2(9,0)); var d=Dummy(g,504,new V2(9.5f,0));
                Impact(g,TowerKind.Arc,a); Near(a.Health,76); Near(b.Health,84); Near(c.Health,90); Near(d.Health,100); });
            Check("Shatter applies primary once, secondary once, with unique target cap",()=> {
                var g=New(); g.Balance.PlayerDamage=0; var a=Dummy(g,501,new V2(7,0));
                for(int i=0;i<7;i++)Dummy(g,502+i,new V2(7.1f+i*.1f,0)); Impact(g,TowerKind.Shatter,a);
                Near(a.Health,36); int hit=0; foreach(Enemy e in g.Enemies)if(e.Health<100)hit++; Assert(hit==5,"target cap"); Near(g.Telemetry.TowerDamage[(int)TowerKind.Shatter],192); });
            Check("Rupture hits its fixed location after the original target leaves",()=> {
                var g=New(); g.Balance.PlayerDamage=0; var a=Dummy(g,501,new V2(7,0)); var b=Dummy(g,502,new V2(7.3f,0));
                g.Projectiles.Add(new Projectile { Id=9001,TargetId=a.Id,SourceId=1,FromTower=true,FixedImpact=true,Kind=TowerKind.Rupture,Position=new V2(7,0),ImpactPosition=new V2(7,0),Stats=g.Stats(TowerKind.Rupture,1),Speed=20 });
                a.Position=new V2(13,0); Advance(g,.02f); Near(a.Health,100); Near(b.Health,68); });
            Check("Binding launches only distinct targets and slows them",()=> {
                var g=New(); g.Balance.PlayerDamage=0; var t=Build(g,4,TowerKind.Binding,false);
                for(int i=0;i<5;i++)Dummy(g,501+i,new V2(i*.1f,-6)); Advance(g,.3f); int slowed=0;
                foreach(Enemy e in g.Enemies)if(e.SlowFraction>0)slowed++; Assert(slowed==3,"binding multiplicity"); });
            Check("Slow picks strongest, applies siege resistance and falls back when stronger expires",()=> {
                var g=New(); var e=Dummy(g,501,new V2(10,0),EnemyKind.Siege); e.Slows.Add(new SlowEffect { Source=1,Fraction=.55f,Remaining=.1f });
                e.Slows.Add(new SlowEffect { Source=2,Fraction=.35f,Remaining=1 }); Near(e.SlowFraction,.22); Advance(g,.2f); Near(e.SlowFraction,.14); Advance(g,1); Near(e.SlowFraction,0); });
            Check("Card tags apply once to fusion definition, additive damage and capped offers",()=> {
                var g=New(140,true); g.Cards[(int)CardKind.Ballistic]=2; Near(g.Stats(TowerKind.Seal,1).Damage,156); Near(g.Stats(TowerKind.Binding,1).Damage,3);
                Advance(g,30.1f); Assert(g.AwaitingCard && g.State==RunState.Paused && g.CardOffers.Count==3,"card menu");
                Assert(!g.CardOffers.Contains(CardKind.Ballistic),"maxed card offered"); double time=g.Elapsed; Advance(g,20); Near(g.Elapsed,time);
                string reason; Assert(!g.TryBuild(4,out reason),"buy during card choice"); CardKind choice=g.CardOffers[0]; Assert(g.ChooseCard(choice),"choice rejected"); Assert(!g.ChooseCard(choice),"choice duplicated"); });
            Check("All three card pauses and both core grants happen once",()=> {
                var g=New(140,true); int choices=0;
                for(int i=0;i<2000 && g.State!=RunState.Won;i++) { Advance(g,1); if(g.AwaitingCard) { choices++; g.ChooseCard(g.CardOffers[0]); } }
                Assert(choices==3 && g.Cores==2 && g.Telemetry.CoresGranted==2,"duplicate/missing progression"); Near(g.Elapsed,180); });
            Check("Engagement cap2 and siege ignores hunter",()=> {
                var g=New(); g.Balance.PlayerDamage=0; for(int i=0;i<5;i++)Dummy(g,501+i,g.PlayerPosition+new V2(1,0));
                var siege=Dummy(g,600,g.PlayerPosition+new V2(1,0),EnemyKind.Siege); Advance(g,.02f);
                Assert(g.Enemies.FindAll(e=>e.Engaged).Count==2 && !siege.Engaged,"unlimited body blocking");
                Enemy e=g.Enemies[0]; e.Position=new V2(10,0); Advance(g,.02f); Assert(!e.Engaged && e.ReengageDelay>.9,"release hysteresis"); });
            Check("All147 scheduled enemies spawn, rewards197 and no spawn at170+",()=> {
                var g=New(140,false,true); g.Balance.PlayerDamage=0;
                // Independent high-health observer records schedule without ending early.
                g=new DefenseSimulation(new GameBalance { StartingCredits=140,PlayerDamage=0,PlayerHealth=1000000,CoreHealth=1000000,OfferCards=false },7); g.Start(); Advance(g,169.9f);
                Assert(g.Telemetry.Spawned==147,"spawn total "+g.Telemetry.Spawned); int[] expected={94,34,13,6}; for(int i=0;i<4;i++)Assert(g.Telemetry.SpawnedKinds[i]==expected[i],"kind count");
                Advance(g,10.1f); Assert(g.Telemetry.Spawned==147 && g.State==RunState.Won,"late spawn/end"); Assert(94+34+13*3+6*5==197,"reward budget"); });
            Check("Frame rates30/60/144 preserve duration and guaranteed340 income",()=> {
                foreach(int fps in new[]{30,60,144}) { var g=New(140); for(int i=0;i<181*fps;i++) { g.Tick(1f/fps,new V2()); g.Events.Clear(); }
                    Near(g.Elapsed,180); Assert(g.State==RunState.Won && g.Credits==340 && g.Cores==2,"frame rate drift"); } });
            Check("Invalid delta/movement cannot corrupt state; movement respects bounds",()=> {
                var g=New(); Advance(g,float.NaN); Advance(g,float.PositiveInfinity); Near(g.Elapsed,0); g.Tick(1,new V2(float.NaN,1)); Assert(!float.IsNaN(g.PlayerPosition.X),"NaN position");
                g.Tick(10,new V2(1,1)); Assert(g.PlayerPosition.X<=18 && g.PlayerPosition.Y<=10,"escaped map"); });
            Check("Defeat wins over survival when core dies at the final tick",()=> {
                var g=new DefenseSimulation(new GameBalance { Duration=.01f,CoreHealth=1,PlayerDamage=0,SpawnEnemies=false,OfferCards=false },1); g.Start();
                var e=Dummy(g,501,new V2(1.12f,0),EnemyKind.Siege); e.Waypoint=4; g.Tick(.02f,new V2()); Assert(g.State==RunState.Lost,"defeat precedence"); });
            CombatRuns(); report.Add("PASS: "+checks+" simulation checks. Scripted combat is not a human playtest."); return string.Join(Environment.NewLine,report.ToArray());
        }
        private static void CombatRuns()
        {
            var idle=new DefenseSimulation(new GameBalance { OfferCards=false },7); idle.Start(); Advance(idle,180); Assert(idle.State==RunState.Lost,"idle should fail");
            report.Add("COMBAT idle="+idle.State+" at="+idle.Elapsed.ToString("F1"));
            // Legal commands and normal stats; move between planned purchases, then defend the weakest approach.
            for(int strategy=0;strategy<3;strategy++) {
                var g=new DefenseSimulation(new GameBalance(),7); g.Start(); int next=0; bool fused=false;
                int[] slots={2,7,3,4,1}; TowerKind[] kinds={TowerKind.Autocannon,TowerKind.Binding,TowerKind.Rupture,TowerKind.Autocannon,TowerKind.Binding};
                for(int frame=0;frame<190*60 && (g.State==RunState.Running || g.AwaitingCard);frame++) {
                    if(g.AwaitingCard) { CardKind choice=g.CardOffers.Contains(CardKind.Credits) ? CardKind.Credits : g.CardOffers[0]; g.ChooseCard(choice); }
                    V2 target; string reason;
                    if(strategy>0 && !fused && g.Cores>0 && g.TowerAt(2)!=null && g.TowerAt(7)!=null && g.TowerAt(2).Level==2 && g.TowerAt(7).Level==2) {
                        target=DefenseSimulation.Sockets[2]; Tower a=g.TowerAt(2),b=g.TowerAt(7);
                        if(g.TryFuse(0,a.Id,b.Id,2,null,out reason))fused=true;
                    } else if(next<slots.Length) {
                        target=DefenseSimulation.Sockets[slots[next]]; Tower t=g.TowerAt(slots[next]);
                        if(t==null)g.TryBuild(slots[next],kinds[next],null,out reason);
                        else if(t.Level==1)g.TryUpgrade(t.Id,null,out reason); else next++;
                    } else {
                        target=new V2(3,-2.5f);
                        if(strategy==2 && g.Towers.Count>0) { Tower t=g.TowerAt(2); if(t!=null && t.Level==1) { target=t.Position; g.TryUpgrade(t.Id,null,out reason); } }
                    }
                    V2 delta=target-g.PlayerPosition; g.Tick(1f/60,delta.Length>.2f ? delta.Normalized : new V2()); g.Events.Clear();
                }
                report.Add("COMBAT strategy="+strategy+" result="+g.State+" time="+g.Elapsed.ToString("F1")+" core="+g.CoreHealth+" hunter="+g.PlayerHealth+" kills="+g.Kills+" fusion="+g.Telemetry.FirstFusion.ToString("F1")+" spend="+g.Telemetry.CreditsSpent);
            }
            checks++; report.Add("PASS scripted idle-loss check and three strategy telemetry runs (no assumed win rate)");
        }
    }
}
