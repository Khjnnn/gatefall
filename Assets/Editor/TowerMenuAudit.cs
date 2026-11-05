using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Gatefall.Core;
using UnityEditor;
using UnityEngine;

namespace Gatefall.Editor
{
    // Diagnostic audit: a 240px minimum is not every tower's actual menu width.
    public static class TowerMenuAudit
    {
        [Serializable] private sealed class Row {
            public string context, text; public int fontSize; public float width, height, availableWidth, availableHeight;
            public bool overflowsMinimum;
        }
        [Serializable] private sealed class Report { public string version; public Row[] rows; }
        public static void Run()
        {
            var rows=new List<Row>();
            MethodInfo stat=typeof(MobileHud).GetMethod("StatLine",BindingFlags.NonPublic|BindingFlags.Static);
            if(stat==null)throw new Exception("Current HUD stat formatter missing");
            foreach(TowerDefinition tower in TowerCatalog.All)for(int i=0;i<tower.Levels.Length;i++) {
                TowerStats now=tower.Levels[i], next=i+1<tower.Levels.Length?tower.Levels[i+1]:null;
                string text=(string)stat.Invoke(null,new object[]{tower.Kind,now,next})+" · 사거리 "+now.Range.ToString("0.#");
                Measure(rows,tower.Kind+" Lv"+(i+1)+" base stats",text,14,false,22);
            }
            foreach(int enemies in new[]{0,27,9999})foreach(bool resonance in new[]{false,true}) {
                string common="현재 범위 안 적 "+enemies;
                Measure(rows,"Common field count="+enemies+" resonance="+resonance,common+(resonance?" · 인접 공명 +5%":""),13,true,18);
                Measure(rows,"Marker field count="+enemies+" resonance="+resonance,common+" · 화력 겹친 적 우선"+(resonance?" · 인접 공명 +5%":""),13,true,18);
                Measure(rows,"Observer field count="+enemies+" resonance="+resonance,"표식 우선 3초 · 범위 적 "+enemies+(resonance?" · 공명 +5%":""),13,true,18);
            }
            File.WriteAllText("TestResults/tower-menu-font-audit-v0206.json",JsonUtility.ToJson(new Report{version=Application.version,rows=rows.ToArray()},true));
            Debug.Log("GATEFALL TOWER MENU AUDIT COMPLETE: "+rows.Count+" labels; diagnostic minimum width, not actual Android screenshots.");
        }
        public static void ReviewReachable()
        { ReviewReachable(false); }
        public static void ReviewReachableMax()
        { ReviewReachable(true); }
        private static void ReviewReachable(bool maximumResearchAndDamageCards)
        {
            var rows=new List<Row>();
            MethodInfo stat=typeof(MobileHud).GetMethod("StatLine",BindingFlags.NonPublic|BindingFlags.Static);
            TowerKind[] kinds={TowerKind.Binding,TowerKind.Arc,TowerKind.Shatter,TowerKind.Gravity};
            int[] stages={1,2,3,3};
            for(int i=0;i<kinds.Length;i++) {
                var game=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,new CampaignBattle{Stage=CampaignCatalog.Stages[stages[i]],Research=maximumResearchAndDamageCards?new[]{3,3,3,3}:new int[4]});
                if(maximumResearchAndDamageCards){game.Cards[(int)CardKind.Ballistic]=2;game.Cards[(int)CardKind.Blast]=1;}
                if(!game.Allows(kinds[i]))throw new Exception("Tower unavailable in example operation");
                TowerDefinition definition=TowerCatalog.Get(kinds[i]);int options=0;
                foreach(FusionRecipe recipe in TowerCatalog.Recipes) {
                    if(recipe.First!=kinds[i]&&recipe.Second!=kinds[i])continue;
                    TowerKind other=recipe.First==kinds[i]?recipe.Second:recipe.First;
                    if(game.Allows(recipe.Result)&&game.Allows(other))options++;
                }
                int tiles=2+options;float contentWidth=Mathf.Max(330,28+tiles*140+(tiles-1)*8)-90;
                var tower=new Tower{Kind=kinds[i],Level=1,Socket=0,Position=game.BoardSockets[0]};
                TowerStats now=game.Stats(tower),next=game.Stats(tower,2);
                string text=(string)stat.Invoke(null,new object[]{kinds[i],now,next})+" · 사거리 "+now.Range.ToString("0.#");
                string context="1-"+(stages[i]+1)+" "+kinds[i]+" Lv1 /"+options+" fusion tiles";
                Measure(rows,context+" original",text,14,false,22,contentWidth);
                Measure(rows,context+" candidate minimum352",text,14,false,22,Mathf.Max(352,28+tiles*140+(tiles-1)*8)-90);
                if(maximumResearchAndDamageCards)Measure(rows,context+" candidate minimum360",text,14,false,22,Mathf.Max(360,28+tiles*140+(tiles-1)*8)-90);
            }
            File.WriteAllText("TestResults/tower-menu-reachable"+(maximumResearchAndDamageCards?"-max":"")+"-v0206.json",JsonUtility.ToJson(new Report{version=Application.version,rows=rows.ToArray()},true));
            Debug.Log("GATEFALL TOWER MENU REACHABLE AUDIT COMPLETE: four available narrow menus; Marker resonance branch unreachable because ActiveForResonance excludes Marker.");
        }
        private static void Measure(List<Row> rows,string context,string text,int size,bool bold,float availableHeight,float availableWidth=240)
        {
            var style=new GUIStyle{font=Resources.Load<Font>(bold?"Fonts/NanumGothic-Bold":"Fonts/NanumGothic-Regular"),fontSize=size,wordWrap=true,padding=new RectOffset()};
            style.font.RequestCharactersInTexture(text,size,FontStyle.Normal);
            var content=new GUIContent(text);float width=style.CalcSize(content).x,height=style.CalcHeight(content,availableWidth);
            rows.Add(new Row{context=context,text=text,fontSize=size,width=width,height=height,availableWidth=availableWidth,availableHeight=availableHeight,overflowsMinimum=width>availableWidth||height>availableHeight});
        }
    }
}
