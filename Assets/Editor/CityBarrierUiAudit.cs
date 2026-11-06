using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Gatefall.Core;
using UnityEditor.Build;
using UnityEngine;

namespace Gatefall.Editor
{
    public static class CityBarrierUiAudit
    {
        [Serializable] private sealed class FontRow { public string text; public int size, width, height; public float measuredWidth, measuredHeight; }
        [Serializable] private sealed class Report {
            public string version, style, manifest;
            public int cachedRoleSurfaces, cachedGlyphs;
            public FontRow[] labels;
            public string scope="Real Unity surface/cache and Korean font bounds; separate Android UI/input/save review required.";
        }

        public static void Validate()
        {
            const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
            const string folder="TestResults/refined-ui-20261106";
            Directory.CreateDirectory(folder+"/skins");
            var hud=new MobileHud(null,null);
            var theme=typeof(MobileHud).GetMethod("ThemeTexture",flags);
            var role=typeof(MobileHud).GetMethod("RoleSkin",flags);
            var cache=(System.Collections.IDictionary)typeof(MobileHud).GetField("themeTextures",flags).GetValue(hud);
            var colors=new HashSet<Color>();
            try {
                foreach(TowerDefinition definition in TowerCatalog.All)colors.Add(GatefallArt.RoleColor(definition.Kind));
                colors.Add(GatefallUiStyle.ColorActionFusion);
                foreach(Color color in colors) {
                    string name=(string)role.Invoke(hud,new object[]{color});
                    var texture=(Texture2D)theme.Invoke(hud,new object[]{name});
                    Color expected=Color.Lerp(GatefallUiStyle.ColorCardFace,color,GatefallUiStyle.CardRoleTintOpacity);
                    Color actual=texture.GetPixel(64,64);
                    if(texture.GetPixel(0,0).a!=0 || !ReferenceEquals(texture,theme.Invoke(hud,new object[]{name})) ||
                        Mathf.Abs(actual.r-expected.r)>.01f || Mathf.Abs(actual.g-expected.g)>.01f || Mathf.Abs(actual.b-expected.b)>.01f)
                        throw new BuildFailedException("City barrier role surface/cache failed: "+name);
                    File.WriteAllBytes(folder+"/skins/"+name+".png",texture.EncodeToPNG());
                }
                int glyphs=0;
                foreach(string name in new[]{"ui-glyph-upgrade","ui-glyph-fusion","ui-glyph-sell","ui-glyph-close","ui-glyph-check"}) {
                    var texture=(Texture2D)theme.Invoke(hud,new object[]{name});
                    int ink=0;foreach(Color32 pixel in texture.GetPixels32())if(pixel.a>127)ink++;
                    if(ink<50 || ink>1500 || texture.GetPixel(0,0).a!=0 || !ReferenceEquals(texture,theme.Invoke(hud,new object[]{name})))
                        throw new BuildFailedException("HUD glyph/cache failed: "+name);
                    File.WriteAllBytes(folder+"/skins/"+name+".png",texture.EncodeToPNG());glyphs++;
                }
                var rows=new List<FontRow>();
                void Measure(string text,int size,int width,int height,bool heavy=false) {
                    var font=Resources.Load<Font>("Fonts/"+(heavy?GatefallUiStyle.FontResourceHeavy:GatefallUiStyle.FontResourceBold));
                    font.RequestCharactersInTexture(text,size,FontStyle.Normal);
                    var style=new GUIStyle{font=font,fontSize=size,wordWrap=true,padding=new RectOffset()};
                    var content=new GUIContent(text);
                    var row=new FontRow{text=text,size=size,width=width,height=height,measuredWidth=style.CalcSize(content).x,measuredHeight=style.CalcHeight(content,width)};
                    rows.Add(row);
                    if(row.measuredHeight>height || row.measuredWidth>width && row.measuredHeight>size*1.5f)
                        throw new BuildFailedException("City barrier font overflow: "+text+" "+row.measuredWidth+"/"+width+" x "+row.measuredHeight+"/"+height);
                }
                foreach(string label in new[]{"단일","감속","범위","저지","표식","부족","상한","불가","완료","잠금","가능","재료"})Measure(label,GatefallUiStyle.FontSizeLabel,40,22);
                Measure("한 번 더 누르면 판매",GatefallUiStyle.FontSizeLabel,234,48);
                Measure("Lv3 강화",GatefallUiStyle.FontSizeAction,118,26);
                Measure("강화 완료",GatefallUiStyle.FontSizeAction,118,26);
                Measure("최대 Lv3",GatefallUiStyle.FontSizeLabel,150,26);
                Measure(MobileHud.SlowResistanceHint,GatefallUiStyle.FontSizeLabel,158,28);
                Measure("선택한 자리에 Lv1 생성 · 재료 Lv2 이상",GatefallUiStyle.FontSizeLabel,330,24);
                Measure("범위·감속 · 재료2개",GatefallUiStyle.FontSizeLabel,150,20);
                Measure("해금·편성 필요",GatefallUiStyle.FontSizeLabel,140,26);
                foreach(TowerDefinition definition in TowerCatalog.All) {
                    for(int level=1;level<=3;level++)Measure(definition.Name+"  Lv"+level,GatefallUiStyle.FontSizeSection,170,28,true);
                    Measure(definition.Name.Replace(" 결계주",""),GatefallUiStyle.FontSizeAction,150,25,true);
                }
                var report=new Report{version=Application.version,style=GatefallUiStyle.StyleId,manifest=GatefallUiStyle.ManifestSha256,cachedRoleSurfaces=colors.Count,cachedGlyphs=glyphs,labels=rows.ToArray()};
                File.WriteAllText(folder+"/unity-style-audit.json",JsonUtility.ToJson(report,true));
                Debug.Log("GATEFALL CITY BARRIER STYLE PASSED: "+colors.Count+" cached 2% role surfaces; "+glyphs+" line glyphs; "+rows.Count+" Korean text bounds.");
            }
            finally { foreach(Texture2D texture in cache.Values)UnityEngine.Object.DestroyImmediate(texture);cache.Clear(); }
        }
    }
}
