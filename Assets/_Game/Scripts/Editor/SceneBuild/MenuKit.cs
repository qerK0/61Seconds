using SixtyOne.Text;
using SixtyOne.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SixtyOne.Editor
{
    /// <summary>
    /// Builders shared by the main menu and the end screen ("disaster control room" look): keyed labels in the
    /// menu fonts, the framed primary button, the red secondary rows, the underlined text button, corner brackets,
    /// full-screen overlays and particles. UiFactory.Theme must be set.
    /// </summary>
    public static class MenuKit
    {
        public static readonly Color OffWhite = MenuArt.Hex(0xE9E6E1);
        public static readonly Color Amber = MenuArt.Hex(0xF2A23A);
        public static readonly Color AlertRed = MenuArt.Hex(0xE5483E);
        public static readonly Color DarkRed = MenuArt.Hex(0x5A0A0A);
        public static readonly Color RowRed = MenuArt.Hex(0x3A0A0C, 0.7f);
        public static readonly Color BrightRed = MenuArt.Hex(0xFF2A2A);
        public static readonly Color HudGrey = new Color(0.82f, 0.78f, 0.76f, 0.9f);

        public enum Font { DisplayBold, DisplayMedium, Mono, MonoBold, Body }

        private static TMP_FontAsset FontOf(Font font)
        {
            UITheme t = UiFactory.Theme;
            if (t == null) return null;
            switch (font)
            {
                case Font.DisplayBold: return t.DisplayBold;
                case Font.DisplayMedium: return t.DisplayMedium;
                case Font.Mono: return t.Mono;
                case Font.MonoBold: return t.MonoBold;
                default: return t.Regular;
            }
        }

        /// <summary>A label in a menu font; key null = the text is set by code at runtime.</summary>
        public static TextMeshProUGUI Label(string name, Transform parent, Vector2 min, Vector2 max, string key, Font font, float size, Color color,
            TextAlignmentOptions align = TextAlignmentOptions.MidlineLeft, float spacing = 0f)
        {
            TextMeshProUGUI label = UiFactory.MakeLabel(name, parent, min, max, size, align, color);
            TMP_FontAsset f = FontOf(font);
            if (f != null) label.font = f;
            label.characterSpacing = spacing;
            label.textWrappingMode = TextWrappingModes.Normal;
            if (!string.IsNullOrEmpty(key)) SerializedWire.SetTextKey(label.gameObject.AddComponent<LocalizedText>(), "key", key);
            return label;
        }

        /// <summary>A stretched image (sliced when the sprite has a border).</summary>
        public static Image Image(string name, Transform parent, Vector2 min, Vector2 max, Sprite sprite, Color color, bool raycast = false)
        {
            Image img = UiFactory.MakeImage(name, parent, min, max, color, raycast);
            img.sprite = sprite;
            img.preserveAspect = false;
            if (sprite != null && sprite.border != Vector4.zero) img.type = UnityEngine.UI.Image.Type.Sliced;
            return img;
        }

        /// <summary>A tiled full-rect RawImage (repeat texture) with a uv scale.</summary>
        public static RawImage Tiled(string name, Transform parent, Vector2 min, Vector2 max, Texture texture, Color color, Vector2 tiles)
        {
            RectTransform rect = UiFactory.MakeRect(name, parent, min, max);
            RawImage raw = rect.gameObject.AddComponent<RawImage>();
            raw.texture = texture;
            raw.color = color;
            raw.uvRect = new Rect(0f, 0f, tiles.x, tiles.y);
            raw.raycastTarget = false;
            return raw;
        }

        /// <summary>
        /// The framed primary button: outer glow, red gradient panel, 2 px bright border, metal corner brackets, a
        /// shine band (masked) and the Oswald Medium label.
        /// </summary>
        public static Button Primary(string name, Transform parent, Vector2 min, Vector2 max, string key, float size, MenuArt.Set art, MenuAudio.Cues cues,
            Color? panelTint = null, Color? glowColor = null, Color? borderColor = null)
        {
            RectTransform root = UiFactory.MakeRect(name, parent, min, max);
            Image glow = Image("Glow", root, Vector2.zero, Vector2.one, art.Glow, glowColor ?? new Color(1f, 0.12f, 0.1f, 0.6f));
            glow.rectTransform.offsetMin = new Vector2(-40f, -40f);
            glow.rectTransform.offsetMax = new Vector2(40f, 40f);
            Image panel = Image("Panel", root, Vector2.zero, Vector2.one, art.RedGradient, panelTint ?? Color.white, raycast: true);
            Image("Border", root, Vector2.zero, Vector2.one, art.Frame, borderColor ?? BrightRed);
            RectTransform mask = UiFactory.MakeRect("ShineMask", root, Vector2.zero, Vector2.one);
            mask.gameObject.AddComponent<RectMask2D>();
            Image shine = Image("Shine", mask, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), art.Shine, Color.white);
            shine.rectTransform.sizeDelta = new Vector2(220f, 0f);
            Brackets(root, art.Bracket, 26f, new Color(0.8f, 0.78f, 0.75f, 1f), -6f);
            TextMeshProUGUI label = Label("Label", root, Vector2.zero, Vector2.one, key, Font.DisplayMedium, size, OffWhite, TextAlignmentOptions.Center);
            label.textWrappingMode = TextWrappingModes.NoWrap;

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            button.transition = Selectable.Transition.None;
            MenuButtonFx fx = root.gameObject.AddComponent<MenuButtonFx>();
            fx.Configure(MenuButtonFx.Style.Primary, root, label, glow, shine.rectTransform, null, null, new Color(1f, 0.93f, 0.86f, 1f), Color.white, cues.Hover, cues.Press, cues.Thud);
            return button;
        }

        /// <summary>A dark translucent red row: uppercase, letter-spaced label, an accent bar and a › that slide in when hot.</summary>
        public static Button Secondary(string name, Transform parent, Vector2 min, Vector2 max, string key, float size, MenuArt.Set art, MenuAudio.Cues cues)
        {
            RectTransform root = UiFactory.MakeRect(name, parent, min, max);
            Image bg = Image("Background", root, Vector2.zero, Vector2.one, null, RowRed, raycast: true);
            Image accent = Image("Accent", root, new Vector2(0f, 0f), new Vector2(0f, 1f), null, BrightRed);
            accent.rectTransform.pivot = new Vector2(0f, 0.5f);
            accent.rectTransform.sizeDelta = new Vector2(6f, 0f);
            Image accentGlow = Image("AccentGlow", accent.rectTransform, Vector2.zero, Vector2.one, art.Glow, new Color(1f, 0.15f, 0.1f, 0.5f));
            accentGlow.rectTransform.offsetMin = new Vector2(-16f, -16f);
            accentGlow.rectTransform.offsetMax = new Vector2(16f, 16f);
            TextMeshProUGUI label = Label("Label", root, Vector2.zero, Vector2.one, key, Font.DisplayMedium, size, new Color(0.82f, 0.72f, 0.7f, 1f), TextAlignmentOptions.Center, 6f);
            label.fontStyle = FontStyles.UpperCase;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            TextMeshProUGUI arrow = Label("Arrow", root, new Vector2(0.06f, 0f), new Vector2(0.2f, 1f), null, Font.DisplayMedium, size + 6f, BrightRed, TextAlignmentOptions.Midline);
            arrow.text = "›";

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            button.transition = Selectable.Transition.None;
            MenuButtonFx fx = root.gameObject.AddComponent<MenuButtonFx>();
            fx.Configure(MenuButtonFx.Style.Secondary, root, label, null, null, accent.rectTransform, arrow, new Color(0.82f, 0.72f, 0.7f, 1f), Color.white, cues.Hover, cues.Click, null);
            return button;
        }

        /// <summary>An amber text button with an underline that grows from the left when hot (Yeniden Dene).</summary>
        public static Button Text(string name, Transform parent, Vector2 min, Vector2 max, string key, float size, Color color, MenuAudio.Cues cues)
        {
            RectTransform root = UiFactory.MakeRect(name, parent, min, max);
            Image hit = Image("Hit", root, Vector2.zero, Vector2.one, null, new Color(0f, 0f, 0f, 0f), raycast: true);
            TextMeshProUGUI label = Label("Label", root, Vector2.zero, Vector2.one, key, Font.DisplayMedium, size, color, TextAlignmentOptions.Center, 4f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
            Image underline = Image("Underline", root, new Vector2(0.15f, 0.12f), new Vector2(0.85f, 0.12f), null, color);
            underline.rectTransform.pivot = new Vector2(0f, 0.5f);
            underline.rectTransform.sizeDelta = new Vector2(0f, 2f);

            Button button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = hit;
            button.transition = Selectable.Transition.None;
            MenuButtonFx fx = root.gameObject.AddComponent<MenuButtonFx>();
            fx.Configure(MenuButtonFx.Style.Text, root, label, null, null, underline.rectTransform, null, color, Color.Lerp(color, Color.white, 0.45f), cues.Hover, cues.Click, null);
            return button;
        }

        /// <summary>Four L brackets in the corners of a rect (inset px; negative = outside).</summary>
        public static void Brackets(Transform parent, Sprite bracket, float size, Color color, float inset)
        {
            var corners = new[] { new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f), new Vector2(1f, 0f) };
            foreach (Vector2 c in corners)
            {
                Image img = Image("Bracket", parent, c, c, bracket, color);
                img.rectTransform.pivot = new Vector2(0f, 1f);
                img.rectTransform.sizeDelta = new Vector2(size, size);
                // The sprite is a top-left L: mirror it for the other corners.
                img.rectTransform.localScale = new Vector3(c.x > 0.5f ? -1f : 1f, c.y < 0.5f ? -1f : 1f, 1f);
                img.rectTransform.anchoredPosition = new Vector2(c.x > 0.5f ? -inset : inset, c.y < 0.5f ? inset : -inset);
            }
        }

        /// <summary>Drifting ash / smoke motes over a full rect.</summary>
        public static UiParticles Particles(string name, Transform parent, Sprite dot, int count, Vector2 size, Color tint)
        {
            RectTransform rect = UiFactory.MakeRect(name, parent, Vector2.zero, Vector2.one);
            UiParticles p = rect.gameObject.AddComponent<UiParticles>();
            p.Configure(dot, count, size, new Vector2(-34f, -8f), new Vector2(-8f, 12f), tint);
            return p;
        }

        /// <summary>Scanlines (drift + faint flicker) and the vignette on top of everything but the fader.</summary>
        public static void Overlays(Transform parent, MenuArt.Set art, float scanAlpha, bool grain, float grainAlpha = 0.035f)
        {
            Image("Vignette", parent, Vector2.zero, Vector2.one, art.Vignette, Color.white);
            if (grain)
            {
                RawImage g = Tiled("Grain", parent, Vector2.zero, Vector2.one, art.Grain, new Color(1f, 1f, 1f, grainAlpha), new Vector2(7.5f, 4.2f));
                g.gameObject.AddComponent<ScreenOverlayFx>().Configure(g, ScreenOverlayFx.Mode.Grain, grainAlpha);
            }

            RawImage scan = Tiled("Scanlines", parent, Vector2.zero, Vector2.one, art.Scanlines, new Color(1f, 1f, 1f, scanAlpha), new Vector2(1f, 1080f / 4f / 1.5f));
            scan.gameObject.AddComponent<ScreenOverlayFx>().Configure(scan, ScreenOverlayFx.Mode.Scanlines, scanAlpha);
        }

        /// <summary>The topmost black fader (fade in on start, fade out before a scene change).</summary>
        public static ScreenFader Fader(Transform parent, float fadeIn)
        {
            Image black = UiFactory.MakeImage("Fader", parent, Vector2.zero, Vector2.one, Color.black);
            CanvasGroup group = UiFactory.MakeGroup(black.gameObject, true);
            ScreenFader fader = black.gameObject.AddComponent<ScreenFader>();
            fader.Configure(group, fadeIn);
            return fader;
        }

        /// <summary>Explicit Up/Down navigation through buttons (wrapping).</summary>
        public static void VerticalNavigation(params Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var nav = new Navigation
                {
                    mode = Navigation.Mode.Explicit,
                    selectOnUp = items[(i - 1 + items.Length) % items.Length],
                    selectOnDown = items[(i + 1) % items.Length]
                };
                items[i].navigation = nav;
            }
        }
    }
}
