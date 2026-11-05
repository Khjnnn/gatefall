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
        public static readonly string[] TowerKeys = { "spear", "tether", "rupture", "arc", "shatter", "gravity", "seal", "shockgate", "searchlight", "observer", "marker" };
        public static readonly Color Ally = Hex("3FD6C6"), Threat = Hex("E0406A"), Amber = Hex("F0B340"), Ink = Hex("1B2230");
        public static readonly Color Paper = Hex("EEF1F2"), Muted = Hex("A9B4BE"), Slate = Hex("19212B");

        private readonly Dictionary<string, Texture2D> textures = new Dictionary<string, Texture2D>();
        private readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        public Font Regular { get; private set; }
        public Font Bold { get; private set; }
        public Font Heavy { get; private set; }

        public GatefallArt()
        {
            Regular = Resources.Load<Font>("Fonts/NanumGothic-Regular");
            Bold = Resources.Load<Font>("Fonts/NanumGothic-Bold") ?? Regular;
            Heavy = Resources.Load<Font>("Fonts/NanumGothic-ExtraBold") ?? Bold;
        }

        public Texture2D Texture(string name)
        {
            Texture2D texture;
            if (!textures.TryGetValue(name, out texture)) {
                texture = Resources.Load<Texture2D>("Art/V12/" + name) ?? Resources.Load<Texture2D>("Art/V6/" + name) ?? Resources.Load<Texture2D>("Art/V5/" + name);
                if (texture != null) { texture.wrapMode = TextureWrapMode.Clamp; texture.filterMode = FilterMode.Bilinear; }
                textures[name] = texture;
            }
            return texture;
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

        public Sprite Scenery(int index)
        {
            string key = "district-block-" + index;
            Sprite sprite; if (sprites.TryGetValue(key, out sprite)) return sprite;
            Texture2D texture = Texture("district-blocks"); if (texture == null) return null;
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
                case TowerKind.Autocannon: return Amber;
                case TowerKind.Binding: return Hex("4CDBA0");
                case TowerKind.Rupture: return Hex("FF705C");
                case TowerKind.Barrier: return Hex("78ADFF");
                case TowerKind.Arc: return Hex("FFD45A");
                case TowerKind.Shatter: return Hex("F59A4A");
                case TowerKind.Gravity: return Hex("8E80F0");
                case TowerKind.Marker: return Hex("ED89F4");
                case TowerKind.Observer: return Hex("85EAF4");
                case TowerKind.Beacon: return Hex("B69AF6");
                default: return Hex("FFF2C4");
            }
        }

        public void Dispose()
        {
            foreach (Sprite sprite in sprites.Values) if (sprite != null) Object.Destroy(sprite);
            sprites.Clear();
        }

        public static Color Hex(string value) { Color color; ColorUtility.TryParseHtmlString("#" + value, out color); return color; }
        public static Color Alpha(Color color, float alpha) { color.a = alpha; return color; }
    }
}
