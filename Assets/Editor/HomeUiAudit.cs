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
                            string expected=stage<14?(veteran?(crisis?"14 / 42":"42 / 42"):"0 / 42"):(crisis?"11 / 18":"17 / 18");
                            Require(displayed.Contains(expected) && displayed.Any(t=>t.StartsWith("챕터 "+controller.CurrentChapter)),"chapter-specific Home record "+expected);
                            var regions=controller.TouchInput.Regions;
                            Require(regions.Count==(veteran?5:4),"enabled/locked navigation count");
                            Require(regions.Any(r=>(int)r.Action==652)==veteran,"loadout gate");
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
                    Require(writes==0 && before.SequenceEqual(CampaignStore.Encode(profile)),"navigation changed profile");
                }
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
