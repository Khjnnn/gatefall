using System.Collections.Generic;
using Gatefall.Core;
using UnityEngine;

namespace Gatefall
{
    // v0.6 uses new generated world art and reauthored small UI/effect shapes.
    // V5 remains a defensive fallback if a resource is unexpectedly missing.
    // Pivots sit on each object's ground footprint so y-sorting and positions agree with the simulation.
    public sealed class GatefallArt
    {
        private struct Spec { public float Ppu, X, Y; public Spec(float ppu, float x, float y) { Ppu = ppu; X = x; Y = y; } }
        private static readonly Dictionary<string, Spec> specs = new Dictionary<string, Spec> {
            { "pad", new Spec(84, .5f, .40f) }, { "core", new Spec(80, .5f, .175f) },
            { "enemy-grunt", new Spec(70, .5f, .20f) }, { "enemy-runner", new Spec(80, .5f, .17f) },
            { "enemy-brute", new Spec(74, .5f, .15f) }, { "enemy-siege", new Spec(74, .48f, .19f) },
            { "enemy-boss", new Spec(56, .5f, .13f) },
            { "hunter-0", new Spec(65, .47f, .085f) }, { "hunter-1", new Spec(65, .47f, .085f) }, { "hunter-2", new Spec(65, .47f, .085f) },
            { "fx-rift", new Spec(58, .5f, .47f) }, { "fx-select", new Spec(84, .5f, .5f) }, { "fx-range", new Spec(100, .5f, .5f) },
        };
        public static readonly string[] TowerKeys = { "spear", "tether", "rupture", "arc", "shatter", "gravity", "seal", "shockgate", "searchlight", "observer", "marker", "scatter", "barrage" };
        public static readonly Color Ally = GatefallUiStyle.ColorAlly, Threat = GatefallUiStyle.ColorThreat, Amber = GatefallUiStyle.ColorActionUpgrade, Ink = GatefallUiStyle.ColorTextInk;
        public static readonly Color Paper = GatefallUiStyle.ColorTextPrimary, Muted = GatefallUiStyle.ColorTextSecondary, Slate = Hex("19212B");

        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        private readonly List<Texture2D> generatedTextures=new List<Texture2D>();
        public Font Regular { get; private set; }
        public Font Bold { get; private set; }
        public Font Heavy { get; private set; }

        public GatefallArt()
        {
            Regular = Resources.Load<Font>("Fonts/"+GatefallUiStyle.FontResourceRegular);
            Bold = Resources.Load<Font>("Fonts/"+GatefallUiStyle.FontResourceBold) ?? Regular;
            Heavy = Resources.Load<Font>("Fonts/"+GatefallUiStyle.FontResourceHeavy) ?? Bold;
        }

        public Texture2D Texture(string name)
        {
            Texture2D texture;
            if (!textures.TryGetValue(name, out texture)) {
                texture = Resources.Load<Texture2D>("Art/V12/" + name) ?? Resources.Load<Texture2D>("Art/V6/" + name) ?? Resources.Load<Texture2D>("Art/V5/" + name) ?? Resources.Load<Texture2D>("Art/Chapter2/"+name);
                if(texture==null && (name.StartsWith("tower-scatter-") || name.StartsWith("tower-barrage-")))texture=ChapterTowerTexture(name);
                if (texture != null) { texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear; }
                textures[name] = texture;
            }
            return texture;
        }

        // Normalize an authored atlas cell once, into the same256x320 footprint as the existing tower set.
        // Reused by the field and both HUD render paths. No per-frame readback or texture creation.
        Texture2D ChapterTowerTexture(string name)
        {
            Texture2D atlas=Resources.Load<Texture2D>("Art/Chapter2/tower-atlas");if(atlas==null)return null;
            int cw=atlas.width/2,ch=atlas.height/2,col=name.StartsWith("tower-barrage-")?1:0,row=name.EndsWith("-1")?1:0;
            Color[] pixels=atlas.GetPixels(col*cw,row*ch,cw,ch);
            int left=cw,right=0,bottom=ch,top=0;
            for(int y=0;y<ch;y++)for(int x=0;x<cw;x++)if(pixels[y*cw+x].a>.08f){left=Mathf.Min(left,x);right=Mathf.Max(right,x);bottom=Mathf.Min(bottom,y);top=Mathf.Max(top,y);}
            float scale=Mathf.Min(224f/(right-left+1),266f/(top-bottom+1));
            var result=new Texture2D(256,320,TextureFormat.RGBA32,false){name=name,filterMode=FilterMode.Bilinear,wrapMode=TextureWrapMode.Clamp};
            var output=new Color[256*320];float width=(right-left+1)*scale;
            for(int y=10;y<Mathf.Min(319,10+(top-bottom+1)*scale);y++)for(int x=0;x<256;x++){
                float sx=(x-(256-width)*.5f)/scale+left,sy=(y-10)/scale+bottom;
                if(sx>=left && sx<=right && sy<=top)output[y*256+x]=pixels[Mathf.Clamp((int)sy,0,ch-1)*cw+Mathf.Clamp((int)sx,0,cw-1)];
            }
            result.SetPixels(output);result.Apply(false,true);generatedTextures.Add(result);return result;
        }

        public Sprite Sprite(string name)
        {
            Sprite sprite;
            if (sprites.TryGetValue(name, out sprite)) return sprite;
            Texture2D texture = Texture(name);
            if (texture == null) { Debug.LogWarning("Missing Gatefall art: " + name); sprites[name] = null; return null; }
            Spec spec;
            if (!specs.TryGetValue(name, out spec))
                spec = name.StartsWith("tower-") ? new Spec(86, .5f, .181f) : name.StartsWith("stage-") ? new Spec(54, .5f, .5f) : new Spec(100, .5f, .5f);
            // Import resolution is independent of world size; then ArenaView sets the smaller body scale.
            float sourceWidth = name == "enemy-grunt" || name == "enemy-runner" ? 200 : name == "enemy-brute" ? 220 : name == "enemy-siege" ? 264 : name == "enemy-boss" ? 360 : texture.width;
            float ppu = spec.Ppu * texture.width / sourceWidth;
            sprite = UnityEngine.Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(spec.X, spec.Y), ppu);
            sprites[name] = sprite;
            return sprite;
        }

        public Sprite Scenery(int index,bool market=false)
        {
            string key = (market?"market-block-":"district-block-") + index;
            Sprite sprite; if (sprites.TryGetValue(key, out sprite)) return sprite;
            Texture2D texture = Texture(market?"market-props":"district-blocks"); if (texture == null) return null;
            float w = texture.width / 2f, h = texture.height / 2f;
            sprite = UnityEngine.Sprite.Create(texture, new Rect(index % 2 * w, (1 - index / 2) * h, w, h), new Vector2(.5f, .5f), 100);
            sprites[key] = sprite; return sprite;
        }

        public Sprite Tower(TowerKind kind, int level)
        {
            int shown = kind == TowerKind.Seal ? 1 : Mathf.Clamp(level, 1, 2);
            return Sprite("tower-" + TowerKeys[(int)kind] + "-" + shown);
        }
        public Texture2D TowerIcon(TowerKind kind, int level) { return Texture("tower-" + TowerKeys[(int)kind] + "-" + (kind == TowerKind.Seal ? 1 : Mathf.Clamp(level, 1, 2))); }
        public static Color TowerTint(TowerKind kind, int level)
        {
            if (kind == TowerKind.Beacon) return level >= 3 ? Hex("E4B4FF") : Hex("BE9CFF");
            return level >= 3 ? Hex("FFE4A2") : Color.white;
        }
        public static string EnemyKey(EnemyKind kind) { return "enemy-" + kind.ToString().ToLowerInvariant(); }

        public static Color RoleColor(TowerKind kind)
        {
            switch (kind) {
                case TowerKind.Autocannon: return GatefallUiStyle.ColorRoleSingle;
                case TowerKind.Binding: return GatefallUiStyle.ColorRoleControl;
                case TowerKind.Rupture: return GatefallUiStyle.ColorRoleBlast;
                case TowerKind.Barrier: return GatefallUiStyle.ColorRoleBlock;
                case TowerKind.Arc: return GatefallUiStyle.ColorRoleChain;
                case TowerKind.Shatter: return GatefallUiStyle.ColorRoleShatter;
                case TowerKind.Gravity: return GatefallUiStyle.ColorRoleGravity;
                case TowerKind.Marker: return GatefallUiStyle.ColorRoleMark;
                case TowerKind.Observer: return GatefallUiStyle.ColorRoleObserve;
                case TowerKind.Beacon: return GatefallUiStyle.ColorRoleBeacon;
                case TowerKind.Scatter: return GatefallUiStyle.ColorRoleScatter;
                case TowerKind.Barrage: return GatefallUiStyle.ColorRoleBarrage;
                default: return GatefallUiStyle.ColorRoleFinal;
            }
        }

        public void Dispose()
        {
            foreach (Sprite sprite in sprites.Values) if (sprite != null) {if(Application.isPlaying)Object.Destroy(sprite);else Object.DestroyImmediate(sprite);}
            sprites.Clear();
            foreach(var texture in generatedTextures){if(Application.isPlaying)Object.Destroy(texture);else Object.DestroyImmediate(texture);}generatedTextures.Clear();
        }

        public static Color Hex(string value) { Color color; ColorUtility.TryParseHtmlString("#" + value, out color); return color; }
        public static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
    }
}
