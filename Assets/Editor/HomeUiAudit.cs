using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Gatefall.Core;
using UnityEditor.Build;
using UnityEngine;

namespace Gatefall.Editor
{
    public static class HomeUiAudit
    {
        const BindingFlags Instance=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
        [Serializable] private sealed class Report {
            public string version; public int layouts,textBounds,touchHits,navigationChecks,glyphs,pressedSurfaces;
            public string scope="Real HUD draw list, real touch router, synthetic profile navigation and Unity font/cache/import checks. Native Android review remains separate.";
        }
        static void Set(object o,string name,object value) {o.GetType().GetProperty(name,Instance).SetValue(o,value);}
        static void Require(bool value,string message) {if(!value)throw new BuildFailedException("Home UI: "+message);}
        public static void Validate()
        {
            string folder="TestResults/chapter2-design-20261107";Directory.CreateDirectory(folder+"/skins");
            var report=new Report{version=Application.version};
            var art=new GatefallArt();
            Require(art.Texture("home-city")!=null,"background resource missing");
            Require(art.Texture("home-city").width<=2048,"background import budget");
            var go=new GameObject("Home UI audit");go.SetActive(false);
            var controller=go.AddComponent<GatefallGame>();
            typeof(GatefallGame).GetField("art",Instance).SetValue(controller,art);
            var hud=new MobileHud(controller,art);
            var cache=(IDictionary)typeof(MobileHud).GetField("themeTextures",Instance).GetValue(hud);
            var theme=typeof(MobileHud).GetMethod("ThemeTexture",Instance);
            try {
                Require(controller.ScreenPage==CampaignScreen.Home,"default boot must be Home");
                foreach(string name in new[]{"ward","home","map","loadout","hunter","gear","back","play","lock"}) {
                    string key="ui-glyph-"+name;
                    var texture=(Texture2D)theme.Invoke(hud,new object[]{key});
                    int ink=texture.GetPixels32().Count(p=>p.a>127);
                    Require(ink>=50 && ink<=1500 && texture.GetPixel(0,0).a==0 && ReferenceEquals(texture,theme.Invoke(hud,new object[]{key})),"glyph/cache "+key+" ink="+ink);
                    File.WriteAllBytes(folder+"/skins/"+key+".png",texture.EncodeToPNG());report.glyphs++;
                }
                foreach(string key in new[]{"ui-button","ui-button-primary","ui-button-upgrade","ui-button-fusion"}) {
                    var normal=(Texture2D)theme.Invoke(hud,new object[]{key});
                    var pressed=(Texture2D)theme.Invoke(hud,new object[]{key+"-pressed"});
                    Require(!normal.GetPixels32().SequenceEqual(pressed.GetPixels32()) && pressed.GetPixel(0,0).a==0 && pressed.GetPixel(64,64).a>.99f && ReferenceEquals(pressed,theme.Invoke(hud,new object[]{key+"-pressed"})),"pressed surface/cache "+key);
                    File.WriteAllBytes(folder+"/skins/"+key+"-pressed.png",pressed.EncodeToPNG());report.pressedSurfaces++;
                }
                foreach(bool veteran in new[]{false,true}) {
                    var profile=new CampaignProfile{MagicCores=veteran?284:0};
                    if(veteran) {
                        for(int i=0;i<CampaignCatalog.Stages.Length;i++){profile.BestStars[i]=3;profile.FirstCleared[i]=true;profile.CrisisStars[i]=i<14?1:2;}
                        profile.BestStars[19]=2;profile.CrisisStars[19]=1;
                    }
                    byte[] before=CampaignStore.Encode(profile);int writes=0;
                    Set(controller,"Campaign",new CampaignService(profile,p=>writes++));
                    Set(controller,"Simulation",new DefenseSimulation(new GameBalance(),7));
                    foreach(bool crisis in veteran?new[]{false,true}:new[]{false})
                    foreach(Vector2 size in new[]{new Vector2(960,600),new Vector2(1067,600),new Vector2(1550,600),new Vector2(960,720)}) {
                        Set(controller,"CrisisSelection",crisis);
                        Set(controller,"TouchInput",new MobileInput(new MobileLayout(size.x,size.y,new Box2(0,0,size.x,size.y))));
                        Set(controller,"ScreenPage",CampaignScreen.Home);
                        // Every current operation name is measured in the actual Home item rectangles.
                        for(int stage=0;stage<(veteran?CampaignCatalog.Stages.Length:1);stage++) {
                            Set(controller,"SelectedStage",stage);hud.Refresh();
                            ValidateTexts(hud,ref report.textBounds);
                            var displayed=HomeTexts(hud);
                            int[] indices=CampaignCatalog.Stages.Where(s=>s.Chapter==CampaignCatalog.Stages[stage].Chapter).Select(s=>s.Index).ToArray();
                            int stars=indices.Sum(i=>(crisis?profile.CrisisStars:profile.BestStars)[i]);
                            string expected=stars+" / "+(indices.Length*3);
                            Require(displayed.Contains(expected) && displayed.Any(t=>t.StartsWith("챕터 "+controller.CurrentChapter)),"chapter-specific Home record "+expected);
                            var regions=controller.TouchInput.Regions;
                            // Top controls, pass strip, selected operation, then enabled tab/right entries.
                            var expectedActions=new List<int>{602,684,685,604,880,600};
                            foreach(var entry in HomeMenu.Tabs.Concat(HomeMenu.Right)) {
                                bool unlocked=entry.UnlockStage<0 || profile.FirstCleared[entry.UnlockStage];
                                if(unlocked)expectedActions.Add(entry.Action);
                                else Require(displayed.Contains(entry.UnlockStage==9?"1-10 클리어":"1-3 클리어"),"locked navigation condition "+entry.Action);
                            }
                            Require(regions.Select(r=>(int)r.Action).OrderBy(x=>x).SequenceEqual(expectedActions.OrderBy(x=>x)),"enabled/locked tab, right and pass navigation actions");
                            Require(regions.Any(r=>(int)r.Action==680)==veteran && regions.Any(r=>(int)r.Action==652)==veteran,"shop/card gates");
                            foreach(TouchRegion region in regions) {
                                Box2 b=region.Box;
                                Require(b.X>=0 && b.Y>=0 && b.X+b.Width<=controller.Layout.Width && b.Y+b.Height<=controller.Layout.Height && b.Width>=48 && b.Height>=48,"touch box outside safe area");
                                foreach(TouchRegion other in regions)if((int)other.Action!=(int)region.Action)
                                    Require(b.X>=other.Box.X+other.Box.Width || other.Box.X>=b.X+b.Width || b.Y>=other.Box.Y+other.Box.Height || other.Box.Y>=b.Y+b.Height,"overlapping Home buttons");
                                controller.TouchInput.Pointer(99,PointerPhase.Begin,b.Center);controller.TouchInput.Pointer(99,PointerPhase.End,b.Center);
                                Require(controller.TouchInput.Actions.Count==1 && controller.TouchInput.Actions[0]==region.Action,"actual touch router action");
                                controller.TouchInput.Reset();report.touchHits++;
                            }
                        }
                        report.layouts++;
                    }
                    var action=typeof(GatefallGame).GetMethod("CampaignAction",Instance);
                    foreach(int id in veteran?new[]{600,602,604,652}:new[]{600,602,604}) {
                        action.Invoke(controller,new object[]{id});
                        CampaignScreen expected=id==600?CampaignScreen.City:id==602?CampaignScreen.Growth:id==604?CampaignScreen.Settings:CampaignScreen.Loadout;
                        Require(controller.ScreenPage==expected,"navigation action "+id);
                        action.Invoke(controller,new object[]{603});Require(controller.ScreenPage==CampaignScreen.Home,"return Home from "+expected);report.navigationChecks++;
                    }
                    action.Invoke(controller,new object[]{660});
                    action.Invoke(controller,new object[]{662});
                    Require(controller.CurrentChapter==(veteran?3:1),"rail chapter lock and action");
                    if(veteran) {
                        action.Invoke(controller,new object[]{661});
                        int pages=(int)typeof(GatefallGame).GetProperty("MapPageCount",Instance).GetValue(controller);
                        Require(controller.CurrentChapter==2 && pages==4,"market pages");
                        for(int i=0;i<3;i++)action.Invoke(controller,new object[]{656});
                        Require(controller.MapPage==3 && CampaignCatalog.StageId(controller.SelectedStage)=="2-22","market last page");
                        report.navigationChecks+=2;
                    }
                    Require(before.SequenceEqual(CampaignStore.Encode(profile)) && controller.Profile.PendingRun==profile.PendingRun && controller.Profile.MagicCores==profile.MagicCores,"navigation changed campaign progress");
                    // Measure the longest new resource and modal labels with the real imported font.
                    var hearts=controller.Profile.Copy();hearts.Hearts=0;hearts.ExtraHearts=999;
                    hearts.CrystalsFree=int.MaxValue;hearts.CrystalsPaid=int.MaxValue;
                    Set(controller,"Campaign",new CampaignService(hearts,p=>{}));
                    Set(controller,"ScreenPage",CampaignScreen.Home);hud.Refresh();ValidateTexts(hud,ref report.textBounds);
                    Set(controller,"HeartShortage",true);
                    foreach(bool ready in new[]{false,true}) {
                        controller.RewardedAds=new FakeRewardedAdService{IsReady=ready};
                        hud.Refresh();ValidateTexts(hud,ref report.textBounds);
                        Require(controller.TouchInput.Regions.All(t=>(int)t.Action>=696 && (int)t.Action<=698),"modal leaked Home actions");
                    }
                    Set(controller,"HeartShortage",false);
                }
                ValidateShop(controller,hud,ref report.textBounds,ref report.touchHits);
                ValidateDaily(controller,hud,ref report.textBounds,ref report.touchHits);
                File.WriteAllText(folder+"/unity-home-audit.json",JsonUtility.ToJson(report,true));
                Debug.Log("GATEFALL HOME PASSED: "+report.layouts+" layouts, "+report.textBounds+" real text bounds, "+report.touchHits+" router hits, "+report.navigationChecks+" navigation paths,9 glyphs and4 pressed surfaces.");
            }
            finally {
                foreach(Texture2D texture in cache.Values)UnityEngine.Object.DestroyImmediate(texture);cache.Clear();
                UnityEngine.Object.DestroyImmediate(go);art.Dispose();
            }
        }
        static string[] HomeTexts(MobileHud hud)
        {
            var list=new List<string>();
            foreach(object item in (IEnumerable)typeof(MobileHud).GetField("items",Instance).GetValue(hud)) {
                Type type=item.GetType();if(type.GetField("Kind").GetValue(item).ToString()=="Text")list.Add((string)type.GetField("Text").GetValue(item));
            }
            return list.ToArray();
        }
        static void ValidateShop(GatefallGame controller,MobileHud hud,ref int texts,ref int touches)
        {
            var action=typeof(GatefallGame).GetMethod("CampaignAction",Instance);
            // Same twelve physical/safe-area profiles as Tests/HudOcclusionChecks.cs.
            foreach(int[] screen in new[]{new[]{1280,720,0,652},new[]{1280,720,0,639},new[]{1280,720,0,720},new[]{1280,720,45,720},
                new[]{1280,576,0,495},new[]{1280,800,0,710},new[]{1280,800,0,800},new[]{1920,1080,0,959},
                new[]{1920,1080,68,1080},new[]{2400,1080,0,929},new[]{2400,1080,136,1080},new[]{2048,1536,0,1496}}) {
                var p=new CampaignProfile{MagicCores=500,CrystalsFree=int.MaxValue,CrystalsPaid=int.MaxValue};
                foreach(var stage in CampaignCatalog.Stages){p.FirstCleared[stage.Index]=true;p.BestStars[stage.Index]=3;}
                for(int i=0;i<SupplyCatalog.TowerCount;i++)p.Supply.Cards[i]=4;
                Set(controller,"Campaign",new CampaignService(p,x=>{}));Set(controller,"SupplyRandom",new Func<int,int>(max=>0));
                Set(controller,"TouchInput",new MobileInput(new MobileLayout(screen[0],screen[1],new Box2(screen[2],0,screen[0]-screen[2],screen[3]))));
                action.Invoke(controller,new object[]{680});
                foreach(int tab in new[]{800,801,802,803,804}) {
                    action.Invoke(controller,new object[]{tab});hud.Refresh();ValidateTexts(hud,ref texts);ValidateShopTouches(controller,ref touches);
                }
                action.Invoke(controller,new object[]{800});action.Invoke(controller,new object[]{810});hud.Refresh();ValidateTexts(hud,ref texts);
                foreach(bool ready in new[]{false,true}) {
                    controller.RewardedAds=new FakeRewardedAdService{IsReady=ready};hud.Refresh();ValidateTexts(hud,ref texts);
                }
                action.Invoke(controller,new object[]{801});action.Invoke(controller,new object[]{805});hud.Refresh();ValidateTexts(hud,ref texts);
                Require(controller.TouchInput.Regions.All(t=>(int)t.Action>=819 && (int)t.Action<=821),"result leaked underlying controls");
                action.Invoke(controller,new object[]{819});action.Invoke(controller,new object[]{807});
                foreach(int kind in new[]{822,823}) {
                    action.Invoke(controller,new object[]{kind});
                    for(int page=0;page<6;page++) {
                        hud.Refresh();ValidateTexts(hud,ref texts);ValidateShopTouches(controller,ref touches);
                        Require(controller.TouchInput.Regions.All(t=>(int)t.Action>=819 && (int)t.Action<=823),"odds leaked underlying controls");
                        action.Invoke(controller,new object[]{821});
                    }
                }
                action.Invoke(controller,new object[]{819});action.Invoke(controller,new object[]{652});action.Invoke(controller,new object[]{835});
                for(int page=0;page<3;page++) {hud.Refresh();ValidateTexts(hud,ref texts);ValidateShopTouches(controller,ref touches);action.Invoke(controller,new object[]{838});}
                // Ten-digit cumulative card counts and currency sums use the same real font audit.
                p=controller.Profile.Copy();p.MagicCores=100000000;
                for(int i=0;i<SupplyCatalog.TowerCount;i++)p.Supply.Cards[i]=int.MaxValue;
                Set(controller,"Campaign",new CampaignService(p,x=>{}));hud.Refresh();ValidateTexts(hud,ref texts);
                // Early unlocked cards retain dimmed towers and the actual operation conditions.
                p=new CampaignProfile{MagicCores=0};for(int i=0;i<3;i++){p.FirstCleared[i]=true;p.BestStars[i]=3;}
                Set(controller,"Campaign",new CampaignService(p,x=>{}));
                action.Invoke(controller,new object[]{652});
                for(int page=0;page<3;page++){hud.Refresh();ValidateTexts(hud,ref texts);action.Invoke(controller,new object[]{838});}
                action.Invoke(controller,new object[]{680});action.Invoke(controller,new object[]{801});hud.Refresh();ValidateTexts(hud,ref texts);
            }
        }
        static void ValidateDaily(GatefallGame controller,MobileHud hud,ref int texts,ref int touches)
        {
            var action=typeof(GatefallGame).GetMethod("CampaignAction",Instance);
            // The victory panel is drawn by the battle HUD, which reads the game camera.
            if(controller.GameCamera==null) {
                var camera=controller.gameObject.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=14;camera.transform.position=new Vector3(0,0,-10);
                typeof(GatefallGame).GetField("gameCamera",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,camera);
            }
            long now=DateTimeOffset.UtcNow.ToUnixTimeSeconds();int day=(int)(DateTime.Today-new DateTime(1970,1,1)).TotalDays;
            foreach(int[] screen in new[]{new[]{1280,720,0,652},new[]{1280,720,0,639},new[]{1280,720,0,720},new[]{1280,720,45,720},
                new[]{1280,576,0,495},new[]{1280,800,0,710},new[]{1280,800,0,800},new[]{1920,1080,0,959},
                new[]{1920,1080,68,1080},new[]{2400,1080,0,929},new[]{2400,1080,136,1080},new[]{2048,1536,0,1496}}) {
                Set(controller,"TouchInput",new MobileInput(new MobileLayout(screen[0],screen[1],new Box2(screen[2],0,screen[0]-screen[2],screen[3]))));
                var p=new CampaignProfile{MagicCores=500,CrystalsFree=int.MaxValue,CrystalsPaid=int.MaxValue,LastSeenUtc=now,HeartAnchorUtc=now};
                for(int i=0;i<3;i++){p.FirstCleared[i]=true;p.BestStars[i]=3;}
                p.Daily=new DailyState{LocalDay=day,Season=DailyCatalog.SeasonNumber(day),PassPoints=1500,PointsToday=60};
                foreach(var row in DailyCatalog.Missions())p.Daily.Progress[(int)row.Mission]=row.Target;
                Require(p.Valid(),"daily audit fixture validity");
                Set(controller,"Campaign",new CampaignService(p,x=>{},()=>now,()=>day));
                Set(controller,"SelectedStage",0);Set(controller,"CrisisSelection",false);
                Set(controller,"ScreenPage",CampaignScreen.Home);hud.Refresh();ValidateTexts(hud,ref texts);
                foreach(int open in new[]{681,682,683,880}) {
                    action.Invoke(controller,new object[]{open});hud.Refresh();ValidateTexts(hud,ref texts);ValidateShopTouches(controller,ref touches);ValidateDailyWidths(hud);
                    if(open==681) {
                        Set(controller,"DailyAdPending",true);hud.Refresh();ValidateTexts(hud,ref texts);ValidateDailyWidths(hud);
                        Require(controller.TouchInput.Regions.Count==0,"pending roulette leaked controls");Set(controller,"DailyAdPending",false);
                        foreach(var slot in DailyCatalog.RouletteDisclosure()) {
                            Set(controller,"RouletteResult","획득 · "+DailyLabels.Reward(slot.Reward));hud.Refresh();ValidateTexts(hud,ref texts);ValidateDailyWidths(hud);
                        }
                        action.Invoke(controller,new object[]{882});
                        for(int page=0;page<10;page++){hud.Refresh();ValidateTexts(hud,ref texts);ValidateDailyWidths(hud);ValidateShopTouches(controller,ref touches);action.Invoke(controller,new object[]{886});}
                        action.Invoke(controller,new object[]{603});
                        foreach(bool ready in new[]{false,true}){controller.RewardedAds=new FakeRewardedAdService{IsReady=ready};hud.Refresh();ValidateTexts(hud,ref texts);}
                    }
                    if(open==682) {
                        for(int i=0;i<6;i++){action.Invoke(controller,new object[]{900+i});hud.Refresh();ValidateTexts(hud,ref texts);ValidateDailyWidths(hud);}
                        action.Invoke(controller,new object[]{900});hud.Refresh();ValidateTexts(hud,ref texts);
                    }
                    if(open==683) {
                        for(int position=0;position<7;position++)foreach(bool claimed in new[]{false,true}) {
                            var attendance=p.Copy();attendance.Daily.AttendancePosition=claimed?(position+1)%7:position;attendance.Daily.AttendanceDay=claimed?day:day-1;
                            Require(attendance.Valid(),"attendance audit fixture validity");Set(controller,"Campaign",new CampaignService(attendance,x=>{},()=>now,()=>day));
                            hud.Refresh();ValidateTexts(hud,ref texts);ValidateShopTouches(controller,ref touches);
                        }
                        Set(controller,"Campaign",new CampaignService(p,x=>{},()=>now,()=>day));
                    }
                    if(open==880) {
                        foreach(bool paid in new[]{false,true})foreach(bool claimed in new[]{false,true}) {
                            var pass=p.Copy();if(paid)pass.Daily.PurchaseLedger.Add("audit-pass",new PurchaseEntry("audit-pass",PurchaseKind.Pass,0,pass.Daily.Season));pass.Daily.CapRaised=claimed;
                            pass.Daily.FreeClaims=claimed?(1<<30)-1:0;pass.Daily.PaidClaims=paid && claimed?(1<<30)-1:0;
                            Require(pass.Valid(),"pass audit fixture validity");Set(controller,"Campaign",new CampaignService(pass,x=>{},()=>now,()=>day));
                            Set(controller,"DailyFirst",0);
                            for(int page=0;page<30;page++){hud.Refresh();ValidateTexts(hud,ref texts);ValidateDailyWidths(hud);ValidateShopTouches(controller,ref touches);action.Invoke(controller,new object[]{886});}
                        }
                        var locked=p.Copy();locked.Daily.PassPoints=0;Set(controller,"Campaign",new CampaignService(locked,x=>{},()=>now,()=>day));
                        Set(controller,"DailyFirst",0);hud.Refresh();ValidateTexts(hud,ref texts);ValidateDailyWidths(hud);
                    }
                    action.Invoke(controller,new object[]{603});
                }
                // Render the actual result with an eligible reward, doubled reward, and a defeat.
                Set(controller,"Campaign",new CampaignService(p,x=>{},()=>now,()=>day));controller.RewardedAds=new FakeRewardedAdService();
                controller.BeginPreparation();typeof(DefenseSimulation).GetProperty("State").SetValue(controller.Simulation,RunState.Won,null);
                typeof(GatefallGame).GetMethod("SaveCampaignOutcome",Instance).Invoke(controller,null);
                hud.Refresh();ValidateTexts(hud,ref texts);ValidateShopTouches(controller,ref touches);
                Require(controller.TouchInput.Regions.Any(t=>(int)t.Action==887),"eligible double missing");
                Set(controller,"DailyAdPending",true);hud.Refresh();ValidateTexts(hud,ref texts);
                Require(controller.TouchInput.Regions.Count==0,"pending double leaked controls");Set(controller,"DailyAdPending",false);
                action.Invoke(controller,new object[]{887});hud.Refresh();ValidateTexts(hud,ref texts);
                Require(!controller.TouchInput.Regions.Any(t=>(int)t.Action==887),"claimed double visible");
                controller.BeginPreparation();typeof(DefenseSimulation).GetProperty("State").SetValue(controller.Simulation,RunState.Lost,null);
                typeof(GatefallGame).GetMethod("SaveCampaignOutcome",Instance).Invoke(controller,null);hud.Refresh();ValidateTexts(hud,ref texts);
                Require(!controller.TouchInput.Regions.Any(t=>(int)t.Action==887),"defeat double visible");
            }
        }
        static void ValidateDailyWidths(MobileHud hud)
        {
            foreach(object item in (IEnumerable)typeof(MobileHud).GetField("items",Instance).GetValue(hud)) {
                Type type=item.GetType();if(type.GetField("Kind").GetValue(item).ToString()!="Text")continue;
                string text=(string)type.GetField("Text").GetValue(item);Rect r=(Rect)type.GetField("Rect").GetValue(item);
                int size=(int)type.GetField("Size").GetValue(item),weight=(int)type.GetField("Weight").GetValue(item);
                Font font=Resources.Load<Font>("Fonts/"+(weight==2?GatefallUiStyle.FontResourceHeavy:weight==1?GatefallUiStyle.FontResourceBold:GatefallUiStyle.FontResourceRegular));
                font.RequestCharactersInTexture(text,size,FontStyle.Normal);
                var style=new GUIStyle{font=font,fontSize=size,wordWrap=false,padding=new RectOffset()};
                foreach(string line in text.Split('\n'))Require(style.CalcSize(new GUIContent(line)).x<=r.width+.1f,"daily label width: "+line+" / "+r.width);
            }
        }
        static void ValidateShopTouches(GatefallGame controller,ref int count)
        {
            var regions=controller.TouchInput.Regions.ToArray();
            foreach(TouchRegion region in regions) {
                Box2 b=region.Box;Require(b.Width>=48 && b.Height>=48 && b.X>=0 && b.Y>=0 && b.X+b.Width<=controller.Layout.Width && b.Y+b.Height<=controller.Layout.Height,"shop touch safe area");
                foreach(TouchRegion other in regions)if(other.Action!=region.Action)
                    Require(b.X>=other.Box.X+other.Box.Width || other.Box.X>=b.X+b.Width || b.Y>=other.Box.Y+other.Box.Height || other.Box.Y>=b.Y+b.Height,"shop touch overlap");
                controller.TouchInput.Pointer(99,PointerPhase.Begin,b.Center);controller.TouchInput.Pointer(99,PointerPhase.End,b.Center);
                Require(controller.TouchInput.Actions.Count==1 && controller.TouchInput.Actions[0]==region.Action,"shop router hit");controller.TouchInput.Reset();count++;
            }
        }
        static void ValidateTexts(MobileHud hud,ref int count)
        {
            var items=(IEnumerable)typeof(MobileHud).GetField("items",Instance).GetValue(hud);
            foreach(object item in items) {
                Type type=item.GetType();if(type.GetField("Kind").GetValue(item).ToString()!="Text")continue;
                string text=(string)type.GetField("Text").GetValue(item);int size=(int)type.GetField("Size").GetValue(item),weight=(int)type.GetField("Weight").GetValue(item);
                Rect r=(Rect)type.GetField("Rect").GetValue(item);
                Font font=Resources.Load<Font>("Fonts/"+(weight==2?GatefallUiStyle.FontResourceHeavy:weight==1?GatefallUiStyle.FontResourceBold:GatefallUiStyle.FontResourceRegular));
                font.RequestCharactersInTexture(text,size,FontStyle.Normal);
                var style=new GUIStyle{font=font,fontSize=size,wordWrap=true,padding=new RectOffset()};
                float height=style.CalcHeight(new GUIContent(text),r.width);
                Require(height<=r.height+.1f,"Korean text wraps outside Home: "+text+" "+height+"/"+r.height);count++;
            }
        }
    }
}
