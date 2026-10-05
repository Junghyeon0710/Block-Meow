using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockMeow
{
    /// <summary>Forwards raw pointer events on the play area to the game session (UI buttons sit above it).</summary>
    public sealed class GameInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        int _pointer = int.MinValue;

        public void OnPointerDown(PointerEventData e)
        {
            if (_pointer != int.MinValue) return;
            _pointer = e.pointerId;
            GameSession.I?.PointerDown(e.position);
        }

        public void OnDrag(PointerEventData e)
        {
            if (e.pointerId == _pointer) GameSession.I?.PointerDrag(e.position);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (e.pointerId != _pointer) return;
            _pointer = int.MinValue;
            GameSession.I?.PointerUp(e.position);
        }

        void OnDisable() => _pointer = int.MinValue;
    }

    /// <summary>In-game HUD: score/goals, partner cat + skill gauge, combo &amp; fever, boosters, praise texts and the tutorial hand.</summary>
    public sealed class GameScreen : UIScreen
    {
        public static GameScreen I { get; private set; }
        public static float TopPx = 400f, BottomPx = 300f;

        const float TopUnits = 420f, BoosterUnits = 210f;

        Text _score, _best, _title, _combo, _hint;
        Image _scoreSwipe;
        RectTransform _scoreRt, _comboRt, _hintRt, _goalsRow, _bestRow, _boosterRow, _fever;
        CatView _cat;
        Image _hand, _crown;
        // partner cat: gauge ring, skill badge, count badge, stuck bar
        RectTransform _partnerRt, _countRt, _stuckRt;
        Image _gaugeFill, _readyGlow, _skillIcon, _countBg;
        Text _countText, _stuckText;
        float _t;
        readonly Text[] _goalText = new Text[4];
        readonly RectTransform[] _goalRt = new RectTransform[4];
        readonly Text[] _boosterCount = new Text[3];
        readonly Image[] _boosterAd = new Image[3];
        readonly Image[] _boosterBg = new Image[3];
        int _shownScore;
        Tween _handTween;

        public override void Build()
        {
            I = this;
            var input = UIKit.Rect("Input", Rt).Fill();
            var hit = input.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            input.gameObject.AddComponent<GameInput>();

            var pause = UIKit.Round(Rt, "ic_pause", Pal.Panel3.WithA(0.9f), () => UIRoot.I.Open<PausePopup>(), 0.45f);
            ((RectTransform)pause.transform).At(0f, 1f, 30, -30, 110, 110);

            BuildPartner();

            // classic: best + score
            _bestRow = UIKit.Rect("Best", Rt);
            _bestRow.At(0.5f, 1f, 0, -40, 500, 60);
            _crown = UIKit.Img(_bestRow, "ic_crown", Color.white, "Crown");
            _crown.preserveAspect = true;
            _crown.rectTransform.At(0f, 0.5f, 60, 0, 64, 64);
            _best = UIKit.Txt(_bestRow, "0", 44, Pal.Yellow, TextAnchor.MiddleLeft);
            _best.rectTransform.Fill(140, 0, 0, 0);

            _title = UIKit.Txt(Rt, "", 50, Color.white);
            _title.rectTransform.At(0.5f, 1f, 0, -40, 600, 70);

            _scoreSwipe = UIKit.Img(Rt, "ui_hl_swipe", Pal.Yellow.WithA(0.85f), "ScoreSwipe");
            _scoreSwipe.rectTransform.At(0.5f, 1f, 0, -176, 440, 44);
            _scoreSwipe.rectTransform.localRotation = Quaternion.Euler(0, 0, -1.2f);
            _score = UIKit.Txt(Rt, "0", 116, Color.white);
            _scoreRt = _score.rectTransform;
            _scoreRt.At(0.5f, 1f, 0, -90, 640, 140);

            // adventure / daily goals
            _goalsRow = UIKit.Rect("Goals", Rt);
            _goalsRow.At(0.5f, 1f, 0, -140, 600, 130);
            var gh = _goalsRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            gh.childAlignment = TextAnchor.MiddleCenter;
            gh.spacing = 24;
            gh.childControlWidth = gh.childControlHeight = false;
            for (int k = 1; k <= 3; k++)
            {
                var chip = UIKit.Panel(_goalsRow, Pal.Panel, 1.25f, "Goal" + k);
                chip.pixelsPerUnitMultiplier = 0.8f;
                chip.rectTransform.sizeDelta = new Vector2(170, 120);
                var gem = UIKit.Img(chip.transform, "gem_" + k, Color.white, "Gem");
                gem.rectTransform.At(0f, 0.5f, 4, 0, 100, 100);
                _goalText[k] = UIKit.Txt(chip.transform, "0", 50, Color.white, TextAnchor.MiddleLeft);
                _goalText[k].rectTransform.Fill(100, 0, 6, 0);
                _goalRt[k] = chip.rectTransform;
            }

            // combo badge
            var combo = UIKit.Sticky(Rt, null, -5f, true, "Combo");
            _comboRt = combo.rectTransform;
            _comboRt.At(0f, 1f, 150, -250, 230, 76);
            _combo = UIKit.Txt(combo.transform, "", 40, Color.white);
            _combo.rectTransform.Fill();
            combo.gameObject.SetActive(false);

            var fever = UIKit.Img(Rt, "ui_pill", Pal.Pink, "Fever");
            UIKit.Outline(fever, true);
            _fever = fever.rectTransform;
            _fever.At(0.5f, 1f, 0, -250, 280, 70);
            var ft = UIKit.Txt(fever.transform, "피버 x2", 44, Color.white);
            ft.rectTransform.Fill();
            fever.gameObject.SetActive(false);

            // hint
            var hint = UIKit.Sticky(Rt, Pal.Hex("#FFF6B8"), -1f, false, "Hint");
            _hintRt = hint.rectTransform;
            _hintRt.At(0.5f, 1f, 0, -338, 900, 80);
            _hint = UIKit.Txt(hint.transform, "", 38, Color.white);
            _hint.rectTransform.Fill(24, 0, 24, 0);
            hint.gameObject.SetActive(false);

            // stuck bar: no piece fits, but the cat or a booster can still help
            var stuck = UIKit.Sticky(Rt, Pal.Pink, 1f, true, "Stuck");
            stuck.raycastTarget = true;
            _stuckRt = stuck.rectTransform;
            _stuckRt.At(0.5f, 1f, 0, -298, 1000, 104);
            _stuckText = UIKit.Txt(_stuckRt, "", 34, Color.white, TextAnchor.MiddleLeft);
            _stuckText.rectTransform.Fill(44, 0, 250, 0);
            var giveUp = UIKit.Button(_stuckRt, "포기하기", Pal.Panel3, () => GameSession.I.GiveUp(), 34);
            giveUp.Rt.At(1f, 0.5f, -14, 2, 220, 76);
            stuck.gameObject.SetActive(false);

            // boosters
            _boosterRow = UIKit.Rect("Boosters", Rt);
            _boosterRow.Bottom(UIRoot.BannerH + 30, 170, 120, 120);
            var bh = _boosterRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            bh.childAlignment = TextAnchor.MiddleCenter;
            bh.spacing = 80;
            bh.childControlWidth = bh.childControlHeight = false;
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                var btn = UIKit.Round(_boosterRow, Profile.BoosterIcon[i], Pal.Panel3, () => GameSession.I.UseBooster((Booster)k), 0.62f);
                var brt = (RectTransform)btn.transform;
                brt.sizeDelta = new Vector2(150, 150);
                _boosterBg[i] = btn.GetComponent<Image>();
                var badge = UIKit.Img(brt, "ui_circle", Pal.Pink, "Badge");
                badge.rectTransform.At(1f, 1f, 8, 8, 64, 64);
                UIKit.Ring(badge.transform);
                _boosterCount[i] = UIKit.Txt(badge.transform, "0", 36, Color.white);
                _boosterCount[i].rectTransform.Fill();
                _boosterAd[i] = UIKit.Img(brt, "ui_circle", Pal.Green, "Ad");
                _boosterAd[i].rectTransform.At(1f, 1f, 8, 8, 64, 64);
                UIKit.Ring(_boosterAd[i].transform);
                var adIc = UIKit.Img(_boosterAd[i].transform, "ic_ad", Color.white, "AdIcon");
                adIc.rectTransform.Fill(12, 12, 12, 12);
            }

            _hand = UIKit.Img(UIRoot.I.TopLayer, "hand", Color.white, "TutorialHand");
            _hand.rectTransform.sizeDelta = new Vector2(170, 170);
            _hand.rectTransform.pivot = new Vector2(0.45f, 0.9f);
            _hand.gameObject.SetActive(false);
        }

        void BuildPartner()
        {
            _partnerRt = UIKit.Rect("Partner", Rt);
            _partnerRt.At(1f, 1f, -20, -20, 240, 240);
            _readyGlow = UIKit.Img(_partnerRt, "ui_radial", Pal.Yellow.WithA(0f), "ReadyGlow");
            _readyGlow.rectTransform.Fill(-80, -80, -80, -80);
            var ringBg = UIKit.Img(_partnerRt, "ui_disc", Pal.Panel, "GaugeBg");
            ringBg.rectTransform.Fill();
            _gaugeFill = UIKit.Img(_partnerRt, "ui_disc", Pal.Cyan, "Gauge");
            _gaugeFill.type = Image.Type.Filled;
            _gaugeFill.fillMethod = Image.FillMethod.Radial360;
            _gaugeFill.fillOrigin = (int)Image.Origin360.Top;
            _gaugeFill.fillClockwise = true;
            _gaugeFill.rectTransform.Fill();
            var hole = UIKit.Img(_partnerRt, "ui_disc", Pal.Panel, "Hole");
            hole.rectTransform.Fill(15, 15, 15, 15);
            UIKit.Ring(_partnerRt);
            UIKit.Ring(hole.transform);
            _cat = CatView.Create(_partnerRt, Profile.D.mascot, 210);
            _cat.GetComponent<RectTransform>().Fill(16, 8, 16, 24);
            _cat.OnTap = () => GameSession.I.TapCat();

            var badge = UIKit.Img(_partnerRt, "ui_circle", Pal.Blue, "SkillBadge");
            badge.rectTransform.At(0f, 0f, -14, -14, 96, 96);
            UIKit.Ring(badge.transform);
            _skillIcon = UIKit.Img(badge.transform, "sk_row", Color.white, "SkillIcon");
            _skillIcon.preserveAspect = true;
            _skillIcon.rectTransform.Fill(10, 10, 10, 10);

            _countBg = UIKit.Img(_partnerRt, "ui_pill", Pal.Panel, "Count");
            UIKit.Outline(_countBg, true);
            _countRt = _countBg.rectTransform;
            _countRt.At(1f, 0f, 14, -14, 120, 58);
            _countText = UIKit.Txt(_countRt, "", 32, Color.white);
            _countText.rectTransform.Fill();
        }

        void Update()
        {
            var s = GameSession.I;
            if (s == null || _partnerRt == null) return;
            _t += Clock.Dt;
            bool ready = s.SkillReady && s.State == Phase.Playing;
            float pulse = 0.5f + 0.5f * Mathf.Sin(_t * 6f);
            _readyGlow.color = Pal.Yellow.WithA(ready ? 0.25f + 0.3f * pulse : 0f);
            if (!Tweener.IsActive(_partnerRt)) _partnerRt.localScale = Vector3.one * (ready ? 1f + 0.04f * pulse : 1f);
            if (s.Aiming) _countRt.localScale = Vector3.one * (1f + 0.08f * pulse);
            else _countRt.localScale = Vector3.one;
        }

        void RefreshSkill(GameSession s)
        {
            bool on = s.SkillsEnabled;
            _partnerRt.gameObject.SetActive(true);
            _skillIcon.transform.parent.gameObject.SetActive(on);
            _countRt.gameObject.SetActive(on);
            if (!on) { _gaugeFill.fillAmount = 0f; return; }
            bool ready = s.SkillReady;
            _gaugeFill.fillAmount = s.GaugeNeed > 0 ? s.Gauge / (float)s.GaugeNeed : 0f;
            _gaugeFill.color = ready ? Pal.Yellow : Pal.Pink;
            if (s.Aiming) { _countText.text = "조준!"; _countBg.color = Pal.Red; }
            else if (ready) { _countText.text = "탭!"; _countBg.color = Pal.Orange; }
            else if (s.Napping) { _countText.text = $"Zz {s.NapTurns}"; _countBg.color = Pal.Purple; }
            else { _countText.text = $"{s.Gauge}/{s.GaugeNeed}"; _countBg.color = Pal.Panel; }
            if (s.Stuck) _stuckText.text = StuckMessage(s);
        }

        static string StuckMessage(GameSession s)
        {
            if (s.SkillReady) return $"놓을 곳이 없어요!\n<color=#7A1F4D>{Pal.EulReul(s.Partner.Name)} 눌러 {s.SkillName}!</color>";
            if (s.SkillAdAvailable) return $"놓을 곳이 없어요!\n<color=#7A1F4D>{Pal.EulReul(s.Partner.Name)} 누르면 스킬을 바로 써요</color>";
            return "놓을 곳이 없어요!\n<color=#7A1F4D>부스터로 길을 만들어 보세요</color>";
        }

        /// <summary>The gauge just filled up.</summary>
        public void SkillReadyFx()
        {
            Refresh();
            Tweener.Kill(_partnerRt);
            _partnerRt.localScale = Vector3.one;
            Tweener.Punch(_partnerRt, 0.25f, 0.4f);
            SetMood(CatMood.Wow);
        }

        /// <summary>First full gauge ever: point at the cat so the player discovers skills.</summary>
        public void ShowSkillTip(string catName)
        {
            ShowHint($"스킬 준비! {Pal.EulReul(catName)} 눌러요");
            Vector2 cat = RectTransformUtility.WorldToScreenPoint(null, _partnerRt.TransformPoint(_partnerRt.rect.center));
            float s = UIRoot.I.Scale;
            ShowHand(cat + new Vector2(30f, -210f) * s, cat + new Vector2(10f, -60f) * s);
        }

        /// <summary>Big slide-in banner with the cat's face: "치즈의 꼬리 휩쓸기!".</summary>
        public void ShowSkillBanner(int cat, string text)
        {
            var def = Cats.Get(cat);
            var board = GameSession.I.WorldToScreen(GameSession.I.BoardCenter);
            var root = UIKit.Rect("SkillBanner", Rt);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(1000, 230);
            Vector2 to = Local(board) - Rt.rect.center + Vector2.up * 150f;
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
            Color bannerCol = def.Rarity == CatRarity.Legendary ? Pal.Orange : def.Rarity == CatRarity.Rare ? Pal.Blue : Pal.Purple;
            var bg = UIKit.Img(root, "ui_pill", Pal.Panel, "Bg");
            bg.rectTransform.Fill(0, 36, 0, 36);
            UIKit.Outline(bg, true);
            var swipe = UIKit.Img(root, "ui_hl_swipe", bannerCol, "Swipe");
            swipe.rectTransform.Fill(236, 84, 40, 84);
            var cv = CatView.Create(root, cat, 230);
            cv.Tappable = false;
            cv.GetComponent<RectTransform>().At(0f, 0.5f, 6, 6, 230, 230);
            cv.SetMood(CatMood.Wow, 99f);
            var t = UIKit.Txt(root, text, 56, Color.white, TextAnchor.MiddleLeft);
            t.rectTransform.Fill(250, 0, 24, 0);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 36;
            t.resizeTextMaxSize = 56;
            root.anchoredPosition = to + Vector2.right * 1300f;
            Tweener.Move(root, to, 0.32f, Ease.OutBack);
            Tweener.Delay(1.05f, () =>
            {
                if (!root) return;
                Tweener.Move(root, to + Vector2.left * 1300f, 0.3f, Ease.InBack);
                Tweener.Fade(cg, 0f, 0.3f).OnComplete(() => { if (root) Destroy(root.gameObject); });
            }, root);
        }

        public void SetStuck(bool on)
        {
            var s = GameSession.I;
            if (on) _stuckText.text = StuckMessage(s);
            bool show = on && !_hintRt.gameObject.activeSelf;
            bool was = _stuckRt.gameObject.activeSelf;
            _stuckRt.gameObject.SetActive(show);
            if (show && !was) Tweener.PopIn(_stuckRt, 0.3f);
            if (on) Tweener.Punch(_partnerRt, 0.15f, 0.35f);
        }

        void LateUpdate()
        {
            var ui = UIRoot.I;
            float scale = ui.Scale;
            var safe = ui.SafePx;
            TopPx = (Screen.height - safe.yMax) + TopUnits * scale;
            BottomPx = safe.yMin + (ui.BannerHeight + BoosterUnits) * scale;
        }

        public override void OnShow()
        {
            var s = GameSession.I;
            _cat.SetCat(s.PartnerCat);
            _skillIcon.sprite = Atlas.Get(CatSkills.Icon[(int)s.Partner.Skill]);
            _shownScore = s.Score;
            SetFever(false);
            _stuckRt.gameObject.SetActive(false);
            HideHint();
            HideHand();
            Refresh();
            if (s.PartnerIsTrial) Tweener.Delay(0.6f, () => UIRoot.I.Toast($"체험 중! 이번 판은 {Pal.WaGwa(s.Partner.Name)} 함께해요"), this);
        }

        public override void OnBack()
        {
            if (GameSession.I.Aiming) { GameSession.I.TapCat(); return; }
            if (GameSession.I.ActiveTool != null) { GameSession.I.CancelTool(); return; }
            UIRoot.I.Open<PausePopup>();
        }

        public void Refresh()
        {
            var s = GameSession.I;
            bool classic = s.Mode == GameMode.Classic || s.Mode == GameMode.Tutorial;
            _bestRow.gameObject.SetActive(s.Mode == GameMode.Classic);
            _title.gameObject.SetActive(!classic);
            _goalsRow.gameObject.SetActive(!classic);
            _scoreRt.gameObject.SetActive(classic);
            _scoreSwipe.gameObject.SetActive(classic);
            _boosterRow.gameObject.SetActive(s.Mode != GameMode.Tutorial);

            if (s.IsChallenge)
            {
                _crown.sprite = Atlas.Get("ic_flag");
                _best.text = $"친구 {UIKit.N(s.ChallengeTarget)}";
                _best.color = Pal.ForText(s.Score > s.ChallengeTarget ? Pal.Green : Pal.Yellow);
            }
            else
            {
                _crown.sprite = Atlas.Get("ic_crown");
                _best.text = UIKit.N(Mathf.Max(s.Best, s.Score));
                _best.color = Pal.ForText(Pal.Yellow);
            }
            if (s.Mode == GameMode.Adventure) _title.text = $"레벨 {s.Level} · {s.CurrentLevel?.Picture}";
            else if (s.Mode == GameMode.Daily) _title.text = "오늘의 도전";
            for (int k = 1; k <= 3; k++)
            {
                _goalRt[k].gameObject.SetActive(s.GoalTotals[k] > 0);
                _goalText[k].text = s.Goals[k].ToString();
                _goalText[k].color = s.Goals[k] > 0 ? Pal.Ink : Pal.PenGreen;
            }
            if (s.Score != _shownScore)
            {
                long from = _shownScore;
                _shownScore = s.Score;
                Tweener.Kill(_score);
                Tweener.Count(_score, from, s.Score, 0.35f);
                Tweener.Punch(_scoreRt, 0.08f, 0.2f);
            }
            else _score.text = UIKit.N(s.Score);

            bool comboOn = s.Combo >= 2;
            _comboRt.gameObject.SetActive(comboOn);
            if (comboOn) _combo.text = $"콤보 {s.Combo}";

            for (int i = 0; i < 3; i++)
            {
                int n = Profile.Boosters((Booster)i);
                _boosterCount[i].transform.parent.gameObject.SetActive(n > 0);
                _boosterCount[i].text = n.ToString();
                _boosterAd[i].gameObject.SetActive(n <= 0);
                bool active = s.ActiveTool == (Booster)i;
                _boosterBg[i].color = active ? Pal.Yellow : Pal.Panel;
            }
            RefreshSkill(s);
        }

        public void SetMood(CatMood m) => _cat.SetMood(m);

        public void SetFever(bool on)
        {
            bool was = _fever.gameObject.activeSelf;
            _fever.gameObject.SetActive(on);
            if (on && !was) Tweener.PopIn(_fever, 0.35f);
            _score.color = on ? Pal.PenPink : Pal.Ink;
            _scoreSwipe.color = (on ? Pal.Pink : Pal.Yellow).WithA(0.85f);
        }

        public void ShowCombo(int combo)
        {
            _comboRt.gameObject.SetActive(true);
            _combo.text = $"콤보 {combo}";
            Tweener.Kill(_comboRt);
            _comboRt.localScale = Vector3.one;
            Tweener.Punch(_comboRt, 0.25f, 0.3f);
        }

        Vector2 Local(Vector2 screen)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(Rt, screen, null, out var p);
            return p;
        }

        RectTransform Floating(string text, int size, Color color, Vector2 local)
        {
            var t = UIKit.Txt(Rt, text, size, color);
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1000, size * 1.6f);
            rt.anchoredPosition = local;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            return rt;
        }

        public void ScorePopup(int points, Vector2 screen)
        {
            var rt = Floating("+" + UIKit.N(points), 64, Pal.Yellow, Local(screen) - (Rt.rect.center));
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            Vector2 from = rt.anchoredPosition;
            Tweener.PopIn(rt, 0.25f);
            Tweener.To(0.9f, k => { if (rt) rt.anchoredPosition = from + Vector2.up * 120f * k; }, rt).SetEase(Ease.OutCubic);
            Tweener.Delay(0.55f, () => { if (cg) Tweener.Fade(cg, 0f, 0.35f).OnComplete(() => { if (rt) Destroy(rt.gameObject); }); }, rt);
        }

        public void ShowPraise(string text, Color color)
        {
            var board = GameSession.I.WorldToScreen(GameSession.I.BoardCenter);
            var rt = Floating(text, 110, color, Local(board) - Rt.rect.center + Vector2.up * 40f);
            var sh = rt.gameObject.AddComponent<Shadow>();
            sh.effectColor = Pal.Paper;
            sh.effectDistance = new Vector2(3, -4);
            var cg = rt.gameObject.AddComponent<CanvasGroup>();
            rt.localScale = Vector3.zero;
            Tweener.Scale(rt, Vector3.one, 0.35f, Ease.OutBack);
            Vector2 from = rt.anchoredPosition;
            Tweener.To(1.1f, k => { if (rt) rt.anchoredPosition = from + Vector2.up * 60f * k; }, rt).SetEase(Ease.Linear);
            Tweener.Delay(0.8f, () => { if (cg) Tweener.Fade(cg, 0f, 0.3f).OnComplete(() => { if (rt) Destroy(rt.gameObject); }); }, rt);
        }

        public void FlyGem(int kind, Vector2 screen, float delay)
        {
            if (_goalRt[kind] == null || !_goalsRow.gameObject.activeInHierarchy) return;
            var img = UIKit.Img(Rt, "gem_" + kind, Color.white, "FlyGem");
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(90, 90);
            Vector2 from = Local(screen) - Rt.rect.center;
            Vector2 to = (Vector2)Rt.InverseTransformPoint(_goalRt[kind].TransformPoint(new Vector3(-30f, 0f, 0f))) - Rt.rect.center;
            rt.anchoredPosition = from;
            rt.localScale = Vector3.zero;
            Vector2 ctrl = new Vector2((from.x + to.x) * 0.5f + Random.Range(-200f, 200f), Mathf.Max(from.y, to.y) + 200f);
            Tweener.To(0.65f, k =>
            {
                if (!rt) return;
                float u = 1f - k;
                rt.anchoredPosition = u * u * from + 2f * u * k * ctrl + k * k * to;
                rt.localScale = Vector3.one * Mathf.Lerp(1.3f, 0.8f, k);
            }, rt).SetEase(Ease.InOutQuad).SetDelay(delay).OnComplete(() =>
            {
                if (rt) Destroy(rt.gameObject);
                AudioManager.Play(Sfx.Gem, 0.7f, Random.Range(0.95f, 1.15f));
                if (_goalRt[kind]) Tweener.Punch(_goalRt[kind], 0.18f, 0.25f);
                Refresh();
            });
        }

        public void ShowHint(string text)
        {
            _hint.text = text;
            _stuckRt.gameObject.SetActive(false);
            if (!_hintRt.gameObject.activeSelf) { _hintRt.gameObject.SetActive(true); Tweener.PopIn(_hintRt, 0.3f); }
        }

        public void HideHint()
        {
            _hintRt.gameObject.SetActive(false);
            var s = GameSession.I;
            if (s != null && s.Stuck && s.State == Phase.Playing) SetStuck(true);
        }

        public void ShowHand(Vector2 fromScreen, Vector2 toScreen)
        {
            var top = UIRoot.I.TopLayer;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(top, fromScreen, null, out var a);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(top, toScreen, null, out var b);
            var rt = _hand.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            Vector2 offset = top.rect.center;
            _hand.gameObject.SetActive(true);
            _handTween?.Kill();
            _handTween = Tweener.To(1.8f, k =>
            {
                if (!rt) return;
                float m = Mathf.Clamp01((k - 0.15f) / 0.6f);
                m = m * m * (3f - 2f * m);
                rt.anchoredPosition = Vector2.Lerp(a, b, m) - offset;
                rt.localScale = Vector3.one * (k < 0.15f ? Mathf.Lerp(1.15f, 1f, k / 0.15f) : 1f);
                _hand.color = new Color(1f, 1f, 1f, k > 0.85f ? (1f - k) / 0.15f : 1f);
            }, rt).SetEase(Ease.Linear).Looping();
        }

        public void HideHand()
        {
            _handTween?.Kill();
            if (_hand) _hand.gameObject.SetActive(false);
        }

        void OnDisable() => HideHand();
    }
}
