using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace BlockMeow
{
    public enum Ease { Linear, InQuad, OutQuad, InOutQuad, OutCubic, OutBack, InBack, OutElastic, OutBounce }

    public sealed class Tween
    {
        internal float Duration, Elapsed, Delay;
        internal Ease EaseType = Ease.OutQuad;
        internal bool Killed, Loop;
        internal Action<float> Step;
        internal Action Done;
        internal UnityEngine.Object Owner;
        internal bool HasOwner;

        public Tween SetEase(Ease e) { EaseType = e; return this; }
        public Tween SetDelay(float d) { Delay = d; return this; }
        public Tween OnComplete(Action a) { Done = a; return this; }
        public Tween Looping() { Loop = true; return this; }
        public void Kill() => Killed = true;

        public static float Eval(Ease e, float t)
        {
            switch (e)
            {
                case Ease.InQuad: return t * t;
                case Ease.OutQuad: return 1f - (1f - t) * (1f - t);
                case Ease.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) * 0.5f;
                case Ease.OutCubic: return 1f - Mathf.Pow(1f - t, 3f);
                case Ease.OutBack: { const float c1 = 1.70158f, c3 = c1 + 1f; return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f); }
                case Ease.InBack: { const float c1 = 1.70158f, c3 = c1 + 1f; return c3 * t * t * t - c1 * t * t; }
                case Ease.OutElastic:
                    if (t <= 0f) return 0f;
                    if (t >= 1f) return 1f;
                    return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
                case Ease.OutBounce:
                {
                    const float n1 = 7.5625f, d1 = 2.75f;
                    if (t < 1f / d1) return n1 * t * t;
                    if (t < 2f / d1) { t -= 1.5f / d1; return n1 * t * t + 0.75f; }
                    if (t < 2.5f / d1) { t -= 2.25f / d1; return n1 * t * t + 0.9375f; }
                    t -= 2.625f / d1; return n1 * t * t + 0.984375f;
                }
                default: return t;
            }
        }
    }

    /// <summary>Minimal tween runner on unscaled time, so menus animate while gameplay is paused.</summary>
    public sealed class Tweener : MonoBehaviour
    {
        static Tweener _i;
        static readonly List<Tween> Active = new List<Tween>(128);
        static readonly List<Tween> Pending = new List<Tween>(32);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _i = null;
            Active.Clear();
            Pending.Clear();
        }

        static void Ensure()
        {
            if (_i != null) return;
            var go = new GameObject("[Tweener]");
            DontDestroyOnLoad(go);
            _i = go.AddComponent<Tweener>();
        }

        public static Tween To(float duration, Action<float> step, UnityEngine.Object owner = null)
        {
            Ensure();
            var t = new Tween { Duration = Mathf.Max(0.0001f, duration), Step = step, Owner = owner, HasOwner = owner != null };
            Pending.Add(t);
            return t;
        }

        public static Tween Delay(float seconds, Action action, UnityEngine.Object owner = null)
            => To(seconds, null, owner).SetEase(Ease.Linear).OnComplete(action);

        public static void Kill(UnityEngine.Object owner)
        {
            if (owner == null) return;
            foreach (var t in Active) if (t.Owner == owner) t.Killed = true;
            foreach (var t in Pending) if (t.Owner == owner) t.Killed = true;
        }

        public static bool IsActive(UnityEngine.Object owner)
        {
            if (owner == null) return false;
            foreach (var t in Active) if (!t.Killed && t.Owner == owner) return true;
            foreach (var t in Pending) if (!t.Killed && t.Owner == owner) return true;
            return false;
        }

        void Update()
        {
            if (Pending.Count > 0) { Active.AddRange(Pending); Pending.Clear(); }
            float dt = Clock.Dt;
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                if (i >= Active.Count) continue;
                var t = Active[i];
                if (t.Killed || (t.HasOwner && t.Owner == null)) { Active.RemoveAt(i); continue; }
                if (t.Delay > 0f) { t.Delay -= dt; continue; }
                t.Elapsed += dt;
                float p = Mathf.Clamp01(t.Elapsed / t.Duration);
                try { t.Step?.Invoke(Tween.Eval(t.EaseType, p)); }
                catch (Exception e) { Debug.LogException(e); t.Killed = true; }
                if (p < 1f) continue;
                if (t.Loop) { t.Elapsed = 0f; continue; }
                t.Killed = true;
                Active.Remove(t);
                try { t.Done?.Invoke(); } catch (Exception e) { Debug.LogException(e); }
            }
        }

        // ------------------------------------------------------------------ helpers

        public static Tween Scale(Transform tr, Vector3 to, float d, Ease e = Ease.OutBack)
        {
            Vector3 from = tr.localScale;
            return To(d, k => tr.localScale = Vector3.LerpUnclamped(from, to, k), tr).SetEase(e);
        }

        public static Tween PopIn(Transform tr, float d = 0.35f, float delay = 0f)
        {
            Vector3 to = tr.localScale == Vector3.zero ? Vector3.one : tr.localScale;
            tr.localScale = Vector3.zero;
            return To(d, k => tr.localScale = to * k, tr).SetEase(Ease.OutBack).SetDelay(delay);
        }

        public static Tween Punch(Transform tr, float amount = 0.12f, float d = 0.25f)
        {
            Vector3 b = Vector3.one;
            return To(d, k => tr.localScale = b * (1f + Mathf.Sin(k * Mathf.PI) * amount), tr)
                .SetEase(Ease.Linear).OnComplete(() => { if (tr) tr.localScale = b; });
        }

        public static Tween Fade(CanvasGroup cg, float to, float d, Ease e = Ease.OutQuad)
        {
            float from = cg.alpha;
            return To(d, k => cg.alpha = Mathf.LerpUnclamped(from, to, k), cg).SetEase(e);
        }

        public static Tween Move(RectTransform rt, Vector2 to, float d, Ease e = Ease.OutCubic)
        {
            Vector2 from = rt.anchoredPosition;
            return To(d, k => rt.anchoredPosition = Vector2.LerpUnclamped(from, to, k), rt).SetEase(e);
        }

        public static Tween Tint(Graphic g, Color to, float d, Ease e = Ease.OutQuad)
        {
            Color from = g.color;
            return To(d, k => g.color = Color.LerpUnclamped(from, to, k), g).SetEase(e);
        }

        public static Tween Count(Text label, long from, long to, float d, string format = "N0")
            => To(d, k => label.text = ((long)Mathf.Lerp(from, to, k)).ToString(format), label).SetEase(Ease.OutCubic);
    }
}
