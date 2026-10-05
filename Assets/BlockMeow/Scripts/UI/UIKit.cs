using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockMeow
{
    /// <summary>
    /// Code-first uGUI toolkit used to build every screen and popup, drawn as a graph-paper notebook:
    /// paper cards with ballpoint outlines, highlighter fills, ink text.
    /// </summary>
    public static class UIKit
    {
        public static Font Font { get; private set; }

        public static void Init()
        {
            if (Font != null) return;
            Font = Font.CreateDynamicFontFromOSFont(new[]
            {
                "Malgun Gothic", "맑은 고딕", "Apple SD Gothic Neo", "AppleSDGothicNeo-Bold", "Noto Sans CJK KR", "Noto Sans KR",
                "NotoSansCJK-Regular", "SamsungKorean", "NanumGothic", "Droid Sans Fallback", "Roboto", "Arial"
            }, 44);
            if (Font == null) Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = 5 };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        public static RectTransform Fill(this RectTransform rt, float l = 0, float t = 0, float r = 0, float b = 0)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
            return rt;
        }

        /// <summary>Anchor and pivot at the same normalized point; position/size in canvas units.</summary>
        public static RectTransform At(this RectTransform rt, float ax, float ay, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(ax, ay);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        public static RectTransform Top(this RectTransform rt, float y, float h, float l = 0, float r = 0)
        {
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1); rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = new Vector2(l, -y - h); rt.offsetMax = new Vector2(-r, -y);
            return rt;
        }

        public static RectTransform Bottom(this RectTransform rt, float y, float h, float l = 0, float r = 0)
        {
            rt.anchorMin = new Vector2(0, 0); rt.anchorMax = new Vector2(1, 0); rt.pivot = new Vector2(0.5f, 0);
            rt.offsetMin = new Vector2(l, y); rt.offsetMax = new Vector2(-r, y + h);
            return rt;
        }

        public static Image Img(Transform parent, string sprite, Color color, string name = "Img", bool raycast = false)
        {
            var rt = Rect(name, parent);
            var img = rt.gameObject.AddComponent<Image>();
            if (!string.IsNullOrEmpty(sprite))
            {
                img.sprite = Atlas.Get(sprite);
                if (img.sprite != null && img.sprite.border.sqrMagnitude > 0f) img.type = Image.Type.Sliced;
            }
            img.color = color;
            img.raycastTarget = raycast;
            return img;
        }

        public static Image Icon(Transform parent, string sprite, float size, Color? tint = null)
        {
            var img = Img(parent, sprite, tint ?? Color.white, sprite);
            img.preserveAspect = true;
            img.rectTransform.sizeDelta = new Vector2(size, size);
            return img;
        }

        /// <summary>Paper card (or highlighter-colored box) with a hand-drawn ink outline.</summary>
        public static Image Panel(Transform parent, Color color, float radius = 1f, string name = "Panel")
        {
            var img = Img(parent, "ui_round", color, name, true);
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.05f, radius);
            Outline(img);
            return img;
        }

        /// <summary>Adds the ballpoint outline as a child under the content (it never takes clicks).</summary>
        public static Image Outline(Image host, bool pill = false, float alpha = 1f)
        {
            var line = Img(host.transform, pill ? "ui_note_pill" : "ui_note_line", Pal.Ink.WithA(alpha), "Line");
            line.pixelsPerUnitMultiplier = host.pixelsPerUnitMultiplier;
            line.rectTransform.Fill();
            line.transform.SetAsFirstSibling();
            return line;
        }

        /// <summary>Hand-drawn ring around a round image.</summary>
        public static Image Ring(Transform host, float alpha = 1f, float inset = 0f)
        {
            var ring = Img(host, "ui_note_ring", Pal.Ink.WithA(alpha), "Ring");
            ring.rectTransform.Fill(inset, inset, inset, inset);
            return ring;
        }

        /// <summary>A yellow sticky note (rotated a little), optionally held by a strip of tape.</summary>
        public static Image Sticky(Transform parent, Color? color = null, float tilt = -3f, bool tape = true, string name = "Sticky")
        {
            var note = Img(parent, "ui_round_sm", color ?? Pal.Sticky, name);
            note.pixelsPerUnitMultiplier = 2.5f;
            note.rectTransform.localRotation = Quaternion.Euler(0, 0, tilt);
            if (tape)
            {
                var t = Img(note.transform, "ui_tape", new Color(0.86f, 0.88f, 0.92f, 0.9f), "Tape");
                t.rectTransform.At(0.5f, 1f, 0, 14, 74, 26);
                t.rectTransform.localRotation = Quaternion.Euler(0, 0, 4f - tilt);
            }
            return note;
        }

        /// <summary>A highlighter swipe (for marking a word or a number).</summary>
        public static Image Swipe(Transform parent, Color color, string name = "Swipe")
        {
            var s = Img(parent, "ui_hl_swipe", color, name);
            s.transform.SetAsFirstSibling();
            return s;
        }

        static bool IsHighlighter(Color c) => c.a > 0.6f && Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b)) > 0.18f;

        public static Text Txt(Transform parent, string text, int size, Color color, TextAnchor align = TextAnchor.MiddleCenter,
            bool outline = true, FontStyle style = FontStyle.Bold, string name = "Text")
        {
            var rt = Rect(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = Pal.ForText(color);
            t.alignment = align;
            t.fontStyle = style;
            t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.supportRichText = true;
            // ink on paper needs no outline; the parameter is kept for call-site compatibility
            return t;
        }

        public sealed class Btn
        {
            public Button Button;
            public Image Bg;
            public Text Label;
            public Image Icon;
            public RectTransform Rt;

            public void SetEnabled(bool on)
            {
                Button.interactable = on;
                var c = Bg.color; c.a = on ? 1f : 0.45f; Bg.color = c;
                if (Label) { var lc = Label.color; lc.a = on ? 1f : 0.6f; Label.color = lc; }
            }
        }

        /// <summary>Button drawn in pen with a highlighter fill that strays a little outside the line; squish feedback and click sound.</summary>
        public static Btn Button(Transform parent, string label, Color bg, Action onClick, int fontSize = 52,
            string icon = null, Color? textColor = null, float radius = 1.6f)
        {
            var img = Img(parent, "ui_round", new Color(0, 0, 0, 0), "Btn_" + label, true);
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.05f, radius);
            var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            img.gameObject.AddComponent<Squish>();
            var face = Img(img.transform, "ui_note_fill", IsHighlighter(bg) ? bg : Pal.Panel, "Face");
            face.pixelsPerUnitMultiplier = img.pixelsPerUnitMultiplier;
            face.rectTransform.Fill(-4, 3, 6, -5);
            var line = Img(img.transform, "ui_note_line", Pal.Ink, "Line");
            line.pixelsPerUnitMultiplier = img.pixelsPerUnitMultiplier;
            line.rectTransform.Fill();

            var view = new Btn { Button = b, Bg = face, Rt = img.rectTransform };
            var row = Rect("Content", img.transform).Fill(18, 6, 18, 6);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.spacing = 16;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            if (!string.IsNullOrEmpty(icon))
            {
                view.Icon = Img(row, icon, Color.white, "Icon");
                view.Icon.preserveAspect = true;
                LE(view.Icon, fontSize * 1.3f, fontSize * 1.3f);
            }
            if (!string.IsNullOrEmpty(label))
            {
                var tc = textColor ?? Color.white;
                view.Label = Txt(row, label, fontSize, tc, TextAnchor.MiddleCenter, tc != Pal.TextDark);
                view.Label.horizontalOverflow = HorizontalWrapMode.Overflow;
                LE(view.Label, fontSize * 1.25f);
            }
            b.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); onClick?.Invoke(); });
            return view;
        }

        /// <summary>Round icon button (no label): paper or highlighter disc inside a pen ring.</summary>
        public static Button Round(Transform parent, string icon, Color bg, Action onClick, float iconScale = 0.6f, Color? iconTint = null)
        {
            var img = Img(parent, "ui_circle", IsHighlighter(bg) ? bg : Pal.Panel, "Round_" + icon, true);
            var b = img.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            img.gameObject.AddComponent<Squish>();
            Ring(img.transform);
            var ic = Img(img.transform, icon, iconTint ?? Color.white, "Icon");
            ic.preserveAspect = true;
            ic.rectTransform.anchorMin = new Vector2(0.5f - iconScale / 2f, 0.5f - iconScale / 2f);
            ic.rectTransform.anchorMax = new Vector2(0.5f + iconScale / 2f, 0.5f + iconScale / 2f);
            ic.rectTransform.offsetMin = ic.rectTransform.offsetMax = Vector2.zero;
            b.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); onClick?.Invoke(); });
            return b;
        }

        public static Image Bar(Transform parent, Color back, Color fill, out Image fillImg)
        {
            var bg = Img(parent, "ui_pill", Pal.Panel2, "Bar");
            fillImg = Img(bg.transform, "ui_pill", fill, "Fill");
            fillImg.rectTransform.anchorMin = Vector2.zero;
            fillImg.rectTransform.anchorMax = Vector2.one;
            fillImg.rectTransform.offsetMin = new Vector2(5, 5);
            fillImg.rectTransform.offsetMax = new Vector2(-5, -5);
            var line = Img(bg.transform, "ui_note_pill", Pal.Ink, "Line");
            line.rectTransform.Fill();
            return bg;
        }

        public static void SetBar(Image fill, float frac)
        {
            fill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(frac), 1f);
            fill.enabled = frac > 0.01f;
        }

        public static Image Dot(Transform parent, float size = 44f)
        {
            var d = Img(parent, "ui_reddot", Color.white, "RedDot");
            d.rectTransform.At(1, 1, 6, 6, size, size);
            return d;
        }

        public static LayoutElement LE(Component c, float h = -1, float w = -1, float flexW = -1, float flexH = -1)
        {
            var le = c.GetComponent<LayoutElement>();
            if (le == null) le = c.gameObject.AddComponent<LayoutElement>();
            if (h >= 0) { le.preferredHeight = h; le.minHeight = h; }
            if (w >= 0) { le.preferredWidth = w; le.minWidth = w; }
            if (flexW >= 0) le.flexibleWidth = flexW;
            if (flexH >= 0) le.flexibleHeight = flexH;
            return le;
        }

        public static HorizontalLayoutGroup Row(Transform parent, float spacing = 16, TextAnchor align = TextAnchor.MiddleCenter, string name = "Row")
        {
            var h = Rect(name, parent).gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = spacing;
            h.childAlignment = align;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = h.childForceExpandHeight = false;
            return h;
        }

        public static VerticalLayoutGroup Col(Transform parent, float spacing = 16, TextAnchor align = TextAnchor.UpperCenter, string name = "Col")
        {
            var v = Rect(name, parent).gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.childAlignment = align;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true;
            v.childForceExpandHeight = false;
            return v;
        }

        public static ScrollRect Scroll(Transform parent, out RectTransform content, float spacing = 24, int pad = 30)
        {
            var root = Rect("Scroll", parent);
            var sr = root.gameObject.AddComponent<ScrollRect>();
            var vp = Rect("Viewport", root).Fill();
            vp.gameObject.AddComponent<RectMask2D>();
            var hit = vp.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            content = Rect("Content", vp);
            content.anchorMin = new Vector2(0, 1); content.anchorMax = new Vector2(1, 1); content.pivot = new Vector2(0.5f, 1);
            content.offsetMin = content.offsetMax = Vector2.zero;
            var v = content.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = spacing;
            v.padding = new RectOffset(pad, pad, pad, pad + 60);
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.viewport = vp; sr.content = content;
            sr.horizontal = false; sr.vertical = true;
            sr.scrollSensitivity = 40f;
            sr.movementType = ScrollRect.MovementType.Elastic;
            return sr;
        }

        /// <summary>Coin chip "(coin) 1,234" used in headers.</summary>
        public static Text CoinChip(Transform parent, out RectTransform rt, Action onPlus = null)
        {
            var bg = Img(parent, "ui_pill", Pal.Panel, "CoinChip", onPlus != null);
            Outline(bg, true);
            rt = bg.rectTransform;
            var ic = Img(bg.transform, "ic_coin", Color.white, "Coin");
            ic.rectTransform.At(0, 0.5f, 4, 0, 84, 84);
            var t = Txt(bg.transform, "0", 44, Color.white, TextAnchor.MiddleLeft);
            t.rectTransform.Fill(96, 0, onPlus != null ? 70 : 20, 0);
            if (onPlus != null)
            {
                var plus = Img(bg.transform, "ui_circle", Pal.Green, "Plus");
                plus.rectTransform.At(1, 0.5f, -8, 0, 58, 58);
                Ring(plus.transform);
                var pi = Img(plus.transform, "ic_close", Color.white, "PlusIcon");
                pi.rectTransform.Fill(14, 14, 14, 14);
                pi.rectTransform.localRotation = Quaternion.Euler(0, 0, 45);
                var b = bg.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                bg.gameObject.AddComponent<Squish>();
                b.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); onPlus(); });
            }
            return t;
        }

        public static string N(long v) => v.ToString("N0");
    }

    /// <summary>Press feedback: shrink on press, spring back on release.</summary>
    public sealed class Squish : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        bool _down;

        public void OnPointerDown(PointerEventData e)
        {
            var b = GetComponent<Button>();
            if (b != null && !b.interactable) return;
            _down = true;
            Tweener.Kill(transform);
            Tweener.Scale(transform, Vector3.one * 0.92f, 0.08f, Ease.OutQuad);
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) { if (_down) Release(); }

        void Release()
        {
            _down = false;
            Tweener.Kill(transform);
            Tweener.Scale(transform, Vector3.one, 0.25f, Ease.OutBack);
        }
    }
}
