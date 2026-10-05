using System.Collections.Generic;
using UnityEngine;

namespace BlockMeow
{
    public sealed class LevelDef
    {
        public int Number;
        public string Picture;
        public readonly Board Start = new Board();
        public readonly int[] Goals = new int[4]; // index = gem kind (1..3)
        public float PieceGemChance;
        public readonly List<byte> PieceGemKinds = new List<byte>();
        public int Seed;
        public float Difficulty;

        public int TotalGoal => Goals[1] + Goals[2] + Goals[3];
    }

    /// <summary>Adventure levels and daily puzzles, generated from 8x8 pixel pictures and a seed.</summary>
    public static class Levels
    {
        // 'a','b','c' = colored blocks of the picture, '.' = empty
        static readonly (string name, string[] rows)[] Pictures =
        {
            ("하트", new[] { "........", ".aa..aa.", "aaaaaaaa", "aaaaaaaa", ".aaaaaa.", "..aaaa..", "...aa...", "........" }),
            ("다이아몬드", new[] { "...aa...", "..aaaa..", ".aaaaaa.", "aaaaaaaa", ".aaaaaa.", "..aaaa..", "...aa...", "........" }),
            ("별", new[] { "...aa...", "...aa...", "aaaaaaaa", ".aaaaaa.", "..aaaa..", ".aa..aa.", ".a....a.", "........" }),
            ("고양이", new[] { "a......a", "aa....aa", "aaaaaaaa", "abaaaaba", "aaaaaaaa", "aaaccaaa", ".aaaaaa.", "..aaaa.." }),
            ("발바닥", new[] { "..a..a..", ".aa..aa.", "........", "a..aa..a", "..aaaa..", ".aaaaaa.", ".aaaaaa.", "..aaaa.." }),
            ("물고기", new[] { "........", "..aaa..b", ".aaaaabb", "aabaaabb", ".aaaaabb", "..aaa..b", "........", "........" }),
            ("스마일", new[] { "..aaaa..", ".aaaaaa.", "aabaabaa", "aaaaaaaa", "abaaaaba", "aabbbbaa", ".aaaaaa.", "..aaaa.." }),
            ("집", new[] { "...aa...", "..aaaa..", ".aaaaaa.", "aaaaaaaa", ".bbbbbb.", ".bccbbb.", ".bccbbb.", ".bbbbbb." }),
            ("나무", new[] { "...aa...", "..aaaa..", ".aaaaaa.", "..aaaa..", ".aaaaaa.", "aaaaaaaa", "...bb...", "...bb..." }),
            ("꽃", new[] { "..a..a..", ".aaaaaa.", "..abba..", ".aabbaa.", "..aaaa..", "...cc...", ".cccc...", "...cc..." }),
            ("달", new[] { "..aaaa..", ".aaa....", "aaa.....", "aaa.....", "aaa.....", "aaa.....", ".aaa....", "..aaaa.." }),
            ("버섯", new[] { "..aaaa..", ".abaaba.", "aaaaaaaa", "abaaaaba", "........", "..bbbb..", "..bbbb..", "..bbbb.." }),
            ("유령", new[] { "..aaaa..", ".aaaaaa.", "aabaabaa", "aaaaaaaa", "aaaaaaaa", "aaaaaaaa", "aa.aa.aa", "a..a..a." }),
            ("왕관", new[] { "a..aa..a", "aa.aa.aa", "aaaaaaaa", "abaaaaba", "aaaaaaaa", "........", "........", "........" }),
            ("로켓", new[] { "...aa...", "..aaaa..", "..abba..", "..aaaa..", "..aaaa..", ".aaaaaa.", ".a.cc.a.", "...cc..." }),
            ("체리", new[] { "....bbb.", "...b...b", "..b....b", ".aa...aa", "aaaa.aaa", "aaaa.aaa", ".aa...a.", "........" }),
            ("화살표", new[] { "...aa...", "..aaaa..", ".aaaaaa.", "aaaaaaaa", "...aa...", "...aa...", "...aa...", "...aa..." }),
            ("십자", new[] { "...aa...", "...aa...", "...aa...", "aaaaaaaa", "aaaaaaaa", "...aa...", "...aa...", "...aa..." }),
            ("체크무늬", new[] { "a.a.a.a.", ".a.a.a.a", "a.a.a.a.", ".a.a.a.a", "a.a.a.a.", ".a.a.a.a", "a.a.a.a.", ".a.a.a.a" }),
            ("액자", new[] { "aaaaaaaa", "a......a", "a.bbbb.a", "a.b..b.a", "a.b..b.a", "a.bbbb.a", "a......a", "aaaaaaaa" }),
            ("계단", new[] { "a.......", "aa......", "aaa.....", "aaaa....", "aaaaa...", "aaaaaa..", "aaaaaaa.", "........" }),
            ("음표", new[] { "...aaaaa", "...a...a", "...a...a", "...a...a", ".aaa.aaa", "aaaa.aaa", ".aa...a.", "........" }),
            ("컵", new[] { "aaaaaaa.", "aaaaaaaa", "aaaaaa.a", "aaaaaa.a", "aaaaaaaa", ".aaaaa..", "..aaa...", ".aaaaa.." }),
            ("우주선", new[] { "...aa...", "..abba..", ".aaaaaa.", "aaaaaaaa", "a.aaaa.a", "...cc...", "..c..c..", "........" }),
        };

        public static int PictureCount => Pictures.Length;

        public static LevelDef Adventure(int n)
        {
            n = Mathf.Max(1, n);
            float d = Mathf.Clamp01((n - 1) / 40f);
            int picture = (n - 1) % Pictures.Length;
            int boardGems = Mathf.Clamp(3 + n / 3, 3, 12);
            int extra = n >= 11 ? Mathf.Min(8, n / 6) : 0;
            int kinds = n <= 5 ? 1 : n <= 15 ? 2 : 3;
            var lv = Build(n * 7919 + 13, picture, Mathf.Lerp(0.6f, 0.88f, d), boardGems, kinds, extra, n >= 11 ? Mathf.Min(0.3f, 0.12f + 0.004f * n) : 0f);
            lv.Number = n;
            lv.Difficulty = d;
            return lv;
        }

        public static LevelDef Daily(int dayIndex)
        {
            var rng = new System.Random(dayIndex * 104729 + 7);
            var lv = Build(dayIndex * 31337 + 1, rng.Next(Pictures.Length), 0.66f, 8, 3, 3, 0.2f);
            lv.Number = 0;
            lv.Difficulty = 0.55f;
            return lv;
        }

        static LevelDef Build(int seed, int picture, float keep, int gems, int kinds, int extra, float pieceGemChance)
        {
            var rng = new System.Random(seed);
            var lv = new LevelDef { Seed = seed, Picture = Pictures[picture].name, PieceGemChance = pieceGemChance };
            var rows = Pictures[picture].rows;
            bool mirror = rng.NextDouble() < 0.5;

            // three distinct palette slots for the picture's a/b/c colors
            var slots = new List<byte> { 1, 2, 3, 4, 5, 6, 7, 8 };
            var abc = new byte[3];
            for (int i = 0; i < 3; i++) { int k = rng.Next(slots.Count); abc[i] = slots[k]; slots.RemoveAt(k); }

            var filled = new List<int>();
            var b = lv.Start;
            for (int row = 0; row < Board.N; row++)
            {
                string r = rows[row];
                for (int col = 0; col < Board.N; col++)
                {
                    char ch = r[col];
                    if (ch == '.') continue;
                    if (rng.NextDouble() > keep) continue;
                    int x = mirror ? Board.N - 1 - col : col, y = Board.N - 1 - row;
                    b.Color[y * Board.N + x] = abc[ch == 'b' ? 1 : ch == 'c' ? 2 : 0];
                    filled.Add(y * Board.N + x);
                }
            }

            // never start with a completed line
            var fullRows = new List<int>(); var fullCols = new List<int>();
            b.FindFullLines(fullRows, fullCols);
            foreach (var y in fullRows) { int x = rng.Next(Board.N); b.Color[y * Board.N + x] = 0; filled.Remove(y * Board.N + x); }
            foreach (var x in fullCols) { int y = rng.Next(Board.N); b.Color[y * Board.N + x] = 0; filled.Remove(y * Board.N + x); }

            // sprinkle gems on picture cells, kinds spread evenly
            gems = Mathf.Min(gems, filled.Count);
            for (int i = 0; i < gems; i++)
            {
                int k = rng.Next(filled.Count);
                int cell = filled[k];
                filled.RemoveAt(k);
                byte kind = (byte)(1 + i % kinds);
                b.Gem[cell] = kind;
                lv.Goals[kind]++;
            }
            for (int i = 0; i < extra; i++) lv.Goals[1 + i % kinds]++;
            for (byte k = 1; k <= kinds; k++) lv.PieceGemKinds.Add(k);
            return lv;
        }

        /// <summary>Scripted first-time board: one row with a single gap, then a column with a two-cell gap.</summary>
        public static Board TutorialBoard(int step)
        {
            var b = new Board();
            if (step == 0)
            {
                for (int x = 0; x < Board.N; x++) if (x != 4) b.Color[3 * Board.N + x] = (byte)(1 + x % 8);
            }
            else
            {
                for (int y = 0; y < Board.N; y++) if (y != 5 && y != 6) b.Color[y * Board.N + 2] = (byte)(1 + y % 8);
                for (int x = 4; x < 7; x++) b.Color[0 * Board.N + x] = 5;
            }
            return b;
        }
    }
}
