using System.Collections.Generic;
using UnityEngine;

namespace BlockMeow
{
    public enum GameMode { Classic, Adventure, Daily, Tutorial }
    public enum Phase { Idle, Playing, Over, Won }
    public enum CatMood { Idle, Happy, Wow, Sad, Sleepy }

    /// <summary>
    /// The puzzle: board, tray, scoring, combos/fever, goals, boosters, revive and the end-of-game hand-off.
    /// Rendering lives in GameSession.View.cs and pointer handling in GameSession.Input.cs.
    /// </summary>
    public sealed partial class GameSession : MonoBehaviour
    {
        public static GameSession I { get; private set; }

        public GameMode Mode { get; private set; }
        public Phase State { get; private set; } = Phase.Idle;
        public bool Paused;

        public readonly Board Board = new Board();
        public readonly Piece[] Tray = new Piece[3];
        public int Score, Best, Combo, MaxCombo, Level;
        public readonly int[] Goals = new int[4];
        public readonly int[] GoalTotals = new int[4];
        public LevelDef CurrentLevel { get; private set; }
        public bool Fever => Combo >= 4;
        public bool Revived { get; private set; }
        public Booster? ActiveTool { get; private set; }
        /// <summary>Seed of the piece sequence; with the score it becomes a shareable challenge code.</summary>
        public int Seed { get; private set; }
        /// <summary>Score to beat in a friend challenge (0 = normal game).</summary>
        public int ChallengeTarget { get; private set; }
        public bool IsChallenge => ChallengeTarget > 0;
        bool GoalsDone => CurrentLevel != null && Goals[1] + Goals[2] + Goals[3] == 0;

        PieceGenerator _gen;
        int _miss, _lines, _gems, _tutStep, _placed;
        bool _newBestShown, _targetBeaten;
        readonly List<int> _rows = new List<int>(), _cols = new List<int>(), _fullest = new List<int>();
        readonly List<ClearedCell> _cleared = new List<ClearedCell>();

        public static readonly string[] Praise = { null, null, "좋아요!", "멋져요!", "대단해요!", "완벽해요!" };

        public void Init(Camera cam)
        {
            I = this;
            InitView(cam);
        }

        // ------------------------------------------------------------------ starting games

        void ResetCommon()
        {
            Score = 0; Combo = 0; MaxCombo = 0; _miss = 0; _lines = 0; _gems = 0; _placed = 0;
            Revived = false; ActiveTool = null; Paused = false; _newBestShown = false;
            ChallengeTarget = 0; _targetBeaten = false;
            for (int i = 0; i < 4; i++) { Goals[i] = 0; GoalTotals[i] = 0; }
            for (int i = 0; i < 3; i++) Tray[i] = null;
            CurrentLevel = null;
            ResetSkillState();
            ResetView();
        }

        public void StartClassic(bool resume)
        {
            ResetCommon();
            Mode = GameMode.Classic;
            Best = Profile.D.bestClassic;
            bool restore = resume && Profile.D.hasSaved;
            Seed = restore ? Profile.D.savedSeed & ChallengeCode.MaxSeed : Random.Range(0, ChallengeCode.MaxSeed + 1);
            _gen = new PieceGenerator(Seed);
            SetupPartner();
            if (restore) RestoreClassic();
            else
            {
                Board.Clear();
                Profile.D.hasSaved = false;
                Deal();
            }
            Enter();
            Track.Log("game_start", "classic resume=" + resume);
        }

        /// <summary>Friend challenge: classic rules from the friend's seed, try to beat their score.</summary>
        public void StartChallenge(int seed, int target)
        {
            ResetCommon();
            Mode = GameMode.Classic;
            Best = Profile.D.bestClassic;
            Seed = seed & ChallengeCode.MaxSeed;
            ChallengeTarget = Mathf.Max(1, target);
            _gen = new PieceGenerator(Seed);
            SetupPartner();
            Board.Clear();
            Deal();
            Profile.D.challengesPlayed++;
            Profile.MarkDirty();
            Enter();
            Track.Log("game_start", $"challenge target={target}");
        }

        public void StartAdventure(int level)
        {
            ResetCommon();
            Mode = GameMode.Adventure;
            Level = level;
            SetupPartner();
            SetupLevel(Levels.Adventure(level));
            Enter();
            Track.Log("game_start", "adventure " + level);
        }

        public void StartDaily()
        {
            ResetCommon();
            Mode = GameMode.Daily;
            SetupPartner();
            SetupLevel(Levels.Daily(Profile.Today));
            Enter();
            Track.Log("game_start", "daily");
        }

        public void StartTutorial()
        {
            ResetCommon();
            Mode = GameMode.Tutorial;
            SetupPartner();
            _tutStep = 0;
            Board.CopyFrom(Levels.TutorialBoard(0));
            Tray[1] = new Piece(Shapes.All[0], 5);
            StartDealAnim();
            Enter();
            ShowTutorialHint();
        }

        void SetupLevel(LevelDef lv)
        {
            CurrentLevel = lv;
            Board.CopyFrom(lv.Start);
            for (int k = 1; k <= 3; k++) { Goals[k] = lv.Goals[k]; GoalTotals[k] = lv.Goals[k]; }
            Seed = lv.Seed & ChallengeCode.MaxSeed;
            _gen = new PieceGenerator(lv.Seed);
            _gen.GemChance = lv.PieceGemChance;
            Deal();
        }

        void Enter()
        {
            State = Phase.Playing;
            SetWorldVisible(true);
            UIRoot.I.ShowGame();
            GameScreen.I?.Refresh();
            GameScreen.I?.SetMood(CatMood.Idle);
            AudioManager.PlayMusic(Music.Game);
        }

        public void Restart()
        {
            switch (Mode)
            {
                case GameMode.Adventure: StartAdventure(Level); break;
                case GameMode.Daily: StartDaily(); break;
                default:
                    if (IsChallenge) StartChallenge(Seed, ChallengeTarget);
                    else StartClassic(false);
                    break;
            }
        }

        /// <summary>Leaves the board (a running classic game is kept so it can be resumed).</summary>
        public void LeaveToHome()
        {
            if (Mode == GameMode.Classic && State == Phase.Playing) SaveClassic();
            State = Phase.Idle;
            ActiveTool = null;
            Aiming = false;
            SetWorldVisible(false);
            GameApp.I.GoHome();
        }

        // ------------------------------------------------------------------ dealing

        void Deal()
        {
            float difficulty, help;
            if (Mode == GameMode.Classic)
            {
                // pieces placed, not score: skills that raise the score must not make the pieces harder
                difficulty = Mathf.Clamp01(_placed / 70f);
                help = Mathf.Lerp(0.55f, 0.25f, difficulty);
            }
            else
            {
                difficulty = (CurrentLevel?.Difficulty ?? 0f) * 0.8f;
                help = 0.45f;
            }
            _gen.GemKinds.Clear();
            if (CurrentLevel != null)
                for (byte k = 1; k <= 3; k++)
                    if (Goals[k] > Board.GemCount(k)) _gen.GemKinds.Add(k);
            var pieces = _gen.Deal(Board, difficulty, help);
            for (int i = 0; i < 3; i++) Tray[i] = pieces[i];
            StartDealAnim();
            AudioManager.Play(Sfx.Deal, 0.6f);
        }

        bool TrayEmpty => Tray[0] == null && Tray[1] == null && Tray[2] == null;

        public bool Fits(int slot) => Tray[slot] != null && Board.AnyFit(Tray[slot].Shape);

        bool AnyTrayFits()
        {
            for (int i = 0; i < 3; i++) if (Fits(i)) return true;
            return false;
        }

        // ------------------------------------------------------------------ placing & clearing

        void Place(int slot, int ox, int oy)
        {
            var p = Tray[slot];
            Tray[slot] = null;
            _placed++;
            Board.Place(p, ox, oy);
            Vector2 center = Vector2.zero;
            foreach (var c in p.Shape.Cells)
            {
                MarkPlaced(ox + c.x, oy + c.y);
                center += CellPos(ox + c.x, oy + c.y);
            }
            center /= p.Shape.Size;
            Score += p.Shape.Size * (Napping ? 2 : 1);
            var placedCol = Theme.Colors[Mathf.Clamp(p.Color - 1, 0, 7)];
            foreach (var c in p.Shape.Cells)
                Fx.Burst(CellPos(ox + c.x, oy + c.y), placedCol.Lighten(0.5f), 1, 0.5f, 1.5f, 0.25f, 0.4f, 0.35f, "p_spark", true, 0f, 4f);
            AudioManager.Play(Sfx.Place, 0.9f, Random.Range(0.94f, 1.06f));
            if (ActiveTool == Booster.Rotate) { ActiveTool = null; GameScreen.I?.HideHint(); }

            Board.FindFullLines(_rows, _cols);
            int lines = _rows.Count + _cols.Count;
            if (lines > 0) OnLinesCleared(lines, center);
            else if (!Napping)
            {
                _miss++;
                if (_miss >= 3 && Combo > 0)
                {
                    bool wasFever = Fever;
                    Combo = 0;
                    if (wasFever) { AudioManager.PlayMusic(Music.Game); GameScreen.I?.SetFever(false); }
                }
            }

            if (Napping) TickNap();
            UpdateBest();
            GameScreen.I?.Refresh();

            if (Mode == GameMode.Tutorial) { TutorialAfterPlace(); return; }
            if (GoalsDone) { Win(); return; }
            if (TrayEmpty) Deal();
            CheckStuck();
            if (Mode == GameMode.Classic) SaveClassic();
        }

        /// <summary>New-best and challenge-target celebrations (classic rules only).</summary>
        void UpdateBest()
        {
            if (Mode != GameMode.Classic) return;
            if (IsChallenge && !_targetBeaten && Score > ChallengeTarget)
            {
                _targetBeaten = true;
                GameScreen.I?.ShowPraise("친구 점수 돌파!", Pal.Green);
                AudioManager.Play(Sfx.NewBest, 0.8f);
                Fx.Confetti(BoardCenter + Vector2.up * 3f, 7f, 60, Theme.Colors);
            }
            if (Score <= Best) return;
            if (!_newBestShown && Best > 0 && !IsChallenge)
            {
                _newBestShown = true;
                GameScreen.I?.ShowPraise("최고 기록!", Pal.Yellow);
                AudioManager.Play(Sfx.NewBest, 0.8f);
            }
            Best = Score;
        }

        /// <summary>Clears the full lines in _rows/_cols. Skill-made lines can score extra but do not refill the gauge.</summary>
        void OnLinesCleared(int lines, Vector2 center, float extraMult = 1f, bool fromSkill = false)
        {
            Board.ClearLines(_rows, _cols, _cleared);
            Combo++;
            _miss = 0;
            MaxCombo = Mathf.Max(MaxCombo, Combo);
            _lines += lines;
            if (!fromSkill) AddGauge(lines);

            float lineMult = lines == 1 ? 1f : lines == 2 ? 1.5f : lines == 3 ? 2f : 3f;
            float comboMult = 1f + 0.5f * (Combo - 1);
            float fever = Fever ? 2f : 1f;
            float nap = Napping ? 2f : 1f;
            int pts = Mathf.RoundToInt(_cleared.Count * 10 * lineMult * comboMult * fever * nap * extraMult);
            bool allClear = Board.Count() == 0;
            if (allClear) pts += Mathf.RoundToInt(1000 * comboMult);
            Score += pts;

            foreach (var c in _cleared)
            {
                Vector2 pos = CellPos(c.X, c.Y);
                float delay = (pos - center).magnitude * 0.035f;
                Fx.PopBlock(pos, c.Color, c.Gem, delay);
                if (c.Gem > 0)
                {
                    _gems++;
                    if (Goals[c.Gem] > 0) Goals[c.Gem]--;
                    GameScreen.I?.FlyGem(c.Gem, WorldToScreen(pos), delay + 0.1f);
                }
            }

            AudioManager.Play(Sfx.Clear, 1f, Mathf.Min(1.7f, 1f + 0.07f * (Combo - 1) + 0.04f * (lines - 1)));
            if (Combo >= 2) AudioManager.Play(Sfx.Combo, 0.7f, Mathf.Min(1.6f, 0.9f + 0.06f * Combo));

            var screen = WorldToScreen(center);
            GameScreen.I?.ScorePopup(pts, screen);
            if (Combo >= 2) GameScreen.I?.ShowCombo(Combo);
            string praise = allClear ? null : Praise[Mathf.Min(lines, 5)];
            if (praise != null) GameScreen.I?.ShowPraise(praise, Color.white);
            GameScreen.I?.SetMood(lines >= 3 || Combo >= 3 || allClear ? CatMood.Wow : CatMood.Happy);

            if (lines >= 3 || allClear) Shake(0.25f + 0.08f * lines);
            if (Combo == 4)
            {
                AudioManager.Play(Sfx.Fever);
                AudioManager.PlayMusic(Music.Fever);
                GameScreen.I?.SetFever(true);
                GameScreen.I?.ShowPraise("피버 타임!", Pal.Pink);
            }
            if (allClear) AllClearFx();

            Profile.MissionAdd(MissionKind.Lines, lines);
            if (Combo == 3) Profile.MissionAdd(MissionKind.Combo3);
        }

        void AllClearFx()
        {
            GameScreen.I?.ShowPraise("올 클리어!", Pal.Yellow);
            AudioManager.Play(Sfx.AllClear);
            Fx.Confetti(BoardCenter + Vector2.up * 2f, 8f, 90, Theme.Colors);
        }

        // ------------------------------------------------------------------ boosters

        public void UseBooster(Booster b)
        {
            if (State != Phase.Playing || Paused || _dragSlot >= 0 || Mode == GameMode.Tutorial || Busy) return;
            if (Aiming) CancelAim();
            if (ActiveTool == b) { CancelTool(); return; }
            if (Profile.Boosters(b) <= 0)
            {
                UIRoot.I.ShowBoosterOffer(b, () => GameScreen.I?.Refresh());
                return;
            }
            switch (b)
            {
                case Booster.Hammer:
                    ActiveTool = Booster.Hammer;
                    GameScreen.I?.ShowHint("부술 블록을 터치하세요");
                    break;
                case Booster.Refresh:
                    Profile.UseBooster(Booster.Refresh);
                    Deal();
                    AudioManager.Play(Sfx.Refresh);
                    CheckStuck();
                    break;
                case Booster.Rotate:
                    Profile.UseBooster(Booster.Rotate);
                    ActiveTool = Booster.Rotate;
                    GameScreen.I?.ShowHint("돌릴 조각을 터치하세요");
                    break;
            }
            GameScreen.I?.Refresh();
        }

        public void CancelTool()
        {
            // a rotate charge is spent on activation; the hammer only when it hits
            ActiveTool = null;
            GameScreen.I?.HideHint();
            GameScreen.I?.Refresh();
            if (Stuck) CheckStuck();
        }

        void HammerAt(int x, int y)
        {
            if (!Profile.UseBooster(Booster.Hammer)) { CancelTool(); return; }
            int k = y * Board.N + x;
            byte color = Board.Color[k], gem = Board.Gem[k];
            Board.Color[k] = 0;
            Board.Gem[k] = 0;
            Vector2 pos = CellPos(x, y);
            Fx.PopBlock(pos, color, gem, 0f);
            Fx.Burst(pos, Color.white, 8, 3f, 8f, 0.15f, 0.3f, 0.5f, "p_spark", true, 0f, 3f);
            Shake(0.2f);
            AudioManager.Play(Sfx.Hammer);
            if (gem > 0)
            {
                _gems++;
                if (Goals[gem] > 0) Goals[gem]--;
                GameScreen.I?.FlyGem(gem, WorldToScreen(pos), 0.1f);
            }
            CancelTool();
            if (GoalsDone) { Win(); return; }
            CheckStuck();
        }

        // ------------------------------------------------------------------ end of game

        void GameOver()
        {
            State = Phase.Over;
            ActiveTool = null;
            Aiming = false;
            SetStuck(false);
            GameScreen.I?.HideHint();
            GameScreen.I?.HideHand();
            StartGrayOut();
            GameScreen.I?.SetMood(CatMood.Sad);
            GameScreen.I?.SetFever(false);
            AudioManager.PlayMusic(Music.None);
            AudioManager.Play(Sfx.GameOver);
            Haptics.Tap();
            if (Mode == GameMode.Classic && !IsChallenge) { Profile.D.hasSaved = false; Profile.MarkDirty(); }
            Tweener.Delay(1.3f, () =>
            {
                if (State != Phase.Over) return;
                if (!Revived && Ads.CanReward(AdPlacement.Revive))
                    UIRoot.I.ShowRevive(Score, () => Ads.Rewarded(AdPlacement.Revive, ok => { if (ok) DoRevive(); else Finish(); }), Finish);
                else Finish();
            }, this);
        }

        void DoRevive()
        {
            Revived = true;
            State = Phase.Playing;
            StopGrayOut();
            Board.FullestLines(4, _fullest);
            _rows.Clear(); _cols.Clear();
            foreach (int line in _fullest) { if (line < Board.N) _rows.Add(line); else _cols.Add(line - Board.N); }
            Board.ClearLines(_rows, _cols, _cleared);
            foreach (var c in _cleared)
            {
                Fx.PopBlock(CellPos(c.X, c.Y), c.Color, c.Gem, Random.Range(0f, 0.25f));
                if (c.Gem > 0 && Goals[c.Gem] > 0) { Goals[c.Gem]--; _gems++; }
            }
            var small = new List<Shape>();
            foreach (var s in Shapes.All) if (s.Size <= 3 && Board.AnyFit(s)) small.Add(s);
            if (small.Count == 0) small.Add(Shapes.All[0]);
            var set = new Shape[3];
            for (int i = 0; i < 3; i++) set[i] = small[Random.Range(0, small.Count)];
            var pieces = _gen.ToPieces(set);
            for (int i = 0; i < 3; i++) Tray[i] = pieces[i];
            StartDealAnim();
            AudioManager.Play(Sfx.Revive);
            AudioManager.PlayMusic(Music.Game);
            GameScreen.I?.SetMood(CatMood.Happy);
            GameScreen.I?.Refresh();
            Track.Log("revive");
            if (GoalsDone) { Win(); return; }
            CheckStuck();
        }

        void CountGameStats()
        {
            var d = Profile.D;
            d.gamesPlayed++;
            d.totalLines += _lines;
            d.totalGems += _gems;
            d.bestCombo = Mathf.Max(d.bestCombo, MaxCombo);
            if (_gems > 0) Profile.MissionAdd(MissionKind.Gems, _gems);
        }

        void Finish()
        {
            if (State == Phase.Idle) return;
            CountGameStats();
            var d = Profile.D;
            if (Mode == GameMode.Classic)
            {
                bool newBest = Score > d.bestClassic;
                if (newBest) { d.bestClassic = Score; d.bestSeed = Seed; }
                long coins = Score / 50 + (newBest && Score > 0 ? 50 : 0);
                bool won = IsChallenge && Score > ChallengeTarget;
                if (won) { d.challengesWon++; coins += ChallengeWinCoins; }
                Profile.AddCoins(coins);
                Profile.MissionAdd(MissionKind.PlayClassic);
                if (Score >= 1500) Profile.MissionAdd(MissionKind.ClassicScore);
                if (!IsChallenge) Profile.D.hasSaved = false;
                Profile.Save();
                Track.Log("game_end", $"classic score={Score} lines={_lines} combo={MaxCombo} skills={SkillUses} challenge={ChallengeTarget}");
                UIRoot.I.ShowClassicResult(Score, d.bestClassic, newBest, coins, ChallengeTarget);
            }
            else
            {
                Profile.Save();
                Track.Log("game_end", $"{Mode} fail level={Level}");
                UIRoot.I.ShowLevelFailed(Mode, Level, Goals);
            }
        }

        void Win()
        {
            if (State == Phase.Won) return;
            State = Phase.Won;
            ActiveTool = null;
            Aiming = false;
            SetStuck(false);
            GameScreen.I?.HideHint();
            GameScreen.I?.HideHand();
            GameScreen.I?.SetMood(CatMood.Wow);
            AudioManager.PlayMusic(Music.None);
            AudioManager.Play(Sfx.Win);
            Fx.Confetti(BoardCenter + Vector2.up * 3f, 9f, 120, Theme.Colors);
            Tweener.Delay(1.4f, () =>
            {
                CountGameStats();
                if (Mode == GameMode.Daily)
                {
                    long coins = Profile.CompleteDaily();
                    Profile.Save();
                    Track.Log("daily_complete", "streak=" + Profile.D.dailyStreak);
                    UIRoot.I.ShowDailyComplete(coins, Profile.D.dailyStreak);
                }
                else
                {
                    long coins = 40 + 2 * Level;
                    Profile.AddCoins(coins);
                    bool firstClear = Level >= Profile.D.adventureLevel;
                    Profile.D.adventureLevel = Mathf.Max(Profile.D.adventureLevel, Level + 1);
                    int cat = firstClear ? Profile.CatForLevel(Level) : -1;
                    string catLine = null;
                    if (cat >= 0)
                    {
                        var r = Profile.GiveCat(cat);
                        catLine = r.IsNew ? $"새 고양이 {Pal.EulReul(Cats.Get(cat).Name)} 만났어요!" : CatUI.ResultTitle(r);
                    }
                    Profile.MissionAdd(MissionKind.AdventureClear);
                    Profile.Save();
                    Track.Log("level_complete", "level=" + Level);
                    UIRoot.I.ShowLevelComplete(Level, coins, cat, catLine);
                }
            }, this);
        }

        public const int ChallengeWinCoins = 100;

        /// <summary>Grants a doubled reward from the result popups.</summary>
        public void DoubleCoins(long coins) => Profile.AddCoins(coins);

        // ------------------------------------------------------------------ tutorial

        void ShowTutorialHint()
        {
            GameScreen.I?.ShowHint(_tutStep == 0 ? "조각을 빈칸으로 끌어다 놓아 줄을 채워 보세요" : "세로 줄도 채우면 사라져요!");
            Vector2 target = _tutStep == 0 ? CellPos(4, 3) : CellPos(2, 5) + new Vector2(0f, 0.5f);
            GameScreen.I?.ShowHand(WorldToScreen(SlotPos(1)), WorldToScreen(target));
        }

        void TutorialAfterPlace()
        {
            GameScreen.I?.HideHand();
            if (_lines > 0 && _tutStep == 0)
            {
                _tutStep = 1;
                Tweener.Delay(0.6f, () =>
                {
                    Board.CopyFrom(Levels.TutorialBoard(1));
                    for (int i = 0; i < 3; i++) Tray[i] = null;
                    Tray[1] = new Piece(Shapes.All[2], 3); // vertical 2-line
                    StartDealAnim();
                    ShowTutorialHint();
                }, this);
            }
            else if (_tutStep == 1 && _lines > 1)
            {
                GameScreen.I?.ShowHint("잘했어요! 이제 진짜 게임을 시작해요");
                Profile.D.tutorialDone = true;
                Profile.Save();
                Tweener.Delay(1.4f, () => { GameScreen.I?.HideHint(); StartClassic(false); }, this);
            }
            else
            {
                // the player placed the piece somewhere else: retry the step
                Tweener.Delay(0.4f, () =>
                {
                    Board.CopyFrom(Levels.TutorialBoard(_tutStep));
                    Tray[1] = _tutStep == 0 ? new Piece(Shapes.All[0], 5) : new Piece(Shapes.All[2], 3);
                    StartDealAnim();
                    ShowTutorialHint();
                }, this);
            }
        }

        // ------------------------------------------------------------------ classic suspend / resume

        public void SaveClassic()
        {
            if (Mode != GameMode.Classic || IsChallenge || State != Phase.Playing) return;
            var d = Profile.D;
            for (int i = 0; i < 64; i++) { d.savedColors[i] = Board.Color[i]; d.savedGems[i] = Board.Gem[i]; }
            for (int i = 0; i < 3; i++)
            {
                d.savedTray[i] = Tray[i] != null ? Tray[i].Shape.Id : -1;
                d.savedTrayColor[i] = Tray[i] != null ? Tray[i].Color : 0;
            }
            d.savedScore = Score; d.savedCombo = Combo; d.savedMiss = _miss;
            d.savedSeed = Seed; d.savedGauge = Gauge; d.savedSkillUses = SkillUses; d.savedPlaced = _placed;
            d.hasSaved = true;
            Profile.MarkDirty();
        }

        void RestoreClassic()
        {
            var d = Profile.D;
            for (int i = 0; i < 64; i++) { Board.Color[i] = (byte)d.savedColors[i]; Board.Gem[i] = (byte)d.savedGems[i]; }
            for (int i = 0; i < 3; i++) Tray[i] = d.savedTray[i] >= 0 ? new Piece(Shapes.Get(d.savedTray[i]), (byte)Mathf.Max(1, d.savedTrayColor[i])) : null;
            Score = d.savedScore; Combo = d.savedCombo; _miss = d.savedMiss;
            SkillUses = Mathf.Max(0, d.savedSkillUses);
            _placed = Mathf.Max(0, d.savedPlaced);
            Gauge = Mathf.Clamp(d.savedGauge, 0, GaugeNeed);
            if (TrayEmpty) Deal();
            StartDealAnim();
            if (!AnyTrayFits()) { Board.Clear(); Deal(); }
        }

        void OnApplicationPause(bool paused)
        {
            if (paused && Mode == GameMode.Classic && !IsChallenge && State == Phase.Playing) { SaveClassic(); Profile.Save(); }
        }
    }
}
