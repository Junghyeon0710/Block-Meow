using System;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace BlockMeow
{
    /// <summary>
    /// Friend challenge codes without a server: an 8-character code ("K7Q2-MZ4P") carries the game seed,
    /// the score to beat and a checksum. Whoever enters it plays the same opening pieces and tries to beat the score.
    /// </summary>
    public static class ChallengeCode
    {
        const string Alphabet = "0123456789ABCDEFGHJKMNPQRSTVWXYZ"; // Crockford base32: no I, L, O, U
        public const int SeedBits = 14, ScoreBits = 22;
        public const int MaxSeed = (1 << SeedBits) - 1, MaxScore = (1 << ScoreBits) - 1;
        const ulong Mask40 = (1UL << 40) - 1;
        const ulong Scramble = 0x9E3779B97FUL & Mask40;

        static int Check(int seed, int score)
        {
            uint h = ((uint)seed * 2654435761u) ^ ((uint)score * 40503u) ^ 0x5bd1e995u;
            h ^= h >> 15;
            h *= 0x2c1b3c6du;
            h ^= h >> 12;
            return (int)(h & 0xF);
        }

        public static string Encode(int seed, int score)
        {
            seed &= MaxSeed;
            score = Mathf.Clamp(score, 0, MaxScore);
            ulong v = ((ulong)(uint)seed << 26) | ((ulong)(uint)score << 4) | (uint)Check(seed, score);
            v ^= Scramble;
            var c = new char[8];
            for (int i = 7; i >= 0; i--) { c[i] = Alphabet[(int)(v & 31)]; v >>= 5; }
            return new string(c, 0, 4) + "-" + new string(c, 4, 4);
        }

        public static bool TryDecode(string code, out int seed, out int score)
        {
            seed = score = 0;
            if (string.IsNullOrEmpty(code)) return false;
            var sb = new StringBuilder(8);
            foreach (char raw in code.ToUpperInvariant())
            {
                char ch = raw == 'O' ? '0' : raw == 'I' || raw == 'L' ? '1' : raw;
                if (ch == '-' || ch == ' ' || ch == '\n' || ch == '\r' || ch == '\t') continue;
                if (Alphabet.IndexOf(ch) < 0) return false;
                sb.Append(ch);
            }
            if (sb.Length != 8) return false;
            ulong v = 0;
            for (int i = 0; i < 8; i++) v = (v << 5) | (uint)Alphabet.IndexOf(sb[i]);
            v ^= Scramble;
            int chk = (int)(v & 0xF);
            score = (int)((v >> 4) & MaxScore);
            seed = (int)((v >> 26) & MaxSeed);
            return chk == Check(seed, score);
        }

        /// <summary>Finds a valid code inside pasted text (the whole share message is fine).</summary>
        public static bool TryExtract(string text, out string code)
        {
            code = null;
            if (string.IsNullOrEmpty(text)) return false;
            foreach (Match m in Regex.Matches(text, "[0-9A-Za-z]{4}[- ]?[0-9A-Za-z]{4}"))
            {
                if (!TryDecode(m.Value, out int seed, out int score)) continue;
                code = Encode(seed, score);
                return true;
            }
            return false;
        }

        public static string StoreUrl => "https://play.google.com/store/apps/details?id=" + Application.identifier;

        public static string Message(string code, int score, string cat)
            => $"블록냥 도전장이 도착했어요!\n{Pal.WaGwa(cat)} 함께 {score:N0}점! 넘을 수 있을까요?\n도전 코드: {code}\n블록냥 > 친구 대결 > 붙여넣기\n{StoreUrl}";
    }

    /// <summary>Native text share sheet on Android; clipboard elsewhere (editor, desktop).</summary>
    public static class Share
    {
        public static void Text(string text, string title = "공유하기")
        {
            Track.Log("share", title);
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var intentClass = new AndroidJavaClass("android.content.Intent"))
                using (var intent = new AndroidJavaObject("android.content.Intent"))
                {
                    intent.Call<AndroidJavaObject>("setAction", intentClass.GetStatic<string>("ACTION_SEND"));
                    intent.Call<AndroidJavaObject>("setType", "text/plain");
                    intent.Call<AndroidJavaObject>("putExtra", intentClass.GetStatic<string>("EXTRA_TEXT"), text);
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var chooser = intentClass.CallStatic<AndroidJavaObject>("createChooser", intent, title))
                        activity.Call("startActivity", chooser);
                }
                return;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Share] native share failed: " + e.Message);
            }
#endif
            GUIUtility.systemCopyBuffer = text;
            UIRoot.I?.Toast("복사했어요! 메신저에 붙여넣어 보내세요");
        }
    }
}
