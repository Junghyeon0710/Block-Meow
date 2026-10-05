using System.Collections.Generic;
using UnityEngine;

namespace BlockMeow
{
    /// <summary>
    /// The partner cat: clearing lines fills its gauge, tapping it fires the cat's skill. One rewarded ad per game
    /// charges the gauge instantly. Also owns the "stuck" state: no piece fits, but the cat or a booster can still help.
    /// </summary>
    public sealed partial class GameSession
    {
        public const int SkillAdsPerGame = 1;
        /// <summary>Level a trial cat plays at when it is not owned yet, so the player sees what it can do.</summary>
        public const int TrialLevel = 3;

        public int PartnerCat { get; private set; }
        public CatDef Partner => Cats.Get(PartnerCat);
        public int PartnerLevel { get; private set; } = 1;
        public bool PartnerIsTrial { get; private set; }
        public bool SkillStrong { get; private set; }
        public int Gauge { get; private set; }
        /// <summary>Lines needed for the next use: the cat's base cost plus a little more after every use.</summary>
        public int GaugeNeed => _gaugeBase + Mathf.Min(SkillUses, CatSkills.MaxTiredUses) * CatSkills.TiredStep;
        /// <summary>The punch skill waits for a tap on the board.</summary>
        public bool Aiming { get; private set; }
        public int NapTurns { get; private set; }
        public bool Stuck { get; private set; }
        public int SkillUses { get; private set; }

        public bool SkillsEnabled => Mode != GameMode.Tutorial;
        public bool SkillReady => SkillsEnabled && Gauge >= GaugeNeed;
        public bool Napping => NapTurns > 0;
        public bool SkillAdAvailable => SkillsEnabled && !SkillReady && _skillAds < SkillAdsPerGame && Ads.CanReward(AdPlacement.SkillCharge);
        public string SkillName => CatSkills.Name[(int)Partner.Skill];
        public bool Busy => _busyT > 0f;

        int _skillAds, _gameId, _gaugeBase = 8;
        float _busyT;
        const float FallAccel = 55f; // cells/s^2 for the knead drop
        readonly List<int> _cells = new List<int>();
        readonly List<(int from, int to)> _moves = new List<(int from, int to)>();
        readonly float[] _fall = new float[64];  // visual drop offset (cells) after the knead skill
        readonly float[] _fallV = new float[64];

        void ResetSkillState()
        {
            _gameId++;
            Gauge = 0; _skillAds = 0; NapTurns = 0; SkillUses = 0; _busyT = 0f;
            Aiming = false; Stuck = false;
            for (int i = 0; i < 64; i++) { _fall[i] = 0f; _fallV[i] = 0f; }
        }

        /// <summary>Picks the cat for this game: a one-game trial cat if one was granted, otherwise the chosen partner.</summary>
        void SetupPartner()
        {
            var d = Profile.D;
            PartnerIsTrial = SkillsEnabled && d.trialCat >= 0;
            PartnerCat = PartnerIsTrial ? d.trialCat : d.mascot;
            if (PartnerIsTrial) { d.trialCat = -1; Profile.MarkDirty(); }
            PartnerLevel = Mathf.Max(1, Profile.CatLevel(PartnerCat));
            if (PartnerIsTrial) PartnerLevel = Mathf.Max(PartnerLevel, TrialLevel);
            SkillStrong = CatSkills.IsStrong(Partner.Rarity, PartnerLevel);
            _gaugeBase = CatSkills.GaugeNeed(Partner.Rarity, PartnerLevel, Partner.Skill);
        }

        void AddGauge(int lines)
        {
            if (!SkillsEnabled || lines <= 0 || Gauge >= GaugeNeed) return;
            Gauge = Mathf.Min(GaugeNeed, Gauge + lines);
            if (Gauge < GaugeNeed) return;
            AudioManager.Play(Sfx.Ready, 0.8f);
            GameScreen.I?.SkillReadyFx();
            if (!Profile.D.skillTipShown) GameScreen.I?.ShowSkillTip(Partner.Name);
        }

        void UpdateSkillTimers(float dt)
        {
            if (_busyT > 0f) _busyT -= dt;
            for (int i = 0; i < 64; i++)
            {
                if (_fall[i] <= 0f) continue;
                _fallV[i] += FallAccel * dt;
                _fall[i] -= _fallV[i] * dt;
                if (_fall[i] > 0f) continue;
                _fall[i] = 0f;
                _fallV[i] = 0f;
                _placeT[i] = 0f; // landing squash
            }
        }

        // ------------------------------------------------------------------ activation

        /// <summary>Tap on the partner cat in the HUD.</summary>
        public void TapCat()
        {
            if (!SkillsEnabled || State != Phase.Playing || Paused || _dragSlot >= 0 || Busy || UIRoot.I.HasPopup) return;
            if (Aiming) { CancelAim(); return; }
            if (ActiveTool != null) CancelTool();
            if (SkillReady) { FireSkill(); return; }
            if (SkillAdAvailable) { UIRoot.I.Open<SkillChargePopup>(); return; }
            AudioManager.Play(Sfx.Meow, 0.8f, Random.Range(0.95f, 1.1f));
            GameScreen.I?.SetMood(CatMood.Happy);
            UIRoot.I.Toast($"줄을 {GaugeNeed - Gauge}개 더 지우면 {SkillName}!");
        }

        /// <summary>Rewarded ad: fills the gauge and fires the skill right away (once per game).</summary>
        public void ChargeSkillWithAd()
        {
            if (!SkillAdAvailable) return;
            Ads.Rewarded(AdPlacement.SkillCharge, ok =>
            {
                if (!ok || State != Phase.Playing) return;
                _skillAds++;
                Gauge = GaugeNeed;
                Track.Log("skill_charge_ad", Partner.Skill.ToString());
                GameScreen.I?.Refresh();
                FireSkill();
            });
        }

        bool SkillUseful(SkillType s)
        {
            int filled = Board.Count();
            switch (s)
            {
                case SkillType.Yarn:
                case SkillType.LuckyBell: return filled < Board.N * Board.N;
                case SkillType.Nap: return true;
                case SkillType.Knead: return GravityWouldMove();
                default: return filled > 0;
            }
        }

        /// <summary>The nap only changes scoring, so it cannot rescue a stuck board.</summary>
        bool SkillCanUnstick(SkillType s) => s != SkillType.Nap && SkillUseful(s);

        bool GravityWouldMove()
        {
            for (int x = 0; x < Board.N; x++)
            {
                bool gap = false;
                for (int y = 0; y < Board.N; y++)
                {
                    if (!Board.Filled(x, y)) gap = true;
                    else if (gap) return true;
                }
            }
            return false;
        }

        void FireSkill()
        {
            var s = Partner.Skill;
            if (!SkillUseful(s))
            {
                UIRoot.I.Toast(s == SkillType.Knead ? "블록이 벌써 바닥에 꼭 붙어 있어요" : "지금은 쓸 곳이 없어요");
                return;
            }
            if (s == SkillType.Punch)
            {
                GameScreen.I?.HideHand();
                Aiming = true;
                AudioManager.Play(Sfx.Meow, 0.8f, 1.1f);
                GameScreen.I?.ShowHint($"{SkillName}! 날릴 곳을 터치하세요");
                GameScreen.I?.Refresh();
                return;
            }
            BeginSkill();
            switch (s)
            {
                case SkillType.RowSweep: FillLines(true); break;
                case SkillType.ColStamp: FillLines(false); break;
                case SkillType.Knead: Knead(); return; // finishes after the fall animation
                case SkillType.Yarn: Yarn(); break;
                case SkillType.FishParty: FishParty(); break;
                case SkillType.Nap: Nap(); break;
                case SkillType.LuckyBell: LuckyBell(); break;
            }
            AfterSkill();
        }

        void CancelAim()
        {
            Aiming = false;
            GameScreen.I?.HideHint();
            GameScreen.I?.Refresh();
        }

        void BeginSkill()
        {
            if (!Profile.D.skillTipShown)
            {
                Profile.D.skillTipShown = true;
                Profile.MarkDirty();
                GameScreen.I?.HideHand();
                GameScreen.I?.HideHint();
            }
            Gauge = 0;
            SkillUses++;
            Profile.MissionAdd(MissionKind.UseSkill);
            Track.Log("skill", Partner.Skill + (SkillStrong ? " strong" : ""));
            AudioManager.Play(Sfx.Skill);
            AudioManager.Play(Sfx.Meow, 0.9f, Random.Range(1.0f, 1.15f));
            GameScreen.I?.SetMood(CatMood.Wow);
            GameScreen.I?.ShowSkillBanner(PartnerCat, $"{Partner.Name}의 {SkillName}!");
        }

        /// <summary>Common ending: goals, refill, stuck check and the classic auto-save.</summary>
        void AfterSkill()
        {
            GameScreen.I?.Refresh();
            if (State != Phase.Playing) return;
            if (GoalsDone) { Win(); return; }
            if (TrayEmpty) Deal();
            CheckStuck();
            if (Mode == GameMode.Classic) SaveClassic();
        }

        /// <summary>Scores and animates the blocks a skill removed (in _cleared).</summary>
        void ScoreSkillClear(System.Func<ClearedCell, float> delayOf)
        {
            if (_cleared.Count == 0) return;
            float mult = (Fever ? 2f : 1f) * (Napping ? 2f : 1f);
            int pts = Mathf.RoundToInt(_cleared.Count * 10 * mult);
            Vector2 center = Vector2.zero;
            foreach (var c in _cleared)
            {
                Vector2 pos = CellPos(c.X, c.Y);
                center += pos;
                float delay = delayOf(c);
                Fx.PopBlock(pos, c.Color, c.Gem, delay);
                if (c.Gem > 0)
                {
                    _gems++;
                    if (Goals[c.Gem] > 0) Goals[c.Gem]--;
                    GameScreen.I?.FlyGem(c.Gem, WorldToScreen(pos), delay + 0.1f);
                }
            }
            center /= _cleared.Count;
            if (Board.Count() == 0)
            {
                pts += 1000;
                AllClearFx();
            }
            Score += pts;
            GameScreen.I?.ScorePopup(pts, WorldToScreen(center));
            AudioManager.Play(Sfx.Clear, 1f, 1.12f);
            UpdateBest();
        }

        // ------------------------------------------------------------------ the eight skills

        void PunchAt(int cx, int cy)
        {
            _cells.Clear();
            bool any = false;
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                {
                    bool inside = SkillStrong ? Mathf.Abs(dx) + Mathf.Abs(dy) <= 2 : Mathf.Abs(dx) <= 1 && Mathf.Abs(dy) <= 1;
                    int x = cx + dx, y = cy + dy;
                    if (!inside || !Board.In(x, y)) continue;
                    _cells.Add(y * Board.N + x);
                    if (Board.Filled(x, y)) any = true;
                }
            if (!any)
            {
                AudioManager.Play(Sfx.Invalid, 0.6f);
                UIRoot.I.Toast("블록이 있는 곳을 눌러 주세요");
                return;
            }
            Aiming = false;
            GameScreen.I?.HideHint();
            BeginSkill();
            Vector2 hit = CellPos(cx, cy);
            Board.ClearCells(_cells, _cleared);
            ScoreSkillClear(c => (CellPos(c.X, c.Y) - hit).magnitude * 0.05f);
            Fx.Ring(hit, 0.3f, SkillStrong ? 3.3f : 2.4f, 0.45f, Color.white);
            Fx.Burst(hit, Color.white, 1, 0f, 0.1f, 1.6f, 2.2f, 0.45f, CatSkills.Icon[(int)SkillType.Punch], false, 0f, 4f);
            Fx.Burst(hit, Pal.Yellow, 14, 4f, 10f, 0.2f, 0.4f, 0.5f, "p_star", true, 0f, 3f);
            Shake(0.4f);
            AudioManager.Play(Sfx.Hammer);
            Haptics.Tap();
            AfterSkill();
        }

        /// <summary>
        /// Tail sweep (rows, left to right) or paw stamps (columns, top to bottom): the gaps of the fullest lines are
        /// filled, so they pop as real line clears (combo included) and may complete crossing lines too.
        /// </summary>
        void FillLines(bool rows)
        {
            Board.FullestOf(rows, SkillStrong ? 2 : 1, _fullest);
            byte paint = (byte)(1 + PartnerCat % 8);
            string icon = CatSkills.Icon[(int)(rows ? SkillType.RowSweep : SkillType.ColStamp)];
            foreach (int line in _fullest)
            {
                for (int j = 0; j < Board.N; j++)
                {
                    int x = rows ? j : line, y = rows ? line : Board.N - 1 - j;
                    int k = y * Board.N + x;
                    var p = CellPos(x, y);
                    Fx.Burst(p, Color.white, 2, 2f, 5f, 0.3f, 0.5f, 0.35f, "p_spark", true, 0f, 3f, j * 0.04f);
                    if (Board.Color[k] != 0) continue;
                    Board.Color[k] = paint;
                    Board.Gem[k] = 0;
                    _placeT[k] = 0f;
                    if (!rows) Fx.Burst(p, Color.white, 1, 0f, 0.1f, 0.8f, 0.9f, 0.4f, icon, false, 0f, 4f, j * 0.04f);
                }
                if (rows) Fx.Burst(CellPos(0, line), Color.white, 1, 0f, 0.1f, 1.3f, 1.6f, 0.5f, icon, false, 0f, 4f);
            }
            Shake(0.22f);
            Board.FindFullLines(_rows, _cols);
            int lines = _rows.Count + _cols.Count;
            if (lines == 0) return;
            Vector2 from = rows ? CellPos(0, _fullest[0]) : CellPos(_fullest[0], Board.N - 1);
            OnLinesCleared(lines, from, 1f, true);
        }

        void Knead()
        {
            Board.ApplyGravity(_moves, SkillStrong ? Board.N : 2);
            int maxDrop = 1;
            foreach (var (from, to) in _moves)
            {
                int drop = from / Board.N - to / Board.N;
                maxDrop = Mathf.Max(maxDrop, drop);
                _fall[to] = drop;
                _fallV[to] = 0f;
                _placeT[to] = 10f;
            }
            float land = Mathf.Sqrt(2f * maxDrop / FallAccel) + 0.12f;
            for (int i = 0; i < 4; i++)
            {
                var p = CellPos(1 + i * 2, Board.N - 1) + new Vector2(0.5f, 0.3f);
                Fx.Burst(p, Color.white, 1, 0f, 0.2f, 1.1f, 1.3f, 0.6f, CatSkills.Icon[(int)SkillType.Knead], false, -6f, 0f, i * 0.06f);
            }
            _busyT = land + 0.05f;
            Shake(0.15f);
            AudioManager.Play(Sfx.Refresh, 0.9f, 0.8f);
            int game = _gameId;
            Tweener.Delay(land, () =>
            {
                if (State != Phase.Playing || game != _gameId) return;
                Shake(0.25f);
                Board.FindFullLines(_rows, _cols);
                int lines = _rows.Count + _cols.Count;
                if (lines > 0) OnLinesCleared(lines, BoardCenter, SkillStrong ? 2f : 1f, true);
                AfterSkill();
            }, this);
        }

        void Yarn()
        {
            var pool = new List<Shape>();
            if (SkillStrong)
                foreach (var s in Shapes.All) if (s.Size <= 3 && Board.BestClear(s) > 0) pool.Add(s);
            if (pool.Count == 0)
                foreach (var s in Shapes.All) if (s.Size <= 2 && Board.AnyFit(s)) pool.Add(s);
            if (pool.Count == 0) pool.Add(Shapes.All[0]);
            var set = new Shape[3];
            for (int i = 0; i < 3; i++) set[i] = pool[_gen.Range(0, pool.Count)];
            ReplaceTray(set, CatSkills.Icon[(int)SkillType.Yarn]);
        }

        void LuckyBell()
        {
            var cands = new List<(Shape s, int lines)>();
            foreach (var s in Shapes.All)
            {
                int l = Board.BestClear(s);
                if (l > 0) cands.Add((s, l));
            }
            if (cands.Count == 0) { Yarn(); return; }
            var set = new Shape[3];
            if (SkillStrong)
            {
                cands.Sort((a, b) => a.lines != b.lines ? b.lines.CompareTo(a.lines) : b.s.Size.CompareTo(a.s.Size));
                for (int i = 0; i < 3; i++) set[i] = cands[Mathf.Min(i, cands.Count - 1)].s;
            }
            else
            {
                for (int i = 0; i < 3; i++) set[i] = cands[_gen.Range(0, cands.Count)].s;
            }
            ReplaceTray(set, CatSkills.Icon[(int)SkillType.LuckyBell]);
        }

        void ReplaceTray(Shape[] set, string icon)
        {
            var pieces = _gen.ToPieces(set);
            for (int i = 0; i < 3; i++)
            {
                Fx.Burst(SlotPos(i), Color.white, 6, 2f, 5f, 0.2f, 0.35f, 0.4f, "p_star", true, 0f, 3f);
                Fx.Burst(SlotPos(i) + Vector2.up * 0.4f, Color.white, 1, 1f, 2f, 0.9f, 1.1f, 0.6f, icon, false, 2f, 2f, i * 0.08f);
                Tray[i] = pieces[i];
            }
            StartDealAnim();
            AudioManager.Play(Sfx.Deal, 0.8f);
        }

        void FishParty()
        {
            var colors = Board.ColorsByCount();
            int n = Mathf.Min(SkillStrong ? 2 : 1, colors.Count);
            _cells.Clear();
            for (int i = 0; i < Board.Color.Length; i++)
                for (int k = 0; k < n; k++)
                    if (Board.Color[i] == colors[k]) { _cells.Add(i); break; }
            Board.ClearCells(_cells, _cleared);
            string fish = CatSkills.Icon[(int)SkillType.FishParty];
            int fx = 0;
            ScoreSkillClear(c =>
            {
                float delay = Random.Range(0f, 0.35f);
                if (fx++ % 2 == 0) Fx.Burst(CellPos(c.X, c.Y), Color.white, 1, 2f, 4f, 0.5f, 0.7f, 0.7f, fish, false, -4f, 1f, delay);
                return delay;
            });
            AudioManager.Play(Sfx.Gem, 0.8f);
        }

        void Nap()
        {
            NapTurns += SkillStrong ? 9 : 6;
            GameScreen.I?.SetMood(CatMood.Sleepy);
            Fx.Burst(BoardCenter, Pal.Purple.Lighten(0.4f), 20, 1f, 4f, 0.25f, 0.45f, 1.2f, "p_star", true, 1f, 1.5f);
            GameScreen.I?.ShowPraise($"낮잠 시간! 점수 2배", Pal.Purple.Lighten(0.5f));
        }

        void TickNap()
        {
            if (NapTurns <= 0) return;
            NapTurns--;
            if (NapTurns == 0) GameScreen.I?.ShowPraise($"{Pal.EunNeun(Partner.Name)} 깼어요!", Color.white);
        }

        // ------------------------------------------------------------------ stuck state

        /// <summary>After any change to the tray or board: ends the game, or waits while the cat or a booster can still help.</summary>
        void CheckStuck()
        {
            if (State != Phase.Playing) return;
            if (AnyTrayFits()) { SetStuck(false); return; }
            if (CanRescue()) { SetStuck(true); return; }
            SetStuck(false);
            GameOver();
        }

        bool CanRescue()
        {
            if (!SkillsEnabled) return false;
            if (ActiveTool == Booster.Rotate || Aiming || Busy) return true;
            if ((SkillReady || SkillAdAvailable) && SkillCanUnstick(Partner.Skill)) return true;
            for (int i = 0; i < 3; i++) if (Profile.Boosters((Booster)i) > 0) return true;
            return false;
        }

        void SetStuck(bool on)
        {
            if (Stuck == on) return;
            Stuck = on;
            if (on)
            {
                AudioManager.Play(Sfx.Invalid, 0.8f);
                GameScreen.I?.SetMood(CatMood.Sad);
                Haptics.Tap();
                Track.Log("stuck", "ready=" + SkillReady);
            }
            GameScreen.I?.SetStuck(on);
        }

        /// <summary>"포기하기" while stuck: the normal game over (with its revive offer).</summary>
        public void GiveUp()
        {
            if (State != Phase.Playing || !Stuck) return;
            Aiming = false;
            ActiveTool = null;
            SetStuck(false);
            GameOver();
        }
    }
}
