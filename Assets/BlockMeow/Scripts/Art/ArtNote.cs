using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlockMeow
{
    /// <summary>
    /// The notebook skin: ballpoint outlines with a hand-drawn wobble, highlighter fills, pencil cells,
    /// the graph-paper tile and the block-lettered logo.
    /// </summary>
    public static partial class Atlas
    {
        static readonly Color InkC = Pal.Ink;

        /// <summary>Smooth deterministic wobble (pixels) so straight shapes look drawn by hand.</summary>
        static float Wob(float x, float y, float seed, float amp)
        {
            float a = Mathf.Sin(x * 0.083f + seed * 1.91f) * 0.55f + Mathf.Sin(y * 0.097f + seed * 2.73f) * 0.45f
                      + Mathf.Sin((x - y) * 0.051f + seed * 0.77f) * 0.35f;
            return a * amp / 1.35f;
        }

        /// <summary>A pen line along the shape's edge: wobbly path, slightly uneven pressure.</summary>
        static Sdf Pen(Sdf shape, float width, float seed, float wobble)
            => (x, y) => Mathf.Abs(shape(x, y) + Wob(x, y, seed, wobble)) - width * 0.5f * (1f + 0.16f * Mathf.Sin((x + 2f * y) * 0.045f + seed));

        static void UR(string name, int w, int h, Vector4 border, Action<PixelCanvas> draw)
            => Defs.Add(new Def { Name = name, W = w, H = h, Ppu = 100f, Border = border, Draw = draw });

        static void DefineNote()
        {
            // ---------------------------------------------------------------- world: blocks, cells, board frame
            // highlighter body (tinted per piece): a little off the pen line, with marker stroke overlaps
            W("blk_note", 128, c =>
            {
                Sdf body = (x, y) => S.RBox(x, y, 60, 60, 55, 55, 1.4f, 8f) + Wob(x, y, 5f, 2f);
                c.Fill(body, Wh);
                for (int i = 0; i < 3; i++)
                {
                    float ly = 30 + i * 32;
                    c.Fill((x, y) => S.I(body(x, y), Mathf.Abs(y - ly - Wob(x, 0, i, 2f)) - 2.2f), G(0.9f));
                }
            });
            for (int v = 0; v < 3; v++)
            {
                int seed = v + 1;
                string suffix = v == 0 ? "" : v.ToString();
                W("blk_note_line" + suffix, 128, c => c.Fill(Pen((x, y) => S.Box(x, y, 64, 64, 55, 55, 9), 6.2f, seed, 1.7f), InkC));
                W("cell_line" + suffix, 64, c => c.Fill(Pen((x, y) => S.Box(x, y, 32, 32, 27.5f, 27.5f, 4), 1.9f, seed + 4, 0.8f), Pal.Pencil.WithA(0.75f)));
            }
            W("board_line", 512, c =>
            {
                Sdf frame = (x, y) => S.Box(x, y, 256, 256, 250, 250, 26);
                c.Fill(Pen(frame, 6.5f, 2f, 3f), InkC);
                c.Fill(Pen(frame, 2.6f, 9f, 3.4f), InkC.WithA(0.35f)); // a second, lighter pass
            });

            // ---------------------------------------------------------------- UI (white, tinted at use)
            UR("ui_note_line", 64, 64, new Vector4(28, 28, 28, 28), c => c.Fill(Pen((x, y) => S.Box(x, y, 32, 32, 29.2f, 29.2f, 24), 3.4f, 3f, 0.9f), Wh));
            UR("ui_note_pill", 64, 64, new Vector4(31, 31, 31, 31), c => c.Fill(Pen((x, y) => S.Box(x, y, 32, 32, 30.2f, 30.2f, 30.2f), 3.4f, 5f, 0.8f), Wh));
            UR("ui_note_fill", 64, 64, new Vector4(28, 28, 28, 28), c => c.Fill((x, y) => S.Box(x, y, 32, 32, 30, 30, 20) + Wob(x, y, 7f, 1.1f), Wh));
            UR("ui_note_ring", 128, 128, Vector4.zero, c => c.Fill(Pen((x, y) => S.Circle(x, y, 64, 64, 60), 5f, 6f, 1.6f), Wh));
            // one grid line each way through the middle, so a tiled image reads as graph paper
            UR("ui_graph", 64, 64, Vector4.zero, c =>
            {
                c.Fill((x, y) => Mathf.Abs(x - 32f) - 0.7f, Pal.GridLine, 0.8f);
                c.Fill((x, y) => Mathf.Abs(y - 32f) - 0.7f, Pal.GridLine, 0.8f);
            });
            UR("ui_tape", 96, 40, Vector4.zero, c =>
            {
                // translucent tape with torn short ends
                c.Fill((x, y) => S.Box(x, y, 48, 20, 44 + Mathf.Sin(y * 0.9f) * 1.6f, 15, 0), new Color(1f, 1f, 1f, 0.72f));
            });
            UR("ui_hl_swipe", 128, 40, new Vector4(22, 0, 22, 0), c =>
                c.Fill((x, y) => S.Box(x, y, 64, 20, 60, 15, 10) + Wob(x, y, 3f, 1.4f), Wh));

            // ---------------------------------------------------------------- the logo: "블록냥" colored in on graph squares
            UR("logo_note", 512, 272, Vector4.zero, c =>
            {
                var marker = new[] { Pal.Hex("#FF6FB1"), Pal.Hex("#4FB6F0"), Pal.Hex("#FF9F40") };
                const float u = 15f, top = 252f, gapS = 22f;
                float sylW = 9 * u, x0 = (512f - (3 * sylW + 2 * gapS)) * 0.5f;
                for (int si = 0; si < 3; si++)
                {
                    string[] rows = LogoRows[si];
                    float ox = x0 + si * (sylW + gapS);
                    var col = marker[si].WithA(0.93f);
                    for (int r = 0; r < rows.Length; r++)
                        for (int k = 0; k < rows[r].Length; k++)
                        {
                            if (rows[r][k] != 'X') continue;
                            float cx = ox + k * u + u * 0.5f + Wob(k * 9f, r * 7f, si, 1.2f);
                            float cy = top - r * u - u * 0.5f + Wob(r * 8f, k * 5f, si + 3, 1.2f);
                            c.Fill((x, y) => S.Box(x, y, cx, cy, u * 0.56f, u * 0.56f, 1.5f), col);
                        }
                }
                var line = new List<Vector2>();
                for (int i = 0; i <= 40; i++)
                {
                    float t = i / 40f, x = Mathf.Lerp(52f, 462f, t);
                    line.Add(new Vector2(x, 40f + Mathf.Sin(t * 7.5f) * 3.2f + t * 4f));
                }
                c.Fill(Lines(line, 2.8f), InkC);
            });
        }

        // the same 9x12 lettering as the design mockups
        static readonly string[][] LogoRows =
        {
            new[] { ".X.....X.", ".XXXXXXX.", ".X.....X.", ".XXXXXXX.", ".........", "XXXXXXXXX", ".........", ".XXXXXXX.", ".......X.", ".XXXXXXX.", ".X.......", ".XXXXXXX." },
            new[] { ".XXXXXXX.", ".......X.", ".XXXXXXX.", ".X.......", ".XXXXXXX.", "....X....", "XXXXXXXXX", ".........", ".XXXXXXX.", ".......X.", ".......X.", ".......X." },
            new[] { "X.....X..", "X.....XXX", "X.....X..", "XXXXX.XXX", "......X..", ".........", ".........", "..XXXXX..", ".X.....X.", ".X.....X.", ".X.....X.", "..XXXXX.." },
        };
    }
}
