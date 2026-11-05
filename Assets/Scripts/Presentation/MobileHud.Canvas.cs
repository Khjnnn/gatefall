using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Gatefall
{
    // uGUI back end for the same per-frame draw list that Draw() renders through IMGUI.
    // Item i is always drawn by pooled slot i, so sibling order equals draw order. A slot writes a property only
    // when it changes, which keeps unchanged frames from dirtying the canvas. Touch input stays in MobileInput:
    // no graphic is a raycast target and the canvas has no GraphicRaycaster.
    public sealed partial class MobileHud
    {
        private sealed class Slot
        {
            public RectTransform Rect;
            public Image Image;
            public RawImage Raw;
            public Text Text, Shadow;
            public Rect Area = new Rect(float.NaN, float.NaN, 0, 0);
        }

        private Canvas canvas;
        private RectTransform canvasRoot;
        private readonly List<Slot> slots = new List<Slot>(256);
        private readonly Dictionary<string, Sprite> skinSprites = new Dictionary<string, Sprite>();
        private int usedSlots;

        private Font TextFont(int weight) { return weight >= 2 ? art.Heavy : weight == 1 ? art.Bold : art.Regular; }

        public void SetCanvasVisible(bool visible)
        {
            if (canvas != null && canvas.enabled != visible) canvas.enabled = visible;
        }

        // Call after the last Refresh() of the frame (LateUpdate).
        public void RenderCanvas()
        {
            if (game == null || layout == null) return;
            if (canvas == null) CreateCanvas();
            SetCanvasVisible(true);
            // Same mapping as IMGUI's GUI.matrix: reference units scaled by layout.Scale from a top-left screen origin.
            // Scaling the canvas (not a transform) lets text rasterize at the final pixel size.
            if (canvas.scaleFactor != layout.Scale) canvas.scaleFactor = layout.Scale;
            Vector2 origin = new Vector2(layout.Origin.X / layout.Scale, -layout.Origin.Y / layout.Scale);
            if (canvasRoot.anchoredPosition != origin) canvasRoot.anchoredPosition = origin;
            for (int i = 0; i < items.Count; i++) {
                if (i == slots.Count) slots.Add(NewSlot(i));
                Apply(slots[i], items[i]);
            }
            for (int i = items.Count; i < usedSlots; i++) Show(slots[i], null);
            usedSlots = items.Count;
        }

        private void CreateCanvas()
        {
            var root = new GameObject("HUD canvas");
            canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.pixelPerfect = false;
            var space = new GameObject("HUD reference space", typeof(RectTransform));
            canvasRoot = (RectTransform)space.transform;
            canvasRoot.SetParent(root.transform, false);
            canvasRoot.anchorMin = canvasRoot.anchorMax = canvasRoot.pivot = new Vector2(0, 1);
            canvasRoot.sizeDelta = Vector2.zero;
        }

        private Slot NewSlot(int index)
        {
            var go = new GameObject("HUD " + index, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(canvasRoot, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            return new Slot { Rect = rect };
        }

        private static T Child<T>(RectTransform parent, string name) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0, 1);
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            T graphic = go.AddComponent<T>();
            graphic.raycastTarget = false;
            return graphic;
        }

        private void Apply(Slot slot, Item item)
        {
            Rect area = item.Kind == Kind.Texture ? FitInside(item.Rect, item.Texture) : item.Rect;
            if (area != slot.Area) {
                slot.Rect.anchoredPosition = new Vector2(area.x, -area.y);
                slot.Rect.sizeDelta = new Vector2(area.width, area.height);
                slot.Area = area;
            }
            switch (item.Kind) {
                case Kind.Fill: ShowImage(slot, null, item.Color); break;
                case Kind.Skin: {
                    Sprite sprite = SkinSprite(item.Skin);
                    // IMGUI draws nothing for a missing skin texture; an Image without a sprite would draw white.
                    if (sprite == null) Show(slot, null); else ShowImage(slot, sprite, SkinTint(item));
                    break;
                }
                case Kind.Texture: ShowRaw(slot, item.Texture, new Rect(0, 0, 1, 1), item.Color); break;
                case Kind.Crop: ShowRaw(slot, item.Texture, item.Uv, item.Color); break;
                case Kind.Text: ShowText(slot, item); break;
            }
        }

        // IMGUI ScaleMode.ScaleToFit: the largest centred rectangle with the texture's aspect ratio.
        private static Rect FitInside(Rect r, Texture texture)
        {
            if (texture == null || r.width <= 0 || r.height <= 0 || texture.height <= 0) return r;
            float aspect = (float)texture.width / texture.height;
            if (r.width / r.height > aspect) { float width = r.height * aspect; return new Rect(r.x + (r.width - width) / 2, r.y, width, r.height); }
            float height = r.width / aspect;
            return new Rect(r.x, r.y + (r.height - height) / 2, r.width, height);
        }

        private Sprite SkinSprite(string name)
        {
            Sprite sprite;
            if (skinSprites.TryGetValue(name, out sprite)) return sprite;
            Texture2D texture = SkinTexture(name);
            if (texture != null) {
                // 100 pixels per unit matches the canvas reference, so a border pixel is one reference unit as in IMGUI.
                float border = Mathf.Min(SkinBorder(name), Mathf.Min(texture.width, texture.height) / 2f);
                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(.5f, .5f), 100, 0,
                    SpriteMeshType.FullRect, new Vector4(border, border, border, border));
                sprite.name = name;
            }
            skinSprites[name] = sprite;
            return sprite;
        }

        // Enables exactly one graphic of the slot (or none) without destroying the others.
        private static void Show(Slot slot, Graphic visible)
        {
            Enable(slot.Image, slot.Image == visible);
            Enable(slot.Raw, slot.Raw == visible);
            Enable(slot.Text, slot.Text == visible);
            if (visible != slot.Text) Enable(slot.Shadow, false);
        }
        private static void Enable(Graphic graphic, bool on) { if (graphic != null && graphic.enabled != on) graphic.enabled = on; }

        private static void ShowImage(Slot slot, Sprite sprite, Color color)
        {
            if (slot.Image == null) slot.Image = Child<Image>(slot.Rect, "image");
            Image image = slot.Image;
            if (image.sprite != sprite) {
                image.sprite = sprite;
                image.type = sprite != null ? Image.Type.Sliced : Image.Type.Simple;
            }
            if (image.color != color) image.color = color;
            Show(slot, image);
        }

        private static void ShowRaw(Slot slot, Texture texture, Rect uv, Color color)
        {
            if (slot.Raw == null) slot.Raw = Child<RawImage>(slot.Rect, "texture");
            RawImage raw = slot.Raw;
            if (raw.texture != texture) raw.texture = texture;
            if (raw.uvRect != uv) raw.uvRect = uv;
            if (raw.color != color) raw.color = color;
            Show(slot, raw);
        }

        private void ShowText(Slot slot, Item item)
        {
            if (slot.Text == null) {
                // The shadow is created first so it stays behind the label.
                slot.Shadow = NewText(slot.Rect, "shadow");
                RectTransform shadow = slot.Shadow.rectTransform;
                shadow.offsetMin = shadow.offsetMax = new Vector2(1.5f, -2);
                slot.Text = NewText(slot.Rect, "text");
            }
            Font font = TextFont(item.Weight);
            SetText(slot.Text, font, item, item.Color);
            if (item.Shadow) SetText(slot.Shadow, font, item, TextShadow);
            Show(slot, slot.Text);
            Enable(slot.Shadow, item.Shadow);
        }

        private static Text NewText(RectTransform parent, string name)
        {
            Text text = Child<Text>(parent, name);
            // Matches the IMGUI label: word wrap, overflow instead of clipping, no rich text.
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.supportRichText = false;
            text.resizeTextForBestFit = false;
            return text;
        }

        private static void SetText(Text text, Font font, Item item, Color color)
        {
            if (text.font != font) text.font = font;
            if (text.fontSize != item.Size) text.fontSize = item.Size;
            if (text.alignment != item.Align) text.alignment = item.Align;
            if (text.color != color) text.color = color;
            string value = item.Text ?? "";
            if (text.text != value) text.text = value;
        }

        private void DisposeCanvas()
        {
            foreach (Sprite sprite in skinSprites.Values) if (sprite != null) Object.Destroy(sprite);
            skinSprites.Clear();
            slots.Clear(); usedSlots = 0;
            if (canvas != null) Object.Destroy(canvas.gameObject);
            canvas = null; canvasRoot = null;
        }
    }
}
