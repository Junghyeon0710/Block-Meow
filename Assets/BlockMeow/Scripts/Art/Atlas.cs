using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace BlockMeow
{
    public struct UV
    {
        public Vector2 Min, Max;
    }

    /// <summary>
    /// Draws every sprite of the game procedurally into one texture at startup (on worker threads),
    /// then exposes them as Sprites (for uGUI) and UV rects (for the gameplay quad batches).
    /// </summary>
    public static partial class Atlas
    {
        public static Texture2D Tex { get; private set; }
        public static Material SpriteMat { get; private set; }
        public static Material AdditiveMat { get; private set; }
        public static bool Ready { get; private set; }
        public static bool FromCache { get; private set; }
        const int ArtVersion = 4; // bump whenever a drawing changes

        static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, UV> Uvs = new Dictionary<string, UV>();
        static readonly List<Def> Defs = new List<Def>();

        sealed class Def
        {
            public string Name;
            public int W, H;
            public float Ppu;
            public Vector4 Border;
            public Action<PixelCanvas> Draw;
            public RectInt Rect;
        }

        /// <summary>
        /// Play mode without a domain reload keeps statics, but the textures of the last session are destroyed:
        /// start every session from scratch.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Ready = false;
            FromCache = false;
            Tex = null;
            SpriteMat = null;
            AdditiveMat = null;
            Sprites.Clear();
            Uvs.Clear();
            Defs.Clear();
        }

        public static Sprite Get(string name)
        {
            if (Sprites.TryGetValue(name, out var s)) return s;
            Debug.LogWarning("[Atlas] missing sprite " + name);
            return Sprites.TryGetValue("white", out s) ? s : null;
        }

        public static UV Uv(string name)
        {
            if (Uvs.TryGetValue(name, out var u)) return u;
            Debug.LogWarning("[Atlas] missing uv " + name);
            return Uvs["white"];
        }

        // world sprite: 1 unit = full cell
        static void W(string name, int size, Action<PixelCanvas> draw) => Defs.Add(new Def { Name = name, W = size, H = size, Ppu = size, Draw = draw });
        // UI sprite (100 px per unit so 9-slice borders map 1:1 to canvas units)
        static void U(string name, int size, int border, Action<PixelCanvas> draw) => Defs.Add(new Def { Name = name, W = size, H = size, Ppu = 100f, Border = new Vector4(border, border, border, border), Draw = draw });

        public static IEnumerator Build()
        {
            if (Ready) yield break;
            Defs.Clear();
            DefineWorld();
            DefineUi();
            DefineIcons();
            DefineSkillIcons();
            DefineCats();
            DefineNote();

            const int pad = 2, atlasW = 2048;
            var order = new List<Def>(Defs);
            order.Sort((a, b) => b.H != a.H ? b.H.CompareTo(a.H) : string.CompareOrdinal(a.Name, b.Name));
            int x = pad, y = pad, shelf = 0;
            foreach (var d in order)
            {
                if (x + d.W + pad > atlasW) { x = pad; y += shelf + pad; shelf = 0; }
                d.Rect = new RectInt(x, y, d.W, d.H);
                x += d.W + pad;
                shelf = Mathf.Max(shelf, d.H);
            }
            int atlasH = ((y + shelf + pad + 63) / 64) * 64;

            // Builds after the first launch load the generated atlas from disk instead of re-drawing it.
            string cache = Path.Combine(Application.persistentDataPath, $"atlas_{Application.version}_{ArtVersion}_{atlasW}x{atlasH}_{Defs.Count}.png");
            bool useCache = !Application.isEditor;
            FromCache = false;
            if (useCache && File.Exists(cache))
            {
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                try
                {
                    if (tex.LoadImage(File.ReadAllBytes(cache), true) && tex.width == atlasW && tex.height == atlasH)
                    {
                        Tex = tex;
                        FromCache = true;
                    }
                }
                catch (Exception e) { Debug.LogWarning("[Atlas] cache read failed: " + e.Message); }
            }

            if (!FromCache)
            {
                var pixels = new Color32[atlasW * atlasH];
                var task = Task.Run(() => Parallel.ForEach(order, d =>
                {
                    var c = new PixelCanvas(d.W, d.H);
                    d.Draw(c);
                    c.CopyTo(pixels, atlasW, d.Rect.x, d.Rect.y);
                }));
                while (!task.IsCompleted) yield return null;
                if (task.Exception != null) Debug.LogException(task.Exception);

                Tex = new Texture2D(atlasW, atlasH, TextureFormat.RGBA32, false);
                Tex.SetPixels32(pixels);
                Tex.Apply(false, !useCache);
                if (useCache)
                {
                    try
                    {
                        var png = Tex.EncodeToPNG();
                        Task.Run(() => { try { File.WriteAllBytes(cache, png); } catch { /* cache is optional */ } });
                    }
                    catch (Exception e) { Debug.LogWarning("[Atlas] cache write failed: " + e.Message); }
                    Tex.Apply(false, true);
                }
            }
            Tex.name = "BlockMeowAtlas";
            Tex.filterMode = FilterMode.Bilinear;
            Tex.wrapMode = TextureWrapMode.Clamp;

            foreach (var d in Defs)
            {
                var r = new Rect(d.Rect.x, d.Rect.y, d.W, d.H);
                var sp = Sprite.Create(Tex, r, new Vector2(0.5f, 0.5f), d.Ppu, 0, SpriteMeshType.FullRect, d.Border);
                sp.name = d.Name;
                Sprites[d.Name] = sp;
                Uvs[d.Name] = new UV
                {
                    Min = new Vector2((r.x + 0.5f) / atlasW, (r.y + 0.5f) / atlasH),
                    Max = new Vector2((r.xMax - 0.5f) / atlasW, (r.yMax - 0.5f) / atlasH)
                };
            }

            var sh = Resources.Load<Shader>("Shaders/BMSprite");
            if (sh == null) sh = Shader.Find("BlockMeow/Sprite");
            var ah = Resources.Load<Shader>("Shaders/BMAdditive");
            if (ah == null) ah = Shader.Find("BlockMeow/Additive");
            SpriteMat = new Material(sh) { name = "BM_Sprite", mainTexture = Tex };
            AdditiveMat = new Material(ah) { name = "BM_Additive", mainTexture = Tex };
            Ready = true;
        }

        // ------------------------------------------------------------------ shared drawing helpers

        static readonly Color Wh = Color.white;
        static Color A(float a) => new Color(1f, 1f, 1f, a);
        static Color G(float v, float a = 1f) => new Color(v, v, v, a);
        static Sdf P(Vector2[] pts) => (px, py) => S.Poly(px, py, pts);

        static Sdf Lines(List<Vector2> pts, float r)
        {
            var arr = pts.ToArray();
            return (px, py) =>
            {
                float d = float.MaxValue;
                for (int i = 0; i < arr.Length - 1; i++) d = Mathf.Min(d, S.Seg(px, py, arr[i].x, arr[i].y, arr[i + 1].x, arr[i + 1].y, r));
                return d;
            };
        }

        static List<Vector2> Arc(float cx, float cy, float r, float a0, float a1, int steps)
        {
            var l = new List<Vector2>();
            for (int i = 0; i <= steps; i++)
            {
                float a = Mathf.Lerp(a0, a1, i / (float)steps) * Mathf.Deg2Rad;
                l.Add(new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r));
            }
            return l;
        }

        /// <summary>Colored icon in the notebook style: flat highlighter fill inside a slightly wobbly ballpoint line.</summary>
        static void Icon(PixelCanvas c, Sdf sdf, Color col, float glow = 0.06f, bool outline = true)
        {
            float s = c.VW;
            c.Fill(sdf, col);
            if (outline) c.Fill(Pen(sdf, s * 0.036f, col.r * 7f + col.b * 3f, s * 0.006f), Pal.Ink);
        }

        // ------------------------------------------------------------------ app icon

        static void EnsureDefs()
        {
            if (Defs.Count > 0) return;
            DefineWorld();
            DefineUi();
            DefineIcons();
            DefineSkillIcons();
            DefineCats();
            DefineNote();
        }

        static Action<PixelCanvas> DrawOf(string name)
        {
            foreach (var d in Defs) if (d.Name == name) return d.Draw;
            return null;
        }

        /// <summary>Renders the store/app icon (cat on jelly blocks) at any size; returns RGBA pixels, bottom row first.</summary>
        public static Color32[] RenderAppIcon(int size)
        {
            EnsureDefs();
            var cat = Cats.Get(0);
            var icon = new PixelCanvas(size, size) { K = 256f / size };
            // graph paper with one square per board cell
            icon.Fill((x, y) => S.Box(x, y, 128, 128, 128, 128, 0), Pal.Paper);
            for (int i = 1; i < 8; i++)
            {
                float g = i * 32f;
                icon.Fill((x, y) => Mathf.Abs(x - g) - 0.8f, Pal.GridLine, 0.8f);
                icon.Fill((x, y) => Mathf.Abs(y - g) - 0.8f, Pal.GridLine, 0.8f);
            }
            // highlighter blocks peeking from the bottom, outlined in pen
            var blocks = new[] { (40f, 34f, Pal.Pink), (96f, 22f, Pal.Yellow), (160f, 22f, Pal.Blue), (216f, 34f, Pal.Green) };
            float seed = 1f;
            foreach (var (bx, by, col) in blocks)
            {
                float sd = seed++;
                Sdf box = (x, y) => S.Box(x, y, bx, by, 25, 25, 7) + Wob(x, y, sd, 0.9f);
                icon.Fill(box, col);
                icon.Fill(Pen(box, 4.4f, sd + 3f, 0.8f), Pal.Ink);
            }
            // the mascot, composited layer by layer with its tints
            void Layer(string name, Color tint)
            {
                var draw = DrawOf(name);
                if (draw == null) return;
                var layer = new PixelCanvas(size, size) { K = 256f / size };
                draw(layer);
                icon.Composite(layer, tint);
            }
            var shadow = new PixelCanvas(size, size) { K = 256f / size };
            shadow.Fill((x, y) => S.Ellipse(x, y, 128, 52, 80, 14), Pal.Ink.WithA(0.16f), 6f);
            icon.Composite(shadow, Color.white);
            Layer("cat_head", cat.Fur);
            Layer("cat_tabby", cat.PatternColor);
            Layer("cat_ear_in", Color.white);
            Layer("cat_line", Color.white);
            Layer("cat_muzzle", Color.white);
            Layer("cat_blush", Color.white);
            Layer("cat_whisk", Color.white);
            Layer("cat_eye_open", Color.white);
            Layer("cat_mouth_smile", Color.white);
            return icon.ToColor32();
        }
    }
}
