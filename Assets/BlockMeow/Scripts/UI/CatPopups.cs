using System;
using UnityEngine;
using UnityEngine.UI;

namespace BlockMeow
{
    /// <summary>Shared cat widgets: rarity tag, skill description block, result lines.</summary>
    public static class CatUI
    {
        public static Color RarityColor(int cat) => Cats.RarityColor[(int)Cats.Get(cat).Rarity];

        /// <summary>Small colored pill: "전설".</summary>
        public static Image RarityTag(Transform parent, CatRarity r, int fontSize = 30)
        {
            var tag = UIKit.Img(parent, "ui_pill", Cats.RarityColor[(int)r], "Rarity");
            bool light = r == CatRarity.Legendary;
            var t = UIKit.Txt(tag.transform, Cats.RarityName[(int)r], fontSize, light ? Pal.TextDark : Color.white, TextAnchor.MiddleCenter, !light);
            t.rectTransform.Fill();
            return tag;
        }

        /// <summary>Skill icon, name and what it does at this level (optionally the upgrade and the gauge).</summary>
        public static RectTransform SkillBlock(Transform parent, int cat, int level, bool details)
        {
            var def = Cats.Get(cat);
            var root = UIKit.Rect("Skill", parent);
            var box = UIKit.Panel(root, Pal.Panel2, 1f, "Box");
            box.raycastTarget = false;
            box.rectTransform.Fill();
            var iconBg = UIKit.Img(root, "ui_circle", Pal.Panel3, "IconBg");
            iconBg.rectTransform.At(0f, 1f, 22, -22, 116, 116);
            var icon = UIKit.Img(iconBg.transform, CatSkills.Icon[(int)def.Skill], Color.white, "Icon");
            icon.preserveAspect = true;
            icon.rectTransform.Fill(12, 12, 12, 12);
            bool strong = CatSkills.IsStrong(def.Rarity, level);
            string text = $"<size=46>{CatSkills.Name[(int)def.Skill]}</size>" + (strong ? "  <size=30><color=#E0408A>강화</color></size>" : "")
                          + $"\n<size=36>{CatSkills.Describe(def.Skill, def.Rarity, level)}</size>";
            if (details)
            {
                if (!strong) text += $"\n<size=32><color=#E0408A>Lv.{CatSkills.StrongLevel(def.Rarity)} 강화:</color> <color=#7D879E>{CatSkills.DescribeStrong(def.Skill)}</color></size>";
                text += $"\n<size=32><color=#7D879E>줄 {CatSkills.GaugeNeed(def.Rarity, level, def.Skill)}개를 지우면 사용 · 쓸 때마다 {CatSkills.TiredStep}개씩 더 필요</color></size>";
            }
            var t = UIKit.Txt(root, text, 36, Color.white, TextAnchor.UpperLeft);
            t.lineSpacing = 1.1f;
            t.rectTransform.Fill(160, 22, 22, 16);
            return root;
        }

        public static string ResultTitle(Profile.CatBoxResult r)
        {
            var name = Cats.Get(r.Cat).Name;
            return r.IsNew ? $"새 냥이 {name}!" : r.LevelUp ? $"{name} Lv.{r.Level}!" : $"{name} 최고 레벨!";
        }

        public static string ResultText(Profile.CatBoxResult r)
        {
            var def = Cats.Get(r.Cat);
            string skill = CatSkills.Name[(int)def.Skill];
            if (r.IsNew) return $"{Cats.RarityName[(int)def.Rarity]} · {skill}\n<size=34><color=#7D879E>{CatSkills.Describe(def.Skill, def.Rarity, 1)}</color></size>";
            if (r.LevelUp)
            {
                bool nowStrong = CatSkills.IsStrong(def.Rarity, r.Level) && !CatSkills.IsStrong(def.Rarity, r.Level - 1);
                if (nowStrong) return $"{skill} 강화!\n<size=34><color=#7D879E>{CatSkills.DescribeStrong(def.Skill)}</color></size>";
                if (r.Level == 3 || r.Level == 5) return $"스킬을 더 빨리 쓸 수 있어요!\n<size=34><color=#7D879E>줄 {CatSkills.GaugeNeed(def.Rarity, r.Level, def.Skill)}개를 지우면 {skill}</color></size>";
                return $"{Pal.EunNeun(def.Name)} 조금 더 자랐어요\n<size=34><color=#7D879E>최고 레벨까지 {Cats.MaxLevel - r.Level}번 더!</color></size>";
            }
            return $"이미 다 자란 냥이라\n코인 {UIKit.N(r.Coins)}개로 바꿨어요";
        }
    }

    /// <summary>Tapping the partner before its gauge is full: one rewarded ad per game fires the skill right away.</summary>
    public sealed class SkillChargePopup : Popup
    {
        protected override void Build()
        {
            var s = GameSession.I;
            int cat = s.PartnerCat;
            MakePanel(900, 1180, s.Stuck ? "냥이가 도와줄게요!" : "냥이 스킬", true, CatUI.RarityColor(cat));
            var cv = CatView.Create(Panel, cat, 280);
            cv.GetComponent<RectTransform>().At(0.5f, 1f, 0, -100, 280, 280);
            cv.SetMood(CatMood.Happy, 99f);
            var skill = CatUI.SkillBlock(Panel, cat, s.PartnerLevel, false);
            skill.At(0.5f, 1f, 0, -400, 820, 230);
            var info = UIKit.Txt(Panel, $"줄을 {s.GaugeNeed - s.Gauge}개 더 지우면 쓸 수 있어요\n<size=34><color=#7D879E>광고를 보면 지금 바로! (한 판에 1번)</color></size>", 40, Color.white);
            info.rectTransform.At(0.5f, 1f, 0, -660, 840, 130);
            var ad = UIKit.Button(Panel, "광고 보고 바로 쓰기", Pal.Green, () => Close(() => GameSession.I.ChargeSkillWithAd()), 50, "ic_ad");
            ad.Rt.At(0.5f, 0f, 0, 180, 680, 150);
            var no = UIKit.Txt(Panel, "괜찮아요", 42, Pal.TextDim);
            no.raycastTarget = true;
            no.rectTransform.At(0.5f, 0f, 0, 60, 400, 90);
            no.gameObject.AddComponent<Button>().onClick.AddListener(() => { AudioManager.Play(Sfx.Click); Close(); });
        }
    }

    /// <summary>One cat: rarity, level, skill (now and upgraded), partner choice or a one-game trial via a rewarded ad.</summary>
    public sealed class CatDetailPopup : Popup
    {
        protected override void Build() { }

        public void Init(int i, Action changed)
        {
            var def = Cats.Get(i);
            bool owned = Profile.CatUnlocked(i);
            int lv = Profile.CatLevel(i);
            bool partner = Profile.D.mascot == i;
            var col = Cats.RarityColor[(int)def.Rarity];
            MakePanel(920, 1450, def.Name, true, col);

            var glow = UIKit.Img(Panel, "ui_radial", col.WithA(0.35f), "Glow");
            glow.rectTransform.At(0.5f, 1f, 0, -40, 620, 470);
            var cv = CatView.Create(Panel, i, 330);
            cv.GetComponent<RectTransform>().At(0.5f, 1f, 0, -90, 330, 330);
            cv.SetMood(owned ? CatMood.Happy : CatMood.Idle, 1.5f);

            var tag = CatUI.RarityTag(Panel, def.Rarity, 32);
            tag.rectTransform.At(0.5f, 1f, owned ? -175 : -165, -440, 170, 58);
            var lvText = UIKit.Txt(Panel, owned ? $"Lv.{lv}" : "아직 못 만났어요", 40, owned ? Color.white : Pal.TextDim, TextAnchor.MiddleLeft);
            lvText.rectTransform.At(0.5f, 1f, owned ? -20 : 90, -440, owned ? 120 : 330, 58);
            if (owned)
            {
                for (int k = 0; k < Cats.MaxLevel; k++)
                {
                    var pip = UIKit.Img(Panel, "ui_circle", k < lv ? Pal.Yellow : Pal.Panel3, "Pip");
                    pip.rectTransform.At(0.5f, 1f, 60 + k * 36, -456, 26, 26);
                }
            }

            var skill = CatUI.SkillBlock(Panel, i, Mathf.Max(1, owned ? lv : GameSession.TrialLevel), true);
            skill.At(0.5f, 1f, 0, -530, 840, 350);

            string note = owned
                ? (lv < Cats.MaxLevel ? "냥이 상자에서 또 만나면 레벨이 올라요" : "최고 레벨까지 자랐어요!")
                : i > 0 ? $"모험 {i * 5}레벨 보상 · 냥이 상자에서도 나와요" : "냥이 상자에서 만날 수 있어요";
            if (!owned) note += $"\n<size=30>체험은 Lv.{GameSession.TrialLevel} 실력으로 한 판 함께해요</size>";
            var n = UIKit.Txt(Panel, note, 34, Pal.TextDim);
            n.rectTransform.At(0.5f, 1f, 0, -900, 840, 110);

            if (owned)
            {
                var pick = UIKit.Button(Panel, partner ? "함께하는 중" : "파트너로 선택", partner ? Pal.Panel3 : Pal.Yellow, () =>
                {
                    if (partner) { Close(); return; }
                    Profile.SetPartner(i);
                    if (Profile.D.trialCat >= 0) Profile.D.trialCat = -1;
                    AudioManager.Play(Sfx.Meow);
                    UIRoot.I.Toast($"이제 {Pal.WaGwa(def.Name)} 함께해요!");
                    Close(changed);
                }, 52, partner ? "ic_check" : "ic_paw", partner ? (Color?)null : Pal.TextDark);
                pick.Rt.At(0.5f, 0f, 0, 70, 640, 150);
            }
            else
            {
                int left = Ads.Remaining(AdPlacement.CatTrial);
                bool booked = Profile.D.trialCat == i;
                var trial = UIKit.Button(Panel, booked ? "다음 판에 함께해요!" : $"광고 보고 1판 함께하기 ({left})", Pal.Green, () =>
                    Ads.Rewarded(AdPlacement.CatTrial, ok =>
                    {
                        if (!ok) return;
                        Profile.D.trialCat = i;
                        Profile.MarkDirty();
                        AudioManager.Play(Sfx.Unlock);
                        UIRoot.I.Toast($"다음 판은 {Pal.WaGwa(def.Name)} 함께해요!");
                        Close(changed);
                    }), 46, booked ? "ic_check" : "ic_ad");
                trial.Rt.At(0.5f, 0f, 0, 70, 700, 150);
                trial.SetEnabled(left > 0 && !booked);
            }
        }
    }

    /// <summary>Cat box opening: the box shakes, then the cat pops out (new cat, level up or coins).</summary>
    public sealed class CatBoxPopup : Popup
    {
        protected override void Build() { BackCloses = false; }

        public void Init(Profile.CatBoxResult r, Action after, Action again, string againLabel)
        {
            var def = Cats.Get(r.Cat);
            var col = Cats.RarityColor[(int)def.Rarity];
            MakePanel(920, 1320, "냥이 상자", false, Pal.Purple);
            var glow = UIKit.Img(Panel, "ui_radial", col.WithA(0f), "Glow");
            glow.rectTransform.At(0.5f, 1f, 0, -20, 760, 620);
            var box = UIKit.Img(Panel, "ic_catbox", Color.white, "Box");
            box.preserveAspect = true;
            box.rectTransform.At(0.5f, 1f, 0, -140, 380, 380);
            var cv = CatView.Create(Panel, r.Cat, 400);
            var cvRt = cv.GetComponent<RectTransform>();
            cvRt.At(0.5f, 1f, 0, -100, 400, 400);
            cv.gameObject.SetActive(false);

            var texts = UIKit.Rect("Texts", Panel);
            texts.At(0.5f, 1f, 0, -520, 860, 330);
            var tcg = texts.gameObject.AddComponent<CanvasGroup>();
            tcg.alpha = 0f;
            var tag = CatUI.RarityTag(texts, def.Rarity, 32);
            tag.rectTransform.At(0.5f, 1f, 0, 0, 170, 58);
            var title = UIKit.Txt(texts, CatUI.ResultTitle(r), 64, col);
            title.rectTransform.At(0.5f, 1f, 0, -66, 860, 90);
            var sub = UIKit.Txt(texts, CatUI.ResultText(r), 40, Color.white);
            sub.rectTransform.At(0.5f, 1f, 0, -160, 860, 170);

            var buttons = UIKit.Rect("Buttons", Panel).Fill();
            var bcg = buttons.gameObject.AddComponent<CanvasGroup>();
            bcg.alpha = 0f;
            bcg.blocksRaycasts = false;
            var ok = UIKit.Button(buttons, "확인", Pal.Yellow, () => Close(after), 52, null, Pal.TextDark);
            ok.Rt.At(0.5f, 0f, 0, 60, 560, 140);
            if (again != null)
            {
                var more = UIKit.Button(buttons, againLabel, Pal.Green, () => Close(again), 44, "ic_ad");
                more.Rt.At(0.5f, 0f, 0, 220, 640, 130);
            }

            AudioManager.Play(Sfx.Deal);
            var boxRt = box.rectTransform;
            Tweener.To(1.0f, k =>
            {
                if (!boxRt) return;
                boxRt.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(k * 42f) * 13f * k);
                boxRt.localScale = Vector3.one * (1f + 0.18f * k * k);
            }, boxRt).SetEase(Ease.Linear).OnComplete(() =>
            {
                if (!this) return;
                box.gameObject.SetActive(false);
                cv.gameObject.SetActive(true);
                cvRt.localScale = Vector3.zero;
                Tweener.Scale(cvRt, Vector3.one, 0.5f, Ease.OutBack);
                cv.SetMood(CatMood.Wow, 99f);
                Tweener.Tint(glow, col.WithA(def.Rarity == CatRarity.Legendary ? 0.85f : 0.55f), 0.4f);
                for (int s = 0; s < 10; s++)
                {
                    var st = UIKit.Img(Panel, "ic_star", Color.white, "Star");
                    var srt = st.rectTransform;
                    srt.At(0.5f, 1f, 0, -300, 70, 70);
                    float a = s / 10f * Mathf.PI * 2f + UnityEngine.Random.value * 0.4f;
                    Vector2 to = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * UnityEngine.Random.Range(260f, 380f) + new Vector2(0f, -300f);
                    Tweener.Move(srt, to, 0.7f, Ease.OutCubic);
                    var scg = st.gameObject.AddComponent<CanvasGroup>();
                    Tweener.Delay(0.45f, () => { if (scg) Tweener.Fade(scg, 0f, 0.35f); }, srt);
                }
                Tweener.Fade(tcg, 1f, 0.3f);
                Tweener.Fade(bcg, 1f, 0.3f).OnComplete(() => { if (bcg) bcg.blocksRaycasts = true; });
                AudioManager.Play(def.Rarity == CatRarity.Legendary ? Sfx.NewBest : Sfx.Unlock);
                AudioManager.Play(Sfx.Meow, 1f, UnityEngine.Random.Range(0.95f, 1.1f));
                Haptics.Tap();
            });
        }
    }

    /// <summary>Friend battle: enter (or paste) a challenge code, or send your best game as a challenge.</summary>
    public sealed class ChallengePopup : Popup
    {
        InputField _input;
        RectTransform _field;

        protected override void Build()
        {
            var d = Profile.D;
            MakePanel(920, 1160, "친구 대결", true, Pal.Pink);
            var t = UIKit.Txt(Panel, "친구가 보낸 도전 코드를 넣으면\n같은 조각으로 시작해 점수를 겨뤄요!", 40, Color.white);
            t.rectTransform.At(0.5f, 1f, 0, -110, 840, 130);

            var field = UIKit.Panel(Panel, Pal.Bg, 1f, "CodeField");
            _field = field.rectTransform;
            _field.At(0.5f, 1f, 0, -270, 640, 140);
            var line = UIKit.Img(field.transform, "ui_round_line", Pal.Pink, "Line");
            line.rectTransform.Fill();
            var txt = UIKit.Txt(field.transform, "", 70, Pal.Yellow, TextAnchor.MiddleCenter, false);
            txt.supportRichText = false;
            txt.rectTransform.Fill(20, 0, 20, 0);
            var ph = UIKit.Txt(field.transform, "XXXX-XXXX", 70, Pal.TextDim.WithA(0.35f), TextAnchor.MiddleCenter, false, FontStyle.Normal);
            ph.rectTransform.Fill(20, 0, 20, 0);
            _input = field.gameObject.AddComponent<InputField>();
            _input.textComponent = txt;
            _input.placeholder = ph;
            _input.characterLimit = 9;
            _input.lineType = InputField.LineType.SingleLine;
            _input.onValidateInput = (text, index, ch) => char.IsLetterOrDigit(ch) || ch == '-' ? char.ToUpperInvariant(ch) : '\0';

            var paste = UIKit.Button(Panel, "붙여넣기", Pal.Panel3, Paste, 40, "ic_copy");
            paste.Rt.At(0.5f, 1f, -170, -440, 330, 110);
            var go = UIKit.Button(Panel, "도전!", Pal.Yellow, Go, 50, "ic_play", Pal.TextDark);
            go.Rt.At(0.5f, 1f, 170, -440, 330, 110);

            var div = UIKit.Img(Panel, "white", Pal.Line, "Divider");
            div.rectTransform.At(0.5f, 1f, 0, -610, 760, 4);
            var mine = UIKit.Txt(Panel, d.bestClassic > 0
                ? $"내 최고 기록 <color=#E0408A>{UIKit.N(d.bestClassic)}</color>점으로\n친구에게 도전장을 보내 보세요"
                : "클래식을 한 판 하면\n그 기록으로 도전장을 보낼 수 있어요", 40, Color.white);
            mine.rectTransform.At(0.5f, 1f, 0, -650, 840, 130);
            var send = UIKit.Button(Panel, "도전장 보내기", Pal.Pink, SendBest, 50, "ic_share");
            send.Rt.At(0.5f, 0f, 0, 170, 640, 150);
            send.SetEnabled(d.bestClassic > 0 && d.bestSeed >= 0);
            var stats = UIKit.Txt(Panel, $"친구 대결 {d.challengesPlayed}번 · 승리 {d.challengesWon}번 · 이기면 코인 +{GameSession.ChallengeWinCoins}", 32, Pal.TextDim, TextAnchor.MiddleCenter, false, FontStyle.Normal);
            stats.rectTransform.At(0.5f, 0f, 0, 70, 860, 60);
        }

        void Paste()
        {
            if (ChallengeCode.TryExtract(GUIUtility.systemCopyBuffer, out var code))
            {
                _input.text = code;
                Tweener.Punch(_field, 0.08f, 0.25f);
                AudioManager.Play(Sfx.Pop);
            }
            else UIRoot.I.Toast("복사한 내용에 도전 코드가 없어요");
        }

        void Go()
        {
            if (!ChallengeCode.TryDecode(_input.text, out int seed, out int score))
            {
                AudioManager.Play(Sfx.Invalid);
                Tweener.Punch(_field, 0.1f, 0.3f);
                UIRoot.I.Toast("코드를 다시 확인해 주세요 (예: K7Q2-MZ4P)");
                return;
            }
            Track.Log("challenge_accept", score.ToString());
            Close(() => GameSession.I.StartChallenge(seed, score));
        }

        void SendBest()
        {
            var d = Profile.D;
            if (d.bestClassic <= 0 || d.bestSeed < 0) return;
            string code = ChallengeCode.Encode(d.bestSeed, d.bestClassic);
            Share.Text(ChallengeCode.Message(code, d.bestClassic, Cats.Get(d.mascot).Name), "도전장 보내기");
        }
    }
}
