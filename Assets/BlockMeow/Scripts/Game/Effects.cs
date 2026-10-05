using UnityEngine;

namespace BlockMeow
{
    /// <summary>Particles, popping blocks, shock rings and confetti for the board.</summary>
    public sealed class Effects
    {
        struct Part
        {
            public Vector2 Pos, Vel;
            public float Life, Max, S0, S1, Rot, RotSpd, Drag, Gravity, Delay;
            public Color32 C0, C1;
            public UV Uv;
            public bool Add;
        }

        struct PopFx { public Vector2 Pos; public float Delay, T; public byte Color, Gem; public bool Emitted; }
        struct RingFx { public Vector2 Pos; public float R0, R1, Life, Max; public Color32 Col; }

        const int MaxP = 1400, MaxPop = 160, MaxRing = 32;
        readonly Part[] _p = new Part[MaxP];
        readonly PopFx[] _pop = new PopFx[MaxPop];
        readonly RingFx[] _ring = new RingFx[MaxRing];
        int _pc, _popc, _rc;
        readonly UV _dot, _glow, _spark, _shard, _ringUv, _confetti, _star;
        readonly UV[] _gems = new UV[4];

        public Effects()
        {
            _dot = Atlas.Uv("p_dot"); _glow = Atlas.Uv("p_glow"); _spark = Atlas.Uv("p_spark"); _shard = Atlas.Uv("p_shard");
            _ringUv = Atlas.Uv("p_ring"); _confetti = Atlas.Uv("p_confetti"); _star = Atlas.Uv("p_star");
            for (int i = 1; i <= 3; i++) _gems[i] = Atlas.Uv("gem_" + i);
        }

        public void Clear() { _pc = _popc = _rc = 0; }
        public int PopCount => _popc;

        public void Burst(Vector2 pos, Color col, int n, float vMin, float vMax, float sMin, float sMax, float life,
            string sprite = "p_shard", bool additive = false, float gravity = -9f, float drag = 1.5f, float delay = 0f)
        {
            UV uv = sprite == "p_shard" ? _shard : sprite == "p_spark" ? _spark : sprite == "p_dot" ? _dot : sprite == "p_star" ? _star : Atlas.Uv(sprite);
            for (int i = 0; i < n && _pc < MaxP; i++)
            {
                float a = Random.value * Mathf.PI * 2f;
                float v = Random.Range(vMin, vMax);
                float l = life * Random.Range(0.75f, 1.2f);
                _p[_pc++] = new Part
                {
                    Pos = pos, Vel = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * v + Vector2.up * v * 0.35f,
                    Life = l, Max = l, S0 = Random.Range(sMin, sMax), S1 = 0f,
                    Rot = Random.value * 6.28f, RotSpd = Random.Range(-8f, 8f), Drag = drag, Gravity = gravity, Delay = delay,
                    C0 = col, C1 = new Color(col.r, col.g, col.b, 0f), Uv = uv, Add = additive
                };
            }
        }

        public void PopBlock(Vector2 pos, byte color, byte gem, float delay)
        {
            if (_popc >= MaxPop) return;
            _pop[_popc++] = new PopFx { Pos = pos, Color = color, Gem = gem, Delay = delay };
        }

        public void Ring(Vector2 pos, float r0, float r1, float life, Color col)
        {
            if (_rc >= MaxRing) return;
            _ring[_rc++] = new RingFx { Pos = pos, R0 = r0, R1 = r1, Life = life, Max = life, Col = col };
        }

        public void Confetti(Vector2 center, float width, int n, Color[] palette)
        {
            for (int i = 0; i < n && _pc < MaxP; i++)
            {
                var col = palette[Random.Range(0, palette.Length)];
                float l = Random.Range(1.6f, 2.6f);
                _p[_pc++] = new Part
                {
                    Pos = center + new Vector2(Random.Range(-width, width) * 0.5f, Random.Range(-1f, 1f)),
                    Vel = new Vector2(Random.Range(-3f, 3f), Random.Range(4f, 11f)),
                    Life = l, Max = l, S0 = Random.Range(0.18f, 0.3f), S1 = 0.12f, Rot = Random.value * 6.28f, RotSpd = Random.Range(-10f, 10f),
                    Drag = 1.2f, Gravity = -9f, Delay = Random.Range(0f, 0.25f),
                    C0 = col, C1 = new Color(col.r, col.g, col.b, 0f), Uv = _confetti, Add = false
                };
            }
        }

        public void Update(float dt, ThemeDef theme)
        {
            for (int i = _pc - 1; i >= 0; i--)
            {
                ref var p = ref _p[i];
                if (p.Delay > 0f) { p.Delay -= dt; continue; }
                p.Life -= dt;
                if (p.Life <= 0f) { _p[i] = _p[--_pc]; continue; }
                p.Vel.y += p.Gravity * dt;
                p.Vel *= Mathf.Exp(-p.Drag * dt);
                p.Pos += p.Vel * dt;
                p.Rot += p.RotSpd * dt;
            }
            for (int i = _popc - 1; i >= 0; i--)
            {
                ref var b = ref _pop[i];
                if (b.Delay > 0f) { b.Delay -= dt; continue; }
                if (!b.Emitted)
                {
                    b.Emitted = true;
                    var col = theme.Colors[Mathf.Clamp(b.Color - 1, 0, 7)];
                    Burst(b.Pos, col, 6, 2f, 6f, 0.16f, 0.3f, 0.6f);
                    Burst(b.Pos, Color.white, 2, 1f, 3f, 0.25f, 0.45f, 0.35f, "p_spark", true, 0f, 3f);
                }
                b.T += dt;
                if (b.T >= 0.3f) _pop[i] = _pop[--_popc];
            }
            for (int i = _rc - 1; i >= 0; i--)
            {
                _ring[i].Life -= dt;
                if (_ring[i].Life <= 0f) _ring[i] = _ring[--_rc];
            }
        }

        static readonly Color InkCol = Pal.Ink;

        /// <summary>Light that would glow on a dark screen is drawn as an ink mark on paper.</summary>
        static Color32 OnPaper(Color32 c)
        {
            Color col = c;
            var r = Color.Lerp(col, InkCol, 0.6f);
            r.a = col.a;
            return r;
        }

        public void Draw(QuadBatch blocks, QuadBatch alpha, QuadBatch add, ThemeDef theme)
        {
            var baseUv = Atlas.Uv(theme.Block);
            var hiUv = Atlas.Uv(theme.Overlay);
            for (int i = 0; i < _popc; i++)
            {
                ref var b = ref _pop[i];
                float k = b.Delay > 0f ? 0f : b.T / 0.3f;
                float s = k < 0.35f ? 1f + 0.22f * Mathf.Sin(k / 0.35f * Mathf.PI) : Mathf.Lerp(1f, 0f, (k - 0.35f) / 0.65f);
                Color col = theme.Colors[Mathf.Clamp(b.Color - 1, 0, 7)];
                if (b.Delay <= 0f && k < 0.2f) col = Color.Lerp(col, Color.white, 0.6f);
                blocks.Add(b.Pos.x, b.Pos.y, s, s, 0f, baseUv, col);
                blocks.Add(b.Pos.x, b.Pos.y, s, s, 0f, hiUv, new Color32(255, 255, 255, 255));
                if (b.Gem > 0) blocks.Add(b.Pos.x, b.Pos.y, s * 0.62f, s * 0.62f, 0f, _gems[b.Gem], new Color32(255, 255, 255, 255));
            }
            for (int i = 0; i < _pc; i++)
            {
                ref var p = ref _p[i];
                if (p.Delay > 0f) continue;
                float k = 1f - p.Life / p.Max;
                float s = Mathf.Lerp(p.S0, p.S1, k);
                var c = Color32.Lerp(p.C0, p.C1, k * k);
                if (p.Add) c = OnPaper(c);
                alpha.Add(p.Pos.x, p.Pos.y, s, s, p.Rot, p.Uv, c);
            }
            for (int i = 0; i < _rc; i++)
            {
                ref var r = ref _ring[i];
                float k = 1f - r.Life / r.Max;
                float rad = Mathf.Lerp(r.R0, r.R1, 1f - (1f - k) * (1f - k));
                var c = OnPaper(r.Col);
                c.a = (byte)(c.a * (1f - k));
                float size = rad / 0.41f;
                alpha.Add(r.Pos.x, r.Pos.y, size, size, 0f, _ringUv, c);
            }
        }
    }
}
