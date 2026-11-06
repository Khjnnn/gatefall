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
        private static readonly Color FusionColor = GatefallArt.Hex("B89AFF");

        private Texture2D ThemeTexture(string name)
        {
            Texture2D cached;
            if (themeTextures.TryGetValue(name, out cached)) return cached;
            Color top, bottom, rim, foot; float radius = 19, depth = 5;
            switch (name) {
                case "ui-panel": top=C("304C72"); bottom=C("1B2C48"); rim=C("7A9AC6"); foot=C("101E36"); radius=23; depth=3; break;
                case "ui-panel-flat": top=C("2B4568"); bottom=C("233954"); rim=C("48688F"); foot=C("172941"); depth=2; break;
                case "ui-selected": top=C("304F74"); bottom=C("243955"); rim=C("79F0CB"); foot=C("14283E"); depth=3; break;
                case "ui-chip": top=C("30496A"); bottom=C("1D304C"); rim=C("526E93"); foot=C("13243A"); radius=12; depth=2; break;
                case "ui-button-primary": top=C("8AF0C5"); bottom=C("39C8A5"); rim=C("D0FFE4"); foot=C("147B72"); break;
                case "ui-button": top=C("4D709E"); bottom=C("304D75"); rim=C("92B1D8"); foot=C("1B3152"); break;
                case "ui-button-danger": top=C("F17F92"); bottom=C("C54668"); rim=C("FFD0D7"); foot=C("7D2F52"); break;
                case "ui-button-disabled": top=C("3C4D63"); bottom=C("2D3B50"); rim=C("5D6A7C"); foot=C("202D40"); depth=2; break;
                // Tinting this neutral frame gives each tower/action a coloured rim and a quiet dark face.
                case "ui-role-card": top=new Color(.29f,.32f,.39f); bottom=new Color(.16f,.20f,.28f); rim=Color.white; foot=new Color(.09f,.12f,.20f); depth=4; break;
                case "ui-bar": top=Color.white; bottom=new Color(.75f,.85f,.92f); rim=Color.white; foot=new Color(.45f,.6f,.7f); radius=7; depth=1; break;
                case "ui-bar-back": top=C("15243A"); bottom=C("101D30"); rim=C("405875"); foot=C("101D30"); radius=7; depth=1; break;
                default: return null;
            }
            cached = BakeSurface(name, top, bottom, rim, foot, radius, depth);
            themeTextures.Add(name, cached);
            return cached;
        }

        private static Color C(string hex) { return GatefallArt.Hex(hex); }
        private static Texture2D BakeSurface(string name, Color top, Color bottom, Color rim, Color foot, float radius, float depth)
        {
            const int size=128;
            var pixels=new Color32[size*size];
            for(int y=0;y<size;y++)for(int x=0;x<size;x++) {
                float px=x+.5f,py=size-y-.5f;
                Color p=Color.clear;
                Composite(ref p, new Color(.015f,.035f,.07f,.45f), Coverage(px,py,1,5,126,123,radius));
                Composite(ref p, foot, Coverage(px,py,1,2,126,125,radius));
                float height=126-depth;
                Composite(ref p, rim, Coverage(px,py,1,1,126,height,radius));
                float face=Coverage(px,py,3,3,122,height-4,Mathf.Max(2,radius-2));
                Color fill=Color.Lerp(top,bottom,Mathf.Clamp01((py-3)/(height-4)));
                Composite(ref p,fill,face);
                // A thin soft top-light gives a tactile edge without reflective metal or noisy ornament.
                float light=Mathf.Clamp01(1-(py-4)/12f)*.12f*face;
                Composite(ref p,new Color(1,1,1,light),1);
                pixels[y*size+x]=p;
            }
            var texture=new Texture2D(size,size,TextureFormat.RGBA32,false);
            texture.name="Gatefall soft "+name;texture.wrapMode=TextureWrapMode.Clamp;texture.filterMode=FilterMode.Bilinear;
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
        }
        private void CardAccent(Rect area,Color color)
        {
            Skin(new Rect(area.x+14,area.y+9,area.width-28,4),"ui-bar",color);
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
