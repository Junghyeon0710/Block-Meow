using System.Collections;
using UnityEngine;

namespace BlockMeow
{
    /// <summary>Editor/dev helpers: a greedy bot used for automated play-tests and balancing.</summary>
    public sealed partial class GameSession
    {
        /// <summary>Moves made by the last <see cref="DebugAutoPlay"/> run.</summary>
        public int DebugMoves { get; private set; }
        public bool DebugRunning { get; private set; }

        /// <summary>Plays one greedy move (prefers clears, then compact placements). Returns false when no move exists.</summary>
        public bool DebugAutoMove()
        {
            if (!DebugBestMove(out int slot, out int x, out int y)) return false;
            Place(slot, x, y);
            return true;
        }

        /// <summary>The greedy bot's choice: tray slot and the board cell for the piece's bottom-left corner.</summary>
        public bool DebugBestMove(out int bestSlot, out int bx, out int by)
        {
            bestSlot = -1; bx = 0; by = 0;
            if (State != Phase.Playing || Busy) return false;
            float best = float.MinValue;
            for (int slot = 0; slot < 3; slot++)
            {
                var p = Tray[slot];
                if (p == null) continue;
                var s = p.Shape;
                for (int y = 0; y <= Board.N - s.H; y++)
                    for (int x = 0; x <= Board.N - s.W; x++)
                    {
                        if (!Board.CanPlace(s, x, y)) continue;
                        float score = Board.LinesIfPlaced(s, x, y) * 100f + s.Size * 2f;
                        // prefer hugging walls and existing blocks
                        foreach (var c in s.Cells)
                        {
                            int cx = x + c.x, cy = y + c.y;
                            score += Neighbor(cx - 1, cy) + Neighbor(cx + 1, cy) + Neighbor(cx, cy - 1) + Neighbor(cx, cy + 1);
                        }
                        score += Random.value * 0.5f;
                        if (score > best) { best = score; bestSlot = slot; bx = x; by = y; }
                    }
            }
            return bestSlot >= 0;
        }

        /// <summary>Screen point of a tray slot (where a finger picks the piece up).</summary>
        public Vector2 DebugSlotScreen(int slot) => WorldToScreen(SlotPos(slot));

        /// <summary>Screen point where the finger must let go so the slot's piece lands with its corner on (x, y).</summary>
        public Vector2 DebugDropScreen(int slot, int x, int y)
        {
            var s = Tray[slot].Shape;
            var center = _origin + new Vector2(x + (s.W - 1) * 0.5f, y + (s.H - 1) * 0.5f);
            return WorldToScreen(center - Vector2.up * Lift(Tray[slot]));
        }

        /// <summary>Fills the skill gauge up to <paramref name="missing"/> lines short of ready.</summary>
        public void DebugChargeSkill(int missing = 0)
        {
            Gauge = Mathf.Clamp(GaugeNeed - missing, 0, GaugeNeed);
            GameScreen.I?.Refresh();
        }

        float Neighbor(int x, int y) => !Board.In(x, y) || Board.Filled(x, y) ? 1f : 0f;

        /// <summary>Fires the partner skill when it is ready and worth it (crowded board, stuck, or a buff skill).</summary>
        public bool DebugUseSkill()
        {
            if (State != Phase.Playing || Busy || !SkillReady) return false;
            var s = Partner.Skill;
            bool worth = Stuck || Board.Fill01 > 0.42f || s == SkillType.Nap || s == SkillType.LuckyBell;
            if (!worth || !SkillUseful(s)) return false;
            if (s != SkillType.Punch) { FireSkill(); return true; }
            // aim the punch where it removes the most blocks
            int bestX = 0, bestY = 0, most = -1;
            for (int y = 0; y < Board.N; y++)
                for (int x = 0; x < Board.N; x++)
                {
                    int n = 0;
                    for (int dy = -2; dy <= 2; dy++)
                        for (int dx = -2; dx <= 2; dx++)
                        {
                            bool inside = SkillStrong ? Mathf.Abs(dx) + Mathf.Abs(dy) <= 2 : Mathf.Abs(dx) <= 1 && Mathf.Abs(dy) <= 1;
                            if (inside && Board.In(x + dx, y + dy) && Board.Filled(x + dx, y + dy)) n++;
                        }
                    if (n > most) { most = n; bestX = x; bestY = y; }
                }
            FireSkill();
            if (Aiming) PunchAt(bestX, bestY);
            return true;
        }

        /// <summary>Plays greedy moves over time (uses the skill, gives up when stuck) for automated play-tests.</summary>
        public void DebugAutoPlay(int maxMoves = 500, float interval = 0.05f)
        {
            if (DebugRunning) return;
            StartCoroutine(AutoPlay(maxMoves, interval));
        }

        public string DebugReport { get; private set; }

        /// <summary>Balance sim: several bot games per skill (common cat at the trial level) plus a no-skill baseline.</summary>
        public void DebugBalance(int gamesPerCat = 4, int maxMoves = 700)
        {
            if (DebugRunning) return;
            StartCoroutine(Balance(gamesPerCat, maxMoves));
        }

        IEnumerator Balance(int games, int maxMoves)
        {
            DebugRunning = true;
            var sb = new System.Text.StringBuilder();
            var cats = new System.Collections.Generic.List<int> { -1 };
            foreach (SkillType sk in System.Enum.GetValues(typeof(SkillType)))
                cats.Add(System.Array.FindIndex(Cats.All, c => c.Skill == sk && c.Rarity == CatRarity.Common));
            int keepPartner = Profile.D.mascot;
            foreach (int cat in cats)
            {
                long score = 0;
                int lines = 0, moves = 0, uses = 0, capped = 0;
                var perGame = new System.Collections.Generic.List<int>();
                var perScore = new System.Collections.Generic.List<int>();
                for (int g = 0; g < games; g++)
                {
                    UIRoot.I.CloseAll();
                    Profile.D.trialCat = cat;
                    StartClassic(false);
                    // every setup plays the same seeds (and bot tie-breaks), so differences come from the skill
                    Random.InitState(9000 + g);
                    Seed = 1000 + g;
                    _gen = new PieceGenerator(Seed);
                    Board.Clear();
                    Deal();
                    int m = 0;
                    while (State == Phase.Playing && m < maxMoves)
                    {
                        // several moves per frame; frames only matter for the knead drop
                        for (int k = 0; k < 8 && State == Phase.Playing && !Busy && m < maxMoves; k++)
                        {
                            if (cat >= 0 && DebugUseSkill()) continue;
                            if (DebugAutoMove()) { m++; continue; }
                            if (Stuck) GiveUp();
                            break;
                        }
                        yield return null;
                    }
                    if (State == Phase.Playing) capped++;
                    score += Score; lines += _lines; moves += m; uses += SkillUses;
                    perGame.Add(m);
                    perScore.Add(Score);
                }
                perGame.Sort();
                perScore.Sort();
                string name = cat >= 0 ? $"{Cats.All[cat].Name}/{Cats.All[cat].Skill}" : "no skill";
                sb.AppendLine($"{name}: score avg {score / games} med {perScore[games / 2]}, moves avg {moves / games} med {perGame[games / 2]}, lines {lines / games}, skills {uses / (float)games:0.0}, capped {capped}/{games}");
            }
            Profile.D.mascot = keepPartner;
            Profile.D.hasSaved = false;
            State = Phase.Idle;
            UIRoot.I.CloseAll();
            SetWorldVisible(false);
            GameApp.I.GoHome();
            DebugReport = sb.ToString();
            Debug.Log("[Balance]\n" + DebugReport);
            DebugRunning = false;
        }

        IEnumerator AutoPlay(int maxMoves, float interval)
        {
            DebugRunning = true;
            DebugMoves = 0;
            var wait = new WaitForSecondsRealtime(interval);
            while (DebugMoves < maxMoves && State == Phase.Playing)
            {
                yield return wait;
                if (Busy || Paused || UIRoot.I.HasPopup) continue;
                if (DebugUseSkill()) continue;
                if (DebugAutoMove()) { DebugMoves++; continue; }
                if (Stuck) GiveUp();
            }
            DebugRunning = false;
        }
    }
}
