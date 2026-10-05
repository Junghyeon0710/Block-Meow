using System.Collections.Generic;
using UnityEngine;

namespace BlockMeow
{
    public static partial class Atlas
    {
        static void I(string name, System.Action<PixelCanvas> draw) => Defs.Add(new Def { Name = name, W = 128, H = 128, Ppu = 100f, Draw = draw });

        static Vector2[] ArrowHead(float cx, float cy, float r, float deg, float size, bool ccw)
        {
            float a = deg * Mathf.Deg2Rad;
            var radial = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            var tangent = ccw ? new Vector2(-radial.y, radial.x) : new Vector2(radial.y, -radial.x);
            var basePt = new Vector2(cx, cy) + radial * r;
            var tip = basePt + tangent * size;
            var b1 = basePt + radial * size * 0.85f - tangent * 2f;
            var b2 = basePt - radial * size * 0.85f - tangent * 2f;
            return new[] { tip, b1, b2 };
        }

        static Sdf ArcArrow(float cx, float cy, float r, float a0, float a1, float width, float head)
        {
            var arc = Lines(Arc(cx, cy, r, a0, a1, 24), width * 0.5f);
            var tri = ArrowHead(cx, cy, r, a1, head, a1 > a0);
            return (x, y) => S.U(arc(x, y), S.Poly(x, y, tri));
        }

        static void DefineIcons()
        {
            I("ic_coin", c =>
            {
                c.Glow((x, y) => S.Circle(x, y, 64, 64, 50), Pal.Coin.WithA(0.45f), 9f, 2f, false);
                c.Fill((x, y) => S.Circle(x, y, 64, 64, 50), Pal.Hex("#E39B0B"));
                c.Fill((x, y) => S.Circle(x, y, 64, 67, 43), Pal.Coin);
                c.Stroke((x, y) => S.Circle(x, y, 64, 67, 31), 5f, Pal.Hex("#FFF1A8"));
                c.Fill(P(S.Star(64, 67, 19, 7, 4, 90)), Pal.Hex("#FFF9DD"));
            });
            I("ic_hammer", c =>
            {
                Icon(c, (x, y) => S.Seg(x, y, 30, 24, 76, 78, 9f), Pal.Hex("#C07A43"), 0.03f);
                Icon(c, (x, y) => S.RBox(x, y, 84, 86, 34, 17, -40f, 6f), Pal.Hex("#C9D3E0"), 0.05f);
                c.Fill((x, y) => S.RBox(x, y, 84, 86, 34, 5, -40f, 2f), A(0.45f));
            });
            I("ic_refresh", c =>
            {
                var col = Pal.Hex("#3BD6FF");
                Sdf a = ArcArrow(64, 64, 36, 25, 160, 13, 17);
                Sdf b = ArcArrow(64, 64, 36, 205, 340, 13, 17);
                Icon(c, (x, y) => S.U(a(x, y), b(x, y)), col);
            });
            I("ic_rotate", c => Icon(c, ArcArrow(64, 62, 36, 110, 395, 13, 18), Pal.Hex("#FF9F43")));
            I("ic_retry", c => c.Fill(ArcArrow(64, 62, 34, 110, 395, 12, 17), InkC));

            I("ic_pause", c => c.Fill((x, y) => S.U(S.Box(x, y, 46, 64, 10, 34, 5), S.Box(x, y, 82, 64, 10, 34, 5)), InkC));
            I("ic_play", c => c.Fill((x, y) => S.Poly(x, y, S.Pts(1, 40, 24, 40, 104, 104, 64)) - 4f, InkC));
            I("ic_close", c => c.Fill((x, y) => S.U(S.RBox(x, y, 64, 64, 42, 9, 45, 9), S.RBox(x, y, 64, 64, 42, 9, -45, 9)), InkC));
            I("ic_check", c => c.Fill((x, y) => S.U(S.Seg(x, y, 28, 66, 54, 40, 9), S.Seg(x, y, 54, 40, 102, 92, 9)), InkC));
            I("ic_left", c => c.Fill((x, y) => S.U(S.Seg(x, y, 80, 22, 44, 64, 9), S.Seg(x, y, 44, 64, 80, 106, 9)), InkC));
            I("ic_right", c => c.Fill((x, y) => S.U(S.Seg(x, y, 48, 22, 84, 64, 9), S.Seg(x, y, 84, 64, 48, 106, 9)), InkC));
            I("ic_home", c => c.Fill((x, y) =>
            {
                float roof = S.Poly(x, y, S.Pts(1, 18, 62, 64, 106, 110, 62)) - 3f;
                float body = S.Box(x, y, 64, 42, 32, 28, 4);
                float door = S.Box(x, y, 64, 30, 10, 16, 3);
                return S.Sub(S.U(roof, body), door);
            }, InkC));
            I("ic_settings", c => c.Fill((x, y) =>
            {
                float d = S.Circle(x, y, 64, 64, 34);
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 45f, r = a * Mathf.Deg2Rad;
                    d = Mathf.Min(d, S.RBox(x, y, 64 + Mathf.Cos(r) * 42, 64 + Mathf.Sin(r) * 42, 9, 8, a, 2));
                }
                return S.Sub(d, S.Circle(x, y, 64, 64, 15));
            }, InkC));
            I("ic_lock", c => c.Fill((x, y) =>
            {
                float shackle = S.U(Mathf.Max(S.Ring(S.Circle(x, y, 64, 74, 22), 6), 74 - y),
                    S.U(S.Box(x, y, 42, 66, 6, 10), S.Box(x, y, 86, 66, 6, 10)));
                float body = S.Sub(S.Box(x, y, 64, 44, 34, 26, 7), S.U(S.Circle(x, y, 64, 48, 7), S.Box(x, y, 64, 38, 3, 9)));
                return S.U(shackle, body);
            }, InkC));
            I("ic_info", c => c.Fill((x, y) => S.Sub(S.Circle(x, y, 64, 64, 54), S.U(S.Circle(x, y, 64, 90, 8), S.Box(x, y, 64, 52, 7, 22, 4))), InkC));
            I("ic_ad", c => c.Fill((x, y) => S.Sub(S.Box(x, y, 64, 64, 50, 38, 12), S.Poly(x, y, S.Pts(1, 52, 44, 52, 84, 86, 64))), InkC));
            I("ic_sound", c => c.Fill((x, y) =>
            {
                float spk = S.U(S.Box(x, y, 34, 64, 12, 16, 3), S.Poly(x, y, S.Pts(1, 42, 78, 66, 100, 66, 28, 42, 50)));
                float w1 = Mathf.Max(S.Ring(S.Circle(x, y, 66, 64, 22), 5), 76 - x);
                float w2 = Mathf.Max(S.Ring(S.Circle(x, y, 66, 64, 38), 5), 86 - x);
                return S.U(spk, w1, w2);
            }, InkC));
            I("ic_music", c => c.Fill((x, y) =>
            {
                float head = S.Ellipse(x, y, 46, 36, 18, 13);
                float stem = S.Box(x, y, 60, 70, 5, 36);
                float flag = S.Poly(x, y, S.Pts(1, 55, 106, 96, 92, 96, 74, 60, 86));
                return S.U(head, stem, flag);
            }, InkC));
            I("ic_vibrate", c => c.Fill((x, y) =>
            {
                float phone = S.Ring(S.Box(x, y, 64, 64, 22, 38, 8), 5);
                float l = S.U(S.Seg(x, y, 24, 50, 24, 78, 4), S.Seg(x, y, 104, 50, 104, 78, 4));
                float l2 = S.U(S.Seg(x, y, 12, 56, 12, 72, 4), S.Seg(x, y, 116, 56, 116, 72, 4));
                return S.U(phone, l, l2);
            }, InkC));

            // ---------------------------------------------------------------- colorful menu icons
            I("ic_gift", c =>
            {
                var pink = Pal.Hex("#FF5CC8"); var gold = Pal.Yellow;
                Icon(c, (x, y) => S.Box(x, y, 64, 44, 38, 28, 6), pink);
                Icon(c, (x, y) => S.Box(x, y, 64, 80, 44, 11, 6), pink.Lighten(0.2f), 0f);
                c.Fill((x, y) => S.Box(x, y, 64, 54, 8, 40), gold);
                c.Fill((x, y) => S.U(S.Ellipse(x, y, 50, 100, 14, 9), S.Ellipse(x, y, 78, 100, 14, 9)), gold);
            });
            I("ic_calendar", c =>
            {
                Icon(c, (x, y) => S.Box(x, y, 64, 58, 46, 42, 9), Pal.Hex("#F3F4FF"), 0.04f);
                c.Fill((x, y) => S.I(S.Box(x, y, 64, 58, 46, 42, 9), 80 - y), Pal.Red);
                c.Fill((x, y) => S.U(S.Box(x, y, 42, 102, 5, 10, 4), S.Box(x, y, 86, 102, 5, 10, 4)), Pal.Hex("#3A3278"));
                for (int i = 0; i < 6; i++)
                {
                    float gx = 38 + (i % 3) * 26, gy = 60 - (i / 3) * 22;
                    int k = i;
                    c.Fill((x, y) => S.Box(x, y, gx, gy, 8, 7, 2), k == 4 ? Pal.Red : Pal.Hex("#B4ADE8"));
                }
            });
            I("ic_mission", c =>
            {
                var blue = Pal.Blue;
                Icon(c, (x, y) => S.Box(x, y, 64, 60, 38, 48, 8), blue);
                c.Fill((x, y) => S.Box(x, y, 64, 108, 16, 7, 4), Pal.Hex("#C9D8FF"));
                for (int i = 0; i < 3; i++)
                {
                    float ly = 84 - i * 22;
                    c.Fill((x, y) => S.Box(x, y, 74, ly, 18, 4, 3), A(0.92f));
                    c.Fill((x, y) => S.Circle(x, y, 44, ly, 6), Pal.Green);
                }
            });
            I("ic_wheel", c =>
            {
                var cols = new[] { Pal.Red, Pal.Orange, Pal.Yellow, Pal.Green, Pal.Cyan, Pal.Blue, Pal.Purple, Pal.Pink };
                c.Glow((x, y) => S.Circle(x, y, 64, 60, 48), Pal.Yellow.WithA(0.35f), 8f, 2f, false);
                c.Fill((x, y) => S.Circle(x, y, 64, 60, 50), Pal.Hex("#5A3E9E"));
                c.Fill((x, y) => S.Circle(x, y, 64, 60, 43), (x, y) =>
                {
                    float a = Mathf.Atan2(y - 60, x - 64) * Mathf.Rad2Deg + 360f;
                    return cols[(int)(a / 45f) % 8];
                });
                c.Fill((x, y) => S.Circle(x, y, 64, 60, 10), Wh);
                c.Fill((x, y) => S.Poly(x, y, S.Pts(1, 54, 120, 74, 120, 64, 100)) - 2f, Pal.Yellow);
            });
            I("ic_palette", c =>
            {
                Icon(c, (x, y) => S.Sub(S.Ellipse(x, y, 64, 62, 52, 44), S.Circle(x, y, 84, 44, 11)), Pal.Hex("#E8C9A0"));
                var dots = new[] { (40f, 72f, Pal.Red), (60f, 92f, Pal.Yellow), (86f, 86f, Pal.Green), (40f, 48f, Pal.Blue) };
                foreach (var (dx, dy, col) in dots) c.Fill((x, y) => S.Circle(x, y, dx, dy, 10), col);
            });
            I("ic_paw", c =>
            {
                var pink = Pal.Hex("#FF8FB8");
                Sdf paw = (x, y) => S.U(S.Ellipse(x, y, 64, 46, 30, 24),
                    S.U(S.Ellipse(x, y, 30, 74, 11, 14), S.Ellipse(x, y, 50, 94, 11, 14)),
                    S.U(S.Ellipse(x, y, 78, 94, 11, 14), S.Ellipse(x, y, 98, 74, 11, 14)));
                Icon(c, paw, pink);
            });
            I("ic_flag", c =>
            {
                c.Fill((x, y) => S.Box(x, y, 36, 62, 5, 48, 3), InkC);
                Icon(c, P(S.Pts(1, 40, 108, 104, 92, 72, 78, 104, 62, 40, 66)), Pal.Green);
                c.Fill((x, y) => S.Ellipse(x, y, 36, 14, 24, 6), A(0.35f), 2f);
            });
            I("ic_crown", c =>
            {
                Icon(c, (x, y) => S.Poly(x, y, S.Pts(1, 18, 30, 110, 30, 116, 92, 88, 66, 64, 104, 40, 66, 12, 92)) - 2f, Pal.Yellow);
                c.Fill((x, y) => S.Box(x, y, 64, 34, 46, 5), Pal.Hex("#FFF1B8"));
                c.Fill((x, y) => S.Circle(x, y, 64, 54, 7), Pal.Red);
            });
            I("ic_trophy", c =>
            {
                Sdf cup = (x, y) => S.U(Mathf.Max(S.Circle(x, y, 64, 82, 34), y - 108),
                    S.U(S.Box(x, y, 64, 40, 7, 14), S.Box(x, y, 64, 22, 26, 7, 3)));
                Sdf handles = (x, y) => S.U(S.Ring(S.Circle(x, y, 30, 86, 13), 4), S.Ring(S.Circle(x, y, 98, 86, 13), 4));
                Icon(c, (x, y) => S.U(cup(x, y), handles(x, y)), Pal.Yellow);
            });
            I("ic_star", c => Icon(c, (x, y) => S.Poly(x, y, S.Star(64, 62, 54, 24, 5, 90)) - 3f, Pal.Yellow));
            I("ic_flame", c =>
            {
                Sdf outer = (x, y) => S.U(S.Circle(x, y, 64, 46, 32), S.Poly(x, y, S.Pts(1, 34, 52, 64, 120, 94, 52)) - 4f);
                Icon(c, outer, Pal.Hex("#FF7A2F"));
                c.Fill((x, y) => S.U(S.Circle(x, y, 64, 40, 16), S.Poly(x, y, S.Pts(1, 50, 44, 64, 82, 78, 44))), Pal.Yellow);
            });
            I("ic_blocks", c =>
            {
                var cols = new[] { Pal.Hex("#FF5A7A"), Pal.Hex("#FFD93D"), Pal.Hex("#4D96FF"), Pal.Hex("#6BCB77") };
                var pos = new[] { (40f, 88f), (88f, 88f), (40f, 40f), (88f, 40f) };
                for (int i = 0; i < 4; i++)
                {
                    var (px, py) = pos[i];
                    var col = cols[i];
                    c.Fill((x, y) => S.Box(x, y, px, py, 22, 22, 7), col.Mul(0.7f));
                    c.Fill((x, y) => S.Box(x, y, px, py + 2, 20, 20, 6), col);
                    c.Fill((x, y) => S.Ellipse(x, y, px - 6, py + 11, 9, 4), A(0.6f), 2f);
                }
            });
        }
    }
}
