using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace BlockMeow
{
    /// <summary>Voice-limited SFX playback and cross-faded music loops, all synthesized at startup.</summary>
    public sealed class AudioManager : MonoBehaviour
    {
        public static AudioManager I { get; private set; }

        readonly AudioClip[] _sfx = new AudioClip[(int)Sfx.Count];
        readonly float[] _last = new float[(int)Sfx.Count];
        readonly Dictionary<Music, AudioClip> _songs = new Dictionary<Music, AudioClip>();
        AudioSource[] _pool;
        AudioSource _a, _b;
        bool _aOn;
        int _next;
        Music _current = Music.None, _wanted = Music.None;
        float _fade = 1f;
        bool _ready;

        const float MusicVol = 0.45f;

        void Awake()
        {
            I = this;
            _pool = new AudioSource[16];
            for (int i = 0; i < _pool.Length; i++)
            {
                _pool[i] = gameObject.AddComponent<AudioSource>();
                _pool[i].playOnAwake = false;
            }
            _a = gameObject.AddComponent<AudioSource>();
            _b = gameObject.AddComponent<AudioSource>();
            foreach (var s in new[] { _a, _b }) { s.loop = true; s.playOnAwake = false; s.volume = 0f; s.priority = 0; }
        }

        public IEnumerator Init()
        {
            int n = (int)Sfx.Count;
            var data = new float[n][];
            var task = Task.Run(() => Parallel.For(0, n, i => data[i] = Synth.MakeSfx((Sfx)i)));
            while (!task.IsCompleted) yield return null;
            if (task.Exception != null) Debug.LogException(task.Exception);
            for (int i = 0; i < n; i++)
            {
                var d = data[i] ?? new float[64];
                var clip = AudioClip.Create(((Sfx)i).ToString(), d.Length, 1, Synth.SR, false);
                clip.SetData(d, 0);
                _sfx[i] = clip;
            }
            _ready = true;
            StartCoroutine(BuildMusic());
        }

        IEnumerator BuildMusic()
        {
            foreach (var m in new[] { Music.Home, Music.Game, Music.Fever })
            {
                float[] samples = null;
                var task = Task.Run(() => samples = Synth.MakeMusic(m));
                while (!task.IsCompleted) yield return null;
                if (task.Exception != null) { Debug.LogException(task.Exception); continue; }
                var clip = AudioClip.Create("Music_" + m, samples.Length, 1, Synth.SR, false);
                clip.SetData(samples, 0);
                _songs[m] = clip;
                if (_wanted == m && _current != m) Switch(m);
            }
        }

        public static void Play(Sfx s, float volume = 1f, float pitch = 1f)
        {
            if (I == null || !I._ready || Profile.D == null || !Profile.D.sfx) return;
            I.PlayInternal(s, volume, pitch);
        }

        void PlayInternal(Sfx s, float volume, float pitch)
        {
            int i = (int)s;
            float now = Time.unscaledTime;
            if (now - _last[i] < 0.035f) return;
            _last[i] = now;
            AudioSource src = null;
            for (int k = 0; k < _pool.Length; k++)
            {
                var c = _pool[(_next + k) % _pool.Length];
                if (!c.isPlaying) { src = c; _next = (_next + k + 1) % _pool.Length; break; }
            }
            if (src == null) { src = _pool[_next]; _next = (_next + 1) % _pool.Length; }
            src.clip = _sfx[i];
            float boost = s == Sfx.Fever ? 2.2f : s == Sfx.GameOver ? 1.8f : s == Sfx.Deal ? 1.4f : 1f;
            src.volume = Mathf.Clamp01(volume * boost);
            src.pitch = pitch;
            src.Play();
        }

        public static void PlayMusic(Music m)
        {
            if (I == null) return;
            I._wanted = m;
            if (m == I._current) return;
            if (m == Music.None || I._songs.ContainsKey(m)) I.Switch(m);
        }

        void Switch(Music m)
        {
            _current = m;
            _aOn = !_aOn;
            var src = _aOn ? _a : _b;
            if (m != Music.None && _songs.TryGetValue(m, out var clip))
            {
                src.clip = clip;
                src.volume = 0f;
                src.Play();
            }
            else src.Stop();
            _fade = 0f;
        }

        bool _duck;

        /// <summary>Mutes the music while a (simulated) ad is on screen.</summary>
        public static void PlayMusicDuck(bool duck) { if (I != null) I._duck = duck; }

        void Update()
        {
            float target = Profile.D != null && Profile.D.music && !_duck ? MusicVol : 0f;
            _fade = Mathf.MoveTowards(_fade, 1f, Time.unscaledDeltaTime / 0.8f);
            var on = _aOn ? _a : _b;
            var off = _aOn ? _b : _a;
            on.volume = target * _fade;
            off.volume = target * (1f - _fade);
            if (_fade >= 1f && off.isPlaying) off.Stop();
        }
    }
}
