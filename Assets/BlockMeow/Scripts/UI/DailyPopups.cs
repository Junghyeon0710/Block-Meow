using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlockMeow
{
    /// <summary>7-day login calendar.</summary>
    public sealed class LoginPopup : Popup
    {
        protected override void Build()
        {
            MakePanel(940, 1180, "출석 보상", true, Pal.Pink);
            var grid = UIKit.Rect("Days", Panel);
            grid.Fill(40, 140, 40, 250);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(200, 300);
            g.spacing = new Vector2(16, 24);
            g.childAlignment = TextAnchor.UpperCenter;
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 4;
            var d = Profile.D;
            bool available = Profile.LoginAvailable;
            int next = d.loginIndex;
            for (int i = 0; i < 7; i++)
            {
                bool claimed = i < next;
                bool today = available && i == next;
                var cell = UIKit.Panel(grid, today ? Pal.Sticky : Pal.Panel2, 0.8f, "Day" + i);
                var t = UIKit.Txt(cell.transform, $"{i + 1}일차", 36, today ? Pal.PenPink : Pal.TextDim);
                t.rectTransform.At(0.5f, 1f, 0, -10, 190, 50);
                var r = Profile.LoginRewards[i];
                string icon = r.Coins > 0 ? "ic_coin" : r.Hammer > 0 ? "ic_hammer" : r.Refresh > 0 ? "ic_refresh" : "ic_rotate";
                string amount = r.Coins > 0 ? UIKit.N(r.Coins) : "x" + (r.Hammer + r.Refresh + r.Rotate);
                if (i == 6) icon = "ic_gift";
                var ic = UIKit.Img(cell.transform, icon, Color.white, "Icon");
                ic.rectTransform.At(0.5f, 0.5f, 0, 10, 130, 130);
                var a = UIKit.Txt(cell.transform, amount, 38, Color.white);
                a.rectTransform.At(0.5f, 0f, 0, 14, 190, 50);
                if (claimed)
                {
                    var dim = UIKit.Img(cell.transform, "ui_round", Pal.Panel.WithA(0.78f), "Done");
                    dim.pixelsPerUnitMultiplier = 1f / 0.8f;
                    dim.rectTransform.Fill();
                    var check = UIKit.Img(dim.transform, "ic_check", Pal.Green, "Check");
                    check.rectTransform.At(0.5f, 0.5f, 0, 0, 120, 120);
                }
            }
            var claim = UIKit.Button(Panel, available ? "받기" : "내일 또 만나요", available ? Pal.Yellow : Pal.Panel3, () =>
            {
                if (!Profile.LoginAvailable) { Close(); return; }
                var r = Profile.ClaimLogin();
                Close(() => UIRoot.I.Open<RewardPopup>().Init(r, "출석 보상!"));
            }, 54, available ? "ic_gift" : null, available ? Pal.TextDark : (Color?)null);
            claim.Rt.At(0.5f, 0f, 0, 70, 560, 150);
        }
    }

    /// <summary>Three daily missions plus an all-clear bonus spin.</summary>
    public sealed class MissionsPopup : Popup
    {
        protected override void Build()
        {
            MakePanel(940, 1220, "일일 미션", true, Pal.Blue);
            Profile.CheckDaily();
            var col = UIKit.Col(Panel, 22);
            ((RectTransform)col.transform).Fill(40, 150, 40, 120);
            var d = Profile.D;
            for (int i = 0; i < 3; i++)
            {
                int slot = i;
                int id = d.missionIds[i];
                var row = UIKit.Panel(col.transform, Pal.Panel2, 1f, "Mission" + i);
                UIKit.LE(row, 210);
                var t = UIKit.Txt(row.transform, Profile.MissionText[id], 42, Color.white, TextAnchor.UpperLeft);
                t.rectTransform.Fill(36, 30, 300, 100);
                var bar = UIKit.Bar(row.transform, Pal.Panel2, Pal.Green, out var fill);
                bar.rectTransform.At(0f, 0f, 36, 40, 470, 44);
                float frac = d.missionProgress[i] / (float)Profile.MissionTarget[id];
                UIKit.SetBar(fill, frac);
                var pt = UIKit.Txt(bar.transform, $"{d.missionProgress[i]}/{Profile.MissionTarget[id]}", 30, Color.white);
                pt.rectTransform.Fill();
                bool done = Profile.MissionDone(i), claimed = d.missionClaimed[i];
                var btn = UIKit.Button(row.transform, claimed ? "완료" : UIKit.N(Profile.MissionCoins[id]), claimed ? Pal.Panel3 : Pal.Yellow, () =>
                {
                    long c = Profile.ClaimMission(slot);
                    if (c > 0) { AudioManager.Play(Sfx.Coin); Close(() => UIRoot.I.Open<MissionsPopup>()); }
                }, 44, claimed ? null : "ic_coin", claimed ? (Color?)null : Pal.TextDark);
                btn.Rt.At(1f, 0.5f, -30, 0, 240, 120);
                btn.SetEnabled(done && !claimed);
            }
            bool all = d.missionClaimed[0] && d.missionClaimed[1] && d.missionClaimed[2];
            var bonus = UIKit.Panel(col.transform, Pal.Panel3, 1f, "Bonus");
            UIKit.LE(bonus, 190);
            var bi = UIKit.Img(bonus.transform, "ic_wheel", Color.white, "Icon");
            bi.rectTransform.At(0f, 0.5f, 30, 0, 130, 130);
            var bt = UIKit.Txt(bonus.transform, "모두 완료 보너스\n<size=34>행운 룰렛 1회</size>", 42, Color.white, TextAnchor.MiddleLeft);
            bt.rectTransform.Fill(190, 0, 280, 0);
            var bb = UIKit.Button(bonus.transform, d.missionBonus ? "완료" : "받기", d.missionBonus ? Pal.Panel2 : Pal.Green, () =>
            {
                if (Profile.ClaimMissionBonus()) Close(() => UIRoot.I.Open<RewardPopup>().Init(new Reward { Spins = 1 }, "보너스 획득!"));
            }, 44);
            bb.Rt.At(1f, 0.5f, -30, 0, 230, 120);
            bb.SetEnabled(all && !d.missionBonus);
            var note = UIKit.Txt(Panel, "미션은 매일 자정에 바뀌어요", 34, Pal.TextDim, TextAnchor.MiddleCenter, false, FontStyle.Normal);
            note.rectTransform.At(0.5f, 0f, 0, 50, 800, 60);
        }
    }

    /// <summary>Lucky wheel: one free spin per day, bonus spins, and up to three ad spins.</summary>
    public sealed class SpinPopup : Popup
    {
        static readonly Reward[] Prizes =
        {
            Reward.C(50), Reward.C(100), Reward.C(200), Reward.C(500),
            new Reward { Hammer = 1 }, new Reward { Refresh = 1 }, new Reward { Rotate = 1 }, Reward.C(1000)
        };
        static readonly float[] Weights = { 25, 22, 15, 6, 10, 10, 10, 2 };
        static readonly Color[] SegColors = { Pal.Pink, Pal.Orange, Pal.Green, Pal.Blue, Pal.Purple, Pal.Cyan, Pal.Hex("#FF9F8F"), Pal.Yellow };

        RectTransform _wheel;
        UIKit.Btn _spinBtn;
        bool _spinning;

        protected override void Build()
        {
            MakePanel(940, 1360, "행운 룰렛", true, Pal.Orange);
            var holder = UIKit.Rect("Wheel", Panel);
            holder.At(0.5f, 1f, 0, -150, 760, 760);
            var rim = UIKit.Img(holder, "ui_disc", Pal.Panel, "Rim");
            rim.rectTransform.Fill(-20, -20, -20, -20);
            UIKit.Ring(rim.transform);
            _wheel = UIKit.Rect("Disc", holder).Fill();
            for (int i = 0; i < 8; i++)
            {
                var seg = UIKit.Img(_wheel, "ui_disc", SegColors[i], "Seg" + i);
                seg.type = Image.Type.Filled;
                seg.fillMethod = Image.FillMethod.Radial360;
                seg.fillOrigin = (int)Image.Origin360.Top;
                seg.fillClockwise = true;
                seg.fillAmount = 1f / 8f;
                seg.rectTransform.Fill();
                seg.rectTransform.localRotation = Quaternion.Euler(0, 0, -i * 45f);
                float mid = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                var r = Prizes[i];
                string icon = r.Coins > 0 ? "ic_coin" : r.Hammer > 0 ? "ic_hammer" : r.Refresh > 0 ? "ic_refresh" : "ic_rotate";
                var lab = UIKit.Rect("Label" + i, _wheel);
                lab.At(0.5f, 0.5f, Mathf.Sin(mid) * 245f, Mathf.Cos(mid) * 245f, 160, 160);
                lab.localRotation = Quaternion.Euler(0, 0, -i * 45f - 22.5f);
                var ic = UIKit.Img(lab, icon, Color.white, "Icon");
                ic.rectTransform.At(0.5f, 1f, 0, 0, 90, 90);
                var t = UIKit.Txt(lab, r.Coins > 0 ? UIKit.N(r.Coins) : "x1", 34, Color.white);
                t.rectTransform.At(0.5f, 0f, 0, 10, 160, 50);
            }
            UIKit.Ring(holder, 0.8f, 2f);
            var hub = UIKit.Img(holder, "ui_disc", Pal.Panel, "Hub");
            hub.rectTransform.At(0.5f, 0.5f, 0, 0, 150, 150);
            UIKit.Ring(hub.transform);
            var hubIcon = UIKit.Img(hub.transform, "ic_paw", Color.white, "Paw");
            hubIcon.rectTransform.Fill(25, 25, 25, 25);
            var pointer = UIKit.Img(holder, "ic_play", Color.white, "Pointer");
            pointer.rectTransform.At(0.5f, 1f, 0, 40, 110, 110);
            pointer.rectTransform.localRotation = Quaternion.Euler(0, 0, -90f);
            _spinBtn = UIKit.Button(Panel, "돌리기", Pal.Green, OnSpin, 50);
            _spinBtn.Rt.At(0.5f, 0f, 0, 80, 640, 150);
            RefreshButton();
        }

        void RefreshButton()
        {
            var d = Profile.D;
            Profile.CheckDaily();
            if (!d.freeSpinUsed) _spinBtn.Label.text = "무료로 돌리기";
            else if (d.bonusSpins > 0) _spinBtn.Label.text = $"보너스 회전 ({d.bonusSpins})";
            else _spinBtn.Label.text = $"광고 보고 돌리기 ({Ads.Remaining(AdPlacement.Spin)}/3)";
            _spinBtn.SetEnabled(!_spinning && (!d.freeSpinUsed || d.bonusSpins > 0 || Ads.CanReward(AdPlacement.Spin)));
        }

        void OnSpin()
        {
            if (_spinning) return;
            var d = Profile.D;
            if (!d.freeSpinUsed) { d.freeSpinUsed = true; Profile.MarkDirty(); Spin(); }
            else if (d.bonusSpins > 0) { d.bonusSpins--; Profile.MarkDirty(); Spin(); }
            else Ads.Rewarded(AdPlacement.Spin, ok => { if (ok) Spin(); });
        }

        void Spin()
        {
            _spinning = true;
            RefreshButton();
            float total = 0f;
            foreach (var w in Weights) total += w;
            float roll = UnityEngine.Random.value * total;
            int pick = Weights.Length - 1;
            for (int i = 0; i < Weights.Length; i++) { roll -= Weights[i]; if (roll <= 0f) { pick = i; break; } }
            float start = _wheel.localEulerAngles.z % 360f;
            float target = 360f * 6f + pick * 45f + 22.5f + UnityEngine.Random.Range(-14f, 14f);
            int lastSeg = -1;
            Tweener.To(3.6f, k =>
            {
                if (!_wheel) return;
                float z = Mathf.Lerp(start, target, k);
                _wheel.localRotation = Quaternion.Euler(0, 0, z);
                int seg = Mathf.FloorToInt(z / 45f);
                if (seg != lastSeg) { lastSeg = seg; AudioManager.Play(Sfx.SpinTick, 0.6f); }
            }, _wheel).SetEase(Ease.OutCubic).OnComplete(() =>
            {
                _spinning = false;
                var prize = Prizes[pick];
                Profile.Grant(prize);
                RefreshButton();
                UIRoot.I.Open<RewardPopup>().Init(prize, "당첨!");
            });
        }
    }

    /// <summary>Daily challenge calendar with stamps and the streak.</summary>
    public sealed class DailyPopup : Popup
    {
        protected override void Build()
        {
            MakePanel(960, 1440, "오늘의 도전", true, Pal.Blue);
            var today = DateTime.Now.Date;
            var month = new DateTime(today.Year, today.Month, 1);
            var title = UIKit.Txt(Panel, $"{today.Year}년 {today.Month}월", 50, Color.white);
            title.rectTransform.At(0.5f, 1f, 0, -130, 800, 70);
            string[] wd = { "일", "월", "화", "수", "목", "금", "토" };
            var grid = UIKit.Rect("Calendar", Panel);
            grid.At(0.5f, 1f, 0, -210, 840, 720);
            var g = grid.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(112, 100);
            g.spacing = new Vector2(8, 8);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            g.constraintCount = 7;
            foreach (var w in wd)
            {
                var t = UIKit.Txt(grid, w, 34, w == "일" ? Pal.Red : Pal.TextDim);
                UIKit.LE(t, 60);
            }
            int offset = (int)month.DayOfWeek;
            int days = DateTime.DaysInMonth(today.Year, today.Month);
            var done = new HashSet<int>(Profile.D.dailyDays);
            for (int i = 0; i < offset; i++) UIKit.Rect("Empty", grid);
            for (int day = 1; day <= days; day++)
            {
                var date = new DateTime(today.Year, today.Month, day);
                int idx = (int)(date - new DateTime(2020, 1, 1)).TotalDays;
                bool isToday = date == today, cleared = done.Contains(idx);
                var cell = UIKit.Img(grid, "ui_round_sm", isToday ? Pal.Yellow : Pal.Panel2, "Day" + day);
                if (cleared)
                {
                    var paw = UIKit.Img(cell.transform, "ic_paw", Color.white, "Stamp");
                    paw.rectTransform.Fill(14, 8, 14, 8);
                }
                var t = UIKit.Txt(cell.transform, day.ToString(), 30, cleared ? Pal.TextDark : (date > today ? Pal.TextDim : Color.white), TextAnchor.UpperLeft, false);
                t.rectTransform.Fill(10, 4, 4, 4);
                if (isToday)
                {
                    var ring = UIKit.Img(cell.transform, "ui_round_line", Pal.Yellow, "Today");
                    ring.pixelsPerUnitMultiplier = 2f;
                    ring.rectTransform.Fill(-4, -4, -4, -4);
                }
            }
            var d = Profile.D;
            var streak = UIKit.Rect("Streak", Panel);
            streak.At(0.5f, 0f, 0, 300, 800, 110);
            var flame = UIKit.Img(streak, "ic_flame", Color.white, "Flame");
            flame.rectTransform.At(0f, 0.5f, 80, 0, 100, 100);
            var st = UIKit.Txt(streak, $"{d.dailyStreak}일 연속 성공\n<size=32><color=#7D879E>7일 연속마다 코인 300 + 회전 부스터</color></size>", 46, Color.white, TextAnchor.MiddleLeft);
            st.rectTransform.Fill(200, 0, 0, 0);
            bool doneToday = Profile.DailyDoneToday;
            var play = UIKit.Button(Panel, doneToday ? "오늘은 완료! 내일 또 만나요" : "도전하기", doneToday ? Pal.Panel3 : Pal.Yellow,
                () => Close(() => GameSession.I.StartDaily()), 52, doneToday ? null : "ic_play", doneToday ? (Color?)null : Pal.TextDark);
            play.Rt.At(0.5f, 0f, 0, 90, 700, 150);
            play.SetEnabled(!doneToday);
        }
    }
}
