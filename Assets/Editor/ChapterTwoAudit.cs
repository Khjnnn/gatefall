using System;
using System.IO;
using System.Reflection;
using Gatefall.Core;
using UnityEngine;
using UnityEditor.Build;
namespace Gatefall.Editor
{
    public static class ChapterTwoAudit
    {
        public static void Validate()
        {
            var art=new GatefallArt();
            foreach(var kind in new[]{TowerKind.Scatter,TowerKind.Barrage})for(int level=1;level<=2;level++){
                var image=art.TowerIcon(kind,level);var sprite=art.Tower(kind,level);
                if(image==null || image.width!=256 || image.height!=320 || sprite==null || sprite.texture!=image || !Mathf.Approximately(sprite.pixelsPerUnit,86))
                    throw new BuildFailedException("Chapter2 tower atlas/icon/field mismatch: "+kind+level);
            }
            for(int i=14;i<20;i++){
                var stage=CampaignCatalog.Stages[i];var g=new DefenseSimulation(new GameBalance(),7,new CampaignBattle{Stage=stage});
                if(stage.Boss || g.BoardSockets.Length!=stage.SocketCount || g.WaveCount!=stage.Waves.Length)throw new BuildFailedException("Chapter2 geometry/wave mismatch");
                var text=new GUIStyle{font=art.Bold,fontSize=15,wordWrap=true};
                if(text.CalcHeight(new GUIContent(stage.Briefing),344)>50 || text.CalcHeight(new GUIContent(stage.Lesson.Replace(". ",".\n")),344)>70)
                    throw new BuildFailedException("Chapter2 Korean briefing overflow: "+CampaignCatalog.StageId(i));
            }
            var roleFont=new GUIStyle{font=art.Regular,fontSize=GatefallUiStyle.FontSizeLabel,wordWrap=false};
            var titleFont=new GUIStyle{font=art.Bold,fontSize=GatefallUiStyle.FontSizeSection,wordWrap=false};
            foreach(var kind in new[]{TowerKind.Scatter,TowerKind.Barrage}) {
                if(roleFont.CalcSize(new GUIContent(MobileHud.ArmorHint(kind))).x>158)throw new BuildFailedException("Chapter2 armor role overflow");
                for(int level=1;level<=3;level++)if(titleFont.CalcSize(new GUIContent(TowerCatalog.Get(kind).Name+"  Lv"+level)).x>170)throw new BuildFailedException("Chapter2 armor title overflow");
            }
            foreach(var kind in new[]{TowerKind.Barrage,TowerKind.Seal,TowerKind.Beacon})for(int targets=1;targets<=9;targets++) {
                Vector2 size=roleFont.CalcSize(new GUIContent(MobileHud.FusionRole(kind,targets)));
                if(size.x>150 || size.y>20)throw new BuildFailedException("Chapter2 fusion role overflow");
            }
            var priceFont=new GUIStyle{font=art.Bold,fontSize=GatefallUiStyle.FontSizeAction,wordWrap=true};
            foreach(string price in new[]{"100","125","160","999"})
                if(priceFont.CalcSize(new GUIContent(price)).x>48 || priceFont.CalcHeight(new GUIContent(price),48)>26)
                    throw new BuildFailedException("Chapter2 three-digit cost overflow: "+price);
            ValidateControllerCalls();
            Directory.CreateDirectory("TestResults/chapter2-implementation");
            File.WriteAllText("TestResults/chapter2-implementation/unity-audit.txt","PASS4 normalized tower sprites,6 staged maps/Korean briefing bounds and actual controller action dispatch through all6/7/8 waves with supply/catalyst caps. Android gameplay separately required.");
            art.Dispose();Debug.Log("GATEFALL CHAPTER TWO AUDIT PASSED");
        }
        static void Set(GatefallGame target,string property,object value)
        {typeof(GatefallGame).GetProperty(property).GetSetMethod(true).Invoke(target,new[]{value});}
        static void ValidateControllerCalls()
        {
            // Inactive Editor-only component: no Awake, persistent profile, camera or audio setup.
            var host=new GameObject("Chapter2 controller audit");host.SetActive(false);
            try {
                var controller=host.AddComponent<GatefallGame>();
                if(controller.Campaign!=null)throw new BuildFailedException("Controller audit must not initialize persistent storage");
                var handle=typeof(GatefallGame).GetMethod("HandleAction",BindingFlags.Instance|BindingFlags.NonPublic);
                Set(controller,"ScreenPage",CampaignScreen.Battle);
                Set(controller,"TouchInput",new MobileInput(new MobileLayout(2400,1080,new Box2(0,0,2400,952))));
                foreach(int index in new[]{13,16,19}) {
                    var game=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,new CampaignBattle{Stage=CampaignCatalog.Stages[index]});
                    game.WaveCallsEnabled=true;game.Start();Set(controller,"Simulation",game);
                    while(game.Elapsed<235) {
                        if(game.CanCallNextWave()=="") {
                            int before=game.Telemetry.WaveCalls;
                            handle.Invoke(controller,new object[]{(MobileAction)(MobileLayout.WaveCallActionBase+game.NextCallWave)});
                            if(game.Telemetry.WaveCalls!=before+1)throw new BuildFailedException("Controller dropped wave call: "+game.NextCallWave);
                        }
                        game.Tick(.05f,new V2());game.Events.Clear();
                    }
                    if(game.Telemetry.WaveCalls!=game.WaveCount-1 || game.SuppliesGranted!=5 || game.Telemetry.CoresGranted!=2)
                        throw new BuildFailedException("Controller late-wave call/economy mismatch");
                }
            } finally {UnityEngine.Object.DestroyImmediate(host);}
        }
    }
}
