using System.Collections.Generic;
using UnityEngine;

namespace BlockMeow
{
    /// <summary>A block piece: a set of cells with (0,0) at the bottom-left of its bounding box.</summary>
    public sealed class Shape
    {
        public readonly int Id, W, H, Tier;
        public readonly float Weight;
        public readonly Vector2Int[] Cells;
        public int RotatedId = -1; // id of this shape rotated 90 degrees clockwise

        public Shape(int id, string pattern, int tier, float weight)
        {
            Id = id; Tier = tier; Weight = weight;
            var rows = pattern.Split('/');
            H = rows.Length;
            W = 0;
            foreach (var r in rows) W = Mathf.Max(W, r.Length);
            var list = new List<Vector2Int>();
            for (int row = 0; row < rows.Length; row++)
                for (int x = 0; x < rows[row].Length; x++)
                    if (rows[row][x] == 'X') list.Add(new Vector2Int(x, H - 1 - row));
            Cells = list.ToArray();
        }

        public int Size => Cells.Length;
    }

    public static class Shapes
    {
        public static readonly Shape[] All;

        static Shapes()
        {
            var defs = new (string pat, int tier, float w)[]
            {
                ("X", 0, 1.0f),
                ("XX", 0, 1.1f), ("X/X", 0, 1.1f),
                ("XXX", 1, 1.0f), ("X/X/X", 1, 1.0f),
                ("XXXX", 1, 0.8f), ("X/X/X/X", 1, 0.8f),
                ("XXXXX", 2, 0.6f), ("X/X/X/X/X", 2, 0.6f),
                ("XX/XX", 1, 1.2f),
                ("XXX/XXX/XXX", 2, 0.45f),
                ("XXX/XXX", 2, 0.6f), ("XX/XX/XX", 2, 0.6f),
                ("X./XX", 0, 0.8f), (".X/XX", 0, 0.8f), ("XX/X.", 0, 0.8f), ("XX/.X", 0, 0.8f),
                ("X../X../XXX", 2, 0.5f), ("..X/..X/XXX", 2, 0.5f), ("XXX/X../X..", 2, 0.5f), ("XXX/..X/..X", 2, 0.5f),
                ("X./X./XX", 1, 0.55f), (".X/.X/XX", 1, 0.55f), ("XX/X./X.", 1, 0.55f), ("XX/.X/.X", 1, 0.55f),
                ("XXX/X..", 1, 0.55f), ("XXX/..X", 1, 0.55f), ("X../XXX", 1, 0.55f), ("..X/XXX", 1, 0.55f),
                ("XXX/.X.", 1, 0.6f), (".X./XXX", 1, 0.6f), ("X./XX/X.", 1, 0.6f), (".X/XX/.X", 1, 0.6f),
                (".XX/XX.", 1, 0.45f), ("XX./.XX", 1, 0.45f), ("X./XX/.X", 1, 0.45f), (".X/XX/X.", 1, 0.45f),
            };
            All = new Shape[defs.Length];
            for (int i = 0; i < defs.Length; i++) All[i] = new Shape(i, defs[i].pat, defs[i].tier, defs[i].w);

            // precompute the clockwise rotation of each shape (used by the rotate booster)
            foreach (var s in All)
            {
                var rotated = new HashSet<Vector2Int>();
                foreach (var c in s.Cells) rotated.Add(new Vector2Int(c.y, s.W - 1 - c.x));
                foreach (var o in All)
                {
                    if (o.Size != s.Size || o.W != s.H || o.H != s.W) continue;
                    bool same = true;
                    foreach (var c in o.Cells) if (!rotated.Contains(c)) { same = false; break; }
                    if (same) { s.RotatedId = o.Id; break; }
                }
                if (s.RotatedId < 0) s.RotatedId = s.Id;
            }
        }

        public static Shape Get(int id) => All[Mathf.Clamp(id, 0, All.Length - 1)];
    }
}
