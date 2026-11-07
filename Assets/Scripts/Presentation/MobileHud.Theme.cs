using System.Collections.Generic;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    public sealed partial class MobileHud
    {
        // Small original nine-slice surfaces, baked once per HUD. Both renderers use the same pixels.
        // The shadow and raised lower edge stay inside the existing touch rectangle.
        private readonly Dictionary<string, Texture2D> themeTextures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<Color, string> roleSkins = new Dictionary<Color, string>();
        private static readonly Color FusionColor = GatefallUiStyle.ColorActionFusion;

        private string RoleSkin(Color accent)
        {
            string name;
            if (!roleSkins.TryGetValue(accent, out name)) {
                name = "ui-role-" + ColorUtility.ToHtmlStringRGB(accent);
                roleSkins.Add(accent, name);
            }
            return name;
        }

        // The shared 100-PPU sliced border preserves one edge texel per HUD unit.
        // Coverage starts at (1,1), so subtract that inset from the requested outer corner extent.
        private static float BakeRadius(int hudRadius) { return Mathf.Max(2, hudRadius - 1); }

        private Texture2D ThemeTexture(string name)
        {
            Texture2D cached;
            if (themeTextures.TryGetValue(name, out cached)) return cached;
            if(name.StartsWith("ui-glyph-")) {
                cached=BakeGlyph(name);themeTextures.Add(name,cached);return cached;
            }
            bool pressed=name.EndsWith("-pressed");
            string surface=pressed?name.Substring(0,name.Length-8):name;
            Color top, bottom, rim, foot;
            float radius = BakeRadius(GatefallUiStyle.RadiusMedium), depth = GatefallUiStyle.DepthControl;
            switch (surface) {
                case "ui-panel": top=GatefallUiStyle.ColorPanelTop; bottom=GatefallUiStyle.ColorPanelBottom; rim=GatefallUiStyle.ColorPanelRim; foot=GatefallUiStyle.ColorPanelFoot; radius=BakeRadius(GatefallUiStyle.RadiusLarge); depth=GatefallUiStyle.DepthPanel; break;
                case "ui-panel-flat": top=bottom=GatefallUiStyle.ColorCardFace; rim=GatefallUiStyle.ColorCardRim; foot=GatefallUiStyle.ColorPanelFoot; depth=GatefallUiStyle.DepthPanel; break;
                case "ui-selected": top=bottom=GatefallUiStyle.ColorCardFace; rim=GatefallUiStyle.ColorSelected; foot=GatefallUiStyle.ColorPanelFoot; break;
                case "ui-chip": top=bottom=GatefallUiStyle.ColorCardFace; rim=GatefallUiStyle.ColorCardRim; foot=GatefallUiStyle.ColorPanelFoot; radius=BakeRadius(GatefallUiStyle.RadiusSmall); depth=GatefallUiStyle.DepthPanel; break;
                case "ui-button-primary": top=GatefallUiStyle.ColorActionPrimaryTop; bottom=GatefallUiStyle.ColorActionPrimaryBottom; rim=GatefallUiStyle.ColorActionPrimaryRim; foot=GatefallUiStyle.ColorActionPrimaryFoot; break;
                case "ui-button": top=GatefallUiStyle.ColorActionSecondaryTop; bottom=GatefallUiStyle.ColorActionSecondaryBottom; rim=GatefallUiStyle.ColorActionSecondaryRim; foot=GatefallUiStyle.ColorActionSecondaryFoot; break;
                case "ui-button-upgrade": top=GatefallUiStyle.ColorActionUpgradeTop; bottom=GatefallUiStyle.ColorActionUpgradeBottom; rim=GatefallUiStyle.ColorActionUpgradeRim; foot=GatefallUiStyle.ColorActionUpgradeFoot; break;
                case "ui-button-fusion": top=GatefallUiStyle.ColorActionFusionTop; bottom=GatefallUiStyle.ColorActionFusionBottom; rim=GatefallUiStyle.ColorActionFusionRim; foot=GatefallUiStyle.ColorActionFusionFoot; break;
                case "ui-button-danger": top=GatefallUiStyle.ColorActionDangerTop; bottom=GatefallUiStyle.ColorActionDangerBottom; rim=GatefallUiStyle.ColorActionDangerRim; foot=GatefallUiStyle.ColorActionDangerFoot; break;
                case "ui-button-disabled": top=GatefallUiStyle.ColorDisabledTop; bottom=GatefallUiStyle.ColorDisabledBottom; rim=GatefallUiStyle.ColorDisabledRim; foot=GatefallUiStyle.ColorDisabledFoot; break;
                case "ui-role-card": top=bottom=GatefallUiStyle.ColorCardFace; rim=GatefallUiStyle.ColorCardRim; foot=GatefallUiStyle.ColorPanelFoot; break;
                case "ui-bar": top=Color.white; bottom=new Color(.75f,.85f,.92f); rim=Color.white; foot=new Color(.45f,.6f,.7f); radius=7; depth=1; break;
                case "ui-bar-back": top=C("15243A"); bottom=C("101D30"); rim=C("405875"); foot=C("101D30"); radius=7; depth=1; break;
                default:
                    if (!surface.StartsWith("ui-role-")) return null;
                    Color role=C(surface.Substring(8));
                    rim=Color.Lerp(GatefallUiStyle.ColorCardRim,role,GatefallUiStyle.CardRoleRimOpacity);
                    top=bottom=Color.Lerp(GatefallUiStyle.ColorCardFace,role,GatefallUiStyle.CardRoleTintOpacity);
                    foot=GatefallUiStyle.ColorPanelFoot;
                    break;
            }
            if(pressed){top=Color.Lerp(top,bottom,.25f);depth=1;}
            cached = BakeSurface(name, top, bottom, rim, foot, radius, depth);
            themeTextures.Add(name, cached);
            return cached;
        }

        private static Color C(string hex) { return GatefallArt.Hex(hex); }
        private static Texture2D BakeSurface(string name, Color top, Color bottom, Color rim, Color foot, float radius, float depth)
        {
            const int size=128;
            var pixels=new Color32[size*size];
            bool chamfer=GatefallUiStyle.ShapeCorner=="chamfer" && name!="ui-chip" && !name.StartsWith("ui-bar");
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                float px=x+.5f,py=size-y-.5f;
                Color p=Color.clear;
                Composite(ref p, new Color(.015f,.035f,.07f,GatefallUiStyle.ShadowOpacity), SurfaceCoverage(px,py,1,5,126,123,radius,chamfer));
                Composite(ref p, foot, SurfaceCoverage(px,py,1,2,126,125,radius,chamfer));
                float height=126-depth;
                Composite(ref p, rim, SurfaceCoverage(px,py,1,1,126,height,radius,chamfer));
                float inset=1+GatefallUiStyle.BorderDefault;
                float face=SurfaceCoverage(px,py,inset,inset,126-2*GatefallUiStyle.BorderDefault,height-2*GatefallUiStyle.BorderDefault,Mathf.Max(2,radius-GatefallUiStyle.BorderDefault),chamfer);
                Color fill=Color.Lerp(top,bottom,Mathf.Clamp01((py-inset)/(height-2*GatefallUiStyle.BorderDefault)));
                Composite(ref p,fill,face);
                float light=Mathf.Clamp01(1-(py-inset)/12f)*GatefallUiStyle.SurfaceHighlightOpacity*face;
                Composite(ref p,new Color(1,1,1,light),1);
                pixels[y*size+x]=p;
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.name="Gatefall urban ward "+name;texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
            texture.SetPixels32(pixels);texture.Apply(false,false);
            return texture;
        }
        private static float Coverage(float px,float py,float x,float y,float width,float height,float radius)
        {
            float qx=Mathf.Abs(px-x-width/2)-width/2+radius,qy=Mathf.Abs(py-y-height/2)-height/2+radius;
            float dx=Mathf.Max(qx,0),dy=Mathf.Max(qy,0);
            float distance=Mathf.Sqrt(dx*dx+dy*dy)+Mathf.Min(Mathf.Max(qx,qy),0)-radius;
            return Mathf.Clamp01(.5f-distance);
        }
        private static float SurfaceCoverage(float px,float py,float x,float y,float width,float height,float corner,bool chamfer)
        {
            if(!chamfer)return Coverage(px,py,x,y,width,height,corner);
            float ax=Mathf.Abs(px-x-width/2),ay=Mathf.Abs(py-y-height/2);
            float distance=Mathf.Max(Mathf.Max(ax-width/2,ay-height/2),(ax+ay-width/2-height/2+corner)*.70710678f);
            return Mathf.Clamp01(.5f-distance);
        }
        private static void Composite(ref Color under,Color over,float coverage)
        {
            float alpha=over.a*coverage;if(alpha<=0)return;
            float combined=alpha+under.a*(1-alpha);
            under=new Color((over.r*alpha+under.r*under.a*(1-alpha))/combined,
                (over.g*alpha+under.g*under.a*(1-alpha))/combined,
                (over.b*alpha+under.b*under.a*(1-alpha))/combined,combined);
        }
        private void DisposeTheme()
        {
            foreach(Texture2D texture in themeTextures.Values)if(texture!=null)Object.Destroy(texture);
            themeTextures.Clear();
            roleSkins.Clear();
        }
        private void RoleBadge(Rect area,string label,Color accent)
        {
            Fill(new Rect(area.x+3,area.y+8,2,area.height-16),accent);
            Text(new Rect(area.x+10,area.y,area.width-10,area.height),label,GatefallUiStyle.FontSizeLabel,Muted,TextAnchor.MiddleLeft,0);
        }
        private void Glyph(Rect area,string name,Color color) { Tex(area,ThemeTexture(name),color); }
        private static Texture2D BakeGlyph(string name)
        {
            Vector4[] lines;
            switch(name) {
                case "ui-glyph-upgrade": lines=new[]{new Vector4(16,31,32,17),new Vector4(32,17,48,31),new Vector4(16,47,32,33),new Vector4(32,33,48,47)};break;
                case "ui-glyph-fusion": lines=new[]{new Vector4(9,32,23,18),new Vector4(23,18,37,32),new Vector4(37,32,23,46),new Vector4(23,46,9,32),new Vector4(27,32,41,18),new Vector4(41,18,55,32),new Vector4(55,32,41,46),new Vector4(41,46,27,32)};break;
                case "ui-glyph-sell": lines=new[]{new Vector4(14,34,34,14),new Vector4(34,14,49,14),new Vector4(49,14,49,29),new Vector4(49,29,29,49),new Vector4(29,49,14,34),new Vector4(39,23,40,24)};break;
                case "ui-glyph-close": lines=new[]{new Vector4(20,20,44,44),new Vector4(44,20,20,44)};break;
                case "ui-glyph-check": lines=new[]{new Vector4(16,32,27,43),new Vector4(27,43,49,21)};break;
                case "ui-glyph-ward": lines=new[]{new Vector4(32,7,45,25),new Vector4(45,25,32,44),new Vector4(32,44,19,25),new Vector4(19,25,32,7),new Vector4(32,7,32,44),new Vector4(12,24,12,43),new Vector4(12,43,32,57),new Vector4(32,57,52,43),new Vector4(52,43,52,24)};break;
                case "ui-glyph-home": lines=new[]{new Vector4(10,29,32,10),new Vector4(32,10,54,29),new Vector4(16,27,16,53),new Vector4(16,53,48,53),new Vector4(48,53,48,27),new Vector4(27,53,27,36),new Vector4(27,36,37,36),new Vector4(37,36,37,53)};break;
                case "ui-glyph-map": lines=new[]{new Vector4(10,16,24,10),new Vector4(24,10,40,17),new Vector4(40,17,54,10),new Vector4(54,10,54,48),new Vector4(54,48,40,54),new Vector4(40,54,24,47),new Vector4(24,47,10,54),new Vector4(10,54,10,16),new Vector4(24,10,24,47),new Vector4(40,17,40,54)};break;
                case "ui-glyph-loadout": lines=new[]{new Vector4(10,43,10,29),new Vector4(10,29,16,22),new Vector4(16,22,22,29),new Vector4(22,29,22,43),new Vector4(26,43,26,18),new Vector4(26,18,32,8),new Vector4(32,8,38,18),new Vector4(38,18,38,43),new Vector4(42,43,42,29),new Vector4(42,29,48,22),new Vector4(48,22,54,29),new Vector4(54,29,54,43),new Vector4(8,52,56,52)};break;
                case "ui-glyph-hunter": lines=new[]{new Vector4(24,13,40,13),new Vector4(40,13,44,23),new Vector4(44,23,40,33),new Vector4(40,33,24,33),new Vector4(24,33,20,23),new Vector4(20,23,24,13),new Vector4(14,53,16,43),new Vector4(16,43,26,38),new Vector4(26,38,38,38),new Vector4(38,38,48,43),new Vector4(48,43,50,53),new Vector4(50,53,14,53)};break;
                case "ui-glyph-gear": lines=new[]{new Vector4(26,8,38,8),new Vector4(38,8,40,16),new Vector4(40,16,48,18),new Vector4(48,18,56,26),new Vector4(56,26,56,38),new Vector4(56,38,48,46),new Vector4(48,46,40,48),new Vector4(40,48,38,56),new Vector4(38,56,26,56),new Vector4(26,56,24,48),new Vector4(24,48,16,46),new Vector4(16,46,8,38),new Vector4(8,38,8,26),new Vector4(8,26,16,18),new Vector4(16,18,24,16),new Vector4(24,16,26,8),new Vector4(25,25,39,25),new Vector4(39,25,39,39),new Vector4(39,39,25,39),new Vector4(25,39,25,25)};break;
                case "ui-glyph-back": lines=new[]{new Vector4(30,15,13,32),new Vector4(13,32,30,49),new Vector4(13,32,53,32)};break;
                case "ui-glyph-play": lines=new[]{new Vector4(20,12,51,32),new Vector4(51,32,20,52),new Vector4(20,52,20,12)};break;
                case "ui-glyph-lock": lines=new[]{new Vector4(20,28,20,18),new Vector4(20,18,26,11),new Vector4(26,11,38,11),new Vector4(38,11,44,18),new Vector4(44,18,44,28),new Vector4(14,28,50,28),new Vector4(50,28,50,53),new Vector4(50,53,14,53),new Vector4(14,53,14,28),new Vector4(32,37,32,44)};break;
                default: throw new System.ArgumentException("Unknown HUD glyph: "+name);
            }
            const int size=64;var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                Vector2 p=new Vector2(x+.5f,size-y-.5f);float distance=float.MaxValue;
                foreach(Vector4 line in lines) {
                    Vector2 a=new Vector2(line.x,line.y),b=new Vector2(line.z,line.w),d=b-a;
                    Vector2 closest=a+d*Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);
                    distance=Mathf.Min(distance,Vector2.Distance(p,closest));
                }
                pixels[y*size+x]=new Color(1,1,1,Mathf.Clamp01(GatefallUiStyle.GlyphStrokeWidth/2+.5f-distance));
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false){name=name,wrapMode=TextureWrapMode.Clamp,filterMode=FilterMode.Bilinear};
            texture.SetPixels32(pixels);texture.Apply(false,false);return texture;
        }
        // Campaign loadout cards share this narrow role cue with the contextual menus.
        private void CardAccent(Rect area,Color color)
        {
            Skin(new Rect(area.x+GatefallUiStyle.Space3,area.y+GatefallUiStyle.Space2,area.width-2*GatefallUiStyle.Space3,2),"ui-bar",color);
        }
        private void StateBadge(Rect area,string label)
        {
            if(string.IsNullOrEmpty(label))return;
            Skin(area,"ui-chip");
            Text(area,label,GatefallUiStyle.FontSizeLabel,Paper,TextAnchor.MiddleCenter,1);
        }
        private static Color GrowthColor(CardKind kind)
        {
            switch(kind) {
                case CardKind.Ballistic:return Amber;
                case CardKind.Control:return GatefallArt.RoleColor(TowerKind.Binding);
                case CardKind.Blast:return GatefallArt.RoleColor(TowerKind.Rupture);
                case CardKind.Range:return C("8BBFFF");
                case CardKind.Credits:return C("FFDE81");
                default:return FusionColor;
            }
        }
    }
}
