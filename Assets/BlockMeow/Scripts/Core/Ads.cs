using System;
using UnityEngine;

namespace BlockMeow
{
    public enum AdPlacement { Revive, DoubleCoins, Booster, Spin, Theme, FreeCoins, SkillCharge, CatBox, CatTrial }

    /// <summary>
    /// The game's only revenue source. All placement rules live here; the actual ad is shown by
    /// <see cref="IAdProvider"/> (a simulated provider in this build - swap in LevelPlay / AppLovin MAX / AdMob).
    /// </summary>
    public static class Ads
    {
        public interface IAdProvider
        {
            void ShowInterstitial(Action closed);
            void ShowRewarded(Action<bool> finished);
            void SetBanner(bool visible);
        }

        public static IAdProvider Provider;

        /// <summary>Size of the per-day counter array in the save file (room for future placements).</summary>
        public const int SlotCount = 12;

        // daily caps per rewarded placement (Revive / DoubleCoins / SkillCharge are capped per game by the game itself)
        static readonly int[] DailyCap = { 999, 999, 5, 3, 999, 5, 999, 5, 3 };

        // interstitial frequency rules (remote-config candidates)
        public const int FreeGamesBeforeInterstitial = 3;
        public const int MinSecondsBetweenInterstitials = 90;
        public const int SkipAfterRewardedSeconds = 45;

        public static bool BannerVisible { get; private set; }

        public static void SetBanner(bool on)
        {
            BannerVisible = on;
            Provider?.SetBanner(on);
        }

        public static int Remaining(AdPlacement p) => Mathf.Max(0, DailyCap[(int)p] - Profile.AdCount(p));
        public static bool CanReward(AdPlacement p) => Remaining(p) > 0;

        public static void Rewarded(AdPlacement p, Action<bool> done)
        {
            if (!CanReward(p))
            {
                UIRoot.I?.Toast("오늘은 더 이상 볼 수 없어요. 내일 다시 만나요!");
                done?.Invoke(false);
                return;
            }
            Track.Log("ad_rewarded_request", p.ToString());
            if (Provider == null) { Grant(p, done); return; }
            Provider.ShowRewarded(ok =>
            {
                if (ok) Grant(p, done);
                else done?.Invoke(false);
            });
        }

        static void Grant(AdPlacement p, Action<bool> done)
        {
            Profile.AdWatched(p);
            Track.Log("ad_rewarded_complete", p.ToString());
            done?.Invoke(true);
        }

        /// <summary>Shows an interstitial between games when the frequency rules allow, then continues.</summary>
        public static void BetweenGames(Action then)
        {
            var d = Profile.D;
            long now = Profile.Now;
            d.gamesSinceInterstitial++;
            bool allowed = d.gamesPlayed > FreeGamesBeforeInterstitial
                           && now - d.lastInterstitial >= MinSecondsBetweenInterstitials
                           && now - d.lastRewarded >= SkipAfterRewardedSeconds;
            if (!allowed || Provider == null) { then?.Invoke(); return; }
            d.lastInterstitial = now;
            d.gamesSinceInterstitial = 0;
            Profile.MarkDirty();
            Track.Log("ad_interstitial");
            Provider.ShowInterstitial(() => then?.Invoke());
        }
    }

    /// <summary>Analytics stub. Replace with Firebase / Unity Analytics events when integrating.</summary>
    public static class Track
    {
        public static void Log(string evt, string detail = null)
        {
            if (Debug.isDebugBuild) Debug.Log($"[Track] {evt} {detail}");
        }
    }

    public static class Haptics
    {
        public static void Tap()
        {
            if (Profile.D == null || !Profile.D.vibration) return;
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            Handheld.Vibrate();
#endif
        }
    }
}
