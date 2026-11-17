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
            foreach(var kind in new[]{TowerKind.Scatter,TowerKind.Barrage,TowerKind.Piercer,TowerKind.Surge})for(int level=1;level<=2;level++){
                var image=art.TowerIcon(kind,level);var sprite=art.Tower(kind,level);
                if(image==null || image.width!=256 || image.height!=320 || sprite==null || sprite.texture!=image || !Mathf.Approximately(sprite.pixelsPerUnit,86))
                    throw new BuildFailedException("Chapter2 tower atlas/icon/field mismatch: "+kind+level);
            }
            for(int i=CampaignCatalog.ChapterStart(2);i<CampaignCatalog.Stages.Length;i++){
                var stage=CampaignCatalog.Stages[i];var g=new DefenseSimulation(new GameBalance(),7,new CampaignBattle{Stage=stage});
                if(stage.Boss!=(stage.Id=="C02-S022") || g.BoardSockets.Length!=stage.SocketCount || g.WaveCount!=stage.Waves.Length)
                    throw new BuildFailedException("Authored geometry or waves mismatch: "+stage.Id);
                var text=new GUIStyle{font=art.Bold,fontSize=15,wordWrap=true};
                if(text.CalcHeight(new GUIContent(stage.Briefing),344)>50 || text.CalcHeight(new GUIContent(stage.Lesson.Replace(". ",".\n")),344)>70)
                    throw new BuildFailedException("Authored Korean briefing overflow: "+CampaignCatalog.StageId(i));
                if(stage.Boss && text.CalcSize(new GUIContent(stage.BossDisplayName+"  1100 / 1100")).x>340)
                    throw new BuildFailedException("Boss heading overflow: "+stage.Id);
            }
            var tabFont = new GUIStyle { font=art.Bold, fontSize=13, wordWrap=false };
            int chapters = CampaignCatalog.PlayableChapterCount;
            float narrowTabWidth = (960-436-24-(chapters-1)*8)/(float)chapters;
            for (int chapter=2; chapter<=chapters; chapter++) {
                int predecessor = CampaignCatalog.ChapterStart(chapter)-1;
                string locked = chapter+" · "+CampaignCatalog.StageId(predecessor)+" 일반 승리";
                if (tabFont.CalcSize(new GUIContent(locked)).x > Mathf.Min(152,narrowTabWidth)-12)
                    throw new BuildFailedException("Chapter lock text overflow: "+locked);
            }
            var prepFont = new GUIStyle { font=art.Regular, fontSize=14, wordWrap=false };
            if (prepFont.CalcSize(new GUIContent("배치 준비 · 시간 정지")).x > 150)
                throw new BuildFailedException("Compact preparation header text overflow");
            var compactFont = new GUIStyle { font=art.Bold, fontSize=15, wordWrap=false };
            foreach (string progress in new[]{"웨이브 9/9","잔여 적 999"})
                if (compactFont.CalcSize(new GUIContent(progress)).x > 91)
                    throw new BuildFailedException("Compact wave label overflow");
            ValidateBattleReadability(art);
            ValidateRailArt(art);
            var bossFont = new GUIStyle { font=art.Bold, fontSize=13, wordWrap=false };
            foreach (CampaignStage stage in CampaignCatalog.Stages)
                if (stage.BossEntryDistance.HasValue && bossFont.CalcSize(new GUIContent(stage.BossDisplayName)).x > 184)
                    throw new BuildFailedException("Boss dock name overflow: "+stage.Id);
            bossFont.fontSize=12;
            if (bossFont.CalcSize(new GUIContent(MobileHud.DockedBossWarningText)).x > 188)
                throw new BuildFailedException("Boss dock warning overflow");
            bossFont.font=art.Regular;
            if (bossFont.CalcSize(new GUIContent("9999 / 9999")).x > 80)
                throw new BuildFailedException("Boss dock health count overflow");
            var roleFont=new GUIStyle{font=art.Regular,fontSize=GatefallUiStyle.FontSizeLabel,wordWrap=false};
            var titleFont=new GUIStyle{font=art.Bold,fontSize=GatefallUiStyle.FontSizeSection,wordWrap=false};
            foreach(var kind in new[]{TowerKind.Scatter,TowerKind.Barrage,TowerKind.Piercer,TowerKind.Surge}) {
                if(roleFont.CalcSize(new GUIContent(MobileHud.ArmorHint(kind))).x>158)throw new BuildFailedException("Chapter2 armor role overflow");
                for(int level=1;level<=3;level++)if(titleFont.CalcSize(new GUIContent(TowerCatalog.Get(kind).Name+"  Lv"+level)).x>170)throw new BuildFailedException("Chapter2 armor title overflow");
            }
            foreach(var kind in new[]{TowerKind.Barrage,TowerKind.Seal,TowerKind.Beacon,TowerKind.Surge})for(int targets=1;targets<=9;targets++) {
                Vector2 size=roleFont.CalcSize(new GUIContent(MobileHud.FusionRole(kind,targets)));
                if(size.x>150 || size.y>20)throw new BuildFailedException("Chapter2 fusion role overflow");
            }
            var recipeTitleFont=new GUIStyle{font=art.Heavy,fontSize=GatefallUiStyle.FontSizeAction,wordWrap=false};
            var situationFont=new GUIStyle{font=art.Bold,fontSize=GatefallUiStyle.FontSizeCaption,wordWrap=false};
            var previewGame=new DefenseSimulation(new GameBalance(),7);
            foreach(FusionRecipe recipe in TowerCatalog.Recipes) {
                if(recipe.Inheritance==SealInheritance.None)continue;
                if(recipeTitleFont.CalcSize(new GUIContent(recipe.ResultName)).x>150 ||
                    situationFont.CalcSize(new GUIContent(FusionPreview.Situation(previewGame.Stats(recipe)))).x>150 ||
                    titleFont.CalcSize(new GUIContent(recipe.ResultName+"  Lv1")).x>170)
                    throw new BuildFailedException("Inherited fusion label overflow: recipe "+recipe.Id);
            }
            // Narrowest fusion picker is 508 wide. The fit row ends before the cancel column.
            float fitWidth=MobileLayout.FusionFitBox(new Box2(0,0,508,96)).Width;
            foreach(string fit in new[]{FusionPreview.HighFit,FusionPreview.MediumFit,FusionPreview.LowFit,FusionPreview.MarkFit,FusionPreview.NoMarkFit,
                "적중 지점의 폭발 범위 표시","선택한 자리의 사거리 표시"})
                if(roleFont.CalcSize(new GUIContent(fit)).x>fitWidth || roleFont.CalcSize(new GUIContent(fit)).y>18)
                    throw new BuildFailedException("Fusion fit label overflow: "+fit);
            var pickFont=new GUIStyle{font=art.Heavy,fontSize=18,wordWrap=false};
            foreach(FusionRecipe recipe in TowerCatalog.Recipes) {
                foreach(TowerKind material in new[]{recipe.First,recipe.Second})
                    if(pickFont.CalcSize(new GUIContent(MobileHud.ShortName(material)+" Lv2+ 재료를 탭하세요")).x>fitWidth)
                        throw new BuildFailedException("Fusion partner heading overflow: "+recipe.Id);
                string result="주황 자리: "+recipe.ResultName.Replace(" 결계주","")+" · 사거리 "+previewGame.Stats(recipe).Range.ToString("0.#");
                if(roleFont.CalcSize(new GUIContent(result)).x>fitWidth)
                    throw new BuildFailedException("Fusion preview result overflow: "+recipe.Id);
            }
            // Every recipe card names its two materials under the result; both reading orders must fit the card.
            var materialFont=new GUIStyle{font=art.Heavy,fontSize=GatefallUiStyle.FontSizeLabel,wordWrap=false};
            foreach(FusionRecipe recipe in TowerCatalog.Recipes)foreach(TowerKind anchor in new[]{recipe.First,recipe.Second})
                if(materialFont.CalcSize(new GUIContent(MobileHud.FusionMaterials(recipe,anchor))).x>150)
                    throw new BuildFailedException("Fusion material label overflow: recipe "+recipe.Id);
            // The compact wave call keeps the same facts on two lines inside its narrowest shape.
            float compactCall=MobileLayout.NarrowWaveCallWidth-16;
            var callFont=new GUIStyle{font=art.Bold,fontSize=15,wordWrap=false};
            if(callFont.CalcSize(new GUIContent(MobileHud.WaveCallCompactTitle(6,999,100))).x>compactCall)
                throw new BuildFailedException("Compact wave call title overflow");
            callFont.fontSize=12;
            foreach(CampaignStage stage in CampaignCatalog.Stages) {
                var battle=new CampaignBattle{Stage=stage};
                if(!battle.MobileWaveCalls)continue;
                var g=new DefenseSimulation(new GameBalance(),7,battle);
                for(int wave=1;wave<g.WaveCount;wave++)
                    if(callFont.CalcSize(new GUIContent(g.GetWaveBriefing(wave).Summary)).x>compactCall)
                        throw new BuildFailedException("Compact wave call summary overflow: "+stage.Id+" wave "+(wave+1));
            }
            var priceFont=new GUIStyle{font=art.Bold,fontSize=GatefallUiStyle.FontSizeAction,wordWrap=true};
            foreach(string price in new[]{"100","125","160","999"})
                if(priceFont.CalcSize(new GUIContent(price)).x>48 || priceFont.CalcHeight(new GUIContent(price),48)>26)
                    throw new BuildFailedException("Chapter2 three-digit cost overflow: "+price);
            ValidateControllerCalls();
            Directory.CreateDirectory("TestResults/chapter2-implementation");
            File.WriteAllText("TestResults/chapter2-implementation/unity-audit.txt","PASS4 normalized tower sprites,23 authored maps/Korean briefing bounds and actual controller calls through 6–9 waves with supply/catalyst caps.");
            art.Dispose();Debug.Log("GATEFALL CHAPTER TWO AUDIT PASSED");
        }
        static void ValidateRailArt(GatefallArt art)
        {
            void TextureSize(string key, int width, int height)
            {
                Texture2D texture = art.Texture(key);
                if (texture == null || texture.width != width || texture.height != height)
                    throw new BuildFailedException("Rail art texture size: "+key);
            }
            TextureSize("rail-concrete-painted",512,512);
            TextureSize("rail-ballast-painted",256,256);
            TextureSize("rail-props-painted",768,384);
            TextureSize("rail-tunnel-painted",256,192);
            for (int i=0;i<6;i++)
                if (art.RailProp(i).rect.size != new Vector2(256,192))
                    throw new BuildFailedException("Rail scenery atlas cell: "+i);
            Debug.Log("GATEFALL RAIL ART AUDIT PASSED");
        }
        static void ValidateBattleReadability(GatefallArt art)
        {
            void Fits(string text, Font font, int size, float width)
            {
                var style = new GUIStyle { font=font, fontSize=size, wordWrap=false };
                float measured = style.CalcSize(new GUIContent(text)).x;
                if (measured > width)
                    throw new BuildFailedException("Battle HUD label overflow: "+text+" ("+measured+" > "+width+")");
            }
            Fits("경과 9:59", art.Bold, 13, 150);
            Fits("9:59", art.Bold, 13, 45);
            Fits("배치 준비 · 시간 정지", art.Bold, 14, 150);
            Fits("28/28", art.Heavy, 24, 75);
            Fits("28/28", art.Heavy, 17, 60);
            foreach (string direction in new[]{"서","북","동","남"})
                Fits(direction+" 999", art.Heavy, 15, MobileLayout.EntranceLabelWidth-8);
            Fits(CampaignCatalog.ResultCoreStatus(true,204,300), art.Bold, 16, 532);
            Fits(CampaignCatalog.ResultDamageDirections(new[]{144f,156f,0f,0f}), art.Bold, 15, 532);
            foreach (FusionRecipe recipe in TowerCatalog.Recipes) {
                foreach (TowerKind material in new[]{recipe.First,recipe.Second})
                    Fits(MobileHud.ShortName(material)+" Lv2 필요", art.Regular, GatefallUiStyle.FontSizeLabel, 150);
            }
            foreach (string label in new[]{"현재 결계주 Lv2 필요","촉매 필요","마석 부족","2-5 클리어 후 상위 경로가 열립니다.","3-5 클리어 후 상위 경로가 열립니다."}) {
                string compact = label.EndsWith("상위 경로가 열립니다.") ? label.Substring(0, 3)+" 클리어 필요" : label;
                Fits(compact, art.Regular, GatefallUiStyle.FontSizeLabel, 150);
            }
            int maximum=0;
            foreach (CampaignStage stage in CampaignCatalog.Stages) {
                var game = new DefenseSimulation(new GameBalance(),7,new CampaignBattle{Stage=stage});
                for (int i=0;i<game.WaveCount;i++) {
                    string text = "예고 · "+game.GetWaveBriefing(i).Summary;
                    Fits(text, art.Bold, 16, MobileLayout.BattleNoticeWidth(text.Length)-12);
                    maximum=Math.Max(maximum,text.Length);
                }
            }
            Debug.Log("GATEFALL READABILITY AUDIT PASSED: all wave notices, longest "+maximum+" characters; entrance/count/elapsed/result and 13 recipe locks");
        }
        static void Set(GatefallGame target,string property,object value)
        {typeof(GatefallGame).GetProperty(property).GetSetMethod(true).Invoke(target,new[]{value});}

        public static void ValidateBossPresentation()
        {
            var art=new GatefallArt();
            var parent=new GameObject("Authored boss audit");
            var controllerHost=new GameObject("Inactive boss HUD audit");
            controllerHost.SetActive(false);
            try {
                var stage=new CampaignStage {
                    Index=1000,Chapter=3,Number=24,Name="지상 분기장",BossRequired=true,BossEntryDistance=0,
                    BossName="전환기 파수자",BossVictoryTitle="지하 진입로 확보",SocketCount=4,Lanes=new[]{0},Counts=new[]{1,1,1},
                    Layout=new BattlefieldLayout { Id="authored-boss-audit",
                        Roads=new[]{new[]{new V2(-21,4),new V2(-10,4),new V2()},new V2[0],new V2[0],new V2[0]} },
                    Waves=new[]{
                        new CampaignWave{Start=1,Groups=new[]{new SpawnGroup{Lane=0,Count=1,Kind=EnemyKind.Grunt}}},
                        new CampaignWave{Start=10,Groups=new[]{new SpawnGroup{Lane=0,Count=1,Kind=EnemyKind.Boss}}},
                        new CampaignWave{Start=30,Groups=new[]{new SpawnGroup{Lane=0,Count=1,Kind=EnemyKind.Grunt}}}
                    }
                };
                var game=new DefenseSimulation(new GameBalance{OfferCards=false},7,new CampaignBattle{Stage=stage});
                game.Start();
                while(game.Elapsed<10) game.Tick(1f/60,new V2());
                var boss=game.Enemies.Find(e=>e.Kind==EnemyKind.Boss);
                if(game.BossWave!=1 || boss==null || V2.Distance(boss.Position,game.Roads[0][0])>.02f)
                    throw new BuildFailedException("Authored boss entered at the wrong place or wave");
                var legacy=new DefenseSimulation(new GameBalance{OfferCards=false},7,new CampaignBattle{Stage=CampaignCatalog.Stages[5]});
                if(legacy.BossWave!=5 || legacy.Rules.Stage.BossDisplayName!="잿빛 문지기")
                    throw new BuildFailedException("Legacy boss heading changed");

                while(game.Elapsed<16 && boss.BossWindup<=0) game.Tick(1f/60,new V2());
                var view=new ArenaView(parent.transform,game,art);
                view.Sync(game,(float)game.Elapsed,1f/60,new ArenaSelection());
                var ring=Array.Find(parent.GetComponentsInChildren<SpriteRenderer>(),r=>r.name=="Boss telegraph");
                if(ring==null || !ring.enabled || ring.sprite==null || (ring.transform.position-ArenaView.Point(boss.BossTarget)).sqrMagnitude>.0001f)
                    throw new BuildFailedException("Shared boss warning ring failed");

                var controller=controllerHost.AddComponent<GatefallGame>();
                var camera=parent.AddComponent<Camera>();camera.orthographic=true;camera.orthographicSize=14;
                camera.transform.position=new Vector3(0,0,-10);
                typeof(GatefallGame).GetField("gameCamera",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(controller,camera);
                Set(controller,"Simulation",game);Set(controller,"ScreenPage",CampaignScreen.Battle);
                Set(controller,"TouchInput",new MobileInput(new MobileLayout(2400,1080,new Box2(0,0,2400,952))));
                var hud=new MobileHud(controller,art);
                hud.Refresh();
                var items=(System.Collections.IEnumerable)typeof(MobileHud).GetField("items",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);
                bool found=false;
                foreach(object item in items) {
                    string line=(string)item.GetType().GetField("Text").GetValue(item);
                    if(line!=null && line.StartsWith(stage.BossDisplayName+"  ",StringComparison.Ordinal))found=true;
                }
                if(!found || controller.Campaign!=null)throw new BuildFailedException("Authored boss HUD or synthetic isolation failed");
                Debug.Log("GATEFALL AUTHORED BOSS AUDIT PASSED: scheduled wave, road entrance, warning sprite and HUD name");
            } finally {
                UnityEngine.Object.DestroyImmediate(controllerHost);
                UnityEngine.Object.DestroyImmediate(parent);
                art.Dispose();
            }
        }

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
                foreach(int index in new[]{13,16,19,CampaignCatalog.FindStage("C02-S022")}) {
                    var game=new DefenseSimulation(new GameBalance{SpawnEnemies=false,OfferCards=false},7,new CampaignBattle{Stage=CampaignCatalog.Stages[index]});
                    game.WaveCallsEnabled=true;game.Start();Set(controller,"Simulation",game);
                    while(game.Elapsed<game.Balance.Duration) {
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
