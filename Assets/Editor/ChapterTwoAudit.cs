using System;
using System.IO;
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
                if(text.CalcHeight(new GUIContent(stage.Briefing),344)>50 || text.CalcHeight(new GUIContent(stage.Lesson),344)>70)
                    throw new BuildFailedException("Chapter2 Korean briefing overflow: "+CampaignCatalog.StageId(i));
            }
            Directory.CreateDirectory("TestResults/chapter2-implementation");
            File.WriteAllText("TestResults/chapter2-implementation/unity-audit.txt","PASS4 normalized tower sprites,6 staged maps and Korean briefing bounds. Android gameplay separately required.");
            art.Dispose();Debug.Log("GATEFALL CHAPTER TWO AUDIT PASSED");
        }
    }
}
