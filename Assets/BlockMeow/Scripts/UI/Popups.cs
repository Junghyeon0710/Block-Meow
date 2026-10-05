using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockMeow
{
    // ------------------------------------------------------------------------------------------------ ads (simulated)

    /// <summary>Stand-in for an ad network creative. Rewarded: 3 s with a skippable close. Interstitial: closable after 1.5 s.</summary>
    public sealed class AdOverlay : Popup
    {
        bool _rewarded, _done, _finished;
        float _t;
        const float Length = 3f;
        Image _fill;
        Text _status;
        GameObject _close;
        Action<bool> _cb;

        protected override void Build() { BackCloses = false; }

        public void Init(bool rewarded, Action<bool> done)
        {
            _rewarded = rewarded;
            _cb = done;
            var bg = UIKit.Img(transform, null, Pal.Paper, "AdBg", true);
            bg.rectTransform.Fill();
            var safe = UIKit.Rect("Safe", transform).Fill(0, 60, 0, 60);
            var tag = UIKit.Img(safe, "ui_round_sm", Pal.Yellow, "Tag");
            tag.rectTransform.At(0f, 1f, 30, -30, 210, 70);
            var tagT = UIKit.Txt(tag.transform, rewarded ? "보상형 광고" : "광고", 32, Pal.TextDark, TextAnchor.MiddleCenter, false);
            tagT.rectTransform.Fill();
            var card = UIKit.Panel(safe, Pal.Panel2, 2f, "Creative");
            card.rectTransform.At(0.5f, 0.5f, 0, 60, 900, 900);
            var ic = UIKit.Img(card.transform, "ic_blocks", Color.white, "Icon");
            ic.rectTransform.At(0.5f, 0.5f, 0, 160, 320, 320);
            var t = UIKit.Txt(card.transform, "테스트 광고 화면\n<size=40>실서비스에서는 광고 네트워크(SDK)의\n동영상 광고가 이 자리에 재생됩니다</size>", 56, Color.white);
            t.rectTransform.At(0.5f, 0.5f, 0, -160, 860, 300);
            var bar = UIKit.Bar(safe, new Color(1f, 1f, 1f, 0.15f), Pal.Green, out _fill);
            bar.rectTransform.At(0.5f, 0f, 0, 260, 800, 40);
            UIKit.SetBar(_fill, 0f);
            _status = UIKit.Txt(safe, "", 40, Pal.TextDim);
            _status.rectTransform.At(0.5f, 0f, 0, 180, 900, 60);
            var close = UIKit.Round(safe, "ic_close", new Color(1f, 1f, 1f, 0.2f), OnClose, 0.45f);
            _close = close.gameObject;
            ((RectTransform)close.transform).At(1f, 1f, -30, -30, 100, 100);
            _close.SetActive(rewarded);
            AudioManager.PlayMusicDuck(true);
        }

        void Update()
        {
            if (_done) return;
            _t += Clock.Dt;
            float len = _rewarded ? Length : 2.5f;
            UIKit.SetBar(_fill, _t / len);
            if (_rewarded)
            {
                _status.text = _t < len ? $"{Mathf.CeilToInt(len - _t)}초 후 보상 지급" : "보상 지급 완료!";
                if (_t >= len) { _finished = true; Finish(true); }
            }
            else
            {
                _status.text = "";
                if (_t >= 1.5f && !_close.activeSelf) _close.SetActive(true);
                if (_t >= len + 3f) Finish(true);
            }
        }

        void OnClose()
        {
            if (_rewarded && !_finished)
            {
                Finish(false);
                UIRoot.I.Toast("광고를 끝까지 보면 보상을 받을 수 있어요");
                return;
            }
            Finish(true);
        }

        void Finish(bool ok)
        {
            if (_done) return;
            _done = true;
            AudioManager.PlayMusicDuck(false);
            var cb = _cb;
            Close(() => cb?.Invoke(ok));
        }
    }

    // ------------------------------------------------------------------------------------------------ generic

    public sealed class ConfirmPopup : Popup
    {
        protected override void Build() { }

        public void Init(string title, string message, string ok, Action onOk, string cancel = "취소", Action onCancel = null)
        {
            MakePanel(860, 620, title);
            var t = UIKit.Txt(Panel, message, 46, Color.white);
            t.rectTransform.Fill(50, 120, 50, 220);
            var okB = UIKit.Button(Panel, ok, Pal.Green, () => Close(onOk), 50);
            okB.Rt.At(1f, 0f, -50, 60, 350, 130);
            var noB = UIKit.Button(Panel, cancel, Pal.Panel3, () => Close(onCancel), 50);
            noB.Rt.At(0f, 0f, 50, 60, 350, 130);
        }
    }

    /// <summary>"You got..." popup with item icons.</summary>
    public sealed class RewardPopup : Popup
    {
        protected override void Build() { }

        public void Init(Reward r, string title = "보상 획득!", Action after = null)
        {
            MakePanel(860, 640, title, false, Pal.Green);
            var row = UIKit.Row(Panel, 40);
            ((RectTransform)row.transform).Fill(40, 140, 40, 220);
            void Item(string icon, string amount)
            {
                var col = UIKit.Rect("Item", row.transform);
                UIKit.LE(col, 260, 180);
                var ic = UIKit.Img(col, icon, Color.white, "Icon");
                ic.preserveAspect = true;
                ic.rectTransform.At(0.5f, 1f, 0, 0, 170, 170);
                var t = UIKit.Txt(col, amount, 50, Pal.Yellow);
                t.rectTransform.At(0.5f, 0f, 0, 0, 220, 70);
                Tweener.PopIn(col, 0.4f, 0.1f);
            }
            if (r.Coins > 0) Item("ic_coin", "+" + UIKit.N(r.Coins));
            if (r.Hammer > 0) Item("ic_hammer", "x" + r.Hammer);
            if (r.Refresh > 0) Item("ic_refresh", "x" + r.Refresh);
            if (r.Rotate > 0) Item("ic_rotate", "x" + r.Rotate);
            if (r.Spins > 0) Item("ic_wheel", "x" + r.Spins);
            var ok = UIKit.Button(Panel, "확인", Pal.Yellow, () => Close(after), 52, null, Pal.TextDark);
            ok.Rt.At(0.5f, 0f, 0, 60, 420, 130);
            AudioManager.Play(Sfx.Reward);
        }
    }

    public static class Toggles
    {
        /// <summary>Label + on/off switch row.</summary>
        public static void Row(Transform parent, string label, string icon, Func<bool> get, Action<bool> set)
        {
            var row = UIKit.Rect("Toggle_" + label, parent);
            UIKit.LE(row, 120);
            var ic = UIKit.Img(row, icon, Color.white, "Icon");
            ic.rectTransform.At(0f, 0.5f, 10, 0, 80, 80);
            var t = UIKit.Txt(row, label, 46, Color.white, TextAnchor.MiddleLeft);
            t.rectTransform.Fill(110, 0, 220, 0);
            var pill = UIKit.Img(row, "ui_pill", Pal.Green, "Switch", true);
            pill.rectTransform.At(1f, 0.5f, -10, 0, 180, 90);
            UIKit.Outline(pill, true);
            var knob = UIKit.Img(pill.transform, "ui_circle", Pal.Panel, "Knob");
            knob.rectTransform.At(0.5f, 0.5f, 0, 0, 74, 74);
            UIKit.Ring(knob.transform);
            void Apply(bool on)
            {
                pill.color = on ? Pal.Green : Pal.Panel3;
                Tweener.Move(knob.rectTransform, new Vector2(on ? 45f : -45f, 0f), 0.15f);
            }
            knob.rectTransform.anchoredPosition = new Vector2(get() ? 45f : -45f, 0f);
            pill.color = get() ? Pal.Green : Pal.Panel3;
            var b = pill.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(() =>
            {
                bool v = !get();
                set(v);
                Apply(v);
                Profile.MarkDirty();
                AudioManager.Play(Sfx.Click);
            });
        }
    }

    public sealed class SettingsPopup : Popup
    {
        protected override void Build()
        {
            MakePanel(900, 1180, "설정", true);
            var col = UIKit.Col(Panel, 16);
            ((RectTransform)col.transform).Fill(60, 150, 60, 200);
            Toggles.Row(col.transform, "효과음", "ic_sound", () => Profile.D.sfx, v => Profile.D.sfx = v);
            Toggles.Row(col.transform, "배경 음악", "ic_music", () => Profile.D.music, v => Profile.D.music = v);
            Toggles.Row(col.transform, "진동", "ic_vibrate", () => Profile.D.vibration, v => Profile.D.vibration = v);
            var stats = UIKit.Txt(col.transform,
                $"<size=36><color=#7D879E>플레이 {UIKit.N(Profile.D.gamesPlayed)}판 · 지운 줄 {UIKit.N(Profile.D.totalLines)}개 · 최고 콤보 {Profile.D.bestCombo}</color></size>",
                36, Color.white, TextAnchor.MiddleCenter, false, FontStyle.Normal);
            UIKit.LE(stats, 120);
            var reset = UIKit.Button(col.transform, "진행 초기화", Pal.Red, () =>
                UIRoot.I.Open<ConfirmPopup>().Init("진행 초기화", "코인, 테마, 고양이, 기록이\n모두 사라져요. 정말 초기화할까요?", "초기화", () =>
                {
                    Profile.ResetAll();
                    UIRoot.I.CloseAll();
                    GameApp.I.GoHome();
                    UIRoot.I.Toast("처음부터 다시 시작해요");
                }), 44);
            UIKit.LE(reset.Rt, 120);
            var ver = UIKit.Txt(Panel, $"블록냥 v{GameApp.Version} · 광고로 운영되는 무료 게임입니다", 32, Pal.TextDim, TextAnchor.MiddleCenter, false, FontStyle.Normal);
            ver.rectTransform.At(0.5f, 0f, 0, 70, 840, 60);
        }
    }

    public sealed class PausePopup : Popup
    {
        protected override void Build()
        {
            if (GameSession.I != null) GameSession.I.Paused = true;
            MakePanel(860, 1080, "일시정지");
            var col = UIKit.Col(Panel, 18);
            ((RectTransform)col.transform).Fill(60, 150, 60, 60);
            Toggles.Row(col.transform, "효과음", "ic_sound", () => Profile.D.sfx, v => Profile.D.sfx = v);
            Toggles.Row(col.transform, "배경 음악", "ic_music", () => Profile.D.music, v => Profile.D.music = v);
            var resume = UIKit.Button(col.transform, "계속하기", Pal.Green, () => Close(), 54, "ic_play");
            UIKit.LE(resume.Rt, 150);
            var restart = UIKit.Button(col.transform, "다시 하기", Pal.Yellow, () =>
                Close(() => Ads.BetweenGames(() => GameSession.I.Restart())), 50, "ic_retry", Pal.TextDark);
            UIKit.LE(restart.Rt, 140);
            var home = UIKit.Button(col.transform, "홈으로", Pal.Blue, () => Close(() => GameSession.I.LeaveToHome()), 50, "ic_home");
            UIKit.LE(home.Rt, 140);
            var s = GameSession.I;
            string noteText = s.IsChallenge ? "친구 대결은 홈으로 나가면 끝나요" : s.Mode == GameMode.Classic ? "클래식은 홈으로 나가도 이어서 할 수 있어요" : "";
            var note = UIKit.Txt(col.transform, noteText, 34, Pal.TextDim, TextAnchor.MiddleCenter, false, FontStyle.Normal);
            UIKit.LE(note, 70);
        }

        void OnDestroy() { if (GameSession.I != null) GameSession.I.Paused = false; }
    }

    // ------------------------------------------------------------------------------------------------ game results

    public sealed class RevivePopup : Popup
    {
        Image _ring;
        float _t;
        const float Countdown = 6f;
        Action _skip;
        bool _done;

        protected override void Build() { }

        public void Init(int score, Action onAd, Action onSkip)
        {
            _skip = onSkip;
            MakePanel(880, 1100, "앗, 놓을 곳이 없어요!", false, Pal.Red);
            var cat = CatView.Create(Panel, GameSession.I.PartnerCat, 300);
            cat.GetComponent<RectTransform>().At(0.5f, 1f, 0, -110, 300, 300);
            cat.SetMood(CatMood.Sad);
            var ringBg = UIKit.Img(Panel, "ui_disc", Pal.Panel2, "RingBg");
            ringBg.rectTransform.At(0.5f, 0.5f, 0, -40, 200, 200);
            _ring = UIKit.Img(Panel, "ui_disc", Pal.Yellow, "Ring");
            _ring.type = Image.Type.Filled;
            _ring.fillMethod = Image.FillMethod.Radial360;
            _ring.fillOrigin = (int)Image.Origin360.Top;
            _ring.fillClockwise = false;
            _ring.rectTransform.At(0.5f, 0.5f, 0, -40, 200, 200);
            var hole = UIKit.Img(Panel, "ui_disc", Pal.Panel, "Hole");
            hole.rectTransform.At(0.5f, 0.5f, 0, -40, 160, 160);
            var heart = UIKit.Img(hole.transform, "gem_1", Color.white, "Heart");
            heart.rectTransform.Fill(25, 25, 25, 25);
            var t = UIKit.Txt(Panel, $"점수 {UIKit.N(score)}\n<size=38>광고를 보면 꽉 찬 줄 4개를 지워 드려요</size>", 48, Color.white);
            t.rectTransform.At(0.5f, 0f, 0, 330, 820, 150);
            var ad = UIKit.Button(Panel, "이어하기", Pal.Green, () => { _done = true; Close(onAd); }, 56, "ic_ad");
            ad.Rt.At(0.5f, 0f, 0, 160, 620, 150);
            var skip = UIKit.Txt(Panel, "괜찮아요", 42, Pal.TextDim);
            skip.raycastTarget = true;
            skip.rectTransform.At(0.5f, 0f, 0, 50, 400, 80);
            var sb = skip.gameObject.AddComponent<Button>();
            sb.onClick.AddListener(() => { AudioManager.Play(Sfx.Click); Skip(); });
        }

        void Update()
        {
            if (_done) return;
            _t += Clock.Dt;
            _ring.fillAmount = 1f - _t / Countdown;
            if (_t >= Countdown) Skip();
        }

        void Skip()
        {
            if (_done) return;
            _done = true;
            Close(_skip);
        }

        public override void OnBack() => Skip();
    }

    public sealed class ClassicResultPopup : Popup
    {
        protected override void Build() { BackCloses = false; }

        public void Init(int score, int best, bool newBest, long coins, int challengeTarget)
        {
            var session = GameSession.I;
            bool challenge = challengeTarget > 0, won = challenge && score > challengeTarget;
            string title = challenge ? (won ? "도전 성공!" : "아쉬워요!") : newBest ? "새 최고 기록!" : "게임 오버";
            Color accent = challenge ? (won ? Pal.Green : Pal.Red) : newBest ? Pal.Orange : Pal.Purple;
            MakePanel(900, 1560, title, false, accent);
            var cat = CatView.Create(Panel, session.PartnerCat, 320);
            cat.GetComponent<RectTransform>().At(0.5f, 1f, 0, -100, 320, 320);
            cat.SetMood(newBest || won ? CatMood.Wow : challenge ? CatMood.Sad : CatMood.Happy, 99f);
            var label = UIKit.Txt(Panel, "점수", 40, Pal.TextDim);
            label.rectTransform.At(0.5f, 1f, 0, -430, 600, 60);
            var s = UIKit.Txt(Panel, "0", 130, Color.white);
            s.rectTransform.At(0.5f, 1f, 0, -490, 800, 160);
            Tweener.Count(s, 0, score, 0.9f);
            string line = challenge
                ? $"친구 점수  {UIKit.N(challengeTarget)}" + (won ? $"   <color=#2E9E5B>승리 보너스 +{GameSession.ChallengeWinCoins}</color>" : "")
                : $"최고 기록  {UIKit.N(best)}";
            var b = UIKit.Txt(Panel, line, 44, Pal.Yellow);
            b.rectTransform.At(0.5f, 1f, 0, -650, 860, 70);
            var coinRow = UIKit.Rect("Coins", Panel);
            coinRow.At(0.5f, 1f, 0, -730, 600, 100);
            var ci = UIKit.Img(coinRow, "ic_coin", Color.white, "Coin");
            ci.rectTransform.At(0f, 0.5f, 140, 0, 90, 90);
            var ct = UIKit.Txt(coinRow, "+" + UIKit.N(coins), 60, Pal.Yellow, TextAnchor.MiddleLeft);
            ct.rectTransform.Fill(250, 0, 0, 0);

            UIKit.Btn dbl = null;
            dbl = UIKit.Button(Panel, "코인 2배 받기", Pal.Green, () =>
                Ads.Rewarded(AdPlacement.DoubleCoins, ok =>
                {
                    if (!ok) return;
                    Profile.AddCoins(coins);
                    ct.text = "+" + UIKit.N(coins * 2);
                    Tweener.Punch(coinRow, 0.2f, 0.3f);
                    AudioManager.Play(Sfx.Coin);
                    dbl.SetEnabled(false);
                }), 50, "ic_ad");
            dbl.Rt.At(0.5f, 0f, 0, 370, 640, 140);
            dbl.SetEnabled(coins > 0);
            // the viral loop: every result can become a challenge for a friend (same seed, this score)
            string shareLabel = challenge ? (won ? "이긴 기록으로 도전장 보내기" : "내 점수로 도전장 보내기") : "친구에게 도전장 보내기";
            var share = UIKit.Button(Panel, shareLabel, Pal.Pink, () =>
            {
                string code = ChallengeCode.Encode(session.Seed, score);
                Share.Text(ChallengeCode.Message(code, score, session.Partner.Name), "도전장 보내기");
            }, 46, "ic_share");
            share.Rt.At(0.5f, 0f, 0, 530, 700, 130);
            share.SetEnabled(score > 0);
            var retry = UIKit.Button(Panel, challenge ? "다시 도전" : "다시 하기", Pal.Yellow, () => Close(() => Ads.BetweenGames(() => GameSession.I.Restart())), 54, "ic_retry", Pal.TextDark);
            retry.Rt.At(0.5f, 0f, 70, 200, 560, 150);
            var home = UIKit.Round(Panel, "ic_home", Pal.Blue, () => Close(() => Ads.BetweenGames(() => GameSession.I.LeaveToHome())), 0.5f);
            ((RectTransform)home.transform).At(0.5f, 0f, -300, 205, 140, 140);
            if (newBest || won)
            {
                AudioManager.Play(Sfx.NewBest);
                GameSession.I.Fx.Confetti(GameSession.I.BoardCenter + Vector2.up * 4f, 9f, 100, Themes.Current.Colors);
            }
        }
    }

    public sealed class LevelCompletePopup : Popup
    {
        protected override void Build() { BackCloses = false; }

        public void Init(GameMode mode, int level, long coins, int cat, int streak, string catLine)
        {
            bool daily = mode == GameMode.Daily;
            MakePanel(900, cat >= 0 ? 1400 : 1200, daily ? "오늘의 도전 성공!" : $"레벨 {level} 클리어!", false, Pal.Green);
            float y = -110;
            if (cat >= 0)
            {
                var cv = CatView.Create(Panel, cat, 380);
                cv.GetComponent<RectTransform>().At(0.5f, 1f, 0, y, 380, 380);
                cv.SetMood(CatMood.Wow, 99f);
                Tweener.PopIn(cv.transform, 0.5f, 0.3f);
                var skill = Cats.Get(cat).Skill;
                string text = (catLine ?? $"새 고양이 {Pal.EulReul(Cats.All[cat].Name)} 만났어요!")
                              + $"\n<size=34><color=#7D879E>스킬: {CatSkills.Name[(int)skill]}</color></size>";
                var t = UIKit.Txt(Panel, text, 46, Pal.Yellow);
                t.rectTransform.At(0.5f, 1f, 0, y - 385, 860, 120);
                AudioManager.Play(Sfx.Unlock);
                y -= 520;
            }
            else
            {
                var row = UIKit.Rect("Stars", Panel);
                row.At(0.5f, 1f, 0, y - 20, 700, 260);
                for (int i = 0; i < 3; i++)
                {
                    var st = UIKit.Img(row, "ic_star", Color.white, "Star");
                    st.rectTransform.At(0.5f, 0.5f, (i - 1) * 220, i == 1 ? 30 : 0, i == 1 ? 240 : 190, i == 1 ? 240 : 190);
                    Tweener.PopIn(st.transform, 0.45f, 0.15f + i * 0.18f);
                }
                y -= 300;
            }
            if (daily)
            {
                var st = UIKit.Txt(Panel, $"{streak}일 연속 도전 성공!", 50, Pal.Orange);
                st.rectTransform.At(0.5f, 1f, 0, y - 10, 800, 80);
                y -= 90;
            }
            var coinRow = UIKit.Rect("Coins", Panel);
            coinRow.At(0.5f, 1f, 0, y - 20, 600, 100);
            var ci = UIKit.Img(coinRow, "ic_coin", Color.white, "Coin");
            ci.rectTransform.At(0f, 0.5f, 140, 0, 90, 90);
            var ct = UIKit.Txt(coinRow, "+" + UIKit.N(coins), 60, Pal.Yellow, TextAnchor.MiddleLeft);
            ct.rectTransform.Fill(250, 0, 0, 0);

            UIKit.Btn dbl = null;
            dbl = UIKit.Button(Panel, "코인 2배 받기", Pal.Green, () =>
                Ads.Rewarded(AdPlacement.DoubleCoins, ok =>
                {
                    if (!ok) return;
                    Profile.AddCoins(coins);
                    ct.text = "+" + UIKit.N(coins * 2);
                    AudioManager.Play(Sfx.Coin);
                    dbl.SetEnabled(false);
                }), 50, "ic_ad");
            dbl.Rt.At(0.5f, 0f, 0, 360, 640, 140);
            if (daily)
            {
                var home = UIKit.Button(Panel, "홈으로", Pal.Yellow, () => Close(() => Ads.BetweenGames(() => GameSession.I.LeaveToHome())), 54, "ic_home", Pal.TextDark);
                home.Rt.At(0.5f, 0f, 0, 190, 560, 150);
            }
            else
            {
                var next = UIKit.Button(Panel, "다음 레벨", Pal.Yellow, () => Close(() => Ads.BetweenGames(() => GameSession.I.StartAdventure(level + 1))), 54, "ic_play", Pal.TextDark);
                next.Rt.At(0.5f, 0f, 70, 190, 560, 150);
                var home = UIKit.Round(Panel, "ic_home", Pal.Blue, () => Close(() => Ads.BetweenGames(() => GameSession.I.LeaveToHome())), 0.5f);
                ((RectTransform)home.transform).At(0.5f, 0f, -300, 195, 140, 140);
            }
        }
    }

    public sealed class LevelFailedPopup : Popup
    {
        protected override void Build() { BackCloses = false; }

        public void Init(GameMode mode, int level, int[] goals)
        {
            MakePanel(880, 1060, "아쉬워요!", false, Pal.Red);
            var cat = CatView.Create(Panel, GameSession.I.PartnerCat, 300);
            cat.GetComponent<RectTransform>().At(0.5f, 1f, 0, -110, 300, 300);
            cat.SetMood(CatMood.Sad);
            var t = UIKit.Txt(Panel, mode == GameMode.Daily ? "오늘의 도전, 다시 해볼까요?" : $"레벨 {level} · 남은 보석", 44, Color.white);
            t.rectTransform.At(0.5f, 1f, 0, -430, 800, 70);
            var row = UIKit.Row(Panel, 30);
            ((RectTransform)row.transform).At(0.5f, 1f, 0, -520, 700, 130);
            for (int k = 1; k <= 3; k++)
            {
                if (goals[k] <= 0) continue;
                var chip = UIKit.Rect("Goal", row.transform);
                UIKit.LE(chip, 120, 190);
                var gem = UIKit.Img(chip, "gem_" + k, Color.white, "Gem");
                gem.rectTransform.At(0f, 0.5f, 0, 0, 110, 110);
                var n = UIKit.Txt(chip, goals[k].ToString(), 56, Color.white, TextAnchor.MiddleLeft);
                n.rectTransform.Fill(115, 0, 0, 0);
            }
            var retry = UIKit.Button(Panel, "다시 하기", Pal.Yellow, () => Close(() => Ads.BetweenGames(() => GameSession.I.Restart())), 54, "ic_retry", Pal.TextDark);
            retry.Rt.At(0.5f, 0f, 70, 190, 560, 150);
            var home = UIKit.Round(Panel, "ic_home", Pal.Blue, () => Close(() => Ads.BetweenGames(() => GameSession.I.LeaveToHome())), 0.5f);
            ((RectTransform)home.transform).At(0.5f, 0f, -300, 195, 140, 140);
        }
    }

    public sealed class BoosterOfferPopup : Popup
    {
        protected override void Build() { }

        public void Init(Booster b, Action onGot)
        {
            int i = (int)b;
            MakePanel(860, 960, Profile.BoosterName[i], true, Pal.Blue);
            var ic = UIKit.Img(Panel, Profile.BoosterIcon[i], Color.white, "Icon");
            ic.rectTransform.At(0.5f, 1f, 0, -120, 260, 260);
            var t = UIKit.Txt(Panel, Profile.BoosterDesc[i] + "\n<size=36><color=#7D879E>보유 0개</color></size>", 48, Color.white);
            t.rectTransform.At(0.5f, 1f, 0, -400, 800, 160);
            var buy = UIKit.Button(Panel, $"{UIKit.N(Profile.BoosterPrice[i])}으로 구매", Pal.Yellow, () =>
            {
                if (Profile.SpendCoins(Profile.BoosterPrice[i])) { Profile.AddBooster(b, 1); AudioManager.Play(Sfx.Coin); Close(onGot); }
                else UIRoot.I.Toast("코인이 부족해요");
            }, 46, "ic_coin", Pal.TextDark);
            buy.Rt.At(0.5f, 0f, 0, 230, 640, 140);
            int left = Ads.Remaining(AdPlacement.Booster);
            var ad = UIKit.Button(Panel, $"무료로 1개 받기 ({left}/5)", Pal.Green, () =>
                Ads.Rewarded(AdPlacement.Booster, ok => { if (ok) { Profile.AddBooster(b, 1); AudioManager.Play(Sfx.Reward); Close(onGot); } }), 46, "ic_ad");
            ad.Rt.At(0.5f, 0f, 0, 70, 640, 140);
            ad.SetEnabled(left > 0);
        }
    }
}
