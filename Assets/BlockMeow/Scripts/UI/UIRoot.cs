using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace BlockMeow
{
    public abstract class UIScreen : MonoBehaviour
    {
        public RectTransform Rt => (RectTransform)transform;
        public abstract void Build();
        public virtual void OnShow() { }
        public virtual void OnBack() { }
    }

    /// <summary>Canvas, safe area, screen switching, popups, toast, banner and the simulated ad provider.</summary>
    public sealed partial class UIRoot : MonoBehaviour, Ads.IAdProvider
    {
        public static UIRoot I { get; private set; }

        public Canvas Canvas { get; private set; }
        CanvasScaler _scaler;
        RectTransform _full, _safe, _screens, _bannerLayer, _popups, _top;
        Image _bg;
        GameObject _banner;
        readonly List<Popup> _stack = new List<Popup>();
        readonly Dictionary<Type, UIScreen> _screenCache = new Dictionary<Type, UIScreen>();
        UIScreen _current;
        Rect _lastSafe;
        Vector2Int _lastRes;
        GameObject _splash;
        Text _splashText;

        public const float BannerH = 150f;
        public float BannerHeight => Ads.BannerVisible ? BannerH : 0f;
        public bool HasPopup => _stack.Count > 0;
        public float Scale => Canvas != null ? Canvas.scaleFactor : 1f;
        public Rect SafePx => _lastSafe;
        public RectTransform Safe => _safe;
        public RectTransform TopLayer => _top;

        public static UIRoot Create()
        {
            var go = new GameObject("UI");
            DontDestroyOnLoad(go);
            var root = go.AddComponent<UIRoot>();
            root.BuildCanvas();
            return root;
        }

        void BuildCanvas()
        {
            I = this;
            gameObject.layer = 5;
            Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            _scaler = gameObject.AddComponent<CanvasScaler>();
            _scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            _scaler.referenceResolution = new Vector2(1080, 1920);
            _scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            gameObject.AddComponent<GraphicRaycaster>();

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                DontDestroyOnLoad(es);
                es.AddComponent<EventSystem>();
                var module = es.AddComponent<InputSystemUIInputModule>();
                module.AssignDefaultActions();
            }

            _full = UIKit.Rect("Full", transform).Fill();
            _bg = UIKit.Img(_full, null, Pal.Bg, "Background");
            _bg.rectTransform.Fill();
            _safe = UIKit.Rect("Safe", _full);
            _screens = UIKit.Rect("Screens", _safe).Fill();
            _bannerLayer = UIKit.Rect("Banner", _safe).Fill();
            _popups = UIKit.Rect("Popups", _full).Fill();
            _top = UIKit.Rect("Top", _full).Fill();
            ApplySafeArea();
            BuildSplash();
        }

        // ------------------------------------------------------------------ splash (shown before the atlas exists)

        void BuildSplash()
        {
            UIKit.Init();
            var rt = UIKit.Rect("Splash", _top).Fill();
            _splash = rt.gameObject;
            var bg = rt.gameObject.AddComponent<Image>();
            bg.color = Pal.Bg;
            var title = UIKit.Txt(rt, "블록냥", 150, Pal.Yellow);
            title.rectTransform.At(0.5f, 0.5f, 0, 160, 900, 220);
            var sub = UIKit.Txt(rt, "BLOCK MEOW", 52, Pal.Pink);
            sub.rectTransform.At(0.5f, 0.5f, 0, 30, 900, 80);
            _splashText = UIKit.Txt(rt, "냥이가 블록을 준비하는 중...", 40, Pal.TextDim, TextAnchor.MiddleCenter, false, FontStyle.Normal);
            _splashText.rectTransform.At(0.5f, 0.5f, 0, -160, 900, 80);
        }

        public void SetSplash(string msg) { if (_splashText) _splashText.text = msg; }

        public void HideSplash()
        {
            if (_splash == null) return;
            var cg = _splash.AddComponent<CanvasGroup>();
            var go = _splash;
            _splash = null;
            Tweener.Fade(cg, 0f, 0.4f).OnComplete(() => Destroy(go));
        }

        // ------------------------------------------------------------------ layout

        void ApplySafeArea()
        {
            var sa = Screen.safeArea;
            _lastSafe = sa;
            _lastRes = new Vector2Int(Screen.width, Screen.height);
            float w = Mathf.Max(1, Screen.width), h = Mathf.Max(1, Screen.height);
            _safe.anchorMin = new Vector2(sa.xMin / w, sa.yMin / h);
            _safe.anchorMax = new Vector2(sa.xMax / w, sa.yMax / h);
            _safe.offsetMin = _safe.offsetMax = Vector2.zero;
            // wide screens (tablets) match height so the 1920-unit layout always fits
            _scaler.matchWidthOrHeight = w / h > 9f / 16f ? 1f : 0f;
        }

        void Update()
        {
            if (Screen.safeArea != _lastSafe || Screen.width != _lastRes.x || Screen.height != _lastRes.y) ApplySafeArea();
            var kb = Keyboard.current;
            if (kb != null && kb.escapeKey.wasPressedThisFrame) OnBack();
        }

        void OnBack()
        {
            if (_stack.Count > 0) { _stack[_stack.Count - 1].OnBack(); return; }
            _current?.OnBack();
        }

        // ------------------------------------------------------------------ screens

        T GetScreen<T>() where T : UIScreen
        {
            if (_screenCache.TryGetValue(typeof(T), out var s)) return (T)s;
            var rt = UIKit.Rect(typeof(T).Name, _screens).Fill();
            var screen = rt.gameObject.AddComponent<T>();
            screen.Build();
            rt.gameObject.SetActive(false);
            _screenCache[typeof(T)] = screen;
            return screen;
        }

        GameObject _menuBg;

        /// <summary>Graph paper: the paper color with a tiled light-blue grid.</summary>
        void BuildMenuBackground()
        {
            var rt = UIKit.Rect("MenuBackground", _full).Fill();
            rt.SetSiblingIndex(1);
            _menuBg = rt.gameObject;
            var paper = UIKit.Img(rt, null, Pal.Paper, "Paper");
            paper.rectTransform.Fill();
            var grid = UIKit.Img(rt, "ui_graph", Color.white, "Grid");
            grid.type = Image.Type.Tiled;
            grid.pixelsPerUnitMultiplier = 1.45f;
            grid.rectTransform.Fill();
        }

        void Show(UIScreen s, bool menuBackground)
        {
            if (_menuBg == null) BuildMenuBackground();
            if (_current != null && _current != s) _current.gameObject.SetActive(false);
            _current = s;
            s.gameObject.SetActive(true);
            _menuBg.SetActive(menuBackground);
            _bg.color = menuBackground ? Pal.Bg : new Color(0, 0, 0, 0);
            s.OnShow();
        }

        public void ShowHome() { Ads.SetBanner(true); Show(GetScreen<HomeScreen>(), true); }
        public void ShowGame() { Ads.SetBanner(true); Show(GetScreen<GameScreen>(), false); }
        public void ShowAdventure() { Ads.SetBanner(true); Show(GetScreen<AdventureScreen>(), true); }
        public void ShowCats() { Ads.SetBanner(true); Show(GetScreen<CatsScreen>(), true); }
        public void ShowShop() { Ads.SetBanner(true); Show(GetScreen<ShopScreen>(), true); }

        // ------------------------------------------------------------------ popups

        public T Open<T>() where T : Popup
        {
            var rt = UIKit.Rect(typeof(T).Name, _popups).Fill();
            var p = rt.gameObject.AddComponent<T>();
            _stack.Add(p);
            p.Setup();
            return p;
        }

        internal void Closed(Popup p) => _stack.Remove(p);

        public void CloseAll()
        {
            for (int i = _stack.Count - 1; i >= 0; i--) _stack[i].Close();
        }

        // ------------------------------------------------------------------ toast & flash

        public void Toast(string msg)
        {
            var bg = UIKit.Sticky(_top, null, -1.5f, true, "Toast");
            bg.rectTransform.At(0.5f, 1f, 0, -360, 860, 110);
            var t = UIKit.Txt(bg.transform, msg, 42, Color.white);
            t.rectTransform.Fill(30, 0, 30, 0);
            var cg = bg.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            Tweener.Fade(cg, 1f, 0.2f);
            Tweener.Delay(1.8f, () => { if (cg) Tweener.Fade(cg, 0f, 0.3f).OnComplete(() => { if (bg) Destroy(bg.gameObject); }); }, bg);
        }

        // ------------------------------------------------------------------ simulated ads (replace with an SDK adapter)

        public void SetBanner(bool visible)
        {
            if (_banner == null) BuildBanner();
            _banner.SetActive(visible);
        }

        void BuildBanner()
        {
            var bar = UIKit.Img(_bannerLayer, null, Pal.Panel2, "BannerAd", true);
            bar.rectTransform.Bottom(0, BannerH);
            var rule = UIKit.Img(bar.transform, null, Pal.Ink, "Rule");
            rule.rectTransform.Top(0, 3);
            var badge = UIKit.Img(bar.transform, "ui_round_sm", Pal.Yellow, "Badge");
            badge.rectTransform.At(0, 0.5f, 30, 0, 90, 60);
            var ad = UIKit.Txt(badge.transform, "AD", 34, Pal.TextDark, TextAnchor.MiddleCenter, false);
            ad.rectTransform.Fill();
            var t = UIKit.Txt(bar.transform, "배너 광고 영역 (테스트)\n실서비스에서는 광고 SDK 배너가 표시됩니다", 30, Pal.TextDim, TextAnchor.MiddleLeft, false, FontStyle.Normal);
            t.rectTransform.Fill(150, 0, 30, 0);
            _banner = bar.gameObject;
        }

        public void ShowInterstitial(Action closed) => Open<AdOverlay>().Init(false, ok => closed?.Invoke());
        public void ShowRewarded(Action<bool> finished) => Open<AdOverlay>().Init(true, finished);
    }

    /// <summary>Base popup: dimmed backdrop, centered panel, pop-in / fade-out.</summary>
    public abstract class Popup : MonoBehaviour
    {
        protected RectTransform Panel;
        protected CanvasGroup Cg;
        protected bool BackCloses = true;
        bool _closing;

        public void Setup()
        {
            Cg = gameObject.AddComponent<CanvasGroup>();
            var dim = UIKit.Img(transform, null, Pal.Ink.WithA(0.42f), "Dim", true);
            dim.rectTransform.Fill();
            Build();
            Cg.alpha = 0f;
            Tweener.Fade(Cg, 1f, 0.18f);
            if (Panel != null) Tweener.PopIn(Panel, 0.32f);
        }

        protected abstract void Build();

        protected RectTransform MakePanel(float w, float h, string title = null, bool closeX = false, Color? accent = null)
        {
            // Panel is a plain holder so the shadow can sit under the paper (a child of the paper image would draw over it)
            Panel = UIKit.Rect("Panel", transform);
            Panel.At(0.5f, 0.5f, 0, 0, w, h);
            var shadow = UIKit.Img(Panel, "ui_shadow", Color.white.WithA(0.45f), "Shadow");
            shadow.rectTransform.Fill(-40, -40, -40, -50);
            var paper = UIKit.Panel(Panel, Pal.Panel, 1.6f, "Paper");
            paper.rectTransform.Fill();
            if (title != null)
            {
                // the title is a highlighter label taped across the top edge
                var ribbon = UIKit.Panel(Panel, accent ?? Pal.Purple, 1.4f, "Ribbon");
                ribbon.rectTransform.At(0.5f, 1f, 0, 50, Mathf.Min(w - 80, 760), 130);
                ribbon.rectTransform.localRotation = Quaternion.Euler(0, 0, -1.5f);
                ribbon.raycastTarget = false;
                var t = UIKit.Txt(ribbon.transform, title, 60, Color.white);
                t.rectTransform.Fill(20, 0, 20, 0);
                var tape = UIKit.Img(ribbon.transform, "ui_tape", new Color(0.86f, 0.88f, 0.92f, 0.85f), "Tape");
                tape.rectTransform.At(0f, 1f, 10, 16, 92, 34);
                tape.rectTransform.localRotation = Quaternion.Euler(0, 0, 12f);
            }
            if (closeX)
            {
                var x = UIKit.Round(Panel, "ic_close", Pal.Red, () => OnBack(), 0.5f);
                ((RectTransform)x.transform).At(1f, 1f, 30, 30, 100, 100);
            }
            return Panel;
        }

        public virtual void OnBack() { if (BackCloses) Close(); }

        public void Close(Action after = null)
        {
            if (_closing) return;
            _closing = true;
            UIRoot.I.Closed(this);
            Cg.interactable = false;
            Cg.blocksRaycasts = false;
            Tweener.Fade(Cg, 0f, 0.15f).OnComplete(() =>
            {
                if (this) Destroy(gameObject);
                after?.Invoke();
            });
        }
    }
}
