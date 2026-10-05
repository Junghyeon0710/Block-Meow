using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlockMeow
{
    /// <summary>Adventure map: a winding path of levels with cat rewards every five levels.</summary>
    public sealed class AdventureScreen : UIScreen
    {
        Text _coins;
        ScrollRect _scroll;
        RectTransform _content;
        UIKit.Btn _play;

        public override void Build()
        {
            Header.Build(Rt, "모험", () => UIRoot.I.ShowHome(), out _coins);
            _scroll = UIKit.Scroll(Rt, out _content, 0, 20);
            var srt = (RectTransform)_scroll.transform;
            srt.Fill(0, 160, 0, UIRoot.BannerH + 230);
            _play = UIKit.Button(Rt, "시작", Pal.Yellow, () => GameSession.I.StartAdventure(Profile.D.adventureLevel), 60, "ic_play", Pal.TextDark);
            _play.Rt.At(0.5f, 0f, 0, UIRoot.BannerH + 40, 700, 170);
            Profile.Changed += RefreshCoins;
        }

        void OnDestroy() => Profile.Changed -= RefreshCoins;
        void RefreshCoins() { if (_coins) _coins.text = UIKit.N(Profile.D.coins); }

        public override void OnShow()
        {
            RefreshCoins();
            int cur = Profile.D.adventureLevel;
            _play.Label.text = $"레벨 {cur} 시작";
            foreach (Transform c in _content) Destroy(c.gameObject);
            int last = cur + 9;
            int currentIndex = 0;
            for (int n = last; n >= 1; n--)
            {
                MakeRow(n, cur);
                if (n == cur) currentIndex = last - n;
            }
            Canvas.ForceUpdateCanvases();
            float t = last <= 1 ? 1f : 1f - currentIndex / (float)(last - 1);
            _scroll.verticalNormalizedPosition = Mathf.Clamp01(t);
        }

        void MakeRow(int n, int cur)
        {
            var row = UIKit.Rect("Level" + n, _content);
            UIKit.LE(row, 200);
            float x = Mathf.Sin(n * 0.9f) * 230f;
            bool done = n < cur, current = n == cur;

            if (n > 1)
            {
                float x2 = Mathf.Sin((n - 1) * 0.9f) * 230f;
                for (int k = 1; k <= 3; k++)
                {
                    float t = k / 4f;
                    var dot = UIKit.Img(row, "ui_circle", Pal.Pencil.WithA(n <= cur ? 0.9f : 0.4f), "PathDot");
                    dot.rectTransform.At(0.5f, 0.5f, Mathf.Lerp(x, x2, t), -200f * t, 22, 22);
                }
            }

            Color col = done ? Pal.Green : current ? Pal.Yellow : Pal.Panel;
            var node = UIKit.Img(row, "ui_circle", col, "Node", true);
            node.rectTransform.At(0.5f, 0.5f, x, 0, current ? 170 : 140, current ? 170 : 140);
            var ring = UIKit.Ring(node.transform, current ? 1f : 0.55f, current ? -6f : 0f);
            if (n <= cur)
            {
                var label = UIKit.Txt(node.transform, n.ToString(), current ? 64 : 52, current ? Pal.TextDark : Color.white, TextAnchor.MiddleCenter, !current);
                label.rectTransform.Fill();
            }
            else
            {
                var lk = UIKit.Img(node.transform, "ic_lock", Color.white.WithA(0.6f), "Lock");
                lk.rectTransform.Fill(40, 40, 40, 40);
            }
            if (current)
            {
                var pulse = node.rectTransform;
                Tweener.To(1.2f, k => { if (pulse) pulse.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(k * Mathf.PI * 2f)); }, pulse).SetEase(Ease.Linear).Looping();
                var b = node.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                b.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); GameSession.I.StartAdventure(n); });
            }
            else if (done)
            {
                var b = node.gameObject.AddComponent<Button>();
                b.transition = Selectable.Transition.None;
                node.gameObject.AddComponent<Squish>();
                b.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); GameSession.I.StartAdventure(n); });
            }

            int cat = Profile.CatForLevel(n);
            float side = x > 0 ? -1f : 1f;
            if (cat >= 0)
            {
                var badge = UIKit.Img(row, "ui_round", Pal.Panel2.WithA(0.9f), "Reward");
                badge.pixelsPerUnitMultiplier = 0.8f;
                badge.rectTransform.At(0.5f, 0.5f, x + side * 230f, 0, 170, 170);
                var cv = CatView.Create(badge.transform, cat, 150);
                cv.Tappable = false;
                cv.GetComponent<RectTransform>().At(0.5f, 0.5f, 0, 6, 150, 150);
                if (!Profile.CatUnlocked(cat)) cv.SetSilhouette(true);
            }
            else if (n <= cur)
            {
                var name = UIKit.Txt(row, Levels.Adventure(n).Picture, 34, Pal.TextDim);
                name.rectTransform.At(0.5f, 0.5f, x + side * 230f, 0, 260, 60);
            }
        }

        public override void OnBack() => UIRoot.I.ShowHome();
    }

    /// <summary>
    /// Cats: the partner card, cat boxes (daily free, rewarded ads, coins) and the collection.
    /// Tap a cat for its skill, to make it the partner, or to try it for one game.
    /// </summary>
    public sealed class CatsScreen : UIScreen
    {
        Text _coins;
        RectTransform _content;
        ScrollRect _scroll;

        public override void Build()
        {
            Header.Build(Rt, "냥이", () => UIRoot.I.ShowHome(), out _coins);
            _scroll = UIKit.Scroll(Rt, out _content, 26, 30);
            ((RectTransform)_scroll.transform).Fill(0, 160, 0, UIRoot.BannerH + 10);
            Profile.Changed += RefreshCoins;
        }

        void OnDestroy() => Profile.Changed -= RefreshCoins;
        void RefreshCoins() { if (_coins) _coins.text = UIKit.N(Profile.D.coins); }

        public override void OnShow() => Rebuild(true);

        void Rebuild(bool toTop)
        {
            RefreshCoins();
            float keep = _scroll.verticalNormalizedPosition;
            foreach (Transform c in _content) Destroy(c.gameObject);
            PartnerCard();
            BoxCard();
            var title = UIKit.Txt(_content, $"냥이 도감  <size=40><color=#7D879E>{Profile.D.cats.Count} / {Cats.All.Length}</color></size>", 52, Pal.Yellow, TextAnchor.MiddleLeft);
            UIKit.LE(title, 90);
            var grid = UIKit.Rect("Grid", _content);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(316, 400);
            g.spacing = new Vector2(16, 20);
            g.childAlignment = TextAnchor.UpperCenter;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 3;
            grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int i = 0; i < Cats.All.Length; i++) CatCard(grid, i);
            Canvas.ForceUpdateCanvases();
            _scroll.verticalNormalizedPosition = toTop ? 1f : keep;
        }

        void PartnerCard()
        {
            int i = Profile.D.mascot;
            var def = Cats.Get(i);
            int lv = Profile.CatLevel(i);
            var col = Cats.RarityColor[(int)def.Rarity];
            var card = UIKit.Panel(_content, Pal.Panel, 1f, "Partner");
            UIKit.LE(card, 440);
            var border = UIKit.Img(card.transform, "ui_round_line", col, "Border");
            border.rectTransform.Fill();
            var glow = UIKit.Img(card.transform, "ui_radial", col.WithA(0.25f), "Glow");
            glow.rectTransform.At(0f, 0.5f, -40, -10, 420, 420);
            var cv = CatView.Create(card.transform, i, 290);
            cv.GetComponent<RectTransform>().At(0f, 0.5f, 24, -16, 290, 290);
            cv.OnTap = () => UIRoot.I.Open<CatDetailPopup>().Init(i, () => Rebuild(false));
            var tag = UIKit.Img(card.transform, "ui_pill", Pal.Yellow, "Tag");
            tag.rectTransform.At(0f, 1f, 30, -22, 190, 52);
            var tt = UIKit.Txt(tag.transform, "내 파트너", 30, Pal.TextDark, TextAnchor.MiddleCenter, false);
            tt.rectTransform.Fill();
            var name = UIKit.Txt(card.transform, $"{def.Name}  <size=36><color=#7D879E>{Cats.RarityName[(int)def.Rarity]} · Lv.{lv}</color></size>", 56, Color.white, TextAnchor.MiddleLeft);
            name.rectTransform.At(0f, 1f, 340, -22, 650, 80);
            var skill = CatUI.SkillBlock(card.transform, i, lv, false);
            skill.At(0f, 1f, 330, -112, 664, 300);
        }

        void BoxCard()
        {
            var card = UIKit.Panel(_content, Pal.Panel2, 1f, "CatBox");
            UIKit.LE(card, 420);
            var border = UIKit.Img(card.transform, "ui_round_line", Pal.Pink.WithA(0.7f), "Border");
            border.rectTransform.Fill();
            var ic = UIKit.Img(card.transform, "ic_catbox", Color.white, "Box");
            ic.preserveAspect = true;
            ic.rectTransform.At(0f, 1f, 24, -20, 240, 240);
            Tweener.To(1.6f, k => { if (ic) ic.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * Mathf.PI * 2f) * 4f); }, ic.rectTransform).SetEase(Ease.Linear).Looping();
            var t = UIKit.Txt(card.transform,
                "냥이 상자\n<size=34><color=#7D879E>일반 70% · 희귀 25% · 전설 5%\n이미 있는 냥이가 나오면 레벨 업!</color></size>",
                52, Color.white, TextAnchor.UpperLeft);
            t.rectTransform.Fill(290, 34, 24, 150);

            bool free = Profile.CatBoxFreeAvailable;
            var b1 = UIKit.Button(card.transform, free ? "매일 무료" : "내일 무료", free ? Pal.Yellow : Pal.Panel3, OpenFree, 40, free ? "ic_gift" : null, free ? Pal.TextDark : (Color?)null);
            b1.Rt.At(0f, 0f, 24, 28, 312, 116);
            b1.SetEnabled(free);
            if (free) UIKit.Dot(b1.Rt, 44);
            int left = Ads.Remaining(AdPlacement.CatBox);
            var b2 = UIKit.Button(card.transform, $"무료 {left}회", Pal.Green, OpenAd, 40, "ic_ad");
            b2.Rt.At(0.5f, 0f, 0, 28, 312, 116);
            b2.SetEnabled(left > 0);
            var b3 = UIKit.Button(card.transform, UIKit.N(Profile.CatBoxCoins), Pal.Orange, OpenCoins, 40, "ic_coin", Pal.TextDark);
            b3.Rt.At(1f, 0f, -24, 28, 312, 116);
        }

        void CatCard(Transform grid, int i)
        {
            var def = Cats.Get(i);
            bool owned = Profile.CatUnlocked(i);
            bool partner = Profile.D.mascot == i;
            int lv = Profile.CatLevel(i);
            var col = Cats.RarityColor[(int)def.Rarity];
            var card = UIKit.Panel(grid, partner ? Pal.Panel3 : Pal.Panel, 1f, "Cat" + i);
            var border = UIKit.Img(card.transform, "ui_round_line", owned ? col : col.WithA(0.3f), "Border");
            border.rectTransform.Fill();
            var cv = CatView.Create(card.transform, i, 230);
            cv.GetComponent<RectTransform>().At(0.5f, 1f, 0, -16, 230, 230);
            if (!owned) cv.SetSilhouette(true);
            var name = UIKit.Txt(card.transform, def.Name, 40, owned ? Color.white : Pal.TextDim);
            name.rectTransform.At(0.5f, 0f, 0, 72, 300, 56);
            var tag = CatUI.RarityTag(card.transform, def.Rarity, 26);
            tag.rectTransform.At(0.5f, 0f, owned ? -56 : 0, 18, 104, 44);
            if (owned)
            {
                var lt = UIKit.Txt(card.transform, $"Lv.{lv}", 32, lv >= Cats.MaxLevel ? Pal.Yellow : Color.white);
                lt.rectTransform.At(0.5f, 0f, 62, 18, 110, 44);
            }
            var sk = UIKit.Img(card.transform, "ui_circle", Pal.Panel2, "Skill");
            sk.rectTransform.At(1f, 1f, -10, -10, 78, 78);
            var ski = UIKit.Img(sk.transform, CatSkills.Icon[(int)def.Skill], owned ? Color.white : Color.white.WithA(0.45f), "Icon");
            ski.preserveAspect = true;
            ski.rectTransform.Fill(9, 9, 9, 9);
            if (partner || Profile.D.trialCat == i)
            {
                var pt = UIKit.Img(card.transform, "ui_pill", partner ? Pal.Yellow : Pal.Green, "Tag");
                pt.rectTransform.At(0f, 1f, 10, -12, 120, 46);
                var ptt = UIKit.Txt(pt.transform, partner ? "파트너" : "체험", 26, Pal.TextDark, TextAnchor.MiddleCenter, false);
                ptt.rectTransform.Fill();
            }
            var b = card.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            card.gameObject.AddComponent<Squish>();
            Action open = () => { AudioManager.Play(Sfx.Click); UIRoot.I.Open<CatDetailPopup>().Init(i, () => Rebuild(false)); };
            b.onClick.AddListener(() => open());
            cv.OnTap = open;
        }

        void OpenFree()
        {
            if (!Profile.TakeFreeCatBox()) return;
            Reveal(Profile.OpenCatBox(), "free");
        }

        void OpenAd() => Ads.Rewarded(AdPlacement.CatBox, ok => { if (ok) Reveal(Profile.OpenCatBox(), "ad"); });

        void OpenCoins()
        {
            if (!Profile.SpendCoins(Profile.CatBoxCoins)) { UIRoot.I.Toast("코인이 부족해요"); return; }
            Reveal(Profile.OpenCatBox(), "coins");
        }

        void Reveal(Profile.CatBoxResult r, string source)
        {
            Profile.Save();
            Track.Log("catbox", $"{source} cat={r.Cat} new={r.IsNew} lv={r.Level}");
            int left = Ads.Remaining(AdPlacement.CatBox);
            Action again = left > 0 ? () => { Rebuild(false); OpenAd(); } : (Action)null;
            UIRoot.I.Open<CatBoxPopup>().Init(r, () => Rebuild(false), again, $"하나 더 열기 ({left}회)");
        }

        public override void OnBack() => UIRoot.I.ShowHome();
    }

    /// <summary>Shop: themes (coins or ads), boosters (coins) and free coins (ads). No real-money purchases.</summary>
    public sealed class ShopScreen : UIScreen
    {
        Text _coins;
        RectTransform _content;

        public override void Build()
        {
            Header.Build(Rt, "상점", () => UIRoot.I.ShowHome(), out _coins);
            var scroll = UIKit.Scroll(Rt, out _content, 24, 30);
            ((RectTransform)scroll.transform).Fill(0, 160, 0, UIRoot.BannerH + 10);
            Profile.Changed += RefreshCoins;
        }

        void OnDestroy() => Profile.Changed -= RefreshCoins;
        void RefreshCoins() { if (_coins) _coins.text = UIKit.N(Profile.D.coins); }

        public override void OnShow()
        {
            RefreshCoins();
            foreach (Transform c in _content) Destroy(c.gameObject);

            // free coins
            var free = UIKit.Panel(_content, Pal.Panel, 1f, "FreeCoins");
            UIKit.LE(free, 200);
            var fi = UIKit.Img(free.transform, "ic_coin", Color.white, "Icon");
            fi.rectTransform.At(0f, 0.5f, 30, 0, 140, 140);
            int left = Ads.Remaining(AdPlacement.FreeCoins);
            var ft = UIKit.Txt(free.transform, $"무료 코인 100개\n<size=32>오늘 {left}회 남음</size>", 46, Color.white, TextAnchor.MiddleLeft);
            ft.rectTransform.Fill(200, 0, 330, 0);
            var fb = UIKit.Button(free.transform, "받기", Pal.Green, () =>
                Ads.Rewarded(AdPlacement.FreeCoins, ok => { if (ok) { Profile.AddCoins(100); AudioManager.Play(Sfx.Coin); UIRoot.I.Toast("코인 100개 획득!"); OnShow(); } }), 44, "ic_ad");
            fb.Rt.At(1f, 0.5f, -30, 0, 280, 120);
            fb.SetEnabled(left > 0);

            Section("블록 테마");
            var grid = UIKit.Rect("Themes", _content);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(480, 420);
            g.spacing = new Vector2(30, 30);
            g.childAlignment = TextAnchor.UpperCenter;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 2;
            grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            for (int i = 0; i < Themes.All.Length; i++) ThemeCard(grid, i);

            Section("부스터");
            for (int i = 0; i < 3; i++) BoosterRow(i);
        }

        void Section(string title)
        {
            var t = UIKit.Txt(_content, title, 52, Pal.Yellow, TextAnchor.MiddleLeft);
            UIKit.LE(t, 90);
        }

        void ThemeCard(Transform parent, int i)
        {
            var th = Themes.All[i];
            bool owned = Profile.ThemeOwned(i), using_ = Profile.D.theme == i;
            var card = UIKit.Panel(parent, Pal.Panel, 1f, "Theme" + i);
            if (using_)
            {
                var border = UIKit.Img(card.transform, "ui_round_line", Pal.Yellow, "Border");
                border.rectTransform.Fill();
            }
            var name = UIKit.Txt(card.transform, th.Name, 48, Color.white);
            name.rectTransform.At(0.5f, 1f, 0, -16, 440, 70);
            var preview = UIKit.Panel(card.transform, th.Board, 0.6f, "Preview");
            preview.raycastTarget = false;
            preview.rectTransform.At(0.5f, 1f, 0, -96, 420, 150);
            for (int k = 0; k < 4; k++)
            {
                var blk = UIKit.Img(preview.transform, th.Block, th.Colors[(k * 2 + i) % 8], "Block");
                blk.rectTransform.At(0.5f, 0.5f, -150 + k * 100, 0, 100, 100);
                var hi = UIKit.Img(blk.transform, th.Overlay, Color.white, "Hi");
                hi.rectTransform.Fill();
            }
            if (using_)
            {
                var t = UIKit.Txt(card.transform, "사용 중", 44, Pal.Yellow);
                t.rectTransform.At(0.5f, 0f, 0, 50, 440, 90);
            }
            else if (owned)
            {
                var b = UIKit.Button(card.transform, "적용", Pal.Blue, () => { Profile.SetTheme(i); AudioManager.Play(Sfx.Reward); OnShow(); }, 44);
                b.Rt.At(0.5f, 0f, 0, 40, 400, 110);
            }
            else
            {
                var buy = UIKit.Button(card.transform, UIKit.N(th.CoinPrice), Pal.Yellow, () =>
                {
                    if (Profile.BuyTheme(i)) { Profile.SetTheme(i); AudioManager.Play(Sfx.Unlock); UIRoot.I.Toast($"'{th.Name}' 테마를 얻었어요!"); OnShow(); }
                    else UIRoot.I.Toast("코인이 부족해요");
                }, 40, "ic_coin", Pal.TextDark);
                buy.Rt.At(0f, 0f, 24, 40, 200, 110);
                var ad = UIKit.Button(card.transform, $"{Profile.D.themeAds[i]}/{th.AdPrice}", Pal.Green, () =>
                    Ads.Rewarded(AdPlacement.Theme, ok =>
                    {
                        if (!ok) return;
                        if (Profile.ThemeAdWatched(i)) { Profile.SetTheme(i); AudioManager.Play(Sfx.Unlock); UIRoot.I.Toast($"'{th.Name}' 테마를 얻었어요!"); }
                        OnShow();
                    }), 40, "ic_ad");
                ad.Rt.At(1f, 0f, -24, 40, 200, 110);
            }
        }

        void BoosterRow(int i)
        {
            var row = UIKit.Panel(_content, Pal.Panel, 1f, "Booster" + i);
            UIKit.LE(row, 190);
            var ic = UIKit.Img(row.transform, Profile.BoosterIcon[i], Color.white, "Icon");
            ic.rectTransform.At(0f, 0.5f, 30, 0, 130, 130);
            var t = UIKit.Txt(row.transform, $"{Profile.BoosterName[i]}  <size=34>보유 {Profile.Boosters((Booster)i)}개</size>\n<size=32><color=#7D879E>{Profile.BoosterDesc[i]}</color></size>", 46, Color.white, TextAnchor.MiddleLeft);
            t.rectTransform.Fill(190, 0, 300, 0);
            int k = i;
            var b = UIKit.Button(row.transform, UIKit.N(Profile.BoosterPrice[i]), Pal.Yellow, () =>
            {
                if (Profile.SpendCoins(Profile.BoosterPrice[k])) { Profile.AddBooster((Booster)k, 1); AudioManager.Play(Sfx.Coin); OnShow(); }
                else UIRoot.I.Toast("코인이 부족해요");
            }, 40, "ic_coin", Pal.TextDark);
            b.Rt.At(1f, 0.5f, -30, 0, 250, 110);
        }

        public override void OnBack() => UIRoot.I.ShowHome();
    }
}
