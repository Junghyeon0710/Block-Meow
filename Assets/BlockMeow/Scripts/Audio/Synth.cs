using System;
using UnityEngine;

namespace BlockMeow
{
    public enum Sfx
    {
        Pick, Place, Invalid, Clear, Combo, Fever, AllClear, GameOver, NewBest, Win, Gem, Hammer, Refresh, Rotate,
        Click, Coin, SpinTick, Reward, Meow, Deal, Unlock, Revive, Pop, Skill, Ready, Count
    }

    public enum Music { None, Home, Game, Fever }

    /// <summary>Software synthesizer: every sound effect and music loop is generated in code.</summary>
    public static class Synth
    {
        public const int SR = 22050;
        const float TwoPi = Mathf.PI * 2f;

        struct Rng
        {
            uint _s;
            public Rng(uint seed) { _s = seed * 2654435761u + 12345u; }
            public float Next() { _s ^= _s << 13; _s ^= _s >> 17; _s ^= _s << 5; return _s / 4294967295f * 2f - 1f; }
        }

        static float Frac(float x) => x - Mathf.Floor(x);
        static float Sin(float ph) => Mathf.Sin(ph * TwoPi);
        static float Sq(float ph, float duty = 0.5f) => Frac(ph) < duty ? 1f : -1f;
        static float Saw(float ph) => 2f * Frac(ph) - 1f;
        static float Tri(float ph) => 1f - 4f * Mathf.Abs(Frac(ph) - 0.5f);
        public static float Midi(float n) => 440f * Mathf.Pow(2f, (n - 69f) / 12f);
        static float Lp(float fc) => 1f - Mathf.Exp(-TwoPi * fc / SR);

        static float[] Make(float dur, Func<float, float> f)
        {
            int n = Mathf.CeilToInt(dur * SR);
            var b = new float[n];
            for (int i = 0; i < n; i++) b[i] = f(i / (float)SR);
            int fade = Mathf.Min(n, 80);
            for (int i = 0; i < fade; i++) b[n - 1 - i] *= i / (float)fade;
            return b;
        }

        /// <summary>Bell-like tone: sine partials with exponential decay.</summary>
        static float Bell(float t, float f, float decay)
        {
            return (Sin(f * t) + 0.5f * Sin(f * 2.01f * t) + 0.22f * Sin(f * 3.99f * t)) * Mathf.Exp(-t * decay);
        }

        static float[] Notes(int[] notes, float step, float tail, float vol, float decay = 9f)
        {
            float dur = step * notes.Length + tail;
            return Make(dur, t =>
            {
                float s = 0f;
                for (int k = 0; k < notes.Length; k++)
                {
                    float lt = t - k * step;
                    if (lt < 0f) break;
                    s += Bell(lt, Midi(notes[k]), k == notes.Length - 1 ? decay * 0.5f : decay) * 0.5f;
                }
                return s * vol;
            });
        }

        public static float[] MakeSfx(Sfx id)
        {
            switch (id)
            {
                case Sfx.Pick:
                {
                    float ph = 0;
                    return Make(0.08f, t => { ph += Mathf.Lerp(500f, 900f, t / 0.08f) / SR; return Sin(ph) * Mathf.Exp(-t * 30f) * 0.45f; });
                }
                case Sfx.Place:
                {
                    var r = new Rng(3); float ph = 0, lp = 0;
                    return Make(0.16f, t =>
                    {
                        ph += (180f * Mathf.Exp(-t * 25f) + 90f) / SR;
                        lp += 0.25f * (r.Next() - lp);
                        return (Sin(ph) * 0.8f + lp * 0.5f * Mathf.Exp(-t * 60f)) * Mathf.Exp(-t * 22f) * 0.7f;
                    });
                }
                case Sfx.Invalid:
                {
                    float ph = 0;
                    return Make(0.18f, t => { ph += 140f / SR; return (Sq(ph) * 0.2f + Sin(ph) * 0.3f) * Mathf.Exp(-t * 14f) * 0.5f; });
                }
                case Sfx.Clear: return Notes(new[] { 76, 83, 88 }, 0.035f, 0.5f, 0.42f, 7f);
                case Sfx.Combo: return Notes(new[] { 72, 76, 79, 84, 88 }, 0.05f, 0.45f, 0.4f, 8f);
                case Sfx.AllClear: return Notes(new[] { 72, 76, 79, 84, 79, 84, 88, 91, 96 }, 0.07f, 0.9f, 0.4f, 6f);
                case Sfx.NewBest: return Notes(new[] { 67, 72, 76, 79, 84, 88 }, 0.09f, 1.0f, 0.42f, 5f);
                case Sfx.Win: return Notes(new[] { 60, 64, 67, 72, 76, 79, 84 }, 0.08f, 1.1f, 0.42f, 5f);
                case Sfx.Reward: return Notes(new[] { 79, 84, 91 }, 0.06f, 0.5f, 0.38f, 7f);
                case Sfx.Unlock: return Notes(new[] { 72, 79, 84, 88, 91 }, 0.07f, 0.9f, 0.4f, 5f);
                case Sfx.Gem: return Notes(new[] { 88, 95 }, 0.04f, 0.3f, 0.35f, 12f);
                case Sfx.Fever:
                {
                    float ph = 0;
                    var r = new Rng(9);
                    return Make(0.9f, t =>
                    {
                        ph += Mathf.Lerp(300f, 1500f, Mathf.Pow(t / 0.9f, 0.6f)) / SR;
                        return (Saw(ph) * 0.12f + Sin(ph) * 0.25f + r.Next() * 0.05f) * Mathf.Sin(t / 0.9f * Mathf.PI) * 0.7f;
                    });
                }
                case Sfx.GameOver:
                {
                    int[] notes = { 67, 63, 60, 55 };
                    const float step = 0.24f;
                    float ph = 0;
                    return Make(step * notes.Length + 0.5f, t =>
                    {
                        int k = Mathf.Min(notes.Length - 1, (int)(t / step));
                        float lt = t - k * step;
                        ph += Midi(notes[k] + Mathf.Sin(t * 28f) * 0.12f) / SR;
                        return (Tri(ph) * 0.55f + Sin(ph * 2f) * 0.1f) * Mathf.Exp(-lt * (k == notes.Length - 1 ? 2.5f : 6f)) * 0.5f;
                    });
                }
                case Sfx.Hammer:
                {
                    var r = new Rng(5); float lp = 0, ph = 0;
                    return Make(0.35f, t =>
                    {
                        lp += Lp(Mathf.Lerp(4000f, 400f, t / 0.35f)) * (r.Next() - lp);
                        ph += 120f / SR;
                        return (lp * 0.8f + Sin(ph) * 0.5f * Mathf.Exp(-t * 20f)) * Mathf.Exp(-t * 11f) * 0.7f;
                    });
                }
                case Sfx.Refresh:
                case Sfx.Deal:
                {
                    var r = new Rng(7); float lp = 0, bp = 0;
                    float dur = id == Sfx.Deal ? 0.18f : 0.32f;
                    return Make(dur, t =>
                    {
                        float k = t / dur;
                        float a = Lp(Mathf.Lerp(600f, 3000f, k));
                        lp += a * (r.Next() - lp); bp += a * (lp - bp);
                        return (lp - bp) * Mathf.Sin(k * Mathf.PI) * (id == Sfx.Deal ? 0.5f : 0.9f);
                    });
                }
                case Sfx.Rotate:
                {
                    float ph = 0;
                    return Make(0.12f, t => { ph += Mathf.Lerp(700f, 1300f, t / 0.12f) / SR; return Tri(ph) * Mathf.Exp(-t * 20f) * 0.4f; });
                }
                case Sfx.Click:
                {
                    float ph = 0;
                    return Make(0.05f, t => { ph += 1250f / SR; return Sin(ph) * Mathf.Exp(-t * 90f) * 0.35f; });
                }
                case Sfx.Pop:
                {
                    float ph = 0;
                    return Make(0.1f, t => { ph += (900f * Mathf.Exp(-t * 20f) + 300f) / SR; return Sin(ph) * Mathf.Exp(-t * 26f) * 0.45f; });
                }
                case Sfx.SpinTick:
                {
                    float ph = 0;
                    return Make(0.03f, t => { ph += 2200f / SR; return Sq(ph, 0.3f) * Mathf.Exp(-t * 150f) * 0.25f; });
                }
                case Sfx.Coin:
                {
                    float ph = 0;
                    return Make(0.25f, t =>
                    {
                        float f = t < 0.06f ? 987.8f : 1318.5f;
                        ph += f / SR;
                        float env = t < 0.06f ? 1f : Mathf.Exp(-(t - 0.06f) * 14f);
                        return (Sq(ph, 0.25f) * 0.15f + Sin(ph) * 0.3f) * env;
                    });
                }
                case Sfx.Revive: return Notes(new[] { 60, 67, 72, 76, 79, 84 }, 0.05f, 0.7f, 0.4f, 6f);
                case Sfx.Meow: return Meow();
                case Sfx.Ready: return Notes(new[] { 84, 91, 96 }, 0.05f, 0.4f, 0.32f, 9f);
                case Sfx.Skill:
                {
                    // airy whoosh under a fast rising sparkle
                    var r = new Rng(11); float lp = 0;
                    int[] notes = { 72, 79, 84, 88, 91, 96 };
                    const float dur = 0.85f;
                    return Make(dur, t =>
                    {
                        float k = t / dur;
                        lp += Lp(Mathf.Lerp(700f, 5000f, k)) * (r.Next() - lp);
                        float whoosh = lp * Mathf.Sin(Mathf.Min(1f, k * 1.7f) * Mathf.PI) * 0.4f;
                        float s = 0f;
                        for (int i = 0; i < notes.Length; i++)
                        {
                            float lt = t - i * 0.045f;
                            if (lt < 0f) break;
                            s += Bell(lt, Midi(notes[i]), 7f) * 0.12f;
                        }
                        return whoosh + s;
                    });
                }
            }
            return new float[64];
        }

        /// <summary>A synthesized "meow": glottal saw through two moving formant filters.</summary>
        static float[] Meow()
        {
            const float dur = 0.62f;
            float ph = 0f;
            float l1 = 0, b1 = 0, l2 = 0, b2 = 0;
            return Make(dur, t =>
            {
                float k = t / dur;
                float pitch = k < 0.35f ? Mathf.Lerp(520f, 780f, k / 0.35f) : Mathf.Lerp(780f, 470f, (k - 0.35f) / 0.65f);
                pitch *= 1f + 0.015f * Mathf.Sin(t * 38f);
                ph += pitch / SR;
                float src = Saw(ph);
                // formants: "mi" -> "a" -> "u"
                float f1 = k < 0.4f ? Mathf.Lerp(380f, 950f, k / 0.4f) : Mathf.Lerp(950f, 450f, (k - 0.4f) / 0.6f);
                float f2 = k < 0.4f ? Mathf.Lerp(2300f, 1400f, k / 0.4f) : Mathf.Lerp(1400f, 850f, (k - 0.4f) / 0.6f);
                float c1 = 2f * Mathf.Sin(Mathf.PI * f1 / SR), c2 = 2f * Mathf.Sin(Mathf.PI * f2 / SR);
                const float q = 0.18f;
                float h1 = src - l1 - q * b1; b1 += c1 * h1; l1 += c1 * b1;
                float h2 = src - l2 - q * b2; b2 += c2 * h2; l2 += c2 * b2;
                float env = Mathf.Min(1f, t / 0.05f) * Mathf.Clamp01((dur - t) / 0.18f);
                return (b1 * 0.5f + b2 * 0.32f) * env * 0.35f;
            });
        }

        // ------------------------------------------------------------------ music

        static readonly int[][] HomeChords =
        {
            new[] { 41, 53, 57, 60, 64 }, new[] { 40, 52, 55, 59, 62 }, new[] { 38, 50, 53, 57, 60 }, new[] { 36, 48, 52, 55, 59 },
        };

        static readonly int[][] GameChords =
        {
            new[] { 45, 57, 60, 64, 67 }, new[] { 41, 53, 57, 60, 64 }, new[] { 36, 48, 52, 55, 59 }, new[] { 43, 55, 59, 62, 64 },
        };

        static readonly int[][] FeverChords =
        {
            new[] { 45, 57, 60, 64, 69 }, new[] { 41, 53, 57, 60, 65 }, new[] { 48, 60, 64, 67, 72 }, new[] { 43, 55, 59, 62, 67 },
        };

        public static float[] MakeMusic(Music m)
        {
            switch (m)
            {
                case Music.Home: return Render(82, HomeChords, 2, 0);
                case Music.Fever: return Render(128, FeverChords, 2, 2);
                default: return Render(96, GameChords, 2, 1);
            }
        }

        /// <summary>Renders a seamless loop: chords * repeats bars, one chord per bar.</summary>
        static float[] Render(int bpm, int[][] chords, int repeats, int style)
        {
            float beat = 60f / bpm, eighth = beat / 2f;
            int bars = chords.Length * repeats;
            int total = Mathf.RoundToInt(bars * 4 * beat * SR);
            var mix = new float[total];
            var rng = new Rng((uint)(bpm * 31 + style));
            float swing = style == 0 ? 0.14f : style == 1 ? 0.08f : 0f;

            void Add(int at, float v) => mix[((at % total) + total) % total] += v;

            float Pos(int bar, int step8) // step8: 0..7 eighth notes in the bar
            {
                float t = bar * 4 * beat + step8 * eighth;
                if ((step8 & 1) == 1) t += swing * eighth;
                return t;
            }

            void Kick(float t, float vol)
            {
                int s = (int)(t * SR); float ph = 0;
                for (int i = 0; i < (int)(0.3f * SR); i++)
                {
                    float lt = i / (float)SR;
                    ph += (48f + 90f * Mathf.Exp(-lt * 30f)) / SR;
                    Add(s + i, Sin(ph) * Mathf.Exp(-lt * 10f) * vol);
                }
            }

            void Snare(float t, float vol, bool clap)
            {
                int s = (int)(t * SR); float lp = 0, ph = 0;
                for (int i = 0; i < (int)(0.22f * SR); i++)
                {
                    float lt = i / (float)SR;
                    float n = rng.Next(); lp += 0.45f * (n - lp);
                    ph += 200f / SR;
                    float env = clap ? Mathf.Exp(-lt * 22f) * (1f + 0.6f * Mathf.Sin(lt * 300f)) : Mathf.Exp(-lt * 18f);
                    Add(s + i, ((n - lp) * 0.6f + (clap ? 0f : Sin(ph) * 0.25f * Mathf.Exp(-lt * 35f))) * env * vol);
                }
            }

            void Hat(float t, float vol, float len)
            {
                int s = (int)(t * SR); float lp = 0;
                for (int i = 0; i < (int)(len * SR); i++)
                {
                    float lt = i / (float)SR;
                    float n = rng.Next(); lp += 0.65f * (n - lp);
                    Add(s + i, (n - lp) * Mathf.Exp(-lt / len * 4f) * vol);
                }
            }

            void Tone(float t, float note, float len, float vol, int voice)
            {
                int s = (int)(t * SR);
                float f = Midi(note), ph = 0, lp = 0;
                int n = (int)((len + 0.4f) * SR);
                for (int i = 0; i < n; i++)
                {
                    float lt = i / (float)SR;
                    ph += f / SR;
                    float v;
                    switch (voice)
                    {
                        case 0: // electric piano
                            v = (Sin(ph) + 0.35f * Sin(ph * 2f) + 0.12f * Sin(ph * 3f)) * Mathf.Exp(-lt * 2.2f) * (1f + 0.08f * Mathf.Sin(lt * 30f));
                            break;
                        case 1: // pluck
                            lp += Lp(2600f * Mathf.Exp(-lt * 6f) + 300f) * ((Sq(ph, 0.3f) * 0.6f + Tri(ph) * 0.4f) - lp);
                            v = lp * Mathf.Exp(-lt * 7f);
                            break;
                        case 2: // bass
                            v = (Sin(ph) * 0.85f + Tri(ph) * 0.25f) * Mathf.Min(1f, lt * 200f) * Mathf.Exp(-lt * 1.5f);
                            break;
                        default: // bright saw lead
                            lp += Lp(3500f) * (Saw(ph) - lp);
                            v = lp * Mathf.Exp(-lt * 5f) * 0.7f;
                            break;
                    }
                    float release = lt > len ? Mathf.Exp(-(lt - len) * 18f) : 1f;
                    Add(s + i, v * release * vol);
                }
            }

            for (int bar = 0; bar < bars; bar++)
            {
                var ch = chords[bar % chords.Length];
                int bass = ch[0];
                switch (style)
                {
                    case 0: // home: lazy lo-fi
                        for (int k = 1; k < 5; k++) Tone(Pos(bar, 0) + k * 0.012f, ch[k], beat * 1.8f, 0.07f, 0);
                        for (int k = 1; k < 5; k++) Tone(Pos(bar, 5) + k * 0.012f, ch[k], beat * 1.2f, 0.045f, 0);
                        Tone(Pos(bar, 0), bass, beat * 1.5f, 0.22f, 2);
                        Tone(Pos(bar, 5), bass + 7, beat * 0.8f, 0.12f, 2);
                        Kick(Pos(bar, 0), 0.5f); Kick(Pos(bar, 5), 0.3f);
                        Snare(Pos(bar, 2), 0.16f, false); Snare(Pos(bar, 6), 0.16f, false);
                        for (int s = 0; s < 8; s++) Hat(Pos(bar, s), s % 2 == 0 ? 0.05f : 0.03f, 0.05f);
                        break;
                    case 1: // game: soft groove with a pluck arpeggio
                        for (int k = 1; k < 5; k++) Tone(Pos(bar, 0), ch[k], beat * 1.5f, 0.05f, 0);
                        int[] arp = { 1, 2, 3, 4, 3, 2, 3, 4 };
                        for (int s = 0; s < 8; s++) Tone(Pos(bar, s), ch[arp[s]] + 12, eighth * 0.9f, 0.06f, 1);
                        for (int s = 0; s < 8; s += 2) Tone(Pos(bar, s), bass, eighth * 1.6f, 0.2f, 2);
                        Kick(Pos(bar, 0), 0.6f); Kick(Pos(bar, 4), 0.55f);
                        Snare(Pos(bar, 2), 0.2f, false); Snare(Pos(bar, 6), 0.2f, false);
                        for (int s = 0; s < 8; s++) Hat(Pos(bar, s), s % 2 == 1 ? 0.06f : 0.035f, 0.04f);
                        break;
                    default: // fever: four on the floor
                        for (int s = 0; s < 8; s += 2) Kick(Pos(bar, s), 0.75f);
                        Snare(Pos(bar, 2), 0.3f, true); Snare(Pos(bar, 6), 0.3f, true);
                        for (int s = 1; s < 8; s += 2) Hat(Pos(bar, s), 0.09f, 0.09f);
                        for (int s = 0; s < 8; s++) Tone(Pos(bar, s), bass + (s % 2 == 1 ? 12 : 0), eighth * 0.8f, 0.2f, 2);
                        for (int s = 0; s < 16; s++)
                        {
                            float t = bar * 4 * beat + s * beat / 4f;
                            Tone(t, ch[1 + s % 4] + 12, beat / 4f * 0.8f, 0.045f, 3);
                        }
                        for (int k = 1; k < 4; k++) Tone(Pos(bar, 0), ch[k], beat * 3.5f, 0.035f, 0);
                        break;
                }
            }

            if (style == 0)
            {
                // vinyl crackle
                for (int i = 0; i < total; i += 1)
                    if (rng.Next() > 0.9985f) mix[i] += rng.Next() * 0.08f;
            }

            float peak = 0.0001f;
            for (int i = 0; i < total; i++) peak = Mathf.Max(peak, Mathf.Abs(mix[i]));
            float g = 0.85f / peak;
            for (int i = 0; i < total; i++) mix[i] = (float)Math.Tanh(mix[i] * g * 1.1f) * 0.85f;
            return mix;
        }
    }
}
