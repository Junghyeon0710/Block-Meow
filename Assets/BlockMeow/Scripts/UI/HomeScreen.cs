using UnityEngine;
using UnityEngine.UI;

namespace BlockMeow
{
    /// <summary>Lobby: mascot, the three modes and the daily systems.</summary>
    public sealed class HomeScreen : UIScreen
    {
        Text _coins, _best, _classicSub, _advLabel, _dailyLabel;
        CatView _cat;
        Image _dotLogin, _dotMission, _dotSpin, _dotDaily, _dotCats;
        bool _autoLoginShown;

        public override void Build()
        {
            // top bar
            var bar = UIKit.Rect("TopBar", Rt).Top(0, 150);
            var settings = UIKit.Round(bar, "ic_settings", Pal.Panel3, () => UIRoot.I.Open<SettingsPopup>(), 0.55f);
            ((RectTransform)settings.transform).At(0f, 0.5f, 30, 0, 110, 110);
            _coins = UIKit.CoinChip(bar, out var chip, () => UIRoot.I.ShowShop());
            chip.At(1f, 0.5f, -24, 0, 320, 96);

            // the logo: "블록냥" colored in on graph squares, underlined in pen
            var logo = UIKit.Img(Rt, "logo_note", Color.white, "Logo");
            logo.preserveAspect = true;
            logo.rectTransform.At(0.5f, 1f, 0, -118, 540, 287);
            var sub = UIKit.Txt(Rt, "BLOCK MEOW · 블록 퍼즐", 36, Pal.TextDim);
            sub.rectTransform.At(0.5f, 1f, 0, -404, 900, 50);

            // mascot area between the title and the mode buttons
            var area = UIKit.Rect("CatArea", Rt);
            area.anchorMin = new Vector2(0, 0); area.anchorMax = new Vector2(1, 1);
            area.offsetMin = new Vector2(0, 1070); area.offsetMax = new Vector2(0, -452);
            _cat = CatView.Create(area, Profile.D.mascot, 500);
            var fitter = _cat.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 1f;
            _cat.OnTap = () => { AudioManager.Play(Sfx.Meow, 1f, Random.Range(0.92f, 1.12f)); _cat.SetMood(CatMood.Happy, 1.2f); UIRoot.I.ShowCats(); };
            _best = UIKit.Txt(Rt, "", 40, Color.white);
            _best.rectTransform.At(0.5f, 0f, 0, 952, 980, 116);

            // mode buttons (anchored to the bottom, above the menu row and the banner)
            float y = UIRoot.BannerH + 250;
            var daily = UIKit.Button(Rt, "오늘의 도전", Pal.Blue, () => UIRoot.I.Open<DailyPopup>(), 40, "ic_calendar");
            daily.Rt.At(0.5f, 0f, -195, y, 370, 140);
            _dailyLabel = daily.Label;
            _dotDaily = UIKit.Dot(daily.Rt, 48);
            var duel = UIKit.Button(Rt, "친구 대결", Pal.Purple, () => UIRoot.I.Open<ChallengePopup>(), 40, "ic_trophy");
            duel.Rt.At(0.5f, 0f, 195, y, 370, 140);
            y += 140 + 34;
            var adv = UIKit.Button(Rt, "모험", Pal.Pink, () => UIRoot.I.ShowAdventure(), 56, "ic_flag");
            adv.Rt.At(0.5f, 0f, 0, y, 760, 150);
            _advLabel = adv.Label;
            y += 150 + 34;
            var classic = UIKit.Button(Rt, "클래식", Pal.Yellow, OnClassic, 66, "ic_blocks", Pal.TextDark);
            classic.Rt.At(0.5f, 0f, 0, y, 760, 180);
            _classicSub = UIKit.Txt(classic.Rt, "", 30, Pal.TextDark, TextAnchor.MiddleCenter, false);
            _classicSub.rectTransform.At(0.5f, 0f, 0, 6, 500, 40);

            // daily systems row
            var row = UIKit.Rect("Menu", Rt).Bottom(UIRoot.BannerH + 12, 220, 20, 20);
            var h = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.childAlignment = TextAnchor.MiddleCenter;
            h.childControlWidth = h.childControlHeight = true;
            h.childForceExpandWidth = true; h.childForceExpandHeight = true;
            _dotLogin = MenuItem(row, "ic_gift", "출석", Pal.Pink, () => UIRoot.I.Open<LoginPopup>());
            _dotMission = MenuItem(row, "ic_mission", "미션", Pal.Panel, () => UIRoot.I.Open<MissionsPopup>());
            _dotSpin = MenuItem(row, "ic_wheel", "룰렛", Pal.Yellow, () => UIRoot.I.Open<SpinPopup>());
            MenuItem(row, "ic_palette", "상점", Pal.Panel, () => UIRoot.I.ShowShop()).gameObject.SetActive(false);
            _dotCats = MenuItem(row, "ic_paw", "냥이", Pal.Orange, () => UIRoot.I.ShowCats());

            Profile.Changed += Refresh;
        }

        void OnDestroy() => Profile.Changed -= Refresh;

        Image MenuItem(Transform parent, string icon, string label, Color tint, System.Action onClick)
        {
            var cell = UIKit.Rect("Item_" + label, parent);
            var b = cell.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            var hit = cell.gameObject.AddComponent<Image>();
            hit.color = new Color(0, 0, 0, 0);
            cell.gameObject.AddComponent<Squish>();
            // a pen-drawn circle, some colored in with highlighter
            var bg = UIKit.Img(cell, "ui_circle", tint, "Bg");
            bg.rectTransform.At(0.5f, 1f, 0, -6, 144, 144);
            UIKit.Ring(bg.transform);
            var ic = UIKit.Img(bg.transform, icon, Color.white, "Icon");
            ic.preserveAspect = true;
            ic.rectTransform.Fill(28, 28, 28, 28);
            var t = UIKit.Txt(cell, label, 34, Color.white);
            t.rectTransform.At(0.5f, 0f, 0, 6, 180, 50);
            b.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); onClick(); });
            return UIKit.Dot(bg.rectTransform, 44);
        }

        void OnClassic()
        {
            if (Profile.D.hasSaved) GameSession.I.StartClassic(true);
            else GameSession.I.StartClassic(false);
        }

        public override void OnShow()
        {
            _cat.SetCat(Profile.D.mascot);
            _cat.SetMood(CatMood.Happy, 1f);
            Refresh();
            AudioManager.PlayMusic(Music.Home);
            if (!_autoLoginShown && Profile.LoginAvailable)
            {
                _autoLoginShown = true;
                Tweener.Delay(0.5f, () => { if (!UIRoot.I.HasPopup && Profile.LoginAvailable) UIRoot.I.Open<LoginPopup>(); }, this);
            }
        }

        public void Refresh()
        {
            if (this == null) return;
            Profile.CheckDaily();
            var d = Profile.D;
            _coins.text = UIKit.N(d.coins);
            var partner = Cats.Get(d.mascot);
            string skill = CatSkills.Name[(int)partner.Skill];
            string partnerLine = $"<color=#E0408A>{partner.Name}</color> Lv.{Profile.CatLevel(d.mascot)} · {skill}";
            if (d.trialCat >= 0) partnerLine = $"다음 판 체험: <color=#2E9E5B>{Cats.Get(d.trialCat).Name}</color>";
            _best.text = partnerLine + "\n<size=36><color=#7D879E>" + (d.bestClassic > 0 ? $"최고 기록  {UIKit.N(d.bestClassic)}" : "줄을 지우면 냥이 스킬이 충전돼요!") + "</color></size>";
            _classicSub.text = d.hasSaved ? "이어서 하기" : "";
            _advLabel.text = $"모험 · 레벨 {d.adventureLevel}";
            _dailyLabel.text = Profile.DailyDoneToday ? $"완료 · {d.dailyStreak}일 연속" : d.dailyStreak > 0 ? $"도전 · {d.dailyStreak}일째" : "오늘의 도전";
            _dotLogin.gameObject.SetActive(Profile.LoginAvailable);
            _dotMission.gameObject.SetActive(Profile.AnyMissionReady());
            _dotSpin.gameObject.SetActive(Profile.SpinAvailable);
            _dotDaily.gameObject.SetActive(!Profile.DailyDoneToday);
            _dotCats.gameObject.SetActive(Profile.CatBoxFreeAvailable);
        }

        public override void OnBack()
        {
            UIRoot.I.Open<ConfirmPopup>().Init("게임 종료", "블록냥을 종료할까요?", "종료", () => Application.Quit());
        }
    }
}
