using System.Collections.Generic;
using UnityEngine;

namespace BlockMeow
{
    /// <summary>Skill icons (one per cat skill), the cat box and the share/paste glyphs.</summary>
    public static partial class Atlas
    {
        static float Zed(float x, float y, float cx, float cy, float s, float w)
            => S.U(S.Seg(x, y, cx - s, cy + s, cx + s, cy + s, w), S.Seg(x, y, cx + s, cy + s, cx - s, cy - s, w), S.Seg(x, y, cx - s, cy - s, cx + s, cy - s, w));

        /// <summary>Paw print: main pad plus four toes, centered on (cx, cy) with scale k (1 = about 90 px wide).</summary>
        static Sdf Paw(float cx, float cy, float k) => (x, y) => S.U(
            S.Ellipse(x, y, cx, cy - 12 * k, 30 * k, 25 * k),
            S.U(S.Ellipse(x, y, cx - 32 * k, cy + 16 * k, 11 * k, 13 * k), S.Ellipse(x, y, cx - 12 * k, cy + 34 * k, 11 * k, 13 * k)),
            S.U(S.Ellipse(x, y, cx + 12 * k, cy + 34 * k, 11 * k, 13 * k), S.Ellipse(x, y, cx + 32 * k, cy + 16 * k, 11 * k, 13 * k)));

        static void Beans(PixelCanvas c, float cx, float cy, float k, Color col)
        {
            c.Fill((x, y) => S.Ellipse(x, y, cx, cy - 14 * k, 17 * k, 13 * k), col);
            foreach (var (dx, dy) in new[] { (-32f, 16f), (-12f, 34f), (12f, 34f), (32f, 16f) })
                c.Fill((x, y) => S.Ellipse(x, y, cx + dx * k, cy + dy * k, 6 * k, 7 * k), col);
        }

        static void DefineSkillIcons()
        {
            var bean = Pal.Hex("#FF8FB8");

            I("sk_punch", c =>
            {
                c.Glow(P(S.Star(64, 64, 58, 36, 10, 90)), Pal.Yellow.WithA(0.5f), 8f, 2f, false);
                c.Fill(P(S.Star(64, 64, 58, 36, 10, 90)), Pal.Hex("#FFD23F"));
                c.Fill(P(S.Star(64, 64, 44, 28, 10, 108)), Pal.Hex("#FFF1A8"));
                Icon(c, Paw(64, 60, 0.82f), Pal.Hex("#FFF8F0"), 0f);
                Beans(c, 64, 60, 0.82f, bean);
            });

            I("sk_row", c =>
            {
                var col = Pal.Hex("#FFB35C");
                for (int i = 0; i < 3; i++)
                {
                    float ly = 38 + i * 22, len = 26 - i * 6;
                    c.Fill((x, y) => S.Seg(x, y, 8, ly, 8 + len, ly, 4.5f), InkC);
                }
                var pts = new List<Vector2>();
                for (int i = 0; i <= 28; i++)
                {
                    float t = i / 28f;
                    pts.Add(new Vector2(36 + t * 66, 36 + Mathf.Sin(t * Mathf.PI) * 18f + t * t * 50f));
                }
                Sdf tail = Lines(pts, 12f);
                Icon(c, tail, col);
                // tabby stripes across the tail
                for (int i = 1; i <= 3; i++)
                {
                    var p = pts[i * 7];
                    var q = pts[i * 7 + 1];
                    var n = new Vector2(-(q.y - p.y), q.x - p.x).normalized * 11f;
                    c.Fill((x, y) => S.Seg(x, y, p.x - n.x, p.y - n.y, p.x + n.x, p.y + n.y, 3.5f), Pal.Hex("#E8862A"));
                }
            });

            I("sk_col", c =>
            {
                // three paw stamps marching down a column
                var col = Pal.Hex("#FF5A7A");
                c.Fill((x, y) => S.Box(x, y, 64, 64, 22, 58, 10), A(0.18f));
                for (int i = 0; i < 3; i++)
                {
                    float py = 100 - i * 36, px = 64 + (i % 2 == 0 ? -6 : 6);
                    Icon(c, Paw(px, py, 0.36f), col, 0f);
                }
            });

            I("sk_knead", c =>
            {
                var bc = new[] { Pal.Hex("#FF5A7A"), Pal.Hex("#FFD93D"), Pal.Hex("#4D96FF") };
                for (int i = 0; i < 3; i++)
                {
                    float bx = 30 + i * 34;
                    var col = bc[i];
                    c.Fill((x, y) => S.Box(x, y, bx, 20, 15, 14, 5), col.Mul(0.7f));
                    c.Fill((x, y) => S.Box(x, y, bx, 22, 14, 12, 5), col);
                }
                c.Fill((x, y) => S.U(S.Seg(x, y, 16, 62, 16, 82, 3.5f), S.Seg(x, y, 112, 62, 112, 82, 3.5f)), InkC);
                Icon(c, Paw(64, 78, 0.68f), Pal.Hex("#E9D5FF"), 0.03f);
                Beans(c, 64, 78, 0.68f, Pal.Hex("#C77DFF"));
            });

            I("sk_yarn", c =>
            {
                var col = Pal.Hex("#FF6FB5");
                var thread = new List<Vector2> { new Vector2(90, 38), new Vector2(106, 26), new Vector2(96, 14), new Vector2(116, 8) };
                c.Fill(Lines(thread, 3.5f), col.Mul(0.75f));
                Icon(c, (x, y) => S.Circle(x, y, 58, 68, 42), col);
                Sdf wraps = (x, y) => S.I(
                    S.U(S.Ring(S.Circle(x, y, 26, 100, 50), 3f), S.Ring(S.Circle(x, y, 96, 34, 46), 3f), S.Ring(S.Circle(x, y, 18, 40, 52), 3f)),
                    S.Circle(x, y, 58, 68, 39));
                c.Fill(wraps, col.Mul(0.68f));
                c.Fill((x, y) => S.Ellipse(x, y, 44, 88, 12, 6), A(0.45f), 2f);
            });

            I("sk_fish", c =>
            {
                var col = Pal.Hex("#5AC8FA");
                Sdf body = (x, y) => S.U(S.Ellipse(x, y, 54, 64, 40, 26), S.Poly(x, y, S.Pts(1, 84, 64, 118, 92, 118, 36)) - 3f);
                Icon(c, body, col);
                c.Fill((x, y) => S.I(S.Ellipse(x, y, 54, 64, 40, 26), 58 - y), col.Mul(0.82f));
                c.Fill(Lines(Arc(56, 64, 15, -55, 55, 10), 2.5f), col.Mul(0.6f));
                c.Fill((x, y) => S.Circle(x, y, 34, 70, 7), Pal.Hex("#1B2A4A"));
                c.Fill((x, y) => S.Circle(x, y, 32, 72, 2.5f), Wh);
            });

            I("sk_nap", c =>
            {
                Sdf moon = (x, y) => S.Sub(S.Circle(x, y, 50, 54, 40), S.Circle(x, y, 72, 70, 33));
                Icon(c, moon, Pal.Yellow);
                c.Fill((x, y) => Zed(x, y, 94, 92, 11, 3.8f), InkC);
                c.Fill((x, y) => Zed(x, y, 113, 113, 7, 3f), InkC);
            });

            I("sk_bell", c =>
            {
                var gold = Pal.Hex("#FFC93C");
                Sdf bell = (x, y) => S.U(
                    S.Circle(x, y, 64, 76, 30),
                    S.Poly(x, y, S.Pts(1, 34, 78, 94, 78, 106, 40, 22, 40)) - 4f,
                    S.Ellipse(x, y, 64, 40, 46, 9));
                c.Fill((x, y) => S.Circle(x, y, 64, 26, 10), Pal.Hex("#C9861A"));
                Icon(c, bell, gold);
                c.Fill((x, y) => S.Box(x, y, 64, 54, 34, 3, 2), Pal.Hex("#C9861A"));
                c.Fill((x, y) => S.Ellipse(x, y, 52, 86, 9, 14), A(0.5f), 2f);
                var red = Pal.Red;
                c.Fill((x, y) => S.U(S.Ellipse(x, y, 50, 110, 13, 8), S.Ellipse(x, y, 78, 110, 13, 8)), red);
                c.Fill((x, y) => S.Circle(x, y, 64, 108, 7), red.Mul(0.8f));
            });

            I("ic_catbox", c =>
            {
                var body = Pal.Hex("#9B5DE5");
                var lid = Pal.Hex("#C08CFF");
                Icon(c, (x, y) => S.U(S.Poly(x, y, S.Pts(1, 22, 86, 48, 86, 30, 118)) - 3f, S.Poly(x, y, S.Pts(1, 80, 86, 106, 86, 98, 118)) - 3f), lid, 0.04f);
                c.Fill((x, y) => S.U(S.Poly(x, y, S.Pts(1, 30, 90, 43, 90, 33, 108)), S.Poly(x, y, S.Pts(1, 85, 90, 98, 90, 95, 108))), bean);
                Icon(c, (x, y) => S.Box(x, y, 64, 44, 44, 34, 9), body, 0.05f);
                Icon(c, (x, y) => S.Box(x, y, 64, 80, 52, 12, 7), lid, 0f);
                c.Fill((x, y) => S.Box(x, y, 64, 44, 9, 34), Pal.Yellow);
                c.Fill((x, y) => S.Box(x, y, 64, 80, 10, 12), Pal.Hex("#FFE58A"));
                c.Fill(Paw(36, 40, 0.3f), A(0.85f));
                c.Fill(Paw(92, 46, 0.3f), A(0.85f));
            });

            I("ic_share", c => c.Fill((x, y) => S.U(
                S.U(S.Circle(x, y, 92, 100, 15), S.Circle(x, y, 36, 64, 15), S.Circle(x, y, 92, 28, 15)),
                S.U(S.Seg(x, y, 36, 64, 92, 100, 5), S.Seg(x, y, 36, 64, 92, 28, 5))), InkC));

            I("ic_copy", c => c.Fill((x, y) => S.U(
                S.Ring(S.Box(x, y, 52, 52, 26, 32, 8), 5),
                S.Sub(S.Ring(S.Box(x, y, 78, 78, 26, 32, 8), 5), S.Box(x, y, 52, 52, 32, 38, 10))), InkC));
        }
    }
}
