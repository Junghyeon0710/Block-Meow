using System;
using UnityEngine;

namespace BlockMeow
{
    /// <summary>Signed distance in pixels (negative = inside).</summary>
    public delegate float Sdf(float x, float y);

    /// <summary>2D signed-distance primitives in pixel space (y up). Pure math, safe on worker threads.</summary>
    public static class S
    {
        public static float Circle(float x, float y, float cx, float cy, float r)
        {
            float dx = x - cx, dy = y - cy;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        public static float Ellipse(float x, float y, float cx, float cy, float rx, float ry)
        {
            float dx = (x - cx) / rx, dy = (y - cy) / ry;
            return (Mathf.Sqrt(dx * dx + dy * dy) - 1f) * Mathf.Min(rx, ry);
        }

        public static float Box(float x, float y, float cx, float cy, float hw, float hh, float r = 0f)
        {
            float qx = Mathf.Abs(x - cx) - hw + r, qy = Mathf.Abs(y - cy) - hh + r;
            float ox = Mathf.Max(qx, 0f), oy = Mathf.Max(qy, 0f);
            return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0f) - r;
        }

        public static float RBox(float x, float y, float cx, float cy, float hw, float hh, float deg, float r = 0f)
        {
            float a = -deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            float dx = x - cx, dy = y - cy;
            return Box(dx * c - dy * s, dx * s + dy * c, 0f, 0f, hw, hh, r);
        }

        public static float Seg(float x, float y, float ax, float ay, float bx, float by, float r)
        {
            float pax = x - ax, pay = y - ay, bax = bx - ax, bay = by - ay;
            float h = Mathf.Clamp01((pax * bax + pay * bay) / (bax * bax + bay * bay + 1e-6f));
            float dx = pax - bax * h, dy = pay - bay * h;
            return Mathf.Sqrt(dx * dx + dy * dy) - r;
        }

        public static float Poly(float x, float y, Vector2[] v)
        {
            int n = v.Length;
            float d = (x - v[0].x) * (x - v[0].x) + (y - v[0].y) * (y - v[0].y);
            float s = 1f;
            for (int i = 0, j = n - 1; i < n; j = i, i++)
            {
                float ex = v[j].x - v[i].x, ey = v[j].y - v[i].y;
                float wx = x - v[i].x, wy = y - v[i].y;
                float t = Mathf.Clamp01((wx * ex + wy * ey) / (ex * ex + ey * ey + 1e-6f));
                float bx = wx - ex * t, by = wy - ey * t;
                d = Mathf.Min(d, bx * bx + by * by);
                bool c1 = y >= v[i].y, c2 = y < v[j].y, c3 = ex * wy > ey * wx;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        public static Vector2[] Pts(float scale, params float[] xy)
        {
            var p = new Vector2[xy.Length / 2];
            for (int i = 0; i < p.Length; i++) p[i] = new Vector2(xy[i * 2] * scale, xy[i * 2 + 1] * scale);
            return p;
        }

        public static Vector2[] Star(float cx, float cy, float ro, float ri, int n, float rotDeg)
        {
            var p = new Vector2[n * 2];
            for (int i = 0; i < n * 2; i++)
            {
                float a = (rotDeg + 180f * i / n) * Mathf.Deg2Rad;
                float r = (i & 1) == 0 ? ro : ri;
                p[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            return p;
        }

        public static Vector2[] Regular(float cx, float cy, float r, int n, float rotDeg)
        {
            var p = new Vector2[n];
            for (int i = 0; i < n; i++)
            {
                float a = (rotDeg + 360f * i / n) * Mathf.Deg2Rad;
                p[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            return p;
        }

        /// <summary>Heart (Inigo Quilez), tip at (cx, cy - size/2), full height about 1.1*size.</summary>
        public static float Heart(float x, float y, float cx, float cy, float size)
        {
            float px = Mathf.Abs(x - cx) / size, py = (y - cy) / size + 0.5f;
            float d;
            if (py + px > 1f)
            {
                float dx = px - 0.25f, dy = py - 0.75f;
                d = Mathf.Sqrt(dx * dx + dy * dy) - 0.35355339f;
            }
            else
            {
                float a = px * px + (py - 1f) * (py - 1f);
                float m = 0.5f * Mathf.Max(px + py, 0f);
                float b = (px - m) * (px - m) + (py - m) * (py - m);
                d = Mathf.Sqrt(Mathf.Min(a, b)) * Mathf.Sign(px - py);
            }
            return d * size;
        }

        public static float U(float a, float b) => Mathf.Min(a, b);
        public static float U(float a, float b, float c) => Mathf.Min(a, Mathf.Min(b, c));
        public static float Sub(float a, float b) => Mathf.Max(a, -b);
        public static float I(float a, float b) => Mathf.Max(a, b);
        public static float Ring(float d, float halfWidth) => Mathf.Abs(d) - halfWidth;
    }

    /// <summary>
    /// Float RGBA raster with straight-alpha "over" compositing.
    /// Drawing code works in virtual units: K = virtual units per pixel, so the same drawing can be
    /// rendered at any resolution (atlas sprites at K = 1, the 1024 px app icon at K = 0.25).
    /// </summary>
    public sealed class PixelCanvas
    {
        public readonly int W, H;
        public float K = 1f;
        readonly float[] _r, _g, _b, _a;

        public PixelCanvas(int w, int h)
        {
            W = w; H = h;
            int n = w * h;
            _r = new float[n]; _g = new float[n]; _b = new float[n]; _a = new float[n];
        }

        /// <summary>Width in virtual units (what drawing code should size things against).</summary>
        public float VW => W * K;

        void Blend(int i, float r, float g, float b, float a)
        {
            if (a <= 0f) return;
            if (a > 1f) a = 1f;
            float k = _a[i] * (1f - a);
            float oa = a + k;
            if (oa <= 1e-6f) return;
            float inv = 1f / oa;
            _r[i] = (r * a + _r[i] * k) * inv;
            _g[i] = (g * a + _g[i] * k) * inv;
            _b[i] = (b * a + _b[i] * k) * inv;
            _a[i] = oa;
        }

        public void Fill(Sdf sdf, Color c, float aa = 1.25f)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float cov = 0.5f - sdf((x + 0.5f) * K, (y + 0.5f) * K) / K / aa;
                if (cov <= 0f) continue;
                Blend(y * W + x, c.r, c.g, c.b, c.a * Mathf.Min(cov, 1f));
            }
        }

        public void Fill(Sdf sdf, Func<float, float, Color> color, float aa = 1.25f)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float px = (x + 0.5f) * K, py = (y + 0.5f) * K;
                float cov = 0.5f - sdf(px, py) / K / aa;
                if (cov <= 0f) continue;
                var c = color(px, py);
                Blend(y * W + x, c.r, c.g, c.b, c.a * Mathf.Min(cov, 1f));
            }
        }

        public void Stroke(Sdf sdf, float width, Color c, float aa = 1.25f)
        {
            float hw = width * 0.5f;
            Fill((x, y) => Mathf.Abs(sdf(x, y)) - hw, c, aa);
        }

        public void Glow(Sdf sdf, Color c, float radius, float power = 2f, bool inside = true)
        {
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float d = sdf((x + 0.5f) * K, (y + 0.5f) * K);
                float t;
                if (d <= 0f) { if (!inside) continue; t = 1f; }
                else { t = 1f - d / radius; if (t <= 0f) continue; t = Mathf.Pow(t, power); }
                Blend(y * W + x, c.r, c.g, c.b, c.a * t);
            }
        }

        /// <summary>Draws another canvas of the same size on top, multiplied by a tint (used to build the app icon).</summary>
        public void Composite(PixelCanvas src, Color tint)
        {
            for (int i = 0; i < _a.Length && i < src._a.Length; i++)
                Blend(i, src._r[i] * tint.r, src._g[i] * tint.g, src._b[i] * tint.b, src._a[i] * tint.a);
        }

        public Color32[] ToColor32()
        {
            var px = new Color32[W * H];
            CopyTo(px, W, 0, 0);
            return px;
        }

        public void CopyTo(Color32[] dst, int dstW, int ox, int oy)
        {
            for (int y = 0; y < H; y++)
            {
                int row = (oy + y) * dstW + ox;
                for (int x = 0; x < W; x++)
                {
                    int i = y * W + x;
                    dst[row + x] = new Color32(
                        (byte)(Mathf.Clamp01(_r[i]) * 255f + 0.5f), (byte)(Mathf.Clamp01(_g[i]) * 255f + 0.5f),
                        (byte)(Mathf.Clamp01(_b[i]) * 255f + 0.5f), (byte)(Mathf.Clamp01(_a[i]) * 255f + 0.5f));
                }
            }
        }
    }
}
