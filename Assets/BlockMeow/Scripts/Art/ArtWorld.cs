using UnityEngine;

namespace BlockMeow
{
    public static partial class Atlas
    {
        static void DefineWorld()
        {
            // ---------------------------------------------------------------- block skins (white, tinted per piece)
            // body = rounded square filling the cell with a small gap so neighbours read as separate blocks
            W("blk_jelly", 128, c =>
            {
                Sdf body = (x, y) => S.Box(x, y, 64, 64, 58, 58, 20);
                c.Fill(body, (x, y) => G(Mathf.Lerp(0.80f, 1f, y / 128f)));
                c.Fill((x, y) => S.I(body(x, y), -S.Box(x, y, 64, 72, 56, 56, 18)), G(0.62f));
                c.Fill((x, y) => S.I(body(x, y), -S.Box(x, y, 64, 58, 54, 56, 18)), G(1f, 0.9f));
            });
            W("blk_jelly_hi", 128, c =>
            {
                c.Fill((x, y) => S.Ellipse(x, y, 52, 96, 34, 13), A(0.55f), 4f);
                c.Fill((x, y) => S.Circle(x, y, 96, 98, 6), A(0.85f), 2f);
            });

            W("blk_neon", 128, c =>
            {
                Sdf body = (x, y) => S.Box(x, y, 64, 64, 56, 56, 16);
                c.Glow(body, A(0.5f), 7f, 1.6f, false);
                c.Fill(body, G(0.14f));
                c.Fill((x, y) => S.Box(x, y, 64, 64, 40, 40, 10), (x, y) => G(0.30f, 0.6f));
                c.Stroke((x, y) => S.Box(x, y, 64, 64, 51, 51, 13), 8f, Wh);
            });
            W("blk_neon_hi", 128, c =>
            {
                c.Fill((x, y) => S.Seg(x, y, 24, 104, 46, 104, 3f), A(0.9f));
                c.Fill((x, y) => S.Seg(x, y, 24, 82, 24, 104, 3f), A(0.9f));
            });

            W("blk_jewel", 128, c =>
            {
                Sdf body = (x, y) => S.Box(x, y, 64, 64, 58, 58, 10);
                var top = S.Pts(1, 6, 122, 122, 122, 98, 98, 30, 98);
                var left = S.Pts(1, 6, 6, 6, 122, 30, 98, 30, 30);
                var right = S.Pts(1, 122, 122, 122, 6, 98, 30, 98, 98);
                var bottom = S.Pts(1, 6, 6, 122, 6, 98, 30, 30, 30);
                c.Fill(body, G(0.5f));
                c.Fill((x, y) => S.I(S.Poly(x, y, top), body(x, y)), G(1f));
                c.Fill((x, y) => S.I(S.Poly(x, y, left), body(x, y)), G(0.86f));
                c.Fill((x, y) => S.I(S.Poly(x, y, right), body(x, y)), G(0.64f));
                c.Fill((x, y) => S.I(S.Poly(x, y, bottom), body(x, y)), G(0.5f));
                c.Fill((x, y) => S.Box(x, y, 64, 64, 34, 34, 2), (x, y) => G(Mathf.Lerp(0.70f, 0.9f, y / 128f)));
            });
            W("blk_jewel_hi", 128, c =>
            {
                c.Fill((x, y) => S.Seg(x, y, 40, 84, 58, 94, 3.5f), A(0.85f));
                var star = S.Star(90, 88, 11, 3, 4, 90);
                c.Fill(P(star), A(0.9f));
            });

            W("blk_candy", 128, c =>
            {
                Sdf body = (x, y) => S.Box(x, y, 64, 64, 58, 58, 24);
                c.Fill(body, (x, y) =>
                {
                    float stripe = Mathf.Repeat((x + y) / 26f, 1f) < 0.5f ? 1f : 0.84f;
                    return G(stripe * Mathf.Lerp(0.88f, 1f, y / 128f));
                });
                c.Fill((x, y) => S.I(body(x, y), -S.Box(x, y, 64, 72, 56, 56, 22)), G(0.66f));
            });

            W("blk_pixel", 128, c =>
            {
                const float aa = 0.6f;
                c.Fill((x, y) => S.Box(x, y, 64, 64, 60, 60), G(0.42f), aa);
                c.Fill((x, y) => S.Box(x, y, 64, 64, 52, 52), G(0.78f), aa);
                c.Fill((x, y) => S.I(S.Box(x, y, 64, 64, 52, 52), 106 - y), G(1f), aa);
                c.Fill((x, y) => S.I(S.Box(x, y, 64, 64, 52, 52), x - 22), G(0.95f), aa);
                c.Fill((x, y) => S.I(S.Box(x, y, 64, 64, 52, 52), y - 22), G(0.56f), aa);
                c.Fill((x, y) => S.I(S.Box(x, y, 64, 64, 52, 52), 106 - x), G(0.62f), aa);
            });
            W("blk_pixel_hi", 128, c =>
            {
                c.Fill((x, y) => S.Box(x, y, 30, 98, 6, 6), A(0.95f), 0.6f);
                c.Fill((x, y) => S.Box(x, y, 42, 98, 6, 6), A(0.6f), 0.6f);
                c.Fill((x, y) => S.Box(x, y, 30, 86, 6, 6), A(0.6f), 0.6f);
            });

            W("blk_gold", 128, c =>
            {
                Sdf body = (x, y) => S.Box(x, y, 64, 64, 58, 58, 18);
                c.Fill(body, (x, y) => G(0.78f + 0.22f * Mathf.Sin(y / 128f * 9f + 0.6f)));
                c.Fill((x, y) => S.I(body(x, y), -S.Box(x, y, 64, 72, 56, 56, 16)), G(0.58f));
                c.Stroke((x, y) => S.Box(x, y, 64, 64, 50, 50, 14), 3f, A(0.55f));
            });

            // ---------------------------------------------------------------- board
            W("board", 256, c => c.Fill((x, y) => S.Box(x, y, 128, 128, 126, 126, 26), Wh));
            W("cell", 64, c =>
            {
                c.Fill((x, y) => S.Box(x, y, 32, 32, 28, 28, 9), (x, y) => G(Mathf.Lerp(1f, 0.82f, y / 64f)));
            });
            W("cell_glow", 64, c => c.Glow((x, y) => S.Box(x, y, 32, 32, 20, 20, 6), Wh, 11f, 1.5f));
            W("white", 16, c => c.Fill((x, y) => -2f, Wh));

            // ---------------------------------------------------------------- gems (pre-colored)
            W("gem_1", 128, c =>
            {
                Sdf h = (x, y) => S.Heart(x, y, 64, 60, 66);
                c.Fill((x, y) => h(x, y + 5) - 9f, new Color(0f, 0f, 0f, 0.35f), 6f);
                c.Fill((x, y) => h(x, y) - 7f, Wh);
                c.Fill(h, (x, y) => Color.Lerp(Pal.GemHeart, Pal.GemHeart.Lighten(0.4f), Mathf.Clamp01((y - 40f) / 60f)));
                c.Fill((x, y) => S.Ellipse(x, y, 48, 80, 10, 6), A(0.9f), 2f);
            });
            W("gem_2", 128, c =>
            {
                var star = S.Star(64, 62, 50, 22, 5, 90);
                Sdf st = (x, y) => S.Poly(x, y, star) - 4f;
                c.Fill((x, y) => st(x, y + 5) - 9f, new Color(0f, 0f, 0f, 0.35f), 6f);
                c.Fill((x, y) => st(x, y) - 7f, Wh);
                c.Fill(st, (x, y) => Color.Lerp(Pal.Hex("#FFB300"), Pal.GemStar.Lighten(0.35f), Mathf.Clamp01((y - 20f) / 80f)));
                c.Fill((x, y) => S.Ellipse(x, y, 54, 82, 8, 5), A(0.9f), 2f);
            });
            W("gem_3", 128, c =>
            {
                var gem = S.Pts(1, 32, 92, 96, 92, 114, 68, 64, 18, 14, 68);
                Sdf g = (x, y) => S.Poly(x, y, gem) - 3f;
                c.Fill((x, y) => g(x, y + 5) - 9f, new Color(0f, 0f, 0f, 0.35f), 6f);
                c.Fill((x, y) => g(x, y) - 7f, Wh);
                c.Fill(g, Pal.Hex("#1FB8E0"));
                c.Fill(P(S.Pts(1, 34, 90, 94, 90, 106, 70, 22, 70)), Pal.GemDiamond.Lighten(0.45f));
                c.Fill(P(S.Pts(1, 64, 22, 76, 70, 52, 70)), Pal.GemDiamond.Lighten(0.2f));
            });

            // ---------------------------------------------------------------- particles
            W("p_dot", 32, c => c.Fill((x, y) => S.Circle(x, y, 16, 16, 13), Wh, 2.5f));
            W("p_glow", 128, c => c.Glow((x, y) => S.Circle(x, y, 64, 64, 0), Wh, 64, 2.2f));
            W("p_spark", 64, c =>
            {
                c.Glow((x, y) => S.Circle(x, y, 32, 32, 3), A(0.6f), 16, 2f);
                c.Fill(P(S.Star(32, 32, 30, 5, 4, 90)), Wh);
            });
            W("p_shard", 32, c => c.Fill((x, y) => S.Box(x, y, 16, 16, 12, 12, 4), Wh));
            W("p_ring", 128, c =>
            {
                c.Glow((x, y) => S.Ring(S.Circle(x, y, 64, 64, 52), 1f), A(0.6f), 9f, 1.6f);
                c.Stroke((x, y) => S.Circle(x, y, 64, 64, 52), 5f, Wh);
            });
            W("p_confetti", 32, c => c.Fill((x, y) => S.Box(x, y, 16, 16, 6, 11, 2), Wh));
            W("p_star", 64, c => c.Fill(P(S.Star(32, 31, 29, 12, 5, 90)), Wh));

            // tutorial hand (pre-colored)
            W("hand", 128, c =>
            {
                Sdf hand = (x, y) => S.U(
                    S.Seg(x, y, 58, 58, 58, 112, 13f),
                    S.Box(x, y, 70, 40, 30, 26, 14),
                    S.U(S.Seg(x, y, 42, 46, 26, 70, 10f), S.U(S.Circle(x, y, 84, 64, 12), S.Circle(x, y, 96, 56, 11))));
                c.Fill((x, y) => hand(x, y) - 5f, Pal.Ink);
                c.Fill(hand, (x, y) => Color.Lerp(Pal.Hex("#FFE0C8"), Color.white, Mathf.Clamp01(y / 128f)));
            });
        }

        static void DefineUi()
        {
            U("ui_round", 64, 28, c => c.Fill((x, y) => S.Box(x, y, 32, 32, 31, 31, 26), Wh));
            U("ui_round_line", 64, 28, c => c.Stroke((x, y) => S.Box(x, y, 32, 32, 29, 29, 24), 4f, Wh));
            U("ui_pill", 64, 31, c => c.Fill((x, y) => S.Box(x, y, 32, 32, 31, 31, 31), Wh));
            U("ui_round_sm", 32, 14, c => c.Fill((x, y) => S.Box(x, y, 16, 16, 15, 15, 12), Wh));
            U("ui_circle", 64, 0, c => c.Fill((x, y) => S.Circle(x, y, 32, 32, 30.5f), Wh));
            U("ui_ring", 64, 0, c => c.Stroke((x, y) => S.Circle(x, y, 32, 32, 28), 5f, Wh));
            U("ui_disc", 256, 0, c => c.Fill((x, y) => S.Circle(x, y, 128, 128, 126), Wh));
            U("ui_shadow", 128, 60, c => c.Glow((x, y) => S.Box(x, y, 64, 64, 40, 40, 24), G(0f, 0.6f), 22f, 1.4f));
            U("ui_glow_box", 128, 60, c => c.Glow((x, y) => S.Box(x, y, 64, 64, 34, 34, 24), Wh, 28f, 1.6f, false));
            U("ui_sheen", 64, 28, c => c.Fill((x, y) => S.Box(x, y, 32, 32, 31, 31, 26), (x, y) => A(Mathf.Clamp01((y - 30f) / 34f) * 0.32f)));
            U("ui_vgrad", 64, 0, c => c.Fill((x, y) => -2f, (x, y) => A(y / 64f)));
            U("ui_radial", 128, 0, c => c.Glow((x, y) => S.Circle(x, y, 64, 64, 0), Wh, 64, 1.6f));
            U("ui_reddot", 32, 0, c =>
            {
                c.Fill((x, y) => S.Circle(x, y, 16, 16, 14), Wh);
                c.Fill((x, y) => S.Circle(x, y, 16, 16, 11), Pal.PenRed);
            });
        }
    }
}
