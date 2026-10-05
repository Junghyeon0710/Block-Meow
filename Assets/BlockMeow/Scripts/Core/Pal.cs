using UnityEngine;

namespace BlockMeow
{
    /// <summary>Colors and block themes. Pure C# so the art generator can use it from worker threads.</summary>
    public static class Pal
    {
        public static Color Hex(string hex, float alpha = 1f)
        {
            if (hex.StartsWith("#")) hex = hex.Substring(1);
            uint v = uint.Parse(hex, System.Globalization.NumberStyles.HexNumber);
            return new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f, alpha);
        }

        public static Color WithA(this Color c, float a) { c.a = a; return c; }
        public static Color Mul(this Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, c.a);
        public static Color Lighten(this Color c, float k) => Color.Lerp(c, Color.white, k);

        static bool HasBatchim(string word, out int jong)
        {
            jong = 0;
            if (string.IsNullOrEmpty(word)) return false;
            char last = word[word.Length - 1];
            if (last < 0xAC00 || last > 0xD7A3) return false;
            jong = (last - 0xAC00) % 28;
            return jong != 0;
        }

        /// <summary>Korean object particle: "치즈를", "구름을".</summary>
        public static string EulReul(string word) => word + (HasBatchim(word, out _) ? "을" : "를");

        /// <summary>Korean direction particle: "치즈로", "구름으로", "별로" (final ㄹ takes 로).</summary>
        public static string EuRo(string word) => word + (HasBatchim(word, out int j) && j != 8 ? "으로" : "로");

        /// <summary>Korean "and/with" particle: "치즈와", "구름과".</summary>
        public static string WaGwa(string word) => word + (HasBatchim(word, out _) ? "과" : "와");

        /// <summary>Korean topic particle: "치즈는", "구름은".</summary>
        public static string EunNeun(string word) => word + (HasBatchim(word, out _) ? "은" : "는");

        // UI palette: a graph-paper notebook — paper, ballpoint ink, pencil and highlighters
        public static readonly Color Paper = Hex("#FDFCF7");
        public static readonly Color Ink = Hex("#2C3A6B");
        public static readonly Color Pencil = Hex("#8E97AC");
        public static readonly Color GridLine = Hex("#6096CD", 0.2f);
        public static readonly Color Sticky = Hex("#FFF08A");

        public static readonly Color Bg = Paper;
        public static readonly Color Panel = Hex("#FFFEFA");
        public static readonly Color Panel2 = Hex("#F5F2E8");
        public static readonly Color Panel3 = Hex("#E9EDF6");
        public static readonly Color Line = Ink;
        public static readonly Color Text = Ink;
        public static readonly Color TextDim = Hex("#7D879E");
        public static readonly Color TextDark = Ink;

        // highlighters (fills)
        public static readonly Color Yellow = Hex("#FFE45C");
        public static readonly Color Orange = Hex("#FFB061");
        public static readonly Color Pink = Hex("#FF8FC8");
        public static readonly Color Red = Hex("#FF8A8A");
        public static readonly Color Green = Hex("#9BE58F");
        public static readonly Color Blue = Hex("#7CCBFF");
        public static readonly Color Cyan = Hex("#7FE3D2");
        public static readonly Color Purple = Hex("#C3A6FF");
        public static readonly Color Coin = Hex("#FFD54A");

        // pens (the same hues dark enough for text and strokes on paper)
        public static readonly Color PenYellow = Hex("#C98A00");
        public static readonly Color PenOrange = Hex("#E0731A");
        public static readonly Color PenPink = Hex("#E0408A");
        public static readonly Color PenRed = Hex("#E5484D");
        public static readonly Color PenGreen = Hex("#2E9E5B");
        public static readonly Color PenBlue = Hex("#2F7FD1");
        public static readonly Color PenCyan = Hex("#14998A");
        public static readonly Color PenPurple = Hex("#7B55D6");

        static bool Same(Color a, Color b) => Mathf.Abs(a.r - b.r) < 0.004f && Mathf.Abs(a.g - b.g) < 0.004f && Mathf.Abs(a.b - b.b) < 0.004f;

        /// <summary>Text sits on paper: white becomes ink and each highlighter becomes its pen (alpha kept).</summary>
        public static Color ForText(Color c)
        {
            Color r;
            if (c.r > 0.96f && c.g > 0.96f && c.b > 0.96f) r = Ink;
            else if (Same(c, Yellow) || Same(c, Coin)) r = PenYellow;
            else if (Same(c, Orange)) r = PenOrange;
            else if (Same(c, Pink)) r = PenPink;
            else if (Same(c, Red)) r = PenRed;
            else if (Same(c, Green)) r = PenGreen;
            else if (Same(c, Blue)) r = PenBlue;
            else if (Same(c, Cyan)) r = PenCyan;
            else if (Same(c, Purple)) r = PenPurple;
            else if (c.grayscale > 0.72f) r = TextDim; // any other pale color would vanish on the paper
            else return c;
            r.a = c.a;
            return r;
        }

        public static readonly Color GemHeart = Hex("#FF5C93");
        public static readonly Color GemStar = Hex("#FFD23F");
        public static readonly Color GemDiamond = Hex("#3BE8FF");
        public static Color GemColor(int kind) => kind == 1 ? GemHeart : kind == 2 ? GemStar : GemDiamond;
    }

    public sealed class ThemeDef
    {
        public int Index;
        public string Name, Block, Overlay;
        /// <summary>Notebook skin: empty cells are pencil squares and the line preview is a highlighter wash.</summary>
        public bool Sketch;
        public Color[] Colors;
        public Color Board, Cell, BgTop, BgBottom, Accent;
        public int CoinPrice, AdPrice;
    }

    public static class Themes
    {
        static Color[] C(params string[] hex)
        {
            var arr = new Color[hex.Length];
            for (int i = 0; i < hex.Length; i++) arr[i] = Pal.Hex(hex[i]);
            return arr;
        }

        public static readonly ThemeDef[] All =
        {
            new ThemeDef
            {
                Index = 0, Name = "노트", Block = "blk_note", Overlay = "blk_note_line", Sketch = true,
                Colors = C("#FF8FC8", "#FFB061", "#FFE45C", "#9BE58F", "#7CCBFF", "#C3A6FF", "#7FE3D2", "#FF9F8F"),
                Board = Pal.Hex("#FFFEFA"), Cell = Pal.Hex("#8E97AC"), BgTop = Pal.Paper, BgBottom = Pal.Paper,
                Accent = Pal.Ink, CoinPrice = 0, AdPrice = 0
            },
            new ThemeDef
            {
                Index = 1, Name = "젤리", Block = "blk_jelly", Overlay = "blk_jelly_hi",
                Colors = C("#FF5A7A", "#FF9F43", "#FFD93D", "#6BCB77", "#4D96FF", "#9B5DE5", "#00C2D1", "#FF7AC6"),
                Board = Pal.Hex("#1B1840"), Cell = Pal.Hex("#2A2560"), BgTop = Pal.Hex("#2A1E5C"), BgBottom = Pal.Hex("#0D0B22"),
                Accent = Pal.Hex("#FF7AC6"), CoinPrice = 500, AdPrice = 2
            },
            new ThemeDef
            {
                Index = 2, Name = "네온", Block = "blk_neon", Overlay = "blk_neon_hi",
                Colors = C("#FF2E88", "#FF8A00", "#F9F871", "#39FF14", "#00E5FF", "#B026FF", "#00FFC6", "#FF4FD8"),
                Board = Pal.Hex("#08081A"), Cell = Pal.Hex("#14143A"), BgTop = Pal.Hex("#0E0E2E"), BgBottom = Pal.Hex("#020208"),
                Accent = Pal.Hex("#00E5FF"), CoinPrice = 1000, AdPrice = 3
            },
            new ThemeDef
            {
                Index = 3, Name = "보석", Block = "blk_jewel", Overlay = "blk_jewel_hi",
                Colors = C("#E0115F", "#FF7F11", "#F4D03F", "#17A589", "#2E86C1", "#8E44AD", "#48C9B0", "#EC7063"),
                Board = Pal.Hex("#1A1030"), Cell = Pal.Hex("#2A1B4A"), BgTop = Pal.Hex("#2B1546"), BgBottom = Pal.Hex("#0B0614"),
                Accent = Pal.Hex("#48C9B0"), CoinPrice = 2000, AdPrice = 5
            },
            new ThemeDef
            {
                Index = 4, Name = "캔디", Block = "blk_candy", Overlay = "blk_jelly_hi",
                Colors = C("#FF8FAB", "#FFB563", "#FFE66D", "#8EE6A2", "#7FC8F8", "#C3A6FF", "#6EE7E7", "#FFAFCC"),
                Board = Pal.Hex("#3A2452"), Cell = Pal.Hex("#4C3168"), BgTop = Pal.Hex("#5B3577"), BgBottom = Pal.Hex("#22142F"),
                Accent = Pal.Hex("#FFAFCC"), CoinPrice = 3000, AdPrice = 6
            },
            new ThemeDef
            {
                Index = 5, Name = "픽셀", Block = "blk_pixel", Overlay = "blk_pixel_hi",
                Colors = C("#E43B44", "#F77622", "#FEE761", "#63C74D", "#0099DB", "#B55088", "#2CE8F5", "#FF0044"),
                Board = Pal.Hex("#181425"), Cell = Pal.Hex("#262B44"), BgTop = Pal.Hex("#262B44"), BgBottom = Pal.Hex("#0B0A12"),
                Accent = Pal.Hex("#FEE761"), CoinPrice = 4000, AdPrice = 8
            },
            new ThemeDef
            {
                Index = 6, Name = "황금", Block = "blk_gold", Overlay = "blk_jelly_hi",
                Colors = C("#FFD700", "#FFC125", "#F5B800", "#FFDF5A", "#F0C14B", "#FFE57F", "#E6B800", "#FFCF40"),
                Board = Pal.Hex("#1E1606"), Cell = Pal.Hex("#2E2410"), BgTop = Pal.Hex("#3A2A08"), BgBottom = Pal.Hex("#0C0802"),
                Accent = Pal.Hex("#FFD700"), CoinPrice = 8000, AdPrice = 10
            },
        };

        public static ThemeDef Current => All[Mathf.Clamp(Profile.D != null ? Profile.D.theme : 0, 0, All.Length - 1)];
    }
}
