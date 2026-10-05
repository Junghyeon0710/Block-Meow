using UnityEngine;

namespace BlockMeow
{
    /// <summary>Layered cat face sprites. Every layer shares the same 256px frame so they stack in UI.</summary>
    public static partial class Atlas
    {
        const float CS = 256f;

        static void C(string name, System.Action<PixelCanvas> draw) => Defs.Add(new Def { Name = name, W = 256, H = 256, Ppu = 100f, Draw = draw });

        static float SmoothU(float a, float b, float k)
        {
            float h = Mathf.Clamp01(0.5f + 0.5f * (b - a) / k);
            return Mathf.Lerp(b, a, h) - k * h * (1f - h);
        }

        static readonly Vector2[] EarL = S.Pts(CS, 0.15f, 0.56f, 0.22f, 0.93f, 0.46f, 0.70f);
        static readonly Vector2[] EarR = S.Pts(CS, 0.85f, 0.56f, 0.78f, 0.93f, 0.54f, 0.70f);

        static float Head(float x, float y)
        {
            float face = S.Ellipse(x, y, 0.5f * CS, 0.44f * CS, 0.38f * CS, 0.31f * CS);
            float ears = Mathf.Min(S.Poly(x, y, EarL), S.Poly(x, y, EarR)) - 7f;
            return SmoothU(face, ears, 16f);
        }

        static float N(float v) => v * CS;

        /// <summary>Signed distance to the half-plane below the line p0-p1 (negative = below).</summary>
        static float Below(float x, float y, float x0, float y0, float x1, float y1)
        {
            float dx = x1 - x0, dy = y1 - y0;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            float nx = -dy / len, ny = dx / len; // left normal (points up for left-to-right lines)
            return (x - x0) * nx + (y - y0) * ny;
        }

        static void DefineCats()
        {
            var dark = Pal.Ink;
            var mouth = Pal.Ink;

            // flat fur (tinted per cat) with a ballpoint outline as its own layer on top
            C("cat_head", c => c.Fill(Head, Wh));
            C("cat_line", c => c.Fill(Pen(Head, 6.4f, 4f, 1.6f), Pal.Ink));
            C("cat_ear_in", c =>
            {
                var l = S.Pts(CS, 0.21f, 0.64f, 0.245f, 0.86f, 0.38f, 0.71f);
                var r = S.Pts(CS, 0.79f, 0.64f, 0.755f, 0.86f, 0.62f, 0.71f);
                c.Fill((x, y) => Mathf.Min(S.Poly(x, y, l), S.Poly(x, y, r)) - 3f, Pal.Hex("#FFB0C8"));
            });
            C("cat_tabby", c =>
            {
                float r = N(0.018f);
                c.Fill((x, y) =>
                {
                    float d = S.U(S.Seg(x, y, N(0.44f), N(0.69f), N(0.43f), N(0.60f), r), S.Seg(x, y, N(0.5f), N(0.71f), N(0.5f), N(0.61f), r),
                        S.Seg(x, y, N(0.56f), N(0.69f), N(0.57f), N(0.60f), r));
                    d = S.U(d, S.Seg(x, y, N(0.13f), N(0.44f), N(0.23f), N(0.45f), r), S.Seg(x, y, N(0.14f), N(0.38f), N(0.23f), N(0.40f), r));
                    d = S.U(d, S.Seg(x, y, N(0.87f), N(0.44f), N(0.77f), N(0.45f), r), S.Seg(x, y, N(0.86f), N(0.38f), N(0.77f), N(0.40f), r));
                    return S.I(d, Head(x, y));
                }, Wh);
            });
            C("cat_patch", c =>
            {
                c.Fill((x, y) => S.I(S.U(S.Ellipse(x, y, N(0.33f), N(0.58f), N(0.17f), N(0.17f)), S.Poly(x, y, EarL) - 7f), Head(x, y)), Wh);
            });
            C("cat_mask", c =>
            {
                c.Fill((x, y) =>
                {
                    float muzzle = S.Ellipse(x, y, N(0.5f), N(0.36f), N(0.19f), N(0.15f));
                    float tips = Mathf.Min(S.I(S.Poly(x, y, EarL) - 7f, N(0.78f) - y), S.I(S.Poly(x, y, EarR) - 7f, N(0.78f) - y));
                    return S.I(S.U(muzzle, tips), Head(x, y));
                }, Wh, 9f);
            });
            C("cat_muzzle", c =>
            {
                c.Fill((x, y) => S.U(S.Circle(x, y, N(0.44f), N(0.335f), N(0.08f)), S.Circle(x, y, N(0.56f), N(0.335f), N(0.08f)),
                    S.Ellipse(x, y, N(0.5f), N(0.28f), N(0.09f), N(0.05f))), new Color(1f, 0.99f, 0.97f, 0.95f));
            });
            C("cat_blush", c =>
            {
                var pink = Pal.Hex("#FF8FAB").WithA(0.6f);
                c.Fill((x, y) => S.U(S.Ellipse(x, y, N(0.26f), N(0.385f), N(0.055f), N(0.03f)), S.Ellipse(x, y, N(0.74f), N(0.385f), N(0.055f), N(0.03f))), pink, 5f);
            });
            C("cat_whisk", c =>
            {
                float r = N(0.006f);
                c.Fill((x, y) => S.U(
                    S.U(S.Seg(x, y, N(0.31f), N(0.37f), N(0.06f), N(0.41f), r), S.Seg(x, y, N(0.31f), N(0.34f), N(0.06f), N(0.32f), r)),
                    S.U(S.Seg(x, y, N(0.69f), N(0.37f), N(0.94f), N(0.41f), r), S.Seg(x, y, N(0.69f), N(0.34f), N(0.94f), N(0.32f), r))), Pal.Ink.WithA(0.85f));
            });

            // eyes
            C("cat_eye_open", c =>
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    float ex = N(0.5f + s * 0.14f), ey = N(0.47f);
                    c.Fill((x, y) => S.Ellipse(x, y, ex, ey, N(0.066f), N(0.088f)), dark);
                    c.Fill((x, y) => S.Circle(x, y, ex + N(0.023f), ey + N(0.035f), N(0.026f)), Wh);
                    c.Fill((x, y) => S.Circle(x, y, ex - N(0.015f), ey - N(0.03f), N(0.012f)), A(0.85f));
                }
            });
            C("cat_eye_blink", c =>
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    float ex = N(0.5f + s * 0.14f), ey = N(0.47f);
                    c.Fill(Lines(Arc(ex, ey + N(0.05f), N(0.06f), 230, 310, 10), N(0.012f)), dark);
                }
            });
            C("cat_eye_happy", c =>
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    float ex = N(0.5f + s * 0.14f), ey = N(0.46f);
                    var pts = new System.Collections.Generic.List<Vector2> { new Vector2(ex - N(0.06f), ey - N(0.012f)), new Vector2(ex, ey + N(0.05f)), new Vector2(ex + N(0.06f), ey - N(0.012f)) };
                    c.Fill(Lines(pts, N(0.015f)), dark);
                }
            });
            C("cat_eye_wow", c =>
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    float ex = N(0.5f + s * 0.14f), ey = N(0.47f);
                    c.Fill((x, y) => S.Circle(x, y, ex, ey, N(0.085f)), dark);
                    c.Fill((x, y) => S.Circle(x, y, ex + N(0.03f), ey + N(0.035f), N(0.035f)), Wh);
                    c.Fill((x, y) => S.Circle(x, y, ex - N(0.03f), ey - N(0.035f), N(0.016f)), Wh);
                    c.Fill(P(S.Star(ex - N(0.02f), ey + N(0.03f), N(0.022f), N(0.007f), 4, 90)), A(0.9f));
                }
            });
            C("cat_eye_sad", c =>
            {
                for (int s = -1; s <= 1; s += 2)
                {
                    float ex = N(0.5f + s * 0.14f), ey = N(0.455f);
                    // inner end of the lid sits higher than the outer end
                    float ox = ex - s * N(0.08f), ix = ex + s * N(0.08f);
                    float x0 = Mathf.Min(ox, ix), x1 = Mathf.Max(ox, ix);
                    float y0 = s < 0 ? ey + N(0.025f) : ey + N(0.055f);
                    float y1 = s < 0 ? ey + N(0.055f) : ey + N(0.025f);
                    c.Fill((x, y) => S.I(S.Ellipse(x, y, ex, ey, N(0.06f), N(0.072f)), Below(x, y, x0, y0, x1, y1)), dark);
                    c.Fill((x, y) => S.Circle(x, y, ex + N(0.02f), ey + N(0.0f), N(0.018f)), A(0.85f));
                }
                var tear = Pal.Hex("#7FD6FF");
                c.Fill((x, y) => S.U(S.Circle(x, y, N(0.31f), N(0.355f), N(0.026f)), S.Poly(x, y, S.Pts(CS, 0.287f, 0.36f, 0.333f, 0.36f, 0.31f, 0.405f))), tear);
            });

            // mouths (with the nose)
            System.Action<PixelCanvas> nose = c => c.Fill((x, y) => S.Poly(x, y, S.Pts(CS, 0.47f, 0.405f, 0.53f, 0.405f, 0.5f, 0.372f)) - 3f, Pal.Hex("#FF8FAB"));
            C("cat_mouth_w", c =>
            {
                nose(c);
                float r = N(0.008f);
                c.Fill((x, y) => S.U(Lines(Arc(N(0.472f), N(0.36f), N(0.028f), 180, 360, 12), r)(x, y),
                    Lines(Arc(N(0.528f), N(0.36f), N(0.028f), 180, 360, 12), r)(x, y)), mouth);
            });
            C("cat_mouth_o", c =>
            {
                nose(c);
                c.Fill((x, y) => S.Ellipse(x, y, N(0.5f), N(0.315f), N(0.03f), N(0.038f)), mouth);
                c.Fill((x, y) => S.Ellipse(x, y, N(0.5f), N(0.305f), N(0.017f), N(0.018f)), Pal.Hex("#FF7A9C"));
            });
            C("cat_mouth_smile", c =>
            {
                nose(c);
                Sdf open = (x, y) => S.I(S.Circle(x, y, N(0.5f), N(0.355f), N(0.068f)), y - N(0.355f));
                c.Fill(open, Pal.Hex("#5A1F3A"));
                c.Fill((x, y) => S.I(S.Circle(x, y, N(0.5f), N(0.30f), N(0.036f)), open(x, y)), Pal.Hex("#FF7A9C"));
            });
            C("cat_mouth_sad", c =>
            {
                nose(c);
                c.Fill(Lines(Arc(N(0.5f), N(0.285f), N(0.042f), 25, 155, 12), N(0.009f)), mouth);
            });

            // accessories (pre-colored)
            C("acc_bow", c =>
            {
                var pink = Pal.Hex("#FF5C93");
                Sdf bow = (x, y) => S.U(S.Poly(x, y, S.Pts(CS, 0.70f, 0.77f, 0.59f, 0.86f, 0.60f, 0.68f)) - 5f,
                    S.Poly(x, y, S.Pts(CS, 0.70f, 0.77f, 0.81f, 0.86f, 0.80f, 0.68f)) - 5f, S.Circle(x, y, N(0.70f), N(0.77f), N(0.032f)));
                Icon(c, bow, pink, 0f);
            });
            C("acc_flower", c =>
            {
                float cx = N(0.29f), cy = N(0.75f);
                for (int i = 0; i < 5; i++)
                {
                    float a = (90f + i * 72f) * Mathf.Deg2Rad;
                    float px = cx + Mathf.Cos(a) * N(0.045f), py = cy + Mathf.Sin(a) * N(0.045f);
                    Icon(c, (x, y) => S.Circle(x, y, px, py, N(0.038f)), Pal.Hex("#FFC2DA"), 0f);
                }
                c.Fill((x, y) => S.Circle(x, y, cx, cy, N(0.026f)), Pal.Yellow);
            });
            C("acc_crown", c =>
            {
                Icon(c, (x, y) => S.Poly(x, y, S.Pts(CS, 0.38f, 0.80f, 0.62f, 0.80f, 0.655f, 0.93f, 0.57f, 0.87f, 0.5f, 0.96f, 0.43f, 0.87f, 0.345f, 0.93f)) - 3f, Pal.Yellow, 0.02f);
                c.Fill((x, y) => S.Circle(x, y, N(0.5f), N(0.835f), N(0.02f)), Pal.Red);
            });
            C("acc_hat", c =>
            {
                var hat = S.Pts(CS, 0.54f, 0.78f, 0.76f, 0.81f, 0.69f, 0.955f);
                Icon(c, (x, y) => S.Poly(x, y, hat) - 3f, Pal.Blue, 0f);
                c.Fill((x, y) => S.I(S.Poly(x, y, hat) - 3f, Mathf.Abs(y - N(0.86f)) - N(0.016f)), Pal.Yellow);
                c.Fill((x, y) => S.Circle(x, y, N(0.69f), N(0.955f), N(0.03f)), Pal.Pink);
            });
            C("acc_glasses", c =>
            {
                var frame = Pal.Hex("#2A2340");
                for (int s = -1; s <= 1; s += 2)
                {
                    float ex = N(0.5f + s * 0.14f), ey = N(0.47f);
                    c.Fill((x, y) => S.Circle(x, y, ex, ey, N(0.09f)), Pal.Hex("#BFE9FF").WithA(0.25f));
                    c.Fill((x, y) => S.Ring(S.Circle(x, y, ex, ey, N(0.095f)), N(0.012f)), frame);
                }
                c.Fill((x, y) => S.Seg(x, y, N(0.455f), N(0.48f), N(0.545f), N(0.48f), N(0.01f)), frame);
            });
            C("acc_phones", c =>
            {
                c.Fill((x, y) => S.I(S.Ring(S.Circle(x, y, N(0.5f), N(0.47f), N(0.43f)), N(0.028f)), N(0.5f) - y), Pal.Hex("#3A3278"));
                Icon(c, (x, y) => S.U(S.Box(x, y, N(0.10f), N(0.47f), N(0.05f), N(0.085f), N(0.03f)), S.Box(x, y, N(0.90f), N(0.47f), N(0.05f), N(0.085f), N(0.03f))), Pal.Pink, 0f);
            });
            C("acc_star", c => Icon(c, (x, y) => S.Poly(x, y, S.Star(N(0.31f), N(0.73f), N(0.068f), N(0.03f), 5, 90)) - 2f, Pal.Yellow, 0f));
            C("acc_leaf", c =>
            {
                var green = Pal.Hex("#6BCB77");
                c.Fill((x, y) => S.Seg(x, y, N(0.5f), N(0.77f), N(0.5f), N(0.89f), N(0.011f)), green.Mul(0.7f));
                Icon(c, (x, y) => S.U(S.RBox(x, y, N(0.455f), N(0.91f), N(0.05f), N(0.024f), 25f, N(0.02f)), S.RBox(x, y, N(0.548f), N(0.92f), N(0.05f), N(0.024f), -25f, N(0.02f))), green, 0f);
            });
        }
    }
}
