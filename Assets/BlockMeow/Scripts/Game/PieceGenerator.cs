using System.Collections.Generic;
using UnityEngine;

namespace BlockMeow
{
    /// <summary>
    /// Deals three pieces at a time. Guarantees at least one piece fits, and with a "help" probability
    /// slips in a piece that can complete a line - the main lever for the satisfying-but-fair feel.
    /// </summary>
    public sealed class PieceGenerator
    {
        readonly System.Random _rng;
        readonly List<Shape> _cands = new List<Shape>();
        readonly List<float> _w = new List<float>();

        /// <summary>Chance that a dealt piece carries a gem (adventure / daily).</summary>
        public float GemChance;
        /// <summary>Gem kinds that may appear in pieces (1 heart, 2 star, 3 diamond).</summary>
        public readonly List<byte> GemKinds = new List<byte>();

        public PieceGenerator(int seed) { _rng = new System.Random(seed); }

        public float Value() => (float)_rng.NextDouble();
        public int Range(int min, int maxExclusive) => _rng.Next(min, maxExclusive);

        float Weight(Shape s, float d)
        {
            float tier = s.Tier == 0 ? 1.25f - 0.55f * d : s.Tier == 1 ? 1f : 0.45f + 1.1f * d;
            return s.Weight * tier;
        }

        Shape Pick(float d)
        {
            float total = 0f;
            foreach (var s in Shapes.All) total += Weight(s, d);
            float r = Value() * total;
            foreach (var s in Shapes.All)
            {
                r -= Weight(s, d);
                if (r <= 0f) return s;
            }
            return Shapes.All[0];
        }

        Shape PickHelper(Board b, float d)
        {
            _cands.Clear(); _w.Clear();
            foreach (var s in Shapes.All)
            {
                int lines = b.BestClear(s);
                if (lines <= 0) continue;
                _cands.Add(s);
                _w.Add(Weight(s, d) * (1f + lines));
            }
            if (_cands.Count == 0) return null;
            float total = 0f;
            foreach (var w in _w) total += w;
            float r = Value() * total;
            for (int i = 0; i < _cands.Count; i++)
            {
                r -= _w[i];
                if (r <= 0f) return _cands[i];
            }
            return _cands[_cands.Count - 1];
        }

        public Piece[] Deal(Board b, float difficulty, float help)
        {
            float fill = b.Fill01;
            if (fill > 0.6f) { help += 0.25f; difficulty -= 0.3f; }
            difficulty = Mathf.Clamp01(difficulty);
            var set = new Shape[3];
            bool ok = false;
            for (int attempt = 0; attempt < 40 && !ok; attempt++)
            {
                for (int i = 0; i < 3; i++) set[i] = Pick(difficulty);
                bool anyFit = false;
                foreach (var s in set) if (b.AnyFit(s)) { anyFit = true; break; }
                if (!anyFit) continue;
                ok = true;
                if (Value() < help)
                {
                    bool anyClear = false;
                    foreach (var s in set) if (b.BestClear(s) > 0) { anyClear = true; break; }
                    if (!anyClear)
                    {
                        var helper = PickHelper(b, difficulty);
                        if (helper != null) set[Range(0, 3)] = helper;
                    }
                }
            }
            if (!ok)
            {
                // the board is nearly full: hand out the smallest pieces that still fit
                var small = new List<Shape>();
                foreach (var s in Shapes.All) if (s.Size <= 3 && b.AnyFit(s)) small.Add(s);
                if (small.Count == 0) small.Add(Shapes.All[0]);
                for (int i = 0; i < 3; i++) set[i] = small[Range(0, small.Count)];
            }
            return ToPieces(set);
        }

        public Piece[] ToPieces(Shape[] set)
        {
            var pieces = new Piece[set.Length];
            var used = new HashSet<int>();
            for (int i = 0; i < set.Length; i++)
            {
                int color;
                int guard = 0;
                do { color = Range(1, 9); } while (used.Contains(color) && guard++ < 10);
                used.Add(color);
                var p = new Piece(set[i], (byte)color);
                if (GemKinds.Count > 0 && Value() < GemChance)
                    p.Gems[Range(0, p.Shape.Size)] = GemKinds[Range(0, GemKinds.Count)];
                pieces[i] = p;
            }
            return pieces;
        }
    }
}
