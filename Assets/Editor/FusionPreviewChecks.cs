using System;
using System.Collections.Generic;
using Gatefall.Core;

namespace Gatefall.Tests
{
    public static class FusionPreviewChecks
    {
        static void Require(bool pass,string message) { if(!pass)throw new Exception(message); }
        public static string Run()
        {
            var game=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,new CampaignBattle{Stage=CampaignCatalog.Stages[0]});
            // Isolate one straight active road without changing shared catalog data.
            foreach(int lane in game.OpenLanes)game.Roads[lane]=new[]{new V2(0,0),new V2(12,0)};
            var line=new TowerStats{Range=7,LineHalfWidth=.65f,Inheritance=SealInheritance.LineBurst};
            V2 impact, origin=new V2(-.1f,0), aim=FusionPreview.Aim(game,line,origin,out impact);
            float high=FusionPreview.RoadOverlap(game,line,origin,aim);
            Require(FusionPreview.Fit(high)==FusionPreview.HighFit && aim.X>.99f,"line beside a straight road must report high overlap");
            origin=new V2(0,10);aim=FusionPreview.Aim(game,line,origin,out impact);
            Require(FusionPreview.Fit(FusionPreview.RoadOverlap(game,line,origin,aim))==FusionPreview.LowFit,"line away from roads must report low overlap");
            // Seen on the 0.33.1 device run: a strip crossing the road at a right angle read "보통" under area sampling.
            Require(FusionPreview.Fit(FusionPreview.RoadOverlap(game,line,new V2(6,3),new V2(0,-1)))==FusionPreview.LowFit,"line crossing a road at a right angle must report low overlap");
            Require(FusionPreview.Fit(.60f)==FusionPreview.HighFit && FusionPreview.Fit(.25f)==FusionPreview.MediumFit && FusionPreview.Fit(.249f)==FusionPreview.LowFit,"documented overlap threshold boundaries");
            Require(FusionPreview.RoadOverlap(game,new TowerStats{Range=7,ConeCos=.7071068f},new V2(),new V2(1,0))<high,"fan area sampling must differ from the line strip");
            Require(FusionPreview.Situation(line)=="일렬 선로에 강함","line situation follows stats");
            line.LineHalfWidth=0;line.ConeCos=.5f;
            Require(FusionPreview.Situation(line)=="퍼진 적에 강함","retuned cone replaces line text");
            line.ConeCos=0;line.MarkBonus=5;line.Inheritance=SealInheritance.MarkedBlast;
            Require(FusionPreview.Situation(line)=="뒤쪽 표식 적 우선","marked situation follows stats");
            line.Inheritance=SealInheritance.None;
            Require(FusionPreview.Situation(line)=="","legacy result keeps existing role text");
            var lead=new Enemy{Id=2,Position=new V2(1,0),Health=100};
            var marked=new Enemy{Id=3,Position=new V2(3,1),Health=100,MarkRemaining=3};
            var unavailable=new Enemy{Id=1,Position=new V2(.5f,0),Health=10,IncomingDamage=10};
            game.Enemies.Add(lead);game.Enemies.Add(marked);game.Enemies.Add(unavailable);
            var s=new TowerStats{Range=7,LineHalfWidth=.65f};
            aim=FusionPreview.Aim(game,s,new V2(),out impact);
            Require(impact.X==1 && impact.Y==0,"line mirrors simulation lead and incoming damage filtering");
            s.LineHalfWidth=0;s.ConeCos=.7f;
            aim=FusionPreview.Aim(game,s,new V2(),out impact);
            Require(impact.X==1,"cone aims at leading enemy");
            s.ConeCos=0;s.MarkBonus=5;
            aim=FusionPreview.Aim(game,s,new V2(),out impact);
            Require(impact.X==3 && impact.Y==1,"marked preview prefers rear marked enemy");
            s.Inheritance=SealInheritance.MarkedBlast;s.Radius=2;s.MarkedRadiusMultiplier=2;
            float radius;FusionPreview.Aim(game,s,new V2(),out impact,out radius);
            Require(radius==4 && impact.X==3,"marked Observer Seal preview doubles splash at its target");
            marked.MarkRemaining=0;FusionPreview.Aim(game,s,new V2(),out impact,out radius);
            Require(radius==2 && impact.X==1,"unmarked Observer Seal preview keeps base splash");
            marked.MarkRemaining=3;marked.IncomingDamage=marked.Health;
            FusionPreview.Aim(game,s,new V2(),out impact,out radius);
            Require(radius==2 && impact.X==1,"unavailable marked enemy cannot enlarge preview");
            marked.IncomingDamage=0;marked.Position=new V2(30,0);
            FusionPreview.Aim(game,s,new V2(),out impact,out radius);
            Require(radius==2 && impact.X==1,"out-of-range marked enemy cannot enlarge preview");
            marked.Position=new V2(3,1);s.Inheritance=SealInheritance.None;
            FusionPreview.Aim(game,s,new V2(),out impact,out radius);
            Require(radius==2,"non-inherited marked targeting keeps base splash");
            s.Inheritance=SealInheritance.MarkedBlast;game.Enemies.Clear();
            FusionPreview.Aim(game,s,new V2(),out impact,out radius);
            Require(radius==2,"empty road preview keeps base splash");
            game.Enemies.Add(lead);game.Enemies.Add(marked);game.Enemies.Add(unavailable);
            game.Towers.Add(new Tower{Kind=TowerKind.Marker,Socket=1,Position=new V2(2,0)});
            Require(FusionPreview.HasMarker(game,s,new V2(),0),"nearby marker detected");
            Require(!FusionPreview.HasMarker(game,s,new V2(),1) && !FusionPreview.HasMarker(game,s,new V2(),0,1),"consumed marker is excluded");
            Require(!FusionPreview.HasMarker(game,s,new V2(20,20),0),"distant marker excluded");
            var cache=new FusionPreview();FusionRecipe recipe=TowerCatalog.Recipes[0];
            TowerStats first=cache.Stats(game,recipe);
            Require(object.ReferenceEquals(first,cache.Stats(game,recipe)),"stable preview stats must not allocate every frame");
            game.Cards[(int)CardKind.Range]++;
            TowerStats grown=cache.Stats(game,recipe);
            Require(grown.Range>first.Range && !object.ReferenceEquals(first,grown),"range card invalidates preview cache");
            var retuned=new FusionRecipe{Result=TowerKind.Seal,Inheritance=SealInheritance.LineBurst};
            cache.Stats(game,retuned);retuned.Inheritance=SealInheritance.None;
            Require(cache.Stats(game,retuned).Inheritance==SealInheritance.None,"legacy retune invalidates cached inherited shape");
            foreach(FusionRecipe result in TowerCatalog.Recipes) {
                TowerStats actual=game.Stats(result);
                Require(cache.Stats(game,result).Range==actual.Range && cache.Stats(game,result).LineHalfWidth==actual.LineHalfWidth && cache.Stats(game,result).ConeCos==actual.ConeCos,"recipe preview uses produced stats");
                Require(FusionPreview.Situation(actual).Length*12<=150,"situational role must fit 150 units at caption size");
            }
            TowerStats stable=cache.Stats(game,recipe);
            aim=FusionPreview.Aim(game,stable,new V2(),out impact);cache.Overlap(game,stable,new V2(),aim);
            var movingShape=new TowerStats{Range=7,LineHalfWidth=.65f};
            cache.Overlap(game,movingShape,new V2(),new V2(1,0));
            long allocated=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<100;i++) {
                stable=cache.Stats(game,recipe);aim=FusionPreview.Aim(game,stable,new V2(),out impact);
                cache.Overlap(game,movingShape,new V2(),new V2((float)Math.Cos(i*.02),(float)Math.Sin(i*.02)));
            }
            Require(GC.GetAllocatedBytesForCurrentThread()==allocated,"stable shape preview must not allocate per frame");
            return "PASS: Fusion shapes, high/low road overlap, lead/mark direction, marker support, legacy fallback and zero-allocation warm preview (C# 7.3).";
        }
    }
}
