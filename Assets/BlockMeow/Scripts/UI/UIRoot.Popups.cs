using System;

namespace BlockMeow
{
    /// <summary>Entry points the game logic uses to open popups.</summary>
    public sealed partial class UIRoot
    {
        public void ShowRevive(int score, Action onAd, Action onSkip) => Open<RevivePopup>().Init(score, onAd, onSkip);

        public void ShowClassicResult(int score, int best, bool newBest, long coins, int challengeTarget)
            => Open<ClassicResultPopup>().Init(score, best, newBest, coins, challengeTarget);

        public void ShowLevelComplete(int level, long coins, int cat, string catLine)
            => Open<LevelCompletePopup>().Init(GameMode.Adventure, level, coins, cat, 0, catLine);

        public void ShowDailyComplete(long coins, int streak) => Open<LevelCompletePopup>().Init(GameMode.Daily, 0, coins, -1, streak, null);

        public void ShowLevelFailed(GameMode mode, int level, int[] goals) => Open<LevelFailedPopup>().Init(mode, level, goals);

        public void ShowBoosterOffer(Booster b, Action onGot) => Open<BoosterOfferPopup>().Init(b, onGot);
    }
}
