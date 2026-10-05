using System.Collections.Generic;

namespace BlockMeow
{
    public struct ClearedCell
    {
        public int X, Y;
        public byte Color, Gem;
    }

    /// <summary>A piece in the tray: a shape, a palette color and optional gems per cell (adventure).</summary>
    public sealed class Piece
    {
        public Shape Shape;
        public byte Color;    // 1..8
        public byte[] Gems;   // per cell of Shape.Cells, 0 = none

        public Piece(Shape shape, byte color, byte[] gems = null)
        {
            Shape = shape;
            Color = color;
            Gems = gems ?? new byte[shape.Size];
        }

        public bool HasGems
        {
            get { foreach (var g in Gems) if (g != 0) return true; return false; }
        }

        public void Rotate()
        {
            var rotated = Shapes.Get(Shape.RotatedId);
            if (rotated == Shape) return;
            // carry gems over to the rotated cell positions
            var newGems = new byte[rotated.Size];
            for (int i = 0; i < Shape.Cells.Length; i++)
            {
                if (Gems[i] == 0) continue;
                var c = Shape.Cells[i];
                var target = new UnityEngine.Vector2Int(c.y, Shape.W - 1 - c.x);
                for (int j = 0; j < rotated.Cells.Length; j++)
                    if (rotated.Cells[j] == target) { newGems[j] = Gems[i]; break; }
            }
            Shape = rotated;
            Gems = newGems;
        }
    }

    /// <summary>8x8 grid model: placement, line detection and clearing. No Unity dependencies.</summary>
    public sealed class Board
    {
        public const int N = 8;
        public readonly byte[] Color = new byte[N * N]; // 0 = empty, 1..8 palette slot
        public readonly byte[] Gem = new byte[N * N];   // 0 = none, 1 heart, 2 star, 3 diamond

        static int Idx(int x, int y) => y * N + x;
        public bool Filled(int x, int y) => Color[Idx(x, y)] != 0;
        public bool In(int x, int y) => x >= 0 && y >= 0 && x < N && y < N;

        public void Clear()
        {
            for (int i = 0; i < Color.Length; i++) { Color[i] = 0; Gem[i] = 0; }
        }

        public void CopyFrom(Board o)
        {
            System.Array.Copy(o.Color, Color, Color.Length);
            System.Array.Copy(o.Gem, Gem, Gem.Length);
        }

        public int Count()
        {
            int n = 0;
            foreach (var c in Color) if (c != 0) n++;
            return n;
        }

        public float Fill01 => Count() / (float)(N * N);

        public int GemCount(int kind)
        {
            int n = 0;
            for (int i = 0; i < Gem.Length; i++) if (Color[i] != 0 && (kind == 0 ? Gem[i] != 0 : Gem[i] == kind)) n++;
            return n;
        }

        public bool CanPlace(Shape s, int ox, int oy)
        {
            if (ox < 0 || oy < 0 || ox + s.W > N || oy + s.H > N) return false;
            foreach (var c in s.Cells)
                if (Color[Idx(ox + c.x, oy + c.y)] != 0) return false;
            return true;
        }

        public bool AnyFit(Shape s)
        {
            for (int y = 0; y <= N - s.H; y++)
                for (int x = 0; x <= N - s.W; x++)
                    if (CanPlace(s, x, y)) return true;
            return false;
        }

        public void Place(Piece p, int ox, int oy)
        {
            var cells = p.Shape.Cells;
            for (int i = 0; i < cells.Length; i++)
            {
                int k = Idx(ox + cells[i].x, oy + cells[i].y);
                Color[k] = p.Color;
                Gem[k] = p.Gems[i];
            }
        }

        public void FindFullLines(List<int> rows, List<int> cols)
        {
            rows.Clear(); cols.Clear();
            for (int y = 0; y < N; y++)
            {
                bool full = true;
                for (int x = 0; x < N && full; x++) if (Color[Idx(x, y)] == 0) full = false;
                if (full) rows.Add(y);
            }
            for (int x = 0; x < N; x++)
            {
                bool full = true;
                for (int y = 0; y < N && full; y++) if (Color[Idx(x, y)] == 0) full = false;
                if (full) cols.Add(x);
            }
        }

        /// <summary>Removes the given rows and columns; intersections are reported once.</summary>
        public void ClearLines(List<int> rows, List<int> cols, List<ClearedCell> cleared)
        {
            cleared.Clear();
            var mark = new bool[N * N];
            foreach (var y in rows) for (int x = 0; x < N; x++) mark[Idx(x, y)] = true;
            foreach (var x in cols) for (int y = 0; y < N; y++) mark[Idx(x, y)] = true;
            for (int y = 0; y < N; y++)
                for (int x = 0; x < N; x++)
                {
                    int k = Idx(x, y);
                    if (!mark[k] || Color[k] == 0) continue;
                    cleared.Add(new ClearedCell { X = x, Y = y, Color = Color[k], Gem = Gem[k] });
                    Color[k] = 0;
                    Gem[k] = 0;
                }
        }

        readonly bool[] _scratch = new bool[N * N];

        /// <summary>How many lines a placement at (ox, oy) would complete (without modifying the board).</summary>
        public int LinesIfPlaced(Shape s, int ox, int oy)
        {
            for (int i = 0; i < _scratch.Length; i++) _scratch[i] = Color[i] != 0;
            foreach (var c in s.Cells) _scratch[Idx(ox + c.x, oy + c.y)] = true;
            int lines = 0;
            for (int y = 0; y < N; y++)
            {
                bool full = true;
                for (int x = 0; x < N && full; x++) if (!_scratch[Idx(x, y)]) full = false;
                if (full) lines++;
            }
            for (int x = 0; x < N; x++)
            {
                bool full = true;
                for (int y = 0; y < N && full; y++) if (!_scratch[Idx(x, y)]) full = false;
                if (full) lines++;
            }
            return lines;
        }

        /// <summary>Best number of lines the shape could clear anywhere on the board (0 when it cannot clear).</summary>
        public int BestClear(Shape s)
        {
            int best = 0;
            for (int y = 0; y <= N - s.H; y++)
                for (int x = 0; x <= N - s.W; x++)
                    if (CanPlace(s, x, y)) best = System.Math.Max(best, LinesIfPlaced(s, x, y));
            return best;
        }

        /// <summary>Removes the given cells (board indices); reports what was removed.</summary>
        public void ClearCells(IList<int> cells, List<ClearedCell> cleared)
        {
            cleared.Clear();
            foreach (int k in cells)
            {
                if (k < 0 || k >= Color.Length || Color[k] == 0) continue;
                cleared.Add(new ClearedCell { X = k % N, Y = k / N, Color = Color[k], Gem = Gem[k] });
                Color[k] = 0;
                Gem[k] = 0;
            }
        }

        /// <summary>Drops blocks straight down, each at most maxDrop cells. moves receives (fromIndex, toIndex) for animation.</summary>
        public void ApplyGravity(List<(int from, int to)> moves, int maxDrop = N)
        {
            moves.Clear();
            for (int x = 0; x < N; x++)
            {
                int floor = 0; // lowest free row a falling block may reach
                for (int y = 0; y < N; y++)
                {
                    int k = Idx(x, y);
                    if (Color[k] == 0) continue;
                    int target = System.Math.Max(floor, y - maxDrop);
                    if (target != y)
                    {
                        int t = Idx(x, target);
                        Color[t] = Color[k]; Gem[t] = Gem[k];
                        Color[k] = 0; Gem[k] = 0;
                        moves.Add((k, t));
                    }
                    floor = target + 1;
                }
            }
        }

        /// <summary>Palette slots ordered by how many blocks use them (most first).</summary>
        public List<byte> ColorsByCount()
        {
            var counts = new int[10];
            foreach (var c in Color) if (c != 0 && c < counts.Length) counts[c]++;
            var list = new List<byte>();
            for (byte c = 1; c < counts.Length; c++) if (counts[c] > 0) list.Add(c);
            list.Sort((a, b) => counts[b].CompareTo(counts[a]));
            return list;
        }

        /// <summary>The fullest rows (or columns) that are not empty, most filled first.</summary>
        public void FullestOf(bool rows, int count, List<int> result)
        {
            result.Clear();
            var score = new List<(int line, int filled)>();
            for (int i = 0; i < N; i++)
            {
                int f = 0;
                for (int j = 0; j < N; j++) if (Color[rows ? Idx(j, i) : Idx(i, j)] != 0) f++;
                if (f > 0) score.Add((i, f));
            }
            score.Sort((a, b) => b.filled.CompareTo(a.filled));
            for (int i = 0; i < count && i < score.Count; i++) result.Add(score[i].line);
        }

        /// <summary>Indices of the most filled rows (0..7) and columns (8..15) - used by the revive.</summary>
        public void FullestLines(int count, List<int> result)
        {
            result.Clear();
            var score = new List<(int line, int filled)>();
            for (int y = 0; y < N; y++)
            {
                int f = 0;
                for (int x = 0; x < N; x++) if (Color[Idx(x, y)] != 0) f++;
                score.Add((y, f));
            }
            for (int x = 0; x < N; x++)
            {
                int f = 0;
                for (int y = 0; y < N; y++) if (Color[Idx(x, y)] != 0) f++;
                score.Add((N + x, f));
            }
            score.Sort((a, b) => b.filled.CompareTo(a.filled));
            for (int i = 0; i < count && i < score.Count; i++) if (score[i].filled > 0) result.Add(score[i].line);
        }
    }
}
